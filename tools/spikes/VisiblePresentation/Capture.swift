// R41: PREPARED SOURCE ONLY. Do not run without exact root review and user authorization.
// No fallback capture, permission requests, screenshots, UI control, children or egress.
import Foundation
import AppKit
import ScreenCaptureKit
import CoreMedia
import CoreVideo
import CryptoKit
import Darwin

struct Crop: Decodable { let x: Int; let y: Int; let width: Int; let height: Int; let expected: String }
struct Request: Decodable {
    let pid: Int32
    let window: UInt32
    let executable: String
    let executableSHA256: String
    let launchUnixSeconds: Double
    let bundle: String
    let title: String
    let durationSeconds: Int
    let width: Int
    let height: Int
    let crops: [Crop]
}

func nonoverlapping(_ crops: [Crop]) -> Bool {
    for i in 0..<crops.count {
        for j in 0..<i {
            let a = crops[i], b = crops[j]
            if a.x < b.x + b.width && b.x < a.x + a.width && a.y < b.y + b.height && b.y < a.y + a.height { return false }
        }
    }
    return true
}

func emit(_ item: [String: Any]) {
    guard JSONSerialization.isValidJSONObject(item),
          let bytes = try? JSONSerialization.data(withJSONObject: item, options: [.sortedKeys]),
          let line = String(data: bytes, encoding: .utf8) else { exit(4) }
    print(line)
    fflush(stdout)
}

@available(macOS 14.0, *)
final class Observer: NSObject, SCStreamOutput, SCStreamDelegate {
    let request: Request
    var stream: SCStream?
    var timer: DispatchSourceTimer?
    var signalSources: [DispatchSourceSignal] = []
    var stopping = false
    var sequence = 0
    var complete = 0
    var priorEvent: UInt64 = 0

    init(_ request: Request) { self.request = request }
    func stop(_ reason: String) {
        guard !stopping else { return }
        stopping = true
        timer?.cancel()
        emit(["kind": "stop-request", "reason": reason, "frames": sequence,
              "complete": complete, "visible_latency": "Not assessed"])
        // This reports failed stop acknowledgement instead of claiming cleanup from disappearance.
        DispatchQueue.main.asyncAfter(deadline: .now() + 2) {
            emit(["kind": "cleanup", "stream_stop": "unobserved-timeout", "children": 0])
            exit(4)
        }
        guard let active = stream else {
            emit(["kind": "cleanup", "stream_stop": "not-started", "children": 0])
            exit(3)
        }
        active.stopCapture { error in
            emit(["kind": "cleanup", "stream_stop": error == nil ? "acknowledged" : "error",
                  "children": 0, "visible_latency": "Not assessed"])
            exit(error == nil ? 3 : 4)
        }
    }
    func start() {
        let deadline = DispatchSource.makeTimerSource(queue: .main)
        deadline.schedule(deadline: .now() + .seconds(request.durationSeconds))
        deadline.setEventHandler { self.stop("duration-limit") }
        timer = deadline
        deadline.resume()
        for number in [SIGINT, SIGTERM] {
            signal(number, SIG_IGN)
            let source = DispatchSource.makeSignalSource(signal: number, queue: .main)
            source.setEventHandler { self.stop("cancelled") }
            signalSources.append(source)
            source.resume()
        }
        guard CGPreflightScreenCaptureAccess() else { stop("VP-AUTHORIZATION-UNAVAILABLE"); return }
        guard let app = NSRunningApplication(processIdentifier: request.pid),
              app.executableURL?.path == request.executable,
              app.bundleIdentifier ?? "" == request.bundle,
              let launched = app.launchDate,
              abs(launched.timeIntervalSince1970 - request.launchUnixSeconds) < 0.001,
              let bytes = try? Data(contentsOf: URL(fileURLWithPath: request.executable)),
              SHA256.hash(data: bytes).map({ String(format: "%02x", $0) }).joined() == request.executableSHA256
        else { stop("VP-TARGET-MISMATCH"); return }
        // Future authorized metadata lookup only. Never print other-window metadata or capture it.
        SCShareableContent.getExcludingDesktopWindows(true, onScreenWindowsOnly: true) { content, error in
            DispatchQueue.main.async {
                guard !self.stopping else { return }
                guard error == nil, let content else { self.stop("VP-CONTENT-UNAVAILABLE"); return }
                let matches = content.windows.filter {
                    $0.windowID == self.request.window && $0.owningApplication?.processID == self.request.pid
                    && $0.title == self.request.title && $0.isOnScreen
                }
                guard matches.count == 1 else { self.stop("VP-TARGET-MISMATCH"); return }
                let filter = SCContentFilter(desktopIndependentWindow: matches[0])
                let config = SCStreamConfiguration()
                config.width = self.request.width
                config.height = self.request.height
                config.pixelFormat = kCVPixelFormatType_32BGRA
                config.minimumFrameInterval = CMTime(value: 1, timescale: 60)
                config.queueDepth = 3
                config.showsCursor = false
                config.capturesAudio = false
                config.ignoreShadowsSingleWindow = true
                let stream = SCStream(filter: filter, configuration: config, delegate: self)
                self.stream = stream
                do {
                    try stream.addStreamOutput(self, type: .screen, sampleHandlerQueue: .main)
                } catch { self.stop("VP-OUTPUT-ERROR"); return }
                emit(["kind": "target", "pid": self.request.pid, "window": self.request.window,
                      "executable_sha256": self.request.executableSHA256,
                      "width": self.request.width, "height": self.request.height,
                      "point_pixel_scale": filter.pointPixelScale,
                      "content_rect": NSStringFromRect(filter.contentRect),
                      "visibility": "window-content-only-unoccluded-not-proved",
                      "sample_loss": "unknown", "backend": "unobserved"])
                stream.startCapture { error in
                    DispatchQueue.main.async {
                        if error != nil { self.stop("VP-START-ERROR") }
                    }
                }
            }
        }
    }
    func stream(_ stream: SCStream, didStopWithError error: Error) { stop("VP-STREAM-ERROR") }
    func stream(_ stream: SCStream, didOutputSampleBuffer sampleBuffer: CMSampleBuffer, of type: SCStreamOutputType) {
        guard !stopping, type == .screen else { return }
        let receipt = mach_absolute_time()
        sequence += 1
        guard sequence <= 600 else { stop("VP-FRAME-LIMIT"); return }
        guard let attachments = CMSampleBufferGetSampleAttachmentsArray(sampleBuffer, createIfNecessary: false) as? [[SCStreamFrameInfo: Any]],
              let first = attachments.first,
              let rawStatus = first[.status] as? NSNumber,
              let eventNumber = first[.displayTime] as? NSNumber else { stop("VP-FRAME-METADATA"); return }
        let event = eventNumber.uint64Value
        emit(["kind": "frame", "sequence": sequence, "display_mach": event, "receipt_mach": receipt,
              "status": rawStatus.intValue, "scale": first[.scaleFactor] as? NSNumber ?? 0,
              "content_scale": first[.contentScale] as? NSNumber ?? 0,
              "content_rect": String(describing: first[.contentRect] ?? "missing")])
        guard rawStatus.intValue == SCFrameStatus.complete.rawValue else { return }
        guard event > priorEvent, event <= receipt,
              let buffer = CMSampleBufferGetImageBuffer(sampleBuffer),
              CVPixelBufferGetWidth(buffer) == request.width,
              CVPixelBufferGetHeight(buffer) == request.height,
              CVPixelBufferGetPixelFormatType(buffer) == kCVPixelFormatType_32BGRA
        else { stop("VP-FRAME-INVALID"); return }
        priorEvent = event
        CVPixelBufferLockBaseAddress(buffer, .readOnly)
        defer { CVPixelBufferUnlockBaseAddress(buffer, .readOnly) }
        guard let base = CVPixelBufferGetBaseAddress(buffer) else { stop("VP-BLANK"); return }
        let pixels = base.assumingMemoryBound(to: UInt8.self)
        let stride = CVPixelBufferGetBytesPerRow(buffer)
        var evidence: [[String: Any]] = []
        for crop in request.crops {
            var digest = [UInt8](repeating: 0, count: 32)
            var valid = true
            let cw = crop.width / 16, ch = crop.height / 16
            for bit in 0..<256 {
                let x0 = crop.x + (bit % 16) * cw, y0 = crop.y + (bit / 16) * ch
                var cell: Bool?
                // Entire cell interior, not a single completion marker. Edges excluded explicitly.
                for y in (y0 + 1)..<(y0 + ch - 1) {
                    for x in (x0 + 1)..<(x0 + cw - 1) {
                        let p = y * stride + x * 4
                        let black = pixels[p] < 5 && pixels[p + 1] < 5 && pixels[p + 2] < 5
                        let white = pixels[p] > 250 && pixels[p + 1] > 250 && pixels[p + 2] > 250
                        if pixels[p + 3] < 250 || (!black && !white) || (cell != nil && cell != black) { valid = false }
                        cell = black
                    }
                }
                if cell == true { digest[bit / 8] |= UInt8(1 << (bit % 8)) }
            }
            let hex = digest.map { String(format: "%02x", $0) }.joined()
            evidence.append(["bits": hex, "uniform_interiors": valid, "matches_expected": valid && hex == crop.expected])
        }
        complete += 1
        emit(["kind": "region-evidence", "sequence": sequence, "regions": evidence,
              "visible_latency": "Not assessed", "sample_loss": "unknown"])
    }
}

// No default observation and no permission-request API. Permission status alone is not authorization.
guard CommandLine.arguments.count == 3, CommandLine.arguments[1] == "--capture-reviewed" else {
    emit(["code": "VP-REVIEW-AUTHORIZATION-REQUIRED", "visible_latency": "Not assessed"])
    exit(3)
}
guard #available(macOS 14.0, *),
      let data = try? Data(contentsOf: URL(fileURLWithPath: CommandLine.arguments[2])), data.count < 16384,
      let request = try? JSONDecoder().decode(Request.self, from: data),
      request.pid > 0, request.window > 0, (1...8).contains(request.durationSeconds),
      (192...2048).contains(request.width), (192...2048).contains(request.height), request.crops.count == 3,
      request.crops.allSatisfy({ $0.x >= 0 && $0.y >= 0 && $0.width >= 64 && $0.height >= 64
          && $0.width <= request.width && $0.height <= request.height
          && $0.x <= request.width - $0.width && $0.y <= request.height - $0.height
          && $0.width % 16 == 0 && $0.height % 16 == 0 && $0.expected.count == 64 }),
      nonoverlapping(request.crops)
else { emit(["code": "VP-CONFIG-REFUSED"]); exit(3) }
let observer = Observer(request)
observer.start()
dispatchMain()
