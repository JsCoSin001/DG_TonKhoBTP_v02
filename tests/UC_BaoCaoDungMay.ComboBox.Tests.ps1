param(
    [string]$AssemblyPath = (Join-Path $PSScriptRoot '..\DG_TonKhoBTP_v02\bin\Debug\DG_TonKhoBTP_v02.exe')
)

# Run after building the application:
# powershell -NoProfile -ExecutionPolicy Bypass -STA -File tests\UC_BaoCaoDungMay.ComboBox.Tests.ps1
# Instantiates the real control without showing it or firing Load (no database access).
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms
$assembly = [Reflection.Assembly]::LoadFrom((Resolve-Path $AssemblyPath).Path)
$controlType = $assembly.GetType('DG_TonKhoBTP_v02.UI.NghiepVuKhac.SanXuat.UC_BaoCaoDungMay', $true)
$modelType = $assembly.GetType('DG_TonKhoBTP_v02.Models.SanXuat.TenLoiDungMay_Model', $true)
$listType = [System.Collections.Generic.List`1].MakeGenericType($modelType)
$flags = [Reflection.BindingFlags]'Instance,NonPublic'

function Assert-Equal($expected, $actual, [string]$message) {
    if (-not [object]::Equals($expected, $actual)) {
        throw "$message (expected: '$expected', actual: '$actual')"
    }
}

function New-ErrorList([int[]]$ids, [string[]]$names) {
    $list = [Activator]::CreateInstance($listType)
    for ($i = 0; $i -lt $ids.Length; $i++) {
        $item = [Activator]::CreateInstance($modelType)
        $modelType.GetProperty('Id').SetValue($item, $ids[$i], $null)
        $modelType.GetProperty('TenLoi').SetValue($item, $names[$i], $null)
        $list.Add($item)
    }
    return ,$list
}

$control = [Activator]::CreateInstance($controlType)
try {
    $grid = $controlType.GetField('grvDsLoiDungMay', $flags).GetValue($control)
    $grid.BindingContext = New-Object System.Windows.Forms.BindingContext
    $load = $controlType.GetMethod('NapNguonLoaiLoi', $flags)
    $bind = $controlType.GetMethod('GanNguonComboTenLoi', $flags)
    $column = $grid.Columns['colTenLoi']

    # Load/reset binds an empty catalog before the user chooses a production stage.
    $empty = New-ErrorList @() @()
    $null = $load.Invoke($control, [object[]]@(,$empty))
    $catalog = New-ErrorList @(17, 29) @('Mechanical stop', 'Electrical stop')
    $null = $load.Invoke($control, [object[]]@(,$catalog))
    $index = $grid.Rows.Add()
    $cell = $grid.Rows[$index].Cells['colTenLoi']

    # The dropdown must convert the selected label to the integer ID stored/saved.
    $selectedId = $cell.ParseFormattedValue('Electrical stop', $cell.InheritedStyle, $null, $null)
    Assert-Equal 29 $selectedId 'Selecting an error must produce its integer ID'
    $cell.Value = $selectedId
    Assert-Equal 'Electrical stop' $cell.FormattedValue 'Selected ID must display its label'
    Write-Output 'PASS: selecting an error after catalog reset preserves its ID and label'

    $grid.Rows[$index].Cells['colThoiGianDung'].Value = 12
    $grid.Rows[$index].Cells['colGhiChu'].Value = 'Regression test'
    $readInput = $controlType.GetMethod('LayDanhSachNhap', $flags)
    $inputRows = $readInput.Invoke($control, $null)
    Assert-Equal 1 $inputRows.Count 'Selected row must be available for saving'
    Assert-Equal 29 $inputRows[0].TenLoiDungMayId 'Saving must use the selected error ID'
    Assert-Equal 12 $inputRows[0].ThoiGianDung 'Saving must preserve downtime'
    Write-Output 'PASS: selected error is collected with its ID and downtime for saving'

    # Existing reports also rebind while retaining rows and historical error IDs.
    $null = $bind.Invoke($control, $null)
    $null = $bind.Invoke($control, $null)
    Assert-Equal 29 $cell.Value 'Rebinding must preserve the selected ID'
    Assert-Equal 'Electrical stop' $cell.FormattedValue 'Rebinding must preserve the label'
    Assert-Equal 17 ($cell.ParseFormattedValue('Mechanical stop', $cell.InheritedStyle, $null, $null)) 'Another selection must remain valid'
    Write-Output 'PASS: repeated report rebinding preserves selection and conversion'

    $grid.Rows.Clear()
    $otherCatalog = New-ErrorList @(43) @('Material stop')
    $null = $load.Invoke($control, [object[]]@(,$otherCatalog))
    $index = $grid.Rows.Add()
    $cell = $grid.Rows[$index].Cells['colTenLoi']
    Assert-Equal 43 ($cell.ParseFormattedValue('Material stop', $cell.InheritedStyle, $null, $null)) 'Changing stage must use the new catalog'
    Assert-Equal 2 $column.Items.Count 'Previous stage errors must be removed (including placeholder: 2 items)'
    $placeholder = $column.DataSource[0]
    $placeholderName = $modelType.GetProperty('TenLoi').GetValue($placeholder, $null)
    # The placeholder matches NullValue, so WinForms parses it as an unselected cell.
    Assert-Equal $null ($cell.ParseFormattedValue($placeholderName, $cell.InheritedStyle, $null, $null)) 'Placeholder must remain unselected'
    Write-Output 'PASS: changing stage replaces the catalog and keeps the placeholder unselected'
}
finally {
    $control.Dispose()
}
