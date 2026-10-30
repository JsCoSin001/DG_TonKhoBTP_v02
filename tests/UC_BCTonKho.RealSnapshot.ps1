param([string]$WorkspaceRoot=(Split-Path $PSScriptRoot -Parent))
# Reads only the already copied, immutable snapshot. Never opens the configured source database.
$ErrorActionPreference='Stop'
$evidence=Join-Path $WorkspaceRoot 'docs/verification/uc-bctonkho-evidence'
$snapshot=Join-Path $evidence 'configured-snapshot.db'
$bin=Join-Path $WorkspaceRoot 'DG_TonKhoBTP_v02/bin/Debug'
$assembly=[Reflection.Assembly]::LoadFrom((Join-Path $bin 'DG_TonKhoBTP_v02.exe'))
$null=[Reflection.Assembly]::LoadFrom((Join-Path $bin 'System.Data.SQLite.dll'))
$db=$assembly.GetType('DG_TonKhoBTP_v02.Database.Kho.TonKho.BC_TonKho_DB',$true)
$kind=$assembly.GetType('DG_TonKhoBTP_v02.Models.Kho.TonKho.BCTonKho_LoaiTimKiem',$true)
$flags=[Reflection.BindingFlags]'Static,NonPublic'
$connection="Data Source=$snapshot;Read Only=True;FailIfMissing=True;Version=3;"
[DG_TonKhoBTP_v02.Database.DB_Base]::_connStr=$connection
[DG_TonKhoBTP_v02.Database.DatabaseHelper]::_connStr=$connection
$sqlLog=New-Object Collections.Generic.List[string]
$cases=New-Object Collections.Generic.List[object]
function Query([string]$sql) {
    if($sql -notmatch '^\s*(SELECT|WITH|PRAGMA)\b'){throw 'Read-only SQL guard'}
    $c=New-Object System.Data.SQLite.SQLiteConnection($connection)
    try{$c.Open();$cmd=$c.CreateCommand();$cmd.CommandText=$sql;$dt=New-Object Data.DataTable;$dt.Load($cmd.ExecuteReader());return ,$dt}finally{$c.Dispose()}
}
function Oracle([string]$sql) {
    $dt=Query $sql
    $sqlLog.Add($sql+[Environment]::NewLine+($dt|Format-Table -AutoSize|Out-String -Width 240))
    return ,$dt
}
function Detail([int]$mode,[object]$id,[string]$value,[bool]$all=$true){
    $args=[object[]]@([Enum]::ToObject($kind,$mode),$id,$value,$all,[Threading.CancellationToken]::None)
    $task=$db.GetMethod('LayChiTietTonAsync',$flags).Invoke($null,$args)
    return $task.GetAwaiter().GetResult()
}
function Check([string]$id,[string]$title,[string]$expected,[scriptblock]$run) {
    try{$actual=[string](& $run);$status=if($actual -eq $expected){'PASS'}else{'FAIL'}}catch{$actual=$_.Exception.ToString();$status='FAIL'}
    $cases.Add([pscustomobject]@{ID=$id;Title=$title;Expected=$expected;Actual=$actual;Status=$status})
    Write-Output "$status $id expected=$expected actual=$actual"
}
$hash=(Get-FileHash -LiteralPath $snapshot -Algorithm SHA256).Hash
$tables=Query "SELECT name FROM sqlite_master WHERE type='table'"
$required=@('TTThanhPham','DanhSachMaSP','TTNhapKhoTP','TTCuonDay','TonCuonLe','LichSuCat','LichSuLayCuon','KeHoach','KeHoachHang','KeHoachHangQuyCachCuonChan','KeHoachCatNhom','KeHoachCatChiTiet')
foreach($name in $required){if($name -notin $tables.name){throw "Missing table: $name"}; $null=Oracle "PRAGMA table_info('$name')"}
Check 'REAL-01' 'Schema has 12 required tables' '12' { @($required|Where-Object {$_ -in $tables.name}).Count }
$source=Oracle "SELECT tp.id AS BinId,tp.MaBin,sp.Ten,cd.id AS SourceId,cd.SoCuon,cd.ChieuDai_1cuon FROM TTThanhPham tp JOIN DanhSachMaSP sp ON sp.id=tp.DanhSachSP_ID JOIN TTNhapKhoTP nk ON nk.TTThanhPham_ID=tp.id JOIN TTCuonDay cd ON cd.ThongTinNhapKho_ID=nk.id WHERE tp.MaBin='E1-265081/5-03-00'"
if($source.Rows.Count -ne 1){throw 'Expected supplied sample source is absent or ambiguous; do not force expected data'}
$binId=[long]$source.Rows[0].BinId;$sourceId=[long]$source.Rows[0].SourceId;$productName=[string]$source.Rows[0].Ten
$result=Detail 1 $binId 'E1-265081/5-03-00'
$oracle=Oracle "SELECT (SELECT SoCuon FROM TTCuonDay WHERE id=$sourceId) AS Imported, (SELECT SUM(SoLuong) FROM LichSuLayCuon WHERE TTCuonDay_ID=$sourceId) AS Taken,(SELECT COUNT(*) FROM TonCuonLe p WHERE p.TTCuonDay_ID=$sourceId AND EXISTS(SELECT 1 FROM LichSuCat h WHERE h.TonCuonLe_ID=p.id AND h.LoaiNguon='CUON_CHAN')) AS Converted"
Check 'REAL-02' 'Sample whole stock vs raw arithmetic' '10-2-2=6=6' { $o=$oracle.Rows[0];$row=@($result.Dong|Where-Object {$_.TTCuonDayId -eq $sourceId -and [int]$_.Kieu -eq 1})[0]; "$($o.Imported)-$($o.Taken)-$($o.Converted)=$([long]$o.Imported-[long]$o.Taken-[long]$o.Converted)=$($row.SoLuong)" }
$partials=Oracle "SELECT id,MaCuon,SoDau,SoCuoi,ABS(SoCuoi-SoDau) AS Remaining FROM TonCuonLe WHERE TTThanhPham_ID=$binId ORDER BY id"
Check 'REAL-03' 'Sample generated partials are separate 100m and20m' '100,20|100,20|True' { $rows=@($result.Dong|Where-Object {[int]$_.Kieu -eq 2}|Sort-Object TonCuonLeId); ($partials.Remaining -join ',')+'|'+($rows.ChieuDaiConLai -join ',')+'|'+(@($rows|Where-Object {$null -ne $_.ChieuDaiBanDau}).Count -eq 0) }
$descending=Oracle 'SELECT id,SoDau,SoCuoi,ABS(SoCuoi-SoDau) AS Remaining FROM TonCuonLe WHERE SoDau=996 AND SoCuoi=600'
Check 'REAL-04' 'Real descending markers 996 to600' '396=396|False' { $r=Detail 3 ([long]$descending.Rows[0].id) ''; "$($descending.Rows[0].Remaining)=$($r.Dong[0].ChieuDaiConLai)|$($r.Dong[0].LaBatThuong)" }
Check 'REAL-05' 'Real exact Bin selection returns own identities' 'True' { @($result.Dong|Where-Object {$_.TTThanhPhamId -ne $binId}).Count -eq 0 }
Check 'REAL-06' 'Real partial selection contains exactly chosen identity' '1,True' { $id=[long]$partials.Rows[0].id;$r=Detail 3 $id ''; "$($r.Dong.Count),$($r.Dong[0].TonCuonLeId -eq $id)" }
Check 'REAL-07' 'Real product selection includes all same-name Bin sources' 'True' {
    $escaped=$productName.Replace("'","''");$bins=Oracle "SELECT DISTINCT tp.id FROM TTThanhPham tp JOIN TTNhapKhoTP nk ON nk.TTThanhPham_ID=tp.id JOIN DanhSachMaSP sp ON sp.id=tp.DanhSachSP_ID WHERE tp.NhapKho=1 AND tp.Temp=0 AND TRIM(sp.Ten)=TRIM('$escaped') AND EXISTS(SELECT 1 FROM TTCuonDay cd WHERE cd.ThongTinNhapKho_ID=nk.id)"
    $r=Detail 2 $null $productName
    $expected=@($bins.id|Sort-Object);$actual=@($r.Dong.TTThanhPhamId|Where-Object {$null-ne $_}|Sort-Object -Unique)
    ($expected -join ',') -eq ($actual -join ',')
}
Check 'REAL-08' 'Every current partial matches independent ABS markers' 'True' {
    $rows=Oracle 'SELECT id,SoDau,SoCuoi,ABS(SoCuoi-SoDau) AS Remaining FROM TonCuonLe ORDER BY id'
    $ok=$true;foreach($row in $rows.Rows){$r=Detail 3 ([long]$row.id) '';if($r.Dong.Count -ne 1 -or $r.Dong[0].ChieuDaiConLai -ne [long]$row.Remaining){$ok=$false}}
    $ok
}
Check 'REAL-09' 'Legacy production inventory query uses snapshot' 'True' {
    $cd=New-Object 'Collections.Generic.List[DG_TonKhoBTP_v02.Models.CongDoan]'
    $cd.Add([DG_TonKhoBTP_v02.Dictionary.ThongTinChungCongDoan]::KeoRut)
    $r=[DG_TonKhoBTP_v02.Database.DatabaseHelper]::GetTonKhoCD($cd)
    $r.Rows.Count -gt 0
}
Check 'REAL-10' 'Legacy date production query uses snapshot' 'True' {
    $cd=New-Object 'Collections.Generic.List[DG_TonKhoBTP_v02.Models.CongDoan]'
    $cd.Add([DG_TonKhoBTP_v02.Dictionary.ThongTinChungCongDoan]::KeoRut)
    $r=[DG_TonKhoBTP_v02.Database.DatabaseHelper]::GetDataBaoCaoSX([DateTime]'2025-01-01',[DateTime]'2026-10-29',$cd)
    $r.Rows.Count -gt 0
}
Check 'REAL-11' 'Real read APIs leave snapshot bytes untouched' 'True' { (Get-FileHash -LiteralPath $snapshot -Algorithm SHA256).Hash -eq $hash }
$cases | Export-Csv (Join-Path $evidence 'real-snapshot.tsv') -Delimiter "`t" -NoTypeInformation -Encoding UTF8
$sqlLog | Set-Content (Join-Path $evidence 'real-independent-sql.txt') -Encoding UTF8
$result.Dong | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $evidence 'real-sample-api.json') -Encoding UTF8
Write-Output "TOTAL $($cases.Count) PASS $(@($cases|Where-Object Status -eq PASS).Count) FAIL $(@($cases|Where-Object Status -eq FAIL).Count)"
if(@($cases|Where-Object Status -eq FAIL).Count){exit 1}
