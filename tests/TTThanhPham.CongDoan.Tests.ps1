param(
    [Parameter(Mandatory=$true)][string]$SourceDatabase,
    [string]$BinDirectory
)

$ErrorActionPreference = 'Stop'
if (!$BinDirectory) { $BinDirectory = Join-Path (Split-Path $PSScriptRoot -Parent) 'DG_TonKhoBTP_v02/bin/Debug' }
$sourcePath = (Resolve-Path -LiteralPath $SourceDatabase).Path
$fixture = Join-Path ([IO.Path]::GetTempPath()) ('StageClassification-' + [Guid]::NewGuid().ToString('N') + '.db')
$assembly = [Reflection.Assembly]::LoadFrom((Join-Path $BinDirectory 'DG_TonKhoBTP_v02.exe'))
$null = [Reflection.Assembly]::LoadFrom((Join-Path $BinDirectory 'System.Data.SQLite.dll'))
$connectionString = "Data Source=$fixture;Version=3;Foreign Keys=True;Pooling=False;"

function New-Model([string]$Name) {
    [Activator]::CreateInstance($assembly.GetType("DG_TonKhoBTP_v02.Models.SanXuat.$Name", $true), $true)
}

function Assert-Equal($Actual, $Expected, [string]$Label) {
    if ($Actual -cne $Expected) { throw "$Label expected [$Expected], got [$Actual]" }
}

function Invoke-Sql([string]$Sql) {
    $connection = [System.Data.SQLite.SQLiteConnection]::new($connectionString)
    try {
        $connection.Open()
        $command = $connection.CreateCommand()
        try {
            $command.CommandText = $Sql
            $command.ExecuteScalar()
        } finally { $command.Dispose() }
    } finally { $connection.Dispose() }
}

function New-Product([int]$ProductId, [string]$Lot, $Stage) {
    $product = [DG_TonKhoBTP_v02.Core.TTThanhPham]::new()
    $product.DanhSachSP_ID = $ProductId
    $product.MaBin = $Lot
    $product.CongDoan = $Stage
    $product.ChieuDaiTruoc = 100
    $product.ChieuDaiSau = 100
    $product.DateInsert = '2026-11-11 08:00:00'
    $product
}

function Save-Product($Product, $Detail, [switch]$ExpectFailure) {
    $arguments = [object[]]@($shift, $Product, $materials, $Detail, $null, $false, '')
    $saved = $submit.GetMethod('SaveDataSanPhamTrucTiepVoiKiemTraTon').Invoke($null, $arguments)
    if ($ExpectFailure) {
        Assert-Equal $saved $false 'Rejected save'
        return $arguments[6]
    }
    if (!$saved) { throw "Save failed: $($arguments[6])" }
    [long](Invoke-Sql "SELECT id FROM TTThanhPham WHERE MaBin = '$($Product.MaBin)'")
}

function Edit-Product([int]$Id, $Product, $Detail) {
    $arguments = [object[]]@($Id, $shift, $Product, $materials, $Detail, $null, 'stage-test', '')
    if (!$submit.GetMethod('UpdateDataSanPhamVoiDanhSachLoiNhapLieu').Invoke($null, $arguments)) {
        throw "Edit failed: $($arguments[7])"
    }
}

function Assert-Stage([long]$Id, [string]$Expected, [string]$Label) {
    Assert-Equal (Invoke-Sql "SELECT CongDoan FROM TTThanhPham WHERE id=$Id") $Expected $Label
}

function Assert-CoreDetail([long]$Id) {
    Assert-Equal (Invoke-Sql "SELECT count(*) FROM CD_GhepLoiQB WHERE TTThanhPham_ID=$Id") 1 "Core detail for product row $Id"
}

try {
    $source = [System.Data.SQLite.SQLiteConnection]::new("Data Source=$sourcePath;Version=3;Read Only=True;FailIfMissing=True;Pooling=False;")
    $destination = [System.Data.SQLite.SQLiteConnection]::new($connectionString)
    try {
        $source.Open()
        $destination.Open()
        $source.BackupDatabase($destination, 'main', 'main', -1, $null, 0)
    } finally {
        $destination.Dispose()
        $source.Dispose()
    }

    [DG_TonKhoBTP_v02.Database.DB_Base]::_connStr = $connectionString
    [DG_TonKhoBTP_v02.Database.DatabaseHelper]::_connStr = $connectionString
    $submit = $assembly.GetType('DG_TonKhoBTP_v02.Database.SanXuat.SubmitForm_DB', $true)
    $draftDb = $assembly.GetType('DG_TonKhoBTP_v02.Database.SanXuat.LuuTam_DB', $true)
    $coreStage = [DG_TonKhoBTP_v02.Dictionary.ThongTinChungCongDoan]::GhepLoi
    $wrapStage = [DG_TonKhoBTP_v02.Dictionary.ThongTinChungCongDoan]::QuanBang
    $shift = [DG_TonKhoBTP_v02.Core.ThongTinCaLamViec]::new()
    $shift.Ca = '1'
    $shift.CaKetThuc = '1'
    $shift.NgayBatDau = [datetime]'2026-11-11'
    $shift.NgayKetThuc = $shift.NgayBatDau
    $shift.GioBatDau = [timespan]'08:00:00'
    $shift.GioKetThuc = [timespan]'09:00:00'
    $shift.May = 'TEST'
    $shift.NguoiLam = 'stage-test'
    $shift.NguoiKetThuc = $shift.NguoiLam
    $materials = [Collections.Generic.List[DG_TonKhoBTP_v02.Core.TTNVL]]::new()
    $detail = New-Model 'SubmitCongDoanData'
    $detail.ChiTietCongDoan = [DG_TonKhoBTP_v02.Core.CD_GhepLoiQB]::new()
    $detail.ChiTietCongDoan.ChieuXoan = 'Z'
    $lotPrefix = 'TEST-STAGE-' + [Guid]::NewGuid().ToString('N')

    $allowed = [DG_TonKhoBTP_v02.Database.CongDoanTimKiem_DB]::TimSanPham(6, 'AX 2X16RC-0.6-ABC')
    Assert-Equal (@($allowed.Rows | Where-Object { $_['id'] -eq 5084 }).Count) 1 'V3 allows catalog product 5084'
    $tp = New-Product 5084 "$lotPrefix-TP" $coreStage
    $tp.MaTP = 'BTP.20104001'
    $tpId = Save-Product $tp $detail
    Assert-Stage $tpId '5' 'Direct Ghép lõi TP. uses catalog product ID'
    Assert-CoreDetail $tpId

    $btp = New-Product 749 "$lotPrefix-BTP" $coreStage
    $btpId = Save-Product $btp $detail
    Assert-Stage $btpId '6' 'Direct Ghép lõi BTP.'
    Assert-CoreDetail $btpId

    $copy = New-Product 5084 "$lotPrefix-COPY" $coreStage
    $copyId = Save-Product $copy $detail
    Assert-Stage $copyId '5' 'Copy uses the same direct save entry point'
    Assert-CoreDetail $copyId

    $other = New-Product 5084 "$lotPrefix-WRAP" $wrapStage
    $otherId = Save-Product $other $detail
    Assert-Stage $otherId '7' 'Quấn băng uses its own stage even with the same product prefix'
    Assert-CoreDetail $otherId

    $null = Invoke-Sql "UPDATE DanhSachMaSP SET Ma='  tp.31700100  ' WHERE id=5084"
    $normalized = New-Product 5084 "$lotPrefix-NORMALIZED" $coreStage
    $normalizedId = Save-Product $normalized $detail
    Assert-Stage $normalizedId '5' 'Catalog prefix is trimmed and case insensitive'
    $null = Invoke-Sql "UPDATE DanhSachMaSP SET Ma='TP.31700100' WHERE id=5084"
    $weldStage = [DG_TonKhoBTP_v02.Dictionary.ThongTinChungCongDoan]::HanNoi
    $weld = New-Product 5084 "$lotPrefix-WELD" $weldStage
    $weldArguments = [object[]]@($shift, $weld, '')
    if (!$submit.GetMethod('SaveDataCongDoan9').Invoke($null, $weldArguments)) {
        throw "Stage 9 save failed: $($weldArguments[2])"
    }
    $weldId = Invoke-Sql "SELECT id FROM TTThanhPham WHERE MaBin='$($weld.MaBin)'"
    Assert-Stage $weldId '9' 'Hàn nối saves with its own stage'
    Assert-Equal (Invoke-Sql "SELECT HanNoi FROM TTThanhPham WHERE id=$weldId") 1 'Hàn nối preserves its flag'
    Write-Output 'PASS direct TP., BTP., copy, catalog normalization, Quấn băng and Hàn nối'

    $draft = New-Model 'DraftSubmitData'
    $draft.CongDoanId = 6
    $draft.ThongTinThanhPham = New-Product 5084 "$lotPrefix-DRAFT" $coreStage
    $draft.ThongTinCaLamViec = $shift
    $draft.CongDoan = $detail
    $arguments = [object[]]@($draft, [long]0, '')
    if (!$draftDb.GetMethod('SaveDraft').Invoke($null, $arguments)) {
        throw "Draft save failed: $($arguments[2])"
    }
    $draftId = $arguments[1]
    Assert-Stage $draftId '6' 'Draft stage'
    Assert-Equal (Invoke-Sql "SELECT Temp FROM TTThanhPham WHERE id=$draftId") 1 'Draft Temp'
    $openedDraft = [DG_TonKhoBTP_v02.Database.DatabaseHelper]::GetDataByID("$draftId", $coreStage, 3)
    Assert-Equal $openedDraft.Rows.Count 1 'Draft reopens on Ghép lõi before finalization'
    $arguments = [object[]]@($draftId, $draft, $null, $false, '')
    if (!$draftDb.GetMethod('FinalizeDraft').Invoke($null, $arguments)) {
        throw "Draft finalize failed: $($arguments[4])"
    }
    Assert-Stage $draftId '5' 'Finalized draft classification'
    Assert-Equal (Invoke-Sql "SELECT Temp FROM TTThanhPham WHERE id=$draftId") 0 'Finalized draft Temp'
    Assert-CoreDetail $draftId
    Write-Output 'PASS draft stage, finalization and detail'

    $tp.GhiChu = 'note edit'
    Edit-Product $tpId $tp $detail
    Assert-Stage $tpId '5' 'Note-only edit'
    $tp.DanhSachSP_ID = 749
    Edit-Product $tpId $tp $detail
    Assert-Stage $tpId '6' 'Edit TP. to BTP.'
    $tp.DanhSachSP_ID = 5085
    Edit-Product $tpId $tp $detail
    Assert-Stage $tpId '5' 'Edit BTP. to another TP.'
    Assert-Equal (Invoke-Sql "SELECT DanhSachSP_ID FROM TTThanhPham WHERE id=$tpId") 5085 'Updated catalog FK'
    Assert-CoreDetail $tpId

    $null = Invoke-Sql "DELETE FROM CD_GhepLoiQB WHERE TTThanhPham_ID=$btpId"
    $btp.GhiChu = 'legacy note'
    Edit-Product $btpId $btp $detail
    Assert-Stage $btpId '6' 'Legacy same product preserves classification'
    Assert-Equal (Invoke-Sql "SELECT count(*) FROM CD_GhepLoiQB WHERE TTThanhPham_ID=$btpId") 0 'Legacy note does not create detail'
    $btp.DanhSachSP_ID = 5084
    Edit-Product $btpId $btp $detail
    Assert-Stage $btpId '5' 'Legacy row reclassified with new detail'
    Assert-CoreDetail $btpId
    Write-Output 'PASS edit notes, both prefix transitions and legacy missing detail'

    $null = Invoke-Sql "CREATE TRIGGER test_reject_gl_detail BEFORE INSERT ON CD_GhepLoiQB WHEN NEW.ChieuXoan='FAIL_TEST' BEGIN SELECT RAISE(ABORT, 'test detail refusal'); END"
    $detail.ChiTietCongDoan.ChieuXoan = 'FAIL_TEST'
    $rollbackProduct = New-Product 5084 "$lotPrefix-ROLLBACK" $coreStage
    $failure = Save-Product $rollbackProduct $detail -ExpectFailure
    if ($failure -notmatch 'test detail refusal') { throw "Unexpected failure: $failure" }
    Assert-Equal (Invoke-Sql "SELECT count(*) FROM TTThanhPham WHERE MaBin='$($rollbackProduct.MaBin)'") 0 'Rollback removes classified header'
    $detail.ChiTietCongDoan.ChieuXoan = 'Z'
    Write-Output 'PASS failed detail insert rolls back classified header'

    $jacketStage = [DG_TonKhoBTP_v02.Dictionary.ThongTinChungCongDoan]::BocVo
    $jacket = New-Product 5085 "$lotPrefix-JACKET" $jacketStage
    $jacketDetail = New-Model 'SubmitCongDoanData'
    $jacketDetail.ChiTietCongDoan = [DG_TonKhoBTP_v02.Core.CD_BocVo]::new()
    $jacketDetail.CaiDatCDBoc = [DG_TonKhoBTP_v02.Core.CaiDatCDBoc]::new()
    $jacketId = Save-Product $jacket $jacketDetail
    Assert-Stage $jacketId '5' 'Native jacket stage'

    foreach ($mode in 0, 1, 2) {
        $opened = [DG_TonKhoBTP_v02.Database.DatabaseHelper]::GetDataByID("$tpId", $coreStage, $mode)
        Assert-Equal $opened.Rows.Count 1 "Ghép lõi opens classified STT in mode $mode"
        Assert-Equal $opened.Rows[0]['DanhSachMaSP_ID'] 5085 'Opened catalog product'
        Assert-Equal $opened.Rows[0]['GhepLoi_ChieuXoan'] 'Z' 'Opens original detail'
        $opened = [DG_TonKhoBTP_v02.Database.DatabaseHelper]::GetDataByID("$tpId", $jacketStage, $mode)
        Assert-Equal $opened.Rows.Count 0 "Bọc vỏ excludes a Ghép lõi STT in mode $mode"
    }
    $opened = [DG_TonKhoBTP_v02.Database.DatabaseHelper]::GetDataByID("$jacketId", $jacketStage, 2)
    Assert-Equal $opened.Rows.Count 1 'Bọc vỏ can still open its own record with the same catalog product'
    $opened = [DG_TonKhoBTP_v02.Database.DatabaseHelper]::GetDataByID("$jacketId", $coreStage, 2)
    Assert-Equal $opened.Rows.Count 0 'Ghép lõi excludes an actual Bọc vỏ record'
    $opened = [DG_TonKhoBTP_v02.Database.DatabaseHelper]::GetDataByID("$otherId", $wrapStage, 2)
    Assert-Equal $opened.Rows.Count 1 'Quấn băng keeps its own record with shared detail table'
    Write-Output 'PASS open classified and native records in their production screens'

    $coreOnly = [Collections.Generic.List[DG_TonKhoBTP_v02.Models.CongDoan]]::new()
    $coreOnly.Add($coreStage)
    $jacketOnly = [Collections.Generic.List[DG_TonKhoBTP_v02.Models.CongDoan]]::new()
    $jacketOnly.Add($jacketStage)
    $both = [Collections.Generic.List[DG_TonKhoBTP_v02.Models.CongDoan]]::new()
    $both.Add($coreStage)
    $both.Add($jacketStage)
    $date = $shift.NgayKetThuc
    $coreProduction = [DG_TonKhoBTP_v02.Database.DatabaseHelper]::GetDataBaoCaoSX($date, $date, $coreOnly)
    $jacketProduction = [DG_TonKhoBTP_v02.Database.DatabaseHelper]::GetDataBaoCaoSX($date, $date, $jacketOnly)
    $bothProduction = [DG_TonKhoBTP_v02.Database.DatabaseHelper]::GetDataBaoCaoSX($date, $date, $both)
    $legacyProduction = [DG_TonKhoBTP_v02.Database.DB_SanXuat]::GetDataBaoCaoSX($date, $date, $coreOnly)
    $review = [DG_TonKhoBTP_v02.Database.DatabaseHelper]::GetDataByCongDoan($date, $coreStage, 1, 'stage-test')
    foreach ($report in $coreProduction, $bothProduction, $legacyProduction, $review) {
        Assert-Equal (@($report.Rows | Where-Object { $_['STT'] -eq $tpId }).Count) 1 'Production counts this Ghép lõi STT once'
    }
    Assert-Equal (@($jacketProduction.Rows | Where-Object { $_['STT'] -eq $tpId }).Count) 0 'Bọc vỏ production excludes Ghép lõi STT'
    Assert-Equal (@($jacketProduction.Rows | Where-Object { $_['STT'] -eq $jacketId }).Count) 1 'Bọc vỏ production retains native STT'

    $monthly = $assembly.GetType('DG_TonKhoBTP_v02.Database.SanXuat.MonthyReport_DB', $true).GetMethod('GetMonthlyReport')
    $coreMonthly = $monthly.Invoke($null, [object[]]@($date, [int]6))
    $jacketMonthly = $monthly.Invoke($null, [object[]]@($date, [int]5))
    Assert-Equal (@($coreMonthly | Where-Object Id -eq $tpId).Count) 1 'Monthly production uses actual Ghép lõi'
    Assert-Equal (@($jacketMonthly | Where-Object Id -eq $tpId).Count) 0 'Monthly Bọc vỏ excludes classified Ghép lõi'
    Assert-Equal (@($jacketMonthly | Where-Object Id -eq $jacketId).Count) 1 'Monthly Bọc vỏ retains native STT'
    Write-Output 'PASS production, review and monthly reports use the operation'

    $coreInventory = [DG_TonKhoBTP_v02.Database.DatabaseHelper]::GetTonKhoCD($coreOnly)
    $jacketInventory = [DG_TonKhoBTP_v02.Database.DatabaseHelper]::GetTonKhoCD($jacketOnly)
    Assert-Equal (@($coreInventory.Rows | Where-Object { $_['STT'] -eq $tpId }).Count) 0 'Ghép lõi inventory excludes stored stage 5'
    Assert-Equal (@($jacketInventory.Rows | Where-Object { $_['STT'] -eq $tpId }).Count) 1 'Bọc vỏ inventory uses stored stage 5'
    Write-Output 'PASS inventory uses the stored classification'

    $null = Invoke-Sql "INSERT INTO TTNVL(TTThanhPham_ID,DanhSachMaSP_ID,BinNVL) VALUES ($tpId,749,'$lotPrefix-NVL1'),($tpId,749,'$lotPrefix-NVL2')"
    $coreProduction = [DG_TonKhoBTP_v02.Database.DatabaseHelper]::GetDataBaoCaoSX($date, $date, $coreOnly)
    $jacketProduction = [DG_TonKhoBTP_v02.Database.DatabaseHelper]::GetDataBaoCaoSX($date, $date, $jacketOnly)
    $bothProduction = [DG_TonKhoBTP_v02.Database.DatabaseHelper]::GetDataBaoCaoSX($date, $date, $both)
    Assert-Equal (@($coreProduction.Rows | Where-Object { $_['STT'] -eq $tpId }).Count) 2 'Ghép lõi report retains one row per material'
    Assert-Equal (@($bothProduction.Rows | Where-Object { $_['STT'] -eq $tpId }).Count) 2 'Combined report does not count the LOT in another stage'
    Assert-Equal (@($jacketProduction.Rows | Where-Object { $_['STT'] -eq $tpId }).Count) 0 'Bọc vỏ report excludes the Ghép lõi LOT with materials'
    Write-Output 'PASS multi-material report retains its row shape and unique stage'

    $coreReceipt = [DG_TonKhoBTP_v02.Database.ChatLuong.NhapKho_DB]::TimKiemNhapKhoTheoNgay(
        $date, $date, '1', $null, $null, $tpId, -1)
    Assert-Equal $coreReceipt.Items.Count 0 'Classified Ghép lõi LOT without source reels is ineligible for receipt'
    Assert-Equal $coreReceipt.MaBinBatThuong.Count 0 'No false receipt anomaly for a Ghép lõi LOT'
    $jacketReceipt = [DG_TonKhoBTP_v02.Database.ChatLuong.NhapKho_DB]::TimKiemNhapKhoTheoNgay(
        $date, $date, '1', $null, $null, $jacketId, -1)
    Assert-Equal $jacketReceipt.Items.Count 0 'Native Bọc vỏ LOT without source reels is ineligible'
    Assert-Equal $jacketReceipt.MaBinBatThuong.Contains($jacket.MaBin) $true 'Native Bọc vỏ retains missing-reels warning'
    $null = Invoke-Sql @"
INSERT INTO TTCuonDay_CD(SoCuon,TongChieuDai,SoDau,SoCuoi,CongDoan_ID)
SELECT 1,100,0,100,b.id
FROM CD_BocVo b JOIN CaiDatCDBoc c ON c.id=b.CaiDatCDBoc_ID
WHERE c.TTThanhPham_ID=$jacketId
"@
    $jacketReceipt = [DG_TonKhoBTP_v02.Database.ChatLuong.NhapKho_DB]::TimKiemNhapKhoTheoNgay(
        $date, $date, '1', $null, $null, $jacketId, -1)
    Assert-Equal $jacketReceipt.Items.Count 1 'Native Bọc vỏ with valid source reels remains eligible'
    Assert-Equal $jacketReceipt.MaBinBatThuong.Count 0 'Valid Bọc vỏ has no receipt anomaly'
    Write-Output 'PASS receipt distinguishes missing jacket data from a Ghép lõi LOT'
} finally {
    if (Test-Path -LiteralPath $fixture) { Remove-Item -LiteralPath $fixture -ErrorAction Stop }
}
