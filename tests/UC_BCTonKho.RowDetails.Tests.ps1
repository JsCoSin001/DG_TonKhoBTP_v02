param(
    [ValidateSet('Data', 'Ui', 'All')][string]$Suite = 'All',
    [string]$SnapshotPath
)

$ErrorActionPreference = 'Stop'
$script:Failures = 0
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
    } catch {
        $script:Failures++
        Write-Output "FAIL $Name — $($_.Exception.GetBaseException().Message)"
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

    Test-Fixture 'FractionalLegacyReservationKeepsReportTotal' {
        Set-WholePlans
        Invoke-FixtureSql 'UPDATE KeHoachHangQuyCachCuonChan SET SoLuongCuonCanLay=2.5 WHERE id=41;'
        $whole = Read-Report | Where-Object { $_.MaSanPham -eq 'QA-P1' -and $_.Loai -eq 'Cuộn' -and $_.ChieuDaiMoiCuon -eq 200 }
        $excel = Read-Report $false | Where-Object { $_.MaSanPham -eq 'QA-P1' -and $_.Loai -eq 'Cuộn' -and $_.ChieuDaiMoiCuon -eq 200 }
        Assert-Equal $whole.ChieuDaiTonDatTruoc 400 'Round the combined legacy reservation quantity'
        Assert-Equal $excel.ChieuDaiTonDatTruoc 400 'Excel retains the previous reservation total'
        Assert-Equal $whole.ChiTiet.DatTruoc[0].SoCuonDanhDeCat 1 'Actual pending cut groups'
        Assert-Equal $whole.ChiTiet.DatTruoc[0].SoCuonConPhaiLay 1 'Plan explanation reconciles the rounded total'
    }

    Test-Fixture 'PendingSegmentsPreserveLegacyLengthsAndTotals' {
        Invoke-FixtureSql @"
INSERT INTO KeHoachCatChiTiet(id,KeHoachCatNhom_ID,ChieuDaiCanCat)
VALUES(64,53,10.25),(65,53,20.25),(66,53,'unknown');
"@
        $reel = Read-Report | Where-Object TonCuonLeId -eq 303
        $excel = Read-Report $false | Where-Object TonCuonLeId -eq 303
        $kind = $assembly.GetType('DG_TonKhoBTP_v02.Models.Kho.TonKho.BCTonKho_LoaiTimKiem', $true)
        $search = $db.GetMethod('LayChiTietTonAsync', [Reflection.BindingFlags]'Static,NonPublic')
        $task = $search.Invoke($null, [object[]]@([Enum]::ToObject($kind, 3), [long]303,
            'QA-DIRECT', $false, [Threading.CancellationToken]::None))
        $searched = $task.GetAwaiter().GetResult().Dong[0]
        Assert-Equal $reel.ChieuDaiTonDatTruoc 180.5 'Report keeps the SQLite total'
        Assert-Equal $excel.ChieuDaiTonDatTruoc 180.5 'Excel keeps the SQLite total'
        Assert-Equal $searched.DatCho 180.5 'Search keeps the same fractional meter total'
        $segments = $reel.ChiTiet.DoanDatTruoc
        Assert-Equal ($segments.KeHoachCatChiTietId -join ',') '63,64,65,66' 'Each pending segment is retained'
        Assert-Equal $segments[1].ChieuDai 10.25 'First fractional segment'
        Assert-Equal $segments[2].ChieuDai 20.25 'Second fractional segment'
        Assert-Equal $segments[3].ChieuDai $null 'Unreadable length stays unknown'
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

function Test-ReportHistory {
    Test-Fixture 'CutHistoryBySourceAndReel' {
        Invoke-FixtureSql @"
UPDATE LichSuCat SET ThoiGianThucHien='2026-10-01 08:00:00' WHERE id=81;
UPDATE LichSuCat SET ThoiGianThucHien='2026-10-01 08:30:00' WHERE id=82;
INSERT INTO TonCuonLe(id,MaCuon,TTThanhPham_ID,TTCuonDay_ID,SoDau,SoCuoi)
VALUES(304,'QA-LE-400',101,203,0,390);
INSERT INTO KeHoachCatNhom(id,KeHoachHang_ID,TonCuonLe_ID,QuyCachCuonChan_ID) VALUES(54,31,304,42);
INSERT INTO KeHoachCatChiTiet(id,KeHoachCatNhom_ID,ChieuDaiCanCat) VALUES(64,52,5),(65,52,5),(66,54,10);
INSERT INTO LichSuCat(id,KeHoachCatChiTiet_ID,TonCuonLe_ID,LoaiNguon,ChieuDaiCat,
  SoDauTruoc,SoCuoiTruoc,SoDauCat,SoCuoiCat,SoDauSau,SoCuoiSau,HeSoChieu,
  MaQuetThucTe,NguoiThucHien,ThoiGianThucHien) VALUES
(83,64,302,'CUON_LE',5,180,200,180,185,185,200,1,'QA-LE20','qa','2026-10-01 09:00:00'),
(84,65,302,'CUON_LE',5,185,200,185,190,190,200,1,'QA-LE20','qa','2026-10-01 09:00:00'),
(85,66,304,'CUON_CHAN',10,0,400,400,390,0,390,-1,'QA-LE-400','qa','2026-10-01 10:00:00');
UPDATE TonCuonLe SET SoDau=190 WHERE id=302;
"@
        $rows = Read-Report
        $whole200 = $rows | Where-Object { $_.MaSanPham -eq 'QA-P1' -and $_.Loai -eq 'Cuộn' -and $_.ChieuDaiMoiCuon -eq 200 }
        $whole400 = $rows | Where-Object { $_.MaSanPham -eq 'QA-P1' -and $_.Loai -eq 'Cuộn' -and $_.ChieuDaiMoiCuon -eq 400 }
        $partial302 = $rows | Where-Object TonCuonLeId -eq 302
        $partial303 = $rows | Where-Object TonCuonLeId -eq 303
        Assert-Equal ($whole200.ChiTiet.LichSuCat.Id -join ',') '84,83,82,81' 'Source history sorted by time then ID'
        Assert-Equal ($whole400.ChiTiet.LichSuCat.Id -join ',') '85' 'Other specification history'
        Assert-Equal ($partial302.ChiTiet.LichSuCat.Id -join ',') '84,83,82' 'One physical reel'
        Assert-Equal $partial303.ChiTiet.LichSuCat.Count 0 'Empty history'
        $cut = $partial302.ChiTiet.LichSuCat[0]
        Assert-Equal "$($cut.MaCuon)/$($cut.LoaiNguon)/$($cut.MaKeHoach)/$($cut.NguoiThucHien)" 'QA-LE20/CUON_LE/QA-PLAN/qa' 'Cut identity and plan'
        Assert-Equal "$($cut.SoDauTruoc)/$($cut.SoCuoiTruoc)|$($cut.SoDauCat)/$($cut.SoCuoiCat)|$($cut.SoDauSau)/$($cut.SoCuoiSau)" '185/200|185/190|190/200' 'Cut markers'
    }

    Test-Fixture 'InvalidHistoryValuesRemainUnknown' {
        Invoke-FixtureSql "UPDATE LichSuCat SET SoDauTruoc='unknown', SoCuoiCat=5.75, ChieuDaiCat='bad' WHERE id=81;"
        $row = Read-Report | Where-Object TonCuonLeId -eq 301
        $cut = $row.ChiTiet.LichSuCat[0]
        Assert-Equal $cut.SoDauTruoc $null 'Unknown marker does not become zero'
        Assert-Equal $cut.SoCuoiCat $null 'Fractional marker does not become an integer'
        Assert-Equal $cut.ChieuDaiCat $null 'Unreadable cut length remains unavailable'
        Assert-Equal $cut.SoDauCat 0 'Real zero remains zero'
    }
    Test-Fixture 'BrokenSourceIdDoesNotBorrowAnotherLot' {
        Invoke-FixtureSql 'PRAGMA foreign_keys=OFF; UPDATE TonCuonLe SET TTCuonDay_ID=201.75 WHERE id=301;'
        $rows = Read-Report
        $whole = $rows | Where-Object { $_.MaSanPham -eq 'QA-P1' -and $_.Loai -eq 'Cuộn' -and $_.ChieuDaiMoiCuon -eq 200 }
        $reel = $rows | Where-Object TonCuonLeId -eq 301
        Assert-Equal ($whole.ChiTiet.LichSuCat.Id -join ',') '82' 'Only history joined to a real source belongs to the whole row'
        Assert-Equal ($reel.ChiTiet.LichSuCat.Id -join ',') '81' 'History of the physical reel remains available'
        Assert-Equal $reel.ChiTiet.LichSuCat[0].TTCuonDayId $null 'Broken source link is unavailable'
    }
    Test-Fixture 'LoadedRowSurvivesDatabaseChange' {
        $loaded = Read-Report | Where-Object TonCuonLeId -eq 302
        Invoke-FixtureSql @"
INSERT INTO KeHoachCatChiTiet(id,KeHoachCatNhom_ID,ChieuDaiCanCat) VALUES(64,52,5);
INSERT INTO LichSuCat(id,KeHoachCatChiTiet_ID,TonCuonLe_ID,LoaiNguon,ChieuDaiCat,
  SoDauTruoc,SoCuoiTruoc,SoDauCat,SoCuoiCat,SoDauSau,SoCuoiSau,HeSoChieu,
  MaQuetThucTe,NguoiThucHien)
VALUES(83,64,302,'CUON_LE',5,180,200,180,185,185,200,1,'QA-LE20','qa');
UPDATE TonCuonLe SET SoDau=185 WHERE id=302;
"@
        $updated = Read-Report | Where-Object TonCuonLeId -eq 302
        Assert-Equal "$($loaded.SoDau)/$($loaded.ChieuDaiTonThucTe)/$($loaded.ChiTiet.LichSuCat.Id -join ',')" '180/20/82' 'Original report and history remain paired'
        Assert-Equal "$($updated.SoDau)/$($updated.ChieuDaiTonThucTe)/$($updated.ChiTiet.LichSuCat.Id -join ',')" '185/15/83,82' 'Reloaded report and history'
    }

    if ($SnapshotPath) {
        $originalConnection = [DG_TonKhoBTP_v02.Database.DB_Base]::_connStr
        $resolved = (Resolve-Path -LiteralPath $SnapshotPath).Path
        $hash = (Get-FileHash -LiteralPath $resolved -Algorithm SHA256).Hash
        try {
            [DG_TonKhoBTP_v02.Database.DB_Base]::_connStr = "Data Source=$resolved;Read Only=True;FailIfMissing=True;Version=3;"
            $rows = Read-Report
            $whole = $rows | Where-Object { $_.MaBinLot -eq 'E1-265081/5-03-00' -and $_.Loai -eq 'Cuộn' }
            $reel = $rows | Where-Object TonCuonLeId -eq 6
            Assert-Equal "$($whole.SoCuon)/$($whole.ChieuDaiTonThucTe)" '6/1200' 'Snapshot totals'
            Assert-Equal ($whole.ChiTiet.LichSuCat.Id -join ',') '11,10,9,8' 'Snapshot source history'
            Assert-Equal ($reel.ChiTiet.LichSuCat.Id -join ',') '11,10,9' 'Snapshot physical reel history'
        } finally {
            [DG_TonKhoBTP_v02.Database.DB_Base]::_connStr = $originalConnection
            [System.Data.SQLite.SQLiteConnection]::ClearAllPools()
        }
        Assert-Equal (Get-FileHash -LiteralPath $resolved -Algorithm SHA256).Hash $hash 'Snapshot SHA-256'
        Write-Output 'PASS ConfiguredSnapshot'
    }
}

function Get-UiControl($Parent, [string]$Name) {
    $found = $Parent.Controls.Find($Name, $true)
    Assert-Equal $found.Length 1 "Control $Name"
    return $found[0]
}

function Wait-Ui([scriptblock]$Condition, [string]$Message) {
    $deadline = [Diagnostics.Stopwatch]::StartNew()
    while ($deadline.ElapsedMilliseconds -lt 5000) {
        [Windows.Forms.Application]::DoEvents()
        if (& $Condition) { return }
        Start-Sleep -Milliseconds 20
    }
    throw $Message
}

function Invoke-RowButton($Grid, $Row, [string]$Column, [scriptblock]$Inspect = {}) {
    $state = [pscustomobject]@{ Opened = $false; Failure = $null }
    $timer = New-Object Windows.Forms.Timer
    $timer.Interval = 25
    $timer.add_Tick({
        $detail = [Windows.Forms.Application]::OpenForms |
            Where-Object { $_.GetType().Name -like 'Frm_ChiTiet*TonKho' } | Select-Object -First 1
        if ($null -eq $detail) { return }
        $state.Opened = $true
        try { $null = $Inspect.Invoke($detail) }
        catch { $state.Failure = $_.Exception }
        finally { $detail.Close() }
    }.GetNewClosure())
    try {
        $timer.Start()
        $event = [Windows.Forms.DataGridViewCellEventArgs]::new($Grid.Columns[$Column].Index, $Row.Index)
        $click = $Grid.GetType().GetMethod('OnCellContentClick', [Reflection.BindingFlags]'Instance,NonPublic')
        $null = $click.Invoke($Grid, [object[]]@($event))
    } finally { $timer.Dispose() }
    if ($state.Failure) { throw $state.Failure }
    return $state.Opened
}

function Test-ReportUi {
    Add-Type -AssemblyName System.Windows.Forms
    Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
using System.Text;

public static class InventoryReportTestDialogs
{
    private delegate bool WindowCallback(IntPtr handle, IntPtr context);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    private static extern bool EnumThreadWindows(uint threadId, WindowCallback callback, IntPtr context);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr handle, StringBuilder text, int capacity);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr handle, uint message, IntPtr wParam, IntPtr lParam);

    public static void DismissEmptyReport()
    {
        EnumThreadWindows(GetCurrentThreadId(), (handle, context) =>
        {
            var title = new StringBuilder(64);
            GetWindowText(handle, title, title.Capacity);
            if (title.ToString() == "Tồn kho")
                PostMessage(handle, 0x0010, IntPtr.Zero, IntPtr.Zero);
            return true;
        }, IntPtr.Zero);
    }
}
"@
    Test-Fixture 'ReportButtonClearsSearchAndCancelsSuggestions' {
        $hostForm = [Windows.Forms.Form]::new()
        $hostForm.ShowInTaskbar = $false
        try {
            $control = [Activator]::CreateInstance($assembly.GetType('DG_TonKhoBTP_v02.UI.UC_BCTonKho', $true))
            $hostForm.Controls.Add($control)
            $control.Dock = [Windows.Forms.DockStyle]::Fill
            $hostForm.Show()
            $grid = Get-UiControl $control 'grvShowBaoCao'
            $search = Get-UiControl $control 'btnTimKiem'
            $report = Get-UiControl $control 'btnXemTonKho'
            $textUpdate = $search.GetType().GetMethod('OnTextUpdate', [Reflection.BindingFlags]'Instance,NonPublic')

            $null = $search.Focus()
            $search.Text = 'QA-B'
            $null = $textUpdate.Invoke($search, [object[]]@([EventArgs]::Empty))
            Wait-Ui { $search.Items.Count -gt 0 } 'Typing should load the first suggestions'

            $search.Text = '265081'
            $null = $textUpdate.Invoke($search, [object[]]@([EventArgs]::Empty))
            $report.PerformClick()
            Assert-Equal $search.Text '' 'Report clears the current search text'
            Assert-Equal $search.Items.Count 0 'Report clears the existing suggestions'
            Assert-Equal $search.SelectedIndex -1 'Report clears the selection'
            Assert-Equal $search.DataSource $null 'Report clears the suggestion source'
            Assert-Equal $search.DroppedDown $false 'Report closes the dropdown'
            Wait-Ui { $report.Enabled -and $grid.Rows.Count -gt 0 } 'Report still loads and renders its rows'

            $settle = [Diagnostics.Stopwatch]::StartNew()
            Wait-Ui { $settle.ElapsedMilliseconds -ge 600 } 'Wait for the earlier suggestion debounce to elapse'
            Assert-Equal $search.Items.Count 0 'The cancelled search must not restore old suggestions'
            Assert-Equal $search.DroppedDown $false 'The cancelled search must not reopen the dropdown'

            $null = $search.Focus()
            $search.Text = 'QA-B'
            $null = $textUpdate.Invoke($search, [object[]]@([EventArgs]::Empty))
            Wait-Ui { $search.Items.Count -gt 0 } 'New text still produces suggestions after showing the report'
        } finally { $hostForm.Dispose() }
    }

    Test-Fixture 'ViewButtonExplainsSortedReport' {
        Set-WholePlans
        Set-PartialPlans
        Invoke-FixtureSql @"
INSERT INTO DanhSachMaSP(id,Ten,Ten_KhongDau,Ma) VALUES(9,'Missing reel','Missing reel','QA-MISSING');
INSERT INTO TTThanhPham(id,DanhSachSP_ID,MaBin) VALUES(110,9,'QA-MISSING');
INSERT INTO TTNhapKhoTP(id,TTThanhPham_ID) VALUES(20,110);
INSERT INTO TTCuonDay(id,ThongTinNhapKho_ID,SoCuon,ChieuDai_1cuon,SoDau,SoCuoi)
VALUES(212,20,1,180,0,180);
INSERT INTO KeHoachCatChiTiet(id,KeHoachCatNhom_ID,ChieuDaiCanCat) VALUES(67,52,5),(68,52,7);
"@
        $hostForm = New-Object Windows.Forms.Form
        $hostForm.Width = 1400
        $hostForm.Height = 850
        $hostForm.ShowInTaskbar = $false
        try {
            $control = [Activator]::CreateInstance($assembly.GetType('DG_TonKhoBTP_v02.UI.UC_BCTonKho', $true))
            $hostForm.Controls.Add($control)
            $control.Dock = [Windows.Forms.DockStyle]::Fill
            $hostForm.Show()
            $grid = Get-UiControl $control 'grvShowBaoCao'
            (Get-UiControl $control 'btnXemTonKho').PerformClick()
            Wait-Ui { $grid.Columns.Contains('Button_ExcelMoi') -and $grid.Rows.Count -gt 0 } 'Report button did not render inventory rows'
            $headers = 'Tên sản phẩm|Mã|Đvị|LOT / Mã cuộn|Loại|Số cuộn|Chiều dài 1C|Tồn thực tế|Đặt trước|Tồn khả dụng|Số đầu|Số cuối|Thao tác'
            Assert-Equal ($grid.Columns.HeaderText -join '|') $headers 'Report uses the agreed columns'
            $reportWidths = $grid.Columns.Width -join ','
            $reportHeights = "$($grid.ColumnHeadersHeight)/$($grid.Rows[0].Height)"
            $grid.Sort($grid.Columns['MaSP_ExcelMoi'], [ComponentModel.ListSortDirection]::Descending)
            $row = $grid.Rows | Where-Object { $_.Tag.MaSanPham -eq 'QA-P1' -and $_.Tag.Loai -eq 'Cuộn' -and $_.Tag.ChieuDaiMoiCuon -eq 200 }
            Assert-Equal $row.Cells['MaBinLot_ExcelMoi'].Value 'Nhiều LOT' 'Grouped LOT on the report row'
            Assert-Equal $row.Cells['DatTruoc_ExcelMoi'].FormattedValue '2 cuộn' 'Report reservation uses whole reel count'
            $opened = Invoke-RowButton $grid $row 'Button_ExcelMoi' {
                param($detail)
                $lots = Get-UiControl $detail 'dgvChiTiet'
                Assert-Equal (Get-UiControl $detail 'tbxMaSP').Text 'QA-P1' 'Sorted row opens correct product'
                Assert-Equal ((Get-UiControl $detail 'tbxTonThucTe').Text -replace '[., ]','') '1800' 'Summary stock'
                Assert-Equal (Get-UiControl $detail 'tbxDatTruoc').Text '400' 'Summary reservation'
                Assert-Equal ((Get-UiControl $detail 'tbxTonKhaDung').Text -replace '[., ]','') '1400' 'Summary availability'
                $visibleLots = $lots.Rows | ForEach-Object { $_.Cells[0].Value }
                Assert-Equal ($visibleLots -join '|') 'E1-265081/5-03-00|QA-B' 'Visible contributors'
                Assert-Equal (($lots.Rows | ForEach-Object { $_.Cells[1].Value }) -join ',') '6,3' 'Visible contributing counts'
                Assert-Equal $lots.Rows[0].Cells[3].Value 'Dự kiến: 2 cuộn' 'FIFO remains provisional in detail'
                Assert-Equal $lots.Rows[1].Cells[3].Value '' 'Later LOT is unreserved'
            }
            Assert-Equal $opened $true 'Grouped detail window opens'

            $one = $grid.Rows | Where-Object { $_.Tag.MaSanPham -eq 'QA-P1' -and $_.Tag.Loai -eq 'Cuộn' -and $_.Tag.ChieuDaiMoiCuon -eq 400 }
            Assert-Equal $one.Cells['MaBinLot_ExcelMoi'].Value 'E1-265081/5-03-00' 'Single LOT stays a code'
            Assert-Equal (Invoke-RowButton $grid $one 'Button_ExcelMoi' {
                param($detail)
                Assert-Equal (Get-UiControl $detail 'dgvChiTiet').Rows.Count 1 'Single LOT source opens directly'
            }) $true 'Single LOT detail window opens'

            $reel = $grid.Rows | Where-Object { $_.Tag.TonCuonLeId -eq 303 }
            Assert-Equal $reel.Cells['DatTruoc_ExcelMoi'].FormattedValue '450 m' 'Report partial reservation uses meters'
            Assert-Equal (Invoke-RowButton $grid $reel 'Button_ExcelMoi' {
                param($detail)
                Assert-Equal (Get-UiControl $detail 'tbxTonThucTe').Text '396' 'Partial stock'
                Assert-Equal (Get-UiControl $detail 'tbxTonKhaDung').Text '-54' 'Negative availability'
                Assert-Equal (Get-UiControl $detail 'tbxSoDau').Text '996' 'Partial start'
                Assert-Equal (Get-UiControl $detail 'tbxSoCuoi').Text '600' 'Partial end'
                $details = Get-UiControl $detail 'dgvChiTiet'
                Assert-Equal $details.Rows.Count 1 'Reservation is visible without cut history'
                Assert-Equal $details.Rows[0].Cells[4].Value '' 'Unknown cut markers are blank'
                Assert-Equal $details.Columns[1].HeaderText 'CD trước cắt' 'Label for per-cut original length'
                Assert-Equal $details.Rows[0].Cells[8].Value '150 m; 300 m' 'Separate segments already assigned to this reel'
            }) $true 'Partial detail window opens'

            $planOnly = $grid.Rows | Where-Object { $_.Tag.MaSanPham -eq 'QA-PLAN-ONLY' }
            Assert-Equal (Invoke-RowButton $grid $planOnly 'Button_ExcelMoi' {
                param($detail)
                Assert-Equal (Get-UiControl $detail 'tbxDatTruoc').Text '200' 'Plan without inventory'
                Assert-Equal (Get-UiControl $detail 'dgvChiTiet').Rows.Count 0 'No LOT to allocate'
            }) $true 'Plan-only detail window opens'

            $missing = $grid.Rows | Where-Object { $_.Tag.MaSanPham -eq 'QA-MISSING' }
            Assert-Equal (Invoke-RowButton $grid $missing 'Button_ExcelMoi' {
                param($detail)
                Assert-Equal (Get-UiControl $detail 'tbxTonThucTe').Text '' 'Missing physical reel is not shown as zero'
                Assert-Equal ((Get-UiControl $detail 'groupBox1').Text -match 'CÓ CẢNH BÁO') $true 'Missing physical reel warning'
            }) $true 'Missing reel detail window opens'

            $choices = [Data.DataTable]::new()
            $null = $choices.Columns.Add('Loai', [int])
            $null = $choices.Columns.Add('Id', [long])
            $null = $choices.Columns.Add('GiaTri', [string])
            $null = $choices.Rows.Add(1, [long]101, 'E1-265081/5-03-00')
            $null = $choices.Rows.Add(1, [long]105, 'QA-DEPLETED')
            $select = $control.GetType().GetMethod('OnTonKhoItemSelected', [Reflection.BindingFlags]'Instance,NonPublic')
            $null = $select.Invoke($control, [object[]]@($choices.DefaultView[0]))
            Wait-Ui { @($grid.Rows | Where-Object { $_.Tag.TTThanhPhamId -eq 101 }).Count -gt 0 } 'Search selection did not render inventory rows'
            Assert-Equal ($grid.Columns.HeaderText -join '|') $headers 'Search uses the same columns and order'
            Assert-Equal ($grid.Columns.Width -join ',') $reportWidths 'Search uses the same widths'
            Assert-Equal "$($grid.ColumnHeadersHeight)/$($grid.Rows[0].Height)" $reportHeights 'Search uses the same row and header heights'
            $searchWhole = $grid.Rows | Where-Object { $_.Tag.TTCuonDayId -eq 201 -and !$_.Tag.TonCuonLeId }
            Assert-Equal $searchWhole.Cells['MaSP_ExcelMoi'].Value 'QA-P1' 'Search includes product code'
            Assert-Equal $searchWhole.Cells['DonVi_ExcelMoi'].Value 'm' 'Search includes product unit'
            Assert-Equal $searchWhole.Cells['TonThucTe_ExcelMoi'].Value 1200 'Search stock stays within the selected source'
            Assert-Equal $searchWhole.Cells['DatTruoc_ExcelMoi'].FormattedValue '2 cuộn' 'Search reservation uses whole reel count'
            Assert-Equal $searchWhole.Cells['KhaDung_ExcelMoi'].Value 800 'Availability subtracts 400 meters'
            $searchReel = $grid.Rows | Where-Object { $_.Tag.TonCuonLeId -eq 302 }
            Assert-Equal $searchReel.Cells['DatTruoc_ExcelMoi'].FormattedValue '12 m' 'Search partial reservation uses meters'
            Assert-Equal (Invoke-RowButton $grid $searchReel 'Button_ExcelMoi' {
                param($detail)
                Assert-Equal (Get-UiControl $detail 'tbxMaSP').Text 'QA-P1' 'Search fills product code'
                $details = Get-UiControl $detail 'dgvChiTiet'
                Assert-Equal $details.Rows.Count 1 'Only selected reel history'
                Assert-Equal $details.Rows[0].Cells[1].Value '200' 'Length immediately before the cut'
                Assert-Equal $details.Rows[0].Cells[3].Value '180' 'Length cut'
                Assert-Equal $details.Rows[0].Cells[8].Value '5 m; 7 m' 'Only pending segments assigned to the reel'
            }) $true 'Search result opens the selected reel'

            $null = $select.Invoke($control, [object[]]@($choices.DefaultView[1]))
            Wait-Ui { $grid.Rows.Count -eq 0 } 'Depleted source should be hidden by default'
            $includeDepleted = Get-UiControl $control 'cbxKieuTon'
            $includeDepleted.Checked = $true
            Wait-Ui { $grid.Rows.Count -eq 1 } 'Including depleted stock should display the selected source'
            Assert-Equal $grid.Rows[0].Cells['SoCuon_ExcelMoi'].Value 0 'Zero stock remains a number'

            [Threading.SynchronizationContext]::SetSynchronizationContext([Windows.Forms.WindowsFormsSynchronizationContext]::new())
            $lockConnection = [DG_TonKhoBTP_v02.Database.DB_Base]::OpenConnection()
            $lockCommand = $lockConnection.CreateCommand()
            try {
                $lockCommand.CommandText = 'BEGIN EXCLUSIVE'
                $null = $lockCommand.ExecuteNonQuery()
                $lateReport = $control.TonKho_Excel($false)
                Assert-Equal $lateReport.IsCompleted $false 'Inventory report waits on the database lock'
                $newerView = [Data.DataTable]::new()
                $null = $newerView.Columns.Add('NewerReport')
                $null = $newerView.Rows.Add('Newer result')
                $showResult = $control.GetType().GetMethod('ShowResult', [Reflection.BindingFlags]'Instance,NonPublic')
                $null = $showResult.Invoke($control, [object[]](,$newerView))
                Assert-Equal $grid.Columns.Contains('NewerReport') $true 'Newer report is displayed while the inventory report is pending'
            } finally {
                $lockCommand.Dispose()
                $lockConnection.Dispose()
            }
            Wait-Ui { $lateReport.IsCompleted } 'Superseded report did not finish'
            $newerViewSurvives = $grid.Columns.Contains('NewerReport')
            Assert-Equal $newerViewSurvives $true 'Superseded inventory report does not replace the newer view'
            $reloaded = $control.TonKho_Excel($false)
            Wait-Ui { $reloaded.IsCompleted } 'Report did not restore the test inventory'
            Assert-Equal $grid.Columns.Contains('Button_ExcelMoi') $true 'Reload restores report mode'
            Invoke-FixtureSql "UPDATE KeHoach SET TrangThai='HUY'; UPDATE TTThanhPham SET NhapKho=0;"
            Assert-Equal @(Read-Report).Count 0 'Fixture has no report rows left'
            [Threading.SynchronizationContext]::SetSynchronizationContext([Windows.Forms.WindowsFormsSynchronizationContext]::new())
            $timer = [Windows.Forms.Timer]::new()
            $timer.Interval = 25
            $timer.add_Tick({ [InventoryReportTestDialogs]::DismissEmptyReport() })
            try {
                $timer.Start()
                $emptyReport = $control.TonKho_Excel($false)
                Wait-Ui { $emptyReport.IsCompleted } 'Empty report did not finish loading'
            } finally { $timer.Dispose() }
            Assert-Equal $grid.Rows.Count 0 'Empty reload removes stale rows and detail buttons'

        } finally { $hostForm.Dispose() }
    }
}

if ($Suite -ne 'Ui') {
    Test-ReportData
    Test-ReportHistory
}
if ($Suite -ne 'Data') { Test-ReportUi }
if ($script:Failures -gt 0) { exit 1 }
