namespace RepoBook.Reporting;

public static class MarkdownEscaper
{
    public static string EscapeTableCell(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "n/a";
        }

        return value
            .Replace("|", "\\|", StringComparison.Ordinal)
            .Replace("\r\n", " ", StringComparison.Ordinal)
            .Replace('\n', ' ')
            .Replace('\r', ' ')
            .Trim();
    }

    public static string InlineCode(string value)
    {
        var escaped = EscapeTableCell(value);
        if (string.Equals(escaped, "n/a", StringComparison.Ordinal))
        {
            return "n/a";
        }

        return escaped.Contains('`')
            ? escaped
            : $"`{escaped}`";
    }
}
