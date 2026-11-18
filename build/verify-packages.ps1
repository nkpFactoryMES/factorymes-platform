# 공개 자산의 실제 SHA와 package metadata를 확인한다. 비밀 파일이나 원문 설정을 읽지 않는다.
param(
  [string]$Manifest = ''
)

$ErrorActionPreference = 'Stop'
$platformRoot = Split-Path -Parent $PSScriptRoot
if (-not $Manifest) {
  $Manifest = Join-Path $platformRoot 'artifacts/packages/package-lock.json'
}
$definition = Get-Content -LiteralPath $Manifest -Raw | ConvertFrom-Json
if ($definition.schemaVersion -ne 1 -or $definition.packages.Count -lt 1) {
  throw 'Invalid package manifest.'
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
$checks = 0
foreach ($package in $definition.packages) {
  $path = Join-Path $platformRoot ('artifacts/packages/' + $package.file)
  $actual = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()
  if ($actual -ne $package.sha256) {
    throw "Package byte hash does not match manifest: $($package.id)"
  }
  $checks++
  $archive = [System.IO.Compression.ZipFile]::OpenRead($path)
  try {
    $names = @($archive.Entries | ForEach-Object FullName)
    if ($names -notcontains "lib/net10.0/$($package.id).dll") {
      throw "Expected assembly missing: $($package.id)"
    }
    $checks++
    if ($names | Where-Object {
      $_ -match '(^|/)(\.secrets|\.env|appsettings)' -or $_ -match '\.(pfx|p12|pem|key|pdb|cs)$'
    }) {
      throw "Unexpected source, configuration or secret-shaped asset: $($package.id)"
    }
    $checks++
    $entry = $archive.GetEntry("$($package.id).nuspec")
    $reader = [System.IO.StreamReader]::new($entry.Open())
    try {
      [xml]$metadata = $reader.ReadToEnd()
    }
    finally {
      $reader.Dispose()
    }
    if ($metadata.package.metadata.id -ne $package.id `
      -or $metadata.package.metadata.version -ne $package.version) {
      throw 'Package identity differs from the pinned manifest.'
    }
    $checks++
    if ($metadata.package.metadata.repository.url `
      -ne 'https://github.com/nkpFactoryMES/factorymes-platform') {
      throw 'Unexpected package repository metadata.'
    }
    $checks++
    if ($package.id -eq 'FactoryMES.Foundation') {
      $jwt = $metadata.package.metadata.dependencies.group.dependency
      if ($jwt.id -ne 'Microsoft.AspNetCore.Authentication.JwtBearer' `
        -or $jwt.version -ne '[10.0.15]') {
        throw 'Foundation authentication dependency is not exact.'
      }
      $checks++
      $framework = $metadata.package.metadata.frameworkReferences.group.frameworkReference
      if ($framework.name -ne 'Microsoft.AspNetCore.App') {
        throw 'Foundation framework dependency missing.'
      }
      $checks++
    }
  }
  finally {
    $archive.Dispose()
  }
}
Write-Output "Package metadata and byte checks: $checks PASS"
