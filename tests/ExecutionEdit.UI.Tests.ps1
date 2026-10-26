. "$PSScriptRoot/ExecutionEdit.TestSupport.ps1"
$path=$null;$form=$null
try {
    $path=New-ExecutionEditFixture;$null=Add-EETestCut $path
    Invoke-EESql "UPDATE B3TinhNangDuLieu SET ChoPhepSua=1 WHERE Ten='EXECUTION_EDIT';"
    $q=New-EEObject 'ExecutionDetailQuery' @([long]1,[long]40,$null,[long]70)
    $snapshot=Invoke-EE 'Load' @($q)
    $draftType=$script:EEAssembly.GetType('DG_TonKhoBTP_v02.UI.NghiepVuKhac.Kho.XuatKho.ExecutionEditDraft',$true)
    $draft=[Activator]::CreateInstance($draftType,[object[]]@($snapshot))
    $m=New-EEObject 'ReelMarkerEdit';$m.ReelId=70;$m.NewEnd=1650;$m.NewDirection=1;$m.Reason='MARKERS';$draft.SetReelEdit($m)
    $m2=New-EEObject 'ReelMarkerEdit';$m2.ReelId=70;$m2.NewEnd=2650;$m2.NewDirection=1;$m2.Reason='MARKERS';$draft.SetReelEdit($m2)
    $request=$draft.BuildRequest([Guid]::NewGuid())
    Assert-EE ($request.ReelEdits.Count -eq 1 -and $request.ReelEdits[0].NewEnd -eq 2650) 'One physical reel has one editable marker draft'
    $formType=$script:EEAssembly.GetType('DG_TonKhoBTP_v02.UI.NghiepVuKhac.Kho.XuatKho.FrmChiTietThucHien',$true)
    $form=[Activator]::CreateInstance($formType,[object[]]@($q,'Cap CV 1x6'))
    $form.CreateControl()
    $grid=$form.Controls.Find('grvChiTietThucHien',$true)[0]
    $person=$form.Controls.Find('cbxNguoiThucHien',$true)[0]
    $time=$form.Controls.Find('txtThoiGianThucHien',$true)[0]
    $save=$form.Controls.Find('btnLuuThayDoi',$true)[0]
    Assert-EE ($grid.Rows.Count -eq 2) 'Form displays all planned segments'
    Assert-EE ($time.ReadOnly) 'Execution timestamp is read-only'
    Assert-EE ($grid.Rows[1].Cells['colTrangThai'].ReadOnly) 'Pending row cannot become a new execution at B2'
    Assert-EE (-not $save.Enabled) 'Save starts disabled without changes'
    Assert-EE ($form.Controls.Find('btnDieuChinh',$true).Count -eq 0) 'No adjustment-mode button or intermediate popup'
    $person.SelectedValue='inactive'
    Assert-EE ($person.SelectedValue -eq 'inactive') 'Performer can be edited immediately without switching mode'
    Assert-EE (-not $save.Enabled) 'Save waits for complete reasons and a current validated impact preview'
    $reason=$form.Controls.Find('cbxLyDoSuaDong',$true)[0]
    $reason.SelectedValue='INFO'
    $deadline=[DateTime]::UtcNow.AddSeconds(6)
    while(-not $save.Enabled -and [DateTime]::UtcNow -lt $deadline) { [Windows.Forms.Application]::DoEvents();Start-Sleep -Milliseconds 30 }
    Assert-EE $save.Enabled 'Valid latest preview enables one-click save'
    $refresh=$form.Controls.Find('btnDoiChieuDuLieuMoi',$true)[0]
    Assert-EE ($null -ne $refresh) 'Stale snapshots have an explicit reload-and-compare action'
    Invoke-EESql "UPDATE LichSuCat SET NguoiThucHien='editor' WHERE KeHoachCatChiTiet_ID=60;"
    $reload=$formType.GetMethod('ReloadPreservingDraft',[Reflection.BindingFlags]'Instance,NonPublic')
    $null=$reload.Invoke($form,@())
    Assert-EE ($person.SelectedValue -eq 'inactive') 'Reloading latest facts preserves the intended performer correction'
    $end=$form.Controls.Find('numSoCuoiHienTai',$true)[0]
    $end.Value=1650
    $start=$form.Controls.Find('txtSoDauHienTai',$true)[0]
    Assert-EE ($start.Text -eq '1000') 'Current start derives from 650m, independent of history'
    Assert-EE ((Get-EEScalar 'SELECT SoCuoi FROM TonCuonLe WHERE id=70') -eq 650) 'Editing fields does not write before Save'
    Write-Output 'PASS: direct edits, read-only time/pending, single reel draft and no implicit writes'
} finally { if($form){$form.Dispose()};Close-ExecutionEditFixture $path }
