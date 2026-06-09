#!/usr/bin/env bash
set -euo pipefail

REPO_BOOK_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
export DOTNET_CLI_HOME="$REPO_BOOK_ROOT/.dotnet-home"
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export NUGET_PACKAGES="$REPO_BOOK_ROOT/.nuget-packages"

dotnet build "$REPO_BOOK_ROOT/repo-book.slnx" --no-restore

TMP_DIR="$(mktemp -d)"
cd "$TMP_DIR"
git init
printf '# Demo\n' > README.md
mkdir -p src/App
printf 'Console.WriteLine("Hello");\n' > src/App/Program.cs
git add .
git -c user.name='Smoke Tester' -c user.email='smoke@example.com' commit -m 'Initial commit'

dotnet run --project "$REPO_BOOK_ROOT/src/RepoBook.Cli" --no-build -- "$TMP_DIR"
test -f "$TMP_DIR/repository-encyclopedia.md"
grep -q '# Repository Encyclopedia' "$TMP_DIR/repository-encyclopedia.md"
grep -q '## Overview' "$TMP_DIR/repository-encyclopedia.md"
grep -q '## Technologies' "$TMP_DIR/repository-encyclopedia.md"
grep -q '## Timeline' "$TMP_DIR/repository-encyclopedia.md"
