param(
    [string]$ReadOnlyDatabasePath,
    [string]$OutputDirectory
)
# Full schema and existing data are retained. All writes go to an owned temporary backup.
if(-not $ReadOnlyDatabasePath){Write-Output 'SKIP: supply -ReadOnlyDatabasePath to validate an explicit database backup';exit 0}
. "$PSScriptRoot/ExecutionEdit.TestSupport.ps1" -TestAssemblyPath (Join-Path $PSScriptRoot '../DG_TonKhoBTP_v02/bin/Release/DG_TonKhoBTP_v02.exe')
if(-not $OutputDirectory){$OutputDirectory=Join-Path $PSScriptRoot '../artifacts/execution-edit/database-validation-20261027'}
$path=$null;$source=$null;$target=$null;$parent=$null
$checks=New-Object 'System.Collections.Generic.List[object]'
$null=New-Item -ItemType Directory -Path $OutputDirectory -Force
$beforeHash=(Get-FileHash -LiteralPath $ReadOnlyDatabasePath -Algorithm SHA256).Hash
function Check($condition,[string]$description) {
    $checks.Add([pscustomobject]@{name=$description;passed=[bool]$condition})
    if(-not $condition){throw "ASSERT: $description"}
    Write-Output ('PASS: '+$description)
}
function Login-Editor {
    $login=New-Object DG_TonKhoBTP_v02.Models.LoginResult
    $login.Success=$true;$login.UserId=[int]$script:editor;$login.UserName=$script:editorName;$login.Name='Database validation'
    [DG_TonKhoBTP_v02.Models.UserContext]::Set($login)
}
function Current-FullStock([long]$sourceId) {
    return [long](Get-EEScalar "SELECT c.SoCuon-COALESCE((SELECT SUM(l.SoLuong) FROM LichSuLayCuon l WHERE l.TTCuonDay_ID=c.id),0)-(SELECT COUNT(DISTINCT t.id) FROM TonCuonLe t JOIN LichSuCat l ON l.TonCuonLe_ID=t.id WHERE t.TTCuonDay_ID=c.id AND l.LoaiNguon='CUON_CHAN') FROM TTCuonDay c WHERE c.id=$sourceId")
}
function Cut([long]$detail,[string]$token,[string]$raw=$script:partialCode,[long]$group=$script:groupPartial,[long]$plan=$script:planId,[long]$item=$script:itemId,[hashtable]$extra=@{}) {
    $payload=@{raw_qr=$raw;ke_hoach_id=$plan;ke_hoach_hang_id=$item;group_id=$group;detail_id=$detail;operation_token=$token;actor_user_id=$script:worker}
    foreach($key in $extra.Keys){$payload[$key]=$extra[$key]}
    return Invoke-EEB3 $path cut $payload
}
function Draw-Detail($query,[string]$name,[string]$kind='CUT') {
    $type=$script:EEAssembly.GetType('DG_TonKhoBTP_v02.UI.NghiepVuKhac.Kho.XuatKho.FrmChiTietThucHien',$true)
    $form=[Activator]::CreateInstance($type,[object[]]@($query,'Kiem thu ban sao QLSX_v02'))
    try {
        $form.ShowInTaskbar=$false;$form.StartPosition=[Windows.Forms.FormStartPosition]::Manual;$form.Location=New-Object Drawing.Point(-30000,-30000);$form.Opacity=0
        $form.Show();[Windows.Forms.Application]::DoEvents()
        $grid=$form.Controls.Find('grvChiTietThucHien',$true)[0]
        $row=@($grid.Rows|Where-Object {$_.Tag.Done -and $_.Tag.Kind -eq $kind})[0]
        $grid.CurrentCell=$row.Cells[0]
        Check ($form.Controls.Find('cbxNguoiThucHien',$true)[0].SelectedValue -ne $null) ('Actual UI binds selected '+$name+' execution')
        $bitmap=New-Object Drawing.Bitmap($form.Width,$form.Height)
        try{$form.DrawToBitmap($bitmap,(New-Object Drawing.Rectangle(0,0,$form.Width,$form.Height)));$bitmap.Save((Join-Path $OutputDirectory ($name+'.png')),[Drawing.Imaging.ImageFormat]::Png)}finally{$bitmap.Dispose()}
    } finally {$form.Dispose()}
}
try {
    $path=Join-Path ([IO.Path]::GetTempPath()) ('ExecutionEdit.'+[Guid]::NewGuid().ToString('N')+'.db')
    $source=New-Object System.Data.SQLite.SQLiteConnection("Data Source=$ReadOnlyDatabasePath;Read Only=True;FailIfMissing=True;Version=3;")
    $target=New-Object System.Data.SQLite.SQLiteConnection("Data Source=$path;Version=3;")
    $source.Open();$target.Open();$source.BackupDatabase($target,'main','main',-1,$null,0)
    $target.Dispose();$target=$null;$source.Dispose();$source=$null
    [DG_TonKhoBTP_v02.Database.DB_Base]::SetDatabasePath($path)
    $ext=(Join-Path (Split-Path (Get-Command php).Source) 'ext').Replace('\','/')
    $ini=@(('extension_dir="'+$ext+'"'),'memory_limit=512M','date.timezone=Asia/Ho_Chi_Minh')
    foreach($module in @('openssl','curl','fileinfo','mbstring','pdo_sqlite','sqlite3','zip')){$ini+=('extension='+$module)}
    [IO.File]::WriteAllText(($path+'.php.ini'),($ini -join [Environment]::NewLine),(New-Object Text.UTF8Encoding($false)))
    Invoke-EESql "CREATE TABLE ExecutionEditTestSentinel(Name TEXT PRIMARY KEY);INSERT INTO ExecutionEditTestSentinel VALUES('EXECUTION_EDIT_TEST');"
    Check ((Get-EEScalar 'PRAGMA integrity_check') -eq 'ok') 'Original database backup passes integrity_check'
    $originalCuts=[int](Get-EEScalar 'SELECT COUNT(*) FROM LichSuCat');$originalTakes=[int](Get-EEScalar 'SELECT COUNT(*) FROM LichSuLayCuon')
    Check ($originalCuts -eq 4 -and $originalTakes -eq 3) 'Original four cuts and three takes retained in backup'
    $legacyQuery=New-EEObject 'ExecutionDetailQuery' @([long]1,[long]1,$null,[long]1)
    $missing=$false;try{$null=Invoke-EE 'Load' @($legacyQuery)}catch{$missing=$_.Exception.Message.Contains('B2/B3')}
    Check $missing 'Unmigrated database is refused with a controlled schema message'
    $originalActor=[long](Get-EEScalar 'SELECT Users_id FROM KeHoach WHERE id=1')
    $originalQr=[string](Get-EEScalar 'SELECT MaCuon FROM TonCuonLe WHERE id=1')
    $unmigratedB3=Invoke-EEB3 $path cut @{raw_qr=$originalQr;ke_hoach_id=1;ke_hoach_hang_id=1;group_id=1;detail_id=1;operation_token=([Guid]::NewGuid().ToString());actor_user_id=$originalActor}
    Check ($unmigratedB3.error_code -eq 'DATABASE_SCHEMA_REQUIRED') 'New B3 explicitly refuses execution before shared SQL migration'
    Apply-ExecutionEditMigration $path;Apply-ExecutionEditMigration $path
    Check ((Get-EEScalar "SELECT ChoPhepSua FROM B3TinhNangDuLieu WHERE Ten='EXECUTION_EDIT'") -eq 0) 'Migration reapplies safely and keeps editing disabled'
    $legacy=Invoke-EE 'Load' @($legacyQuery)
    Check (-not $legacy.EditingEnabled -and @($legacy.Rows|Where-Object Done).Count -eq 2) 'Migrated copy displays existing executed partial rows read-only'
    $legacyFullQuery=New-EEObject 'ExecutionDetailQuery' @([long]1,[long]2,[long]1,$null)
    $legacyFull=Invoke-EE 'Load' @($legacyFullQuery)
    Check (@($legacyFull.Rows|Where-Object Done).Count -eq 5 -and @($legacyFull.Rows|Where-Object {$_.Done -and $_.OperationToken.Length -gt 0}).Count -eq 0) 'Original full-reel scope displays three takes and two conversion cuts without inventing ledger identity'
    $legacyFormType=$script:EEAssembly.GetType('DG_TonKhoBTP_v02.UI.NghiepVuKhac.Kho.XuatKho.FrmChiTietThucHien',$true)
    $legacyForm=[Activator]::CreateInstance($legacyFormType,[object[]]@($legacyFullQuery,'Existing QLSX data'))
    try {
        $legacyGrid=$legacyForm.Controls.Find('grvChiTietThucHien',$true)[0]
        Check (-not $legacyForm.Controls.Find('btnLuuThayDoi',$true)[0].Enabled -and @($legacyGrid.Rows|Where-Object {$_.Tag.Done -and -not $_.Cells['colTrangThai'].ReadOnly}).Count -eq 0) 'Actual detail form safely shows existing history with editing disabled'
    } finally {$legacyForm.Dispose()}
    # Reserve new positive IDs rather than replacing any source records.
    $maximum=[long](Get-EEScalar 'SELECT MAX(v) FROM (SELECT MAX(id) v FROM DanhSachMaSP UNION ALL SELECT MAX(id) FROM TTThanhPham UNION ALL SELECT MAX(id) FROM TTCuonDay UNION ALL SELECT MAX(id) FROM KeHoach UNION ALL SELECT MAX(user_id) FROM users)')
    $b=[Math]::Max([long]1000000,$maximum+1000)
    $script:worker=$b+1;$script:editor=$b+2;$inactive=$b+3;$product=$b+1;$tp=$b+10;$inbound=$b+20;$fullSource=$b+30;$partialSource=$b+31
    $script:planId=$b+1;$script:itemId=$b+40;$qc=$b+80;$reel=$b+70;$script:groupPartial=$b+50;$groupFull=$b+51;$detail1=$b+60;$detail2=$b+61;$detail3=$b+62;$detail4=$b+63
    $segment='EE'+[Guid]::NewGuid().ToString('N').Substring(0,10).ToUpperInvariant()
    $script:editorName=$segment+'_editor';$workerName=$segment+'_worker';$inactiveName=$segment+'_inactive';$script:partialCode='LE-'+$segment;$planCode='KH-'+$segment
    Invoke-EESql @"
INSERT INTO users(user_id,username,password_hash,is_active) VALUES($worker,'$workerName','test',1),($editor,'$editorName','test',1),($inactive,'$inactiveName','test',0);
INSERT INTO user_roles(user_id,role_id) SELECT $worker,role_id FROM roles WHERE role_name='Worker_WH';
INSERT INTO DanhSachMaSP(id,Ten,Ma,DonVi,KieuSP) VALUES($product,'EXECUTION EDIT DATABASE VALIDATION','$segment','m','TP');
INSERT INTO TTThanhPham(id,DanhSachSP_ID,MaBin,NhapKho,Temp) VALUES($tp,$product,'E7-$segment/4-01-00',1,0);
INSERT INTO TTNhapKhoTP(id,TTThanhPham_ID,NgayNhapKho,TenSP,TongChieuDai,GhiChu,NguoiLam) VALUES($inbound,$tp,'2026-10-27','EXECUTION EDIT DATABASE VALIDATION',10800,'Temporary test data','$editorName');
INSERT INTO TTCuonDay(id,ThongTinNhapKho_ID,SoCuon,ChieuDai_1cuon) VALUES($fullSource,$inbound,10,1000);
INSERT INTO TTCuonDay(id,ThongTinNhapKho_ID,SoCuon,ChieuDai_1cuon,SoDau,SoCuoi) VALUES($partialSource,$inbound,1,800,0,800);
INSERT INTO KeHoach(id,MaKeHoach,NgayKeHoach,Users_id) VALUES($planId,'$planCode','2026-10-27',$worker);
INSERT INTO KeHoachHang(id,KeHoach_ID,DanhSachMaSP_ID,SoLuongCuonCanLay) VALUES($itemId,$planId,$product,2);
INSERT INTO KeHoachHangQuyCachCuonChan(id,KeHoachHang_ID,ChieuDai1Cuon,SoLuongCuonCanLay) VALUES($qc,$itemId,1000,2);
INSERT INTO TonCuonLe(id,MaCuon,TTThanhPham_ID,TTCuonDay_ID,SoDau,SoCuoi) VALUES($reel,'$partialCode',$tp,$partialSource,0,800);
INSERT INTO KeHoachCatNhom(id,KeHoachHang_ID,TonCuonLe_ID,QuyCachCuonChan_ID) VALUES($groupPartial,$itemId,$reel,NULL),($groupFull,$itemId,NULL,$qc);
INSERT INTO KeHoachCatChiTiet(id,KeHoachCatNhom_ID,ChieuDaiCanCat) VALUES($detail1,$groupPartial,150),($detail2,$groupPartial,100),($detail3,$groupFull,200),($detail4,$groupFull,150);
"@
    Login-Editor
    $token1=[Guid]::NewGuid().ToString();$token2=[Guid]::NewGuid().ToString()
    $cut=Cut $detail1 $token1
    Check $cut.ok 'B3 cuts existing partial with edit feature still disabled'
    $q=New-EEObject 'ExecutionDetailQuery' @($planId,$itemId,$null,$reel)
    $s=Invoke-EE 'Load' @($q)
    Check (-not $s.EditingEnabled -and $s.Reels[$reel].Length -eq 650) 'B2 reads actual B3 cut 800 minus 150 equals 650'
    Invoke-EESql "UPDATE B3TinhNangDuLieu SET ChoPhepSua=1 WHERE Ten='EXECUTION_EDIT';"
    $legacy=Invoke-EE 'Load' @($legacyQuery);$r=New-EERequest $legacy;$e=New-EEObject 'ExecutionRowEdit';$e.OperationToken='';$e.NewPerformerUsername=$inactiveName;$e.Reason='INFO';$r.RowEdits.Add($e)
    Check (-not (Invoke-EE 'Save' @($r)).Success) 'Pre-ledger history is not guessed or rewritten when editing is enabled'
    $s=Invoke-EE 'Load' @($q);$time=$s.Rows[0].PerformedAt;$r=New-EERequest $s;$e=New-EEObject 'ExecutionRowEdit';$e.OperationToken=$token1;$e.NewPerformerUsername=$inactiveName;$e.Reason='INFO';$r.RowEdits.Add($e)
    $saved=Invoke-EE 'Save' @($r)
    Check ($saved.Success -and $saved.NewSnapshot.Rows[0].PerformerUsername -eq $inactiveName -and $saved.NewSnapshot.Rows[0].PerformedAt -eq $time) 'B2 corrects performer to inactive account without altering time'
    Check ((Get-EEScalar "SELECT NguoiSua_ID FROM B2DieuChinhLanLuu LIMIT 1") -eq $editor -and (Get-EEScalar "SELECT NguoiGuiGoc_ID FROM B3ThaoTac WHERE MaThaoTac='$token1'") -eq $worker) 'Editor is audited separately from immutable original B3 requester'
    $recover=Invoke-EEB3 $path recover @{raw_qr=$partialCode;ke_hoach_id=$planId;ke_hoach_hang_id=$itemId;group_id=$groupPartial;detail_id=$detail1;operation_token=$token1;actor_user_id=$worker}
    Check ($recover.ok -and $recover.result.operation_status -eq 'SAVED') 'B3 recovers original request after performer correction'
    $oldVersion=$recover.result.source_version
    foreach($end in @(1650,650)) {
        $s=Invoke-EE 'Load' @($q);$r=New-EERequest $s;$m=New-EEObject 'ReelMarkerEdit';$m.ReelId=$reel;$m.NewEnd=$end;$m.NewDirection=1;$m.Reason='MARKERS';$r.ReelEdits.Add($m)
        Check (Invoke-EE 'Save' @($r)).Success ('B2 marker end '+$end+' preserves 650m')
    }
    $stale=Cut $detail2 $token2 $partialCode $groupPartial $planId $itemId @{source_version=$oldVersion;expected_cut_length=100}
    Check ($stale.error_code -eq 'SOURCE_STATE_CHANGED') 'ABA marker change invalidates old B3 session'
    Draw-Detail $q 'partial'
    # Keep a real parent draft while B3 advances its execution facts.
    $parentType=$script:EEAssembly.GetType('DG_TonKhoBTP_v02.UI.NghiepVuKhac.Kho.XuatKho.UC_LapKeHoachCatDay',$true);$flags=[Reflection.BindingFlags]'Instance,NonPublic'
    $db=$script:EEAssembly.GetType('DG_TonKhoBTP_v02.Database.Kho.XuatKho.LapKeHoachCatDay_DB',$true)
    $context=$db.GetMethod('LayKeHoachTheoMa').Invoke($null,@($planCode));$parent=[Activator]::CreateInstance($parentType,$true)
    $parentType.GetField('_keHoachId',$flags).SetValue($parent,$planId);$parentType.GetField('_maKeHoachDaNap',$flags).SetValue($parent,$planCode)
    foreach($row in $context.Rows){$null=$parentType.GetMethod('ThemHoacCapNhatDongGrid',$flags).Invoke($parent,@($row,$false))}
    $null=$parentType.GetMethod('GhiNhanBanGoc',$flags).Invoke($parent,@())
    $rows=$parentType.GetField('_rows',$flags).GetValue($parent);$draftRow=@($rows.Values|Where-Object {$_.TonCuonLe_ID -eq $reel})[0];$draftRow.ChuoiChieuDaiCat='75'
    $parent.Controls.Find('tbGhiChu',$true)[0].Text='UNSAVED DATABASE TEST NOTE'
    $cut=Cut $detail2 $token2;Check $cut.ok 'B3 cuts second segment using current data'
    $refresh=New-EEObject 'ExecutionEditResult';$refresh.Success=$true;$null=$parentType.GetMethod('RefreshExecutionFactsPreservingDraft',$flags).Invoke($parent,@($refresh))
    Check ($rows[$draftRow.RowKey].ChuoiChieuDaiCat -eq '75' -and $rows[$draftRow.RowKey].TonThucTe -eq 550 -and $parent.Controls.Find('tbGhiChu',$true)[0].Text -eq 'UNSAVED DATABASE TEST NOTE') 'Parent refresh preserves draft and updates real progress to 550m'
    $parent.Dispose();$parent=$null
    $s=Invoke-EE 'Load' @($q);$r=New-EERequest $s;Add-EEUndo $r $token1
    Check (-not (Invoke-EE 'Preview' @($r)).Valid) 'Undo refuses an older cut without selected dependent later cut'
    Add-EEUndo $r $token2;$preview=Invoke-EE 'Preview' @($r)
    Check ($preview.Valid -and $preview.MarkerEventsToSupersede.Count -eq 2 -and $preview.ImpactByReelId[$reel].Contains('1650')) 'Undo previews exact dependent marker corrections'
    $saved=Invoke-EE 'Save' @($r)
    Check ($saved.Success -and (Get-EEScalar "SELECT SoCuoi FROM TonCuonLe WHERE id=$reel") -eq 800) 'Selected suffix undo restores original 0 to 800 markers'
    $replay=Cut $detail1 $token1;Check ($replay.error_code -eq 'OPERATION_CANCELLED') 'B3 refuses cancelled operation token after B2 undo'
    $newToken=[Guid]::NewGuid().ToString();Check (Cut $detail1 $newToken).ok 'B3 accepts a genuinely new operation after undo'
    Invoke-EESql "UPDATE KeHoachCatChiTiet SET ChieuDaiCanCat=650 WHERE id=$detail2;"
    $exhaustToken=[Guid]::NewGuid().ToString();Check (Cut $detail2 $exhaustToken).ok 'B3 can exhaust exactly the remaining 650m'
    $s=Invoke-EE 'Load' @($q);$r=New-EERequest $s;$m=New-EEObject 'ReelMarkerEdit';$m.ReelId=$reel;$m.NewEnd=1000;$m.NewDirection=1;$m.Reason='MARKERS';$r.ReelEdits.Add($m)
    Check ($s.Reels[$reel].Status -eq 'HET' -and -not (Invoke-EE 'Save' @($r)).Success) 'Marker editing cannot create stock on HET reel'
    $r=New-EERequest $s;Add-EEUndo $r $exhaustToken;Check (Invoke-EE 'Save' @($r)).Success 'Mistaken exhausted cut can be undone with proper state restoration'
    Invoke-EESql "UPDATE KeHoachCatChiTiet SET ChieuDaiCanCat=100 WHERE id=$detail2;"
    $rawFull='cuontp;'+$segment+';1000;CONVERT';$conversionToken=[Guid]::NewGuid().ToString()
    $cut=Cut $detail3 $conversionToken $rawFull $groupFull $planId $itemId @{end_value=1000;direction=1}
    Check $cut.ok 'B3 converts whole reel into partial with original database constraints and triggers'
    $generated=[long](Get-EEScalar "SELECT TonCuonLe_ID FROM KeHoachCatNhom WHERE id=$groupFull")
    $qGenerated=New-EEObject 'ExecutionDetailQuery' @($planId,$itemId,$null,$generated);Draw-Detail $qGenerated 'conversion'
    $s=Invoke-EE 'Load' @($qGenerated);$r=New-EERequest $s;Add-EEUndo $r $conversionToken
    Invoke-EESql "CREATE TRIGGER DatabaseValidationAuditFailure BEFORE INSERT ON B2DieuChinhKetQua BEGIN SELECT RAISE(ABORT,'test_audit_failure'); END;"
    $failedSave=Invoke-EE 'Save' @($r)
    Check (-not $failedSave.Success -and (Get-EEScalar "SELECT COUNT(*) FROM B3QrSuDung WHERE RawQr='$rawFull'") -eq 1 -and (Get-EEScalar "SELECT TrangThai FROM B3ThaoTac WHERE MaThaoTac='$conversionToken'") -eq 'ACTIVE') 'Audit failure rolls back conversion undo including QR and ledger'
    Invoke-EESql 'DROP TRIGGER DatabaseValidationAuditFailure;'
    $saved=Invoke-EE 'Save' @($r)
    Check ($saved.Success -and $null -eq $saved.NewSnapshot -and (Get-EEScalar "SELECT COUNT(*) FROM TonCuonLe WHERE id=$generated") -eq 0 -and (Get-EEScalar "SELECT COUNT(*) FROM KeHoachCatChiTiet WHERE KeHoachCatNhom_ID=$groupFull") -eq 2) 'Conversion undo via generated partial commits and preserves both planned segments'
    Check ((Get-EEScalar "SELECT COUNT(*) FROM B3QrSuDung WHERE RawQr='$rawFull'") -eq 0 -and (Get-EEScalar "SELECT SoCuon FROM TTCuonDay WHERE id=$fullSource") -eq 10) 'Conversion undo releases QR without incrementing imported quantity'
    $qFull=New-EEObject 'ExecutionDetailQuery' @($planId,$itemId,$qc,$null)
    $rawTake='cuontp;'+$segment+';1000;TAKE';$takeToken=[Guid]::NewGuid().ToString()
    $take=Invoke-EEB3 $path take @{raw_qr=$rawTake;ke_hoach_id=$planId;ke_hoach_hang_id=$itemId;quy_cach_cuon_chan_id=$qc;operation_token=$takeToken;actor_user_id=$worker}
    Check ($take.ok -and (Current-FullStock $fullSource) -eq 9) 'B3 takes a whole reel with explicit specification and derived stock becomes nine';Draw-Detail $qFull 'whole' 'TAKE'
    $s=Invoke-EE 'Load' @($qFull);$r=New-EERequest $s;Add-EEUndo $r $takeToken;$saved=Invoke-EE 'Save' @($r)
    Check ($saved.Success -and (Get-EEScalar "SELECT COUNT(*) FROM B3QrSuDung WHERE RawQr='$rawTake'") -eq 0 -and (Get-EEScalar "SELECT SoCuon FROM TTCuonDay WHERE id=$fullSource") -eq 10 -and (Current-FullStock $fullSource) -eq 10) 'Whole take undo restores derived stock to ten and QR without changing imports'
    $auditCount=Get-EEScalar 'SELECT COUNT(*) FROM B2DieuChinhLanLuu'
    Check ((Invoke-EE 'Save' @($r)).Success -and (Get-EEScalar 'SELECT COUNT(*) FROM B2DieuChinhLanLuu') -eq $auditCount) 'Committed ChangeId replay never duplicates effects or audit'
    $s=Invoke-EE 'Load' @($q);$staleRequest=New-EERequest $s;$m=New-EEObject 'ReelMarkerEdit';$m.ReelId=$reel;$m.NewEnd=2650;$m.NewDirection=1;$m.Reason='MARKERS';$staleRequest.ReelEdits.Add($m)
    $take2=Invoke-EEB3 $path take @{raw_qr=('cuontp;'+$segment+';1000;TAKE2');ke_hoach_id=$planId;ke_hoach_hang_id=$itemId;quy_cach_cuon_chan_id=$qc;operation_token=([Guid]::NewGuid().ToString());actor_user_id=$worker}
    Check ($take2.ok -and (Invoke-EE 'Save' @($staleRequest)).Conflict) 'Independent B3 write makes stale B2 snapshot fail safely'
    $planOther=$planId+1;$itemOther=$itemId+1;$groupOther=$groupPartial+2;$detailOther=$detail4+1
    Invoke-EESql "INSERT INTO KeHoach(id,MaKeHoach,NgayKeHoach,Users_id) VALUES($planOther,'$planCode-OTHER','2026-10-27',$worker);INSERT INTO KeHoachHang(id,KeHoach_ID,DanhSachMaSP_ID) VALUES($itemOther,$planOther,$product);INSERT INTO KeHoachCatNhom(id,KeHoachHang_ID,TonCuonLe_ID) VALUES($groupOther,$itemOther,$reel);INSERT INTO KeHoachCatChiTiet(id,KeHoachCatNhom_ID,ChieuDaiCanCat) VALUES($detailOther,$groupOther,10);"
    Check (Cut $detailOther ([Guid]::NewGuid().ToString()) $partialCode $groupOther $planOther $itemOther).ok 'Another plan can cut its reserved segment on the shared reel'
    $s=Invoke-EE 'Load' @($q);$r=New-EERequest $s;Add-EEUndo $r $newToken
    Check (-not (Invoke-EE 'Preview' @($r)).Valid) 'Undo blocks unselected later execution from another plan'
    Invoke-EESql "UPDATE KeHoach SET TrangThai='HUY' WHERE id=$planId;"
    $s=Invoke-EE 'Load' @($q);$r=New-EERequest $s;Add-EEUndo $r $newToken
    Check (-not $s.EditingEnabled -and -not (Invoke-EE 'Save' @($r)).Success) 'HUY plan remains read-only at the database boundary'
    $audit=Invoke-EE 'LoadAudit' @($planId,$reel,'')
    Check (@($audit.Rows|Where-Object {$_.Loai -eq 'HOAN_TAC'}).Count -ge 3) 'Cancelled operations remain visible in reel audit'
    Check ((Get-EEScalar "SELECT COUNT(*) FROM LichSuCat WHERE KeHoachCatChiTiet_ID<$b") -eq $originalCuts -and (Get-EEScalar "SELECT COUNT(*) FROM LichSuLayCuon WHERE KeHoachHang_ID<$b") -eq $originalTakes) 'All original execution rows retained in test copy'
    $conn=[DG_TonKhoBTP_v02.Database.DB_Base]::OpenConnection()
    try{$cmd=$conn.CreateCommand();$cmd.CommandText='PRAGMA foreign_key_check';$reader=$cmd.ExecuteReader();$foreignKeyClean=-not $reader.Read();$reader.Dispose();$cmd.Dispose()}finally{$conn.Dispose()}
    Check $foreignKeyClean 'Full copied database still passes foreign_key_check after all operations'
    Check ((Get-EEScalar 'PRAGMA integrity_check') -eq 'ok') 'Full copied database still passes integrity_check after all operations'
    Check ((Get-EEScalar "SELECT COUNT(*) FROM sqlite_master WHERE type='trigger' AND name IN ('TR_B3_QuyCachLayInsert','TR_B3_QuyCachNhomInsert','TR_B3_QuyCachNhomUpdate')") -eq 3) 'All original specification-parent protection triggers remain present'
} catch {
    $checks.Add([pscustomobject]@{name='Unhandled validation failure';passed=$false;message=$_.Exception.ToString()})
    Write-Output ('FAIL: '+$_.Exception.Message)
} finally {
    if($source){$source.Dispose()};if($target){$target.Dispose()};if($parent){$parent.Dispose()}
    $afterHash=(Get-FileHash -LiteralPath $ReadOnlyDatabasePath -Algorithm SHA256).Hash
    $checks.Add([pscustomobject]@{name='Original database SHA256 unchanged';passed=($beforeHash -eq $afterHash)})
    $report=[pscustomobject]@{source=$ReadOnlyDatabasePath;assembly='Release';source_sha256_before=$beforeHash;source_sha256_after=$afterHash;checks=$checks.ToArray();passed=@($checks|Where-Object passed).Count;failed=@($checks|Where-Object {-not $_.passed}).Count}
    [IO.File]::WriteAllText((Join-Path $OutputDirectory 'real-database-results.json'),($report|ConvertTo-Json -Depth 10),(New-Object Text.UTF8Encoding($false)))
    Close-ExecutionEditFixture $path
    Write-Output ('RESULT: '+$report.passed+' passed, '+$report.failed+' failed; original DB unchanged='+($beforeHash -eq $afterHash))
}
if($report.failed){exit 1}
