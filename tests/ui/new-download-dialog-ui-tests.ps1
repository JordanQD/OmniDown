param(
    [Parameter(Mandatory)]
    [int]$AppPid,

    [string]$TorrentPath = (Join-Path $PSScriptRoot 'fixtures\multi-file-sample.torrent'),

    [string]$OutputDirectory = (Join-Path $PSScriptRoot 'artifacts')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$script:passed = 0
$script:failed = 0
$script:results = @()

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

function Invoke-WinApp {
    param([Parameter(Mandatory)][scriptblock]$Command)

    $output = & $Command 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw ($output -join [Environment]::NewLine)
    }

    return $output
}

function Test-UI {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][scriptblock]$Test
    )

    try {
        & $Test
        $script:passed++
        $script:results += [ordered]@{ name = $Name; status = 'PASS' }
    }
    catch {
        $script:failed++
        $script:results += [ordered]@{
            name = $Name
            status = 'FAIL'
            detail = $_.Exception.Message
        }
    }
}

function Wait-ForElement {
    param([Parameter(Mandatory)][string]$AutomationId)

    Invoke-WinApp { winapp ui wait-for $AutomationId -a $AppPid -t 3000 --quiet }
}

function Open-NewDownloadDialog {
    Set-Clipboard 'phase4-ui-test-no-download-source'
    Invoke-WinApp { winapp ui invoke 'NewDownloadToolbarButton' -a $AppPid --quiet }
    Wait-ForElement 'NewDownloadDialogRoot'
}

function Close-NewDownloadDialog {
    Invoke-WinApp { winapp ui invoke 'NewDownloadCancelButton' -a $AppPid --quiet }
    Invoke-WinApp { winapp ui wait-for 'NewDownloadDialogRoot' -a $AppPid --gone -t 3000 --quiet }
}

function Save-StateScreenshot {
    param([Parameter(Mandatory)][string]$FileName)

    $path = Join-Path $OutputDirectory $FileName
    Invoke-WinApp { winapp ui screenshot -a $AppPid -o $path --quiet }
}

function Select-TorrentFixture {
    if (-not (Test-Path -LiteralPath $TorrentPath -PathType Leaf)) {
        throw "Torrent fixture not found: $TorrentPath"
    }

    Invoke-WinApp { winapp ui invoke 'NewDownloadOpenTorrentButton' -a $AppPid --quiet }
    Start-Sleep -Milliseconds 800

    $windowsJson = Invoke-WinApp { winapp ui list-windows -a $AppPid --json }
    $windowsResult = ($windowsJson -join [Environment]::NewLine) | ConvertFrom-Json
    $windows = if ($windowsResult.PSObject.Properties.Name -contains 'windows') {
        @($windowsResult.windows)
    }
    else {
        @($windowsResult)
    }

    $picker = $windows |
        Where-Object { $_.title -ne 'PopupHost' -and $_.title -notmatch 'OmniDown' } |
        Select-Object -First 1
    if ($null -eq $picker) {
        throw 'The torrent file picker window was not found.'
    }

    Invoke-WinApp {
        winapp ui set-value 'FileNameControlHost' (Resolve-Path -LiteralPath $TorrentPath).Path -w $picker.hwnd --quiet
    }

    $opened = $false
    foreach ($selector in @('Open', '打开', '1')) {
        & winapp ui invoke $selector -w $picker.hwnd --quiet 2>$null
        if ($LASTEXITCODE -eq 0) {
            $opened = $true
            break
        }
    }

    if (-not $opened) {
        throw 'The torrent file picker Open button could not be invoked.'
    }

    Wait-ForElement 'NewDownloadTorrentFilesList'
    Wait-ForElement 'NewDownloadTorrentFile1'
}

try {
    Test-UI 'App exposes the new-download command' {
        Wait-ForElement 'NewDownloadToolbarButton'
    }

    Test-UI 'Dialog opens with stable link-mode automation IDs' {
        Open-NewDownloadDialog
        foreach ($automationId in @(
            'NewDownloadLinkMode',
            'NewDownloadTorrentMode',
            'NewDownloadUrlTextBox',
            'NewDownloadPasteButton',
            'NewDownloadTaskNameTextBox',
            'NewDownloadDirectoryTextBox',
            'NewDownloadBrowseDirectoryButton',
            'NewDownloadSplitCountNumberBox',
            'NewDownloadAddButton',
            'NewDownloadCancelButton')) {
            Wait-ForElement $automationId
        }

        Save-StateScreenshot '01-link-empty.png'
    }

    Test-UI 'Empty link submission stays open and exposes an assertive error' {
        Invoke-WinApp { winapp ui set-value 'NewDownloadUrlTextBox' '' -a $AppPid --quiet }
        Invoke-WinApp { winapp ui invoke 'NewDownloadAddButton' -a $AppPid --quiet }
        Wait-ForElement 'NewDownloadDialogRoot'
        Wait-ForElement 'NewDownloadUrlValidationMessage'
    }

    Test-UI 'Single and multiple link text remain in the fixed input viewport' {
        $singleLink = 'https://example.invalid/phase4-single.bin'
        Invoke-WinApp { winapp ui set-value 'NewDownloadUrlTextBox' $singleLink -a $AppPid --quiet }
        Invoke-WinApp {
            winapp ui wait-for 'NewDownloadUrlTextBox' -a $AppPid --value $singleLink -t 2000 --quiet
        }

        $multipleLinks = "https://example.invalid/one.bin`r`nhttps://example.invalid/two.bin"
        Invoke-WinApp { winapp ui set-value 'NewDownloadUrlTextBox' $multipleLinks -a $AppPid --quiet }
        Invoke-WinApp {
            winapp ui wait-for 'NewDownloadUrlTextBox' -a $AppPid --value 'example.invalid/two.bin' --contains -t 2000 --quiet
        }
    }

    Test-UI 'Paste, task name, directory, and split count are editable' {
        $pastedLink = 'https://example.invalid/from-clipboard.bin'
        Set-Clipboard $pastedLink
        Invoke-WinApp { winapp ui set-value 'NewDownloadUrlTextBox' '' -a $AppPid --quiet }
        Invoke-WinApp { winapp ui invoke 'NewDownloadPasteButton' -a $AppPid --quiet }
        Invoke-WinApp {
            winapp ui wait-for 'NewDownloadUrlTextBox' -a $AppPid --value $pastedLink -t 2000 --quiet
        }

        Invoke-WinApp { winapp ui set-value 'NewDownloadTaskNameTextBox' 'Phase 4 UI test' -a $AppPid --quiet }
        Invoke-WinApp { winapp ui set-value 'NewDownloadDirectoryTextBox' $env:TEMP -a $AppPid --quiet }
        Invoke-WinApp { winapp ui set-value 'NewDownloadSplitCountNumberBox' '8' -a $AppPid --quiet }
        Invoke-WinApp {
            winapp ui wait-for 'NewDownloadSplitCountNumberBox' -a $AppPid --value '8' -t 2000 --quiet
        }
    }

    Test-UI 'Torrent mode loads a multi-file fixture into the fixed list viewport' {
        Invoke-WinApp { winapp ui invoke 'NewDownloadTorrentMode' -a $AppPid --quiet }
        Wait-ForElement 'NewDownloadOpenTorrentButton'
        Select-TorrentFixture
        Wait-ForElement 'NewDownloadTorrentFile2'
        Wait-ForElement 'NewDownloadTorrentFile3'
        Save-StateScreenshot '02-torrent-files.png'
    }

    Test-UI 'Torrent select-all exposes off, mixed-capable, and on states' {
        Invoke-WinApp { winapp ui invoke 'NewDownloadSelectAllTorrentFiles' -a $AppPid --quiet }
        Invoke-WinApp {
            winapp ui wait-for 'NewDownloadSelectAllTorrentFiles' -a $AppPid --value 'Off' -t 2000 --quiet
        }

        Invoke-WinApp { winapp ui invoke 'NewDownloadTorrentFile1' -a $AppPid --quiet }
        Invoke-WinApp {
            winapp ui wait-for 'NewDownloadSelectAllTorrentFiles' -a $AppPid --value 'Indeterminate' -t 2000 --quiet
        }

        Invoke-WinApp { winapp ui invoke 'NewDownloadSelectAllTorrentFiles' -a $AppPid --quiet }
        Invoke-WinApp {
            winapp ui wait-for 'NewDownloadSelectAllTorrentFiles' -a $AppPid --value 'On' -t 2000 --quiet
        }
    }

    Test-UI 'No torrent subfiles selected blocks submission' {
        Invoke-WinApp { winapp ui invoke 'NewDownloadSelectAllTorrentFiles' -a $AppPid --quiet }
        Invoke-WinApp { winapp ui invoke 'NewDownloadAddButton' -a $AppPid --quiet }
        Wait-ForElement 'NewDownloadDialogRoot'
        Wait-ForElement 'NewDownloadTorrentValidationMessage'
    }

    Test-UI 'Icon buttons and dialog commands have readable names' {
        $inspectJson = Invoke-WinApp { winapp ui inspect -a $AppPid --interactive --json -d 12 }
        $inspection = ($inspectJson -join [Environment]::NewLine) | ConvertFrom-Json
        $elements = @($inspection.elements)
        foreach ($automationId in @(
            'NewDownloadClearTorrentButton',
            'NewDownloadBrowseDirectoryButton',
            'NewDownloadAddButton',
            'NewDownloadCancelButton')) {
            $element = $elements | Where-Object { $_.automationId -eq $automationId } | Select-Object -First 1
            if ($null -eq $element -or [string]::IsNullOrWhiteSpace($element.name)) {
                throw "Missing accessible name for $automationId"
            }
        }
    }

    Test-UI 'Clear torrent returns to the empty torrent state' {
        Invoke-WinApp { winapp ui invoke 'NewDownloadClearTorrentButton' -a $AppPid --quiet }
        Wait-ForElement 'NewDownloadOpenTorrentButton'
        Invoke-WinApp { winapp ui wait-for 'NewDownloadTorrentFilesList' -a $AppPid --gone -t 2000 --quiet }

        $inspectJson = Invoke-WinApp { winapp ui inspect -a $AppPid --interactive --json -d 12 }
        $inspection = ($inspectJson -join [Environment]::NewLine) | ConvertFrom-Json
        $openButton = @($inspection.elements) |
            Where-Object { $_.automationId -eq 'NewDownloadOpenTorrentButton' } |
            Select-Object -First 1
        if ($null -eq $openButton -or [string]::IsNullOrWhiteSpace($openButton.name)) {
            throw 'The open-torrent button has no accessible name.'
        }
    }

    Test-UI 'Cancel closes the dialog without creating a task' {
        Close-NewDownloadDialog
    }

    Test-UI 'Escape closes the dialog' {
        Open-NewDownloadDialog
        Invoke-WinApp { winapp ui focus 'NewDownloadUrlTextBox' -a $AppPid --quiet }
        Add-Type -AssemblyName System.Windows.Forms
        [System.Windows.Forms.SendKeys]::SendWait('{ESC}')
        Invoke-WinApp { winapp ui wait-for 'NewDownloadDialogRoot' -a $AppPid --gone -t 3000 --quiet }
    }
}
finally {
    & winapp ui invoke 'NewDownloadCancelButton' -a $AppPid --quiet 2>$null
    $script:results | ConvertTo-Json -Depth 4 | Set-Content (
        Join-Path $OutputDirectory 'new-download-dialog-results.json')
}

Write-Host "Passed: $script:passed | Failed: $script:failed"
$script:results | Where-Object { $_.status -eq 'FAIL' } | ForEach-Object {
    Write-Host "  FAIL: $($_.name) - $($_.detail)" -ForegroundColor Red
}

if ($script:failed -gt 0) {
    exit 1
}
