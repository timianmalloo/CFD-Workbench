#include <fcntl.h>
#include <stdint.h>
#include <sys/types.h>

/* Fixed ABI for managed callers. The SDK declares open/openat variadic;
 * the C compiler supplies Apple's arm64 variadic calling convention. */
__attribute__((visibility("default")))
int cfd_store_open(const char *path, int flags, uint32_t mode)
{
    return open(path, flags, (mode_t)mode);
}

__attribute__((visibility("default")))
int cfd_store_openat(int parent, const char *name, int flags, uint32_t mode)
{
    return openat(parent, name, flags, (mode_t)mode);
}
