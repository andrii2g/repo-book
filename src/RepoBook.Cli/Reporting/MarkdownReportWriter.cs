using RepoBook.Model;
using System.Globalization;
using System.Text;

namespace RepoBook.Reporting;

public sealed class MarkdownReportWriter
{
    public string Write(RepositoryReport report)
    {
        var builder = new StringBuilder();

        builder.AppendLine("# Repository Encyclopedia");
        builder.AppendLine();
        builder.AppendLine($"Generated: {report.GeneratedAtUtc.UtcDateTime.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)} UTC  ");
        builder.AppendLine($"Repository: {MarkdownEscaper.InlineCode(report.RepositoryName)}  ");
        builder.AppendLine($"Root: {MarkdownEscaper.InlineCode(report.RepositoryRoot)}");
        builder.AppendLine();

        WriteOverview(builder, report);
        WriteTechnologies(builder, report);
        WriteStructure(builder, report);
        WriteLargestModules(builder, report);
        WriteOldestFiles(builder, report);
        WriteMostActiveAreas(builder, report);
        WriteContributorStatistics(builder, report);
        WriteTimeline(builder, report);

        return builder.ToString();
    }

    private static void WriteOverview(StringBuilder builder, RepositoryReport report)
    {
        builder.AppendLine("## Overview");
        builder.AppendLine();
        builder.AppendLine("| Metric | Value |");
        builder.AppendLine("|---|---:|");
        builder.AppendLine($"| Repository | {MarkdownEscaper.InlineCode(report.RepositoryName)} |");
        builder.AppendLine($"| First commit | {FormatDate(report.FirstCommitDate)} |");
        builder.AppendLine($"| Last commit | {FormatDate(report.LastCommitDate)} |");
        builder.AppendLine($"| Commits | {FormatNumber(report.CommitCount)} |");
        builder.AppendLine($"| Contributors | {FormatNumber(report.ContributorCount)} |");
        builder.AppendLine($"| Files | {FormatNumber(report.FileCount)} |");
        builder.AppendLine($"| Directories | {FormatNumber(report.DirectoryCount)} |");
        builder.AppendLine($"| Lines of code | {FormatNumber(report.TotalLines)} |");
        builder.AppendLine($"| Repository size | {FormatBytes(report.TotalBytes)} |");
        builder.AppendLine($"| Primary language | {MarkdownEscaper.EscapeTableCell(report.PrimaryLanguage)} |");
        builder.AppendLine();
    }

    private static void WriteTechnologies(StringBuilder builder, RepositoryReport report)
    {
        builder.AppendLine("## Technologies");
        builder.AppendLine();

        if (report.Technologies.Count == 0)
        {
            builder.AppendLine("No clear technologies detected.");
            builder.AppendLine();
            return;
        }

        builder.AppendLine("| Technology | Evidence |");
        builder.AppendLine("|---|---|");
        foreach (var technology in report.Technologies)
        {
            builder.AppendLine($"| {MarkdownEscaper.EscapeTableCell(technology.Technology)} | {MarkdownEscaper.EscapeTableCell(technology.Evidence)} |");
        }

        builder.AppendLine();
    }

    private static void WriteStructure(StringBuilder builder, RepositoryReport report)
    {
        builder.AppendLine("## Structure");
        builder.AppendLine();
        builder.AppendLine("```text");
        foreach (var line in report.StructureTreeLines)
        {
            builder.AppendLine(line);
        }
        builder.AppendLine("```");
        builder.AppendLine();
        builder.AppendLine("| Top-level area | Files | Lines | Size |");
        builder.AppendLine("|---|---:|---:|---:|");
        foreach (var stat in report.DirectoryStats.OrderBy(stat => SortTopLevelArea(stat.Path), StringComparer.Ordinal))
        {
            builder.AppendLine($"| {MarkdownEscaper.EscapeTableCell(stat.Path)} | {FormatNumber(stat.FileCount)} | {FormatNumber(stat.Lines)} | {FormatBytes(stat.Bytes)} |");
        }

        builder.AppendLine();
    }

    private static void WriteLargestModules(StringBuilder builder, RepositoryReport report)
    {
        builder.AppendLine("## Largest Modules");
        builder.AppendLine();
        builder.AppendLine("| Module | Files | Lines | Size |");
        builder.AppendLine("|---|---:|---:|---:|");
        foreach (var module in report.LargestModules)
        {
            builder.AppendLine($"| {MarkdownEscaper.EscapeTableCell(module.Path)} | {FormatNumber(module.FileCount)} | {FormatNumber(module.Lines)} | {FormatBytes(module.Bytes)} |");
        }

        builder.AppendLine();
    }

    private static void WriteOldestFiles(StringBuilder builder, RepositoryReport report)
    {
        builder.AppendLine("## Oldest Files");
        builder.AppendLine();

        if (report.OldestFiles.Count == 0)
        {
            builder.AppendLine("No file creation history found.");
            builder.AppendLine();
            return;
        }

        builder.AppendLine("| File | First added |");
        builder.AppendLine("|---|---:|");
        foreach (var file in report.OldestFiles)
        {
            builder.AppendLine($"| {MarkdownEscaper.EscapeTableCell(file.RelativePath)} | {FormatDate(file.FirstAddedDate)} |");
        }

        builder.AppendLine();
    }

    private static void WriteMostActiveAreas(StringBuilder builder, RepositoryReport report)
    {
        builder.AppendLine("## Most Active Areas");
        builder.AppendLine();
        builder.AppendLine("| Area | Commit touches |");
        builder.AppendLine("|---|---:|");
        foreach (var area in report.MostActiveAreas)
        {
            builder.AppendLine($"| {MarkdownEscaper.EscapeTableCell(area.Path)} | {FormatNumber(area.CommitTouches)} |");
        }

        builder.AppendLine();
    }

    private static void WriteContributorStatistics(StringBuilder builder, RepositoryReport report)
    {
        builder.AppendLine("## Contributor Statistics");
        builder.AppendLine();

        if (report.Contributors.Count == 0)
        {
            builder.AppendLine("No contributor history found.");
            builder.AppendLine();
            return;
        }

        builder.AppendLine("| Contributor | Commits | Share |");
        builder.AppendLine("|---|---:|---:|");
        foreach (var contributor in report.Contributors.Take(15))
        {
            builder.AppendLine($"| {MarkdownEscaper.EscapeTableCell(FormatContributor(contributor))} | {FormatNumber(contributor.Commits)} | {contributor.CommitSharePercent.ToString("0.0", CultureInfo.InvariantCulture)}% |");
        }

        builder.AppendLine();
        builder.AppendLine($"Top contributor share: {report.Contributors[0].CommitSharePercent.ToString("0.0", CultureInfo.InvariantCulture)}%  ");
        builder.AppendLine($"Bus factor estimate: {CalculateBusFactor(report.Contributors)}");
        builder.AppendLine();
    }

    private static void WriteTimeline(StringBuilder builder, RepositoryReport report)
    {
        builder.AppendLine("## Timeline");
        builder.AppendLine();

        if (report.Timeline.Count == 0)
        {
            builder.AppendLine("No timeline data found.");
            builder.AppendLine();
            return;
        }

        builder.AppendLine("| Year | Commits | Files added |");
        builder.AppendLine("|---|---:|---:|");
        foreach (var item in report.Timeline)
        {
            builder.AppendLine($"| {item.Year.ToString(CultureInfo.InvariantCulture)} | {FormatNumber(item.Commits)} | {FormatNumber(item.FilesAdded)} |");
        }

        builder.AppendLine();
    }

    private static string FormatDate(DateOnly? date)
    {
        return date?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "n/a";
    }

    private static string FormatNumber(long value)
    {
        return value.ToString("N0", CultureInfo.InvariantCulture);
    }

    private static string FormatBytes(long bytes)
    {
        string[] suffixes = ["B", "KB", "MB", "GB"];
        double value = bytes;
        var suffixIndex = 0;

        while (value >= 1024 && suffixIndex < suffixes.Length - 1)
        {
            value /= 1024;
            suffixIndex++;
        }

        return suffixIndex == 0
            ? $"{value.ToString("0", CultureInfo.InvariantCulture)} {suffixes[suffixIndex]}"
            : $"{value.ToString("0.0", CultureInfo.InvariantCulture)} {suffixes[suffixIndex]}";
    }

    private static string FormatContributor(ContributorStat contributor)
    {
        return string.IsNullOrWhiteSpace(contributor.Email)
            ? contributor.Name
            : $"{contributor.Name} <{contributor.Email}>";
    }

    private static int CalculateBusFactor(IReadOnlyList<ContributorStat> contributors)
    {
        double cumulative = 0;
        for (var index = 0; index < contributors.Count; index++)
        {
            cumulative += contributors[index].CommitSharePercent;
            if (cumulative >= 50d)
            {
                return index + 1;
            }
        }

        return contributors.Count;
    }

    private static string SortTopLevelArea(string path)
    {
        return path == "/" ? "\uFFFF" : path;
    }
}
