param(
    [string]$AssemblyPath = (Join-Path $PSScriptRoot '..\DG_TonKhoBTP_v02\bin\Debug\DG_TonKhoBTP_v02.exe')
)

# Build first, then run with Windows PowerShell (no database access):
# powershell -NoProfile -ExecutionPolicy Bypass -STA -File tests\UC_TTCaLamViec.StartDate.Tests.ps1
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Data
$assembly = [Reflection.Assembly]::LoadFrom((Resolve-Path $AssemblyPath).Path)
$controlType = $assembly.GetType('DG_TonKhoBTP_v02.UI.UC_TTCaLamViec', $true)
$flags = [Reflection.BindingFlags]'Instance,NonPublic'
$today = [DateTime]::Today
$script:passed = 0
$script:failed = 0

function Input-Control($control, [string]$name) {
    return $controlType.GetField($name, $flags).GetValue($control)
}

function Assert-Equal($expected, $actual, [string]$message) {
    if ($expected -ne $actual) {
        throw "$message (expected: '$expected', actual: '$actual')"
    }
}

function Assert-Date($control, [DateTime]$expected) {
    Assert-Equal ($expected.ToString('dd/MM/yyyy')) (Input-Control $control 'tbNgayBD').Text 'Displayed start date'
    Assert-Equal $expected.Date ($control.GetThongTinCaLamViec().NgayBatDau) 'Collected start date'
}

function Set-StartTime($control, [string]$time) {
    # Its hidden calendar date must not be used as the business date.
    (Input-Control $control 'dtGioBD').Value = $today.AddDays(-10).Add([TimeSpan]::Parse($time))
}

function New-SavedReport($date = '2001-02-03') {
    $table = New-Object System.Data.DataTable
    foreach ($name in @('Ca', 'NgayBatDau', 'GioBatDau')) {
        $null = $table.Columns.Add($name, [string])
    }
    $row = $table.NewRow()
    $row['Ca'] = '3'
    $row['NgayBatDau'] = $date
    $row['GioBatDau'] = '01:00:00'
    $table.Rows.Add($row)
    return ,$table
}

function Test-Control([string]$name, [scriptblock]$check) {
    $control = [Activator]::CreateInstance($controlType)
    try {
        & $check $control
        $script:passed++
        Write-Output "PASS: $name"
    }
    catch {
        $script:failed++
        Write-Output "FAIL: $name - $($_.Exception.Message)"
    }
    finally { $control.Dispose() }
}

Test-Control 'New form has no shift, today as start date, and a disabled date input' {
    param($control)
    Assert-Equal -1 (Input-Control $control 'caBD').SelectedIndex 'Initial shift'
    Assert-Date $control $today
    Assert-Equal $false (Input-Control $control 'tbNgayBD').Enabled 'Start date must be disabled'
    Assert-Equal $false (Input-Control $control 'dtGioBD').Enabled 'Start time requires a shift'
}

foreach ($time in @('00:00:00', '05:59:59')) {
    Test-Control "Shift 3 at $time uses yesterday" {
        param($control)
        (Input-Control $control 'caBD').SelectedItem = '3'
        Set-StartTime $control $time
        Assert-Date $control ($today.AddDays(-1))
    }
}

foreach ($time in @('06:00:00', '22:00:00', '23:59:59')) {
    Test-Control "Shift 3 at $time uses today" {
        param($control)
        (Input-Control $control 'caBD').SelectedItem = '3'
        Set-StartTime $control $time
        Assert-Date $control $today
    }
}

Test-Control 'Changing shifts does not subtract days repeatedly' {
    param($control)
    $shift = Input-Control $control 'caBD'
    foreach ($case in @(
        @('1', '06:00:00', 0), @('3', '01:00:00', -1),
        @('2', '01:00:00', -1), @('3', '01:00:00', -1),
        @('1', '06:00:00', 0), @('3', '01:00:00', -1)
    )) {
        $shift.SelectedItem = $case[0]
        Set-StartTime $control $case[1]
        Assert-Date $control ($today.AddDays($case[2]))
    }
    $shift.SelectedIndex = -1
    Assert-Date $control $today
}

Test-Control 'Changing time updates the date across midnight and 06:00' {
    param($control)
    $shift = Input-Control $control 'caBD'
    $shift.SelectedItem = '3'
    Set-StartTime $control '23:00:00'
    Assert-Date $control $today
    Set-StartTime $control '00:00:00'
    Assert-Date $control ($today.AddDays(-1))
    Set-StartTime $control '06:00:00'
    Assert-Date $control $today
    Set-StartTime $control '05:59:59'
    Assert-Date $control ($today.AddDays(-1))
}

Test-Control 'Subscribers receive one notification per time change with the recalculated date' {
    param($control)
    (Input-Control $control 'caBD').SelectedItem = '3'
    $script:notifications = New-Object 'System.Collections.Generic.List[object]'
    $handler = [Action[DG_TonKhoBTP_v02.Core.ThongTinCaLamViec]] {
        param($model)
        $script:notifications.Add($model)
    }
    $control.add_Event_ThongTinCaLamViecChanged($handler)
    Set-StartTime $control '01:00:00'
    Assert-Equal 1 $script:notifications.Count 'Time change notification count'
    Assert-Equal ($today.AddDays(-1)) $script:notifications[0].NgayBatDau 'Notified start date'
    Set-StartTime $control '06:00:00'
    Assert-Equal 2 $script:notifications.Count 'Time change notification count'
    Assert-Equal $today $script:notifications[1].NgayBatDau 'Notified date at 06:00'
}

Test-Control 'Changing shift sends one notification containing current time and date' {
    param($control)
    Set-StartTime $control '01:00:00'
    $script:notifications = New-Object 'System.Collections.Generic.List[object]'
    $handler = [Action[DG_TonKhoBTP_v02.Core.ThongTinCaLamViec]] {
        param($model)
        $script:notifications.Add($model)
    }
    $control.add_Event_ThongTinCaLamViecChanged($handler)
    $before = [DateTime]::Now
    (Input-Control $control 'caBD').SelectedItem = '1'
    $after = [DateTime]::Now
    Assert-Equal 1 $script:notifications.Count 'Shift change notification count'
    Assert-Equal $today $script:notifications[0].NgayBatDau 'Shift 1 date'
    $actual = (Input-Control $control 'dtGioBD').Value
    if ($actual -lt $before.AddTicks(-($before.Ticks % [TimeSpan]::TicksPerMinute)) -or $actual -gt $after) {
        throw 'Shift change notification must contain current time'
    }
    Assert-Equal $actual.TimeOfDay $script:notifications[0].GioBatDau 'Notified time'
}

foreach ($time in @('00:00:00', '05:59:59', '06:00:00', '23:59:59')) {
    Test-Control "Shift 1 at $time always uses today" {
        param($control)
        (Input-Control $control 'caBD').SelectedItem = '1'
        Set-StartTime $control $time
        Assert-Date $control $today
    }
}

foreach ($mode in @(2, 3)) {
    Test-Control "Saved report/draft (mode $mode) refreshes date after user changes shift" {
        param($control)
        $control.LoadData((New-SavedReport), $mode)
        Assert-Equal '3' (Input-Control $control 'caBD').Text 'Loaded shift'
        Assert-Date $control ([DateTime]::new(2001, 2, 3))
        (Input-Control $control 'caBD').SelectedItem = '1'
        Assert-Date $control $today
        Set-StartTime $control '23:00:00'
        (Input-Control $control 'caBD').SelectedItem = '3'
        Set-StartTime $control '00:00:00'
        Assert-Date $control ($today.AddDays(-1))
    }
    Test-Control "Saved report/draft (mode $mode) preserves empty date until user changes shift" {
        param($control)
        $control.LoadData((New-SavedReport ([DBNull]::Value)), $mode)
        Set-StartTime $control '00:00:00'
        Assert-Equal '' (Input-Control $control 'tbNgayBD').Text 'Empty stored date must stay empty'
        Assert-Equal $null ($control.GetThongTinCaLamViec().NgayBatDau) 'Collected empty date'
        (Input-Control $control 'caBD').SelectedItem = '1'
        Assert-Date $control $today
    }
}

Test-Control 'Reset clears the shift and restores automatic dates after loading a draft' {
    param($control)
    $control.LoadData((New-SavedReport), 3)
    $control.ClearInputs()
    Assert-Equal -1 (Input-Control $control 'caBD').SelectedIndex 'Shift after reset'
    Assert-Date $control $today
    (Input-Control $control 'caBD').SelectedItem = '3'
    Set-StartTime $control '01:00:00'
    Assert-Date $control ($today.AddDays(-1))
}

foreach ($mode in @(0, 1)) {
    Test-Control "New/copy mode $mode uses fresh defaults instead of saved shift/date" {
        param($control)
        $control.LoadData((New-SavedReport), $mode)
        Assert-Equal -1 (Input-Control $control 'caBD').SelectedIndex 'Fresh shift'
        Assert-Date $control $today
    }
}

Write-Output "Results: $script:passed passed, $script:failed failed"
if ($script:failed -gt 0) { exit 1 }
