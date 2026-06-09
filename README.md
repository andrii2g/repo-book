# repo-book

`repo-book` generates a single Markdown report for a local Git repository named `repository-encyclopedia.md`.

## Build

```bash
dotnet build repo-book.slnx
```

## Run

```bash
repo-book .
```

Or:

```bash
dotnet run --project src/RepoBook.Cli -- .
```

## Report Sections

- `## Overview`
- `## Technologies`
- `## Structure`
- `## Largest Modules`
- `## Oldest Files`
- `## Most Active Areas`
- `## Contributor Statistics`
- `## Timeline`
