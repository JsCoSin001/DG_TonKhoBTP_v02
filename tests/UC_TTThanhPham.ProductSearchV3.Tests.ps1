param([switch]$Baseline)

$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Windows.Forms
[Windows.Forms.Application]::SetUnhandledExceptionMode([Windows.Forms.UnhandledExceptionMode]::ThrowException)
[Windows.Forms.Control]::CheckForIllegalCrossThreadCalls=$true

$root=Split-Path $PSScriptRoot -Parent
$bin=Join-Path $root 'DG_TonKhoBTP_v02/bin/Debug'
$null=[Reflection.Assembly]::LoadFrom((Join-Path $bin 'DG_TonKhoBTP_v02.exe'))
$null=[Reflection.Assembly]::LoadFrom((Join-Path $bin 'System.Data.SQLite.dll'))
$db=Join-Path ([IO.Path]::GetTempPath()) ('ProductSearchV3-'+[Guid]::NewGuid().ToString('N')+'.db')
$connectionString="Data Source=$db;Version=3;Foreign Keys=True;"
$original=[DG_TonKhoBTP_v02.Database.DatabaseHelper]::_connStr
[DG_TonKhoBTP_v02.Database.DatabaseHelper]::_connStr=$connectionString

function Invoke-Sql([string]$Sql) {
    $connection=[System.Data.SQLite.SQLiteConnection]::new($connectionString)
    try {
        $connection.Open()
        $cmd=$connection.CreateCommand()
        try { $cmd.CommandText=$Sql; $null=$cmd.ExecuteNonQuery() }
        finally { $cmd.Dispose() }
    } finally { $connection.Dispose() }
}

function Assert-Equal($Actual,$Expected,[string]$Label) {
    if($Actual -cne $Expected){throw "$Label expected [$Expected], got [$Actual]"}
}

function Wait-Ui([scriptblock]$Condition,[string]$Failure) {
    $watch=[Diagnostics.Stopwatch]::StartNew()
    while($watch.ElapsedMilliseconds -lt 5000){
        [Windows.Forms.Application]::DoEvents()
        if(& $Condition){return}
        Start-Sleep -Milliseconds 20
    }
    throw $Failure
}

function Enter-Keyword($Search,[string]$Word) {
    $Search.Text=$Word
    $update=$Search.GetType().GetMethod('OnTextUpdate',[Reflection.BindingFlags]'Instance,NonPublic')
    $null=$update.Invoke($Search,[object[]]@([EventArgs]::Empty))
}

function Test-Suggestions($Stage) {
    $form=[Windows.Forms.Form]::new()
    $form.ShowInTaskbar=$false
    $timer=[Windows.Forms.Timer]::new()
    $timer.Interval=1
    $state=[pscustomobject]@{Failure=$null}
    $timer.add_Tick({
        $timer.Stop()
        try{
            $uc=[DG_TonKhoBTP_v02.UI.UC_TTThanhPham]::new($Stage)
            $uc.Dock=[Windows.Forms.DockStyle]::Fill
            $stagePanel=[Windows.Forms.Panel]::new()
            $stagePanel.Dock=[Windows.Forms.DockStyle]::Fill
            $form.Controls.Add($stagePanel)
            $stagePanel.Controls.Add($uc)
            $search=$uc.Controls.Find('timTenTPCongDoan',$true)[0]
            Enter-Keyword $search 'CX'
            Wait-Ui {$search.Items.Count -eq 1 -and $search.DroppedDown} 'Initial suggestion did not open'
            Assert-Equal $search.Items[0].Row['id'] 101 'Original matching product'

            Enter-Keyword $search 'no match'
            Assert-Equal $search.Items.Count 0 'Previous suggestion cannot be selected during debounce'
            Assert-Equal $search.Text 'no match' 'Typed keyword is preserved'

            if(!$Baseline){
                Invoke-Sql "INSERT INTO CongDoan_DacBiet(DanhSachCongDoan_id,DanhSachMaSP_ID,LoaiQuyTac) VALUES(142,101,'LOAI_TRU'),(142,103,'CHO_PHEP');"
                Enter-Keyword $search 'CX'
                Wait-Ui {$search.Items.Count -eq 1 -and $search.DroppedDown} 'Rule change did not appear on the next search'
                Assert-Equal $search.Items[0].Row['id'] 103 'Old result is denied and explicitly allowed result appears'
            }
            Enter-Keyword $search 'CX'
            Enter-Keyword $search ''
            $watch=[Diagnostics.Stopwatch]::StartNew()
            while($watch.ElapsedMilliseconds -lt 800){
                [Windows.Forms.Application]::DoEvents()
                Start-Sleep -Milliseconds 20
            }
            Assert-Equal $search.Items.Count 0 'Clearing text cancels pending suggestions'
            Assert-Equal $search.DroppedDown $false 'Cleared keyword does not reopen the dropdown'

            Enter-Keyword $search 'CX'
            $form.Controls.Remove($stagePanel)
            $watch.Restart()
            while($watch.ElapsedMilliseconds -lt 800){
                [Windows.Forms.Application]::DoEvents()
                Start-Sleep -Milliseconds 20
            }
            Assert-Equal $search.Items.Count 0 'A detached stage does not apply pending results'
        }catch{$state.Failure=$_}
        finally{
            if($stagePanel){$stagePanel.Dispose()}
            $form.Close()
        }
    }.GetNewClosure())
    try {
        $timer.Start()
        [Windows.Forms.Application]::Run($form)
    } finally {
        $timer.Dispose()
        $form.Dispose()
    }
    if($state.Failure){throw $state.Failure}
}

try {
    Invoke-Sql @"
CREATE TABLE DanhSachCongDoan(id INTEGER PRIMARY KEY, MaCongDoan INTEGER, TenCongDoan TEXT);
WITH RECURSIVE stages(n) AS (SELECT 0 UNION ALL SELECT n+1 FROM stages WHERE n<10)
INSERT INTO DanhSachCongDoan SELECT 100+n*7,n,'Stage '||n FROM stages;
CREATE TABLE DanhSachMaSP(id INTEGER PRIMARY KEY, Ten TEXT, Ma TEXT, DonVi TEXT, ChuyenDoi REAL, Active INTEGER);
INSERT INTO DanhSachMaSP VALUES(101,'CX match','BTP.20104001','m',1,1),(103,'CX exception','TP.30100100','m',1,1),(105,'CX inactive','BTP.20104008','m',1,0);
CREATE TABLE TTThanhPham(id INTEGER PRIMARY KEY, DanhSachSP_ID INTEGER, Temp INTEGER, CongDoan TEXT);
CREATE TABLE TTNVL(TTThanhPham_ID INTEGER, DanhSachMaSP_ID INTEGER);
INSERT INTO TTThanhPham VALUES(201,101,0,'7'),(202,103,0,'6'),(203,103,1,'6');
"@
    $stage=[DG_TonKhoBTP_v02.Dictionary.ThongTinChungCongDoan]::GhepLoi
    if(!$Baseline){
        $migration=Join-Path $root 'database/migrations/2026-11-10_product_search_rules.sql'
        Invoke-Sql ([IO.File]::ReadAllText($migration))
        $results=[DG_TonKhoBTP_v02.Database.CongDoanTimKiem_DB]::TimSanPham(6,'CX')
        Assert-Equal $results.Rows.Count 1 'Runtime service filters by the configured stage and active product'
        Assert-Equal $results.Rows[0]['id'] 101 'Runtime service uses the catalog FK'
        Invoke-Sql 'UPDATE CongDoan_MaTimKiem SET Active=0 WHERE DanhSachCongDoan_id=142;'
        $message=$null
        try { $null=[DG_TonKhoBTP_v02.Database.CongDoanTimKiem_DB]::TimSanPham(6,'CX') }
        catch { $message=$_.Exception.GetBaseException().Message }
        if($message -notlike '*CHO_PHEP*'){throw 'Runtime service must report missing allowing configuration'}
        Invoke-Sql 'UPDATE CongDoan_MaTimKiem SET Active=1 WHERE DanhSachCongDoan_id=142;'
        'PASS Configured search and missing configuration'

        $reportStage=[DG_TonKhoBTP_v02.Models.CongDoan]::new($stage)
        $reportStage.ListData_Report=[Collections.Generic.List[string]]::new()
        $report=[DG_TonKhoBTP_v02.Helper.ReportHelper]::GetDataReport([DG_TonKhoBTP_v02.Models.CongDoan[]]@($reportStage))
        Assert-Equal $report.Rows.Count 1 'Report excludes temporary rows'
        Assert-Equal $report.Rows[0]['id'] 202 'Report uses stored stage even if product code differs'
        'PASS ReportHelper filters by stored stage'
    }

    Test-Suggestions $stage
    'PASS Stage UI refreshes rule changes and discards stale suggestions'
} finally {
    [DG_TonKhoBTP_v02.Database.DatabaseHelper]::_connStr=$original
    [System.Data.SQLite.SQLiteConnection]::ClearAllPools()
    Remove-Item -LiteralPath $db -ErrorAction SilentlyContinue
}
