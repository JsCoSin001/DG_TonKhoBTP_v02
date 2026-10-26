. "$PSScriptRoot/ExecutionEdit.TestSupport.ps1"
$path=$null;$locker=$null
try {
    $path=New-ExecutionEditFixture
    Invoke-EESql "UPDATE B3TinhNangDuLieu SET ChoPhepSua=1 WHERE Ten='EXECUTION_EDIT';"
    $scan=Invoke-EEB3 $path scan @{raw_qr='LE-001';ke_hoach_id=1}
    Assert-EE $scan.ok 'Bridge must exercise real B3 scanning'
    $null=Add-EETestCut $path
    $q=New-EEObject 'ExecutionDetailQuery' @([long]1,[long]40,$null,[long]70)
    $s=Invoke-EE 'Load' @($q);$r=New-EERequest $s
    $edit=New-EEObject 'ExecutionRowEdit';$edit.OperationToken=$s.Rows[0].OperationToken;$edit.NewPerformerUsername='inactive';$edit.Reason='INFO';$r.RowEdits.Add($edit)
    Assert-EE (Invoke-EE 'Save' @($r)).Success 'B2 corrects performer on a real B3 cut'
    $p=@{raw_qr='LE-001';ke_hoach_id=1;ke_hoach_hang_id=40;group_id=50;detail_id=60;operation_token=$edit.OperationToken}
    $recover=Invoke-EEB3 $path recover $p
    Assert-EE ($recover.ok -and $recover.result.operation_status -eq 'SAVED') 'Original B3 requester can recover after B2 performer correction'
    $version=$recover.result.source_version
    foreach($end in @(1650,650)) {
        $s=Invoke-EE 'Load' @($q);$r=New-EERequest $s;$m=New-EEObject 'ReelMarkerEdit'
        $m.ReelId=70;$m.NewEnd=$end;$m.NewDirection=1;$m.Reason='MARKERS';$r.ReelEdits.Add($m)
        Assert-EE (Invoke-EE 'Save' @($r)).Success 'B2 marker correction preserves length'
    }
    $stale=Invoke-EEB3 $path cut @{raw_qr='LE-001';ke_hoach_id=1;ke_hoach_hang_id=40;group_id=50;detail_id=61;operation_token='22222222-2222-4222-8222-222222222222';source_version=$version;expected_cut_length=100}
    Assert-EE ($stale.error_code -eq 'SOURCE_STATE_CHANGED') 'Returning to the same markers cannot revive an old B3 session'
    $s=Invoke-EE 'Load' @($q);$r=New-EERequest $s;Add-EEUndo $r $edit.OperationToken
    $preview=Invoke-EE 'Preview' @($r)
    Assert-EE ($preview.Valid -and $preview.ImpactByReelId[70].Contains('2')) 'Preview includes both dependent marker corrections'
    Assert-EE (Invoke-EE 'Save' @($r)).Success 'B2 undoes a mistaken cut and dependent markers atomically'
    Assert-EE ((Get-EEScalar 'SELECT SoCuoi FROM TonCuonLe WHERE id=70') -eq 800) 'Undo restores raw before state'
    $cancelled=Invoke-EEB3 $path recover $p
    Assert-EE ($cancelled.error_code -eq 'OPERATION_CANCELLED') 'B3 recovery refuses persisted cancelled token'
    $p.operation_token='33333333-3333-4333-8333-333333333333'
    Assert-EE (Invoke-EEB3 $path cut $p).ok 'A genuinely new B3 operation can execute after undo'
    $s=Invoke-EE 'Load' @($q);$r=New-EERequest $s;$m=New-EEObject 'ReelMarkerEdit';$m.ReelId=70;$m.NewEnd=2650;$m.NewDirection=1;$m.Reason='MARKERS';$r.ReelEdits.Add($m)
    $ini=(Resolve-Path "$PSScriptRoot/../.superpowers/sdd/2026-10-26-b2-b3-execution-edit/php.ini").Path
    $bridge=(Resolve-Path "$PSScriptRoot/fixtures/execution-edit/b3-bridge.php").Path
    $locker=Start-Process -FilePath (Get-Command php).Source -WindowStyle Hidden -PassThru -ArgumentList @('-c',"`"$ini`"","`"$bridge`"",'--db',"`"$path`"",'--action','hold')
    $deadline=[DateTime]::UtcNow.AddSeconds(10)
    while(-not (Test-Path ($path+'.locked')) -and [DateTime]::UtcNow -lt $deadline) { Start-Sleep -Milliseconds 30 }
    Assert-EE (Test-Path ($path+'.locked')) 'Second PHP connection must hold a real SQLite write lock'
    $timer=[Diagnostics.Stopwatch]::StartNew();$saved=Invoke-EE 'Save' @($r);$timer.Stop()
    Assert-EE ($saved.Success -and $timer.ElapsedMilliseconds -gt 1800) 'B2 waits/retries on an independent SQLite lock and saves once'
    $count=Get-EEScalar 'SELECT COUNT(*) FROM B2DieuChinhSuKien'
    Assert-EE (Invoke-EE 'Save' @($r)).Success 'Same ChangeId can recover a committed result'
    Assert-EE ((Get-EEScalar 'SELECT COUNT(*) FROM B2DieuChinhSuKien') -eq $count) 'Retry after lost acknowledgement never applies twice'
    Write-Output 'PASS: real B3 scan/cut/recover, B2 corrections/undo, ABA, new/cancelled tokens, SQLITE_BUSY retry and idempotency'
} finally {
    if($locker){$locker.WaitForExit(10000) | Out-Null;$locker.Dispose()}
    if($path){Remove-Item -LiteralPath ($path+'.locked') -ErrorAction SilentlyContinue}
    Close-ExecutionEditFixture $path
}
