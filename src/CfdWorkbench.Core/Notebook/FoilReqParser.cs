using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace CfdWorkbench.Core.Notebook;

/// <summary>
/// Strict recursive-descent parser for <c>FoilReq 1.0</c> implementing the closed §A5 EBNF grammar
/// (<c>docs/specs/ai-guided-design-notebook.md:143-192</c>). Pure zero-I/O Core.
/// </summary>
public static class FoilReqParser
{
    public const string SyntaxErrorCode = "NB-FOILREQ-SYNTAX";
    private const int MaxInputBytes = 131_072;

    private static readonly Regex IdentRegex = new(@"\A[a-z][a-z0-9_]*\z", RegexOptions.CultureInvariant);
    private static readonly Regex TraitRegex = new(@"\A[a-z][a-z0-9-]*\z", RegexOptions.CultureInvariant);
    private static readonly Regex NumberRegex = new(@"\A-?[0-9]+(\.[0-9]+)?\z", RegexOptions.CultureInvariant);

    public static FoilReqParseResult Parse(string? dslText)
    {
        if (string.IsNullOrWhiteSpace(dslText))
            return Fail("FoilReq source is empty.", 1, 1);

        if (Encoding.UTF8.GetByteCount(dslText) > MaxInputBytes)
            return Fail($"FoilReq source exceeds {MaxInputBytes} UTF-8 bytes.", 1, 1);

        try
        {
            var tokens = Tokenize(dslText);
            var reader = new TokenReader(tokens);
            var doc = ParseDocument(reader);
            return new FoilReqParseResult(true, doc, null, null);
        }
        catch (ParseFailure ex)
        {
            return Fail(ex.Message, ex.Line, ex.Column);
        }
    }

    private static FoilReqParseResult Fail(string message, int line, int col) =>
        new(false, null, SyntaxErrorCode, $"{SyntaxErrorCode} (line {line}, col {col}): {message}", line, col);

    private static FoilReqDocument ParseDocument(TokenReader r)
    {
        r.ExpectWord("foilreq");
        var verTok = r.ExpectString();
        if (verTok.Value != FoilReqDocument.CurrentVersion)
            throw r.Error($"Unsupported FoilReq version '{verTok.Value}'; expected \"{FoilReqDocument.CurrentVersion}\".", verTok);
        r.ExpectSymbol("{");

        FoilReqBrief? brief = null;
        FoilReqRider? rider = null;
        FoilReqWater? water = null;
        var envelope = new List<FoilReqConstraint>();
        var references = new List<FoilReqReference>();
        var objectives = new List<FoilReqObjective>();
        var maneuvers = new List<FoilReqManeuver>();
        var openQuestions = new List<FoilReqOpenQuestion>();

        int blockCount = 0;
        while (!r.MatchSymbol("}"))
        {
            if (r.IsEof)
                throw r.Error("Unexpected end of file before closing '}' of foilreq block.");
            var blockKw = r.ExpectWordToken();
            blockCount++;
            switch (blockKw.Value)
            {
                case "brief":
                    if (brief is not null) throw r.Error("Duplicate 'brief' block.", blockKw);
                    brief = ParseBriefBlock(r);
                    break;
                case "rider":
                    if (rider is not null) throw r.Error("Duplicate 'rider' block.", blockKw);
                    rider = ParseRiderBlock(r);
                    break;
                case "water":
                    if (water is not null) throw r.Error("Duplicate 'water' block.", blockKw);
                    water = ParseWaterBlock(r);
                    break;
                case "envelope":
                    envelope.AddRange(ParseEnvelopeBlock(r));
                    break;
                case "reference":
                    references.Add(ParseReferenceBlock(r));
                    break;
                case "objectives":
                    objectives.AddRange(ParseObjectivesBlock(r));
                    break;
                case "maneuvers":
                    maneuvers.AddRange(ParseManeuversBlock(r));
                    break;
                case "open_questions":
                    openQuestions.AddRange(ParseOpenQuestionsBlock(r));
                    break;
                default:
                    throw r.Error($"Unknown FoilReq block '{blockKw.Value}'.", blockKw);
            }
        }

        if (blockCount == 0)
            throw r.Error("FoilReq document requires at least one block.");
        if (!r.IsEof)
            throw r.Error($"Unexpected trailing token '{r.Current.Value}'.", r.Current);
        if (brief is null)
            throw r.Error("FoilReq document requires a 'brief' block.");
        if (rider is null)
            throw r.Error("FoilReq document requires a 'rider' block.");
        if (water is null)
            throw r.Error("FoilReq document requires a 'water' block.");

        return new FoilReqDocument(
            FoilReqDocument.CurrentVersion,
            brief,
            rider,
            water,
            envelope,
            references,
            objectives,
            maneuvers,
            openQuestions);
    }

    private static FoilReqBrief ParseBriefBlock(TokenReader r)
    {
        r.ExpectSymbol("{");
        r.ExpectWord("discipline");
        r.ExpectSymbol(":");
        var discTok = r.ExpectWordToken();
        if (!FoilReqDocument.AllowedDisciplines.Contains(discTok.Value))
            throw r.Error($"Unknown discipline '{discTok.Value}'.", discTok);
        r.ExpectSymbol(";");

        r.ExpectWord("skill");
        r.ExpectSymbol(":");
        var skillTok = r.ExpectWordToken();
        if (!FoilReqDocument.AllowedSkills.Contains(skillTok.Value))
            throw r.Error($"Unknown skill '{skillTok.Value}'.", skillTok);
        r.ExpectSymbol(";");
        r.ExpectSymbol("}");
        return new FoilReqBrief(discTok.Value, skillTok.Value);
    }

    private static FoilReqRider ParseRiderBlock(TokenReader r)
    {
        r.ExpectSymbol("{");
        var fields = new List<FoilReqRiderField>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        while (!r.MatchSymbol("}"))
        {
            var keyTok = r.ExpectWordToken();
            if (!FoilReqDocument.AllowedRiderFields.Contains(keyTok.Value))
                throw r.Error($"Unknown rider field '{keyTok.Value}'.", keyTok);
            if (!seen.Add(keyTok.Value))
                throw r.Error($"Duplicate rider field '{keyTok.Value}'.", keyTok);
            r.ExpectSymbol(":");
            var qty = ParseQty(r);
            var tag = ParseTag(r);
            r.ExpectSymbol(";");
            fields.Add(new FoilReqRiderField(keyTok.Value, qty, tag));
        }
        if (fields.Count == 0)
            throw r.Error("rider block requires at least one field.");
        return new FoilReqRider(fields);
    }

    private static FoilReqWater ParseWaterBlock(TokenReader r)
    {
        r.ExpectSymbol("{");
        string? kind = null;
        FoilReqTag? kindTag = null;
        FoilReqQty? temp = null;
        FoilReqTag? tempTag = null;
        FoilReqQty? depthRef = null;
        FoilReqTag? depthTag = null;
        bool depthUnknown = false;
        FoilReqQty? pAtm = null;
        FoilReqTag? pAtmTag = null;
        var seen = new HashSet<string>(StringComparer.Ordinal);

        while (!r.MatchSymbol("}"))
        {
            var keyTok = r.ExpectWordToken();
            if (!seen.Add(keyTok.Value))
                throw r.Error($"Duplicate water field '{keyTok.Value}'.", keyTok);
            r.ExpectSymbol(":");
            switch (keyTok.Value)
            {
                case "kind":
                    var wKind = r.ExpectWordToken();
                    if (wKind.Value is not ("salt" or "fresh"))
                        throw r.Error($"water.kind must be 'salt' or 'fresh', got '{wKind.Value}'.", wKind);
                    kind = wKind.Value;
                    kindTag = ParseTag(r);
                    r.ExpectSymbol(";");
                    break;
                case "temp":
                    temp = ParseQty(r);
                    tempTag = ParseTag(r);
                    r.ExpectSymbol(";");
                    break;
                case "depth_ref":
                    if (r.MatchSymbol("?"))
                    {
                        depthUnknown = true;
                        depthRef = null;
                    }
                    else
                    {
                        depthRef = ParseQty(r);
                    }
                    depthTag = ParseTag(r);
                    r.ExpectSymbol(";");
                    break;
                case "p_atm":
                    pAtm = ParseQty(r);
                    pAtmTag = ParseTag(r);
                    r.ExpectSymbol(";");
                    break;
                default:
                    throw r.Error($"Unknown water field '{keyTok.Value}'.", keyTok);
            }
        }

        if (kind is null || kindTag is null)
            throw r.Error("water block requires 'kind'.");
        if (temp is null || tempTag is null)
            throw r.Error("water block requires 'temp'.");

        return new FoilReqWater(kind, kindTag, temp, tempTag, depthRef, depthTag, depthUnknown, pAtm, pAtmTag);
    }

    private static List<FoilReqConstraint> ParseEnvelopeBlock(TokenReader r)
    {
        r.ExpectSymbol("{");
        var list = new List<FoilReqConstraint>();
        while (!r.MatchSymbol("}"))
        {
            var keyTok = r.ExpectWordToken();
            if (!FoilReqDocument.AllowedConstraintKeys.Contains(keyTok.Value))
                throw r.Error($"Unknown envelope constraint key '{keyTok.Value}'.", keyTok);
            r.ExpectSymbol(":");
            string comp = ParseComparator(r);
            FoilReqQty? qty = null;
            bool isUnknown = false;
            if (r.MatchSymbol("?"))
                isUnknown = true;
            else
                qty = ParseQty(r);
            FoilReqPriority? priority = TryParsePriority(r);
            var tag = ParseTag(r);
            r.ExpectSymbol(";");
            list.Add(new FoilReqConstraint(keyTok.Value, comp, qty, isUnknown, priority, tag));
        }
        if (list.Count == 0)
            throw r.Error("envelope block requires at least one constraint line.");
        return list;
    }

    private static FoilReqReference ParseReferenceBlock(TokenReader r)
    {
        var nameTok = r.ExpectString();
        r.ExpectSymbol("{");

        r.ExpectWord("traits");
        r.ExpectSymbol(":");
        r.ExpectSymbol("[");
        var traits = new List<string>();
        var firstTrait = r.ExpectWordToken();
        if (!TraitRegex.IsMatch(firstTrait.Value))
            throw r.Error($"Invalid trait identifier '{firstTrait.Value}'.", firstTrait);
        traits.Add(firstTrait.Value);
        while (r.MatchSymbol(","))
        {
            var nextTrait = r.ExpectWordToken();
            if (!TraitRegex.IsMatch(nextTrait.Value))
                throw r.Error($"Invalid trait identifier '{nextTrait.Value}'.", nextTrait);
            traits.Add(nextTrait.Value);
        }
        r.ExpectSymbol("]");
        r.ExpectSymbol(";");

        r.ExpectWord("policy");
        r.ExpectSymbol(":");
        var policyTok = r.ExpectString();
        if (policyTok.Value != FoilReqDocument.ConceptualReferencePolicy)
            throw r.Error($"reference.policy must be \"{FoilReqDocument.ConceptualReferencePolicy}\".", policyTok);
        r.ExpectSymbol(";");

        r.ExpectWord("confidence");
        r.ExpectSymbol(":");
        var confTok = r.ExpectWordToken();
        if (confTok.Value is not ("Inferred" or "Flagged"))
            throw r.Error($"reference.confidence must be 'Inferred' or 'Flagged', got '{confTok.Value}'.", confTok);
        r.ExpectSymbol(";");
        r.ExpectSymbol("}");

        return new FoilReqReference(nameTok.Value, traits, policyTok.Value, confTok.Value);
    }

    private static List<FoilReqObjective> ParseObjectivesBlock(TokenReader r)
    {
        r.ExpectSymbol("{");
        var list = new List<FoilReqObjective>();
        while (!r.MatchSymbol("}"))
        {
            var keyTok = r.ExpectWordToken();
            if (!IdentRegex.IsMatch(keyTok.Value))
                throw r.Error($"Invalid objective identifier '{keyTok.Value}'.", keyTok);
            r.ExpectSymbol(":");

            string? comp = null;
            FoilReqQty? qty = null;
            FoilReqPriority? valuePriority = null;
            bool isUnknown = false;

            if (r.IsComparator())
            {
                comp = ParseComparator(r);
                qty = ParseQty(r);
            }
            else if (r.MatchSymbol("?"))
            {
                isUnknown = true;
            }
            else if (TryParsePriority(r) is { } vp)
            {
                valuePriority = vp;
            }
            else
            {
                throw r.Error($"Expected comparator + quantity, priority, or '?' in objective '{keyTok.Value}'.", r.Current);
            }

            FoilReqPriority? extraPriority = TryParsePriority(r);
            var tag = ParseTag(r);
            r.ExpectSymbol(";");
            list.Add(new FoilReqObjective(keyTok.Value, comp, qty, valuePriority, isUnknown, extraPriority, tag));
        }
        if (list.Count == 0)
            throw r.Error("objectives block requires at least one line.");
        return list;
    }

    private static List<FoilReqManeuver> ParseManeuversBlock(TokenReader r)
    {
        r.ExpectSymbol("{");
        var list = new List<FoilReqManeuver>();
        while (!r.MatchSymbol("}"))
        {
            var keyTok = r.ExpectWordToken();
            if (!IdentRegex.IsMatch(keyTok.Value))
                throw r.Error($"Invalid maneuver identifier '{keyTok.Value}'.", keyTok);
            r.ExpectSymbol(":");
            var descTok = r.ExpectString();
            var prio = TryParsePriority(r) ?? throw r.Error($"Maneuver '{keyTok.Value}' requires a priority.", r.Current);
            var tag = ParseTag(r);
            r.ExpectSymbol(";");
            list.Add(new FoilReqManeuver(keyTok.Value, descTok.Value, prio, tag));
        }
        if (list.Count == 0)
            throw r.Error("maneuvers block requires at least one line.");
        return list;
    }

    private static List<FoilReqOpenQuestion> ParseOpenQuestionsBlock(TokenReader r)
    {
        r.ExpectSymbol("{");
        var list = new List<FoilReqOpenQuestion>();
        while (!r.MatchSymbol("}"))
        {
            var idTok = r.ExpectWordToken();
            if (!IdentRegex.IsMatch(idTok.Value))
                throw r.Error($"Invalid open_question identifier '{idTok.Value}'.", idTok);
            r.ExpectSymbol(":");
            var promptTok = r.ExpectString();
            var stateTok = r.ExpectTagToken();
            if (!FoilReqDocument.AllowedQuestionStates.Contains(stateTok.Value))
                throw r.Error($"Question state must be @open, @resolved, or @skipped; got '{stateTok.Value}'.", stateTok);
            r.ExpectSymbol(";");
            list.Add(new FoilReqOpenQuestion(idTok.Value, promptTok.Value, stateTok.Value));
        }
        return list;
    }

    private static string ParseComparator(TokenReader r)
    {
        var tok = r.Current;
        if (tok.Kind == TokKind.Symbol && tok.Value is "<=" or ">=" or "~" or "=")
        {
            r.Advance();
            return tok.Value;
        }
        throw r.Error($"Expected comparator (<=, >=, ~, =), got '{tok.Value}'.", tok);
    }

    private static FoilReqQty ParseQty(TokenReader r)
    {
        var numTok = r.ExpectNumber();
        if (!double.TryParse(numTok.Value, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double val) ||
            !double.IsFinite(val))
        {
            throw r.Error($"Invalid numeric literal '{numTok.Value}'.", numTok);
        }

        string? unit = null;
        if (!r.IsEof && r.Current.Kind is TokKind.Word or TokKind.Symbol)
        {
            if (r.Current.Value == "%")
            {
                unit = "%";
                r.Advance();
            }
            else if (r.Current.Kind == TokKind.Word && FoilReqDocument.AllowedUnits.Contains(r.Current.Value))
            {
                unit = r.Current.Value;
                r.Advance();
            }
        }
        return new FoilReqQty(val, unit);
    }

    private static FoilReqPriority? TryParsePriority(TokenReader r)
    {
        if (r.IsEof || r.Current.Kind != TokKind.Word) return null;
        string w = r.Current.Value;
        if (w is "hard" or "high" or "medium" or "low")
        {
            r.Advance();
            return new FoilReqPriority(w);
        }
        if (w == "soft")
        {
            r.Advance();
            r.ExpectSymbol("(");
            var numTok = r.ExpectNumber();
            if (!double.TryParse(numTok.Value, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double weight) ||
                !double.IsFinite(weight) || weight < 0.0 || weight > 1.0)
            {
                throw r.Error($"soft(...) weight must be finite in [0.0, 1.0], got '{numTok.Value}'.", numTok);
            }
            r.ExpectSymbol(")");
            return new FoilReqPriority("soft", weight);
        }
        return null;
    }

    private static FoilReqTag ParseTag(TokenReader r)
    {
        var tagTok = r.ExpectTagToken();
        if (!FoilReqDocument.AllowedTags.Contains(tagTok.Value))
            throw r.Error($"Unknown tag '{tagTok.Value}'.", tagTok);
        string? rationale = null;
        if (r.MatchSymbol("("))
        {
            rationale = r.ExpectString().Value;
            r.ExpectSymbol(")");
        }
        return new FoilReqTag(tagTok.Value, rationale);
    }

    private enum TokKind { Word, Number, String, Tag, Symbol, Eof }

    private readonly record struct Token(TokKind Kind, string Value, int Line, int Column);

    private sealed class ParseFailure(string message, int line, int column) : Exception(message)
    {
        public int Line { get; } = line;
        public int Column { get; } = column;
    }

    private static List<Token> Tokenize(string src)
    {
        var list = new List<Token>();
        int i = 0;
        int line = 1;
        int col = 1;

        while (i < src.Length)
        {
            char c = src[i];
            if (c is ' ' or '\t' or '\r')
            {
                i++; col++;
                continue;
            }
            if (c == '\n')
            {
                i++; line++; col = 1;
                continue;
            }
            // Allow // line comments outside strings
            if (c == '/' && i + 1 < src.Length && src[i + 1] == '/')
            {
                while (i < src.Length && src[i] != '\n') { i++; col++; }
                continue;
            }

            int startLine = line;
            int startCol = col;

            // Quoted string
            if (c == '"')
            {
                i++; col++;
                var sb = new StringBuilder();
                bool closed = false;
                while (i < src.Length)
                {
                    char ch = src[i++];
                    col++;
                    if (ch == '"') { closed = true; break; }
                    if (ch == '\n' || ch == '\r')
                        throw new ParseFailure("Unterminated string literal.", startLine, startCol);
                    if (ch == '\\')
                    {
                        if (i >= src.Length)
                            throw new ParseFailure("Unterminated escape in string literal.", line, col);
                        char esc = src[i++];
                        col++;
                        sb.Append(esc switch
                        {
                            '"' => '"',
                            '\\' => '\\',
                            'n' => '\n',
                            't' => '\t',
                            _ => esc
                        });
                    }
                    else
                    {
                        sb.Append(ch);
                    }
                }
                if (!closed)
                    throw new ParseFailure("Unterminated string literal.", startLine, startCol);
                list.Add(new Token(TokKind.String, sb.ToString(), startLine, startCol));
                continue;
            }

            // Tag (@stated, @flagged, @open, ...)
            if (c == '@')
            {
                int start = i;
                i++; col++;
                while (i < src.Length && (char.IsAsciiLetterLower(src[i]) || char.IsAsciiDigit(src[i]) || src[i] == '_'))
                {
                    i++; col++;
                }
                string tagVal = src[start..i];
                if (tagVal.Length < 2)
                    throw new ParseFailure("Empty '@' tag.", startLine, startCol);
                list.Add(new Token(TokKind.Tag, tagVal, startLine, startCol));
                continue;
            }

            // Two-char comparators <=, >=
            if ((c == '<' || c == '>') && i + 1 < src.Length && src[i + 1] == '=')
            {
                list.Add(new Token(TokKind.Symbol, src.Substring(i, 2), startLine, startCol));
                i += 2; col += 2;
                continue;
            }

            // Single-char punctuation
            if (c is '{' or '}' or '[' or ']' or '(' or ')' or ':' or ';' or ',' or '?' or '~' or '=' or '%')
            {
                list.Add(new Token(TokKind.Symbol, c.ToString(), startLine, startCol));
                i++; col++;
                continue;
            }

            // Number: optional '-' followed by digit
            if (char.IsAsciiDigit(c) || (c == '-' && i + 1 < src.Length && char.IsAsciiDigit(src[i + 1])))
            {
                int start = i;
                if (c == '-') { i++; col++; }
                while (i < src.Length && char.IsAsciiDigit(src[i])) { i++; col++; }
                if (i < src.Length && src[i] == '.' && i + 1 < src.Length && char.IsAsciiDigit(src[i + 1]))
                {
                    i++; col++;
                    while (i < src.Length && char.IsAsciiDigit(src[i])) { i++; col++; }
                }
                string numStr = src[start..i];
                if (!NumberRegex.IsMatch(numStr))
                    throw new ParseFailure($"Malformed number '{numStr}'.", startLine, startCol);
                list.Add(new Token(TokKind.Number, numStr, startLine, startCol));
                continue;
            }

            // Word / unit / identifier / trait (including 'm/s' and 'e-foil' and 'cm2')
            if (char.IsAsciiLetter(c))
            {
                int start = i;
                while (i < src.Length && (char.IsAsciiLetterOrDigit(src[i]) || src[i] is '_' or '-' or '/'))
                {
                    i++; col++;
                }
                list.Add(new Token(TokKind.Word, src[start..i], startLine, startCol));
                continue;
            }

            throw new ParseFailure($"Unexpected character '{c}'.", startLine, startCol);
        }

        list.Add(new Token(TokKind.Eof, "", line, col));
        return list;
    }

    private sealed class TokenReader(List<Token> tokens)
    {
        private int pos;
        public Token Current => pos < tokens.Count ? tokens[pos] : tokens[^1];
        public bool IsEof => Current.Kind == TokKind.Eof;

        public void Advance()
        {
            if (pos < tokens.Count - 1) pos++;
        }

        public bool MatchSymbol(string sym)
        {
            if (Current.Kind == TokKind.Symbol && Current.Value == sym)
            {
                Advance();
                return true;
            }
            return false;
        }

        public bool IsComparator() => Current.Kind == TokKind.Symbol && Current.Value is "<=" or ">=" or "~" or "=";

        public void ExpectSymbol(string sym)
        {
            if (!MatchSymbol(sym))
                throw Error($"Expected '{sym}', got '{Current.Value}'.", Current);
        }

        public void ExpectWord(string word)
        {
            var tok = Current;
            if (tok.Kind != TokKind.Word || tok.Value != word)
                throw Error($"Expected '{word}', got '{tok.Value}'.", tok);
            Advance();
        }

        public Token ExpectWordToken()
        {
            var tok = Current;
            if (tok.Kind != TokKind.Word)
                throw Error($"Expected identifier, got '{tok.Value}'.", tok);
            Advance();
            return tok;
        }

        public Token ExpectString()
        {
            var tok = Current;
            if (tok.Kind != TokKind.String)
                throw Error($"Expected string literal, got '{tok.Value}'.", tok);
            Advance();
            return tok;
        }

        public Token ExpectNumber()
        {
            var tok = Current;
            if (tok.Kind != TokKind.Number)
                throw Error($"Expected number, got '{tok.Value}'.", tok);
            Advance();
            return tok;
        }

        public Token ExpectTagToken()
        {
            var tok = Current;
            if (tok.Kind != TokKind.Tag)
                throw Error($"Expected @tag, got '{tok.Value}'.", tok);
            Advance();
            return tok;
        }

        public ParseFailure Error(string msg, Token? tok = null)
        {
            var t = tok ?? Current;
            return new ParseFailure(msg, t.Line, t.Column);
        }
    }
}

/// <summary>
/// Canonical deterministic serializer and BLAKE3(JCS) content hasher for <see cref="FoilReqDocument"/>.
/// </summary>
public static class FoilReqSerializer
{
    public static string Format(FoilReqDocument doc)
    {
        ArgumentNullException.ThrowIfNull(doc);
        var sb = new StringBuilder(1024);
        sb.Append("foilreq ").Append(QuoteString(doc.Version)).Append(" {\n");

        sb.Append("  brief {\n");
        sb.Append("    discipline: ").Append(doc.Brief.Discipline).Append(";\n");
        sb.Append("    skill: ").Append(doc.Brief.Skill).Append(";\n");
        sb.Append("  }\n");

        sb.Append("  rider {\n");
        foreach (var field in doc.Rider.Fields)
            sb.Append("    ").Append(field.Key).Append(": ").Append(field.Quantity.Format()).Append(' ').Append(field.Tag.Format()).Append(";\n");
        sb.Append("  }\n");

        sb.Append("  water {\n");
        sb.Append("    kind: ").Append(doc.Water.Kind).Append(' ').Append(doc.Water.KindTag.Format()).Append(";\n");
        sb.Append("    temp: ").Append(doc.Water.Temp.Format()).Append(' ').Append(doc.Water.TempTag.Format()).Append(";\n");
        if (doc.Water.DepthExplicitUnknown)
        {
            var dTag = doc.Water.DepthRefTag ?? new FoilReqTag("@unknown");
            sb.Append("    depth_ref: ? ").Append(dTag.Format()).Append(";\n");
        }
        else if (doc.Water.DepthRef is { } dr)
        {
            var dTag = doc.Water.DepthRefTag ?? new FoilReqTag("@defaulted");
            sb.Append("    depth_ref: ").Append(dr.Format()).Append(' ').Append(dTag.Format()).Append(";\n");
        }
        if (doc.Water.PAtm is { } patm)
        {
            var pTag = doc.Water.PAtmTag ?? new FoilReqTag("@defaulted");
            sb.Append("    p_atm: ").Append(patm.Format()).Append(' ').Append(pTag.Format()).Append(";\n");
        }
        sb.Append("  }\n");

        if (doc.Envelope.Count > 0)
        {
            sb.Append("  envelope {\n");
            foreach (var c in doc.Envelope)
            {
                sb.Append("    ").Append(c.Key).Append(": ").Append(c.Comparator).Append(' ');
                sb.Append(c.IsUnknown || c.Quantity is null ? "?" : c.Quantity.Format());
                if (c.Priority is { } prio) sb.Append(' ').Append(prio.Format());
                sb.Append(' ').Append(c.Tag.Format()).Append(";\n");
            }
            sb.Append("  }\n");
        }

        foreach (var r in doc.References)
        {
            sb.Append("  reference ").Append(QuoteString(r.Name)).Append(" {\n");
            sb.Append("    traits: [").Append(string.Join(", ", r.Traits)).Append("];\n");
            sb.Append("    policy: ").Append(QuoteString(r.Policy)).Append(";\n");
            sb.Append("    confidence: ").Append(r.Confidence).Append(";\n");
            sb.Append("  }\n");
        }

        if (doc.Objectives.Count > 0)
        {
            sb.Append("  objectives {\n");
            foreach (var o in doc.Objectives)
            {
                sb.Append("    ").Append(o.Key).Append(": ");
                if (o.Comparator is not null && o.Quantity is not null)
                    sb.Append(o.Comparator).Append(' ').Append(o.Quantity.Format());
                else if (o.IsUnknown)
                    sb.Append('?');
                else if (o.ValuePriority is not null)
                    sb.Append(o.ValuePriority.Format());
                if (o.Priority is { } prio) sb.Append(' ').Append(prio.Format());
                sb.Append(' ').Append(o.Tag.Format()).Append(";\n");
            }
            sb.Append("  }\n");
        }

        if (doc.Maneuvers.Count > 0)
        {
            sb.Append("  maneuvers {\n");
            foreach (var m in doc.Maneuvers)
            {
                sb.Append("    ").Append(m.Key).Append(": ")
                  .Append(QuoteString(m.Description)).Append(' ')
                  .Append(m.Priority.Format()).Append(' ')
                  .Append(m.Tag.Format()).Append(";\n");
            }
            sb.Append("  }\n");
        }

        if (doc.OpenQuestions.Count > 0)
        {
            sb.Append("  open_questions {\n");
            foreach (var q in doc.OpenQuestions)
            {
                sb.Append("    ").Append(q.Id).Append(": ")
                  .Append(QuoteString(q.Prompt)).Append(' ')
                  .Append(q.State).Append(";\n");
            }
            sb.Append("  }\n");
        }

        sb.Append("}\n");
        return sb.ToString();
    }

    public static string ComputeContentHash(FoilReqDocument doc)
    {
        ArgumentNullException.ThrowIfNull(doc);
        string canonicalDsl = Format(doc);
        var payload = new Dictionary<string, object?>
        {
            ["version"] = doc.Version,
            ["dsl"] = canonicalDsl
        };
        return Identity.Blake3(Encoding.UTF8.GetBytes(Jcs.Write(payload)));
    }

    internal static string QuoteString(string value)
    {
        var sb = new StringBuilder(value.Length + 2);
        sb.Append('"');
        foreach (char c in value)
        {
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': break;
                case '\t': sb.Append("\\t"); break;
                default: sb.Append(c); break;
            }
        }
        sb.Append('"');
        return sb.ToString();
    }
}
