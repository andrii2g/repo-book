# repo-book

`repo-book` generates a single Markdown report for a local Git repository named `repository-encyclopedia.md`.

## Build

```bash
dotnet build repo-book.slnx
```

## Run

Analyze local Git repository by path:

```bash
repo-book /path/to/repository
```

With `dotnet run`:

```bash
dotnet run --project src/RepoBook.Cli -- .
```

Or with an explicit repository path:

```bash
dotnet run --project src/RepoBook.Cli -- /path/to/repository
```

The tool always writes `repository-encyclopedia.md` into the analyzed repository root, not into the folder where you launch the command.

## Report Sections

- `## Overview`
- `## Technologies`
- `## Structure`
- `## Largest Modules`
- `## Oldest Files`
- `## Most Active Areas`
- `## Contributor Statistics`
- `## Timeline`
