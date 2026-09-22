using System.Text.RegularExpressions;

namespace GestorAI.API.Services.Conciliacao;

public record ParsedTransaction(
    DateOnly Date,
    decimal Amount,
    string Description,
    string? BankTransactionId);

public class BankStatementParserService
{
    public async Task<List<ParsedTransaction>> ParseOfxAsync(Stream stream)
    {
        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync();
        var results = new List<ParsedTransaction>();

        // Try XML format first (closing </STMTTRN> tags present)
        var xmlBlocks = Regex.Matches(content,
            @"<STMTTRN>(.*?)</STMTTRN>",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);

        if (xmlBlocks.Count > 0)
        {
            foreach (Match block in xmlBlocks)
            {
                var parsed = ParseOfxBlock(block.Groups[1].Value);
                if (parsed != null) results.Add(parsed);
            }
            return results;
        }

        // Fallback: SGML format (no closing tags) — split on <STMTTRN>
        var sgmlBlocks = Regex.Split(content, @"<STMTTRN>", RegexOptions.IgnoreCase);
        foreach (var block in sgmlBlocks.Skip(1))
        {
            var parsed = ParseOfxBlock(block);
            if (parsed != null) results.Add(parsed);
        }

        return results;
    }

    private static ParsedTransaction? ParseOfxBlock(string text)
    {
        var date = ExtractOfxDate(GetTag(text, "DTPOSTED"));
        var amountStr = GetTag(text, "TRNAMT");
        var memo = GetTag(text, "MEMO") ?? GetTag(text, "NAME") ?? "";
        var fitid = GetTag(text, "FITID");

        if (date is null || !decimal.TryParse(amountStr,
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out var amount))
            return null;

        return new ParsedTransaction(date.Value, amount, memo.Trim(), fitid);
    }

    public async Task<List<ParsedTransaction>> ParseCsvAsync(Stream stream)
    {
        using var reader = new StreamReader(stream);
        var results = new List<ParsedTransaction>();
        string? line;
        var isHeader = true;

        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (isHeader) { isHeader = false; continue; }

            var parts = line.Split(',');
            if (parts.Length < 3) continue;

            var dateStr = parts[0].Trim();
            var desc = parts[1].Trim();
            var amountStr = parts[2].Trim();

            if (!TryParseDate(dateStr, out var date)) continue;
            if (!decimal.TryParse(amountStr,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out var amount)) continue;

            results.Add(new ParsedTransaction(date, amount, desc, null));
        }

        return results;
    }

    private static string? GetTag(string text, string tag)
    {
        var m = Regex.Match(text, $@"<{tag}>\s*([^\r\n<]+)", RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value.Trim() : null;
    }

    private static DateOnly? ExtractOfxDate(string? raw)
    {
        if (raw is null) return null;
        var digits = Regex.Match(raw, @"(\d{8})");
        if (!digits.Success) return null;
        var s = digits.Groups[1].Value;
        if (!int.TryParse(s[..4], out var y) ||
            !int.TryParse(s[4..6], out var mo) ||
            !int.TryParse(s[6..8], out var d)) return null;
        return new DateOnly(y, mo, d);
    }

    private static bool TryParseDate(string s, out DateOnly result)
    {
        if (DateOnly.TryParseExact(s, "yyyy-MM-dd",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out result)) return true;
        if (DateOnly.TryParseExact(s, "dd/MM/yyyy",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out result)) return true;
        return false;
    }
}
