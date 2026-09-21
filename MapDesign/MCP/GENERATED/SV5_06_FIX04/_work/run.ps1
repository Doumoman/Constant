param(
    [string]$Filter = 'StarNight.Map.Tests.EditMode.Sv5',
    [string]$Output = 'MapDesign/MCP/GENERATED/SV5_06_FIX04/focused_results.xml'
)
$ErrorActionPreference = 'Stop'
$taskRoot = (Get-Location).Path
$taskOutput = [IO.Path]::GetFullPath((Join-Path $taskRoot $Output))
$taskGenerated = [IO.Path]::GetFullPath((Join-Path $taskRoot 'MapDesign/MCP/GENERATED/SV5_06_FIX04'))
if (-not $taskOutput.StartsWith($taskGenerated + [IO.Path]::DirectorySeparatorChar)) { throw 'Output outside FIX04' }
function Precheck {
    $result = & python -X utf8 MapDesign/MCP/INPUTS/SV5_06_FIX04/PRECHECK.py --mode post-readonly --project-root . --expected-manifest-sha 7ee4edb4e0874c253b1a03384ed3587418c06b7d1ecc35ecd51feda7e5009e03
    if ($LASTEXITCODE -ne 0) { throw ($result -join "`n") }
    $record = ($result -join "`n") | ConvertFrom-Json
    if ($record.checked_worktree_files -ne 299) { throw 'Incomplete ALWAYS check' }
    return $record
}
function Snapshot {
    $snapshot = @{}
    foreach ($path in @(git ls-files --modified --deleted --others --exclude-standard)) {
        if ($path.StartsWith('MapDesign/MCP/GENERATED/SV5_06_FIX04/')) { continue }
        $snapshot[$path] = if (Test-Path -LiteralPath $path -PathType Leaf) {
            (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
        } else { 'MISSING' }
    }
    return $snapshot
}
$beforeLock = Precheck
$before = Snapshot
$start = [DateTime]::UtcNow.ToString('o')
& unity test . --mode EditMode --filter $Filter --output $Output --timeout 1800
$testExit = $LASTEXITCODE
$afterLock = Precheck
$after = Snapshot
$changes = @(@($before.Keys) + @($after.Keys) | Sort-Object -Unique | Where-Object { $before[$_] -ne $after[$_] })
if ($changes.Count -ne 0) { throw ('Files changed during Unity outside FIX04 outputs: ' + ($changes -join ';')) }
$record = [ordered]@{
    command = "unity test . --mode EditMode --filter $Filter --output $Output --timeout 1800"
    started_utc = $start
    finished_utc = [DateTime]::UtcNow.ToString('o')
    exit_code = $testExit
    before_lock = $beforeLock
    after_lock = $afterLock
    outside_output_changes = $changes
    execution_file_hashes = $before
    xml_current_source_match = $false
}
if (Test-Path -LiteralPath $taskOutput -PathType Leaf) {
    $info = Get-Item -LiteralPath $taskOutput
    $xml = [xml][IO.File]::ReadAllText($taskOutput)
    $record.xml_current_source_match = $info.LastWriteTimeUtc -ge [DateTime]::Parse($start).ToUniversalTime()
    $record.xml_sha256 = (Get-FileHash -LiteralPath $taskOutput -Algorithm SHA256).Hash
    $record.xml_bytes = $info.Length
    $record.total = $xml.'test-run'.total
    $record.passed = $xml.'test-run'.passed
    $record.failed = $xml.'test-run'.failed
    $record.skipped = $xml.'test-run'.skipped
}
$json = $record | ConvertTo-Json -Depth 8
[IO.File]::WriteAllText((Join-Path $taskGenerated '_work/run_evidence.json'),$json,[Text.UTF8Encoding]::new($false))
$record | Select-Object command,xml_current_source_match,xml_sha256,xml_bytes,total,passed,failed,skipped | Format-List
exit $testExit
