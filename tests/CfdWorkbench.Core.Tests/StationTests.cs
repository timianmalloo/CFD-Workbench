using System.Globalization;
using System.Text;
using CfdWorkbench.Core;
using static CfdWorkbench.Core.Tests.IdentityTests;

namespace CfdWorkbench.Core.Tests;

/// <summary>
/// Add station, Remove station and the inspection slice's Promote (design add-station.md, track S1; Ruling 252).
/// The fixtures are built by hand on <see cref="FoilSource.NewDefault"/>'s 10-point degree-5 basis, so every pair shares one spacing.
/// The section-equality oracle is <see cref="Placement.Sections"/> on the document before against the document after: existing code, not under test.
/// </summary>
internal static class StationTests
{
    private const double HalfSpan = 0.5;
    private const double Tolerance = 1e-12;
    private static string Id() => Guid.NewGuid().ToString("D");

    internal static void Run()
    {
        Check("Station_PromoteAt035_SectionEqualsBlend_1e12", () =>
        {
            byte[] doc = Camber();
            using var session = Open(doc);
            double eta = Pct("35");
            var outcome = session.AddStation(Id(), eta, "35 %", false);
            byte[] after = session.Snapshot().Source;
            Equal(3, Assignments(after).Length);
            Equal("Station 2", outcome.ProfileName);
            Equal("Station 2", Definition(after).Profiles[Definition(after).Assignments[1].Profile].Name);
            True(MaxDifference(doc, after, [eta], Xs(33)) <= Tolerance, "section at eta 0.35 differs from the blend");
            // Hand literals: the root is symmetric (camber 0), so the new station's camber is 0.35 times the tip's camber.
            var tip = Definition(doc).Profiles[1];
            var added = Definition(after).Profiles.Single(profile => profile.Name == "Station 2");
            double thickness = ThicknessAt(after, eta);
            double root = Placement.ProfileDifferenceMaximum(Definition(doc).Profiles[0].Upper, Definition(doc).Profiles[0].Lower);
            for (int i = 0; i < added.Upper.Points.Length; i++)
            {
                double camber = (added.Upper.Points[i][1] + added.Lower.Points[i][1]) / 2;
                double expected = 0.35 * (tip.Upper.Points[i][1] + tip.Lower.Points[i][1]) / 2;
                True(Math.Abs(camber - expected) <= 1e-16, $"camber {i}: {camber:G17} vs {expected:G17}");
                double width = added.Upper.Points[i][1] - added.Lower.Points[i][1];
                double rootWidth = Definition(doc).Profiles[0].Upper.Points[i][1] - Definition(doc).Profiles[0].Lower.Points[i][1];
                True(Math.Abs(width - rootWidth * thickness / root) <= 1e-15, $"thickness {i}: {width:G17}");
            }
        });
        Check("Station_PromoteLaterBracket_UsesRightNeighbours", () =>
        {
            byte[] doc = Camber3();
            using var session = Open(doc);
            double eta = Pct("80");
            var preview = session.PreviewPromote(eta, "80 %");
            Equal("R1", preview.Regime);
            True(preview.DeviationRelative <= Tolerance, "deviation");
            session.AddStation(Id(), eta, "80 %", false);
            byte[] after = session.Snapshot().Source;
            var definition = Definition(after);
            Equal(4, definition.Assignments.Length);
            var added = definition.Profiles[definition.Assignments[2].Profile];
            var tip = Definition(doc).Profiles[2];
            for (int i = 0; i < added.Upper.Points.Length; i++)
            {
                double camber = (added.Upper.Points[i][1] + added.Lower.Points[i][1]) / 2;
                double mid = 0.5 * CamberOf(tip, i);
                double expected = 0.5 * (mid + CamberOf(tip, i));
                True(Math.Abs(camber - expected) <= 1e-16, $"camber {i}: {camber:G17} vs {expected:G17}");
            }
        });
        Check("Station_PromoteR1_LoftUnchangedBothIntervals", () =>
        {
            foreach (var (doc, label) in new[] { (Camber(), "F-CAMBER"), (SamePeak(), "F-SAMEPEAK") })
            {
                using var session = Open(doc);
                double eta = Pct("35");
                var preview = session.PreviewPromote(eta, "35 %");
                Equal("R1", preview.Regime);
                True(preview.DeviationRelative <= Tolerance, label + " preview deviation " + preview.DeviationRelative.ToString("G17", CultureInfo.InvariantCulture));
                session.AddStation(Id(), eta, "35 %", false);
                byte[] after = session.Snapshot().Source;
                Equal(3, Assignments(after).Length);
                var etas = Enumerable.Range(0, 17).Select(i => 0.35 * i / 16).Concat(Enumerable.Range(1, 17).Select(i => 0.35 + 0.65 * i / 17)).ToArray();
                True(MaxDifference(doc, after, etas, Xs(65)) <= Tolerance, label + " loft changed");
            }
        });
        Check("Station_PromoteR0_CopyIsBitIdentical", () =>
        {
            byte[] doc = FoilSource.NewDefault();
            using var session = Open(doc);
            var preview = session.PreviewPromote(0.5, "50 %");
            Equal("R0", preview.Regime);
            Equal(0d, preview.DeviationRelative);
            Equal(0d, preview.DeviationMeters);
            session.AddStation(Id(), 0.5, "50 %", false);
            byte[] after = session.Snapshot().Source;
            var definition = Definition(after);
            Equal(2, definition.Profiles.Length);
            var root = definition.Profiles.Single(profile => profile.Name == "naca-0012");
            var copy = definition.Profiles.Single(profile => profile.Name == "Station 2");
            string text = Encoding.UTF8.GetString(after);
            string rootBlock = text[root.BlockStart..root.BlockEnd];
            string copyBlock = text[copy.BlockStart..copy.BlockEnd];
            Equal(rootBlock, copyBlock.Replace("\"Station 2\"", "\"naca-0012\"", StringComparison.Ordinal));
            Equal(0d, MaxDifference(doc, after, Enumerable.Range(0, 41).Select(i => i / 40d).ToArray(), Xs(65)));
        });
        Check("Station_PromoteR2_ReportsMeasuredDeviation", () =>
        {
            foreach (var (doc, label) in new[] { (Peak(), "F-PEAK"), (Real(), "F-REAL") })
            {
                using var session = Open(doc);
                var preview = session.PreviewPromote(0.5, "50 %");
                Equal("R2", preview.Regime);
                True(preview.DeviationRelative > Tolerance, label + " deviation " + preview.DeviationRelative.ToString("G17", CultureInfo.InvariantCulture));
                True(preview.AtEta > 0 && preview.AtEta < 1, "AtEta recorded");
                True(preview.Grid.Contains("sampled", StringComparison.Ordinal) && preview.Grid.Contains("129", StringComparison.Ordinal) && preview.Grid.Contains("2001", StringComparison.Ordinal), "grid named");
                Console.WriteLine($"INFO T4 {label} deviation {preview.DeviationRelative:E3} of chord, {preview.DeviationMeters * 1e6:F4} um at eta {preview.AtEta:F4}, admitted {preview.Admitted}, k {K(doc, 0.5):F9}");
                byte[] before = session.Snapshot().Source;
                Refuses("STATION-INEXACT", () => session.AddStation(Id(), 0.5, "50 %", false));
                Equal(true, before.AsSpan().SequenceEqual(session.Snapshot().Source));
                if (preview.DeviationMeters <= 10e-6)
                {
                    Equal(true, preview.Admitted);
                    session.AddStation(Id(), 0.5, "50 %", true);
                    Equal(3, Assignments(session.Snapshot().Source).Length);
                }
                else
                {
                    Equal(false, preview.Admitted);
                    Refuses("STATION-INEXACT", () => session.AddStation(Id(), 0.5, "50 %", true));
                    Equal(true, before.AsSpan().SequenceEqual(session.Snapshot().Source));
                }
            }
            Console.WriteLine($"INFO T4 F-SAMEPEAK k {K(SamePeak(), 0.35):F12}");
        });
        Check("Station_CoincidentWithinJoinTolerance_RefusedExistingSelected", () =>
        {
            using var session = Open(FoilSource.NewDefault());
            session.AddStation(Id(), 0.5, "50 %", false);
            byte[] before = session.Snapshot().Source;
            // Station 2 now sits at 0.25 m. Compare resolved span positions in metres, not eta.
            var equal = session.PreviewPromote(0.5, "50 %");
            Equal("STATION-COINCIDENT", equal.RefusalCode);
            Equal(1, equal.ExistingIndex);
            Refuses("STATION-COINCIDENT", () => session.AddStation(Id(), 0.5, "50 %", false));
            double inside = Metres("250.0099");
            var near = session.PreviewPromote(inside, "250.0099 mm");
            Equal("STATION-COINCIDENT", near.RefusalCode);
            Equal(1, near.ExistingIndex);
            Refuses("STATION-COINCIDENT", () => session.AddStation(Id(), inside, "250.0099 mm", false));
            Equal(true, before.AsSpan().SequenceEqual(session.Snapshot().Source));
            double outside = Metres("250.0101");
            var far = session.PreviewPromote(outside, "250.0101 mm");
            Equal(null, far.RefusalCode);
            Equal("R0", far.Regime);
            session.AddStation(Id(), outside, "250.0101 mm", false);
            Equal(4, Assignments(session.Snapshot().Source).Length);
        });
        Check("Station_EtaAtRootOrTip_Refused", () =>
        {
            using var session = Open(FoilSource.NewDefault());
            Refuses("STATION-COINCIDENT", () => session.AddStation(Id(), 0, "0 %", false));
            Refuses("STATION-COINCIDENT", () => session.AddStation(Id(), 1, "100 %", false));
            Refuses("DSL-RANGE", () => session.AddStation(Id(), -0.1, "-10 %", false));
            Refuses("DSL-RANGE", () => session.AddStation(Id(), 1.2, "120 %", false));
            Refuses("DSL-RANGE", () => session.AddStation(Id(), double.NaN, "NaN", false));
            Equal(2, Assignments(session.Snapshot().Source).Length);
        });
        Check("Station_RemoveRootOrTip_Refused", () =>
        {
            byte[] doc = Camber3();
            using var session = Open(doc);
            Refuses("STATION-ENDPOINT", () => session.RemoveStation(Id(), 0));
            Refuses("STATION-ENDPOINT", () => session.RemoveStation(Id(), 2));
            Refuses("DSL-TARGET", () => session.RemoveStation(Id(), 3));
            Equal(true, doc.AsSpan().SequenceEqual(session.Snapshot().Source));
        });
        Check("Station_RemoveAboveAcceptance_ReportsAndRefuses", () =>
        {
            byte[] doc = Three();
            using var session = Open(doc);
            var preview = session.PreviewRemove(1);
            Equal(false, preview.Admitted);
            True(preview.DeviationMeters > 10e-6, "deviation " + preview.DeviationMeters.ToString("G17", CultureInfo.InvariantCulture));
            Equal(10e-6, preview.LimitMeters);
            Refuses("STATION-REMOVE-ACCEPTANCE", () => session.RemoveStation(Id(), 1));
            Equal(true, doc.AsSpan().SequenceEqual(session.Snapshot().Source));
        });
        Check("Station_AddThenRemove_RestoresBytes", () =>
        {
            byte[] doc = Camber();
            using var session = Open(doc);
            session.AddStation(Id(), Pct("35"), "35 %", false);
            var preview = session.PreviewRemove(1);
            Equal(true, preview.Admitted);
            Equal(true, preview.ProfileRemoved);
            True(preview.DeviationRelative <= Tolerance, "remove deviation");
            session.RemoveStation(Id(), 1);
            Equal(true, doc.AsSpan().SequenceEqual(session.Snapshot().Source));
        });
        Check("Station_UndoAddAndRemove_OneStepEach", () =>
        {
            byte[] doc = Camber();
            using var session = Open(doc);
            int depth = session.Envelope().Accepted.Length;
            session.AddStation(Id(), Pct("35"), "35 %", false);
            byte[] added = session.Snapshot().Source;
            Equal(depth + 1, session.Envelope().Accepted.Length);
            session.Undo(Id());
            Equal(true, doc.AsSpan().SequenceEqual(session.Snapshot().Source));
            session.Redo(Id());
            Equal(true, added.AsSpan().SequenceEqual(session.Snapshot().Source));
            session.RemoveStation(Id(), 1);
            Equal(true, doc.AsSpan().SequenceEqual(session.Snapshot().Source));
            session.Undo(Id());
            Equal(true, added.AsSpan().SequenceEqual(session.Snapshot().Source));
        });
        Check("Station_SaveReopen_StationPresent", () =>
        {
            using var session = Open(Camber());
            double eta = Pct("35");
            session.AddStation(Id(), eta, "35 %", false);
            byte[] image = session.SaveImage();
            using var reopened = new AuthoringSession();
            reopened.Reopen(image);
            var definition = Definition(reopened.Snapshot().Source);
            Equal(3, definition.Assignments.Length);
            Equal(BitConverter.DoubleToInt64Bits(eta), BitConverter.DoubleToInt64Bits(definition.Assignments[1].Eta));
            Equal("Station 2", definition.Profiles[definition.Assignments[1].Profile].Name);
        });
        Check("Station_InspectProjection_ShowsStation", () =>
        {
            using var session = Open(Camber());
            double eta = Pct("35");
            session.AddStation(Id(), eta, "35 %", false);
            var assignment = session.InspectAccepted().Authored.Assignments.ToArray()[1];
            Equal(eta, assignment.Eta);
            Equal(eta * HalfSpan, assignment.SpanMeters);
            Equal("Station 2", assignment.ProfileName);
        });
        Check("Station_SeventhDifferingSection_RefusedBudget", () =>
        {
            byte[] six = Six();
            Equal(GeometryStatus.Certified, Geometry.Assess(FoilSource.Parse(six)).Status);
            Equal(Geometry.OperationBoundCode, Geometry.Assess(FoilSource.Parse(Seven())).Code);
            using var session = Open(six);
            var preview = session.PreviewPromote(Pct("70"), "70 %");
            Equal("STATION-BUDGET", preview.RefusalCode);
            Equal(false, preview.Admitted);
            True(preview.Reason!.Contains("7 stations", StringComparison.Ordinal), preview.Reason);
            Refuses("STATION-BUDGET", () => session.AddStation(Id(), Pct("70"), "70 %", false));
            Equal(true, six.AsSpan().SequenceEqual(session.Snapshot().Source));
            // Control: four differing stations certify.
            using var control = Open(Camber3());
            Equal(null, control.PreviewPromote(Pct("80"), "80 %").RefusalCode);
            control.AddStation(Id(), Pct("80"), "80 %", false);
        });
        Check("Station_MixedSpacing_PromoteRefused", () =>
        {
            // A document with a different spacing between neighbours is Unsupported and cannot be admitted, so the
            // spacing check is reached on the parsed bytes with no session (the assessment must not be called).
            var plan = StationEdits.Promote(Spacing(), 0.5, "50 %", _ => throw new InvalidOperationException("assessed"));
            Equal("STATION-SPACING", plan.Preview.RefusalCode);
            Equal(false, plan.Preview.Admitted);
        });
        Check("Station_Promote_ThicknessKeepCurrent_ChannelsByteIdentical", () =>
        {
            byte[] doc = Camber();
            using var session = Open(doc);
            var outcome = session.AddStation(Id(), Pct("35"), "35 %", false);
            Equal(ThicknessIntent.KeepCurrent, outcome.Thickness);
            byte[] after = session.Snapshot().Source;
            Equal(3, Assignments(after).Length);
            foreach (string channel in new[] { "leading", "trailing", "dihedral", "twist", "thickness" })
                Equal(Channel(doc, channel), Channel(after, channel));
            session.RemoveStation(Id(), 1);
            Equal(2, Assignments(session.Snapshot().Source).Length);
            foreach (string channel in new[] { "leading", "trailing", "dihedral", "twist", "thickness" })
                Equal(Channel(doc, channel), Channel(session.Snapshot().Source, channel));
        });
        Check("Station_ProfileClosure_ReferencedOnceAndClosedRule", () =>
        {
            foreach (var (closureLeft, closureRight, expected, mutantName) in new[] { ("closed", "closed", "closed", "closed+closed"), ("closed", "open", "open", "closed+open") })
            {
                byte[] doc = Doc([("root", Prof("a", Root, Lower(Root), closureLeft)), ("tip", Prof("b", Add(Root, Bend(0.04)), Add(Lower(Root), Bend(0.04)), closureRight))]);
                using var session = Open(doc);
                session.AddStation(Id(), Pct("35"), "35 %", false);
                var definition = Definition(session.Snapshot().Source);
                var added = definition.Profiles.Single(profile => profile.Name == "Station 2");
                Equal(expected, added.Closure);
                Equal(1, definition.Assignments.Count(item => definition.Profiles[item.Profile].Name == "Station 2"));
                True(mutantName.Length > 0, mutantName);
            }
            using (var session = Open(Camber()))
            {
                session.AddStation(Id(), Pct("35"), "35 %", false);
                session.RemoveStation(Id(), 1);
                Equal(false, Definition(session.Snapshot().Source).Profiles.Any(profile => profile.Name == "Station 2"));
            }
            // A shared profile survives the removal of one of its stations.
            using var shared = Open(FoilSource.NewDefault());
            shared.AddStation(Id(), 0.5, "50 %", false);
            shared.AddStation(Id(), 0.75, "75 %", false);
            byte[] both = shared.Snapshot().Source;
            shared.RemoveStation(Id(), 2);
            var kept = Definition(shared.Snapshot().Source);
            Equal(3, kept.Assignments.Length);
            Equal(2, kept.Profiles.Length);
            Equal(false, Definition(both).Profiles.Length == kept.Profiles.Length);
        });
        Check("Station_EditPromotedProfile_NeighbourBlendUpdates", () =>
        {
            using var session = Open(Camber());
            double eta = Pct("35");
            session.AddStation(Id(), eta, "35 %", false);
            byte[] added = session.Snapshot().Source;
            Equal(3, Assignments(added).Length);
            string draftId = Id();
            var view = session.BeginSectionDraft(draftId, 1);
            var profile = session.ProfileAt(1);
            var point = profile.Upper[4];
            view = session.ApplySectionStep(draftId, view.Generation, new SectionStep.Move(SurfaceSide.Upper, point.Id, point.X, point.Y + 0.004));
            byte[] edited = view.Bytes;
            True(MaxDifference(added, edited, [0.2, 0.6], Xs(65)) > 1e-4, "interior sections did not change");
            True(MaxDifference(added, edited, [0.0, 1.0], Xs(65)) <= Tolerance, "root and tip changed");
        });
        Check("Station_DraftOpen_PromoteAndRemoveRefused", () =>
        {
            using var session = Open(Camber3());
            var curve = Definition(session.Snapshot().Source).Curves["leading"];
            session.BeginPointGesture(Id(), "leading", curve.Ids[2]);
            Refuses("DSL-DRAFT-OWNED", () => session.PreviewPromote(Pct("35"), "35 %"));
            Refuses("DSL-DRAFT-OWNED", () => session.AddStation(Id(), Pct("35"), "35 %", false));
            Refuses("DSL-DRAFT-OWNED", () => session.PreviewRemove(1));
            Refuses("DSL-DRAFT-OWNED", () => session.RemoveStation(Id(), 1));
        });
        Check("Station_AssignProfileToPromoted_PreviewReplace", () =>
        {
            using var session = Open(Camber());
            session.AddStation(Id(), Pct("35"), "35 %", false);
            string draftId = Id();
            var view = session.BeginSectionDraft(draftId, 1);
            var source = new ReplaceSource.Coordinates("NACA 0009", new Provenance("gen:naca-0009", false), CatalogGenerator.Naca4("0009"));
            var preview = session.PreviewReplace(draftId, view.Generation, source, ReplaceScope.Draft);
            Equal(null, preview.RefusalCode);
            Equal(true, preview.Stations.SequenceEqual([1]));
            var applied = session.ApplySectionStep(draftId, view.Generation, new SectionStep.Replace(source, ReplaceScope.Draft));
            var definition = Definition(applied.Bytes);
            Equal(3, definition.Assignments.Length);
            Equal("Station 2", definition.Profiles[definition.Assignments[1].Profile].Name);
        });
        Check("Station_PreviewPromote_RecordsDuration", () =>
        {
            using var session = Open(Camber());
            session.PreviewPromote(Pct("35"), "35 %");
            var outcome = session.AddStation(Id(), Pct("35"), "35 %", false);
            Equal("R1", outcome.Report.Regime);
            Equal(3, Assignments(session.Snapshot().Source).Length);
            var events = session.ReadLocalEvents();
            var preview = events.Last(item => item.Operation == "document.preview-promote");
            var add = events.Last(item => item.Operation == "document.add-station");
            True(preview.DurationMilliseconds is > 0, "preview duration");
            True(add.DurationMilliseconds is > 0, "add duration");
            Equal("OK", add.Outcome);
            Console.WriteLine($"INFO T-perf preview-promote {preview.DurationMilliseconds:F0} ms, add-station {add.DurationMilliseconds:F0} ms");
        });
    }

    // ---- fixtures ---------------------------------------------------------------------------------------------------

    private static readonly Curve BaseUpper = Definition(FoilSource.NewDefault()).Profiles[0].Upper;
    private static readonly double[] Root = BaseUpper.Points.Select(point => point[1]).ToArray();

    private static double[] Lower(double[] upper) => upper.Select((value, i) => i == 0 || i == upper.Length - 1 ? 0 : -value).ToArray();
    private static double[] Add(double[] values, double[] more) => values.Zip(more, (a, b) => a + b).ToArray();
    private static double[] Bend(double amplitude) => BaseUpper.Points.Select(point => amplitude * 4 * point[0] * (1 - point[0])).ToArray();

    private static ProfileDefinition Prof(string name, double[] upper, double[] lower, string closure = "closed")
    {
        Curve On(Curve curve, double[] y) => curve with { Points = curve.Points.Select((point, i) => new[] { point[0], y[i] }).ToArray() };
        return new ProfileDefinition(name, On(BaseUpper, upper), On(Definition(FoilSource.NewDefault()).Profiles[0].Lower, lower), closure, 0, 0);
    }

    private static byte[] Doc(params (string At, ProfileDefinition Profile)[] stations)
    {
        string text = Encoding.UTF8.GetString(FoilSource.NewDefault());
        int start = text.IndexOf("  profiles {\n", StringComparison.Ordinal);
        int end = text.IndexOf("  tip open", StringComparison.Ordinal);
        var middle = new StringBuilder("  profiles {\n");
        foreach (var (_, profile) in stations) FoilSource.AppendProfile(middle, profile);
        middle.Append("  }\n  sections {");
        foreach (var (at, profile) in stations)
            middle.Append(" at ").Append(at).Append(" profile ").Append(Jcs.Quote(profile.Name));
        middle.Append(" }\n");
        return Encoding.UTF8.GetBytes(text[..start] + middle + text[end..]);
    }

    /// <summary>F-CAMBER: root NACA 0012; tip adds one camber line to both sides (equal unit thickness, regime R1).</summary>
    private static byte[] Camber() => Doc(("root", Prof("root", Root, Lower(Root))), ("tip", Prof("tip", Add(Root, Bend(0.04)), Add(Lower(Root), Bend(0.04)))));

    /// <summary>F-CAMBER3: F-CAMBER plus a station at 60 percent with half the tip's camber.</summary>
    private static byte[] Camber3() => Doc(("root", Prof("root", Root, Lower(Root))), ("60 %", Prof("mid", Add(Root, Bend(0.02)), Add(Lower(Root), Bend(0.02)))),
        ("tip", Prof("tip", Add(Root, Bend(0.04)), Add(Lower(Root), Bend(0.04)))));

    private static readonly double[] PeakTip = [0, 0.032, 0.046, 0.057, 0.0615, 0.060, 0.052, 0.036, 0.016, 0];

    /// <summary>F-PEAK: the tip's thickness peaks further aft than the root's (regime R2).</summary>
    private static byte[] Peak() => Doc(("root", Prof("root", Root, Lower(Root))), ("tip", Prof("tip", PeakTip, Lower(PeakTip))));

    /// <summary>F-THREE: F-CAMBER with a station at 60 percent on F-PEAK's tip (removing it moves the loft far above 10 micrometres).</summary>
    private static byte[] Three() => Doc(("root", Prof("root", Root, Lower(Root))), ("60 %", Prof("peak", PeakTip, Lower(PeakTip))),
        ("tip", Prof("tip", Add(Root, Bend(0.04)), Add(Lower(Root), Bend(0.04)))));

    /// <summary>
    /// F-SAMEPEAK: control ordinates 2 to 7 of the upper side are one value on both profiles (a plateau: the degree-5 curve is exactly
    /// constant and maximal on the middle span), differing at ordinates 1 and 8. The unit thickness shapes differ and reach 1 together.
    /// </summary>
    private static byte[] SamePeak()
    {
        double[] Plateau(double a, double b) => [0, a, 0.06, 0.06, 0.06, 0.06, 0.06, 0.06, b, 0];
        double[] left = Plateau(0.045, 0.020), right = Plateau(0.030, 0.040);
        return Doc(("root", Prof("root", left, Lower(left))), ("tip", Prof("tip", right, Lower(right))));
    }

    private static byte[] Real()
    {
        byte[] unique = FoilSource.MakeIndependent(FoilSource.NewDefault(), "naca-0012", 1).Source;
        var source = new ReplaceSource.Coordinates("NACA 4412", new Provenance("gen:naca-4412", false), CatalogGenerator.Naca4("4412"));
        return SectionReplace.Preview(unique, 1, SectionScope.Independent, source, ReplaceScope.Draft).Bytes!;
    }

    private static byte[] Six()
    {
        var stations = new List<(string, ProfileDefinition)>();
        string[] at = ["root", "20 %", "40 %", "60 %", "80 %", "tip"];
        for (int i = 0; i < at.Length; i++) stations.Add((at[i], Prof("s" + i, Add(Root, Bend(0.01 * i)), Add(Lower(Root), Bend(0.01 * i)))));
        return Doc([.. stations]);
    }

    private static byte[] Seven()
    {
        var stations = new List<(string, ProfileDefinition)>();
        string[] at = ["root", "20 %", "40 %", "60 %", "70 %", "80 %", "tip"];
        for (int i = 0; i < at.Length; i++) stations.Add((at[i], Prof("s" + i, Add(Root, Bend(0.01 * i)), Add(Lower(Root), Bend(0.01 * i)))));
        return Doc([.. stations]);
    }

    /// <summary>F-SPACING: a 6-point tip beside the 10-point root.</summary>
    private static byte[] Spacing()
    {
        var tip = Prof("tip", Root, Lower(Root));
        Curve Six(Curve curve, double sign) => curve with
        {
            Knots = [0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1],
            Points = [[0, 0], [0, sign * 0.03], [0.2, sign * 0.06], [0.5, sign * 0.05], [0.8, sign * 0.02], [1, 0]],
            Ids = ["cv-0", "cv-1", "cv-2", "cv-3", "cv-4", "cv-5"]
        };
        var coarse = new ProfileDefinition("tip", Six(tip.Upper, 1), Six(tip.Lower, -1), "closed", 0, 0);
        return Doc(("root", Prof("root", Root, Lower(Root))), ("tip", coarse));
    }

    // ---- helpers ----------------------------------------------------------------------------------------------------

    private static AuthoringSession Open(byte[] source)
    {
        var session = new AuthoringSession();
        session.Open(source, Id(), true);
        return session;
    }

    private static Definition Definition(byte[] source) => FoilSource.Parse(source).Definition ?? throw new InvalidOperationException("source does not parse");

    private static (double Eta, int Profile)[] Assignments(byte[] source) => Definition(source).Assignments;

    private static double Pct(string text) => DecimalSi.Parse(text, -2);

    private static double Metres(string millimetres) => DecimalSi.Parse(millimetres, -3) / HalfSpan;

    private static double[] Xs(int count) => Enumerable.Range(0, count).Select(i => i / (double)(count - 1)).ToArray();

    private static double CamberOf(ProfileDefinition profile, int i) => (profile.Upper.Points[i][1] + profile.Lower.Points[i][1]) / 2;

    private static double ThicknessAt(byte[] source, double eta) => ChannelEvaluator.At(Definition(source).Curves["thickness"], eta);

    private static string Channel(byte[] source, string name)
    {
        var curve = Definition(source).Curves[name];
        return string.Join(";", curve.Knots.Select(value => value.ToString("R", CultureInfo.InvariantCulture))) + "|" +
            string.Join(";", curve.Points.Select(point => point[0].ToString("R", CultureInfo.InvariantCulture) + "," + point[1].ToString("R", CultureInfo.InvariantCulture)));
    }

    /// <summary>The largest |upper| or |lower| ordinate difference of two documents over the etas and chord stations, in chord fractions.</summary>
    private static double MaxDifference(byte[] before, byte[] after, IReadOnlyList<double> etas, IReadOnlyList<double> xs)
    {
        var a = Placement.Sections(before, etas, xs, CancellationToken.None);
        var b = Placement.Sections(after, etas, xs, CancellationToken.None);
        double worst = 0;
        for (int i = 0; i < etas.Count; i++)
            for (int j = 0; j < xs.Count; j++)
            {
                double camber = b[i].Camber[j] - a[i].Camber[j], width = (b[i].Thickness[j] - a[i].Thickness[j]) / 2;
                worst = Math.Max(worst, Math.Max(Math.Abs(camber + width), Math.Abs(camber - width)));
            }
        return worst;
    }

    /// <summary>
    /// k of the promotion at <paramref name="eta"/>: the maximum of the blended unit thickness (1 - w) U_L + w U_R, from the neighbours' own
    /// curves. It is 1 exactly when both unit shapes reach 1 at one chord fraction. Measured here, never used to decide the regime.
    /// </summary>
    private static double K(byte[] source, double eta)
    {
        var definition = Definition(source);
        double left = definition.Assignments[0].Eta, right = definition.Assignments[^1].Eta, w = (eta - left) / (right - left);
        var l = definition.Profiles[definition.Assignments[0].Profile];
        var r = definition.Profiles[definition.Assignments[^1].Profile];
        double ml = Placement.ProfileDifferenceMaximum(l.Upper, l.Lower), mr = Placement.ProfileDifferenceMaximum(r.Upper, r.Lower);
        double best = 0;
        for (int i = 0; i <= 4000; i++)
        {
            double x = i / 4000d;
            double ul = (ProfileEvaluator.OrdinateAt(l.Upper, x) - ProfileEvaluator.OrdinateAt(l.Lower, x)) / ml;
            double ur = (ProfileEvaluator.OrdinateAt(r.Upper, x) - ProfileEvaluator.OrdinateAt(r.Lower, x)) / mr;
            best = Math.Max(best, (1 - w) * ul + w * ur);
        }
        return best;
    }

    private static void True(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
