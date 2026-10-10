#include <cuda_runtime.h>

#include <algorithm>
#include <cmath>
#include <cstddef>
#include <fstream>
#include <iomanip>
#include <iostream>
#include <sstream>
#include <stdexcept>
#include <string>
#include <vector>

#define CUDA_CHECK(call) do { \
    cudaError_t status_ = (call); \
    if (status_ != cudaSuccess) { \
        std::ostringstream message_; \
        message_ << #call << ": " << cudaGetErrorString(status_); \
        throw std::runtime_error(message_.str()); \
    } \
} while (false)

__global__ void saxpy_kernel(std::size_t count, float alpha, const float* x, float* y, int iteration, int* marker) {
    const std::size_t index = blockIdx.x * blockDim.x + threadIdx.x;
    if (index < count) {
        y[index] = alpha * x[index] + y[index];
    }
    if (index == 0) {
        *marker = iteration;
    }
}

static std::string json_escape(const char* value) {
    std::string result;
    for (const char* cursor = value; *cursor != '\0'; ++cursor) {
        if (*cursor == '\\' || *cursor == '"') result.push_back('\\');
        result.push_back(*cursor);
    }
    return result;
}

int main(int argc, char** argv) {
    if (argc != 3) {
        std::cerr << "usage: saxpy RESULT_JSON DEVICE_MARKER\n";
        return 64;
    }
    try {
        constexpr std::size_t count = 1u << 24;
        constexpr float alpha = 1.75f;
        constexpr int iterations = 6;
        constexpr double tolerance = 1.0e-4;
        int device_count = 0;
        CUDA_CHECK(cudaGetDeviceCount(&device_count));
        if (device_count < 1) throw std::runtime_error("no CUDA device is available");
        CUDA_CHECK(cudaSetDevice(0));

        cudaDeviceProp properties{};
        CUDA_CHECK(cudaGetDeviceProperties(&properties, 0));
        int managed_memory = 0;
        int concurrent_managed_access = 0;
        CUDA_CHECK(cudaDeviceGetAttribute(&managed_memory, cudaDevAttrManagedMemory, 0));
        CUDA_CHECK(cudaDeviceGetAttribute(&concurrent_managed_access, cudaDevAttrConcurrentManagedAccess, 0));
        if (managed_memory != 1) throw std::runtime_error("selected device does not report managed-memory support");

        int driver_version = 0;
        int runtime_version = 0;
        CUDA_CHECK(cudaDriverGetVersion(&driver_version));
        CUDA_CHECK(cudaRuntimeGetVersion(&runtime_version));
        std::size_t free_before = 0;
        std::size_t total_bytes = 0;
        CUDA_CHECK(cudaMemGetInfo(&free_before, &total_bytes));

        float* x = nullptr;
        float* y = nullptr;
        int* marker = nullptr;
        CUDA_CHECK(cudaMallocManaged(&x, count * sizeof(float)));
        CUDA_CHECK(cudaMallocManaged(&y, count * sizeof(float)));
        CUDA_CHECK(cudaMallocManaged(&marker, sizeof(int)));
        *marker = 0;
        for (std::size_t index = 0; index < count; ++index) {
            x[index] = static_cast<float>(index % 251) / 251.0f;
            y[index] = 0.25f + static_cast<float>(index % 97) * 0.002f;
        }

        cudaEvent_t start{};
        cudaEvent_t stop{};
        CUDA_CHECK(cudaEventCreate(&start));
        CUDA_CHECK(cudaEventCreate(&stop));
        std::vector<float> elapsed;
        elapsed.reserve(iterations);
        const unsigned int threads = 256;
        const unsigned int blocks = static_cast<unsigned int>((count + threads - 1) / threads);
        for (int iteration = 1; iteration <= iterations; ++iteration) {
            CUDA_CHECK(cudaEventRecord(start));
            saxpy_kernel<<<blocks, threads>>>(count, alpha, x, y, iteration, marker);
            CUDA_CHECK(cudaGetLastError());
            CUDA_CHECK(cudaEventRecord(stop));
            CUDA_CHECK(cudaEventSynchronize(stop));
            float milliseconds = 0.0f;
            CUDA_CHECK(cudaEventElapsedTime(&milliseconds, start, stop));
            elapsed.push_back(milliseconds);
        }
        CUDA_CHECK(cudaDeviceSynchronize());

        double max_abs_error = 0.0;
        double max_rel_error = 0.0;
        std::size_t mismatch_count = 0;
        bool oracle_finite = true;
        for (std::size_t index = 0; index < count; ++index) {
            const float initial = 0.25f + static_cast<float>(index % 97) * 0.002f;
            const float reference_x = static_cast<float>(index % 251) / 251.0f;
            const float actual_x = x[index];
            const float expected = initial + static_cast<float>(iterations) * alpha * reference_x;
            const double actual_value = static_cast<double>(y[index]);
            const double expected_value = static_cast<double>(expected);
            const double absolute_error = std::abs(actual_value - expected_value);
            if (!std::isfinite(actual_x) || !std::isfinite(reference_x) || !std::isfinite(actual_value) ||
                !std::isfinite(expected_value) || !std::isfinite(absolute_error)) {
                oracle_finite = false;
                ++mismatch_count;
                continue;
            }
            const double relative_error = absolute_error / std::max(std::abs(expected_value), 1.0e-12);
            max_abs_error = std::max(max_abs_error, absolute_error);
            max_rel_error = std::max(max_rel_error, relative_error);
            if (actual_x != reference_x || absolute_error > tolerance) ++mismatch_count;
        }
        const int device_marker = *marker;
        const bool passed = oracle_finite && device_marker == iterations && mismatch_count == 0;
        float warm_sum = 0.0f;
        for (std::size_t index = 1; index < elapsed.size(); ++index) warm_sum += elapsed[index];

        std::ofstream result(argv[1], std::ios::binary | std::ios::trunc);
        result << std::setprecision(10)
               << "{\n"
               << "  \"schema\": \"cfdw-cuda-managed-saxpy/1\",\n"
               << "  \"passed\": " << (passed ? "true" : "false") << ",\n"
               << "  \"device_index\": 0,\n"
               << "  \"device_count\": " << device_count << ",\n"
               << "  \"device_name\": \"" << json_escape(properties.name) << "\",\n"
               << "  \"compute_capability\": \"" << properties.major << "." << properties.minor << "\",\n"
               << "  \"managed_memory\": " << managed_memory << ",\n"
               << "  \"concurrent_managed_access\": " << concurrent_managed_access << ",\n"
               << "  \"driver_version\": " << driver_version << ",\n"
               << "  \"runtime_version\": " << runtime_version << ",\n"
               << "  \"element_count\": " << count << ",\n"
               << "  \"scalar\": " << alpha << ",\n"
               << "  \"dtype\": \"float32\",\n"
               << "  \"threads_per_block\": " << threads << ",\n"
               << "  \"block_count\": " << blocks << ",\n"
               << "  \"allocation_bytes\": " << (2 * count * sizeof(float) + sizeof(int)) << ",\n"
               << "  \"device_total_bytes\": " << total_bytes << ",\n"
               << "  \"device_free_before_bytes\": " << free_before << ",\n"
               << "  \"iterations\": " << iterations << ",\n"
               << "  \"device_written_marker\": " << device_marker << ",\n"
               << "  \"oracle_finite\": " << (oracle_finite ? "true" : "false") << ",\n"
               << "  \"mismatch_count\": " << mismatch_count << ",\n"
               << "  \"first_kernel_ms\": " << elapsed.front() << ",\n"
               << "  \"warm_mean_kernel_ms\": " << warm_sum / static_cast<float>(iterations - 1) << ",\n"
               << "  \"max_abs_error\": " << max_abs_error << ",\n"
               << "  \"max_rel_error\": " << max_rel_error << ",\n"
               << "  \"tolerance\": " << tolerance << "\n"
               << "}\n";
        result.close();
        std::ofstream marker_file(argv[2], std::ios::binary | std::ios::trunc);
        marker_file << device_marker << "\n";
        marker_file.close();

        CUDA_CHECK(cudaEventDestroy(start));
        CUDA_CHECK(cudaEventDestroy(stop));
        CUDA_CHECK(cudaFree(marker));
        CUDA_CHECK(cudaFree(y));
        CUDA_CHECK(cudaFree(x));
        if (!passed) throw std::runtime_error("full-array numeric oracle or device marker failed");
        std::cout << "SAXPY-PASS device=" << properties.name << " elements=" << count
                  << " first_ms=" << elapsed.front()
                  << " warm_mean_ms=" << warm_sum / static_cast<float>(iterations - 1)
                  << " max_abs_error=" << max_abs_error << "\n";
        return 0;
    } catch (const std::exception& error) {
        std::cerr << "SAXPY-FAIL " << error.what() << "\n";
        return 1;
    }
}
