param(
    [string]$AssemblyPath = (Join-Path $PSScriptRoot '..\DG_TonKhoBTP_v02\bin\Debug\DG_TonKhoBTP_v02.exe'),
    [string]$ReadOnlyDatabasePath
)
# Run with Windows PowerShell -STA after building. The source is opened read-only.
$ErrorActionPreference = 'Stop'
if (-not $ReadOnlyDatabasePath) { Write-Output 'SKIP: supply -ReadOnlyDatabasePath'; exit 0 }
Add-Type -AssemblyName System.Windows.Forms
$assembly = [Reflection.Assembly]::LoadFrom((Resolve-Path $AssemblyPath).Path)
$null = [Reflection.Assembly]::LoadFrom((Join-Path (Split-Path $AssemblyPath) 'System.Data.SQLite.dll'))
$dbType = $assembly.GetType('DG_TonKhoBTP_v02.Database.Kho.XuatKho.LapKeHoachCatDay_DB', $true)
$modelType = $assembly.GetType('DG_TonKhoBTP_v02.Models.Kho.XuatKho.LapKeHoachCatDay_BatchSaveRequest', $true)
$tempPath = Join-Path ([IO.Path]::GetTempPath()) ('NguoiCat.' + [Guid]::NewGuid().ToString('N') + '.db')
$oldConnection = [DG_TonKhoBTP_v02.Database.DB_Base]::_connStr
function Assert-True($condition, $message) { if (-not $condition) { throw $message } }
function Sql([string]$sql) {
    $cmd = $conn.CreateCommand(); $cmd.CommandText = $sql
    try { return $cmd.ExecuteScalar() } finally { $cmd.Dispose() }
}
function Save([string]$username, [long]$id = -901) {
    $request = [Activator]::CreateInstance($modelType, $true)
    $request.KeHoach_IDDuKien = $id; $request.MaKeHoach = 'TEST_NGUOICAT'
    $request.NguoiCat = $username; $request.GhiChu = 'saved'
    return $dbType.GetMethod('LuuKeHoachBatch').Invoke($null, [object[]]@($request))
}
try {
    $source = New-Object System.Data.SQLite.SQLiteConnection("Data Source=$ReadOnlyDatabasePath;Read Only=True;Version=3;")
    $conn = New-Object System.Data.SQLite.SQLiteConnection("Data Source=$tempPath;Version=3;")
    try { $source.Open(); $conn.Open(); $source.BackupDatabase($conn, 'main', 'main', -1, $null, 0) }
    finally { $source.Dispose() }
    [DG_TonKhoBTP_v02.Database.DB_Base]::SetDatabasePath($tempPath)
    $null = Sql @"
INSERT OR IGNORE INTO roles(role_name) VALUES ('Worker_WH');
INSERT INTO users(user_id,username,password_hash,is_active) VALUES
(-901,'Test_Cutter','test',1),(-902,'Test_Inactive','test',0),
(-903,'Test_OtherRole','test',1),(-904,'Test_100%','test',1),(-905,'Test_100X','test',1);
INSERT INTO user_roles(user_id,role_id)
SELECT u.user_id,r.role_id FROM users u,roles r
WHERE u.user_id IN (-901,-902,-904,-905) AND r.role_name='Worker_WH';
INSERT INTO DanhSachMaSP(id,Ten,Ma,DonVi,KieuSP)
VALUES (-901,'TEST_NGUOICAT_PRODUCT','TEST_NGUOICAT_PRODUCT','m','TP');
INSERT INTO TTThanhPham(id,DanhSachSP_ID,MaBin,NhapKho,Temp) VALUES (-901,-901,'TEST_NGUOICAT_BIN',1,0);
INSERT INTO TTNhapKhoTP(id,TTThanhPham_ID,TenSP,TongChieuDai,GhiChu,NguoiLam)
VALUES (-901,-901,'TEST_NGUOICAT_PRODUCT',1000,'','test');
INSERT INTO TTCuonDay(id,ThongTinNhapKho_ID,SoCuon,ChieuDai_1cuon) VALUES (-901,-901,10,100);
INSERT INTO KeHoach(id,MaKeHoach,NgayKeHoach,Users_id,GhiChu)
VALUES (-901,'TEST_NGUOICAT','2026-10-17',-901,'original');
INSERT INTO KeHoachHang(id,KeHoach_ID,DanhSachMaSP_ID)
VALUES (-901,-901,-901);
INSERT INTO KeHoachHangQuyCachCuonChan(KeHoachHang_ID,ChieuDai1Cuon,SoLuongCuonCanLay)
VALUES (-901,100,1);
"@
    $search = $dbType.GetMethod('TimGoiYNguoiCatAsync')
    Assert-True ($null -ne $search) 'Missing cutter search behavior'
    $task = $search.Invoke($null, [object[]]@('tEsT_', [Threading.CancellationToken]::None))
    $table = $task.GetAwaiter().GetResult()
    $names = @($table.Rows | ForEach-Object { $_.username })
    Assert-True ($names -contains 'Test_Cutter') 'Search must match case-insensitive substrings'
    Assert-True ($names -notcontains 'Test_Inactive') 'Search must exclude inactive users'
    Assert-True ($names -notcontains 'Test_OtherRole') 'Search must filter Worker_WH role'
    $table = $search.Invoke($null, [object[]]@('100%', [Threading.CancellationToken]::None)).GetAwaiter().GetResult()
    Assert-True ($table.Rows.Count -eq 1 -and $table.Rows[0].username -eq 'Test_100%') 'Search must treat wildcard characters literally'
    Write-Output 'PASS: suggestions filter role/activity and match literal substrings'
    foreach ($invalid in @('', 'Missing', 'Test_Inactive', 'Test_OtherRole')) {
        $result = Save $invalid
        Assert-True (-not $result.ThanhCong) "Must reject invalid cutter: $invalid"
        Assert-True ((Sql 'SELECT Users_id FROM KeHoach WHERE id=-901') -eq -901) 'Rejected save must preserve assignment'
        Assert-True ((Sql 'SELECT GhiChu FROM KeHoach WHERE id=-901') -eq 'original') 'Rejected save must preserve header'
    }
    Write-Output 'PASS: invalid/empty/ineligible names cannot modify the plan'
    $result = Save '  tEsT_100%  '
    Assert-True $result.ThanhCong 'Exact typed username must save without selecting a suggestion'
    Assert-True ((Sql 'SELECT Users_id FROM KeHoach WHERE id=-901') -eq -904) 'Save must persist user ID'
    $null = Sql 'UPDATE users SET is_active=0 WHERE user_id=-904; DELETE FROM user_roles WHERE user_id=-904;'
    $result = Save 'Test_100%'
    Assert-True $result.ThanhCong 'Existing assignment must survive deactivation/role removal'
    $header = $dbType.GetMethod('LayKeHoachTheoMa').Invoke($null, [object[]]@('TEST_NGUOICAT')).Header
    Assert-True ($header.UsersId -eq -904 -and $header.NguoiCat -eq 'Test_100%') 'Reload must resolve username from Users_id'
    $null = Sql "UPDATE KeHoach SET TrangThai='HUY' WHERE id=-901;"
    $result = Save 'Test_Cutter'
    Assert-True (-not $result.ThanhCong) 'Inactive plan must reject reassignment'
    Assert-True ((Sql 'SELECT Users_id FROM KeHoach WHERE id=-901') -eq -904) 'Inactive plan must retain assignment'
    Write-Output 'PASS: typed names save ID, historical assignment reloads, HUY rejects changes'
    $criteriaType = $assembly.GetType('DG_TonKhoBTP_v02.Models.Kho.XuatKho.LapKeHoachCatDay_SearchCriteria', $true)
    $criteria = [Activator]::CreateInstance($criteriaType, $true)
    $criteria.SearchType = [Enum]::Parse($criteriaType.GetProperty('SearchType').PropertyType, 'TatCa')
    $available = $dbType.GetMethod('TimKiem').Invoke($null, [object[]]@($criteria))
    $row = @($available | Where-Object { $_.DanhSachMaSP_ID -eq -901 -and $_.LoaiDong.ToString() -eq 'CuonChan' -and $_.TonKhaDung -gt 0 })[0]
    Assert-True ($null -ne $row) 'Fixture needs an available whole reel'
    $row.SoLuongCanLay = 1
    $request = [Activator]::CreateInstance($modelType, $true)
    $request.MaKeHoach = 'TEST_NGUOICAT_NEW'; $request.NguoiCat = 'test_cutter'
    $request.DongCanLuu.Add($row)
    $result = $dbType.GetMethod('LuuKeHoachBatch').Invoke($null, [object[]]@($request))
    Assert-True $result.ThanhCong ('Creating plan failed: ' + $result.Loi)
    Assert-True ((Sql "SELECT Users_id FROM KeHoach WHERE MaKeHoach='TEST_NGUOICAT_NEW'") -eq -901) 'New plan must persist cutter ID'
    # Fail after header insertion: both header and assignment must roll back.
    $request.MaKeHoach = 'TEST_NGUOICAT_ROLLBACK'; $row.SoLuongCanLay = [int]::MaxValue
    $result = $dbType.GetMethod('LuuKeHoachBatch').Invoke($null, [object[]]@($request))
    Assert-True (-not $result.ThanhCong) 'Unavailable stock must reject plan creation'
    Assert-True ((Sql "SELECT COUNT(*) FROM KeHoach WHERE MaKeHoach='TEST_NGUOICAT_ROLLBACK'") -eq 0) 'Row failure must roll back cutter/header insertion'
    Write-Output 'PASS: creating plan persists cutter ID; row failure rolls back the entire save'
    $controlType = $assembly.GetType('DG_TonKhoBTP_v02.UI.NghiepVuKhac.Kho.XuatKho.UC_LapKeHoachCatDay', $true)
    $control = [Activator]::CreateInstance($controlType)
    try {
        $flags = [Reflection.BindingFlags]'Instance,NonPublic'
        $combo = $controlType.GetField('cbxNguoiCat', $flags).GetValue($control)
        $helper = $controlType.GetField('_nguoiCatSearchHelper', $flags).GetValue($control)
        $selection = New-Object System.Data.DataTable
        $null = $selection.Columns.Add('username', [string])
        $null = $selection.Rows.Add('Test_Cutter')
        $helper.GetType().GetMethod('FireItemSelected', $flags).Invoke($helper, [object[]]@($selection.DefaultView[0]))
        Assert-True ($combo.Text -eq 'Test_Cutter') 'Selecting suggestion must fill username'
        $controlType.GetMethod('GhiNhanBanGoc', $flags).Invoke($control, $null)
        $combo.Text = 'Test_100X'
        Assert-True ($controlType.GetMethod('CoThayDoiChuaLuu', $flags).Invoke($control, $null)) 'Changing only cutter must mark unsaved changes'
        $controlType.GetMethod('ResetVeTrangThaiMoi', $flags).Invoke($control, $null)
        Assert-True ([string]::IsNullOrEmpty($combo.Text)) 'Reset must clear cutter'
        Assert-True (-not $controlType.GetMethod('CoThayDoiChuaLuu', $flags).Invoke($control, $null)) 'Reset must clear dirty state'
        Write-Output 'PASS: cutter-only edit and reset participate in form state'
    } finally { $control.Dispose() }
} finally {
    [DG_TonKhoBTP_v02.Database.DB_Base]::_connStr = $oldConnection
    if ($conn) { $conn.Dispose() }
    [System.Data.SQLite.SQLiteConnection]::ClearAllPools()
    if (Test-Path -LiteralPath $tempPath) { Remove-Item -LiteralPath $tempPath }
}
