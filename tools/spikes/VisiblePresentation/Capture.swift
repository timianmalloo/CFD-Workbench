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
    let launchReferenceBits: UInt64
    let startSeconds: UInt64
    let startMicroseconds: UInt64
    let bundle: String
    let title: String
    let durationSeconds: Int
    let width: Int
    let height: Int
    let crops: [Crop]
    let packageRoot: String
    let packageFiles: [String: String]
    let helperSHA256: String
    let contextFiles: [String: String]
    let contextRoot: String
    let frameCap: Int
    let geometry: Geometry
}
struct Geometry: Decodable, Equatable {
    let x: Double; let y: Double; let width: Double; let height: Double; let scale: Double
}
struct ProcessIdentity: Equatable {
    let pid: Int32; let seconds: UInt64; let microseconds: UInt64
    let executable: String; let bundle: String; let launchBits: UInt64
}
func identityEqual(_ expected: ProcessIdentity, _ observed: ProcessIdentity?) -> Bool {
    guard let observed else { return false }
    return expected == observed
}
func hexDigest(_ data: Data) -> String { SHA256.hash(data: data).map { String(format: "%02x", $0) }.joined() }
func hashFile(_ path: String) -> String? {
    guard let data = try? Data(contentsOf: URL(fileURLWithPath: path)) else { return nil }
    return hexDigest(data)
}
func safeRelative(_ path: String) -> Bool {
    !path.isEmpty && !path.hasPrefix("/") && !path.contains("\\")
        && path.split(separator: "/", omittingEmptySubsequences: false).allSatisfy { !$0.isEmpty && $0 != "." && $0 != ".." }
}
func digestValid(_ value: String) -> Bool { value.count == 64 && value.allSatisfy { "0123456789abcdef".contains($0) } }
func sealedFiles(_ root: String, _ expected: [String: String]) -> Bool {
    let base = URL(fileURLWithPath: root).standardizedFileURL
    guard base.path == root, base.resolvingSymlinksInPath().path == root,
          !expected.isEmpty, expected.count <= 4096,
          expected.allSatisfy({ safeRelative($0.key) && digestValid($0.value) }),
          Set(expected.keys.map { $0.lowercased() }).count == expected.count,
          let walker = FileManager.default.enumerator(at: base, includingPropertiesForKeys: [.isRegularFileKey, .isSymbolicLinkKey]) else { return false }
    var seen = Set<String>()
    for case let url as URL in walker {
        guard let values = try? url.resourceValues(forKeys: [.isRegularFileKey, .isSymbolicLinkKey]),
              values.isSymbolicLink != true else { return false }
        if values.isRegularFile == true {
            let relative = String(url.path.dropFirst(root.count + 1))
            guard expected[relative] == hashFile(url.path) else { return false }
            seen.insert(relative)
        }
    }
    return seen == Set(expected.keys)
}
func decodeRequest(_ data: Data) -> Request? {
    let keys: Set<String> = ["pid", "window", "executable", "executableSHA256", "launchReferenceBits", "startSeconds", "startMicroseconds", "bundle", "title",
        "durationSeconds", "width", "height", "crops", "packageRoot", "packageFiles", "helperSHA256", "contextFiles", "contextRoot", "frameCap", "geometry"]
    guard data.count < 1_000_000,
          let object = try? JSONSerialization.jsonObject(with: data) as? [String: Any], Set(object.keys) == keys,
          let canonical = try? JSONSerialization.data(withJSONObject: object, options: [.sortedKeys, .withoutEscapingSlashes]), canonical == data,
          let crops = object["crops"] as? [[String: Any]], crops.allSatisfy({ Set($0.keys) == ["x", "y", "width", "height", "expected"] }),
          let geometry = object["geometry"] as? [String: Any], Set(geometry.keys) == ["x", "y", "width", "height", "scale"],
          let request = try? JSONDecoder().decode(Request.self, from: data),
          request.startSeconds > 0, request.startMicroseconds < 1_000_000,
          request.pid > 0, request.window > 0, (1...8).contains(request.durationSeconds), (1...600).contains(request.frameCap),
          (192...2048).contains(request.width), (192...2048).contains(request.height), request.crops.count == 3,
          [request.geometry.x, request.geometry.y, request.geometry.width, request.geometry.height, request.geometry.scale].allSatisfy({ $0.isFinite }),
          request.geometry.scale > 0, request.geometry.width > 0, request.geometry.height > 0,
          request.geometry.width * request.geometry.scale == Double(request.width),
          request.geometry.height * request.geometry.scale == Double(request.height),
          request.executable == request.packageRoot + "/VisiblePresentation",
          request.packageFiles["VisiblePresentation"] == request.executableSHA256,
          ["VisiblePresentation", "VisiblePresentation.dll", "VisiblePresentation.deps.json", "VisiblePresentation.runtimeconfig.json"].allSatisfy({ request.packageFiles[$0] != nil }),
          request.packageFiles.allSatisfy({ safeRelative($0.key) && digestValid($0.value) }),
          digestValid(request.helperSHA256), request.contextRoot.hasPrefix("/"),
          Set(request.contextFiles.keys) == ["tools/spikes/VisiblePresentation/Capture.swift", "tools/spikes/VisiblePresentation/Program.cs",
              "tools/spikes/VisiblePresentation/VisiblePresentation.csproj", "tools/qualify-visible-presentation.py", "global.json"],
          request.contextFiles.allSatisfy({ digestValid($0.value) }),
          request.crops.allSatisfy({ $0.x >= 0 && $0.y >= 0 && $0.width >= 64 && $0.height >= 64
              && $0.width <= request.width && $0.height <= request.height
              && $0.x <= request.width - $0.width && $0.y <= request.height - $0.height
              && $0.width % 16 == 0 && $0.height % 16 == 0 && digestValid($0.expected) }), nonoverlapping(request.crops)
    else { return nil }
    return request
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
    func identityMatches() -> Bool {
        var info = proc_bsdinfo()
        let size = Int32(MemoryLayout<proc_bsdinfo>.size)
        guard proc_pidinfo(request.pid, PROC_PIDTBSDINFO, 0, &info, size) == size,
              info.pbi_pid == UInt32(request.pid) else { return false }
        guard let app = NSRunningApplication(processIdentifier: request.pid), !app.isTerminated,
              let executable = app.executableURL?.path, let launched = app.launchDate,
              identityEqual(ProcessIdentity(pid: request.pid, seconds: request.startSeconds, microseconds: request.startMicroseconds,
                    executable: request.executable, bundle: request.bundle, launchBits: request.launchReferenceBits),
                ProcessIdentity(pid: Int32(info.pbi_pid), seconds: info.pbi_start_tvsec, microseconds: info.pbi_start_tvusec,
                    executable: executable, bundle: app.bundleIdentifier ?? "", launchBits: launched.timeIntervalSinceReferenceDate.bitPattern)),
              sealedFiles(request.packageRoot, request.packageFiles),
              hashFile(CommandLine.arguments[0]) == request.helperSHA256,
              request.contextFiles.allSatisfy({ hashFile(request.contextRoot + "/" + $0.key) == $0.value }) else { return false }
        return true
    }
    func windowMatches() -> Bool {
        guard let rows = CGWindowListCopyWindowInfo(.optionIncludingWindow, request.window) as? [[String: Any]],
              rows.count == 1, let row = rows.first,
              (row[kCGWindowOwnerPID as String] as? NSNumber)?.int32Value == request.pid,
              (row[kCGWindowNumber as String] as? NSNumber)?.uint32Value == request.window,
              let bounds = row[kCGWindowBounds as String] as? [String: Any],
              let rect = CGRect(dictionaryRepresentation: bounds as CFDictionary) else { return false }
        let g = request.geometry
        return rect == CGRect(x: g.x, y: g.y, width: g.width, height: g.height)
    }
    func stop(_ reason: String) {
        guard !stopping else { return }
        stopping = true
        let identityAtCompletion = identityMatches() && (stream == nil || windowMatches())
        timer?.cancel()
        emit(["kind": "stop-request", "reason": identityAtCompletion ? reason : "VP-IDENTITY-COMPLETION", "frames": sequence,
              "identity_at_completion": identityAtCompletion,
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
        guard identityMatches() else { stop("VP-TARGET-MISMATCH"); return }
        guard CGPreflightScreenCaptureAccess() else { stop("VP-AUTHORIZATION-UNAVAILABLE"); return }
        // Future authorized metadata lookup only. Never print other-window metadata or capture it.
        SCShareableContent.getExcludingDesktopWindows(true, onScreenWindowsOnly: true) { content, error in
            DispatchQueue.main.async {
                guard !self.stopping else { return }
                guard error == nil, let content else { self.stop("VP-CONTENT-UNAVAILABLE"); return }
                guard self.identityMatches(), self.windowMatches() else { self.stop("VP-IDENTITY-AFTER-LOOKUP"); return }
                let matches = content.windows.filter {
                    $0.windowID == self.request.window && $0.owningApplication?.processID == self.request.pid
                    && $0.title == self.request.title && $0.isOnScreen
                }
                guard matches.count == 1 else { self.stop("VP-TARGET-MISMATCH"); return }
                let filter = SCContentFilter(desktopIndependentWindow: matches[0])
                let g = self.request.geometry
                guard matches[0].frame == CGRect(x: g.x, y: g.y, width: g.width, height: g.height),
                      Double(filter.pointPixelScale) == g.scale,
                      filter.contentRect.size == CGSize(width: g.width, height: g.height)
                else { self.stop("VP-GEOMETRY"); return }
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
        guard sequence <= request.frameCap else { stop("VP-FRAME-LIMIT"); return }
        guard identityMatches(), windowMatches() else { stop("VP-IDENTITY-FRAME"); return }
        guard let attachments = CMSampleBufferGetSampleAttachmentsArray(sampleBuffer, createIfNecessary: false) as? [[SCStreamFrameInfo: Any]],
              let first = attachments.first,
              let rawStatus = first[.status] as? NSNumber,
              let eventNumber = first[.displayTime] as? NSNumber else { stop("VP-FRAME-METADATA"); return }
        let event = eventNumber.uint64Value
        emit(["kind": "frame", "sequence": sequence, "display_mach": event, "receipt_mach": receipt,
              "status": rawStatus.intValue, "scale": first[.scaleFactor] as? NSNumber ?? 0,
              "content_scale": first[.contentScale] as? NSNumber ?? 0,
              "content_rect": String(describing: first[.contentRect] ?? "missing")])
        guard rawStatus.intValue == SCFrameStatus.complete.rawValue else { stop("VP-FRAME-INCOMPLETE"); return }
        // Native attachment coordinate mapping has not been measured. Refuse ambiguous geometry.
        guard let rectDictionary = first[.contentRect] as? [String: Any],
              let rect = CGRect(dictionaryRepresentation: rectDictionary as CFDictionary),
              rect == CGRect(x: 0, y: 0, width: request.geometry.width, height: request.geometry.height),
              (first[.scaleFactor] as? NSNumber)?.doubleValue == request.geometry.scale,
              (first[.contentScale] as? NSNumber)?.doubleValue == 1
        else { stop("VP-GEOMETRY-FRAME"); return }
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
if CommandLine.arguments == [CommandLine.arguments[0], "--identity-contracts"] {
    let expected = ProcessIdentity(pid: 7, seconds: 10, microseconds: 123, executable: "/synthetic/target", bundle: "synthetic", launchBits: 1)
    let cases: [(String, ProcessIdentity?, Bool)] = [
        ("stable", expected, true), ("exited-or-unverifiable", nil, false),
        ("pid-replaced", ProcessIdentity(pid: 8, seconds: 10, microseconds: 123, executable: "/synthetic/target", bundle: "synthetic", launchBits: 1), false),
        ("start-seconds-changed", ProcessIdentity(pid: 7, seconds: 11, microseconds: 123, executable: "/synthetic/target", bundle: "synthetic", launchBits: 1), false),
        ("start-microseconds-changed", ProcessIdentity(pid: 7, seconds: 10, microseconds: 124, executable: "/synthetic/target", bundle: "synthetic", launchBits: 1), false),
        ("executable-changed", ProcessIdentity(pid: 7, seconds: 10, microseconds: 123, executable: "/other", bundle: "synthetic", launchBits: 1), false),
        ("bundle-changed", ProcessIdentity(pid: 7, seconds: 10, microseconds: 123, executable: "/synthetic/target", bundle: "other", launchBits: 1), false),
        ("launch-bits-changed", ProcessIdentity(pid: 7, seconds: 10, microseconds: 123, executable: "/synthetic/target", bundle: "synthetic", launchBits: 2), false)]
    let rows = cases.map { ["case": $0.0, "expected": $0.2, "actual": identityEqual(expected, $0.1), "passed": identityEqual(expected, $0.1) == $0.2] as [String: Any] }
    emit(["qualification": "pure-identity-contract", "cases": rows, "native_calls": 0])
    exit(cases.allSatisfy { identityEqual(expected, $0.1) == $0.2 } ? 0 : 1)
}
if CommandLine.arguments.count == 3, CommandLine.arguments[1] == "--contract-request" {
    let bytes = (try? Data(contentsOf: URL(fileURLWithPath: CommandLine.arguments[2]))) ?? Data()
    emit(["code": decodeRequest(bytes) == nil ? "VP-CONFIG-REFUSED" : "VP-CONFIG-STRUCTURAL-ONLY",
          "native_calls": 0, "visible_latency": "Not assessed"])
    exit(decodeRequest(bytes) == nil ? 3 : 0)
}
guard CommandLine.arguments.count == 3, CommandLine.arguments[1] == "--capture-reviewed" else {
    emit(["code": "VP-REVIEW-AUTHORIZATION-REQUIRED", "visible_latency": "Not assessed"])
    exit(3)
}
guard #available(macOS 14.0, *),
      let data = try? Data(contentsOf: URL(fileURLWithPath: CommandLine.arguments[2])),
      let request = decodeRequest(data)
else { emit(["code": "VP-CONFIG-REFUSED"]); exit(3) }
let observer = Observer(request)
observer.start()
dispatchMain()
