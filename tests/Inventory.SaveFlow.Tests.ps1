param(
    [string]$AssemblyPath = (Join-Path $PSScriptRoot '..\DG_TonKhoBTP_v02\bin\Debug\DG_TonKhoBTP_v02.exe'),
    [string]$ReadOnlyDatabasePath
)

# Exercises the actual public save transactions against disposable database clones.
# Supply -ReadOnlyDatabasePath explicitly; the source is always opened read-only.
if (-not $ReadOnlyDatabasePath) { Write-Output 'SKIP: supply -ReadOnlyDatabasePath for full save-flow verification'; exit 0 }
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Data
$assembly=[Reflection.Assembly]::LoadFrom((Resolve-Path $AssemblyPath).Path)
$null=[Reflection.Assembly]::LoadFrom((Join-Path (Split-Path $AssemblyPath) 'System.Data.SQLite.dll'))
$dbType=$assembly.GetType('DG_TonKhoBTP_v02.Database.SanXuat.SubmitForm_DB',$true)
$script:passed=0; $script:failed=0
$tempRoot=Join-Path ([IO.Path]::GetTempPath()) ('Codex.Inventory.'+[Guid]::NewGuid().ToString('N'))
$null=[IO.Directory]::CreateDirectory($tempRoot)
$template=Join-Path $tempRoot 'template.db'
$oldBase=[DG_TonKhoBTP_v02.Database.DB_Base]::_connStr
$oldHelper=[DG_TonKhoBTP_v02.Database.DatabaseHelper]::_connStr

function Assert-Equal($expected,$actual,[string]$message) {
    if ($expected -ne $actual) { throw "$message (expected '$expected', actual '$actual')" }
}

function Table($conn,[string]$sql) {
    $cmd=$conn.CreateCommand(); $cmd.CommandText=$sql
    $adapter=New-Object System.Data.SQLite.SQLiteDataAdapter($cmd)
    $table=New-Object System.Data.DataTable
    try { $null=$adapter.Fill($table); return ,$table }
    finally { $adapter.Dispose(); $cmd.Dispose() }
}

function Scalar($conn,[string]$sql) {
    $cmd=$conn.CreateCommand(); $cmd.CommandText=$sql
    try { return $cmd.ExecuteScalar() }
    finally { $cmd.Dispose() }
}

function Model([string]$typeName,$row,[hashtable]$aliases=@{}) {
    $type=$assembly.GetType($typeName,$true)
    $model=[Activator]::CreateInstance($type,$true)
    foreach($prop in $type.GetProperties()) {
        $name=$prop.Name; $column=$name
        if($aliases.ContainsKey($name)) { $column=$aliases[$name] }
        if(-not $row.Table.Columns.Contains($column) -or $row[$column] -eq [DBNull]::Value) { continue }
        $value=$row[$column]
        if($name -eq 'CongDoan' -and $prop.PropertyType.Name -eq 'CongDoan') {
            $value=@([DG_TonKhoBTP_v02.Dictionary.ThongTinChungCongDoan]::TatCaCongDoan | Where-Object { $_.Id -eq [int]$value })[0]
        } else {
            $target=[Nullable]::GetUnderlyingType($prop.PropertyType)
            if($null -eq $target) { $target=$prop.PropertyType }
            if($target -eq [TimeSpan]) { $value=[TimeSpan]::Parse([string]$value,[Globalization.CultureInfo]::InvariantCulture) }
            else { $value=[Convert]::ChangeType($value,$target,[Globalization.CultureInfo]::InvariantCulture) }
        }
        $model.$name=$value
    }
    return ,$model
}

function State($conn,[int]$id) {
    $row=(Table $conn "SELECT tp.*,ds.Ten AS TenTP,ds.Ma AS MaTP,ds.DonVi FROM TTThanhPham tp LEFT JOIN DanhSachMaSP ds ON ds.id=tp.DanhSachSP_ID WHERE tp.id=$id").Rows[0]
    $tp=Model 'DG_TonKhoBTP_v02.Core.TTThanhPham' $row
    # Match the UI snapshot: entered original quantities occupy both model fields.
    $tp.ChieuDaiSau=$tp.ChieuDaiTruoc; $tp.KhoiLuongSau=$tp.KhoiLuongTruoc
    $phe=Table $conn "SELECT * FROM PheLieu WHERE TTThanhPham_ID=$id"
    if($phe.Rows.Count) { $tp.PheLieu=Model 'DG_TonKhoBTP_v02.Core.PheLieuData' $phe.Rows[0] }
    $ca=Model 'DG_TonKhoBTP_v02.Core.ThongTinCaLamViec' (Table $conn "SELECT * FROM ThongTinCaLamViec WHERE TTThanhPham_id=$id").Rows[0] @{DanhSachMayId='DanhSachMay_ID'}
    $materials=New-Object 'System.Collections.Generic.List[DG_TonKhoBTP_v02.Core.TTNVL]'
    foreach($nvlRow in (Table $conn "SELECT * FROM TTNVL WHERE TTThanhPham_ID=$id ORDER BY id").Rows) {
        $materials.Add((Model 'DG_TonKhoBTP_v02.Core.TTNVL' $nvlRow @{TTThanhPhan_ID='TTThanhPham_ID'}))
    }
    $detail=[Activator]::CreateInstance($assembly.GetType('DG_TonKhoBTP_v02.Models.SanXuat.SubmitCongDoanData'),$true)
    if($tp.CongDoan.Id -eq 1) {
        $stage=Table $conn "SELECT * FROM CD_BenRuot WHERE TTThanhPham_ID=$id"
        if($stage.Rows.Count) { $detail.ChiTietCongDoan=Model 'DG_TonKhoBTP_v02.Core.CD_BenRuot' $stage.Rows[0] }
        else { $detail.ChiTietCongDoan=New-Object DG_TonKhoBTP_v02.Core.CD_BenRuot }
    } else {
        $detail.ChiTietCongDoan=New-Object DG_TonKhoBTP_v02.Core.CD_BocMach
        $settings=Table $conn "SELECT * FROM CaiDatCDBoc WHERE TTThanhPham_ID=$id"
        if($settings.Rows.Count) {
            $detail.CaiDatCDBoc=Model 'DG_TonKhoBTP_v02.Core.CaiDatCDBoc' $settings.Rows[0]
            $stage=Table $conn "SELECT mach.* FROM CD_BocMach mach JOIN CaiDatCDBoc c ON c.id=mach.CaiDatCDBoc_ID WHERE c.TTThanhPham_ID=$id"
            if($stage.Rows.Count) { $detail.ChiTietCongDoan=Model 'DG_TonKhoBTP_v02.Core.CD_BocMach' $stage.Rows[0] }
        }
    }
    $errorType=$assembly.GetType('DG_TonKhoBTP_v02.Models.SanXuat.LoiNhapLieuData')
    $errors=[Activator]::CreateInstance([System.Collections.Generic.List`1].MakeGenericType($errorType))
    return [pscustomobject]@{ Id=$id; Product=$tp; Shift=$ca; Materials=$materials; Detail=$detail; Errors=$errors }
}

function Save($state,[switch]$Stage9) {
    if($Stage9) {
        $name='UpdateDataCongDoan9'
        [object[]]$arguments=@([int]$state.Id,$state.Shift,$state.Product,'VERIFICATION',$null)
    } else {
        $name='UpdateDataSanPhamVoiDanhSachLoiNhapLieu'
        [object[]]$arguments=@([int]$state.Id,$state.Shift,$state.Product,$state.Materials,$state.Detail,$state.Errors,'VERIFICATION',$null)
    }
    for($i=0;$i -lt $arguments.Length;$i++) {
        if($null -ne $arguments[$i]) { $arguments[$i]=$arguments[$i].PSObject.BaseObject }
    }
    $success=$dbType.GetMethod($name,[Reflection.BindingFlags]'Static,Public').Invoke($null,$arguments)
    return [pscustomobject]@{Success=$success; Error=$arguments[$arguments.Length-1]}
}

function Assert-Saved($result) { Assert-Equal $true $result.Success ('Save failed: '+$result.Error) }

function Save-New($state,[switch]$VerifyInventory) {
    if($VerifyInventory) {
        $name='SaveDataSanPhamTrucTiepVoiKiemTraTon'
        [object[]]$arguments=@($state.Shift,$state.Product,$state.Materials,$state.Detail,$state.Errors,$false,$null)
    } else {
        $name='SaveDataSanPhamVoiDanhSachLoiNhapLieu'
        [object[]]$arguments=@($state.Shift,$state.Product,$state.Materials,$state.Detail,$state.Errors,$null)
    }
    for($i=0;$i -lt $arguments.Length;$i++) {
        if($null -ne $arguments[$i]) { $arguments[$i]=$arguments[$i].PSObject.BaseObject }
    }
    $success=$dbType.GetMethod($name,[Reflection.BindingFlags]'Static,Public').Invoke($null,$arguments)
    return [pscustomobject]@{ Success=$success; Error=$arguments[$arguments.Length-1]; Conflict=($VerifyInventory -and $arguments[5]) }
}

function New-Material($conn,[int]$id) {
    $row=(Table $conn @"
SELECT tp.MaBin AS BinNVL,tp.DanhSachSP_ID AS DanhSachMaSP_ID,tp.QC,
       tp.KhoiLuongSau AS KlBatDau,tp.KhoiLuongSau AS KlConLai,
       tp.ChieuDaiSau AS CdBatDau,tp.ChieuDaiSau AS CdConLai,ds.DonVi
FROM TTThanhPham tp JOIN DanhSachMaSP ds ON ds.id=tp.DanhSachSP_ID WHERE tp.id=$id
"@).Rows[0]
    return ,(Model 'DG_TonKhoBTP_v02.Core.TTNVL' $row)
}

function Test-Save([string]$name,[scriptblock]$check) {
    $path=Join-Path $tempRoot ([Guid]::NewGuid().ToString('N')+'.db')
    [IO.File]::Copy($template,$path)
    $builder=New-Object System.Data.SQLite.SQLiteConnectionStringBuilder
    $builder.DataSource=[string]$path; $builder.Pooling=$false
    [DG_TonKhoBTP_v02.Database.DB_Base]::_connStr=$builder.ConnectionString
    [DG_TonKhoBTP_v02.Database.DatabaseHelper]::_connStr=$builder.ConnectionString
    $conn=New-Object System.Data.SQLite.SQLiteConnection($builder.ConnectionString)
    try {
        $conn.Open(); & $check $conn
        $script:passed++; Write-Output "PASS: $name"
    } catch { $script:failed++; Write-Output "FAIL: $name - $($_.Exception.Message)" }
    finally { $conn.Dispose() }
}

try {
    $builder=New-Object System.Data.SQLite.SQLiteConnectionStringBuilder
    $builder.DataSource=(Resolve-Path -LiteralPath $ReadOnlyDatabasePath).Path
    $builder.ReadOnly=$true; $builder.FailIfMissing=$true; $builder.Pooling=$false
    $source=New-Object System.Data.SQLite.SQLiteConnection($builder.ConnectionString)
    $destination=New-Object System.Data.SQLite.SQLiteConnection("Data Source=$template;Pooling=False;")
    try { $source.Open(); $destination.Open(); $source.BackupDatabase($destination,'main','main',-1,$null,0) }
    finally { $destination.Dispose(); $source.Dispose() }

    Test-Save 'Unchanged edit commits history and preserves later source stock' {
        param($conn)
        $state=State $conn 56771
        $stock=Scalar $conn 'SELECT ChieuDaiSau FROM TTThanhPham WHERE id=56073'
        $audits=Scalar $conn 'SELECT count(*) FROM LichSuSuaDoiThongTin WHERE TTThanhPham_ID=56771'
        Assert-Saved (Save $state)
        Assert-Equal $stock (Scalar $conn 'SELECT ChieuDaiSau FROM TTThanhPham WHERE id=56073') 'Final source stock'
        Assert-Equal 56799 (Scalar $conn 'SELECT LastEdit_id FROM TTThanhPham WHERE id=56073') 'Latest use'
        Assert-Equal ($audits+1) (Scalar $conn 'SELECT count(*) FROM LichSuSuaDoiThongTin WHERE TTThanhPham_ID=56771') 'Audit committed'
    }

    Test-Save 'Historical remaining amount changes while later physical stock stays unchanged' {
        param($conn)
        $state=State $conn 56771; $state.Materials[0].CdConLai=9000
        Assert-Saved (Save $state)
        Assert-Equal 9000 (Scalar $conn 'SELECT CdConLai FROM TTNVL WHERE TTThanhPham_ID=56771') 'New history saved'
        Assert-Equal 8930 (Scalar $conn 'SELECT ChieuDaiSau FROM TTThanhPham WHERE id=56073') 'Physical stock protected'
    }

    Test-Save 'Deleting an old material commits row deletion without restoring later stock' {
        param($conn)
        $state=State $conn 56771; $state.Materials.Clear()
        Assert-Saved (Save $state)
        Assert-Equal 0 (Scalar $conn 'SELECT count(*) FROM TTNVL WHERE TTThanhPham_ID=56771') 'Historical row removed'
        Assert-Equal 8930 (Scalar $conn 'SELECT ChieuDaiSau FROM TTThanhPham WHERE id=56073') 'Later stock protected'
    }

    Test-Save 'Editing the latest material use updates its source and history together' {
        param($conn)
        $state=State $conn 56799; $state.Materials[0].CdConLai=8500
        Assert-Saved (Save $state)
        Assert-Equal 8500 (Scalar $conn 'SELECT ChieuDaiSau FROM TTThanhPham WHERE id=56073') 'Latest source updated'
        Assert-Equal 8500 (Scalar $conn 'SELECT CdConLai FROM TTNVL WHERE TTThanhPham_ID=56799') 'Latest history updated'
        Assert-Equal 56799 (Scalar $conn 'SELECT LastEdit_id FROM TTThanhPham WHERE id=56073') 'Latest marker retained'
    }

    Test-Save 'Deleting the latest material use restores its historical start' {
        param($conn)
        $state=State $conn 56799; $state.Materials.Clear()
        Assert-Saved (Save $state)
        Assert-Equal 9945 (Scalar $conn 'SELECT ChieuDaiSau FROM TTThanhPham WHERE id=56073') 'Latest use restored'
        Assert-Equal 0 (Scalar $conn 'SELECT count(*) FROM TTNVL WHERE TTThanhPham_ID=56799') 'Latest row removed'
    }

    Test-Save 'A protected original bin and a newly added bin save independently' {
        param($conn)
        $state=State $conn 56771
        $new=New-Material $conn 56782; $new.CdConLai=0; $new.KlConLai=0; $state.Materials.Add($new)
        Assert-Saved (Save $state)
        Assert-Equal 8930 (Scalar $conn 'SELECT ChieuDaiSau FROM TTThanhPham WHERE id=56073') 'Old source unchanged'
        Assert-Equal 0 (Scalar $conn 'SELECT ChieuDaiSau FROM TTThanhPham WHERE id=56782') 'New source consumed'
        Assert-Equal 56771 (Scalar $conn 'SELECT LastEdit_id FROM TTThanhPham WHERE id=56782') 'New use recorded'
        Assert-Equal 2 (Scalar $conn 'SELECT count(*) FROM TTNVL WHERE TTThanhPham_ID=56771') 'Both material rows saved'
    }

    Test-Save 'Stale new-source stock rolls back product, audit and material changes' {
        param($conn)
        $state=State $conn 56771
        $beforeNote=Scalar $conn 'SELECT GhiChu FROM TTThanhPham WHERE id=56771'
        $audits=Scalar $conn 'SELECT count(*) FROM LichSuSuaDoiThongTin WHERE TTThanhPham_ID=56771'
        $new=New-Material $conn 56782; $new.CdConLai=0; $new.KlConLai=0; $state.Materials.Add($new)
        $state.Product.GhiChu='rollback-probe'
        $null=Scalar $conn 'UPDATE TTThanhPham SET ChieuDaiSau=ChieuDaiSau-1 WHERE id=56782'
        $result=Save $state
        Assert-Equal $false $result.Success 'Stale save rejected'
        Assert-Equal $beforeNote (Scalar $conn 'SELECT GhiChu FROM TTThanhPham WHERE id=56771') 'Product rolled back'
        Assert-Equal $audits (Scalar $conn 'SELECT count(*) FROM LichSuSuaDoiThongTin WHERE TTThanhPham_ID=56771') 'Audit rolled back'
        Assert-Equal 1 (Scalar $conn 'SELECT count(*) FROM TTNVL WHERE TTThanhPham_ID=56771') 'Material rows rolled back'
        Assert-Equal 8930 (Scalar $conn 'SELECT ChieuDaiSau FROM TTThanhPham WHERE id=56073') 'Protected source untouched'
    }

    Test-Save 'A consumed finished product preserves current stock when original output changes' {
        param($conn)
        $state=State $conn 56073; $state.Product.ChieuDaiTruoc=25000; $state.Product.ChieuDaiSau=25000
        Assert-Saved (Save $state)
        Assert-Equal 25000 (Scalar $conn 'SELECT ChieuDaiTruoc FROM TTThanhPham WHERE id=56073') 'Original output updated'
        Assert-Equal 8930 (Scalar $conn 'SELECT ChieuDaiSau FROM TTThanhPham WHERE id=56073') 'Physical output stock preserved'
        Assert-Equal 56799 (Scalar $conn 'SELECT LastEdit_id FROM TTThanhPham WHERE id=56073') 'Output latest marker'
    }

    Test-Save 'Renaming a consumed finished product rejects and rolls back its audit' {
        param($conn)
        $state=State $conn 56073; $state.Product.MaBin='VERIFICATION-RENAMED'
        $audits=Scalar $conn 'SELECT count(*) FROM LichSuSuaDoiThongTin WHERE TTThanhPham_ID=56073'
        $result=Save $state
        Assert-Equal $false $result.Success 'Consumed rename rejected'
        Assert-Equal 'B6-265638/3-04-119' (Scalar $conn 'SELECT MaBin FROM TTThanhPham WHERE id=56073') 'Identity preserved'
        Assert-Equal $audits (Scalar $conn 'SELECT count(*) FROM LichSuSuaDoiThongTin WHERE TTThanhPham_ID=56073') 'Rename audit rolled back'
    }

    Test-Save 'An unused finished product can be renamed and quantity edited' {
        param($conn)
        $state=State $conn 56799; $state.Product.MaBin='VERIFICATION-UNUSED'
        $state.Product.ChieuDaiTruoc=1100; $state.Product.ChieuDaiSau=1100
        Assert-Saved (Save $state)
        Assert-Equal 'VERIFICATION-UNUSED' (Scalar $conn 'SELECT MaBin FROM TTThanhPham WHERE id=56799') 'Unused rename saved'
        Assert-Equal 1100 (Scalar $conn 'SELECT ChieuDaiSau FROM TTThanhPham WHERE id=56799') 'Unused quantity saved'
    }

    Test-Save 'Use created after the form snapshot is detected at save time' {
        param($conn)
        $state=State $conn 56771; $state.Product.ChieuDaiTruoc=4200; $state.Product.ChieuDaiSau=4200
        $null=Scalar $conn "INSERT INTO TTNVL(TTThanhPham_ID,BinNVL,CdBatDau,CdConLai) VALUES(56799,'E4-264908/4-03-00',4020,3900); UPDATE TTThanhPham SET ChieuDaiSau=3900,LastEdit_id=56799 WHERE id=56771;"
        Assert-Saved (Save $state)
        Assert-Equal 4200 (Scalar $conn 'SELECT ChieuDaiTruoc FROM TTThanhPham WHERE id=56771') 'Original output changed'
        Assert-Equal 3900 (Scalar $conn 'SELECT ChieuDaiSau FROM TTThanhPham WHERE id=56771') 'Newly confirmed stock protected'
    }

    Test-Save 'Stage 9 edit preserves consumed output stock and does not change materials' {
        param($conn)
        $state=State $conn 56498; $state.Product.ChieuDaiTruoc=5; $state.Product.ChieuDaiSau=5
        $rows=Scalar $conn 'SELECT count(*) FROM TTNVL'
        Assert-Saved (Save $state -Stage9)
        Assert-Equal 5 (Scalar $conn 'SELECT ChieuDaiTruoc FROM TTThanhPham WHERE id=56498') 'Stage 9 original output'
        Assert-Equal 0 (Scalar $conn 'SELECT ChieuDaiSau FROM TTThanhPham WHERE id=56498') 'Stage 9 current stock'
        Assert-Equal 56503 (Scalar $conn 'SELECT LastEdit_id FROM TTThanhPham WHERE id=56498') 'Stage 9 marker'
        Assert-Equal $rows (Scalar $conn 'SELECT count(*) FROM TTNVL') 'Stage 9 still ignores NVL'
    }

    Test-Save 'Stage 9 cannot rename an already consumed finished product' {
        param($conn)
        $state=State $conn 56498; $state.Product.MaBin='VERIFICATION-STAGE9'
        $result=Save $state -Stage9
        Assert-Equal $false $result.Success 'Stage 9 rename rejected'
        Assert-Equal 'R12-1/1-01-217' (Scalar $conn 'SELECT MaBin FROM TTThanhPham WHERE id=56498') 'Stage 9 identity preserved'
    }

    foreach($direct in @($false,$true)) {
        Test-Save "New/copy save commits a new product and consumes current source stock (verifyInventory=$direct)" {
            param($conn)
            $state=State $conn 56771
            $state.Product.MaBin='VERIFICATION-NEW'; $state.Product.DateInsert='2026-10-16 00:00:00'
            $state.Product.ChieuDaiTruoc=100; $state.Product.ChieuDaiSau=100
            $state.Materials.Clear()
            $new=New-Material $conn 56782; $new.CdConLai=1869.74; $new.KlConLai=0; $state.Materials.Add($new)
            Assert-Saved (Save-New $state -VerifyInventory:$direct)
            $newId=Scalar $conn "SELECT id FROM TTThanhPham WHERE MaBin='VERIFICATION-NEW' AND Temp=0"
            if($newId -le 56799) { throw 'Save did not create a new official product' }
            Assert-Equal 100 (Scalar $conn "SELECT ChieuDaiSau FROM TTThanhPham WHERE id=$newId") 'New product amount'
            Assert-Equal 1869.74 ([Math]::Round((Scalar $conn 'SELECT ChieuDaiSau FROM TTThanhPham WHERE id=56782'),5)) 'Source consumed from current stock'
            Assert-Equal $newId (Scalar $conn 'SELECT LastEdit_id FROM TTThanhPham WHERE id=56782') 'New source owner'
            Assert-Equal 1 (Scalar $conn "SELECT count(*) FROM TTNVL WHERE TTThanhPham_ID=$newId") 'New material history'
            Assert-Equal 8930 (Scalar $conn 'SELECT ChieuDaiSau FROM TTThanhPham WHERE id=56073') 'Original template source unaffected'
        }
    }

    Test-Save 'Direct new/copy save rejects changed inventory without creating a product' {
        param($conn)
        $state=State $conn 56771; $state.Product.MaBin='VERIFICATION-STALE'
        $state.Materials.Clear(); $new=New-Material $conn 56782; $new.CdConLai=1869.74; $state.Materials.Add($new)
        $count=Scalar $conn 'SELECT count(*) FROM TTThanhPham'
        $null=Scalar $conn 'UPDATE TTThanhPham SET ChieuDaiSau=ChieuDaiSau-1 WHERE id=56782'
        $result=Save-New $state -VerifyInventory
        Assert-Equal $false $result.Success 'Stale new/copy save rejected'
        Assert-Equal $true $result.Conflict 'Inventory conflict reported'
        Assert-Equal $count (Scalar $conn 'SELECT count(*) FROM TTThanhPham') 'No partial new product'
        Assert-Equal 1969.74 ([Math]::Round((Scalar $conn 'SELECT ChieuDaiSau FROM TTThanhPham WHERE id=56782'),5)) 'Changed source retained'
    }

    Test-Save 'Copy loader refreshes original material snapshots using current stock' {
        param($conn)
        $copy=[DG_TonKhoBTP_v02.Database.DatabaseHelper]::GetDataByID('56792',[DG_TonKhoBTP_v02.Dictionary.ThongTinChungCongDoan]::KeoRut,1)
        $rows=@($copy.Rows | Where-Object { $_.BinNVL -eq 'R10-265577/1-01-51' })
        Assert-Equal 1 $rows.Count 'Reusable source found'
        $current=(Table $conn 'SELECT * FROM TTThanhPham WHERE id=53596').Rows[0]
        Assert-Equal $current.ChieuDaiSau $rows[0].CdBatDau 'Copy starts at current length'
        Assert-Equal $current.KhoiLuongSau $rows[0].KlBatDau 'Copy starts at current weight'
        Assert-Equal $current.ChieuDaiSau $rows[0].CdConLai 'Source starts unconsumed before UI calculation'
        Assert-Equal 40000 $rows[0].ChieuDaiTruoc 'Original product length is copied'
        Assert-Equal 388 $rows[0].KhoiLuongTruoc 'Original product weight is copied'
    }
}
finally {
    [DG_TonKhoBTP_v02.Database.DB_Base]::_connStr=$oldBase
    [DG_TonKhoBTP_v02.Database.DatabaseHelper]::_connStr=$oldHelper
    # Delete only files created in this unique test directory; no recursive removal.
    $resolved=[IO.Path]::GetFullPath($tempRoot)
    $expectedRoot=[IO.Path]::GetFullPath([IO.Path]::GetTempPath())
    if(-not $resolved.StartsWith($expectedRoot,[StringComparison]::OrdinalIgnoreCase) -or
       -not ([IO.Path]::GetFileName($resolved)).StartsWith('Codex.Inventory.')) { throw 'Unexpected test cleanup path' }
    foreach($path in [IO.Directory]::GetFiles($resolved)) { [IO.File]::Delete($path) }
    [IO.Directory]::Delete($resolved)
}

Write-Output "RESULT: $script:passed passed, $script:failed failed"
if($script:failed -gt 0) { exit 1 }
