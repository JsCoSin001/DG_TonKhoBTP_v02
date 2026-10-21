param(
    [string]$AssemblyPath = (Join-Path $PSScriptRoot '..\DG_TonKhoBTP_v02\bin\Debug\DG_TonKhoBTP_v02.exe')
)

# Run after a Debug build with Windows PowerShell; no database writes.
# The harness automatically closes its own validation alerts.
# powershell -NoProfile -ExecutionPolicy Bypass -STA -File tests\UC_TTCaLamViec.ShiftHours.Tests.ps1
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Data
Add-Type -ReferencedAssemblies System.Windows.Forms -TypeDefinition @'
public static class ShiftHoursAlertCloser
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
$controlType = $assembly.GetType('DG_TonKhoBTP_v02.UI.UC_TTCaLamViec', $true)
$modelType = $assembly.GetType('DG_TonKhoBTP_v02.Core.ThongTinCaLamViec', $true)
$flags = [Reflection.BindingFlags]'Instance,NonPublic'
$today = [DateTime]::Today
$script:passed = 0
$script:failed = 0

function Input-Control($control, [string]$name) {
    return $controlType.GetField($name, $flags).GetValue($control)
}

function Assert-Equal($expected, $actual, [string]$message) {
    if ($expected -ne $actual) { throw "$message (expected '$expected', actual '$actual')" }
}

function Test-Case([string]$name, [scriptblock]$check) {
    try { & $check; $script:passed++; Write-Output "PASS: $name" }
    catch { $script:failed++; Write-Output "FAIL: $name - $($_.Exception.Message)" }
}

function Set-Time($control, [string]$time) {
    (Input-Control $control 'dtGioBD').Value = $today.Add([TimeSpan]::Parse($time))
}

function Select-ShiftNow($control, [string]$shift) {
    $before = [DateTime]::Now
    $before = $before.AddTicks(-($before.Ticks % [TimeSpan]::TicksPerMinute))
    (Input-Control $control 'caBD').SelectedItem = $shift
    $after = [DateTime]::Now
    $actual = (Input-Control $control 'dtGioBD').Value
    if ($actual -lt $before -or $actual -gt $after) { throw "Shift change must use current time, got $actual" }
    Assert-Equal 0 $actual.Second 'No hidden seconds'
    Assert-Equal $true (Input-Control $control 'dtGioBD').Checked 'Current time is selected'
    $expectedDate = $actual.Date
    if ($shift -ne '1' -and $actual.Hour -lt 6) { $expectedDate = $expectedDate.AddDays(-1) }
    Assert-Equal $expectedDate ($control.GetThongTinCaLamViec().NgayBatDau) 'Date after shift change'
}

function Leave-Time($control) {
    $onValidating = [System.Windows.Forms.Control].GetMethod('OnValidating', $flags)
    $cancelArgs = [System.ComponentModel.CancelEventArgs]::new()
    $timer = [ShiftHoursAlertCloser]::Start()
    try {
        $null = $onValidating.Invoke((Input-Control $control 'dtGioBD'), [object[]]@($cancelArgs.PSObject.BaseObject))
    }
    finally { $timer.Stop(); $timer.Dispose() }
}

function Saved-Report([string]$shift, [string]$time) {
    $table = New-Object System.Data.DataTable
    foreach ($name in @('Ca', 'GioBatDau', 'NgayBatDau')) { $null = $table.Columns.Add($name, [string]) }
    $row = $table.NewRow()
    $row['Ca'] = $shift
    $row['GioBatDau'] = $time
    $row['NgayBatDau'] = '2001-02-03'
    $table.Rows.Add($row)
    return ,$table
}

# These literal boundaries catch missing, reversed, and overly broad time ranges.
$cases = @(
    @('1', '05:59:59', $false), @('1', '06:00:00', $true),
    @('1', '14:00:00', $true), @('1', '18:00:00', $true),
    @('1', '18:00:01', $false), @('1', '23:00:00', $false),
    @('2', '00:00:00', $true), @('2', '05:59:59', $true),
    @('2', '06:00:00', $true), @('2', '06:00:01', $false),
    @('2', '13:59:59', $false), @('2', '14:00:00', $true),
    @('2', '18:00:00', $true), @('2', '23:59:59', $true),
    @('3', '00:00:00', $true), @('3', '05:59:59', $true),
    @('3', '06:00:00', $true), @('3', '06:00:01', $false),
    @('3', '21:59:59', $false), @('3', '22:00:00', $true),
    @('3', '23:59:59', $true), @('3', '1.00:00:00', $false),
    @('1', '-00:00:01', $false), @('', '06:00:00', $false),
    @('4', '06:00:00', $false), @('2', $null, $false)
)
foreach ($case in $cases) {
    Test-Case "Official and draft validation: shift '$($case[0])', time '$($case[1])', valid=$($case[2])" {
        $model = [Activator]::CreateInstance($modelType)
        $model.Ca = $case[0]
        $model.NgayBatDau = $today
        $model.NgayKetThuc = $today
        $model.May = 'Test machine'
        $model.NguoiLam = 'Test operator'
        $model.NguoiKetThuc = 'Test operator'
        if ($null -ne $case[1]) { $model.GioBatDau = [TimeSpan]::Parse($case[1]) }
        $official = [DG_TonKhoBTP_v02.Helper.Validator]::LayDanhSachLoiTTCaLamViec($model)
        $draft = [DG_TonKhoBTP_v02.Helper.LuuTamValidator]::LayDanhSachLoiCaLamViec($model)
        Assert-Equal $case[2] ($official.Count -eq 0) 'Official save eligibility'
        Assert-Equal $case[2] ($draft.Count -eq 0) 'Draft save eligibility'
        Assert-Equal $case[2] ([DG_TonKhoBTP_v02.Helper.Validator]::TTCaLamViec($model) -eq 0) 'Legacy official validation'
    }
}

Test-Case 'Time input is disabled until a shift is selected and after reset' {
    $control = [Activator]::CreateInstance($controlType)
    try {
        $time = Input-Control $control 'dtGioBD'
        Assert-Equal $false $time.Enabled 'Initial time state'
        (Input-Control $control 'caBD').SelectedItem = '1'
        Assert-Equal $true $time.Enabled 'Selected shift enables time'
        $control.ClearInputs()
        Assert-Equal $false $time.Enabled 'Reset disables time'
        Assert-Equal -1 (Input-Control $control 'caBD').SelectedIndex 'Reset clears shift'
    }
    finally { $control.Dispose() }
}

foreach ($case in @(@('1', '23:00:00', '06:00:00'), @('2', '12:00:00', '14:00:00'), @('3', '12:00:00', '22:00:00'))) {
    Test-Case "Selecting shift $($case[0]) replaces previous time with current time" {
        $control = [Activator]::CreateInstance($controlType)
        try {
            Set-Time $control $case[1]
            Select-ShiftNow $control $case[0]
        }
        finally { $control.Dispose() }
    }
}

Test-Case 'Changing shifts refreshes even a time that is valid in both shifts' {
    $control = [Activator]::CreateInstance($controlType)
    try {
        (Input-Control $control 'caBD').SelectedItem = '1'
        Set-Time $control '16:00:00'
        Select-ShiftNow $control '2'
    }
    finally { $control.Dispose() }
}

Test-Case 'Invalid input is retained and marked on leave, then clears after correction' {
    $control = [Activator]::CreateInstance($controlType)
    try {
        (Input-Control $control 'caBD').SelectedItem = '1'
        Set-Time $control '20:00:00'
        Leave-Time $control
        Assert-Equal ([TimeSpan]::FromHours(20)) (Input-Control $control 'dtGioBD').Value.TimeOfDay 'Invalid input must not be clamped'
        $provider = Input-Control $control '_loiGioBatDau'
        Assert-Equal $false ([string]::IsNullOrWhiteSpace($provider.GetError((Input-Control $control 'dtGioBD')))) 'Inline validation error'
        Set-Time $control '18:00:00'
        Assert-Equal '' ($provider.GetError((Input-Control $control 'dtGioBD'))) 'Correcting input clears error'
    }
    finally { $control.Dispose() }
}

Test-Case 'Invalid hours prevent leaving the control until corrected' {
    $control = [Activator]::CreateInstance($controlType)
    $timer = [ShiftHoursAlertCloser]::Start()
    try {
        (Input-Control $control 'caBD').SelectedItem = '1'
        Set-Time $control '20:00:00'
        Assert-Equal $false ($control.ValidateChildren()) 'Invalid hours must cancel validation'
        Assert-Equal ([TimeSpan]::FromHours(20)) ($control.GetThongTinCaLamViec().GioBatDau) 'Invalid input stays available for correction'
        Set-Time $control '18:00:00'
        Assert-Equal $true ($control.ValidateChildren()) 'Correct hours must allow navigation'
    }
    finally { $timer.Stop(); $timer.Dispose(); $control.Dispose() }
}

Test-Case 'Editing HH:mm removes hidden seconds so the displayed 18:00 is valid' {
    $control = [Activator]::CreateInstance($controlType)
    try {
        (Input-Control $control 'caBD').SelectedItem = '1'
        Set-Time $control '18:00:37'
        Leave-Time $control
        Assert-Equal ([TimeSpan]::FromHours(18)) ($control.GetThongTinCaLamViec().GioBatDau) 'Saved time matches displayed minute'
        $provider = Input-Control $control '_loiGioBatDau'
        Assert-Equal '' ($provider.GetError((Input-Control $control 'dtGioBD'))) 'Displayed endpoint is valid'
    }
    finally { $control.Dispose() }
}

Test-Case 'Selecting shift 1 refreshes displayed time with hidden seconds' {
    $control = [Activator]::CreateInstance($controlType)
    try {
        Set-Time $control '18:00:37'
        Select-ShiftNow $control '1'
    }
    finally { $control.Dispose() }
}

foreach ($case in @(@('00:00:00', -1), @('05:59:59', -1), @('06:00:00', 0), @('14:00:00', 0), @('23:59:59', 0))) {
    Test-Case "Shift 2 at $($case[0]) has date offset $($case[1])" {
        $control = [Activator]::CreateInstance($controlType)
        try {
            (Input-Control $control 'caBD').SelectedItem = '2'
            Set-Time $control $case[0]
            Assert-Equal ($today.AddDays($case[1])) ($control.GetThongTinCaLamViec().NgayBatDau) 'Shift 2 start date'
        }
        finally { $control.Dispose() }
    }
}

foreach ($mode in @(2, 3)) {
    Test-Case "Loaded mode $mode with no time accepts an explicit time selection" {
        $control = [Activator]::CreateInstance($controlType)
        try {
            $table = Saved-Report '1' '16:00:00'
            $table.Rows[0]['GioBatDau'] = [DBNull]::Value
            $control.LoadData($table, $mode)
            $time = Input-Control $control 'dtGioBD'
            Assert-Equal $true $time.ShowCheckBox 'Unset time exposes its selection checkbox'
            Assert-Equal $false $time.Checked 'Missing time starts unselected'
            $time.Checked = $true
            Set-Time $control '17:00:00'
            $model = $control.GetThongTinCaLamViec()
            Assert-Equal ([TimeSpan]::FromHours(17)) $model.GioBatDau 'Explicitly selected time'
            Assert-Equal ([DateTime]::new(2001, 2, 3)) $model.NgayBatDau 'Saved date remains unchanged'
            $model.NgayKetThuc = $today
            $model.May = 'Test machine'
            $model.NguoiLam = 'Test operator'
            $model.NguoiKetThuc = 'Test operator'
            Assert-Equal 0 ([DG_TonKhoBTP_v02.Helper.Validator]::LayDanhSachLoiTTCaLamViec($model).Count) 'Official validation accepts correction'
            Assert-Equal 0 ([DG_TonKhoBTP_v02.Helper.LuuTamValidator]::LayDanhSachLoiCaLamViec($model).Count) 'Draft validation accepts correction'
        }
        finally { $control.Dispose() }
    }
    Test-Case "Loaded mode $mode preserves invalid source time/date but rejects it on save" {
        $control = [Activator]::CreateInstance($controlType)
        try {
            $control.LoadData((Saved-Report '1' '23:00:00'), $mode)
            Assert-Equal ([TimeSpan]::FromHours(23)) (Input-Control $control 'dtGioBD').Value.TimeOfDay 'Source time'
            Assert-Equal $true (Input-Control $control 'dtGioBD').Enabled 'Loaded shift enables editing time'
            $model = $control.GetThongTinCaLamViec()
            Assert-Equal ([DateTime]::new(2001, 2, 3)) $model.NgayBatDau 'Source date'
            $model.NgayKetThuc = $today
            $model.May = 'Test machine'
            $model.NguoiLam = 'Test operator'
            $model.NguoiKetThuc = 'Test operator'
            Assert-Equal 1 ([DG_TonKhoBTP_v02.Helper.Validator]::LayDanhSachLoiTTCaLamViec($model).Count) 'Official validation rejects only the invalid saved time'
            Assert-Equal 1 ([DG_TonKhoBTP_v02.Helper.LuuTamValidator]::LayDanhSachLoiCaLamViec($model).Count) 'Draft validation rejects only the invalid saved time'
            Leave-Time $control
            $provider = Input-Control $control '_loiGioBatDau'
            Assert-Equal $false ([string]::IsNullOrWhiteSpace($provider.GetError((Input-Control $control 'dtGioBD')))) 'Loaded invalid time is marked on interaction'
            Select-ShiftNow $control '2'
            Set-Time $control '01:00:00'
            Assert-Equal ($today.AddDays(-1)) ($control.GetThongTinCaLamViec().NgayBatDau) 'Date follows selected time after shift change'
        }
        finally { $control.Dispose() }
    }
    foreach ($missing in @('null', 'missing', 'unparseable')) {
        Test-Case "Loaded mode $mode with $missing time cannot inherit the previous report time" {
            $control = [Activator]::CreateInstance($controlType)
            try {
                Set-Time $control '16:00:00'
                $table = Saved-Report '1' '16:00:00'
                switch ($missing) {
                    'null' { $table.Rows[0]['GioBatDau'] = [DBNull]::Value }
                    'missing' { $table.Columns.Remove('GioBatDau') }
                    'unparseable' { $table.Rows[0]['GioBatDau'] = 'not a time' }
                }
                $control.LoadData($table, $mode)
                $model = $control.GetThongTinCaLamViec()
                Assert-Equal $null $model.GioBatDau 'Missing source time stays unset'
                $model.NgayKetThuc = $today
                $model.May = 'Test machine'
                $model.NguoiLam = 'Test operator'
                $model.NguoiKetThuc = 'Test operator'
                Assert-Equal 1 ([DG_TonKhoBTP_v02.Helper.Validator]::LayDanhSachLoiTTCaLamViec($model).Count) 'Official save requires a time'
                Assert-Equal 1 ([DG_TonKhoBTP_v02.Helper.LuuTamValidator]::LayDanhSachLoiCaLamViec($model).Count) 'Draft save requires a time'
                Leave-Time $control
                $provider = Input-Control $control '_loiGioBatDau'
                Assert-Equal $false ([string]::IsNullOrWhiteSpace($provider.GetError((Input-Control $control 'dtGioBD')))) 'Unset time is marked on interaction'
                Select-ShiftNow $control '2'
            }
            finally { $control.Dispose() }
        }
    }
}

Write-Output "Results: $script:passed passed, $script:failed failed"
if ($script:failed -gt 0) { exit 1 }
