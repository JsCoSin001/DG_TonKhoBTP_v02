param(
    [string]$AssemblyPath = (Join-Path $PSScriptRoot '..\DG_TonKhoBTP_v02\bin\Debug\DG_TonKhoBTP_v02.exe')
)
# Windows PowerShell -STA; tests real controls/validators without database access.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Data
Add-Type -ReferencedAssemblies System.Windows.Forms -TypeDefinition @'
public static class EndingAlertCloser
{
    public static int Count;
    public static System.Windows.Forms.Timer Start()
    {
        var timer = new System.Windows.Forms.Timer { Interval = 1 };
        timer.Tick += (sender, args) =>
        {
            for (int i = System.Windows.Forms.Application.OpenForms.Count - 1; i >= 0; i--)
            {
                var form = System.Windows.Forms.Application.OpenForms[i];
                if (form.GetType().FullName == "DG_TonKhoBTP_v02.UI.FrmWaiting")
                { Count++; form.Close(); }
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

function Input-Control($control, [string]$name) { return $controlType.GetField($name, $flags).GetValue($control) }
function Assert-Equal($expected, $actual, [string]$message) {
    if ($expected -ne $actual) { throw "$message (expected '$expected', actual '$actual')" }
}
function Test-Case([string]$name, [scriptblock]$check) {
    $timer = [EndingAlertCloser]::Start()
    try { & $check; $script:passed++; Write-Output "PASS: $name" }
    catch { $script:failed++; Write-Output "FAIL: $name - $($_.Exception.Message)" }
    finally { $timer.Stop(); $timer.Dispose() }
}
function Set-Time($control, [string]$name, [string]$time) {
    (Input-Control $control $name).Value = $today.Add([TimeSpan]::Parse($time))
}
function Validate-Field($control, [string]$name) {
    $cancelArgs = [System.ComponentModel.CancelEventArgs]::new()
    $method = [System.Windows.Forms.Control].GetMethod('OnValidating', $flags)
    $null = $method.Invoke((Input-Control $control $name), [object[]]@($cancelArgs.PSObject.BaseObject))
    return $cancelArgs.Cancel
}
function Saved-Report([object]$endTime = '01:00:00') {
    $table = New-Object System.Data.DataTable
    foreach ($name in @('Ca', 'NgayBatDau', 'GioBatDau', 'CaKetThuc', 'NgayKetThuc', 'GioKetThuc')) {
        $null = $table.Columns.Add($name, [string])
    }
    $row = $table.NewRow()
    $row['Ca'] = '3'; $row['NgayBatDau'] = '2001-02-03'; $row['GioBatDau'] = '23:00:00'
    $row['CaKetThuc'] = '3'; $row['NgayKetThuc'] = '2001-02-03'; $row['GioKetThuc'] = $endTime
    $table.Rows.Add($row)
    return ,$table
}

# Expected outcomes are literal business examples, independent of the policy implementation.
$cases = @(
    @('date before start despite later shift', -1, '1', '06:00:00', '3', '23:00:00', $false),
    @('later date ignores lower shift and earlier hour', 1, '3', '23:00:00', '1', '06:00:00', $true),
    @('later date still enforces shift hours', 1, '3', '23:00:00', '1', '20:00:00', $false),
    @('same date higher shift ignores clock order', 0, '2', '23:00:00', '3', '22:00:00', $true),
    @('same date lower shift is invalid', 0, '3', '23:00:00', '2', '23:00:00', $false),
    @('shift 3 23 to midnight', 0, '3', '23:00:00', '3', '00:00:00', $true),
    @('shift 3 midnight to 01', 0, '3', '00:00:00', '3', '01:00:00', $true),
    @('shift 3 01 back to midnight', 0, '3', '01:00:00', '3', '00:00:00', $false),
    @('shift 3 01 back to evening', 0, '3', '01:00:00', '3', '23:00:00', $false),
    @('shift 3 evening to exact 06', 0, '3', '22:00:00', '3', '06:00:00', $true),
    @('shift 3 exact 06 back to evening', 0, '3', '06:00:00', '3', '22:00:00', $false),
    @('shift 2 crosses midnight', 0, '2', '23:00:00', '2', '00:00:00', $true),
    @('shift 2 morning back to afternoon', 0, '2', '05:00:00', '2', '14:00:00', $false),
    @('shift 1 decreasing hour', 0, '1', '10:00:00', '1', '09:00:00', $false),
    @('identical tuple allowed', 0, '1', '06:00:00', '1', '06:00:00', $true),
    @('invalid end shift', 1, '1', '06:00:00', '4', '15:00:00', $false),
    @('missing end shift skips unavailable comparisons', 0, '3', '23:00:00', $null, '01:00:00', $true),
    @('missing end hour skips hour comparison', 0, '3', '23:00:00', '3', $null, $true),
    @('date before start is invalid even without end shift or hour', -1, '1', '06:00:00', $null, $null, $false),
    @('lower end shift invalid even without end hour', 0, '3', '23:00:00', '2', $null, $false),
    @('shift 2 invalid 06:01 even on later date', 1, '1', '06:00:00', '2', '06:01:00', $false),
    @('shift 3 invalid before 22', 1, '1', '06:00:00', '3', '21:59:00', $false)
)
foreach ($case in $cases) {
    Test-Case $case[0] {
        $model = [Activator]::CreateInstance($modelType)
        $model.NgayBatDau = $today
        $model.NgayKetThuc = $today.AddDays($case[1])
        $model.Ca = $case[2]; $model.GioBatDau = [TimeSpan]::Parse($case[3]); $model.CaKetThuc = $case[4]
        if ($null -ne $case[5]) { $model.GioKetThuc = [TimeSpan]::Parse($case[5]) }
        $model.May = 'Test machine'; $model.NguoiLam = 'Test operator'
        Assert-Equal $case[6] ([DG_TonKhoBTP_v02.Helper.Validator]::LayDanhSachLoiTTCaLamViec($model).Count -eq 0) 'Official eligibility'
        Assert-Equal $case[6] ([DG_TonKhoBTP_v02.Helper.LuuTamValidator]::LayDanhSachLoiCaLamViec($model).Count -eq 0) 'Draft eligibility'
        Assert-Equal $case[6] ([DG_TonKhoBTP_v02.Helper.Validator]::TTCaLamViec($model) -eq 0) 'Legacy official eligibility'
    }
}

Test-Case 'Existing save requirements remain when all ending fields are missing' {
    $model = [Activator]::CreateInstance($modelType)
    $model.NgayBatDau = $today; $model.Ca = '1'; $model.GioBatDau = [TimeSpan]::FromHours(6)
    $model.May = 'Test machine'; $model.NguoiLam = 'Test operator'
    Assert-Equal 1 ([DG_TonKhoBTP_v02.Helper.Validator]::LayDanhSachLoiTTCaLamViec($model).Count) 'Official still requires end date'
    Assert-Equal 0 ([DG_TonKhoBTP_v02.Helper.LuuTamValidator]::LayDanhSachLoiCaLamViec($model).Count) 'Draft does not require ending data'
}

Test-Case 'Malformed nonempty ending dates cannot become optional null in save snapshots' {
    $control = [Activator]::CreateInstance($controlType)
    $hostForm = New-Object System.Windows.Forms.Form
    try {
        $hostForm.Controls.Add($control)
        (Input-Control $control 'tbNgayKT').Text = '31/02/2026'
        foreach ($mode in @('official', 'draft')) {
            $rejected = $false
            try {
                if ($mode -eq 'draft') { $null = [DG_TonKhoBTP_v02.Core.FormSnapshotBuilder]::CaptureDraft($hostForm) }
                else { $null = [DG_TonKhoBTP_v02.Core.FormSnapshotBuilder]::Capture($hostForm) }
            }
            catch { $rejected = $true }
            Assert-Equal $true $rejected "$mode capture must reject malformed date text"
        }
        (Input-Control $control 'tbNgayKT').Clear()
        $null = [DG_TonKhoBTP_v02.Core.FormSnapshotBuilder]::CaptureDraft($hostForm)
    }
    finally { $hostForm.Dispose() }
}

Test-Case 'End time is disabled before choosing end shift and after reset' {
    $control = [Activator]::CreateInstance($controlType)
    try {
        Assert-Equal $false (Input-Control $control 'dtGioKT').Enabled 'Initial ending time'
        Set-Time $control 'dtGioKT' '18:00:00'
        (Input-Control $control 'cbxCaKT').SelectedItem = '1'
        Assert-Equal $true (Input-Control $control 'dtGioKT').Enabled 'Chosen end shift'
        $control.ClearInputs()
        Assert-Equal $false (Input-Control $control 'dtGioKT').Enabled 'Reset ending time'
    }
    finally { $control.Dispose() }
}

Test-Case 'Selecting an end shift retains invalid time, alerts and requires correction' {
    $control = [Activator]::CreateInstance($controlType)
    try {
        Set-Time $control 'dtGioKT' '20:00:00'
        $before = [EndingAlertCloser]::Count
        (Input-Control $control 'cbxCaKT').SelectedItem = '1'
        Assert-Equal ([TimeSpan]::FromHours(20)) ($control.GetThongTinCaLamViec().GioKetThuc) 'Invalid time is retained'
        Assert-Equal $true ([EndingAlertCloser]::Count -gt $before) 'Selecting invalid ending shift raises an alert'
        Assert-Equal $true (Validate-Field $control 'dtGioKT') 'Invalid end time prevents focus change'
        Set-Time $control 'dtGioKT' '18:00:00'
        Assert-Equal $false (Validate-Field $control 'dtGioKT') 'Correct time allows focus change'
    }
    finally { $control.Dispose() }
}

Test-Case 'Native focus returns to invalid ending time with only one alert' {
    $control = [Activator]::CreateInstance($controlType)
    $hostForm = New-Object System.Windows.Forms.Form
    try {
        $hostForm.ShowInTaskbar = $false
        $hostForm.Opacity = 0
        $control.Dock = [System.Windows.Forms.DockStyle]::Fill
        $hostForm.Controls.Add($control)
        $hostForm.Show()
        $hostForm.Activate()
        (Input-Control $control 'caBD').SelectedItem = '1'
        Set-Time $control 'dtGioBD' '06:00:00'
        Set-Time $control 'dtGioKT' '20:00:00'
        $shift = Input-Control $control 'cbxCaKT'
        $null = $shift.Focus()
        [System.Windows.Forms.Application]::DoEvents()
        $before = [EndingAlertCloser]::Count
        $shift.SelectedItem = '1'
        [System.Windows.Forms.Application]::DoEvents()
        Assert-Equal 1 ([EndingAlertCloser]::Count - $before) 'Focus transfer must not repeat the same alert'
        Assert-Equal $true (Input-Control $control 'dtGioKT').Focused 'Focus returns to the wrong ending time'
        Set-Time $control 'dtGioKT' '18:00:00'
        $next = Input-Control $control 'tbxNguoiBD'
        $null = $next.Focus()
        [System.Windows.Forms.Application]::DoEvents()
        Assert-Equal $true $next.Focused 'Correcting ending time releases focus'
    }
    finally {
        $control.AutoValidate = [System.Windows.Forms.AutoValidate]::Disable
        $hostForm.Dispose()
    }
}

Test-Case 'End date defaults follow end shift/time and manual changes survive until reset' {
    $control = [Activator]::CreateInstance($controlType)
    try {
        (Input-Control $control 'caBD').SelectedItem = '3'
        Set-Time $control 'dtGioBD' '01:00:00'
        Set-Time $control 'dtGioKT' '23:00:00'
        (Input-Control $control 'cbxCaKT').SelectedItem = '3'
        Assert-Equal $today ($control.GetThongTinCaLamViec().NgayKetThuc) 'Evening end date'
        Set-Time $control 'dtGioKT' '01:00:00'
        Assert-Equal ($today.AddDays(-1)) ($control.GetThongTinCaLamViec().NgayKetThuc) 'Morning end date'
        Set-Time $control 'dtGioKT' '06:00:00'
        Assert-Equal $today ($control.GetThongTinCaLamViec().NgayKetThuc) 'Exact 06 uses today'
        (Input-Control $control 'tbNgayKT').Text = $today.AddDays(2).ToString('dd/MM/yyyy')
        (Input-Control $control 'cbxCaKT').SelectedItem = '2'
        Set-Time $control 'dtGioKT' '01:00:00'
        Assert-Equal ($today.AddDays(2)) ($control.GetThongTinCaLamViec().NgayKetThuc) 'Manual end date is preserved'
        $control.ClearInputs()
        (Input-Control $control 'caBD').SelectedItem = '3'
        Set-Time $control 'dtGioBD' '01:00:00'
        Set-Time $control 'dtGioKT' '01:00:00'
        (Input-Control $control 'cbxCaKT').SelectedItem = '2'
        Assert-Equal ($today.AddDays(-1)) ($control.GetThongTinCaLamViec().NgayKetThuc) 'Reset restores automatic end date'
    }
    finally { $control.Dispose() }
}

foreach ($mode in @(2, 3)) {
    Test-Case "Loaded mode $mode preserves ending values and orders overnight time" {
        $control = [Activator]::CreateInstance($controlType)
        try {
            $control.LoadData((Saved-Report), $mode)
            Assert-Equal ([DateTime]::new(2001, 2, 3)) ($control.GetThongTinCaLamViec().NgayKetThuc) 'Stored end date'
            Assert-Equal $false (Validate-Field $control 'dtGioKT') '23 to 01 is valid in same shift'
            Set-Time $control 'dtGioKT' '22:00:00'
            Assert-Equal $true (Validate-Field $control 'dtGioKT') '23 to 22 is invalid in same shift'
            Set-Time $control 'dtGioKT' '00:00:00'
            Assert-Equal $false (Validate-Field $control 'dtGioKT') '23 to midnight is valid'
            Assert-Equal ([DateTime]::new(2001, 2, 3)) ($control.GetThongTinCaLamViec().NgayKetThuc) 'End date stays stored after editing time'
            (Input-Control $control 'tbNgayKT').Text = '02/02/2001'
            Assert-Equal $true (Validate-Field $control 'tbNgayKT') 'Earlier ending date prevents focus change'
        }
        finally { $control.Dispose() }
    }
    Test-Case "Loaded mode $mode preserves a missing ending time" {
        $control = [Activator]::CreateInstance($controlType)
        try {
            Set-Time $control 'dtGioKT' '16:00:00'
            $control.LoadData((Saved-Report ([DBNull]::Value)), $mode)
            Assert-Equal $null ($control.GetThongTinCaLamViec().GioKetThuc) 'No inherited ending time'
        }
        finally { $control.Dispose() }
    }
}

Write-Output "Results: $script:passed passed, $script:failed failed"
if ($script:failed -gt 0) { exit 1 }
