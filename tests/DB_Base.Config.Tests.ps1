param(
    [string]$AssemblyPath = (Join-Path $PSScriptRoot '..\DG_TonKhoBTP_v02\bin\Debug\DG_TonKhoBTP_v02.exe')
)

# Exercises the shared configuration gate used by SubmitForm and BCTonKho.
# Uses only disposable databases; never reads the application's saved database path.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Data
Add-Type -ReferencedAssemblies System.Windows.Forms -TypeDefinition @'
public static class DatabaseConfigAlertCloser
{
    public static System.Windows.Forms.Timer Start()
    {
        var timer = new System.Windows.Forms.Timer { Interval = 1 };
        timer.Tick += (sender, args) =>
        {
            for (int i = System.Windows.Forms.Application.OpenForms.Count - 1; i >= 0; i--)
            {
                var form = System.Windows.Forms.Application.OpenForms[i];
                if (form.GetType().FullName == "DG_TonKhoBTP_v02.UI.FrmWaiting")
                    form.Close();
            }
        };
        timer.Start();
        return timer;
    }
}
'@
$assembly = [Reflection.Assembly]::LoadFrom((Resolve-Path $AssemblyPath).Path)
$null = [Reflection.Assembly]::LoadFrom((Join-Path (Split-Path $AssemblyPath) 'System.Data.SQLite.dll'))
$submitType = $assembly.GetType('DG_TonKhoBTP_v02.Database.SanXuat.SubmitForm_DB', $true)
$oldBase = [DG_TonKhoBTP_v02.Database.DB_Base]::_connStr
$oldHelper = [DG_TonKhoBTP_v02.Database.DatabaseHelper]::_connStr
$testPath = Join-Path ([IO.Path]::GetTempPath()) ('Codex.DatabaseConfig.' + [Guid]::NewGuid().ToString('N') + '.db')
$builder = New-Object System.Data.SQLite.SQLiteConnectionStringBuilder
$builder.DataSource = [string]$testPath
$builder.Pooling = $false
$testConnection = $builder.ConnectionString
$script:passed = 0
$script:failed = 0

function Assert-Equal($expected, $actual, [string]$message) {
    if ($expected -ne $actual) { throw "$message (expected '$expected', actual '$actual')" }
}

function Test-Case([string]$name, [scriptblock]$check) {
    try {
        [DG_TonKhoBTP_v02.Database.DB_Base]::_connStr = $testConnection
        [DG_TonKhoBTP_v02.Database.DatabaseHelper]::_connStr = $null
        & $check
        $script:passed++; Write-Output "PASS: $name"
    }
    catch { $script:failed++; Write-Output "FAIL: $name - $($_.Exception.Message)" }
}

function Set-Config([string]$rows = '') {
    $conn = New-Object System.Data.SQLite.SQLiteConnection($testConnection)
    try {
        $conn.Open()
        $cmd = $conn.CreateCommand()
        try {
            $cmd.CommandText = 'CREATE TABLE IF NOT EXISTS KhoaDatabase(ID INTEGER PRIMARY KEY, Active INTEGER, Author TEXT, Message TEXT); DELETE FROM KhoaDatabase; ' + $rows
            $null = $cmd.ExecuteNonQuery()
        }
        finally { $cmd.Dispose() }
    }
    finally { $conn.Dispose() }
}

try {
    Test-Case 'Configuration comes from DB_Base and selects the greatest ID' {
        Set-Config "INSERT INTO KhoaDatabase VALUES(20,0,'Supervisor','Maintenance'),(3,1,'Old','Allowed');"
        $config = [DG_TonKhoBTP_v02.Database.DB_Base]::GetConfig()
        Assert-Equal $false $config.Active 'Latest configuration blocks writes'
        Assert-Equal 'Supervisor' $config.Author 'Latest author'
        Assert-Equal 'Maintenance' $config.Message 'Latest message'
    }

    Test-Case 'Null configuration fields retain the existing defaults' {
        Set-Config 'INSERT INTO KhoaDatabase VALUES(1,NULL,NULL,NULL);'
        $config = [DG_TonKhoBTP_v02.Database.DB_Base]::GetConfig()
        Assert-Equal $false $config.Active 'Null Active remains inactive'
        Assert-Equal $null $config.Author 'Null author'
        Assert-Equal $null $config.Message 'Null message'
    }

    Test-Case 'An empty configuration table returns no configuration' {
        Set-Config
        Assert-Equal $null ([DG_TonKhoBTP_v02.Database.DB_Base]::GetConfig()) 'No configuration'
    }

    Test-Case 'The shared gate allows saving with only DB_Base configured' {
        Set-Config "INSERT INTO KhoaDatabase VALUES(1,1,'Supervisor','Allowed');"
        $label = New-Object System.Windows.Forms.Label
        try {
            Assert-Equal '' ([DG_TonKhoBTP_v02.Helper.Helper]::TaoThongBao($label)) 'Save remains allowed'
            Assert-Equal $false $label.Visible 'No blocking notification'
        }
        finally { $label.Dispose() }
    }

    Test-Case 'The shared gate allows saving when configuration is absent' {
        Set-Config
        Assert-Equal '' ([DG_TonKhoBTP_v02.Helper.Helper]::TaoThongBao()) 'Missing configuration remains allowed'
    }

    Test-Case 'The shared gate still blocks saving and displays the configured notice' {
        Set-Config "INSERT INTO KhoaDatabase VALUES(1,0,'Supervisor','Maintenance');"
        $label = New-Object System.Windows.Forms.Label
        $timer = [DatabaseConfigAlertCloser]::Start()
        try {
            $message = [DG_TonKhoBTP_v02.Helper.Helper]::TaoThongBao($label)
            if ([string]::IsNullOrWhiteSpace($message)) { throw 'Inactive configuration allowed saving' }
            Assert-Equal 'SUPERVISOR: MAINTENANCE ' $message 'Configured blocking notice'
            Assert-Equal $message $label.Text 'Label shows the same notice'
            Assert-Equal $true $label.Visible 'Blocking notification is visible'
        }
        finally { $timer.Stop(); $timer.Dispose(); $label.Dispose() }
    }

    foreach ($method in @('SaveDataCongDoan9', 'UpdateDataCongDoan9')) {
        Test-Case "$method rejects an unset DB_Base connection even when DatabaseHelper is configured" {
            [DG_TonKhoBTP_v02.Database.DB_Base]::_connStr = $null
            [DG_TonKhoBTP_v02.Database.DatabaseHelper]::_connStr = $testConnection
            $shift = New-Object DG_TonKhoBTP_v02.Core.ThongTinCaLamViec
            $product = New-Object DG_TonKhoBTP_v02.Core.TTThanhPham
            if ($method -eq 'SaveDataCongDoan9') {
                [object[]]$arguments = @($shift, $product, $null)
            }
            else { [object[]]$arguments = @([int]1, $shift, $product, 'Supervisor', $null) }
            for ($i=0; $i -lt $arguments.Length; $i++) {
                if ($null -ne $arguments[$i]) { $arguments[$i] = $arguments[$i].PSObject.BaseObject }
            }
            $success = $submitType.GetMethod($method).Invoke($null, $arguments)
            Assert-Equal $false $success 'Missing connection rejects saving'
            if ([string]::IsNullOrWhiteSpace($arguments[-1])) { throw 'No connection error returned' }
        }
    }
}
finally {
    [DG_TonKhoBTP_v02.Database.DB_Base]::_connStr = $oldBase
    [DG_TonKhoBTP_v02.Database.DatabaseHelper]::_connStr = $oldHelper
    if ([IO.File]::Exists($testPath)) { [IO.File]::Delete($testPath) }
}

Write-Output "RESULT: $script:passed passed, $script:failed failed"
if ($script:failed -gt 0) { exit 1 }
