using System.Globalization;
using System.Text;

namespace CfdWorkbench.Core.SolverSetup;

/// <summary>
/// Product-published Route catalogue and Cause table (docs/design/guided-solver-setup.md §0, §3, §8; Ruling 69).
/// Pure zero-I/O data with deterministic BLAKE3/SHA-256 build-manifest verification (A8.5).
/// </summary>
public static class RouteCatalogue
{
    public const string MacOpenFoamId = "mac-ofapp-2512";
    public const string WinWslOpenFoamId = "win-wsl-of2512";
    public const string WinSu2Id = "win-su2-850";

    public const string OpenFoamGpgFingerprint = "DC93C096174122E256DA24063386DD74948D208F";
    public const string DefaultWslDistroName = "cfdw-openfoam2512";

    public const string Gpl3SummaryText =
        "GNU GENERAL PUBLIC LICENSE Version 3, 29 June 2007. Copyright (C) 2007 Free Software Foundation, Inc. " +
        "Everyone is permitted to copy and distribute verbatim copies of this license document, but changing it is not allowed. " +
        "OpenFOAM is free software distributed under GPL-3.0 without any warranty.";

    public const string Lgpl21SummaryText =
        "GNU LESSER GENERAL PUBLIC LICENSE Version 2.1, February 1999. Copyright (C) 1991, 1999 Free Software Foundation, Inc. " +
        "SU2 is distributed under LGPL-2.1 without any warranty.";

    public static IReadOnlyList<CauseEntry> AllCauses { get; } =
    [
        new("win.build-too-old", "Windows needs an update before WSL can run.", "Open Windows Update in Settings, install updates, then check again.", null, "Inferred"),
        new("win.virtualization-off", "Virtualization is off in this PC's firmware.", "Restart into your PC's firmware setup, turn on virtualization, save and restart, then check again.", null, "Inferred"),
        new("win.wsl-feature-missing", "WSL is not turned on yet.", "Start step 1 again to turn on WSL.", "wsl.enable", "Inferred"),
        new("win.uac-declined", "You chose No. Nothing changed.", "Start this step again when you are ready and choose Yes in the Windows prompt.", "wsl.enable", "Inferred"),
        new("win.restart-pending", "Windows still needs a restart.", "Save your work and restart Windows, then reopen CFD Workbench.", "os.restart", "Inferred"),
        new("win.policy-blocked", "Your organisation's settings block WSL on this PC.", "Choose SU2 under Other options, or ask your IT team to allow WSL.", null, "Flagged"),
        new("win.store-blocked", "Windows could not download WSL.", "Check your internet connection and try step 1 again.", "wsl.enable", "Flagged"),
        new("net.offline", "No internet connection.", "Reconnect to the internet and try this step again.", null, "Inferred"),
        new("net.proxy-tls", "A network filter blocked the download.", "Try another network, or copy a report to share with your IT team.", null, "Flagged"),
        new("download.hash-mismatch", "The download did not match the tested file and was deleted.", "Try downloading once more; if it repeats, copy a report.", null, "Verified"),
        new("disk.low", "Not enough free disk space on the target drive.", "Free up disk space, then click Check again.", null, "Inferred"),
        new("route.parent-missing", "The setup folder could not be created or written to.", "Check folder permissions and disk space, then try this step again.", null, "Verified"),
        new("wsl.import-failed", "Windows could not create the private Linux.", "Try this step again, or copy a report.", "wsl.base.import", "Flagged"),
        new("apt.unreachable", "The private Linux could not reach OpenFOAM's package server.", "Check your internet connection and try again.", null, "Inferred"),
        new("apt.version-gone", "The tested OpenFOAM build is no longer offered by its publisher.", "Copy a report so a new tested build can be pinned.", null, "Inferred"),
        new("av.quarantined", "Your security software removed SU2 after unpacking.", "Copy a report so we can help; do not turn off your security software.", null, "Flagged"),
        new("mac.intel", "OpenFOAM.app needs an Apple-silicon Mac.", "CAD and local analyses still work on this Mac.", null, "Verified"),
        new("mac.os-too-old", "macOS 14 or later is needed.", "Open System Settings, go to General, choose Software Update, and update macOS.", null, "Verified"),
        new("mac.blocked", "macOS blocked OpenFOAM because Apple has not notarised it.", "Open System Settings, go to Privacy and Security, choose Open Anyway next to OpenFOAM-v2512, then click Check again.", "ofapp.identify", "Flagged"),
        new("mac.volume-busy", "Another OpenFOAM window or disk image is already open.", "Close the other OpenFOAM window, then click Check again.", "ofapp.identify", "Flagged"),
        new("mac.quarantine-removal-refused", "CFD Workbench never removes Apple's quarantine flag.", "Use System Settings, Privacy and Security, Open Anyway, or install the tested build.", null, "Verified"),
        new("smoke.banner", "CFD Workbench could not switch off case code; this is an app fault.", "Copy a report so this app fault can be fixed.", null, "Verified"),
        new("smoke.build-mismatch", "This is a different OpenFOAM build than the tested one.", "Install the tested build.", null, "Verified"),
        new("smoke.scalar", "The test result did not match the tested build.", "Install the tested build, or copy a report.", null, "Verified"),
        new("smoke.no-outputs", "The test finished without writing its result files.", "Try the test again, or copy a report.", null, "Verified"),
        new("smoke.timeout", "The test run took longer than the 60 second limit.", "Try the test again.", null, "Inferred"),
        new("unknown", "Setup stopped with an error CFD Workbench does not recognise.", "Copy a report and share it with whoever helps you with this computer.", null, "Inferred")
    ];

    private static readonly Dictionary<string, CauseEntry> CauseMap =
        AllCauses.ToDictionary(c => c.Code, StringComparer.Ordinal);

    public static IReadOnlyList<FirmwareClickPath> FirmwareClickPaths { get; } =
    [
        new("lenovo", "Lenovo", "F1 (some models: F2)",
        [
            "Save your work, then restart.",
            "When the Lenovo logo appears, press F1 or F2 until firmware setup opens.",
            "Open Security, then Virtualization, then Intel Virtualization Technology or AMD SVM Mode, and choose Enabled.",
            "Press F10, then choose Yes to save and restart.",
            "Open CFD Workbench so setup checks again."
        ]),
        new("dell", "Dell", "F2",
        [
            "Save your work, then restart.",
            "When the Dell logo appears, press F2 repeatedly until firmware setup opens.",
            "Open Virtualization Support, choose Virtualization, and turn on Enable Intel Virtualization Technology or AMD SVM.",
            "Choose Apply, then Exit to restart.",
            "Open CFD Workbench so setup checks again."
        ]),
        new("hp", "HP", "F10",
        [
            "Save your work, then restart.",
            "When the HP logo appears, press F10 until firmware setup opens.",
            "Open Advanced or System Configuration, find Virtualization Technology (VTx or SVM), and choose Enabled.",
            "Press F10 to save changes and exit.",
            "Open CFD Workbench so setup checks again."
        ]),
        new("generic", "this PC", "F2 or Delete",
        [
            "Save your work, then restart.",
            "When your PC maker's logo appears, press F2 or Delete until firmware setup opens.",
            "Open Advanced or CPU settings, find Virtualization (Intel VT-x or AMD SVM), and choose Enabled.",
            "Save your changes and exit to restart.",
            "Open CFD Workbench so setup checks again."
        ])
    ];

    public static IReadOnlyList<RouteDefinition> AllRoutes { get; } = BuildRoutes();

    private static readonly Dictionary<string, RouteDefinition> RouteMap =
        AllRoutes.ToDictionary(r => r.Id, StringComparer.Ordinal);

    /// <summary>Pinned deterministic BLAKE3 hash of the canonical route catalogue (A8.5).</summary>
    public static string ExpectedManifestBlake3 { get; } =
        "43a11c60502b40f2d3becfee0ccd266376a6782d1871c13732c969ac00fc7ad7";

    /// <summary>Pinned deterministic SHA-256 hash of the canonical route catalogue (A8.5).</summary>
    public static string ExpectedManifestSha256 { get; } =
        "5ec444b6c34b9772f8eadd46e1e754cbc53c1f7050444bf92af432994f5b6f5b";

    public static RouteDefinition GetRoute(string routeId) =>
        RouteMap.TryGetValue(routeId, out var route)
            ? route
            : throw new ContractError("SETUP-ROUTE-UNKNOWN", $"Unknown solver route '{routeId}'.");

    public static bool TryGetRoute(string routeId, out RouteDefinition? route) =>
        RouteMap.TryGetValue(routeId, out route);

    public static CauseEntry GetCause(string? code) =>
        code is not null && CauseMap.TryGetValue(code, out var entry)
            ? entry
            : CauseMap["unknown"];

    public static FirmwareClickPath GetFirmwareClickPath(string? deviceMaker)
    {
        if (!string.IsNullOrWhiteSpace(deviceMaker))
        {
            foreach (var entry in FirmwareClickPaths)
            {
                if (entry.MakerKey != "generic" &&
                    deviceMaker.Contains(entry.MakerKey, StringComparison.OrdinalIgnoreCase))
                    return entry;
            }
        }
        return FirmwareClickPaths[^1];
    }

    public static (string Blake3, string Sha256) ComputeManifestHashes(IReadOnlyList<RouteDefinition>? routes = null)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(SerializeCanonical(routes ?? AllRoutes));
        return (Identity.Blake3(bytes), Identity.Sha256(bytes));
    }

    /// <summary>Verifies the route catalogue against its pinned build-manifest digest (A8.5).</summary>
    public static void VerifyManifest(IReadOnlyList<RouteDefinition>? routes = null)
    {
        var actual = ComputeManifestHashes(routes);
        if (!string.Equals(actual.Blake3, ExpectedManifestBlake3, StringComparison.Ordinal) ||
            !string.Equals(actual.Sha256, ExpectedManifestSha256, StringComparison.Ordinal))
        {
            throw new ContractError(
                "SETUP-MANIFEST-HASH",
                $"Solver route catalogue manifest mismatch (actual blake3={actual.Blake3}, sha256={actual.Sha256}).");
        }
    }

    internal static string SerializeCanonical(IReadOnlyList<RouteDefinition> routes)
    {
        var sb = new StringBuilder();
        foreach (var r in routes.OrderBy(x => x.Id, StringComparer.Ordinal))
        {
            sb.Append("ROUTE|").Append(r.Id).Append('|').Append(r.Version).Append('|')
              .Append(r.Os).Append('|').Append(r.MinOsBuild.ToString(CultureInfo.InvariantCulture)).Append('|')
              .Append(r.BackendName).Append('|').Append(r.BackendVersion).Append('|')
              .Append(r.RequiredDiskBytes.ToString(CultureInfo.InvariantCulture)).Append('|')
              .Append(r.ExpectedBuildId ?? "").Append('|')
              .Append(r.SmokeScalarName ?? "").Append('|')
              .Append(r.SmokeScalarReference?.ToString("R", CultureInfo.InvariantCulture) ?? "").Append('|')
              .Append(r.SmokeRelativeTolerance?.ToString("R", CultureInfo.InvariantCulture) ?? "").Append('\n');
            foreach (var p in r.Pins)
            {
                sb.Append("PIN|").Append(p.Id).Append('|').Append(p.FileName).Append('|')
                  .Append(p.Url).Append('|').Append(p.SizeBytes.ToString(CultureInfo.InvariantCulture))
                  .Append('|').Append(p.Sha256).Append('\n');
            }
            foreach (var s in r.Steps)
            {
                sb.Append("STEP|").Append(s.Id).Append('|').Append(s.Kind).Append('|')
                  .Append(s.NeedsAdmin ? "1" : "0").Append('|').Append(s.NeedsRestart ? "1" : "0").Append('|')
                  .Append(s.DoneCheckId).Append('\n');
            }
        }
        return sb.ToString();
    }

    private static IReadOnlyList<RouteDefinition> BuildRoutes()
    {
        var macRoute = new RouteDefinition(
            Id: MacOpenFoamId,
            Version: "2512.1.0",
            DisplayName: "OpenFOAM v2512 on this Mac",
            Reason: "The OpenFOAM you know, built for Apple silicon. About 5 minutes. No password, no restart.",
            Os: SetupOs.MacOs,
            SupportedArches: [SetupArch.Arm64],
            MinOsBuild: 14,
            BackendName: "OpenFOAM",
            BackendVersion: "v2512",
            RequiredDiskBytes: 1_610_612_736L,
            ExpectedBuildId: "_87ed40d256-20251219",
            SmokeScalarName: "courant_mean",
            SmokeScalarReference: 0.222158,
            SmokeRelativeTolerance: 1e-3,
            Pins:
            [
                new("openfoam-app-zip", "openfoam2512-app-arm64.zip",
                    "https://github.com/gerlero/openfoam-app/releases/download/v2.1.3/openfoam2512-app-arm64.zip",
                    253_440_212L, "22ffc888ba49fc6ab3c2207ad33e8044ebc0783edba638f31ecdefd434093d00",
                    "GPL-3.0", Gpl3SummaryText),
                new("openfoam-app-dmg", "OpenFOAM-v2512.dmg",
                    "OpenFOAM-v2512.app/Contents/Resources/OpenFOAM-v2512.dmg",
                    1_100_175_546L, "5eb2ab10af3e90b7e284276b7046632b9c9bc14885c67d6664bd2628eb393439"),
                new("etc-openfoam", "etc/openfoam",
                    "OpenFOAM-v2512.app/Contents/Resources/etc/openfoam",
                    0L, "fa900c4e2cf4c57b0e7c0df4fa167a607435e83aa1675101cf0bffc87502572a"),
                new("volume", "volume",
                    "OpenFOAM-v2512.app/Contents/Resources/volume",
                    0L, "a1d4e57b2931a32501a39cff6fc7ab509f4306c12a8c5f6dd09f7f051196da28")
            ],
            Steps:
            [
                new("survey.mac", StepKind.Survey, "Look at this Mac",
                    "Checks chip type, macOS version, free space, and any OpenFOAM already installed.",
                    "Reads basic machine information without changing anything.",
                    "a few seconds", "none", false, false, false, null, "survey.recorded",
                    "sw_vers; sysctl hw.optional.arm64; statfs", null, ["mac.intel", "mac.os-too-old", "disk.low"]),
                new("ofapp.check-existing", StepKind.Verify, "Check for OpenFOAM already here",
                    "Reuses a tested OpenFOAM installation if you already have one.",
                    "Checks Applications for OpenFOAM-v2512.app and verifies its pinned files.",
                    "a few seconds", "none", false, false, false, null, "ofapp.existing-checked",
                    "sha256 OpenFOAM-v2512.dmg (pin 5eb2ab10af3e90b7...393439) + etc/openfoam + volume", null, ["unknown"]),
                new("ofapp.download", StepKind.Download, "Download OpenFOAM.app",
                    "Fetches the tested OpenFOAM v2512 release for Apple silicon.",
                    "Downloads OpenFOAM.app from its author's GitHub release and checks its SHA-256 hash.",
                    "about 2 min", "253 MB", false, false, true, "cache.clear", "ofapp.zip-verified",
                    "GET https://github.com/gerlero/openfoam-app/releases/download/v2.1.3/openfoam2512-app-arm64.zip (sha256 22ffc888ba49fc6ab3c2207ad33e8044ebc0783edba638f31ecdefd434093d00)",
                    "GPL-3.0", ["net.offline", "net.proxy-tls", "download.hash-mismatch", "disk.low"]),
                new("ofapp.place", StepKind.Place, "Put it in Applications",
                    "Places OpenFOAM-v2512.app where CFD Workbench can start it cleanly.",
                    "Unpacks OpenFOAM-v2512.app into Applications and verifies all three pinned files.",
                    "about 30 s", "none", false, false, true, "ofapp.remove", "ofapp.placed-verified",
                    "unzip openfoam2512-app-arm64.zip -> Applications/OpenFOAM-v2512.app (quarantine flag untouched)",
                    null, ["disk.low", "route.parent-missing", "download.hash-mismatch"]),
                new("ofapp.identify", StepKind.Run, "Start OpenFOAM once",
                    "Confirms OpenFOAM starts and reports the tested build identifier.",
                    "Starts OpenFOAM through CFD Workbench's clean launcher to read its build banner.",
                    "a few seconds", "none", false, false, false, null, "ofapp.identified",
                    "/bin/zsh -f -e .../OpenFOAM-v2512.app/Contents/Resources/etc/openfoam -- foamVersion",
                    null, ["mac.blocked", "mac.volume-busy", "smoke.build-mismatch", "smoke.banner"]),
                new("smoke.openfoam", StepKind.Smoke, "Test run",
                    "Proves OpenFOAM solves accurately on this Mac with case code switched off.",
                    "Runs the bundled cavity test case in a fresh app-owned folder and checks outputs, banner, and Courant number.",
                    "about 4 s", "none", false, false, false, null, "smoke.openfoam-passed",
                    "/bin/zsh -f -e .../etc/openfoam -- blockMesh && icoFoam (allowSystemOperations 0, Disallowing banner, Courant mean 0.222158)",
                    null, ["smoke.banner", "smoke.build-mismatch", "smoke.scalar", "smoke.no-outputs", "smoke.timeout", "disk.low"])
            ],
            UndoStepIds: ["ofapp.remove", "cache.clear"],
            Causes: AllCauses);

        var winWslRoute = new RouteDefinition(
            Id: WinWslOpenFoamId,
            Version: "2512.1.0",
            DisplayName: "OpenFOAM v2512 on this PC (in WSL)",
            Reason: "The same OpenFOAM you already use, kept separate from anything else on your PC. Windows asks for permission once and needs one restart.",
            Os: SetupOs.Windows,
            SupportedArches: [SetupArch.X64, SetupArch.Arm64],
            MinOsBuild: 19041,
            BackendName: "OpenFOAM",
            BackendVersion: "v2512",
            RequiredDiskBytes: 4_294_967_296L,
            ExpectedBuildId: "_bd2b6720-20260127",
            SmokeScalarName: "courant_mean",
            SmokeScalarReference: 0.222158,
            SmokeRelativeTolerance: 1e-3,
            Pins:
            [
                new("ubuntu-wsl-amd64", "ubuntu-24.04.5-wsl-amd64.wsl",
                    "https://releases.ubuntu.com/noble/ubuntu-24.04.5-wsl-amd64.wsl",
                    388_975_696L, "bb415d824822c4b878125729af451a5d18fb13d1cf5cbed9a7393ad64ac6039e"),
                new("openfoam2512-deb", "openfoam2512_2512.0-2_amd64.deb",
                    "https://dl.openfoam.com/repos/deb/dists/noble/main/binary-amd64/Packages",
                    68_264_976L, "c59e65ffd99c9143fd7c2594dc9776e9d7190124965efdb667e28677d93f3fed",
                    "GPL-3.0", Gpl3SummaryText),
                new("openfoam2512-gpg", "pubkey.gpg",
                    "https://dl.openfoam.com/pubkey.gpg",
                    0L, OpenFoamGpgFingerprint)
            ],
            Steps:
            [
                new("survey.win", StepKind.Survey, "Look at this PC",
                    "Checks Windows version, free disk space, virtualization, WSL state, and any existing OpenFOAM.",
                    "Reads basic machine information without changing anything.",
                    "a few seconds", "none", false, false, false, null, "survey.recorded",
                    "wsl.exe --status; wsl.exe --list --verbose; OS build and free disk check", null,
                    ["win.build-too-old", "win.virtualization-off", "disk.low"]),
                new("wsl.enable", StepKind.OsHandoff, "Turn on WSL, Windows' built-in Linux",
                    "OpenFOAM is Linux software. WSL is Microsoft's built-in way to run Linux programs on Windows.",
                    "Windows asks for your permission once, then turns on WSL.",
                    "about 3 min", "Windows component", true, true, false, null, "wsl.enabled",
                    "wsl.exe --install --no-distribution (via Windows administrator prompt)", null,
                    ["win.uac-declined", "win.virtualization-off", "win.wsl-feature-missing", "win.policy-blocked", "win.store-blocked"]),
                new("os.restart", StepKind.OsHandoff, "Restart Windows",
                    "Windows must restart to finish turning on WSL.",
                    "Save your work and restart. CFD Workbench opens again and continues from step 3.",
                    "about 2 min", "none", false, true, false, null, "os.restarted",
                    "Register per-user RunOnce resume entry; restart Windows; verify wsl.exe --status", null,
                    ["win.restart-pending"]),
                new("wsl.base.download", StepKind.Download, "Download Ubuntu for WSL",
                    "Ubuntu is the Linux system that OpenFOAM's publishers build packages for.",
                    "Downloads the official Ubuntu 24.04 image from Canonical and verifies its SHA-256 hash.",
                    "about 2 min", "389 MB", false, false, true, "cache.clear", "wsl.base-verified",
                    "GET https://releases.ubuntu.com/noble/ubuntu-24.04.5-wsl-amd64.wsl (sha256 bb415d824822c4b878125729af451a5d18fb13d1cf5cbed9a7393ad64ac6039e)",
                    null, ["net.offline", "net.proxy-tls", "download.hash-mismatch", "disk.low"]),
                new("wsl.base.import", StepKind.Place, "Create a private Linux for CFD Workbench",
                    "Keeps OpenFOAM separate from any other Linux or tools on your PC.",
                    "Creates the app-owned folder and imports Ubuntu into a private WSL 2 distribution.",
                    "about 1 min", "none", false, false, true, "wsl.unregister", "wsl.distro-imported",
                    "wsl.exe --import cfdw-openfoam2512 %LOCALAPPDATA%\\CFD Workbench\\wsl\\cfdw-openfoam2512 ubuntu-24.04.5-wsl-amd64.wsl --version 2",
                    null, ["route.parent-missing", "wsl.import-failed", "disk.low"]),
                new("of.repo", StepKind.Run, "Add OpenFOAM's package source",
                    "Points the private Linux to OpenFOAM's official signed package repository.",
                    "Writes the repository configuration and verified signing key, then refreshes the package index.",
                    "a few seconds", "none", false, false, false, null, "of.repo-ready",
                    "write /etc/apt/sources.list.d/openfoam.list (key DC93C096174122E256DA24063386DD74948D208F); wsl.exe -d cfdw-openfoam2512 -u root -e /usr/bin/apt-get update",
                    null, ["route.parent-missing", "apt.unreachable", "net.proxy-tls"]),
                new("of.install", StepKind.Run, "Install OpenFOAM",
                    "Installs the tested OpenFOAM v2512 runtime without extra compilers.",
                    "Downloads and installs the pinned OpenFOAM runtime packages and verifies the package hash.",
                    "about 3 min", "68 MB", false, false, true, "wsl.unregister", "of.installed",
                    "wsl.exe -d cfdw-openfoam2512 -u root -e /usr/bin/env DEBIAN_FRONTEND=noninteractive /usr/bin/apt-get install -y --no-install-recommends openfoam2512=2512.0-2 openfoam2512-common=2512.0-2",
                    "GPL-3.0", ["apt.unreachable", "apt.version-gone", "download.hash-mismatch", "disk.low"]),
                new("of.identify", StepKind.Run, "Start OpenFOAM once",
                    "Confirms OpenFOAM starts inside the private Linux and prints its build banner.",
                    "Starts OpenFOAM through CFD Workbench's clean launcher to read its build identifier.",
                    "a few seconds", "none", false, false, false, null, "of.identified",
                    "wsl.exe -d cfdw-openfoam2512 -e /usr/bin/openfoam2512 foamVersion", null,
                    ["smoke.banner", "smoke.build-mismatch"]),
                new("smoke.openfoam", StepKind.Smoke, "Test run",
                    "Proves OpenFOAM solves accurately on this PC with case code switched off.",
                    "Runs the bundled cavity test case inside a fresh app-owned folder and checks outputs, banner, and Courant number.",
                    "about 5 s", "none", false, false, false, null, "smoke.openfoam-passed",
                    "wsl.exe -d cfdw-openfoam2512 -e /usr/bin/openfoam2512 blockMesh && icoFoam (allowSystemOperations 0, Disallowing banner, Courant mean 0.222158)",
                    null, ["route.parent-missing", "smoke.banner", "smoke.build-mismatch", "smoke.scalar", "smoke.no-outputs", "smoke.timeout", "disk.low"])
            ],
            UndoStepIds: ["wsl.unregister", "cache.clear"],
            Causes: AllCauses);

        var winSu2Route = new RouteDefinition(
            Id: WinSu2Id,
            Version: "8.5.0.1",
            DisplayName: "SU2 v8.5.0 (runs directly on Windows)",
            Reason: "Installs in about 2 minutes with no administrator prompt and no restart. Does not support free surface or cavitation.",
            Os: SetupOs.Windows,
            SupportedArches: [SetupArch.X64, SetupArch.Arm64],
            MinOsBuild: 19041,
            BackendName: "SU2",
            BackendVersion: "v8.5.0",
            RequiredDiskBytes: 268_435_456L,
            ExpectedBuildId: "v8.5.0",
            SmokeScalarName: "cd_cylinder",
            SmokeScalarReference: 2.885552317,
            SmokeRelativeTolerance: 1e-2,
            Pins:
            [
                new("su2-win64-omp", "SU2-v8.5.0-win64-omp.zip",
                    "https://github.com/su2code/SU2/releases/download/v8.5.0/SU2-v8.5.0-win64-omp.zip",
                    28_043_921L, "4466fe21aedb5e0bad57afd45f829acbdec6ec79fe8c3f8954ddea06a4b4bc11",
                    "LGPL-2.1", Lgpl21SummaryText)
            ],
            Steps:
            [
                new("survey.win", StepKind.Survey, "Look at this PC",
                    "Checks Windows version and free disk space.",
                    "Reads basic machine information without changing anything.",
                    "a few seconds", "none", false, false, false, null, "survey.recorded",
                    "OS build and free disk check", null, ["win.build-too-old", "disk.low"]),
                new("su2.download", StepKind.Download, "Download SU2 v8.5.0",
                    "Downloads the tested native Windows SU2 package after you accept its licence.",
                    "Downloads SU2 from GitHub and verifies its SHA-256 hash and file size.",
                    "about 1 min", "28 MB", false, false, true, "cache.clear", "su2.zip-verified",
                    "GET https://github.com/su2code/SU2/releases/download/v8.5.0/SU2-v8.5.0-win64-omp.zip (sha256 4466fe21aedb5e0bad57afd45f829acbdec6ec79fe8c3f8954ddea06a4b4bc11)",
                    "LGPL-2.1", ["net.offline", "net.proxy-tls", "download.hash-mismatch", "disk.low"]),
                new("su2.place", StepKind.Place, "Unpack SU2 into CFD Workbench's folder",
                    "Places SU2 inside the app's own folder without administrator approval.",
                    "Unpacks SU2_CFD.exe and records its hash.",
                    "a few seconds", "none", false, false, true, "su2.remove", "su2.placed-verified",
                    "unzip SU2-v8.5.0-win64-omp.zip -> %LOCALAPPDATA%\\CFD Workbench\\su2-8.5.0", null,
                    ["route.parent-missing", "av.quarantined", "disk.low"]),
                new("smoke.su2", StepKind.Smoke, "Test run",
                    "Proves SU2 runs and produces valid aerodynamic output on this PC.",
                    "Runs the bundled small 2D incompressible test case and checks its output file and drag coefficient.",
                    "about 10 s", "none", false, false, false, null, "smoke.su2-passed",
                    "SU2_CFD.exe cylinder.cfg (2,048-cell incompressible smoke, reference CD 2.885552317)", null,
                    ["av.quarantined", "smoke.scalar", "smoke.no-outputs", "smoke.timeout", "disk.low"])
            ],
            UndoStepIds: ["su2.remove", "cache.clear"],
            Causes: AllCauses);

        return [macRoute, winWslRoute, winSu2Route];
    }
}
