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
    $formType=$script:EEAssembly.GetType('DG_TonKhoBTP_v02.UI.NghiepVuKhac.Kho.XuatKho.Frm_ChiTietThucHienKeHoach',$true)
    $form=New-EEForm $q 'Cap CV 1x6'
    $form.CreateControl()
    $grid=$form.Controls.Find('grvDsChiTietCuon',$true)[0]
    $person=$form.Controls.Find('cbxNguoiThucHien',$true)[0]
    $time=$form.Controls.Find('dtNgayThucHien',$true)[0]
    $save=$form.Controls.Find('btnLuuThayDoi',$true)[0]
    Assert-EE ($grid.Rows.Count -eq 2) 'Form displays all planned segments'
    Assert-EE (-not $time.Enabled) 'Execution timestamp is read-only'
    Assert-EE ($grid.Rows[1].Cells['colTrangThai'].ReadOnly) 'Pending row cannot become a new execution at B2'
    Assert-EE (-not $save.Enabled) 'Save starts disabled without changes'
    Assert-EE ($form.Controls.Find('btnDieuChinh',$true).Count -eq 0) 'No adjustment-mode button or intermediate popup'
    $person.SelectedValue='inactive'
    Assert-EE ($person.SelectedValue -eq 'inactive') 'Performer can be edited immediately without switching mode'
    Assert-EE $save.Enabled 'Save can be clicked to display validation errors without a separate impact editor'
    $reason=$form.Controls.Find('rtbLyDoSua',$true)[0]
    $reason.Text='Correct performer'
    $deadline=[DateTime]::UtcNow.AddSeconds(6)
    while(-not $save.Enabled -and [DateTime]::UtcNow -lt $deadline) { [Windows.Forms.Application]::DoEvents();Start-Sleep -Milliseconds 30 }
    Assert-EE $save.Enabled 'Save is available with the required explanation; latest preview is checked at Save'
    $refresh=$form.Controls.Find('btnDoiChieuDuLieuMoi',$true)[0]
    Assert-EE ($null -eq $refresh) 'Reload-and-compare is offered by the conflict prompt rather than a new button'
    Invoke-EESql "UPDATE LichSuCat SET NguoiThucHien='editor' WHERE KeHoachCatChiTiet_ID=60;"
    $reload=$formType.GetMethod('ReloadPreservingDraft',[Reflection.BindingFlags]'Instance,NonPublic')
    $null=$reload.Invoke($form,@())
    Assert-EE ($person.SelectedValue -eq 'inactive') 'Reloading latest facts preserves the intended performer correction'
    $end=$form.Controls.Find('tbxSoCuoi_Sua',$true)[0]
    $end.Text='1650'
    $start=$form.Controls.Find('tbxSoDau_Sua',$true)[0]
    Assert-EE ($start.Text -eq '1000') 'Current start derives from 650m, independent of history'
    Assert-EE ((Get-EEScalar 'SELECT SoCuoi FROM TonCuonLe WHERE id=70') -eq 650) 'Editing fields does not write before Save'
    Write-Output 'PASS: direct edits, read-only time/pending, single reel draft and no implicit writes'
} finally { if($form){$form.Dispose()};Close-ExecutionEditFixture $path }
