using CfdWorkbench.Core.SolverSetup;

namespace CfdWorkbench.Analysis.SolverSetup;

/// <summary>
/// T2 macOS setup adapters (docs/design/guided-solver-setup.md §3.1, DR-SETUP-3, M8).
/// Never runs Homebrew, never runs <c>xattr</c>, and never runs <c>spctl --master-disable</c>.
/// </summary>
public static class MacSetupAdapters
{
    /// <summary>
    /// Inspects an existing OpenFOAM-v2512.app bundle by hashing the inner DMG, <c>etc/openfoam</c>,
    /// and <c>volume</c> using explicit <c>FileShare.Read | FileShare.Delete</c>.
    /// </summary>
    public static ExistingInstallInfo? InspectExistingBundle(string appBundleRoot)
    {
        if (string.IsNullOrWhiteSpace(appBundleRoot) || !Directory.Exists(appBundleRoot))
            return null;

        string resources = Path.Combine(appBundleRoot, "Contents", "Resources");
        string dmgPath = Path.Combine(resources, "OpenFOAM-v2512.dmg");
        string etcPath = Path.Combine(resources, "etc", "openfoam");
        string volumePath = Path.Combine(resources, "volume");

        if (!File.Exists(dmgPath) || !File.Exists(etcPath) || !File.Exists(volumePath))
            return null;

        var hashes = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["openfoam-app-dmg"] = SolverLauncher.ComputeFileSha256(dmgPath),
            ["etc-openfoam"] = SolverLauncher.ComputeFileSha256(etcPath),
            ["volume"] = SolverLauncher.ComputeFileSha256(volumePath)
        };

        var route = RouteCatalogue.GetRoute(RouteCatalogue.MacOpenFoamId);
        var verification = SolverLauncher.VerifyInstallIdentity(route, hashes);
        return new ExistingInstallInfo(
            Backend: "OpenFOAM",
            Version: "v2512",
            InstallRoot: appBundleRoot,
            InstallIdentitySha256: verification.CombinedIdentitySha256,
            MatchesPin: verification.Verified,
            IsSupportedVersion: true);
    }

    /// <summary>M8 / DR-SETUP-3: removing com.apple.quarantine or disabling Gatekeeper is always a refused step.</summary>
    public static AdapterStepResult RefuseQuarantineRemovalStep(string stepId) =>
        new(false, "mac.quarantine-removal-refused",
            $"Step '{stepId}' refused: CFD Workbench never removes Apple's quarantine flag or disables Gatekeeper.");

    /// <summary>Runs macOS OpenFOAM identify through the M1-M7 product launcher.</summary>
    public static Task<SolverLaunchResult> RunIdentifyAsync(
        SolverLauncher launcher,
        string appBundleRoot,
        string runKey,
        bool allowUnverifiedBuild,
        CancellationToken cancellationToken)
    {
        string activation = Path.Combine(appBundleRoot, "Contents", "Resources", "etc", "openfoam");
        var route = RouteCatalogue.GetRoute(RouteCatalogue.MacOpenFoamId);
        var req = new SolverLaunchRequest(
            RunKey: runKey,
            Os: SetupOs.MacOs,
            ActivationPathOrDistro: activation,
            App: OpenFoamApp.FoamVersion,
            ExpectedBuildId: route.ExpectedBuildId,
            AllowUnverifiedBuild: allowUnverifiedBuild);
        return launcher.LaunchAsync(req, cancellationToken);
    }
}
