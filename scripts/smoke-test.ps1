$ErrorActionPreference = "Stop"

$repoBookRoot = Split-Path -Parent $PSScriptRoot
$env:DOTNET_CLI_HOME = Join-Path $repoBookRoot ".dotnet-home"
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"
$env:NUGET_PACKAGES = Join-Path $repoBookRoot ".nuget-packages"

dotnet build (Join-Path $repoBookRoot "repo-book.slnx") --no-restore
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed." }

$tmpDir = Join-Path ([System.IO.Path]::GetTempPath()) ([System.Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $tmpDir | Out-Null
Set-Location $tmpDir

git init | Out-Null
Set-Content -Path (Join-Path $tmpDir "README.md") -Value "# Demo"
New-Item -ItemType Directory -Path (Join-Path $tmpDir "src\App") -Force | Out-Null
Set-Content -Path (Join-Path $tmpDir "src\App\Program.cs") -Value 'Console.WriteLine("Hello");'
git add . | Out-Null
git -c user.name='Smoke Tester' -c user.email='smoke@example.com' commit -m 'Initial commit' | Out-Null

dotnet run --project (Join-Path $repoBookRoot "src\RepoBook.Cli") --no-build -- $tmpDir
if ($LASTEXITCODE -ne 0) { throw "dotnet run failed." }

$reportPath = Join-Path $tmpDir "repository-encyclopedia.md"
if (-not (Test-Path $reportPath)) { throw "Report was not created." }

$reportContent = Get-Content $reportPath -Raw
if (-not $reportContent.Contains("# Repository Encyclopedia")) { throw "Missing header." }
if (-not $reportContent.Contains("## Overview")) { throw "Missing Overview section." }
if (-not $reportContent.Contains("## Technologies")) { throw "Missing Technologies section." }
if (-not $reportContent.Contains("## Timeline")) { throw "Missing Timeline section." }
