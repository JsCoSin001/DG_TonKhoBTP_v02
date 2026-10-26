. "$PSScriptRoot/ExecutionEdit.TestSupport.ps1"
$path=$null
try {
    $path=New-ExecutionEditFixture
    $null=Add-EETestCut $path
    Invoke-EESql "UPDATE B3TinhNangDuLieu SET ChoPhepSua=1 WHERE Ten='EXECUTION_EDIT';"
    $query=New-EEObject 'ExecutionDetailQuery' @([long]1,[long]40,$null,[long]70)
    $snapshot=Invoke-EE 'Load' @($query)
    $request=New-EERequest $snapshot
    $edit=New-EEObject 'ExecutionRowEdit'
    $edit.OperationToken=$snapshot.Rows[0].OperationToken; $edit.NewPerformerUsername='inactive'; $edit.Reason='INFO'
    $request.RowEdits.Add($edit)
    $time=Get-EEScalar 'SELECT ThoiGianThucHien FROM LichSuCat WHERE id=1'
    $result=Invoke-EE 'Save' @($request)
    Assert-EE $result.Success "Performer save: $($result.Message)"
    Assert-EE ((Get-EEScalar 'SELECT NguoiThucHien FROM LichSuCat WHERE id=1') -eq 'inactive') 'Historical performer is corrected'
    Assert-EE ((Get-EEScalar 'SELECT NguoiGuiGoc FROM B3ThaoTac WHERE LichSu_ID=1') -eq 'worker') 'Request origin is immutable'
    Assert-EE ((Get-EEScalar 'SELECT NguoiSua FROM B2DieuChinhLanLuu') -eq 'editor') 'Editor identity is separate and persisted'
    Assert-EE ((Get-EEScalar 'SELECT ThoiGianThucHien FROM LichSuCat WHERE id=1') -eq $time) 'Execution timestamp is unchanged'
    $again=Invoke-EE 'Save' @($request)
    Assert-EE $again.Success 'Retry same change is successful without reapplying'
    Assert-EE ((Get-EEScalar 'SELECT COUNT(*) FROM B2DieuChinhSuKien') -eq 1) 'Retry does not duplicate audit'
    $request.RowEdits[0].NewPerformerUsername='worker'
    Assert-EE (-not (Invoke-EE 'Save' @($request)).Success) 'Same batch ID with altered payload is rejected'
    $snapshot=Invoke-EE 'Load' @($query)
    $stale=New-EERequest $snapshot
    $m=New-EEObject 'ReelMarkerEdit'; $m.ReelId=70; $m.NewEnd=1650; $m.NewDirection=1; $m.Reason='MARKERS'; $stale.ReelEdits.Add($m)
    $result=Invoke-EE 'Save' @($stale)
    Assert-EE $result.Success "Marker save: $($result.Message)"
    Assert-EE ((Get-EEScalar 'SELECT SoDau FROM TonCuonLe WHERE id=70') -eq 1000) 'Start derives from unchanged 650m'
    Assert-EE ((Get-EEScalar 'SELECT SoCuoi-SoDau FROM TonCuonLe WHERE id=70') -eq 650) 'Marker edit cannot alter stock'
    $stale.ChangeId=[Guid]::NewGuid()
    Assert-EE ((Invoke-EE 'Save' @($stale)).Conflict) 'An old source version is rejected'
    $fresh=New-EERequest (Invoke-EE 'Load' @($query))
    $m=New-EEObject 'ReelMarkerEdit'; $m.ReelId=70; $m.NewEnd=0; $m.NewDirection=1; $m.Reason='MARKERS'; $fresh.ReelEdits.Add($m)
    Assert-EE (-not (Invoke-EE 'Save' @($fresh)).Success) 'Negative derived start is rejected'
    Assert-EE ((Get-EEScalar 'SELECT SoCuoi FROM TonCuonLe WHERE id=70') -eq 1650) 'Failed edit preserves current markers'
    $fresh=New-EERequest (Invoke-EE 'Load' @($query))
    $edit=New-EEObject 'ExecutionRowEdit'; $edit.OperationToken=$snapshot.Rows[0].OperationToken; $edit.NewPerformerUsername='worker'; $edit.Reason='INFO'; $fresh.RowEdits.Add($edit)
    Invoke-EESql "CREATE TRIGGER TestAuditFailure BEFORE INSERT ON B2DieuChinhSuKien BEGIN SELECT RAISE(ABORT,'test_audit_failure'); END;"
    Assert-EE (-not (Invoke-EE 'Save' @($fresh)).Success) 'Audit insert failure rejects the transaction'
    Assert-EE ((Get-EEScalar 'SELECT NguoiThucHien FROM LichSuCat WHERE id=1') -eq 'inactive') 'Audit failure rolls performer back'
    Write-Output 'PASS: performer/editor identity, immutable time, marker invariants, idempotency, stale and rollback'
} finally { Close-ExecutionEditFixture $path }
