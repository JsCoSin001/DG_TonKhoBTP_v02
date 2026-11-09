param(
    [ValidateSet('Data', 'Ui', 'All')][string]$Suite = 'All',
    [string]$SnapshotPath
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$bin = Join-Path $root 'DG_TonKhoBTP_v02/bin/Debug'
$assembly = [Reflection.Assembly]::LoadFrom((Join-Path $bin 'DG_TonKhoBTP_v02.exe'))
$null = [Reflection.Assembly]::LoadFrom((Join-Path $bin 'System.Data.SQLite.dll'))
$db = $assembly.GetType('DG_TonKhoBTP_v02.Database.Kho.TonKho.BC_TonKho_DB', $true)
$schema = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'fixtures/execution-edit/base-schema.sql'))
$seed = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'UC_BCTonKho.Acceptance.seed.sql'))

function Assert-Equal($Actual, $Expected, [string]$Label) {
    if ($Actual -cne $Expected) { throw "$Label expected [$Expected], got [$Actual]" }
}

function Invoke-FixtureSql([string]$Sql) {
    $connection = [DG_TonKhoBTP_v02.Database.DB_Base]::OpenConnection()
    try {
        $command = $connection.CreateCommand()
        try {
            $command.CommandText = $Sql
            $null = $command.ExecuteNonQuery()
        } finally { $command.Dispose() }
    } finally { $connection.Dispose() }
}

function Test-Fixture([string]$Name, [scriptblock]$Run) {
    $originalConnection = [DG_TonKhoBTP_v02.Database.DB_Base]::_connStr
    $path = Join-Path ([IO.Path]::GetTempPath()) ("InventoryRowDetails-" + [Guid]::NewGuid() + ".db")
    try {
        [DG_TonKhoBTP_v02.Database.DB_Base]::SetDatabasePath($path)
        Invoke-FixtureSql "$schema
ALTER TABLE DanhSachMaSP ADD COLUMN Ten_KhongDau TEXT;
$seed"
        & $Run
        Write-Output "PASS $Name"
    } finally {
        [DG_TonKhoBTP_v02.Database.DB_Base]::_connStr = $originalConnection
        [System.Data.SQLite.SQLiteConnection]::ClearAllPools()
        Remove-Item -LiteralPath $path -ErrorAction SilentlyContinue
    }
}

function Read-Report([bool]$IncludeDetails = $true) {
    $method = $db.GetMethod('LayTonKhoExcelMoi', [Reflection.BindingFlags]'Static,NonPublic',
        $null, [type[]]@([bool]), $null)
    if ($null -eq $method) { throw 'RED: LayTonKhoExcelMoi(bool) is required to load row details' }
    return $method.Invoke($null, [object[]]@($IncludeDetails))
}

function Set-WholePlans {
    Invoke-FixtureSql @"
UPDATE KeHoachHangQuyCachCuonChan SET SoLuongCuonCanLay=3 WHERE id=41;
INSERT INTO KeHoachCatNhom(id,KeHoachHang_ID,QuyCachCuonChan_ID) VALUES(54,31,41);
INSERT INTO KeHoachCatChiTiet(id,KeHoachCatNhom_ID,ChieuDaiCanCat) VALUES(64,54,20),(65,54,30);
INSERT INTO KeHoach(id,MaKeHoach) VALUES(2,'QA-PLAN-2');
INSERT INTO KeHoachHang(id,KeHoach_ID,DanhSachMaSP_ID) VALUES(36,2,1);
INSERT INTO KeHoachHangQuyCachCuonChan(id,KeHoachHang_ID,ChieuDai1Cuon,SoLuongCuonCanLay) VALUES(45,36,400,1);
"@
}

function Set-PartialPlans {
    Invoke-FixtureSql @"
INSERT INTO KeHoach(id,MaKeHoach) VALUES(3,'QA-PARTIAL-PLAN');
INSERT INTO KeHoachHang(id,KeHoach_ID,DanhSachMaSP_ID) VALUES(35,3,3);
INSERT INTO KeHoachCatNhom(id,KeHoachHang_ID,TonCuonLe_ID) VALUES(55,35,303);
INSERT INTO KeHoachCatChiTiet(id,KeHoachCatNhom_ID,ChieuDaiCanCat) VALUES(66,55,300);
INSERT INTO DanhSachMaSP(id,Ten,Ten_KhongDau,Ma) VALUES(8,'Plan only','Plan only','QA-PLAN-ONLY');
INSERT INTO KeHoachHang(id,KeHoach_ID,DanhSachMaSP_ID) VALUES(37,3,8);
INSERT INTO KeHoachHangQuyCachCuonChan(id,KeHoachHang_ID,ChieuDai1Cuon,SoLuongCuonCanLay) VALUES(46,37,200,1);
"@
}

function Test-ReportData {
    Test-Fixture 'WholeSourcesAndPlans' {
        Set-WholePlans
        $rows = Read-Report
        $whole = $rows | Where-Object { $_.MaSanPham -eq 'QA-P1' -and $_.Loai -eq 'Cuộn' -and $_.ChieuDaiMoiCuon -eq 200 }
        Assert-Equal $whole.SoCuon 9 'Whole count'
        Assert-Equal $whole.ChieuDaiTonThucTe 1800 'Whole stock'
        Assert-Equal $whole.ChieuDaiTonDatTruoc 400 'Whole reserved'
        Assert-Equal $whole.ChieuDaiTonKhaDung 1400 'Whole available'
        $reservation = $whole.ChiTiet.DatTruoc[0]
        Assert-Equal $whole.ChiTiet.DatTruoc.Count 1 'Only matching plan'
        Assert-Equal $reservation.MaKeHoach 'QA-PLAN' 'Matching plan code'
        Assert-Equal $reservation.SoCuonConPhaiLay 1 'Whole takes pending'
        Assert-Equal $reservation.SoCuonDanhDeCat 1 'One cut group, two details'
        Assert-Equal $reservation.SoMet 400 'Plan contribution'
        $source = $whole.ChiTiet.Nguon | Where-Object TTCuonDayId -eq 201
        Assert-Equal "$($source.SoCuonNhap)/$($source.SoCuonDaLay)/$($source.SoCuonChuyenLe)/$($source.SoCuonCon)" '10/2/2/6' 'Source reconciliation'
        Assert-Equal ($whole.ChiTiet.LotGop | Where-Object MaBin -eq 'QA-B').SoCuon 3 'Second lot contribution'
        $other = $rows | Where-Object { $_.MaSanPham -eq 'QA-P1' -and $_.Loai -eq 'Cuộn' -and $_.ChieuDaiMoiCuon -eq 400 }
        Assert-Equal $other.ChieuDaiTonDatTruoc 400 'Other specification reserved'
        Assert-Equal $other.ChiTiet.DatTruoc[0].MaKeHoach 'QA-PLAN-2' 'Other specification plan'
    }

    Test-Fixture 'ContributingLotsAndMissingCode' {
        Invoke-FixtureSql @"
INSERT INTO TTThanhPham(id,DanhSachSP_ID,MaBin) VALUES(108,1,'QA-ZERO'),(109,1,'  ');
INSERT INTO TTNhapKhoTP(id,TTThanhPham_ID) VALUES(18,108),(19,109);
INSERT INTO TTCuonDay(id,ThongTinNhapKho_ID,SoCuon,ChieuDai_1cuon)
VALUES(209,11,2,200),(210,18,0,200),(211,19,1,200);
"@
        $whole = Read-Report | Where-Object { $_.MaSanPham -eq 'QA-P1' -and $_.Loai -eq 'Cuộn' -and $_.ChieuDaiMoiCuon -eq 200 }
        Assert-Equal $whole.LotHienThiBang 'Nhiều LOT' 'Interactive LOT label'
        Assert-Equal $whole.MaBinLot 'E1-265081/5-03-00 | QA-B' 'Excel LOT list'
        Assert-Equal ($whole.ChiTiet.LotGop.SoCuon -join ',') '8,3' 'Named contributions'
        Assert-Equal ($whole.ChiTiet.Nguon | Where-Object TTCuonDayId -eq 211).SoCuonGop 1 'Unnamed contribution'
        Assert-Equal $whole.SoCuon 12 'Whole count includes unnamed contribution'
    }

    Test-Fixture 'PartialReservationsAndPlanOnly' {
        Set-PartialPlans
        $rows = Read-Report
        $partial = $rows | Where-Object TonCuonLeId -eq 303
        Assert-Equal $partial.SoCuon 1 'Physical partial count'
        Assert-Equal "$($partial.SoDau)/$($partial.SoCuoi)" '996/600' 'Descending markers'
        Assert-Equal $partial.ChieuDaiTonThucTe 396 'Partial stock'
        Assert-Equal $partial.ChieuDaiTonDatTruoc 450 'Partial reserved'
        Assert-Equal $partial.ChieuDaiTonKhaDung -54 'Partial available'
        Assert-Equal (($partial.ChiTiet.DatTruoc | Sort-Object KeHoachId).SoMet -join ',') '150,300' 'Partial plan contributions'
        Assert-Equal $partial.LaBatThuong $true 'Negative availability warning'
        $planOnly = $rows | Where-Object MaSanPham -eq 'QA-PLAN-ONLY'
        Assert-Equal $planOnly.ChieuDaiTonThucTe 0 'Plan-only stock'
        Assert-Equal $planOnly.ChieuDaiTonDatTruoc 200 'Plan-only reserved'
        Assert-Equal $planOnly.ChieuDaiTonKhaDung -200 'Plan-only available'
        Assert-Equal $planOnly.ChiTiet.LotGop.Count 0 'Plan-only LOT list'
    }

    Test-Fixture 'ReportProjectionParity' {
        $columns = 'MaSanPham','Loai','MaBinLot','SoCuon','ChieuDaiMoiCuon','ChieuDaiTonThucTe',
            'ChieuDaiTonDatTruoc','ChieuDaiTonKhaDung','SoDau','SoCuoi','CanhBao'
        $table = @(Read-Report)
        $excel = @(Read-Report $false)
        Assert-Equal (($table | Select-Object $columns | ConvertTo-Json -Compress)) (
            $excel | Select-Object $columns | ConvertTo-Json -Compress) 'Table and Excel totals'
        Assert-Equal @($table | Where-Object { $null -eq $_.ChiTiet }).Count 0 'Table rows carry details'
        Assert-Equal @($excel | Where-Object { $null -ne $_.ChiTiet }).Count 0 'Excel rows omit details'
    }
}

if ($Suite -ne 'Ui') { Test-ReportData }

