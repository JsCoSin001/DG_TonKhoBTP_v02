. "$PSScriptRoot/ExecutionEdit.TestSupport.ps1"
$path=$null;$uc=$null
try {
    $path=New-ExecutionEditFixture
    Invoke-EESql "UPDATE B3TinhNangDuLieu SET ChoPhepSua=1 WHERE Ten='EXECUTION_EDIT';"
    $db=$script:EEAssembly.GetType('DG_TonKhoBTP_v02.Database.Kho.XuatKho.LapKeHoachCatDay_DB',$true)
    $context=$db.GetMethod('LayKeHoachTheoMa').Invoke($null,@('KH-TEST'))
    $type=$script:EEAssembly.GetType('DG_TonKhoBTP_v02.UI.NghiepVuKhac.Kho.XuatKho.UC_LapKeHoachCatDay',$true)
    $uc=[Activator]::CreateInstance($type,$true)
    $flags=[Reflection.BindingFlags]'Instance,NonPublic'
    $type.GetField('_keHoachId',$flags).SetValue($uc,[long]1)
    $type.GetField('_maKeHoachDaNap',$flags).SetValue($uc,'KH-TEST')
    foreach($row in $context.Rows) { $null=$type.GetMethod('ThemHoacCapNhatDongGrid',$flags).Invoke($uc,@($row,$false)) }
    $receiver=$uc.Controls.Find('cbxNguoiNhan',$true)[0];$receiver.Text='original receiver'
    $cutter=$uc.Controls.Find('cbxNguoiCat',$true)[0];$cutter.Text='worker'
    $note=$uc.Controls.Find('tbGhiChu',$true)[0];$note.Text='original note'
    $null=$type.GetMethod('GhiNhanBanGoc',$flags).Invoke($uc,@())
    $rows=$type.GetField('_rows',$flags).GetValue($uc)
    $partial=@($rows.Values | Where-Object { $_.TonCuonLe_ID -eq 70 })[0]
    $whole=@($rows.Values | Where-Object { $_.QuyCachCuonChan_ID -eq 80 -and -not $_.TonCuonLe_ID })[0]
    $partial.ChuoiChieuDaiCat='125;75'
    $receiver.Text='draft receiver';$cutter.Text='inactive';$note.Text='draft note'
    $deletes=$type.GetField('_choXoa',$flags).GetValue($uc);$deletes.Add($whole.RowKey,$whole)
    $null=$rows.Remove($whole.RowKey)
    $grid=$uc.Controls.Find('grvKetQuaTimKiem',$true)[0]
    @($grid.Rows | Where-Object { $_.Tag.RowKey -eq $whole.RowKey }) | ForEach-Object { $grid.Rows.Remove($_) }
    $baseline=$type.GetField('_dongGoc',$flags).GetValue($uc)[$partial.RowKey]
    $oldRevision=Invoke-EE 'ReadPlanRevision' @([long]1)
    $null=Add-EETestCut $path
    $refresh=$type.GetMethod('RefreshExecutionFactsPreservingDraft',$flags)
    Assert-EE ($null -ne $refresh) 'Parent must refresh facts while preserving the unsaved planning draft'
    $result=New-EEObject 'ExecutionEditResult';$result.Success=$true
    $null=$refresh.Invoke($uc,@($result))
    $updated=$rows[$partial.RowKey]
    Assert-EE ($updated.ChuoiChieuDaiCat -eq '125;75') 'Typed cutting lengths survive detail save'
    Assert-EE ($updated.TonThucTe -eq 650 -and $updated.NhomCatDaLuu[0].CoThucHien) 'Current stock and B3 progress refresh'
    Assert-EE ($deletes.ContainsKey($whole.RowKey) -and -not $rows.ContainsKey($whole.RowKey)) 'Pending delete survives without reappearing'
    Assert-EE ($receiver.Text -eq 'draft receiver' -and $cutter.Text -eq 'inactive' -and $note.Text -eq 'draft note') 'Unsaved header fields survive'
    Assert-EE ($type.GetField('_dongGoc',$flags).GetValue($uc)[$partial.RowKey] -eq $baseline) 'Dirty row baseline is not silently reset'
    Assert-EE ($type.GetMethod('CoThayDoiChuaLuu',$flags).Invoke($uc,@())) 'Planning draft stays dirty'
    Assert-EE ($type.GetField('_executionDraftReview',$flags).GetValue($uc).Contains($partial.RowKey)) 'Changed facts require comparing the dirty row'
    $null=$type.GetMethod('ConfirmExecutionDraftReview',$flags).Invoke($uc,@($updated))
    Assert-EE (-not $type.GetField('_executionDraftReview',$flags).GetValue($uc).Contains($partial.RowKey)) 'Explicitly checking a valid draft resolves its review flag'
    Assert-EE ((Get-EEScalar 'SELECT Users_id FROM KeHoach WHERE id=1') -eq 1) 'Execution refresh never changes planned cutter assignment'
    $request=New-EEObject 'LapKeHoachCatDay_BatchSaveRequest'
    $request.KeHoach_IDDuKien=1;$request.MaKeHoach='KH-TEST';$request.NguoiCat='worker';$request.GhiChu='must not write'
    $request.ExpectedExecutionRevision=$oldRevision
    $saved=$db.GetMethod('LuuKeHoachBatch').Invoke($null,@($request))
    Assert-EE (-not $saved.ThanhCong -and $saved.DuLieuDaCu) 'Stale plan version must reject before modifying header or groups'
    Assert-EE ((Get-EEScalar 'SELECT COUNT(*) FROM LichSuCat') -eq 1 -and (Get-EEScalar 'SELECT GhiChu FROM KeHoach WHERE id=1') -ne 'must not write') 'Rejected plan save preserves execution and header'
    $request.ExpectedExecutionRevision=$null
    $missing=$db.GetMethod('LuuKeHoachBatch').Invoke($null,@($request))
    Assert-EE (-not $missing.ThanhCong -and $missing.DuLieuDaCu) 'Existing plans cannot bypass enabled concurrency protection without a version'
    Write-Output 'PASS: parent row/header/delete drafts preserved, progress refreshed and stale plan save rejected'
} finally { if($uc){$uc.Dispose()};Close-ExecutionEditFixture $path }
