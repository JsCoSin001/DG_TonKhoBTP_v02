. "$PSScriptRoot/ExecutionEdit.TestSupport.ps1"
$path=$null
try {
    $path=New-ExecutionEditFixture;$null=Add-EETestCut $path
    Invoke-EESql @"
UPDATE B3TinhNangDuLieu SET ChoPhepSua=1 WHERE Ten='EXECUTION_EDIT';
INSERT INTO KeHoach(id,MaKeHoach,Users_id) VALUES(2,'KH-OTHER',1);
INSERT INTO KeHoachHang(id,KeHoach_ID,DanhSachMaSP_ID) VALUES(41,2,1);
INSERT INTO KeHoachCatNhom(id,KeHoachHang_ID,TonCuonLe_ID) VALUES(52,41,70);
INSERT INTO KeHoachCatChiTiet(id,KeHoachCatNhom_ID,ChieuDaiCanCat) VALUES(64,52,50);
"@
    $other=New-EEObject 'ExecutionDetailQuery' @([long]2,[long]41,$null,[long]70)
    $s=Invoke-EE 'Load' @($other);$r=New-EERequest $s;$m=New-EEObject 'ReelMarkerEdit'
    $m.ReelId=70;$m.NewEnd=1650;$m.NewDirection=1;$m.Reason='MARKERS';$r.ReelEdits.Add($m)
    Assert-EE (Invoke-EE 'Save' @($r)).Success 'Independent marker edit can originate in another plan sharing the physical reel'
    $q=New-EEObject 'ExecutionDetailQuery' @([long]1,[long]40,$null,[long]70)
    $s=Invoke-EE 'Load' @($q);$r=New-EERequest $s;Add-EEUndo $r $s.Rows[0].OperationToken
    $preview=Invoke-EE 'Preview' @($r)
    Assert-EE ($preview.Valid -and $preview.MarkerEventsToSupersede.Count -eq 1) 'Cross-plan dependent marker correction is included in preview'
    Assert-EE (Invoke-EE 'Save' @($r)).Success 'Undo invalidates dependent marker correction from the other plan'
    Assert-EE ((Get-EEScalar 'SELECT SoDau FROM TonCuonLe WHERE id=70') -eq 0 -and (Get-EEScalar 'SELECT SoCuoi FROM TonCuonLe WHERE id=70') -eq 800) 'Raw original state restored despite cross-plan marker edit'
    Assert-EE ((Get-EEScalar 'SELECT COUNT(*) FROM KeHoachCatChiTiet WHERE id=64') -eq 1) 'Independent other-plan reservation stays intact'
    Assert-EE ((Get-EEScalar "SELECT COUNT(*) FROM B2DieuChinhSuKien WHERE Loai='DOI_MOC'") -eq 1 -and (Get-EEScalar "SELECT COUNT(*) FROM B2DieuChinhSuKien WHERE Loai='HET_HIEU_LUC_MOC'") -eq 1) 'Both original correction and supersession remain audited'
    Close-ExecutionEditFixture $path;$path=New-ExecutionEditFixture
    Invoke-EESql "UPDATE B3TinhNangDuLieu SET ChoPhepSua=1 WHERE Ten='EXECUTION_EDIT';UPDATE KeHoachCatChiTiet SET ChieuDaiCanCat=800 WHERE id=60;DELETE FROM KeHoachCatChiTiet WHERE id=61;"
    $null=Add-EETestCut $path
    $s=Invoke-EE 'Load' @($q)
    Assert-EE ($s.EditingEnabled -and $s.Rows[0].Done -and $s.Reels[70].Status -eq 'HET') 'Completed execution on ACTIVE plan remains editable'
    $r=New-EERequest $s;$m=New-EEObject 'ReelMarkerEdit';$m.ReelId=70;$m.NewEnd=1000;$m.NewDirection=1;$m.Reason='MARKERS';$r.ReelEdits.Add($m)
    Assert-EE (-not (Invoke-EE 'Save' @($r)).Success) 'HET reel cannot gain length by marker correction'
    $r=New-EERequest $s;$e=New-EEObject 'ExecutionRowEdit';$e.OperationToken=$s.Rows[0].OperationToken;$e.NewPerformerUsername='inactive';$e.Reason='INFO';$r.RowEdits.Add($e)
    Assert-EE (Invoke-EE 'Save' @($r)).Success 'Completed ACTIVE execution still permits performer correction'
    Invoke-EESql "UPDATE KeHoach SET TrangThai='HUY' WHERE id=1;"
    $s=Invoke-EE 'Load' @($q);$r=New-EERequest $s;Add-EEUndo $r $s.Rows[0].OperationToken
    Assert-EE (-not $s.EditingEnabled -and -not (Invoke-EE 'Save' @($r)).Success) 'HUY remains read-only at database boundary'
    Write-Output 'PASS: cross-plan marker supersession preserves independent reservations; ACTIVE completed edits and HUY/HET guards'
} finally { Close-ExecutionEditFixture $path }
