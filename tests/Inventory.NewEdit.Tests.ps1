param(
    [string]$AssemblyPath = (Join-Path $PSScriptRoot '..\DG_TonKhoBTP_v02\bin\Debug\DG_TonKhoBTP_v02.exe'),
    [string]$ReadOnlyDatabasePath
)

# Run in Windows PowerShell -STA after building. All SQLite tests use :memory:.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Data
$assembly = [Reflection.Assembly]::LoadFrom((Resolve-Path $AssemblyPath).Path)
$null = [Reflection.Assembly]::LoadFrom((Join-Path (Split-Path $AssemblyPath) 'System.Data.SQLite.dll'))
$dbType = $assembly.GetType('DG_TonKhoBTP_v02.Database.SanXuat.SubmitForm_DB', $true)
$instanceFlags = [Reflection.BindingFlags]'Instance,NonPublic'
$staticFlags = [Reflection.BindingFlags]'Static,NonPublic'
$script:passed = 0
$script:failed = 0

function Assert-Equal($expected, $actual, [string]$message) {
    if ($expected -ne $actual) { throw "$message (expected '$expected', actual '$actual')" }
}

function Test-Case([string]$name, [scriptblock]$check) {
    try { & $check; $script:passed++; Write-Output "PASS: $name" }
    catch { $script:failed++; Write-Output "FAIL: $name - $($_.Exception.Message)" }
}

function Field($target, [string]$name) {
    return ,$target.GetType().GetField($name, $instanceFlags).GetValue($target)
}

function Invoke-Private($target, [string]$name, [object[]]$arguments) {
    for ($i=0; $i -lt $arguments.Length; $i++) {
        if ($null -ne $arguments[$i]) { $arguments[$i]=$arguments[$i].PSObject.BaseObject }
    }
    try { return $target.GetType().GetMethod($name, $instanceFlags).Invoke($target, $arguments) }
    catch [Reflection.TargetInvocationException] { throw $_.Exception.InnerException }
}

function Invoke-Db([string]$name, [object[]]$arguments) {
    for ($i=0; $i -lt $arguments.Length; $i++) {
        if ($null -ne $arguments[$i]) { $arguments[$i]=$arguments[$i].PSObject.BaseObject }
    }
    try { return $dbType.GetMethod($name, $staticFlags).Invoke($null, $arguments) }
    catch [Reflection.TargetInvocationException] { throw $_.Exception.InnerException }
}

function New-Nvl([string]$bin = 'A', [double]$start = 398, [double]$end = 120) {
    $nvl = New-Object DG_TonKhoBTP_v02.Core.TTNVL
    $nvl.BinNVL = $bin; $nvl.CdBatDau = $start; $nvl.CdConLai = $end
    $nvl.KlBatDau = 200; $nvl.KlConLai = 100; $nvl.QC = 'OK'; $nvl.DonVi = 'M'
    return $nvl
}

function Sql($conn, [string]$sql) {
    $cmd = $conn.CreateCommand()
    try { $cmd.CommandText = $sql; return $cmd.ExecuteScalar() }
    finally { $cmd.Dispose() }
}

function With-Database([scriptblock]$check) {
    $conn = New-Object System.Data.SQLite.SQLiteConnection('Data Source=:memory:;Version=3;')
    $conn.Open()
    try {
        $null = Sql $conn @'
CREATE TABLE TTThanhPham(id INTEGER PRIMARY KEY, MaBin TEXT, DanhSachSP_ID INTEGER,
  KhoiLuongTruoc REAL, KhoiLuongSau REAL, ChieuDaiTruoc REAL, ChieuDaiSau REAL,
  LastEdit_id INTEGER, QC TEXT, Temp INTEGER DEFAULT 0, Active INTEGER DEFAULT 1,
  HanNoi INTEGER DEFAULT 0, GhiChu TEXT, CongDoan INTEGER, NhapKho INTEGER DEFAULT 0);
CREATE TABLE TTNVL(id INTEGER PRIMARY KEY, TTThanhPham_ID INTEGER, BinNVL TEXT,
  KlBatDau REAL, KlConLai REAL, CdBatDau REAL, CdConLai REAL);
INSERT INTO TTThanhPham VALUES(1,'A',10,300,80,600,80,3,'OK',0,1,0,'',1,0);
INSERT INTO TTThanhPham VALUES(2,'P',11,0,0,200,200,0,'OK',0,1,0,'',2,0);
INSERT INTO TTThanhPham VALUES(3,'NEXT',11,0,0,40,40,0,'OK',0,1,0,'',2,0);
INSERT INTO TTThanhPham VALUES(4,'B',10,200,200,600,600,0,'OK',0,1,0,'',1,0);
INSERT INTO TTNVL VALUES(1,2,'A',200,100,398,120);
INSERT INTO TTNVL VALUES(2,3,'A',100,80,120,80);
'@
        $tx = $conn.BeginTransaction()
        try { & $check $conn $tx }
        finally { $tx.Rollback(); $tx.Dispose() }
    }
    finally { $conn.Dispose() }
}

Test-Case 'Edit keeps the confirmed 80 and latest-use marker when history changes to 100' {
    With-Database { param($conn,$tx)
        $items = New-Object 'System.Collections.Generic.List[DG_TonKhoBTP_v02.Core.TTNVL]'
        $items.Add((New-Nvl 'A' 398 100))
        Invoke-Db 'UpdateKhoiLuongConLai_TTThanhPham' @($conn,$tx,$items,[long]2)
        Assert-Equal 80 (Sql $conn "SELECT ChieuDaiSau FROM TTThanhPham WHERE id=1") 'Later stock'
        Assert-Equal 80 (Sql $conn "SELECT KhoiLuongSau FROM TTThanhPham WHERE id=1") 'Later weight'
        Assert-Equal 3 (Sql $conn "SELECT LastEdit_id FROM TTThanhPham WHERE id=1") 'Latest use'
    }
}

Test-Case 'Edit without quantity changes cannot restore historical 120 over current 80' {
    With-Database { param($conn,$tx)
        $items = New-Object 'System.Collections.Generic.List[DG_TonKhoBTP_v02.Core.TTNVL]'
        $items.Add((New-Nvl))
        Invoke-Db 'UpdateKhoiLuongConLai_TTThanhPham' @($conn,$tx,$items,[long]2)
        Assert-Equal 80 (Sql $conn "SELECT ChieuDaiSau FROM TTThanhPham WHERE id=1") 'Unchanged save'
    }
}

Test-Case 'Deleting the latest use restores its start even with an empty NVL list' {
    With-Database { param($conn,$tx)
        $null = Sql $conn 'DELETE FROM TTNVL WHERE TTThanhPham_ID=3; UPDATE TTThanhPham SET ChieuDaiSau=120,KhoiLuongSau=100,LastEdit_id=2 WHERE id=1;'
        $items = New-Object 'System.Collections.Generic.List[DG_TonKhoBTP_v02.Core.TTNVL]'
        Invoke-Db 'UpdateKhoiLuongConLai_TTThanhPham' @($conn,$tx,$items,[long]2)
        Assert-Equal 398 (Sql $conn "SELECT ChieuDaiSau FROM TTThanhPham WHERE id=1") 'Restored start'
    }
}

Test-Case 'Deleting an old use leaves later stock untouched' {
    With-Database { param($conn,$tx)
        $items = New-Object 'System.Collections.Generic.List[DG_TonKhoBTP_v02.Core.TTNVL]'
        Invoke-Db 'UpdateKhoiLuongConLai_TTThanhPham' @($conn,$tx,$items,[long]2)
        Assert-Equal 80 (Sql $conn "SELECT ChieuDaiSau FROM TTThanhPham WHERE id=1") 'Protected deleted source'
        Assert-Equal 3 (Sql $conn "SELECT LastEdit_id FROM TTThanhPham WHERE id=1") 'Protected latest use'
    }
}

Test-Case 'One edit can preserve old A while consuming newly added B' {
    With-Database { param($conn,$tx)
        $items = New-Object 'System.Collections.Generic.List[DG_TonKhoBTP_v02.Core.TTNVL]'
        $items.Add((New-Nvl 'A' 398 100)); $items.Add((New-Nvl 'B' 600 398))
        Invoke-Db 'UpdateKhoiLuongConLai_TTThanhPham' @($conn,$tx,$items,[long]2)
        Assert-Equal 80 (Sql $conn "SELECT ChieuDaiSau FROM TTThanhPham WHERE id=1") 'A stays protected'
        Assert-Equal 398 (Sql $conn "SELECT ChieuDaiSau FROM TTThanhPham WHERE id=4") 'B is consumed'
        Assert-Equal 2 (Sql $conn "SELECT LastEdit_id FROM TTThanhPham WHERE id=4") 'B belongs to edit'
    }
}

Test-Case 'Newly added NVL with changed source stock rejects save before writing' {
    With-Database { param($conn,$tx)
        $null = Sql $conn 'UPDATE TTThanhPham SET ChieuDaiSau=500 WHERE id=4;'
        $items = New-Object 'System.Collections.Generic.List[DG_TonKhoBTP_v02.Core.TTNVL]'
        $items.Add((New-Nvl 'B' 600 398))
        $rejected = $false
        try { Invoke-Db 'UpdateKhoiLuongConLai_TTThanhPham' @($conn,$tx,$items,[long]2) }
        catch { $rejected = $true }
        Assert-Equal $true $rejected 'Stale snapshot must be rejected'
        Assert-Equal 500 (Sql $conn "SELECT ChieuDaiSau FROM TTThanhPham WHERE id=4") 'No stale write'
    }
}

function Edited-Product([string]$bin = 'A') {
    $tp = New-Object DG_TonKhoBTP_v02.Core.TTThanhPham
    $tp.MaBin=$bin; $tp.DanhSachSP_ID=10; $tp.ChieuDaiTruoc=300; $tp.ChieuDaiSau=300
    $tp.KhoiLuongTruoc=250; $tp.KhoiLuongSau=250; $tp.GhiChu='Edited'
    return $tp
}

Test-Case 'Editing a consumed product changes original amounts but preserves current stock' {
    With-Database { param($conn,$tx)
        $items = New-Object 'System.Collections.Generic.List[DG_TonKhoBTP_v02.Core.TTNVL]'
        Invoke-Db 'UpdateTTThanhPham' @($conn,$tx,(Edited-Product),[int]1,$items)
        Assert-Equal 300 (Sql $conn 'SELECT ChieuDaiTruoc FROM TTThanhPham WHERE id=1') 'Edited original length'
        Assert-Equal 80 (Sql $conn 'SELECT ChieuDaiSau FROM TTThanhPham WHERE id=1') 'Confirmed current length'
        Assert-Equal 80 (Sql $conn 'SELECT KhoiLuongSau FROM TTThanhPham WHERE id=1') 'Confirmed current weight'
        Assert-Equal 3 (Sql $conn 'SELECT LastEdit_id FROM TTThanhPham WHERE id=1') 'Product latest use'
    }
}

Test-Case 'Consumed product cannot be renamed even if called outside the UI' {
    With-Database { param($conn,$tx)
        $items = New-Object 'System.Collections.Generic.List[DG_TonKhoBTP_v02.Core.TTNVL]'
        $rejected=$false
        try { Invoke-Db 'UpdateTTThanhPham' @($conn,$tx,(Edited-Product 'RENAMED'),[int]1,$items) }
        catch { $rejected=$true }
        Assert-Equal $true $rejected 'Rename must be rejected'
        Assert-Equal 'A' (Sql $conn 'SELECT MaBin FROM TTThanhPham WHERE id=1') 'Source identity'
    }
}

Test-Case 'Unused product can change its code and both original and current amounts' {
    With-Database { param($conn,$tx)
        $items = New-Object 'System.Collections.Generic.List[DG_TonKhoBTP_v02.Core.TTNVL]'
        Invoke-Db 'UpdateTTThanhPham' @($conn,$tx,(Edited-Product 'RENAMED'),[int]4,$items)
        Assert-Equal 'RENAMED' (Sql $conn 'SELECT MaBin FROM TTThanhPham WHERE id=4') 'Unused rename'
        Assert-Equal 300 (Sql $conn 'SELECT ChieuDaiSau FROM TTThanhPham WHERE id=4') 'Unused current length'
    }
}

function New-ProductData {
    $tp = New-Object DG_TonKhoBTP_v02.Models.ThanhPhamData
    $tp.DanhSachSPId=10; $tp.DonVi='M'; $tp.ChieuDai=200; $tp.KhoiLuong=0; $tp.ChuyenDoi=1
    $tp.BomComponents = New-Object 'System.Collections.Generic.List[DG_TonKhoBTP_v02.Models.BomComponentData]'
    $bom = New-Object DG_TonKhoBTP_v02.Models.BomComponentData; $bom.ComponentId=10
    $tp.BomComponents.Add($bom)
    return $tp
}

function With-NvlControl([int]$mode, [scriptblock]$check) {
    $cd = [DG_TonKhoBTP_v02.Dictionary.ThongTinChungCongDoan]::BocMach
    $writer=[Console]::Out
    try { [Console]::SetOut([IO.TextWriter]::Null); $control = New-Object DG_TonKhoBTP_v02.UI.UC_TTNVL($cd.Columns,$cd) }
    finally { [Console]::SetOut($writer) }
    $script:product = New-ProductData
    $control.GetThanhPhamData = [Func[DG_TonKhoBTP_v02.Models.ThanhPhamData]]{ return $script:product }
    try {
        (Field $control 'isEdit').Value=$mode
        & $check $control
    }
    finally { $control.Dispose() }
}

function Add-NvlRow($control, [string]$bin='A', [double]$start=600) {
    $row = New-Object DG_TonKhoBTP_v02.Core.TTNVLRow
    $row.BinNVL=$bin; $row.DonVi='M'; $row.CdBatDau=$start; $row.CdConLai=$start
    $row.KlBatDau=0; $row.KlConLai=0
    (Field $control '_nvlRows').Add($row)
    return $row
}

foreach ($mode in @(0,2,1)) {
    Test-Case "Mode $mode recalculates 200,400,200 from a fixed 600 without repeated subtraction" {
        With-NvlControl $mode { param($control)
            $row = Add-NvlRow $control
            foreach ($entry in @(@(200,398),@(400,196),@(200,398))) {
                $script:product.ChieuDai=$entry[0]
                $control.OnThanhPhamSoLieuChanged($script:product)
                Assert-Equal $entry[1] $row.CdConLai 'Length from fixed start'
            }
        }
    }
    Test-Case "Mode $mode returns automatic NVL to its start when required product amount is zero" {
        With-NvlControl $mode { param($control)
            $row=Add-NvlRow $control
            $control.OnThanhPhamSoLieuChanged($script:product)
            $script:product.ChieuDai=0
            $control.OnThanhPhamSoLieuChanged($script:product)
            Assert-Equal 600 $row.CdConLai 'Zero product has no length consumption'
        }
    }
}

Test-Case 'Removing A does not subtract product length twice from B' {
    With-NvlControl 0 { param($control)
        $a=Add-NvlRow $control 'A' 600; $b=Add-NvlRow $control 'B' 500
        $control.OnThanhPhamSoLieuChanged($script:product)
        $null=(Field $control '_nvlRows').Remove($a)
        $reasonType=$control.GetType().GetNestedType('LyDoCapNhatGiaTriConLai',[Reflection.BindingFlags]'NonPublic')
        $reason=[Enum]::Parse($reasonType,'ThemHoacXoaDong')
        Invoke-Private $control 'CapNhatGiaTriConLaiToanBang' @($script:product,$reason)
        Assert-Equal 298 $b.CdConLai 'B retains one consumption'
    }
}

Test-Case 'Drawing recalculates weight from its original stock without changing the stage formula' {
    With-NvlControl 0 { param($control)
        $control.GetType().GetField('_CD',$instanceFlags).SetValue($control,[DG_TonKhoBTP_v02.Dictionary.ThongTinChungCongDoan]::KeoRut)
        $script:product.DonVi='KG'; $script:product.TenTP='C 1.00'
        $row=Add-NvlRow $control; $row.DonVi='KG'; $row.TenNVL='C 1.00'; $row.KlBatDau=1000; $row.KlConLai=1000
        foreach($entry in @(@(200,800),@(400,600),@(200,800),@(0,1000))) {
            $script:product.KhoiLuong=$entry[0]
            $control.OnThanhPhamSoLieuChanged($script:product)
            Assert-Equal $entry[1] $row.KlConLai 'Drawing weight from fixed start'
        }
    }
}

Test-Case 'Existing mixed-unit conversion recalculates both length and weight from fixed stock' {
    With-NvlControl 0 { param($control)
        $script:product.ChuyenDoi=2
        $row=Add-NvlRow $control; $row.DonVi='KG'; $row.KlBatDau=1000; $row.KlConLai=1000
        $control.OnThanhPhamSoLieuChanged($script:product)
        Assert-Equal 600 $row.KlConLai '200m consumes 400kg'
        Assert-Equal 400 $row.CdConLai '200m length consumption'
        $script:product.ChieuDai=100
        $control.OnThanhPhamSoLieuChanged($script:product)
        Assert-Equal 800 $row.KlConLai '100m consumes 200kg from original stock'
        Assert-Equal 500 $row.CdConLai '100m length consumption from original stock'
    }
}

function Product-Table([bool]$used=$false) {
    $table=New-Object System.Data.DataTable
    foreach ($name in @('MaBin','Ten','Ma','donvi')) { $null=$table.Columns.Add($name,[string]) }
    foreach ($name in @('KhoiLuongTruoc','ChieuDaiTruoc','ChuyenDoi')) { $null=$table.Columns.Add($name,[decimal]) }
    $null=$table.Columns.Add('DanhSachMaSP_ID',[int]); $null=$table.Columns.Add('ThanhPhamDaDuocSuDung',[bool])
    $r=$table.NewRow(); $r.MaBin='E1-123/1-01-01'; $r.Ten='Product'; $r.Ma='P'; $r.donvi='M'
    $r.KhoiLuongTruoc=100; $r.ChieuDaiTruoc=200; $r.ChuyenDoi=1; $r.DanhSachMaSP_ID=10; $r.ThanhPhamDaDuocSuDung=$used
    $table.Rows.Add($r)
    return ,$table
}

foreach ($mode in @(0,2,1)) {
    Test-Case "Mode $mode treats an empty NumericUpDown text as zero immediately" {
        $cd=[DG_TonKhoBTP_v02.Dictionary.ThongTinChungCongDoan]::BocMach
        $control=New-Object DG_TonKhoBTP_v02.UI.UC_TTThanhPham($cd)
        try {
            if ($mode -ne 0) { $control.LoadData((Product-Table),$mode) }
            $length=Field $control 'chieuDai'; $weight=Field $control 'khoiLuong'
            $length.Value=200; $weight.Value=100
            $length.Text=''; $weight.Text=''
            Assert-Equal 0 $control.GetThanhPhamData().ChieuDai 'Empty length'
            Assert-Equal 0 $control.GetThanhPhamData().KhoiLuong 'Empty weight'
        }
        finally { $control.Dispose() }
    }
}

Test-Case 'Edit locks the consumed product code but copy remains editable' {
    $cd=[DG_TonKhoBTP_v02.Dictionary.ThongTinChungCongDoan]::BocMach
    $control=New-Object DG_TonKhoBTP_v02.UI.UC_TTThanhPham($cd)
    try {
        $control.LoadData((Product-Table $true),2)
        Assert-Equal $false (Field $control 'maHanhTrinh').Enabled 'Consumed edit code'
        Assert-Equal $true (Field $control 'chieuDai').Enabled 'Quantity remains editable'
        $originalCode=$control.GetThanhPhamData().SoLOT
        $control.ChonMay('OTHER')
        Assert-Equal $originalCode $control.GetThanhPhamData().SoLOT 'Programmatic machine changes cannot rename a consumed product'
        (Field $control 'chieuDai').Value=400
        Assert-Equal 400 $control.GetThanhPhamData().ChieuDai 'Locked code does not lock quantities'
        $control.LoadData((Product-Table $true),1)
        Assert-Equal $true (Field $control 'maHanhTrinh').Enabled 'Copy code'
    }
    finally { $control.Dispose() }
}

foreach($entry in @(
    @('M',0,100,1), @('M',100,0,0),
    @('KG',100,0,1), @('KG',0,100,0)
)) {
    Test-Case "Official validation keeps required amounts for unit $($entry[0]) (length=$($entry[1]), weight=$($entry[2]))" {
        $tp=New-Object DG_TonKhoBTP_v02.Core.TTThanhPham
        $tp.DanhSachSP_ID=10; $tp.MaBin='VALIDATION'; $tp.DonVi=$entry[0]
        $tp.ChieuDaiSau=$entry[1]; $tp.KhoiLuongSau=$entry[2]
        $errors=[DG_TonKhoBTP_v02.Helper.Validator]::LayDanhSachLoiTTThanhPham($tp)
        Assert-Equal $entry[3] $errors.Count 'Required amount validation'
    }
}

function Material-Table([string]$bin='A', [int]$sourceId=1, [double]$start=398, [double]$end=120) {
    $table=New-Object System.Data.DataTable
    foreach ($name in @('MaBin','BinNVL','DonViNVL','TenNVL','QC')) { $null=$table.Columns.Add($name,[string]) }
    foreach ($name in @('KlBatDau','KlConLai','CdBatDau','CdConLai')) { $null=$table.Columns.Add($name,[double]) }
    foreach ($name in @('id','NVL_DanhSachMaSP_ID')) { $null=$table.Columns.Add($name,[int]) }
    $r=$table.NewRow(); $r.MaBin='E4-123/1-01-01'; $r.BinNVL=$bin; $r.DonViNVL='M'; $r.TenNVL='Material'; $r.QC='OK'
    $r.KlBatDau=0; $r.KlConLai=0; $r.CdBatDau=$start; $r.CdConLai=$end; $r.id=$sourceId; $r.NVL_DanhSachMaSP_ID=10
    $table.Rows.Add($r)
    return ,$table
}

function Load-EditMaterials($control) {
    $null=$control.Handle; $null=(Field $control 'dtgTTNVL').Handle
    $control.LoadData((Material-Table),2)
    [Windows.Forms.Application]::DoEvents()
}

Test-Case 'Opening edit preserves historical 120 instead of recalculating it' {
    With-NvlControl 2 { param($control)
        Load-EditMaterials $control
        Assert-Equal 120 (Field $control '_nvlRows')[0].CdConLai 'Initial history'
    }
}

Test-Case 'Adding new B preserves unchanged historical A and calculates B from current stock' {
    With-NvlControl 2 { param($control)
        Load-EditMaterials $control
        Invoke-Private $control 'AddRowsToGrid' @((Material-Table 'B' 4 600 600),$script:product)
        $rows=Field $control '_nvlRows'
        Assert-Equal 120 $rows[0].CdConLai 'Unchanged A history'
        Assert-Equal 398 $rows[1].CdConLai 'New B consumption'
    }
}

Test-Case 'Deleting and re-adding old A keeps its historical start rather than current source 80' {
    With-NvlControl 2 { param($control)
        Load-EditMaterials $control
        (Field $control '_nvlRows').Clear()
        Invoke-Private $control 'AddRowsToGrid' @((Material-Table 'A' 1 80 80),$script:product)
        $row=(Field $control '_nvlRows')[0]
        Assert-Equal 398 $row.CdBatDau 'Old A start'
        Assert-Equal 120 $row.CdConLai 'Unchanged A history'
    }
}

Test-Case 'Original-bin lookup restores exhausted A without needing a current-stock query' {
    With-NvlControl 2 { param($control)
        Load-EditMaterials $control
        (Field $control '_nvlRows').Clear()
        $method=$control.GetType().GetMethod('ThuThemLaiNVLCu',$instanceFlags)
        if ($null -eq $method) { throw 'Original bins still depend on the current-stock scan filter' }
        $restored=Invoke-Private $control 'ThuThemLaiNVLCu' @('A',$script:product)
        Assert-Equal $true $restored 'Historical lookup handles original bin'
        $row=(Field $control '_nvlRows')[0]
        Assert-Equal 398 $row.CdBatDau 'Original start survives exhausted source'
        Assert-Equal 120 $row.CdConLai 'Original result survives exhausted source'
        Assert-Equal $false (Invoke-Private $control 'ThuThemLaiNVLCu' @('NEW',$script:product)) 'New bins still require current-stock lookup'
    }
}

Test-Case 'Adding B to unchanged historical zero-quantity edit preserves A history' {
    With-NvlControl 2 { param($control)
        $script:product.ChieuDai=0
        Load-EditMaterials $control
        Invoke-Private $control 'AddRowsToGrid' @((Material-Table 'B' 4 600 600),$script:product)
        $rows=Field $control '_nvlRows'
        Assert-Equal 120 $rows[0].CdConLai 'Unchanged history with missing quantity'
        Assert-Equal 600 $rows[1].CdConLai 'New B has no consumption yet'
    }
}

Test-Case 'Copy on a machine without material reuse starts with an empty material grid' {
    With-NvlControl 1 { param($control)
        $null=$control.Handle; $null=(Field $control 'dtgTTNVL').Handle
        $control.LoadData((Material-Table),1)
        [Windows.Forms.Application]::DoEvents()
        Assert-Equal 0 (Field $control '_nvlRows').Count 'E4 copy must not retain original materials'
    }
}

Test-Case 'Copy on a reuse-enabled machine computes from the refreshed snapshot' {
    With-NvlControl 1 { param($control)
        $table=Material-Table 'A' 1 600 600
        $table.Rows[0].MaBin='B10-123/1-01-01'
        $null=$table.Columns.Add('CopyNvlUnavailable',[bool]); $table.Rows[0].CopyNvlUnavailable=$false
        $null=$control.Handle; $null=(Field $control 'dtgTTNVL').Handle
        $control.LoadData($table,1)
        [Windows.Forms.Application]::DoEvents()
        $row=(Field $control '_nvlRows')[0]
        Assert-Equal 600 $row.CdBatDau 'Refreshed source snapshot'
        Assert-Equal 398 $row.CdConLai 'Initial copied output consumption'
        $script:product.ChieuDai=400; $control.OnThanhPhamSoLieuChanged($script:product)
        Assert-Equal 196 $row.CdConLai 'Copied output change is not cumulative'
    }
}

if ($ReadOnlyDatabasePath) {
    Test-Case 'Actual report 56771 cannot overwrite stock left by report 56799; used-product flag loads correctly' {
        $cs=New-Object System.Data.SQLite.SQLiteConnectionStringBuilder
        $cs.DataSource=(Resolve-Path -LiteralPath $ReadOnlyDatabasePath).Path
        $cs.ReadOnly=$true; $cs.FailIfMissing=$true
        $source=New-Object System.Data.SQLite.SQLiteConnection($cs.ConnectionString)
        $memory=New-Object System.Data.SQLite.SQLiteConnection('Data Source=:memory:;Version=3;')
        $previous=[DG_TonKhoBTP_v02.Database.DatabaseHelper]::_connStr
        try {
            $source.Open(); $memory.Open()
            $source.BackupDatabase($memory,'main','main',-1,$null,0)
            $source.Close()
            [DG_TonKhoBTP_v02.Database.DatabaseHelper]::_connStr=$cs.ConnectionString
            $cd=@([DG_TonKhoBTP_v02.Dictionary.ThongTinChungCongDoan]::TatCaCongDoan | Where-Object { $_.Id -eq 1 })[0]
            $loaded=[DG_TonKhoBTP_v02.Database.DatabaseHelper]::GetDataByID('56073',$cd,2)
            Assert-Equal $true ([bool]$loaded.Rows[0].ThanhPhamDaDuocSuDung) 'Real source is used'

            $before=Sql $memory 'SELECT ChieuDaiSau FROM TTThanhPham WHERE id=56073'
            $beforeWeight=Sql $memory 'SELECT KhoiLuongSau FROM TTThanhPham WHERE id=56073'
            $beforeLatest=Sql $memory 'SELECT LastEdit_id FROM TTThanhPham WHERE id=56073'
            $items=New-Object 'System.Collections.Generic.List[DG_TonKhoBTP_v02.Core.TTNVL]'
            $cmd=$memory.CreateCommand(); $cmd.CommandText='SELECT * FROM TTNVL WHERE TTThanhPham_ID=56771'
            $reader=$cmd.ExecuteReader()
            while($reader.Read()) {
                $nvl=New-Object DG_TonKhoBTP_v02.Core.TTNVL
                $nvl.BinNVL=[string]$reader['BinNVL']; $nvl.QC=[string]$reader['QC']
                foreach($name in @('KlBatDau','KlConLai','CdBatDau','CdConLai')) {
                    if($reader[$name] -ne [DBNull]::Value) { $nvl.$name=[double]$reader[$name] }
                }
                $items.Add($nvl)
            }
            $reader.Dispose(); $cmd.Dispose()
            $tx=$memory.BeginTransaction()
            try {
                Invoke-Db 'UpdateKhoiLuongConLai_TTThanhPham' @($memory,$tx,$items,[long]56771)
                Assert-Equal $before (Sql $memory 'SELECT ChieuDaiSau FROM TTThanhPham WHERE id=56073') 'Real current length'
                Assert-Equal $beforeWeight (Sql $memory 'SELECT KhoiLuongSau FROM TTThanhPham WHERE id=56073') 'Real current weight'
                Assert-Equal $beforeLatest (Sql $memory 'SELECT LastEdit_id FROM TTThanhPham WHERE id=56073') 'Real latest-use marker'
                Write-Output "SNAPSHOT: source 56073 remains $before m; LastEdit_id=$beforeLatest"
            }
            finally { $tx.Rollback(); $tx.Dispose() }
        }
        finally {
            [DG_TonKhoBTP_v02.Database.DatabaseHelper]::_connStr=$previous
            $memory.Dispose(); $source.Dispose()
        }
    }
}

Write-Output "RESULT: $script:passed passed, $script:failed failed"
if ($script:failed -gt 0) { exit 1 }
