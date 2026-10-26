. "$PSScriptRoot/ExecutionEdit.TestSupport.ps1"
$path=$null
try {
    $path=New-ExecutionEditFixture
    $null=Add-EETestCut $path
    Invoke-EESql "UPDATE B3TinhNangDuLieu SET ChoPhepSua=1 WHERE Ten='EXECUTION_EDIT';"
    $q=New-EEObject 'ExecutionDetailQuery' @([long]1,[long]40,$null,[long]70)
    $s=Invoke-EE 'Load' @($q)
    $m=New-EEObject 'ReelMarkerEdit';$m.ReelId=70;$m.NewEnd=1650;$m.NewDirection=1;$m.Reason='MARKERS'
    $r=New-EERequest $s;$r.ReelEdits.Add($m)
    Assert-EE (Invoke-EE 'Save' @($r)).Success 'Prepare real independent marker correction'
    $s=Invoke-EE 'Load' @($q);$r=New-EERequest $s;Add-EEUndo $r $s.Rows[0].OperationToken
    $version=Get-EEScalar "SELECT GiaTri FROM B3PhienBanDuLieu WHERE Khoa='FACTORY'"
    $preview=Invoke-EE 'Preview' @($r)
    Assert-EE $preview.Valid "Undo preview: $($preview.Message)"
    Assert-EE ($preview.UndoPlan.ReelRestores[0].Start -eq 0 -and $preview.UndoPlan.ReelRestores[0].End -eq 800) 'Undo restores the actual pre-cut coordinate frame'
    Assert-EE ($preview.MarkerEventsToSupersede.Count -eq 1) 'Dependent standalone marker correction is explicitly superseded'
    Assert-EE ((Get-EEScalar 'SELECT COUNT(*) FROM LichSuCat') -eq 1) 'Preview never removes effective history'
    Assert-EE ((Get-EEScalar "SELECT GiaTri FROM B3PhienBanDuLieu WHERE Khoa='FACTORY'") -eq $version) 'Preview never advances a revision'
    $null=Add-EETestCut $path 61 '22222222-2222-4222-8222-222222222222'
    $s=Invoke-EE 'Load' @($q);$r=New-EERequest $s;Add-EEUndo $r $s.Rows[0].OperationToken
    Assert-EE (-not (Invoke-EE 'Preview' @($r)).Valid) 'Cannot undo the first cut alone after a later cut'
    Add-EEUndo $r $s.Rows[1].OperationToken
    $preview=Invoke-EE 'Preview' @($r)
    Assert-EE ($preview.Valid -and $preview.UndoPlan.UndoOperations.Count -eq 2) 'Explicit selected suffix is processed as one undo plan'
    Assert-EE ($preview.UndoPlan.ReelRestores[0].End -eq 800) 'Multiple undo restores the pre-chain snapshot'
    Invoke-EESql "INSERT INTO KeHoach(id,MaKeHoach,NgayKeHoach,Users_id) VALUES(2,'KH-OTHER','2026-10-26',1); INSERT INTO KeHoachHang(id,KeHoach_ID,DanhSachMaSP_ID) VALUES(41,2,1); INSERT INTO KeHoachCatNhom(id,KeHoachHang_ID,TonCuonLe_ID) VALUES(52,41,70); INSERT INTO KeHoachCatChiTiet(id,KeHoachCatNhom_ID,ChieuDaiCanCat) VALUES(64,52,50);"
    $other=Invoke-EEB3 $path cut @{raw_qr='LE-001';ke_hoach_id=2;ke_hoach_hang_id=41;group_id=52;detail_id=64;operation_token='33333333-3333-4333-8333-333333333333'}
    Assert-EE $other.ok 'Prepare another-plan dependent cut'
    $s=Invoke-EE 'Load' @($q);$r=New-EERequest $s;foreach($row in $s.Rows){Add-EEUndo $r $row.OperationToken}
    Assert-EE (-not (Invoke-EE 'Preview' @($r)).Valid) 'A later cut in another plan is never implicitly undone'
    Write-Output 'PASS: undo suffix, original marker frame, explicit marker effects, cross-plan dependencies and read-only preview'
} finally { Close-ExecutionEditFixture $path }
