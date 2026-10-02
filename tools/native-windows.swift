// Lists every native window one process owns, with its bounds in screen points (top-left origin), layer and alpha.
// Use it to name the surface that shows a piece of the UI (NS-3): `swift tools/native-windows.swift <pid>`,
// with the pid from `pgrep -f CfdWorkbench`. macOS only; reads CGWindowList and needs no screen-recording permission
// for bounds (window names may be empty without it).
import CoreGraphics
import Foundation

guard CommandLine.arguments.count == 2, let pid = Int32(CommandLine.arguments[1]) else {
    FileHandle.standardError.write("usage: swift tools/native-windows.swift <pid>\n".data(using: .utf8)!)
    exit(2)
}
let windows = (CGWindowListCopyWindowInfo([.optionAll], kCGNullWindowID) as? [[String: Any]] ?? [])
    .filter { ($0[kCGWindowOwnerPID as String] as? Int32) == pid }
for window in windows {
    let bounds = window[kCGWindowBounds as String] as? [String: Double] ?? [:]
    print("window id=\(window[kCGWindowNumber as String] as? Int ?? -1)"
        + " onscreen=\(window[kCGWindowIsOnscreen as String] as? Bool ?? false)"
        + " layer=\(window[kCGWindowLayer as String] as? Int ?? -1)"
        + " alpha=\(window[kCGWindowAlpha as String] as? Double ?? -1)"
        + " x=\(bounds["X"] ?? -1) y=\(bounds["Y"] ?? -1) w=\(bounds["Width"] ?? -1) h=\(bounds["Height"] ?? -1)"
        + " name=\(window[kCGWindowName as String] as? String ?? "")")
}
print("windows \(windows.count) for pid \(pid)")
