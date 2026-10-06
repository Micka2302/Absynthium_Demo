#!/usr/bin/env pwsh
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root 'src/Absynthium_Demo.csproj'
$compiledRoot = Join-Path $root 'compiled'
$pluginName = 'Absynthium_Demo'
# A fresh staging directory prevents stale DLLs or payload.json entering the archive.
$stage = Join-Path $root ('.build/' + $pluginName + '-' + [Guid]::NewGuid().ToString('N'))
$pluginTarget = Join-Path $stage "counterstrikesharp/plugins/$pluginName"
New-Item -ItemType Directory -Path $pluginTarget -Force | Out-Null
New-Item -ItemType Directory -Path $compiledRoot -Force | Out-Null

dotnet publish $project -c Release --nologo -o $pluginTarget
if ($LASTEXITCODE -ne 0) { throw 'Publication failed.' }
if (-not (Test-Path -LiteralPath (Join-Path $pluginTarget "$pluginName.dll"))) {
    throw 'Expected Absynthium_Demo.dll was not produced.'
}

# CounterStrikeSharp is supplied by the server, not bundled with this plugin.
$cssApi = Join-Path $pluginTarget 'CounterStrikeSharp.API.dll'
if (Test-Path -LiteralPath $cssApi) { Remove-Item -LiteralPath $cssApi -Force }
Copy-Item -LiteralPath (Join-Path $root 'examples') -Destination $stage -Recurse
Copy-Item -LiteralPath (Join-Path $root 'MIGRATION.md') -Destination $stage
Copy-Item -LiteralPath (Join-Path $root 'migrate-config.ps1') -Destination $stage
Copy-Item -LiteralPath (Join-Path $root 'LICENSE.md') -Destination $stage

$zipPath = Join-Path $compiledRoot "$pluginName.zip"
Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $zipPath -Force
Write-Host "[OK] Plugin: $pluginTarget"
Write-Host "[OK] Archive: $zipPath"
