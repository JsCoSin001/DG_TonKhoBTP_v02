param([string]$Case='All',[string]$TestAssemblyPath)
. "$PSScriptRoot/ExecutionEdit.TestSupport.ps1" -TestAssemblyPath $TestAssemblyPath
$flags=[Reflection.BindingFlags]'Instance,Public,NonPublic'
$type=$script:EEAssembly.GetType('DG_TonKhoBTP_v02.UI.NghiepVuKhac.Kho.XuatKho.Frm_ChiTietThucHienKeHoach',$true)
Add-Type -ReferencedAssemblies System.Windows.Forms,System,System.Core -TypeDefinition @'
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
public static class EEDesignedFormDriver {
    public static Task<bool> StartSave(Form form) {
        var previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
        try { return (Task<bool>)form.GetType().GetMethod("SaveAsync", BindingFlags.Instance|BindingFlags.NonPublic).Invoke(form, null); }
        finally { SynchronizationContext.SetSynchronizationContext(previous); }
    }
}
'@
function Control($name) { return $form.Controls.Find($name,$true)[0] }
function Invoke-Form($name) { return $type.GetMethod($name,$flags).Invoke($form,@()) }
function Save-Form {
    $task=[EEDesignedFormDriver]::StartSave($form);$deadline=[DateTime]::UtcNow.AddSeconds(12)
    while(-not $task.IsCompleted -and [DateTime]::UtcNow -lt $deadline){[Windows.Forms.Application]::DoEvents();Start-Sleep -Milliseconds 10}
    Assert-EE $task.IsCompleted 'Save must complete without a stuck UI continuation'
    return $task.GetAwaiter().GetResult()
}
$failed=0
foreach($name in @('Binding','Input','ReasonSave','MarkerConfirm','MarkerSave','Conflict','Undo','Conversion','Whole','Closing','Rollback')) {
    if($Case -ne 'All' -and $Case -ne $name){continue}
    $path=$null;$form=$null;$empty=$null
    try {
        $path=New-ExecutionEditFixture;$null=Add-EETestCut $path
        Invoke-EESql "UPDATE B3TinhNangDuLieu SET ChoPhepSua=1 WHERE Ten='EXECUTION_EDIT';"
        $q=New-EEObject 'ExecutionDetailQuery' @([long]1,[long]40,$null,[long]70)
        if($name -eq 'Conversion') {
            $converted=Invoke-EEB3 $path cut @{raw_qr='cuontp;MB001;1000;DESIGNED-CONVERT';ke_hoach_id=1;ke_hoach_hang_id=40;group_id=51;detail_id=62;end_value=1000;direction=1;operation_token='44444444-4444-4444-8444-444444444444'}
            Assert-EE $converted.ok 'Prepare converted reel in real B3'
            $generatedReel=[long](Get-EEScalar 'SELECT TonCuonLe_ID FROM KeHoachCatNhom WHERE id=51')
            $q=New-EEObject 'ExecutionDetailQuery' @([long]1,[long]40,$null,$generatedReel)
        }
        if($name -eq 'Whole') {
            $taken=Invoke-EEB3 $path take @{raw_qr='cuontp;MB001;1000;DESIGNED-TAKE';ke_hoach_id=1;ke_hoach_hang_id=40;quy_cach_cuon_chan_id=80;operation_token='55555555-5555-4555-8555-555555555555'}
            Assert-EE $taken.ok 'Prepare whole reel in real B3'
            $q=New-EEObject 'ExecutionDetailQuery' @([long]1,[long]40,[long]80,$null)
        }
        $constructor=$type.GetConstructor($flags,$null,[type[]]@($q.GetType(),[string]),$null)
        Assert-EE ($null -ne $constructor) 'Designed form must accept the existing execution query and bind actual business data'
        $empty=[Activator]::CreateInstance($type);$captions=@{}
        $pendingControls=New-Object Collections.Queue;$pendingControls.Enqueue($empty)
        while($pendingControls.Count){$parent=$pendingControls.Dequeue();foreach($c in $parent.Controls){if($c -is [Windows.Forms.Label] -and $c.Name -ne 'lblTacDong'){$captions[$c.Name]=$c.Text};$pendingControls.Enqueue($c)}}
        $empty.Dispose();$empty=$null
        $form=$constructor.Invoke(@($q,'Actual product'))
        $form.ShowInTaskbar=$false;$form.StartPosition=[Windows.Forms.FormStartPosition]::Manual
        $form.Location=[Drawing.Point]::new(-30000,-30000);$form.Opacity=0
        $form.Show();[Windows.Forms.Application]::DoEvents()
        $script:DialogAnswer=[Windows.Forms.DialogResult]::Yes
        $script:Dialogs=New-Object Collections.Generic.List[string]
        $script:DialogAction=$null
        $prompt=[Func[string,Windows.Forms.MessageBoxButtons,Windows.Forms.DialogResult]]{
            param($message,$buttons)
            $script:Dialogs.Add($message)
            if($script:DialogAction){& $script:DialogAction}
            return $script:DialogAnswer
        }
        $type.GetProperty('DialogPrompt',$flags).SetValue($form,$prompt)
        $grid=Control 'grvDsChiTietCuon';$person=Control 'cbxNguoiThucHien';$reason=Control 'rtbLyDoSua';$errorLabel=Control 'lblTacDong';$end=Control 'tbxSoCuoi_Sua'
        $executed=@($grid.Rows|Where-Object {$_.Tag.Done})[0];$grid.CurrentCell=$executed.Cells[0]
        if($name -eq 'Binding') {
            Assert-EE ($grid.Rows.Count -eq 2 -and $grid.Columns.Count -eq 6) 'Designer grid shows all execution rows and six existing columns'
            foreach($key in $captions.Keys){Assert-EE ((Control $key).Text -eq $captions[$key]) ('Static label unchanged: '+$key)}
            Assert-EE ($person.SelectedValue -eq 'worker') 'Selected executed row binds performer'
            Assert-EE (-not (Control 'dtNgayThucHien').Enabled) 'Stored date and time cannot be edited'
            Assert-EE ((Control 'tbxQr').ReadOnly -and (Control 'tbxNguonNhap').ReadOnly) 'Source and QR remain read-only'
            Assert-EE ($grid.Rows[1].Cells['colTrangThai'].ReadOnly) 'Pending execution cannot be created at B2'
            Assert-EE (-not (Control 'btnLuuThayDoi').Enabled) 'No changes starts with Save disabled'
            foreach($removed in @('rtbThongTinThucHien','btnBoThayDoi','btnDoiChieuDuLieuMoi','cbxLyDoSuaDong','cbxLyDoSuaMoc')){Assert-EE ($form.Controls.Find($removed,$true).Count -eq 0) ('Removed control must not be created: '+$removed)}
            $reason.Text='Shared explanation';$grid.CurrentCell=$grid.Rows[1].Cells[0]
            Assert-EE ($reason.Text -eq 'Shared explanation' -and (Control 'dtNgayThucHien').CustomFormat -eq "'—'") 'Pending row has no invented timestamp and selection keeps explanation'
            $grid.CurrentCell=$grid.Rows[0].Cells[0]
            Assert-EE ($end.Text -eq '650' -and (Control 'tbxSoDau_SoCuoi_ConLai_DB').Text.Contains('650')) 'Current and historical markers bind independently'
        }
        elseif($name -eq 'Input') {
            $reason.Text='Check actual marks'
            foreach($invalid in @('','abc','-1','2147483648','10')){
                $end.Text=$invalid
                Assert-EE (-not (Save-Form)) ('Invalid marker input refused: '+$invalid)
                Assert-EE ($errorLabel.Text.Length -gt 0 -and $end.Text -eq $invalid) 'Error is visible and invalid text retained'
                Assert-EE ((Get-EEScalar 'SELECT SoCuoi FROM TonCuonLe WHERE id=70') -eq 650) 'Invalid input never writes or reuses an older valid draft'
            }
            $end.Text='1650';Assert-EE ((Control 'tbxSoDau_Sua').Text -eq '1000') 'Start derives from current remaining 650m'
            $grid.CurrentCell=$grid.Rows[1].Cells[0];$grid.CurrentCell=$grid.Rows[0].Cells[0]
            Assert-EE ($end.Text -eq '1650') 'Reel draft survives selecting another segment'
        }
        elseif($name -eq 'ReasonSave') {
            $person.SelectedValue='inactive'
            Assert-EE ((Control 'btnLuuThayDoi').Enabled) 'Save stays available to report incomplete reason'
            Assert-EE (-not (Save-Form)) 'Blank explanation prevents Save'
            Assert-EE ($errorLabel.Text.Length -gt 0) 'Blank reason is explained by dynamic label'
            $reason.Text='Correct performer after checking the record'
            Assert-EE (Save-Form) 'Performer edit saves using existing transactional service'
            Assert-EE ((Get-EEScalar "SELECT NguoiThucHien FROM LichSuCat WHERE KeHoachCatChiTiet_ID=60") -eq 'inactive') 'Selected inactive performer persisted'
            Assert-EE ((Get-EEScalar "SELECT GhiChu FROM B2DieuChinhSuKien WHERE Loai='DOI_NGUOI'") -eq 'Correct performer after checking the record') 'Typed explanation is retained in immutable audit'
            Assert-EE ((Get-EEScalar "SELECT LyDo FROM B2DieuChinhSuKien WHERE Loai='DOI_NGUOI'") -eq 'INFO') 'Internal reason classification remains compatible'
            Assert-EE ((Get-EEScalar 'SELECT NguoiSua FROM B2DieuChinhLanLuu') -eq 'editor') 'Editor is separate from performer'
            Assert-EE ($reason.Text.Length -eq 0 -and -not (Control 'btnLuuThayDoi').Enabled) 'Successful save clears only completed detail draft'
        }
        elseif($name -eq 'MarkerConfirm') {
            $end.Text='1650';$reason.Text='Verify the physical reel';$script:DialogAnswer=[Windows.Forms.DialogResult]::No
            Assert-EE (-not (Save-Form)) 'Rejecting marker impact confirmation cancels write'
            Assert-EE ($script:Dialogs.Count -eq 1 -and $script:Dialogs[0].Contains('LE-001') -and $script:Dialogs[0].Contains('1650') -and $script:Dialogs[0].Contains('650')) 'Confirmation identifies reel, new marks and unchanged length'
            Assert-EE ($reason.Text -eq 'Verify the physical reel' -and $end.Text -eq '1650' -and (Get-EEScalar 'SELECT SoCuoi FROM TonCuonLe WHERE id=70') -eq 650) 'Cancel retains complete draft without writing'
            $script:DialogAnswer=[Windows.Forms.DialogResult]::Yes
            $script:DialogAction={Invoke-EESql "UPDATE B3PhienBanDuLieu SET GiaTri=GiaTri+1 WHERE Khoa='FACTORY';";$script:DialogAction=$null;$script:DialogAnswer=[Windows.Forms.DialogResult]::No}
            Assert-EE (-not (Save-Form)) 'Data changed during confirmation is refused by existing version check'
            Assert-EE ((Get-EEScalar 'SELECT SoCuoi FROM TonCuonLe WHERE id=70') -eq 650) 'Stale confirmation cannot write'
        }
        elseif($name -eq 'Conflict') {
            $person.SelectedValue='inactive';$reason.Text='Retain this reason'
            Invoke-EESql "UPDATE B3PhienBanDuLieu SET GiaTri=GiaTri+1 WHERE Khoa='FACTORY';"
            $script:DialogAnswer=[Windows.Forms.DialogResult]::No
            Assert-EE (-not (Save-Form)) 'Version conflict asks before reloading'
            Assert-EE ($script:Dialogs.Count -eq 1 -and $person.SelectedValue -eq 'inactive' -and $reason.Text -eq 'Retain this reason') 'Declining reload preserves intended performer and reason'
            $script:DialogAnswer=[Windows.Forms.DialogResult]::Yes
            Assert-EE (-not (Save-Form)) 'Reload does not silently resubmit the draft'
            Assert-EE ($person.SelectedValue -eq 'inactive' -and $reason.Text -eq 'Retain this reason') 'Reload preserves the same user draft'
            Assert-EE (Save-Form) 'Explicit next Save persists revalidated draft'
        }
        elseif($name -eq 'MarkerSave') {
            $end.Text='1650';$reason.Text='Real reel marks confirmed'
            $grid.CurrentCell=$grid.Rows[1].Cells[0];$grid.CurrentCell=$grid.Rows[0].Cells[0]
            Assert-EE (Save-Form) 'Confirmed marker correction saves through the new form'
            Assert-EE ((Get-EEScalar 'SELECT SoDau FROM TonCuonLe WHERE id=70') -eq 1000 -and (Get-EEScalar 'SELECT SoCuoi FROM TonCuonLe WHERE id=70') -eq 1650) 'Marker correction preserves 650m of stock'
            Assert-EE ((Get-EEScalar "SELECT COUNT(*) FROM B2DieuChinhSuKien WHERE Loai='DOI_MOC'") -eq 1 -and (Get-EEScalar "SELECT GhiChu FROM B2DieuChinhSuKien WHERE Loai='DOI_MOC'") -eq 'Real reel marks confirmed') 'One physical reel has one audited correction with the typed reason'
        }
        elseif($name -eq 'Undo') {
            $grid.Rows[0].Cells['colTrangThai'].Value='Chưa thực hiện';$reason.Text='Mistaken execution, no physical cut'
            Assert-EE (Save-Form) 'Existing operation can be undone through the new grid'
            Assert-EE ($script:Dialogs.Count -eq 1 -and $script:Dialogs[0].Contains('800')) 'Undo restoration impact is confirmed before writing'
            Assert-EE ((Get-EEScalar 'SELECT COUNT(*) FROM LichSuCat') -eq 0 -and (Get-EEScalar 'SELECT SoCuoi FROM TonCuonLe WHERE id=70') -eq 800) 'Undo restores stock and history atomically'
        }
        elseif($name -eq 'Closing') {
            $person.SelectedValue='inactive';$reason.Text='Keep while deciding';$script:DialogAnswer=[Windows.Forms.DialogResult]::Cancel
            $closeArgs=[Windows.Forms.FormClosingEventArgs]::new([Windows.Forms.CloseReason]::UserClosing,$false)
            $type.GetMethod('ClosingWithDraft',$flags).Invoke($form,[object[]]@($form,$closeArgs))
            Assert-EE ($closeArgs.Cancel -and $reason.Text -eq 'Keep while deciding') 'Cancel closing retains all draft data'
            $script:DialogAnswer=[Windows.Forms.DialogResult]::No;$closeArgs.Cancel=$false
            $type.GetMethod('ClosingWithDraft',$flags).Invoke($form,[object[]]@($form,$closeArgs))
            Assert-EE (-not $closeArgs.Cancel -and (Get-EEScalar 'SELECT NguoiThucHien FROM LichSuCat') -eq 'worker') 'Close without saving replaces discard button and never writes'
        }
        elseif($name -eq 'Conversion') {
            Assert-EE ($end.Enabled -and $end.Text -eq '800') 'Converted physical reel exposes current remaining 800m'
            $executed.Cells['colTrangThai'].Value='Chưa thực hiện';$reason.Text='Conversion recorded by mistake'
            Assert-EE (Save-Form) 'Conversion can be undone from generated partial scope'
            Assert-EE ($form.IsDisposed -and (Get-EEScalar "SELECT COUNT(*) FROM TonCuonLe WHERE id=$generatedReel") -eq 0) 'Removing generated scope closes form after a successful atomic save'
            Assert-EE ((Get-EEScalar 'SELECT COUNT(*) FROM KeHoachCatNhom WHERE id=51 AND TonCuonLe_ID IS NULL') -eq 1) 'Conversion group returns to full reel planning'
        }
        elseif($name -eq 'Whole') {
            Assert-EE (-not $end.Visible -and (Control 'tbxSoDau_SoCuoi_DB').Text -eq ([string][char]0x2014)) 'Whole take has no editable markers or fabricated cut history'
            $executed.Cells['colTrangThai'].Value='Chưa thực hiện';$reason.Text='No actual whole reel was taken'
            Assert-EE (Save-Form) 'Whole take can be undone using existing status edit'
            Assert-EE ((Get-EEScalar 'SELECT COUNT(*) FROM LichSuLayCuon') -eq 0 -and (Get-EEScalar 'SELECT SoCuon FROM TTCuonDay WHERE id=30') -eq 10) 'Whole undo preserves imported reel count and clears effective take'
        }
        elseif($name -eq 'Rollback') {
            $person.SelectedValue='inactive';$reason.Text='Retain on failure'
            Invoke-EESql "CREATE TRIGGER DesignedAuditFail BEFORE INSERT ON B2DieuChinhSuKien BEGIN SELECT RAISE(ABORT,'designed_audit_failure'); END;"
            Assert-EE (-not (Save-Form)) 'Audit failure prevents successful save'
            Assert-EE ($errorLabel.Text.Length -gt 0 -and $person.SelectedValue -eq 'inactive' -and $reason.Text -eq 'Retain on failure') 'Error label and complete unsaved draft survive database failure'
            Assert-EE ((Get-EEScalar 'SELECT NguoiThucHien FROM LichSuCat') -eq 'worker' -and (Get-EEScalar 'SELECT COUNT(*) FROM B2DieuChinhLanLuu') -eq 0) 'Existing service rolls back all effects'
        }
        Write-Output ('PASS: '+$name)
    } catch {$failed++;Write-Output ('FAIL: '+$name+' - '+$_.Exception.Message)}
    finally {if($empty){$empty.Dispose()};if($form){$form.Dispose()};Close-ExecutionEditFixture $path}
}
if($failed){throw "$failed designed-form cases failed"}
