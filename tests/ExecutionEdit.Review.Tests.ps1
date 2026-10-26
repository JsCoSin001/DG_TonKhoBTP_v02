param([string]$Case='All',[string]$TestAssemblyPath)
. "$PSScriptRoot/ExecutionEdit.TestSupport.ps1" -TestAssemblyPath $TestAssemblyPath
$flags=[Reflection.BindingFlags]'Instance,NonPublic'
$ucType=$script:EEAssembly.GetType('DG_TonKhoBTP_v02.UI.NghiepVuKhac.Kho.XuatKho.UC_LapKeHoachCatDay',$true)
$db=$script:EEAssembly.GetType('DG_TonKhoBTP_v02.Database.Kho.XuatKho.LapKeHoachCatDay_DB',$true)
function New-ReviewParent {
    $context=$db.GetMethod('LayKeHoachTheoMa').Invoke($null,@('KH-TEST'))
    $uc=[Activator]::CreateInstance($ucType,$true)
    $ucType.GetField('_keHoachId',$flags).SetValue($uc,[long]1)
    $ucType.GetField('_maKeHoachDaNap',$flags).SetValue($uc,'KH-TEST')
    foreach($row in $context.Rows){$null=$ucType.GetMethod('ThemHoacCapNhatDongGrid',$flags).Invoke($uc,@($row,$false))}
    $null=$ucType.GetMethod('GhiNhanBanGoc',$flags).Invoke($uc,@())
    return $uc
}
function Convert-ReviewReel($path) {
    $b3=Invoke-EEB3 $path cut @{raw_qr='cuontp;MB001;1000;REVIEW';ke_hoach_id=1;ke_hoach_hang_id=40;group_id=51;detail_id=62;end_value=1000;direction=1;operation_token='44444444-4444-4444-8444-444444444444'}
    Assert-EE $b3.ok 'Prepare real B3 conversion'
    return [long](Get-EEScalar 'SELECT TonCuonLe_ID FROM KeHoachCatNhom WHERE id=51')
}
$failed=0
foreach($name in @('Conversion','PendingDelete','Orphan','MarkerPreview','PreviewSelection','AuditView','AuditEffects')) {
    if($Case -ne 'All' -and $Case -ne $name){continue}
    $path=$null;$uc=$null;$form=$null
    try {
        $path=New-ExecutionEditFixture
        Invoke-EESql "UPDATE B3TinhNangDuLieu SET ChoPhepSua=1 WHERE Ten='EXECUTION_EDIT';"
        $q=New-EEObject 'ExecutionDetailQuery' @([long]1,[long]40,$null,[long]70)
        if($name -eq 'Conversion') {
            $id=Convert-ReviewReel $path
            $q=New-EEObject 'ExecutionDetailQuery' @([long]1,[long]40,$null,$id)
            $s=Invoke-EE 'Load' @($q);$r=New-EERequest $s;Add-EEUndo $r (@($s.Rows|Where-Object Done)[0].OperationToken)
            Assert-EE (Invoke-EE 'Preview' @($r)).Valid 'Conversion preview valid through generated partial row'
            $saved=Invoke-EE 'Save' @($r)
            Assert-EE ($saved.Success -and $null -eq $saved.NewSnapshot) 'Undo through removed partial scope succeeds and closes the obsolete detail view'
            Assert-EE ((Get-EEScalar "SELECT COUNT(*) FROM TonCuonLe WHERE id=$id") -eq 0) 'Conversion undo commits'
        }
        elseif($name -eq 'PendingDelete') {
            $uc=New-ReviewParent;$rows=$ucType.GetField('_rows',$flags).GetValue($uc)
            $whole=@($rows.Values|Where-Object {$_.QuyCachCuonChan_ID -eq 80})[0]
            $whole.SoLuongCanLay=3;$whole.ChuoiChieuDaiCat='90;80'
            $groups=$whole.NhomCatPopup.Count
            $deletes=$ucType.GetField('_choXoa',$flags).GetValue($uc);$deletes.Add($whole.RowKey,$whole);$null=$rows.Remove($whole.RowKey)
            $null=Add-EETestCut $path
            $result=New-EEObject 'ExecutionEditResult';$result.Success=$true
            $null=$ucType.GetMethod('RefreshExecutionFactsPreservingDraft',$flags).Invoke($uc,@($result))
            $held=$deletes[$whole.RowKey]
            Assert-EE ($held.SoLuongCanLay -eq 3 -and $held.ChuoiChieuDaiCat -eq '90;80' -and $held.NhomCatPopup.Count -eq $groups) 'Pending-delete row retains all editable inputs during facts refresh'
        }
        elseif($name -eq 'Orphan') {
            $id=Convert-ReviewReel $path;$uc=New-ReviewParent;$rows=$ucType.GetField('_rows',$flags).GetValue($uc)
            $orphan=@($rows.Values|Where-Object {$_.TonCuonLe_ID -eq $id})[0];$orphan.ChuoiChieuDaiCat='75'
            $other=$rows.Values|Where-Object {$_.TonCuonLe_ID -eq 70}|Select-Object -First 1;$other.ChuoiChieuDaiCat='33'
            $source=New-EEObject 'ExecutionDetailQuery' @([long]1,[long]40,[long]80,$null)
            $s=Invoke-EE 'Load' @($source);$r=New-EERequest $s;Add-EEUndo $r (@($s.Rows|Where-Object Done)[0].OperationToken)
            $saved=Invoke-EE 'Save' @($r);Assert-EE $saved.Success 'Undo via source specification'
            $null=$ucType.GetMethod('RefreshExecutionFactsPreservingDraft',$flags).Invoke($uc,@($saved))
            Assert-EE (-not $orphan.DaTonTaiTrongDB -and $orphan.CanDelete -and $orphan.ChuoiChieuDaiCat -eq '75') 'Obsolete dirty row stays visible as a discardable local draft'
            $remove=$ucType.GetMethod('RemovePlanningRow',$flags)
            Assert-EE ($null -ne $remove) 'Confirmed delete can discard a local obsolete row'
            $null=$remove.Invoke($uc,@($orphan.PSObject.BaseObject))
            Assert-EE (-not $rows.ContainsKey($orphan.RowKey) -and -not $ucType.GetField('_executionDraftReview',$flags).GetValue($uc).Contains($orphan.RowKey)) 'Discarding obsolete row removes its save blocker'
            Assert-EE ($rows[$other.RowKey].ChuoiChieuDaiCat -eq '33') 'Discard leaves unrelated drafts intact'
        }
        elseif($name -eq 'MarkerPreview' -or $name -eq 'AuditView') {
            $null=Add-EETestCut $path
            Invoke-EESql "INSERT INTO KeHoach(id,MaKeHoach,Users_id) VALUES(2,'KH-OTHER',1);INSERT INTO KeHoachHang(id,KeHoach_ID,DanhSachMaSP_ID) VALUES(41,2,1);INSERT INTO KeHoachCatNhom(id,KeHoachHang_ID,TonCuonLe_ID) VALUES(52,41,70);INSERT INTO KeHoachCatChiTiet(id,KeHoachCatNhom_ID,ChieuDaiCanCat) VALUES(64,52,50);"
            $other=New-EEObject 'ExecutionDetailQuery' @([long]2,[long]41,$null,[long]70)
            $s=Invoke-EE 'Load' @($other);$r=New-EERequest $s;$m=New-EEObject 'ReelMarkerEdit';$m.ReelId=70;$m.NewEnd=1650;$m.NewDirection=1;$m.Reason='MARKERS';$r.ReelEdits.Add($m)
            Assert-EE (Invoke-EE 'Save' @($r)).Success 'Marker correction from other plan'
            $s=Invoke-EE 'Load' @($q);$r=New-EERequest $s;Add-EEUndo $r $s.Rows[0].OperationToken
            if($name -eq 'MarkerPreview') {
                $preview=Invoke-EE 'Preview' @($r);$text=$preview.ImpactByReelId[70]
                Assert-EE ($preview.Valid -and $text.Contains('1650') -and $text.Contains('KH-OTHER') -and $text.Contains('editor') -and $text.Contains('0')) 'Preview lists actual superseded markers, originating plan, editor and time'
            } else {
                Assert-EE (Invoke-EE 'Save' @($r)).Success 'Undo before viewing audit'
                $audit=Invoke-EE 'LoadAudit' @([long]1,[long]70,'')
                Assert-EE (@($audit.Rows|Where-Object {$_.Loai -eq 'HOAN_TAC'}).Count -eq 1) 'Pending reel row still shows cancelled cut audit'
                Assert-EE (@($audit.Rows|Where-Object {$_.Loai -eq 'DOI_MOC'}).Count -eq 1) 'Physical reel audit includes marker correction from another plan'
            }
        }
        elseif($name -eq 'PreviewSelection') {
            $null=Add-EETestCut $path
            $type=$script:EEAssembly.GetType('DG_TonKhoBTP_v02.UI.NghiepVuKhac.Kho.XuatKho.FrmChiTietThucHien',$true)
            $form=[Activator]::CreateInstance($type,[object[]]@($q,'Review'));$form.CreateControl()
            $form.Controls.Find('cbxNguoiThucHien',$true)[0].SelectedValue='inactive'
            $form.Controls.Find('cbxLyDoSuaDong',$true)[0].SelectedValue='INFO'
            $save=$form.Controls.Find('btnLuuThayDoi',$true)[0];$deadline=[DateTime]::UtcNow.AddSeconds(6)
            while(-not $save.Enabled -and [DateTime]::UtcNow -lt $deadline){[Windows.Forms.Application]::DoEvents();Start-Sleep -Milliseconds 30}
            Assert-EE $save.Enabled 'Valid preview ready'
            $impact=$type.GetField('_impact',$flags).GetValue($form);$text=$impact.Text
            $grid=$form.Controls.Find('grvChiTietThucHien',$true)[0];$grid.CurrentCell=$grid.Rows[1].Cells[0]
            Assert-EE ($impact.Text -eq $text) 'Selection changes preserve validated whole-batch impact'
        }
        elseif($name -eq 'AuditEffects') {
            $id=Convert-ReviewReel $path
            Assert-EE ((Get-EEScalar "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='B2DieuChinhKetQua'") -eq 1) 'Immutable final audit must retain revisions and complete entity transitions'
            $source=New-EEObject 'ExecutionDetailQuery' @([long]1,[long]40,[long]80,$null)
            $s=Invoke-EE 'Load' @($source);$r=New-EERequest $s;Add-EEUndo $r (@($s.Rows|Where-Object Done)[0].OperationToken)
            Invoke-EESql "CREATE TRIGGER ReviewFinalAuditFail BEFORE INSERT ON B2DieuChinhKetQua BEGIN SELECT RAISE(ABORT,'final_audit_failure'); END;"
            Assert-EE (-not (Invoke-EE 'Save' @($r)).Success) 'Final audit error rolls back all undo effects'
            Assert-EE ((Get-EEScalar "SELECT COUNT(*) FROM TonCuonLe WHERE id=$id") -eq 1 -and (Get-EEScalar 'SELECT COUNT(*) FROM B2DieuChinhLanLuu') -eq 0) 'No partial undo or audit header survives final audit error'
            Invoke-EESql 'DROP TRIGGER ReviewFinalAuditFail;'
            Assert-EE (Invoke-EE 'Save' @($r)).Success 'Retry succeeds after audit failure is removed'
            $body=(Get-EEScalar 'SELECT NoiDungJson FROM B2DieuChinhKetQua')|ConvertFrom-Json
            Assert-EE ($body.before_versions.FACTORY -eq $s.Versions['FACTORY'] -and $body.after_versions.FACTORY -gt $body.before_versions.FACTORY) 'Audit has readable before/after monotonic revisions'
            $reel=@($body.entity_effects|Where-Object {$_.Table -eq 'TonCuonLe'})[0]
            $group=@($body.entity_effects|Where-Object {$_.Table -eq 'KeHoachCatNhom'})[0]
            $qr=@($body.entity_effects|Where-Object {$_.Table -eq 'B3QrSuDung'})[0]
            Assert-EE ($reel.Before.id -eq $id -and $null -eq $reel.After) 'Final audit records deleted generated reel'
            Assert-EE ($group.Before.TonCuonLe_ID -eq $id -and $null -eq $group.After.TonCuonLe_ID) 'Final audit records group unbinding'
            Assert-EE ($qr.Before.TrangThai -eq 'DA_CHUYEN_LE' -and $null -eq $qr.After) 'Final audit records QR release'
            $immutable=$false;try{Invoke-EESql "UPDATE B2DieuChinhKetQua SET NoiDungJson='{}';"}catch{$immutable=$true};Assert-EE $immutable 'Final audit is immutable'
        }
        Write-Output ('PASS: review '+$name)
    } catch { $failed++;Write-Output ('FAIL: review '+$name+' - '+$_.Exception.Message) }
    finally {if($uc){$uc.Dispose()};if($form){$form.Dispose()};Close-ExecutionEditFixture $path}
}
if($failed){exit 1}
