# 명시한 태그의 새 Release만 만든다. 기존 버전 자산을 덮어쓰지 않는다.
param(
  [Parameter(Mandatory = $true)]
  [string]$Version,
  [ValidateSet(
    'All',
    'FactoryMES.Contracts',
    'FactoryMES.Foundation'
  )]
  [string]$Package = 'All'
)

$ErrorActionPreference = 'Stop'
$platformRoot = Split-Path -Parent $PSScriptRoot
if ($Version -notmatch '^\d+\.\d+\.\d+$') {
  throw 'Use a stable semantic version.'
}
$tag = "v$Version"
$repository = 'nkpFactoryMES/factorymes-platform'

# 기존 릴리스면 실패시키며 --clobber 또는 release 삭제를 사용하지 않는다.
$null = gh release view $tag --repo $repository --json tagName 2>$null
if ($LASTEXITCODE -eq 0) {
  throw 'This version already exists. Publish a new version instead of replacing its assets.'
}
if ($Package -eq 'All') {
  & (Join-Path $PSScriptRoot 'pack.ps1') -Version $Version
}
else {
  & (Join-Path $PSScriptRoot 'pack.ps1') -Version $Version -PackageId $Package
}
& (Join-Path $PSScriptRoot 'verify-packages.ps1')
$manifest = Get-Content (Join-Path $platformRoot 'artifacts/packages/package-lock.json') -Raw `
  | ConvertFrom-Json
$files = @($manifest.packages | ForEach-Object {
  Join-Path $platformRoot ('artifacts/packages/' + $_.file)
})
$files += Join-Path $platformRoot 'artifacts/packages/package-lock.json'
$source = git -C $platformRoot rev-parse HEAD
if ($LASTEXITCODE -ne 0) {
  throw 'Cannot identify source commit.'
}
gh release create $tag @files --repo $repository --verify-tag --target $source `
  --title "FactoryMES platform $Version" `
  --notes 'Versioned shared libraries. Consumers upgrade explicitly and verify asset SHA-256.'
if ($LASTEXITCODE -ne 0) {
  throw 'Release publication failed.'
}

