param(
    [string]$AssemblyPath = (Join-Path $PSScriptRoot '..\DG_TonKhoBTP_v02\bin\Debug\DG_TonKhoBTP_v02.exe')
)
# Run with Windows PowerShell -STA after building. Uses an isolated fixture database.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms
$assembly = [Reflection.Assembly]::LoadFrom((Resolve-Path $AssemblyPath).Path)
$null = [Reflection.Assembly]::LoadFrom((Join-Path (Split-Path $AssemblyPath) 'System.Data.SQLite.dll'))
$tempPath = Join-Path ([IO.Path]::GetTempPath()) ('CatDay.ChiTiet.' + [Guid]::NewGuid().ToString('N') + '.db')
$oldConnection = [DG_TonKhoBTP_v02.Database.DB_Base]::_connStr
function Assert-True($condition, $message) { if (-not $condition) { throw $message } }
try {
    $conn = New-Object System.Data.SQLite.SQLiteConnection("Data Source=$tempPath;Version=3;")
    $conn.Open()
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = @'
CREATE TABLE KeHoachHang(id INTEGER PRIMARY KEY, KeHoach_ID INTEGER);
CREATE TABLE KeHoachHangQuyCachCuonChan(id INTEGER PRIMARY KEY, KeHoachHang_ID INTEGER, ChieuDai1Cuon INTEGER, SoLuongCuonCanLay INTEGER);
CREATE TABLE LichSuLayCuon(id INTEGER PRIMARY KEY, QuyCachCuonChan_ID INTEGER, KeHoachHang_ID INTEGER, SoLuong INTEGER, TTCuonDay_ID INTEGER, NguoiThucHien TEXT, ThoiGianThucHien TEXT);
CREATE TABLE TTThanhPham(id INTEGER PRIMARY KEY, MaBin TEXT);
CREATE TABLE TonCuonLe(id INTEGER PRIMARY KEY, MaCuon TEXT, TTThanhPham_ID INTEGER);
CREATE TABLE KeHoachCatNhom(id INTEGER PRIMARY KEY, KeHoachHang_ID INTEGER, TonCuonLe_ID INTEGER, QuyCachCuonChan_ID INTEGER);
CREATE TABLE KeHoachCatChiTiet(id INTEGER PRIMARY KEY, KeHoachCatNhom_ID INTEGER, ChieuDaiCanCat INTEGER);
CREATE TABLE LichSuCat(id INTEGER PRIMARY KEY, KeHoachCatChiTiet_ID INTEGER, TonCuonLe_ID INTEGER, NguoiThucHien TEXT, ThoiGianThucHien TEXT);
INSERT INTO KeHoachHang VALUES(1,1);
INSERT INTO KeHoachHangQuyCachCuonChan VALUES(10,1,100,3);
INSERT INTO LichSuLayCuon VALUES(1,10,1,1,100,'worker','2026-10-23');
INSERT INTO TTThanhPham VALUES(1,'BIN-CHAN'),(2,'BIN-LE');
INSERT INTO TonCuonLe VALUES(1,'cuontp;265081;200;20261023;202610231040110003;GOLDCUP; TCVN 6610-3/IEC 60227-3; 450/750V Cu/PVC 70ºC 1x50 SQMM',1),(2,'LOT-LE',2);
INSERT INTO KeHoachCatNhom VALUES(20,1,2,NULL),(30,1,1,10),(40,1,NULL,10);
INSERT INTO KeHoachCatChiTiet VALUES(20,20,20),(30,30,30),(40,40,40);
INSERT INTO LichSuCat VALUES(1,30,1,'worker','2026-10-23'),(2,20,2,'worker','2026-10-23');
'@
    $null = $cmd.ExecuteNonQuery()
    $cmd.Dispose()
    $conn.Dispose()
    [DG_TonKhoBTP_v02.Database.DB_Base]::SetDatabasePath($tempPath)
    $dbType = $assembly.GetType('DG_TonKhoBTP_v02.Database.Kho.XuatKho.LapKeHoachCatDay_DB', $true)
    $method = $dbType.GetMethod('LayChiTietThucHien')
    $full = $method.Invoke($null, [object[]]@(1L,1L,10L,$null))
    $partial = $method.Invoke($null, [object[]]@(1L,1L,$null,2L))
    Assert-True ($full.Count -eq 4) 'Expected whole reels, converted group and pending cut'
    Assert-True ($full[0].GhiChu -eq 'Lấy cả cuộn chẵn' -and $full[0].SoLuong -eq 1) 'Executed whole reel note'
    Assert-True ($full[1].GhiChu -eq '' -and $full[1].SoLuong -eq 2) 'Pending whole reel note'
    Assert-True ($full[2].GhiChu -eq 'Cắt lẻ từ cuộn chẵn: cuontp;265081;200;20261023;202610231040110003;GOLDCUP; TCVN 6610-3/IEC 60227-3; 450/750V Cu/PVC 70ºC 1x50 SQMM') 'Cut note must preserve the complete reel lot instead of the product bin'
    Assert-True ($full[3].GhiChu -eq '') 'Unassigned source must not invent a bin/lot'
    Assert-True ($partial.Count -eq 1 -and $partial[0].GhiChu -eq 'Cắt lẻ từ cuộn lẻ: LOT-LE') 'Partial reel must show its lot'
    Write-Output 'PASS: notes for whole, partial, converted and unassigned sources'

    $uiType = $assembly.GetType('DG_TonKhoBTP_v02.UI.NghiepVuKhac.Kho.XuatKho.UC_LapKeHoachCatDay', $true)
    $popupType = $uiType.GetNestedType('FrmChiTietThucHien', [Reflection.BindingFlags]::NonPublic)
    $popup = [Activator]::CreateInstance($popupType, [object[]]@('Test', $full))
    try {
        $grid = @($popup.Controls | Where-Object { $_ -is [Windows.Forms.DataGridView] })[0]
        $headers = ($grid.Columns | ForEach-Object { $_.HeaderText }) -join '|'
        Assert-True ($headers -eq 'Số lượng|Chiều dài|Trạng thái|Thời gian|Người thực hiện|Ghi chú') 'Detail column order'
        Assert-True ($popup.Padding.All -eq 5 -and $grid.DefaultCellStyle.Padding.All -eq 5) 'Content padding'
        Assert-True ($grid.Rows[0].Cells[0].Value -eq 1 -and $grid.Rows[0].Cells[1].Value -eq 100) 'Values must follow column order'
        Assert-True ($grid.Rows[2].Cells[5].Value -eq $full[2].GhiChu) 'Notes must appear in the final column'
        foreach ($index in @(1,3)) {
            foreach ($column in @(3,4,5)) {
                Assert-True ([string]::IsNullOrEmpty([string]$grid.Rows[$index].Cells[$column].Value)) 'Pending execution fields must be blank'
            }
        }
        $paint = $uiType.GetMethod('ToMauTrangThai', [Reflection.BindingFlags]'NonPublic,Static')
        $colors = @{}
        foreach ($status in @('Chưa lưu','Chưa thực hiện','Đang thực hiện','Hoàn thành','Đã thực hiện','Hết tồn')) {
            $cell = New-Object Windows.Forms.DataGridViewTextBoxCell
            $null = $paint.Invoke($null, [object[]]@($cell.PSObject.BaseObject,$status))
            $colors[$status] = $cell.Style.ForeColor.ToArgb()
            Assert-True ($cell.Style.ForeColor -eq $cell.Style.SelectionForeColor) 'Selection must preserve text color'
            Assert-True (-not $cell.Style.BackColor.IsEmpty) 'Status must have a background color'
            Assert-True ($cell.Style.BackColor -eq $cell.Style.SelectionBackColor) 'Selection must preserve background color'
            Assert-True ($cell.Style.BackColor -ne $cell.Style.ForeColor) 'Status text must differ from its background'
        }
        Assert-True ($colors['Hoàn thành'] -eq $colors['Đã thực hiện']) 'Completed states share a color'
        Assert-True (@($colors.Values | Select-Object -Unique).Count -eq 5) 'Five distinct status colors'
        Write-Output 'PASS: detail layout, padding, values and status colors'
    } finally { $popup.Dispose() }
} finally {
    if ($conn) { $conn.Dispose() }
    [DG_TonKhoBTP_v02.Database.DB_Base]::_connStr = $oldConnection
    [System.Data.SQLite.SQLiteConnection]::ClearAllPools()
    if (Test-Path -LiteralPath $tempPath) { Remove-Item -LiteralPath $tempPath }
}
