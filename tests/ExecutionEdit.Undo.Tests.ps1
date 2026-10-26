. "$PSScriptRoot/ExecutionEdit.TestSupport.ps1"
$path=$null
try {
    $path=New-ExecutionEditFixture
    $null=Add-EETestCut $path
    Invoke-EESql "UPDATE B3TinhNangDuLieu SET ChoPhepSua=1 WHERE Ten='EXECUTION_EDIT';"
    $q=New-EEObject 'ExecutionDetailQuery' @([long]1,[long]40,$null,[long]70)
    $s=Invoke-EE 'Load' @($q);$r=New-EERequest $s;Add-EEUndo $r $s.Rows[0].OperationToken
    $result=Invoke-EE 'Save' @($r)
    Assert-EE $result.Success "Partial undo: $($result.Message)"
    Assert-EE ((Get-EEScalar 'SELECT SoCuoi FROM TonCuonLe WHERE id=70') -eq 800) 'Partial restores original 800m'
    Assert-EE ((Get-EEScalar 'SELECT COUNT(*) FROM LichSuCat') -eq 0) 'Cancelled cut no longer counts as executed'
    Assert-EE ((Get-EEScalar "SELECT COUNT(*) FROM B3ThaoTac WHERE TrangThai='HUY'") -eq 1) 'Cancelled token remains persisted'
    Assert-EE ((Get-EEScalar "SELECT COUNT(*) FROM B2DieuChinhSuKien WHERE Loai='HOAN_TAC'") -eq 1) 'Original cut preserved in audit'
    $replay=Invoke-EEB3 $path cut @{raw_qr='LE-001';ke_hoach_id=1;ke_hoach_hang_id=40;group_id=50;detail_id=60;operation_token=$s.Rows[0].OperationToken}
    Assert-EE ($replay.error_code -eq 'OPERATION_CANCELLED') 'Actual B3 rejects cancelled token after B2 undo'
    Close-ExecutionEditFixture $path;$path=New-ExecutionEditFixture
    Invoke-EESql "UPDATE B3TinhNangDuLieu SET ChoPhepSua=1 WHERE Ten='EXECUTION_EDIT';"
    $b3=Invoke-EEB3 $path cut @{raw_qr='cuontp;MB001;1000;CONVERT';ke_hoach_id=1;ke_hoach_hang_id=40;group_id=51;detail_id=62;end_value=1000;direction=1;operation_token='44444444-4444-4444-8444-444444444444'}
    Assert-EE $b3.ok 'Prepare full-to-partial execution'
    $q=New-EEObject 'ExecutionDetailQuery' @([long]1,[long]40,[long]80,$null)
    $s=Invoke-EE 'Load' @($q);$r=New-EERequest $s
    $done=@($s.Rows|Where-Object Done)[0];$reelId=$done.ReelId;Add-EEUndo $r $done.OperationToken
    $result=Invoke-EE 'Save' @($r)
    Assert-EE $result.Success "Conversion undo: $($result.Message)"
    Assert-EE ((Get-EEScalar "SELECT COUNT(*) FROM TonCuonLe WHERE id=$reelId") -eq 0) 'Generated partial is removed only when unreferenced'
    Assert-EE ((Get-EEScalar 'SELECT COUNT(*) FROM KeHoachCatNhom WHERE id=51 AND TonCuonLe_ID IS NULL') -eq 1) 'Group returns to full-reel planning'
    Assert-EE ((Get-EEScalar 'SELECT COUNT(*) FROM B3QrSuDung') -eq 0) 'Converted QR current use released'
    Assert-EE ((Get-EEScalar 'SELECT SoCuon FROM TTCuonDay WHERE id=30') -eq 10) 'Imported reel count must never be incremented'
    Close-ExecutionEditFixture $path;$path=New-ExecutionEditFixture
    Invoke-EESql "UPDATE B3TinhNangDuLieu SET ChoPhepSua=1 WHERE Ten='EXECUTION_EDIT';"
    $b3=Invoke-EEB3 $path take @{raw_qr='cuontp;MB001;1000;TAKE';ke_hoach_id=1;ke_hoach_hang_id=40;quy_cach_cuon_chan_id=80;operation_token='55555555-5555-4555-8555-555555555555'}
    Assert-EE $b3.ok 'Prepare full take'
    $s=Invoke-EE 'Load' @($q);$r=New-EERequest $s;Add-EEUndo $r (@($s.Rows|Where-Object Done)[0].OperationToken)
    Invoke-EESql "CREATE TRIGGER TestUndoAuditFailure BEFORE INSERT ON B2DieuChinhSuKien BEGIN SELECT RAISE(ABORT,'undo_audit_failure'); END;"
    Assert-EE (-not (Invoke-EE 'Save' @($r)).Success) 'Injected undo error must fail the whole batch'
    Assert-EE ((Get-EEScalar 'SELECT COUNT(*) FROM LichSuLayCuon') -eq 1) 'History survives failed transaction'
    Assert-EE ((Get-EEScalar 'SELECT COUNT(*) FROM B3QrSuDung') -eq 1) 'QR survives failed transaction'
    Assert-EE ((Get-EEScalar "SELECT COUNT(*) FROM B3ThaoTac WHERE TrangThai='ACTIVE'") -eq 1) 'Ledger status rolls back'
    Invoke-EESql 'DROP TRIGGER TestUndoAuditFailure;'
    $result=Invoke-EE 'Save' @($r)
    Assert-EE $result.Success "Whole take undo: $($result.Message)"
    Assert-EE ((Get-EEScalar 'SELECT COUNT(*) FROM LichSuLayCuon') -eq 0) 'Take no longer counts as executed'
    Assert-EE ((Get-EEScalar 'SELECT COUNT(*) FROM B3QrSuDung') -eq 0) 'Taken QR current use released'
    Assert-EE ((Get-EEScalar 'SELECT SoCuon FROM TTCuonDay WHERE id=30') -eq 10) 'Source import count remains unchanged'
    Write-Output 'PASS: partial/conversion/whole undo, QR and ledger consistency, audit failure rolls all changes back'
} finally { Close-ExecutionEditFixture $path }
