Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$fixtureRoot = Join-Path $repoRoot ('.build/migration-test-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $fixtureRoot | Out-Null
$source = Join-Path $fixtureRoot 'legacy.json'
$legacyPayload = Join-Path $fixtureRoot 'legacy-payload.json'
$destination = Join-Path $fixtureRoot 'Absynthium_Demo.json'
$withoutPayload = Join-Path $fixtureRoot 'defaults.json'
$utf8 = New-Object Text.UTF8Encoding($false)
[IO.File]::WriteAllText($source, '{"ConfigVersion":13,"general":{"default-file-name":"retakes4","delete-demo-after-upload":false},"auto-record":{"enabled":true,"min-player-start-record":6,"demo-request":true},"discord":{"webhook-url":"https://example.test/hook"},"demo-request":true}', $utf8)
[IO.File]::WriteAllText($legacyPayload, '{"content":"Custom {map}","embeds":[{"title":"Custom title"}]}', $utf8)
$before = (Get-FileHash -LiteralPath $source).Hash
$migration = Join-Path $repoRoot 'migrate-config.ps1'
& $migration -SourceConfig $source -DestinationConfig $destination -LegacyPayload $legacyPayload
$result = Get-Content -LiteralPath $destination -Raw -Encoding UTF8 | ConvertFrom-Json
if ($result.ConfigVersion -ne 14 -or $result.general.'default-file-name' -ne 'retakes4' -or
    $result.general.'delete-demo-after-upload' -ne $false -or $result.'auto-record'.'min-player-start-record' -ne 6 -or
    $result.discord.payload.content -ne 'Custom {map}' -or $result.discord.'webhook-url' -ne 'https://example.test/hook') {
    throw 'Migration lost settings or custom payload'
}
if ((Get-Content -LiteralPath $destination -Raw).Contains('demo-request')) { throw 'Obsolete option retained' }
if ((Get-FileHash -LiteralPath $source).Hash -ne $before) { throw 'Source was changed' }
& $migration -SourceConfig $source -DestinationConfig $withoutPayload
$result2 = Get-Content -LiteralPath $withoutPayload -Raw -Encoding UTF8 | ConvertFrom-Json
if ($result2.discord.payload.embeds[0].fields.Count -ne 9) { throw 'Default payload missing' }
$beforeDestination = (Get-FileHash -LiteralPath $destination).Hash
$blocked = $false
try { & $migration -SourceConfig $source -DestinationConfig $destination } catch { $blocked = $true }
if (-not $blocked -or (Get-FileHash -LiteralPath $destination).Hash -ne $beforeDestination) {
    throw 'Existing destination was overwritten'
}
$roundTrip = Join-Path $fixtureRoot 'round-trip.json'
& $migration -SourceConfig $destination -DestinationConfig $roundTrip
$result3 = Get-Content -LiteralPath $roundTrip -Raw -Encoding UTF8 | ConvertFrom-Json
if ($result3.discord.payload.content -ne 'Custom {map}') { throw 'Embedded payload lost during migration' }
Write-Host 'PASS: migration preserves settings and custom payload, adds defaults, removes demo-request, leaves sources unchanged and rejects overwrites.'
