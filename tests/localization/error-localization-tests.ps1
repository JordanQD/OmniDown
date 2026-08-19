$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$englishPath = Join-Path $repositoryRoot 'OmniDown\Strings\en-US\Resources.resw'
$chinesePath = Join-Path $repositoryRoot 'OmniDown\Strings\zh-Hans\Resources.resw'

[xml]$englishResources = Get-Content -Raw -LiteralPath $englishPath
[xml]$chineseResources = Get-Content -Raw -LiteralPath $chinesePath

function Get-ResourceMap([xml]$document) {
    $map = @{}
    foreach ($entry in $document.root.data) {
        if ($map.ContainsKey($entry.name)) {
            throw "Duplicate resource key: $($entry.name)"
        }

        $map[$entry.name] = [string]$entry.value
    }

    return $map
}

$english = Get-ResourceMap $englishResources
$chinese = Get-ResourceMap $chineseResources

$missingInChinese = @($english.Keys | Where-Object { -not $chinese.ContainsKey($_) })
$missingInEnglish = @($chinese.Keys | Where-Object { -not $english.ContainsKey($_) })
if ($missingInChinese.Count -gt 0 -or $missingInEnglish.Count -gt 0) {
    throw "Resource key mismatch. Missing in zh-Hans: $($missingInChinese -join ', '); missing in en-US: $($missingInEnglish -join ', ')"
}

$requiredKeys = @(
    'Aria2TaskErrorUnknown',
    'TaskStatusErrorWithReason',
    'TaskStatusErrorWithCode',
    'UserErrorCombinedMessage',
    'UserErrorDetailUnknown',
    'StatusToastCopyTechnicalDetailsButtonLabel',
    'TechnicalDetailsLabel'
)

$requiredKeys += 1..32 |
    Where-Object { $_ -ne 31 } |
    ForEach-Object { "Aria2TaskError$_" }

foreach ($key in $requiredKeys) {
    if (-not $english.ContainsKey($key) -or [string]::IsNullOrWhiteSpace($english[$key])) {
        throw "Missing or empty en-US resource: $key"
    }

    if (-not $chinese.ContainsKey($key) -or [string]::IsNullOrWhiteSpace($chinese[$key])) {
        throw "Missing or empty zh-Hans resource: $key"
    }
}

Write-Host "Error localization resources validated: $($english.Count) matching keys."
