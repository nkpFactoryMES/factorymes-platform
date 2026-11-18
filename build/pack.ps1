# 정확 버전의 두 라이브러리를 pack하고 배포할 실제 바이트의 SHA-256을 기록한다.
param(
  [string]$Version = '',
  [string[]]$PackageId = @(
    'FactoryMES.Contracts',
    'FactoryMES.Foundation'
  )
)

$ErrorActionPreference = 'Stop'
$platformRoot = Split-Path -Parent $PSScriptRoot
$output = Join-Path $platformRoot 'artifacts/packages'
$packageIds = $PackageId

function Confirm-Exit([string]$step) {
  if ($LASTEXITCODE -ne 0) {
    throw "$step failed"
  }
}

if ($Version -and $Version -notmatch '^\d+\.\d+\.\d+$') {
  throw 'Use a stable semantic version such as 1.0.0.'
}

New-Item -ItemType Directory -Force -Path $output | Out-Null
$manifest = @()
foreach ($id in $packageIds) {
  if ($id -notin @('FactoryMES.Contracts', 'FactoryMES.Foundation')) {
    throw 'Unknown platform package.'
  }
  $project = Join-Path $platformRoot "shared/$id/$id.csproj"
  [xml]$metadata = Get-Content -LiteralPath $project -Raw
  $projectVersion = [string]$metadata.Project.PropertyGroup.Version
  if ($Version -and $projectVersion -ne $Version) {
    throw "Project version and requested release differ: $id"
  }

  dotnet restore $project --locked-mode
  Confirm-Exit "Locked restore $id"
  dotnet pack $project -c Release --no-restore -o $output /p:UseAppHost=false
  Confirm-Exit "Pack $id"

  $file = "$id.$projectVersion.nupkg"
  $hash = (Get-FileHash -LiteralPath (Join-Path $output $file) -Algorithm SHA256).Hash
  $manifest += [ordered]@{
    id = $id
    version = $projectVersion
    file = $file
    repo = 'factorymes-platform'
    tag = "v$projectVersion"
    sha256 = $hash.ToLowerInvariant()
  }
}

[ordered]@{
  schemaVersion = 1
  packages = $manifest
} | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $output 'package-lock.json') -Encoding utf8
Write-Output "Packages and SHA-256 manifest: $output"

