namespace CfdWorkbench.Core;

// Nine discipline presets (docs/design/setup-seeding.md §4.1, Ruling 272 DR-SET-4),
// 8 -> 9 purpose mapping (including wakefoil -> surf with Flagged wake-speed note),
// GEN catalog fallback disclosure over CatalogData/catalog.tsv, and default Goal state builder.

/// <summary>One Flagged assumption on a preset or brief that must be acknowledged (or replaced with Stated) before Seed.</summary>
public sealed record FlaggedAssumption(string FieldPath, string Label, string Source);

/// <summary>Immutable definition of one of the nine A5.5 / KB 04 hydrofoil discipline presets.</summary>
public sealed record DisciplinePreset(
    string Id,
    string Name,
    string Version,
    double TargetAreaSquareMeters,
    double TargetAspectRatio,
    double MaxSpanMeters,
    double MaxChordMeters,
    double Taper,
    double RootThicknessRatio,
    double AnhedralDeg,
    double WashoutDeg,
    string PreferredProfileId,
    string PreferredProfileName,
    string GenFallbackProfileId,
    string? FallbackReason,
    string DefaultClassRulePresetId,
    string DefaultClassRuleVersion,
    double DefaultMastLengthCm,
    double DefaultFuselageLengthCm,
    double DefaultRearAreaCm2,
    double DefaultFoilMassKg,
    double DefaultHRefMeters,
    double DefaultDepthMinMeters,
    double DefaultDepthMaxMeters,
    double DefaultWindMinKnots,
    double DefaultWindMaxKnots,
    double TakeoffMinKnots,
    double TakeoffMaxKnots,
    double CruiseMinKnots,
    double CruiseMaxKnots,
    double TopMaxKnots,
    string SpeedSourceLabel,
    string? SpeedDisconfirmingNote,
    IReadOnlyList<MassItem> DefaultMasses,
    double DefaultFrontWingLoadN,
    string FrontWingLoadSource,
    IReadOnlyList<FlaggedAssumption> DefaultFlaggedAssumptions);

/// <summary>Result of resolving a Setup brief purpose string onto one of the 9 discipline presets (SET-04).</summary>
public sealed record PurposeResolution(
    string NormalizedPurpose,
    DisciplinePreset Preset,
    string ClassRulePresetId,
    string ClassRuleVersion,
    FlaggedAssumption? PurposeMappingFlag);

public static class DisciplinePresets
{
    public const string PresetVersion = "2026-11-19";
    public const double KnotsToMps = 0.514444;
    public const double StandardAtmospherePa = 101325.0;
    public const string RootLeadingEdgeDatum = "root LE";
    public const string IttcSource = "ITTC 7.5-02-01-03 Rev 03";
    public const string IttcTableHash = "fca86591cf575f50c0c17ca6de8dac6372d0c2baaa40f7711d184c2b6e154480";

    /// <summary>Exact 15 °C standard seawater row matching <c>IttcWater.csv</c> (Ruling 272, CLI-01).</summary>
    public static WaterRecord DefaultSaltWater15C { get; } = new(
        15.0,
        35.16504,
        1026.0210,
        1.1892e-6,
        1670.9,
        IttcSource,
        IttcTableHash);

    /// <summary>Exact 15 °C fresh water row matching <c>IttcWater.csv</c>.</summary>
    public static WaterRecord DefaultFreshWater15C { get; } = new(
        15.0,
        0.0,
        999.1026,
        1.1386e-6,
        1705.8,
        IttcSource,
        IttcTableHash);

    private const string UiucPendingReason = "Pending admission — terms requested from UIUC · GEN sections remain";
    private const string LinkCiteOnlyReason = "Cite only (LINK) — its coordinates may not be copied, so it cannot be edited here";

    public static IReadOnlyList<DisciplinePreset> All { get; } =
    [
        new(
            Id: "wingfoil-race",
            Name: "Wingfoil race",
            Version: PresetVersion,
            TargetAreaSquareMeters: 0.100,
            TargetAspectRatio: 12.0,
            MaxSpanMeters: 1.10,
            MaxChordMeters: 0.120,
            Taper: 0.24,
            RootThicknessRatio: 0.100,
            AnhedralDeg: -5.0,
            WashoutDeg: -1.0,
            PreferredProfileId: "e817",
            PreferredProfileName: "Eppler E817",
            GenFallbackProfileId: "naca-4412",
            FallbackReason: UiucPendingReason,
            DefaultClassRulePresetId: "gwa-race-2022",
            DefaultClassRuleVersion: "2022-02-01",
            DefaultMastLengthCm: 95.0,
            DefaultFuselageLengthCm: 75.0,
            DefaultRearAreaCm2: 180.0,
            DefaultFoilMassKg: 4.5,
            DefaultHRefMeters: 0.50,
            DefaultDepthMinMeters: 0.30,
            DefaultDepthMaxMeters: 0.50,
            DefaultWindMinKnots: 10.0,
            DefaultWindMaxKnots: 20.0,
            TakeoffMinKnots: 9.0,
            TakeoffMaxKnots: 11.0,
            CruiseMinKnots: 11.0,
            CruiseMaxKnots: 22.0,
            TopMaxKnots: 32.0,
            SpeedSourceLabel: "KB 04 §Goal state — no race GPS dataset",
            SpeedDisconfirmingNote: "Light-wind take-off is pump-assisted",
            DefaultMasses:
            [
                new("rider", "Rider", 90.0, "Stated", "Verified", "User input"),
                new("board", "Race board", 5.5, "Defaulted", "Flagged", "KB 04 §Goal state (GWA min 3 kg; practitioner 4–7 kg)"),
                new("foil", "Foil complete", 4.5, "Defaulted", "Inferred", "KB 04 §Goal state (front wing 2.19 kg; GWA min 3 kg)"),
                new("wing", "Hand wing", 2.4, "Defaulted", "Inferred", "KB 04 §Goal state (Duotone Unit 5.0 m² 2.18–2.34 kg)"),
                new("kit", "Wetsuit, harness, helmet, leashes", 3.0, "Defaulted", "Flagged", "KB 04 §Goal state")
            ],
            DefaultFrontWingLoadN: 1050.0,
            FrontWingLoadSource: "1.00–1.10 W trim share (no measured trim data)",
            DefaultFlaggedAssumptions:
            [
                new("goal.masses.board", "Flagged", "Board mass 5.5 kg & kit 3.0 kg (KB 04 §Goal state)"),
                new("goal.frontWingLoadN", "Flagged", "Front-wing trim share 1050 N (no measured trim data)"),
                new("goal.depthBand", "Flagged", "Depth band 0.3–0.5 m (mast 85–95 cm, board clearance)"),
                new("goal.speedBand", "Flagged", "KB 04 §Goal state — no public race GPS dataset")
            ]),
        new(
            Id: "wingfoil-freeride",
            Name: "Wingfoil freeride",
            Version: PresetVersion,
            TargetAreaSquareMeters: 0.120,
            TargetAspectRatio: 8.0,
            MaxSpanMeters: 1.00,
            MaxChordMeters: 0.170,
            Taper: 0.35,
            RootThicknessRatio: 0.110,
            AnhedralDeg: -3.0,
            WashoutDeg: -1.0,
            PreferredProfileId: "e874",
            PreferredProfileName: "Eppler E874",
            GenFallbackProfileId: "naca-4412",
            FallbackReason: UiucPendingReason,
            DefaultClassRulePresetId: "open-class",
            DefaultClassRuleVersion: "open",
            DefaultMastLengthCm: 85.0,
            DefaultFuselageLengthCm: 72.0,
            DefaultRearAreaCm2: 220.0,
            DefaultFoilMassKg: 4.5,
            DefaultHRefMeters: 0.45,
            DefaultDepthMinMeters: 0.30,
            DefaultDepthMaxMeters: 0.50,
            DefaultWindMinKnots: 12.0,
            DefaultWindMaxKnots: 22.0,
            TakeoffMinKnots: 8.0,
            TakeoffMaxKnots: 10.0,
            CruiseMinKnots: 10.0,
            CruiseMaxKnots: 18.0,
            TopMaxKnots: 22.0,
            SpeedSourceLabel: "KB 04 §Goal state — freeride speed band",
            SpeedDisconfirmingNote: null,
            DefaultMasses:
            [
                new("rider", "Rider", 90.0, "Stated", "Verified", "User input"),
                new("board", "Freeride board", 6.0, "Defaulted", "Flagged", "KB 04 §Goal state"),
                new("foil", "Foil complete", 4.5, "Defaulted", "Inferred", "KB 04 §Goal state"),
                new("wing", "Hand wing", 2.5, "Defaulted", "Inferred", "KB 04 §Goal state"),
                new("kit", "Wetsuit, harness, leashes", 1.5, "Defaulted", "Flagged", "KB 04 §Goal state")
            ],
            DefaultFrontWingLoadN: 1030.0,
            FrontWingLoadSource: "Freeride trim share (Flagged default)",
            DefaultFlaggedAssumptions:
            [
                new("goal.masses.board", "Flagged", "Board mass 6.0 kg & kit 1.5 kg (KB 04)"),
                new("goal.speedBand", "Flagged", "Freeride speed band 8–22 kn (KB 04)")
            ]),
        new(
            Id: "surf",
            Name: "Surf foil",
            Version: PresetVersion,
            TargetAreaSquareMeters: 0.105,
            TargetAspectRatio: 7.4,
            MaxSpanMeters: 0.90,
            MaxChordMeters: 0.175,
            Taper: 0.38,
            RootThicknessRatio: 0.115,
            AnhedralDeg: -2.0,
            WashoutDeg: -1.0,
            PreferredProfileId: "e874",
            PreferredProfileName: "Eppler E874",
            GenFallbackProfileId: "naca-4412",
            FallbackReason: UiucPendingReason,
            DefaultClassRulePresetId: "open-class",
            DefaultClassRuleVersion: "open",
            DefaultMastLengthCm: 80.0,
            DefaultFuselageLengthCm: 68.0,
            DefaultRearAreaCm2: 200.0,
            DefaultFoilMassKg: 4.0,
            DefaultHRefMeters: 0.40,
            DefaultDepthMinMeters: 0.25,
            DefaultDepthMaxMeters: 0.45,
            DefaultWindMinKnots: 0.0,
            DefaultWindMaxKnots: 15.0,
            TakeoffMinKnots: 6.0,
            TakeoffMaxKnots: 8.0,
            CruiseMinKnots: 8.0,
            CruiseMaxKnots: 14.0,
            TopMaxKnots: 18.0,
            SpeedSourceLabel: "KB 04 §Goal state — wave speed band",
            SpeedDisconfirmingNote: null,
            DefaultMasses:
            [
                new("rider", "Rider", 90.0, "Stated", "Verified", "User input"),
                new("board", "Prone / surf board", 4.5, "Defaulted", "Flagged", "KB 04 practitioner estimate"),
                new("foil", "Foil complete", 4.0, "Defaulted", "Flagged", "KB 04 practitioner estimate"),
                new("kit", "Wetsuit and leash", 2.0, "Defaulted", "Flagged", "KB 04 practitioner estimate")
            ],
            DefaultFrontWingLoadN: 990.0,
            FrontWingLoadSource: "Surf trim share (Flagged default)",
            DefaultFlaggedAssumptions:
            [
                new("goal.speedBand", "Flagged", "Wave speed band 6–18 kn (KB 04)"),
                new("goal.masses.board", "Flagged", "Board & foil mass 8.5 kg (practitioner estimate)")
            ]),
        new(
            Id: "pump",
            Name: "Pump / dock-start",
            Version: PresetVersion,
            TargetAreaSquareMeters: 0.155,
            TargetAspectRatio: 10.5,
            MaxSpanMeters: 1.30,
            MaxChordMeters: 0.175,
            Taper: 0.30,
            RootThicknessRatio: 0.110,
            AnhedralDeg: -2.0,
            WashoutDeg: -0.5,
            PreferredProfileId: "e904",
            PreferredProfileName: "Eppler E904",
            GenFallbackProfileId: "naca-4412",
            FallbackReason: UiucPendingReason,
            DefaultClassRulePresetId: "open-class",
            DefaultClassRuleVersion: "open",
            DefaultMastLengthCm: 80.0,
            DefaultFuselageLengthCm: 70.0,
            DefaultRearAreaCm2: 240.0,
            DefaultFoilMassKg: 4.5,
            DefaultHRefMeters: 0.40,
            DefaultDepthMinMeters: 0.25,
            DefaultDepthMaxMeters: 0.45,
            DefaultWindMinKnots: 0.0,
            DefaultWindMaxKnots: 10.0,
            TakeoffMinKnots: 5.0,
            TakeoffMaxKnots: 7.0,
            CruiseMinKnots: 7.0,
            CruiseMaxKnots: 11.0,
            TopMaxKnots: 14.0,
            SpeedSourceLabel: "KB 04 §Goal state — pump cadence & heave (St)",
            SpeedDisconfirmingNote: "Unsteady heave/pitch pumping not modelled in steady tier",
            DefaultMasses:
            [
                new("rider", "Rider", 90.0, "Stated", "Verified", "User input"),
                new("board", "Pump board", 3.0, "Defaulted", "Flagged", "KB 04 §Goal state"),
                new("foil", "Foil complete", 4.5, "Defaulted", "Inferred", "KB 04 §Goal state"),
                new("kit", "Wetsuit", 1.5, "Defaulted", "Flagged", "KB 04 §Goal state")
            ],
            DefaultFrontWingLoadN: 970.0,
            FrontWingLoadSource: "Steady mean load during pump glide (Flagged)",
            DefaultFlaggedAssumptions:
            [
                new("goal.speedBand", "Flagged", "Pump cadence 1–2 Hz & heave 0.2–0.4 m (KB 04)")
            ]),
        new(
            Id: "downwind",
            Name: "Downwind SUP",
            Version: PresetVersion,
            TargetAreaSquareMeters: 0.110,
            TargetAspectRatio: 11.8,
            MaxSpanMeters: 1.20,
            MaxChordMeters: 0.135,
            Taper: 0.28,
            RootThicknessRatio: 0.104,
            AnhedralDeg: -3.0,
            WashoutDeg: -1.0,
            PreferredProfileId: "e904",
            PreferredProfileName: "Eppler E904",
            GenFallbackProfileId: "naca-4412",
            FallbackReason: UiucPendingReason,
            DefaultClassRulePresetId: "open-class",
            DefaultClassRuleVersion: "open",
            DefaultMastLengthCm: 85.0,
            DefaultFuselageLengthCm: 72.0,
            DefaultRearAreaCm2: 190.0,
            DefaultFoilMassKg: 4.5,
            DefaultHRefMeters: 0.45,
            DefaultDepthMinMeters: 0.30,
            DefaultDepthMaxMeters: 0.50,
            DefaultWindMinKnots: 12.0,
            DefaultWindMaxKnots: 25.0,
            TakeoffMinKnots: 7.0,
            TakeoffMaxKnots: 9.0,
            CruiseMinKnots: 9.0,
            CruiseMaxKnots: 18.0,
            TopMaxKnots: 25.0,
            SpeedSourceLabel: "KB 04 §Goal state — open-ocean swell speed band",
            SpeedDisconfirmingNote: null,
            DefaultMasses:
            [
                new("rider", "Rider", 90.0, "Stated", "Verified", "User input"),
                new("board", "Downwind SUP board", 7.5, "Defaulted", "Flagged", "KB 04 §Goal state"),
                new("foil", "Foil complete", 4.5, "Defaulted", "Inferred", "KB 04 §Goal state"),
                new("kit", "Paddle, leash, hydration", 1.5, "Defaulted", "Flagged", "KB 04 §Goal state")
            ],
            DefaultFrontWingLoadN: 1020.0,
            FrontWingLoadSource: "Downwind trim share (Flagged)",
            DefaultFlaggedAssumptions:
            [
                new("goal.masses.board", "Flagged", "SUP board mass 7.5 kg & swell speeds 7–25 kn (KB 04)")
            ]),
        new(
            Id: "parawing",
            Name: "Parawing",
            Version: PresetVersion,
            TargetAreaSquareMeters: 0.100,
            TargetAspectRatio: 11.0,
            MaxSpanMeters: 1.10,
            MaxChordMeters: 0.130,
            Taper: 0.28,
            RootThicknessRatio: 0.104,
            AnhedralDeg: -3.0,
            WashoutDeg: -1.0,
            PreferredProfileId: "e904",
            PreferredProfileName: "Eppler E904",
            GenFallbackProfileId: "naca-4412",
            FallbackReason: UiucPendingReason,
            DefaultClassRulePresetId: "open-class",
            DefaultClassRuleVersion: "open",
            DefaultMastLengthCm: 85.0,
            DefaultFuselageLengthCm: 72.0,
            DefaultRearAreaCm2: 185.0,
            DefaultFoilMassKg: 4.5,
            DefaultHRefMeters: 0.45,
            DefaultDepthMinMeters: 0.30,
            DefaultDepthMaxMeters: 0.50,
            DefaultWindMinKnots: 12.0,
            DefaultWindMaxKnots: 30.0,
            TakeoffMinKnots: 8.0,
            TakeoffMaxKnots: 10.0,
            CruiseMinKnots: 10.0,
            CruiseMaxKnots: 20.0,
            TopMaxKnots: 26.0,
            SpeedSourceLabel: "KB 04 §Goal state — parawing speed band",
            SpeedDisconfirmingNote: null,
            DefaultMasses:
            [
                new("rider", "Rider", 90.0, "Stated", "Verified", "User input"),
                new("board", "Mid-length / DW board", 6.0, "Defaulted", "Flagged", "KB 04 §Goal state"),
                new("foil", "Foil complete", 4.5, "Defaulted", "Inferred", "KB 04 §Goal state"),
                new("wing", "Parawing", 0.5, "Defaulted", "Flagged", "KB 04 §Goal state"),
                new("kit", "Harness & safety", 1.0, "Defaulted", "Flagged", "KB 04 §Goal state")
            ],
            DefaultFrontWingLoadN: 1005.0,
            FrontWingLoadSource: "Parawing trim share (Flagged)",
            DefaultFlaggedAssumptions:
            [
                new("goal.masses.wing", "Flagged", "Parawing mass 0.5 kg & wind band 12–30 kn (KB 04)")
            ]),
        new(
            Id: "windfoil",
            Name: "Windfoil (iQFOiL)",
            Version: PresetVersion,
            TargetAreaSquareMeters: 0.090,
            TargetAspectRatio: 10.0,
            MaxSpanMeters: 0.98,
            MaxChordMeters: 0.130,
            Taper: 0.30,
            RootThicknessRatio: 0.095,
            AnhedralDeg: -2.0,
            WashoutDeg: -0.5,
            PreferredProfileId: "speer-h105",
            PreferredProfileName: "Speer H105",
            GenFallbackProfileId: "naca-0009",
            FallbackReason: LinkCiteOnlyReason,
            DefaultClassRulePresetId: "iqfoil-2025",
            DefaultClassRuleVersion: "2025",
            DefaultMastLengthCm: 95.0,
            DefaultFuselageLengthCm: 115.0,
            DefaultRearAreaCm2: 255.0,
            DefaultFoilMassKg: 5.0,
            DefaultHRefMeters: 0.50,
            DefaultDepthMinMeters: 0.30,
            DefaultDepthMaxMeters: 0.55,
            DefaultWindMinKnots: 8.0,
            DefaultWindMaxKnots: 30.0,
            TakeoffMinKnots: 10.0,
            TakeoffMaxKnots: 13.0,
            CruiseMinKnots: 13.0,
            CruiseMaxKnots: 26.0,
            TopMaxKnots: 35.0,
            SpeedSourceLabel: "KB 04 §Goal state — windfoil speed band",
            SpeedDisconfirmingNote: null,
            DefaultMasses:
            [
                new("rider", "Rider", 90.0, "Stated", "Verified", "User input"),
                new("board", "Windfoil board", 9.0, "Defaulted", "Flagged", "KB 04 §Goal state"),
                new("foil", "Foil complete", 5.0, "Defaulted", "Inferred", "iQFOiL equipment"),
                new("rig", "Sail, mast, boom", 5.5, "Defaulted", "Flagged", "KB 04 §Goal state"),
                new("kit", "Harness & wetsuit", 1.5, "Defaulted", "Flagged", "KB 04 §Goal state")
            ],
            DefaultFrontWingLoadN: 1090.0,
            FrontWingLoadSource: "Windfoil trim share (Flagged)",
            DefaultFlaggedAssumptions:
            [
                new("goal.classRule", "Flagged", "iQFOiL front span & section coordinates (unopened)"),
                new("goal.speedBand", "Flagged", "Windfoil speed band 10–35 kn (KB 04)")
            ]),
        new(
            Id: "kitefoil",
            Name: "Kitefoil race",
            Version: PresetVersion,
            TargetAreaSquareMeters: 0.062,
            TargetAspectRatio: 10.5,
            MaxSpanMeters: 0.85,
            MaxChordMeters: 0.105,
            Taper: 0.26,
            RootThicknessRatio: 0.092,
            AnhedralDeg: -4.0,
            WashoutDeg: -0.5,
            PreferredProfileId: "e818",
            PreferredProfileName: "Eppler E818",
            GenFallbackProfileId: "naca-0009",
            FallbackReason: UiucPendingReason,
            DefaultClassRulePresetId: "formula-kite-2024",
            DefaultClassRuleVersion: "2024-08-10",
            DefaultMastLengthCm: 105.0,
            DefaultFuselageLengthCm: 65.0,
            DefaultRearAreaCm2: 145.0,
            DefaultFoilMassKg: 3.5,
            DefaultHRefMeters: 0.55,
            DefaultDepthMinMeters: 0.35,
            DefaultDepthMaxMeters: 0.60,
            DefaultWindMinKnots: 8.0,
            DefaultWindMaxKnots: 30.0,
            TakeoffMinKnots: 8.0,
            TakeoffMaxKnots: 11.0,
            CruiseMinKnots: 11.0,
            CruiseMaxKnots: 28.0,
            TopMaxKnots: 38.0,
            SpeedSourceLabel: "KB 04 §Goal state — Formula Kite race speed band",
            SpeedDisconfirmingNote: null,
            DefaultMasses:
            [
                new("rider", "Rider", 90.0, "Stated", "Verified", "User input"),
                new("board", "Kitefoil race board", 3.5, "Defaulted", "Flagged", "KB 04 §Goal state"),
                new("foil", "Foil complete", 3.5, "Defaulted", "Inferred", "KB 04 §Goal state"),
                new("kite", "Foil kite & bar", 2.5, "Defaulted", "Flagged", "KB 04 §Goal state"),
                new("kit", "Harness, impact vest, helmet", 1.5, "Defaulted", "Flagged", "KB 04 §Goal state")
            ],
            DefaultFrontWingLoadN: 990.0,
            FrontWingLoadSource: "Kitefoil trim share (Flagged)",
            DefaultFlaggedAssumptions:
            [
                new("goal.classRule", "Flagged", "Formula Kite dimensional maxima (rules PDF unopened)"),
                new("goal.speedBand", "Flagged", "Race speed band 8–38 kn (KB 04)")
            ]),
        new(
            Id: "efoil",
            Name: "E-foil",
            Version: PresetVersion,
            TargetAreaSquareMeters: 0.140,
            TargetAspectRatio: 7.5,
            MaxSpanMeters: 1.08,
            MaxChordMeters: 0.215,
            Taper: 0.40,
            RootThicknessRatio: 0.120,
            AnhedralDeg: -1.5,
            WashoutDeg: -1.0,
            PreferredProfileId: "naca-4412",
            PreferredProfileName: "NACA 4412",
            GenFallbackProfileId: "naca-4412",
            FallbackReason: null,
            DefaultClassRulePresetId: "open-class",
            DefaultClassRuleVersion: "open",
            DefaultMastLengthCm: 80.0,
            DefaultFuselageLengthCm: 75.0,
            DefaultRearAreaCm2: 260.0,
            DefaultFoilMassKg: 7.0,
            DefaultHRefMeters: 0.45,
            DefaultDepthMinMeters: 0.30,
            DefaultDepthMaxMeters: 0.50,
            DefaultWindMinKnots: 0.0,
            DefaultWindMaxKnots: 15.0,
            TakeoffMinKnots: 7.0,
            TakeoffMaxKnots: 9.0,
            CruiseMinKnots: 9.0,
            CruiseMaxKnots: 17.0,
            TopMaxKnots: 22.0,
            SpeedSourceLabel: "KB 04 §Goal state — e-foil speed band",
            SpeedDisconfirmingNote: null,
            DefaultMasses:
            [
                new("rider", "Rider", 90.0, "Stated", "Verified", "User input"),
                new("board", "E-foil board", 12.0, "Defaulted", "Flagged", "Tertiary manufacturer spec"),
                new("battery", "Battery pack", 11.0, "Defaulted", "Flagged", "Tertiary manufacturer spec"),
                new("foil", "Mast, motor & foil", 7.0, "Defaulted", "Flagged", "Tertiary manufacturer spec"),
                new("kit", "Wetsuit & vest", 2.0, "Defaulted", "Flagged", "Practitioner estimate")
            ],
            DefaultFrontWingLoadN: 1200.0,
            FrontWingLoadSource: "E-foil trim share (Flagged)",
            DefaultFlaggedAssumptions:
            [
                new("goal.masses.battery", "Flagged", "E-foil wing geometry & battery mass 11 kg (tertiary spec)")
            ])
    ];

    private static readonly Dictionary<string, DisciplinePreset> ById =
        All.ToDictionary(p => p.Id, StringComparer.OrdinalIgnoreCase);

    public static DisciplinePreset? TryGet(string id) =>
        id is not null && ById.TryGetValue(id.Trim(), out var preset) ? preset : null;

    public static DisciplinePreset Get(string id) =>
        TryGet(id) ?? throw new ContractError("SETUP-PRESET", $"Unknown discipline preset '{id}'.");

    /// <summary>
    /// Maps all 8+ brief purpose names onto the 9 canonical discipline presets (SET-04).
    /// <c>wakefoil</c> maps to <c>surf</c> with a mandatory Flagged wake-speed disclosure;
    /// <c>windfoil-freeride</c> maps to <c>windfoil</c> with <c>open-class</c>.
    /// </summary>
    public static PurposeResolution ResolvePurpose(string purposeOrPresetId)
    {
        if (string.IsNullOrWhiteSpace(purposeOrPresetId))
            throw new ContractError("SETUP-PRESET", "Purpose or preset id is required.");
        string norm = purposeOrPresetId.Trim().ToLowerInvariant().Replace('_', '-').Replace(' ', '-');
        return norm switch
        {
            "wingfoil-race" => Plain("wingfoil-race", Get("wingfoil-race")),
            "wingfoil-freeride" => Plain("wingfoil-freeride", Get("wingfoil-freeride")),
            "surf" or "surffoil" or "surf-foil" or "wingfoil-surf" => Plain(norm, Get("surf")),
            "wakefoil" or "wake-foil" => new PurposeResolution(
                "wakefoil",
                Get("surf"),
                "open-class",
                "open",
                new FlaggedAssumption(
                    "brief.purpose.wakefoil",
                    "Flagged",
                    "Wakefoil uses the Surf preset with Flagged boat-wake speeds (no public wakefoil dataset).")),
            "pump" or "dock-start" => Plain("pump", Get("pump")),
            "downwind" or "downwind-sup" => Plain("downwind", Get("downwind")),
            "parawing" => Plain("parawing", Get("parawing")),
            "windfoil" or "windfoil-race" => Plain(norm, Get("windfoil")),
            "windfoil-freeride" => new PurposeResolution("windfoil-freeride", Get("windfoil"), "open-class", "open", null),
            "kitefoil" or "kitefoil-race" => Plain("kitefoil", Get("kitefoil")),
            "efoil" or "e-foil" => Plain("efoil", Get("efoil")),
            _ => throw new ContractError("SETUP-PRESET", $"Unknown setup purpose or preset '{purposeOrPresetId}'.")
        };
    }

    private static PurposeResolution Plain(string purpose, DisciplinePreset preset) =>
        new(purpose, preset, preset.DefaultClassRulePresetId, preset.DefaultClassRuleVersion, null);

    /// <summary>Builds the default <see cref="SoftTargetSet"/> for a preset with equal weights (1, 1, 1, 1).</summary>
    public static SoftTargetSet DefaultSoftTargets(DisciplinePreset preset, string provenance = "Defaulted", string? source = null)
    {
        string src = source ?? $"{preset.Name} preset ({preset.Version})";
        return new SoftTargetSet(
            new SoftTargetScalar(preset.MaxSpanMeters, 1.0, provenance, src),
            new SoftTargetScalar(preset.MaxChordMeters, 1.0, provenance, src),
            new SoftTargetScalar(preset.TargetAreaSquareMeters, 1.0, provenance, src),
            new SoftTargetScalar(preset.TargetAspectRatio, 1.0, provenance, src),
            new SoftTargetTaper(preset.Taper, provenance, src),
            "equal-default");
    }

    /// <summary>Lists all Flagged assumptions active for a resolved purpose (preset defaults + any wakefoil mapping note).</summary>
    public static IReadOnlyList<FlaggedAssumption> FlaggedFor(PurposeResolution resolution)
    {
        if (resolution.PurposeMappingFlag is null) return resolution.Preset.DefaultFlaggedAssumptions;
        return [resolution.PurposeMappingFlag, .. resolution.Preset.DefaultFlaggedAssumptions];
    }

    /// <summary>Builds the default operating points for a preset (7 points for wingfoil-race matching GOAL-02; 3 representative points for other presets).</summary>
    public static IReadOnlyList<GoalOperatingPoint> DefaultOperatingPoints(DisciplinePreset preset, double frontWingLoadN, double hRefMeters)
    {
        if (preset.Id == "wingfoil-race")
        {
            return
            [
                Op("op-1", "P1 · 10 kn take-off", 10.0, hRefMeters, frontWingLoadN, "takeoff-cl"),
                Op("op-2", "P2 · 14 kn upwind", 14.0, hRefMeters, frontWingLoadN, "cruise-ld"),
                Op("op-3", "P3 · 18 kn downwind", 18.0, hRefMeters, frontWingLoadN, "cruise-ld"),
                Op("op-4", "P4 · 15 kn post-take-off", 15.0, hRefMeters, frontWingLoadN, "cruise-ld"),
                Op("op-5", "P5 · 22 kn upwind", 22.0, hRefMeters, frontWingLoadN, "cruise-ld"),
                Op("op-6", "P6 · 28 kn downwind", 28.0, hRefMeters, frontWingLoadN, "cavitation-margin"),
                Op("op-7", "P7 · 32 kn top end", 32.0, hRefMeters, frontWingLoadN, "cavitation-margin")
            ];
        }
        double midTakeoff = 0.5 * (preset.TakeoffMinKnots + preset.TakeoffMaxKnots);
        double midCruise = 0.5 * (preset.CruiseMinKnots + preset.CruiseMaxKnots);
        return
        [
            Op("op-1", $"Take-off ({midTakeoff:0.#} kn)", midTakeoff, hRefMeters, frontWingLoadN, "takeoff-cl"),
            Op("op-2", $"Cruise ({midCruise:0.#} kn)", midCruise, hRefMeters, frontWingLoadN, "cruise-ld"),
            Op("op-3", $"Top end ({preset.TopMaxKnots:0.#} kn)", preset.TopMaxKnots, hRefMeters, frontWingLoadN, "cavitation-margin")
        ];
    }

    private static GoalOperatingPoint Op(string id, string label, double knots, double hRefMeters, double loadN, string criticalMetric) =>
        new(
            id,
            label,
            knots * KnotsToMps,
            hRefMeters,
            RootLeadingEdgeDatum,
            StandardAtmospherePa,
            loadN,
            "Front-wing load (Flagged trim share)",
            criticalMetric,
            [2, 4],
            "Flagged");

    /// <summary>Builds the default constraint set for a preset and class-rule selection.</summary>
    public static IReadOnlyList<GoalConstraintRow> DefaultConstraints(DisciplinePreset preset, string classRulePresetId, string classRuleVersion, double frontWingLoadN) =>
    [
        new("c-load", "required-load", "Support front-wing load", ">=", frontWingLoadN, null, null, "N", null, null, true, false, $"{preset.Id} preset"),
        new("c-cav", "cavitation-margin", "Cavitation margin σ / (−Cp_min)", ">=", 1.15, null, null, "ratio", null, null, true, false, "A5.4 default 15 % margin (practitioner assumption, not sourced)"),
        new("c-re", "polar-re-envelope", "Re inside polar envelope", "in", null, 200_000.0, 2_000_000.0, "Re", null, null, true, false, "A5.3 catalog polar envelope"),
        new("c-hc", "deep-water-floor", "Submergence ratio h/c", ">=", 5.0, null, null, "h/c", null, null, true, false, "CEHINAV 2026 [S16] deep-water asymptote"),
        new("c-class", "class-rule-ref", ClassRuleLabel(classRulePresetId, classRuleVersion), null, null, null, null, null, classRulePresetId, null, true, false, ClassRuleSource(classRulePresetId)),
        new("c-sanity", "sanity-bounds", "Catalogued product envelope (advisory)", null, null, null, null, null, null, "advisory", true, false, "A5.5 product table (2026-09-20)")
    ];

    private static string ClassRuleLabel(string id, string version) => id switch
    {
        "gwa-race-2022" => $"GWA race box rule ({version})",
        "iqfoil-2025" => $"iQFOiL one-design benchmark ({version})",
        "formula-kite-2024" => $"IKA Formula Kite tolerances ({version})",
        _ => "Open class"
    };

    private static string ClassRuleSource(string id) => id switch
    {
        "gwa-race-2022" => "GWA Wingfoil World Tour [S12]",
        "iqfoil-2025" => "iQFOiL Class Rules 2025 [S13]",
        "formula-kite-2024" => "IKA Formula Kite Class Rules [S14]",
        _ => "Open class"
    };

    /// <summary>Builds a fully hashed immutable <see cref="GoalStateVersion"/> from resolved preset + overrides.</summary>
    public static GoalStateVersion BuildGoalState(
        string goalStateId,
        string? parentGoalStateId,
        string? briefId,
        PurposeResolution resolution,
        SoftTargetSet softTargets,
        double? riderMassKg = null,
        WaterRecord? water = null,
        FieldProvenance? waterProvenance = null,
        double? hRefMeters = null,
        double? mastLengthCm = null,
        string? classRulePresetId = null,
        string? classRuleVersion = null)
    {
        var preset = resolution.Preset;
        string crId = classRulePresetId ?? resolution.ClassRulePresetId;
        string crVer = classRuleVersion ?? resolution.ClassRuleVersion;
        var masses = preset.DefaultMasses
            .Select(m => m.Id == "rider" && riderMassKg is { } rkg
                ? m with { MassKg = rkg, Provenance = "Stated", Confidence = "Verified" }
                : m)
            .ToArray();

        double defaultRider = preset.DefaultMasses.First(m => m.Id == "rider").MassKg;
        double loadN = riderMassKg is { } customRider && Math.Abs(customRider - defaultRider) > 1e-9
            ? Math.Round((masses.Sum(m => m.MassKg) * GoalStateVersion.StandardGravity) * (preset.DefaultFrontWingLoadN / (preset.DefaultMasses.Sum(m => m.MassKg) * GoalStateVersion.StandardGravity)), 2)
            : preset.DefaultFrontWingLoadN;

        var resolvedWater = water ?? DefaultSaltWater15C;
        var resolvedWaterProv = waterProvenance ?? new FieldProvenance(
            "Stated",
            "Verified",
            resolvedWater.SalinityGPerKg > 0
                ? $"User selection (Salt · {resolvedWater.TemperatureC:0.#} °C)"
                : $"User selection (Fresh · {resolvedWater.TemperatureC:0.#} °C)");

        double href = hRefMeters ?? preset.DefaultHRefMeters;
        double depthMin = Math.Min(preset.DefaultDepthMinMeters, href);
        double depthMax = Math.Max(preset.DefaultDepthMaxMeters, href);

        var eq = new EquipmentContextRecord(
            preset.DefaultRearAreaCm2,
            mastLengthCm ?? preset.DefaultMastLengthCm,
            preset.DefaultFuselageLengthCm,
            preset.DefaultFoilMassKg,
            "Defaulted",
            "Flagged",
            $"{preset.Name} preset defaults (used for class-rule checks; surfaces not designed in v1)");

        var ops = DefaultOperatingPoints(preset, loadN, href);
        var constraints = DefaultConstraints(preset, crId, crVer, loadN);

        var draft = new GoalStateVersion(
            goalStateId,
            parentGoalStateId,
            briefId,
            "",
            preset.Id,
            crId,
            crVer,
            masses,
            loadN,
            new FieldProvenance("Defaulted", "Flagged", preset.FrontWingLoadSource),
            resolvedWater,
            resolvedWaterProv,
            new GoalDepthBandRecord(depthMin, depthMax, "Defaulted", "Flagged", $"Mast {eq.MastLengthCm:0.#} cm, board clearance (KB 04)"),
            new GoalWindBandRecord(preset.DefaultWindMinKnots, preset.DefaultWindMaxKnots, "Stated", "Verified", "Setup brief"),
            new GoalSpeedBandRecord(
                preset.TakeoffMinKnots * KnotsToMps,
                preset.TakeoffMaxKnots * KnotsToMps,
                preset.CruiseMinKnots * KnotsToMps,
                preset.CruiseMaxKnots * KnotsToMps,
                preset.TopMaxKnots * KnotsToMps,
                "Defaulted",
                "Flagged",
                preset.SpeedSourceLabel,
                preset.SpeedDisconfirmingNote),
            eq,
            ops,
            constraints,
            softTargets);

        return draft with { ContentHash = SetupStore.GoalContentHash(draft) };
    }
}
