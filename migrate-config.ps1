param(
    [Parameter(Mandatory = $true)][string]$SourceConfig,
    [Parameter(Mandatory = $true)][string]$DestinationConfig,
    [string]$LegacyPayload
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$source = Get-Content -LiteralPath $SourceConfig -Raw -Encoding UTF8 | ConvertFrom-Json
$config = Get-Content -LiteralPath (Join-Path $root 'examples/Absynthium_Demo.json') -Raw -Encoding UTF8 | ConvertFrom-Json

function Merge-Settings($Defaults, $Existing) {
    foreach ($property in $Defaults.PSObject.Properties) {
        $old = $Existing.PSObject.Properties[$property.Name]
        if ($null -eq $old) { continue }
        if ($property.Name -eq 'payload') {
            if ($null -ne $old.Value) { $property.Value = $old.Value }
        } elseif ($property.Value -is [pscustomobject] -and $old.Value -is [pscustomobject]) {
            Merge-Settings $property.Value $old.Value
        } else {
            $property.Value = $old.Value
        }
    }
}

Merge-Settings $config $source
if ($LegacyPayload) {
    $payload = Get-Content -LiteralPath $LegacyPayload -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($payload -isnot [pscustomobject]) { throw 'The legacy payload must be a JSON object.' }
    $config.discord.payload = $payload
}
$config.ConfigVersion = 14
$destination = [IO.Path]::GetFullPath($DestinationConfig)
if (Test-Path -LiteralPath $destination) { throw 'Destination already exists; no files were overwritten.' }
$parent = Split-Path -Parent $destination
New-Item -ItemType Directory -Path $parent -Force | Out-Null
$json = $config | ConvertTo-Json -Depth 100
# CreateNew also prevents overwriting if another process creates it in the meantime.
$stream = [IO.File]::Open($destination, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write)
$writer = New-Object IO.StreamWriter($stream, (New-Object Text.UTF8Encoding($false)))
try { $writer.WriteLine($json) } finally { $writer.Dispose() }
Write-Host "Configuration migrated: $destination"
