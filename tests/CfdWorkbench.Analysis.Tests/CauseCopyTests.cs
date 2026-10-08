using System.Text.RegularExpressions;
using CfdWorkbench.Core;
using static CfdWorkbench.Analysis.Tests.AnalysisChecks;

namespace CfdWorkbench.Analysis.Tests;

/// <summary>Ruling 155: the plain-cause rows COPY-412 to COPY-431 behind the one <see cref="Labels"/> lookup. Ring: fast, each under 0.2 s.</summary>
internal static class CauseCopyTests
{
    private const string Kept = " — the previous file is intact and your changes are kept. ";

    internal static void Run()
    {
        Check("Labels_CauseRows_Ruling155_ExactText", CauseRowsExact);
        Check("Labels_UnknownCode_GenericRowWithCode_Ruling155", UnknownCodeGeneric);
        Check("Labels_UncertainSave_NamesNeitherSavedNorNotSaved_Copy421", UncertainWording);
        Check("Labels_NoRawDocDslCodeAloneOnAProductSurface", NoRawCodeAlone);
        Check("Labels_NoStatusLineLeadsWithACode_Ruling158", NoStatusLeadsWithCode);
        Check("Labels_SaveRetryRows_Ruling158_ExactText", SaveRetryExact);
        Check("Labels_SaveRetryNotConfirmed_OkCode_NeverPrintsOk_Ruling158", RetryOkNeverPrinted);
        Check("Labels_DraftUnavailable_Ruling158_ExactText", DraftUnavailableExact);
    }

    private const string KeptUnsaved = " Your changes are kept and still marked unsaved. ";

    /// <summary>COPY-432 to 435, typed here independently of Labels.</summary>
    private static void SaveRetryExact()
    {
        Equal("Couldn't check the saved file (DOC-IO)." + KeptUnsaved + "Retry, or use Save As.", Labels.ReadBackFailed("DOC-IO"));
        Equal("Save still not confirmed (DOC-CONFLICT)." + KeptUnsaved + "Retry, or use Save As.", Labels.RetryNotConfirmed("DOC-CONFLICT"));
        Equal("Save still not confirmed: the disk didn't confirm the file was stored." + KeptUnsaved + "Retry, or use Save As.", Labels.RetryNotConfirmed("OK"));
        Equal("The file on disk changed after this save was attempted." + KeptUnsaved + "Use Save As to keep them without overwriting the other version.", Labels.DiskChangedAfterSave);
    }

    private static void RetryOkNeverPrinted()
    {
        string text = Labels.RetryNotConfirmed("OK");
        Equal(false, text.Contains("(OK)", StringComparison.Ordinal) || text.Contains("OK", StringComparison.Ordinal), text);
    }

    /// <summary>COPY-436 to 438: each status, with and without reasons, with and without a station; the draft id never appears.</summary>
    private static void DraftUnavailableExact()
    {
        const string shown = " The last valid shape is still shown.";
        Equal("Station 2: this shape isn't valid yet — chord is too short (DSL-GEOMETRY)." + shown, Labels.DraftUnavailable(GeometryStatus.Invalid, "Station 2", "DSL-GEOMETRY", "chord is too short"));
        Equal("Station 2: this shape isn't valid yet (DSL-GEOMETRY)." + shown, Labels.DraftUnavailable(GeometryStatus.Invalid, "Station 2", "DSL-GEOMETRY", ""));
        Equal("Tip: this shape uses something CFD Workbench can't check yet — open trailing edge only (DSL-UNSUPPORTED)." + shown, Labels.DraftUnavailable(GeometryStatus.Unsupported, "Tip", "DSL-UNSUPPORTED", "open trailing edge only"));
        Equal("Root: this shape uses something CFD Workbench can't check yet (DSL-UNSUPPORTED)." + shown, Labels.DraftUnavailable(GeometryStatus.Unsupported, "Root", "DSL-UNSUPPORTED", ""));
        Equal("Station 3: this shape couldn't be checked in time (ANA-BUDGET). The last valid shape is still shown; keep editing or try again.", Labels.DraftUnavailable(GeometryStatus.NotAssessed, "Station 3", "ANA-BUDGET", "ignored"));
        Equal("This shape isn't valid yet — r (C)." + shown, Labels.DraftUnavailable(GeometryStatus.Invalid, null, "C", "r"));
    }

    /// <summary>Control (Ruling 158): no interpolated status in src/ leads with a code, <c>$"{x.Code}: ..."</c>, even after a draft prefix. The code belongs
    /// inside "(code)" after a plain cause (Labels). One known site is listed: "Geometry display unavailable" has no ruled copy yet (reported, not fixed).
    /// Ring: Analysis fast, one scan of src/.</summary>
    private static void NoStatusLeadsWithCode()
    {
        var leading = new Regex(@"\$""(\{[A-Za-z_.]*Prefix\(\)\})?\{[A-Za-z_.]*Code\}:");
        var found = new List<string>();
        foreach (string file in Directory.EnumerateFiles(Path.Combine(StripFixtureTests.RepoRoot(), "src"), "*.cs", SearchOption.AllDirectories))
            foreach (string line in File.ReadAllLines(file))
                if (leading.IsMatch(line) && !line.Contains("Geometry display unavailable", StringComparison.Ordinal))
                    found.Add(Path.GetFileName(file) + ": " + line.Trim());
        if (found.Count > 0) throw new InvalidOperationException(found.Count + " status lines lead with a code, first: " + found[0]);
    }

    /// <summary>Each family's code gives the approved text, typed here independently of Labels.</summary>
    private static void CauseRowsExact()
    {
        (string Code, string Text)[] save =
        [
            ("DOC-IO", "Save failed: the disk couldn't be written (DOC-IO)" + Kept + "Retry or Save As."),
            ("DOC-DISK-FULL", "Save failed: the disk is full (DOC-DISK-FULL)" + Kept + "Free some space, then Retry or Save As."),
            ("DOC-CONFLICT", "Save failed: the file is in use by another program, or it changed since you opened it (DOC-CONFLICT)" + Kept + "Close the other program and Retry, or Save As."),
            ("DOC-CANCELLED", "Save failed: the save was cancelled (DOC-CANCELLED)" + Kept + "Retry or Save As."),
            ("DOC-SAVE-PENDING", "Save failed: another save is still running (DOC-SAVE-PENDING)" + Kept + "Wait for it to finish, then Retry."),
            ("DOC-CLOSED", "Save failed: this project was closed (DOC-CLOSED)" + Kept + "Open the project again."),
            ("DOC-SIZE", "Save failed: the project is larger than CFD Workbench can save (DOC-SIZE)" + Kept + "Save As won't help; remove content first."),
            ("DOC-TYPE", "Save failed: the file name must end in .cfdw.json (DOC-TYPE)" + Kept + "Choose another name."),
            ("DSL-DRAFT-OWNED", "Save failed: a change is still in progress (DSL-DRAFT-OWNED)" + Kept + "Finish or cancel the change, then Save."),
            ("DSL-INVALID-NUMERIC", "Save failed: a change is still in progress (DSL-INVALID-NUMERIC)" + Kept + "Finish or cancel the change, then Save."),
            ("DOC-SAVE-UNCERTAIN", "Save not confirmed: CFD Workbench couldn't tell whether the file was fully written (DOC-SAVE-UNCERTAIN). Your changes are kept and still marked unsaved. Retry to check the file, or Save As."),
            ("DOC-HASH", "Save failed: CFD Workbench's own check of the project data failed (DOC-HASH)" + Kept + "Retry; if it happens again, Save As and report it."),
            ("DOC-EMPTY", "Save failed: CFD Workbench's own check of the project data failed (DOC-EMPTY)" + Kept + "Retry; if it happens again, Save As and report it."),
        ];
        foreach (var (code, text) in save) Equal(text, Labels.SaveRefusal(code), code);

        string edit = "This change wasn't applied: the shape can't take it as entered ({0}). Nothing changed. Adjust it and try again.";
        foreach (string code in new[] { "DSL-LOCK", "DSL-CURVE", "DSL-GROUP-HANDLE", "DSL-GROUP-NEIGHBOUR", "DSL-GROUP-RANGE", "DSL-GROUP-SPAN-SET", "DSL-PROFILE-ORDER", "DSL-PROFILE-CROSS", "DSL-EDGES-CROSS", "DSL-UNIT", "DSL-GEOMETRY", "DSL-TOLERANCE", "DSL-TIP-CHORD-MIN" })
            Equal(string.Format(edit, code), Labels.Refusal(code), code);
        string target = "This change wasn't applied: what it pointed at has changed or is busy ({0}). Nothing changed. Try again.";
        foreach (string code in new[] { "DSL-TARGET", "DSL-PROFILE-TARGET", "DSL-ID", "DSL-CONFLICT", "DSL-STALE", "DSL-VALIDATION-BUSY", "DSL-DRAFT-REUSED", "DSL-CANCELLED", "DOC-OPERATION-CONFLICT" })
            Equal(string.Format(target, code), Labels.Refusal(code), code);
        string source = "This source can't be accepted: it isn't valid foil source ({0}). The original is kept, read-only, and nothing was changed.";
        foreach (string code in new[] { "DSL-SYNTAX", "DSL-LEX", "DSL-LEGACY", "DSL-REFERENCE", "DSL-INVALID", "DSL-UNSUPPORTED" })
            Equal(string.Format(source, code), Labels.Refusal(code), code);
        Equal("This source can't be accepted: it was written for a version of the foil format this build doesn't read (DSL-VERSION). The original is kept, read-only, and nothing was changed.", Labels.Refusal("DSL-VERSION"));
        Equal("This source can't be accepted: it is larger than CFD Workbench can read (DSL-LIMIT). The original is kept, read-only, and nothing was changed.", Labels.Refusal("DSL-LIMIT"));
        Equal("This file can't be imported as a section (DSL-IMPORT) — line 12 is not a number. Nothing was changed.", Labels.Refusal("DSL-IMPORT", "line 12 is not a number"));
        Equal("Code: DOC-IO", Labels.OpenCodeLine("DOC-IO"));
    }

    /// <summary>An unknown code goes to the generic row and carries the code (COPY-423 on save, COPY-431 elsewhere).</summary>
    private static void UnknownCodeGeneric()
    {
        Equal("Save failed: something unexpected stopped the save (DOC-NEW-THING). Your changes are kept in this session. Check the file before relying on it, then Retry or Save As.", Labels.SaveRefusal("DOC-NEW-THING"));
        Equal("Something unexpected stopped this (DSL-NEW-THING). Nothing changed.", Labels.Refusal("DSL-NEW-THING"));
        Equal("Something unexpected stopped this (DOC-REFERENCE). Nothing changed.", Labels.Refusal("DOC-REFERENCE"));
    }

    private static void UncertainWording()
    {
        string text = Labels.SaveRefusal("DOC-SAVE-UNCERTAIN");
        Equal(false, Regex.IsMatch(text, @"\b(not\s+)?saved\b", RegexOptions.IgnoreCase), text);
        Equal(false, text.Contains("intact", StringComparison.Ordinal), text);
    }

    /// <summary>Control (Ruling 155): for every quoted DOC-/DSL- code in src/, the save and refusal text names a plain cause and shows the code
    /// only inside "(code)". The codes are read from the source at test time, so a new literal is covered the day it lands.</summary>
    private static void NoRawCodeAlone()
    {
        var codes = new SortedSet<string>(StringComparer.Ordinal);
        var literal = new Regex("\"((?:DOC|DSL)-[A-Z0-9-]+)\"");
        foreach (string file in Directory.EnumerateFiles(Path.Combine(StripFixtureTests.RepoRoot(), "src"), "*.cs", SearchOption.AllDirectories))
            foreach (Match match in literal.Matches(File.ReadAllText(file))) codes.Add(match.Groups[1].Value);
        Equal(true, codes.Count > 40, "codes found " + codes.Count);
        var bad = new List<string>();
        foreach (string code in codes)
            foreach (string text in new[] { Labels.SaveRefusal(code), Labels.Refusal(code) })
            {
                if (text == Labels.SaveUnavailable) continue;
                string rest = text.Replace("(" + code + ")", "", StringComparison.Ordinal);
                if (!text.Contains("(" + code + ")", StringComparison.Ordinal) || rest.Contains(code, StringComparison.Ordinal) || rest.Length < 40)
                    bad.Add(code + " → " + text);
            }
        if (bad.Count > 0) throw new InvalidOperationException(bad.Count + " raw-code texts, first: " + bad[0]);
    }
}
