param([string]$TestAssemblyPath)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms
if(-not $TestAssemblyPath){$TestAssemblyPath=Join-Path $PSScriptRoot '../DG_TonKhoBTP_v02/bin/Debug/DG_TonKhoBTP_v02.exe'}
$script:EEAssembly = [Reflection.Assembly]::LoadFrom($TestAssemblyPath)
$null = [Reflection.Assembly]::LoadFrom((Join-Path (Split-Path $TestAssemblyPath) 'System.Data.SQLite.dll'))
$script:EEOriginalConnection = [DG_TonKhoBTP_v02.Database.DB_Base]::_connStr
function Assert-EE($condition, [string]$message) { if (-not $condition) { throw "ASSERT: $message" } }
function Invoke-EESql([string]$sql) {
    $conn = [DG_TonKhoBTP_v02.Database.DB_Base]::OpenConnection()
    try { $cmd = $conn.CreateCommand(); $cmd.CommandText=$sql; $null=$cmd.ExecuteNonQuery(); $cmd.Dispose() } finally { $conn.Dispose() }
}
function Get-EEScalar([string]$sql) {
    $conn = [DG_TonKhoBTP_v02.Database.DB_Base]::OpenConnection()
    try { $cmd=$conn.CreateCommand(); $cmd.CommandText=$sql; $value=$cmd.ExecuteScalar(); $cmd.Dispose(); return $value } finally { $conn.Dispose() }
}
function New-ExecutionEditFixture([switch]$WithoutMigration) {
    $path=Join-Path ([IO.Path]::GetTempPath()) ('ExecutionEdit.'+[Guid]::NewGuid().ToString('N')+'.db')
    $extensionDir=(Join-Path (Split-Path (Get-Command php -ErrorAction Stop).Source) 'ext').Replace('\','/')
    $settings=@(('extension_dir="'+$extensionDir+'"'),'memory_limit=512M','date.timezone=Asia/Ho_Chi_Minh')
    foreach($extension in @('openssl','curl','fileinfo','mbstring','pdo_sqlite','sqlite3','zip')) {$settings+=('extension='+$extension)}
    [IO.File]::WriteAllText(($path+'.php.ini'),($settings -join [Environment]::NewLine),(New-Object Text.UTF8Encoding($false)))
    [DG_TonKhoBTP_v02.Database.DB_Base]::SetDatabasePath($path)
    Invoke-EESql ([IO.File]::ReadAllText((Join-Path $PSScriptRoot 'fixtures/execution-edit/base-schema.sql')))
    Invoke-EESql ([IO.File]::ReadAllText((Join-Path $PSScriptRoot 'fixtures/execution-edit/seed.sql')))
    if(-not $WithoutMigration) { Apply-ExecutionEditMigration $path }
    $login=New-Object DG_TonKhoBTP_v02.Models.LoginResult
    $login.Success=$true; $login.UserId=2; $login.UserName='editor'; $login.Name='Editor'
    [DG_TonKhoBTP_v02.Models.UserContext]::Set($login)
    return $path
}
function Apply-ExecutionEditMigration([string]$path) {
    $migration=Join-Path $PSScriptRoot '../database/Migration_B2B3_ExecutionEdit_20261026.sql'
    Assert-EE (Test-Path $migration) 'Shared execution edit schema is missing'
    Invoke-EESql ([IO.File]::ReadAllText($migration))
}
function Close-ExecutionEditFixture([string]$path) {
    [DG_TonKhoBTP_v02.Database.DB_Base]::_connStr=$script:EEOriginalConnection
    [System.Data.SQLite.SQLiteConnection]::ClearAllPools()
    if($path -and (Split-Path $path -Leaf) -like 'ExecutionEdit.*.db') {
        Remove-Item -LiteralPath $path -ErrorAction SilentlyContinue
        Remove-Item -LiteralPath ($path+'.php.ini') -ErrorAction SilentlyContinue
    }
}
function New-EEObject([string]$name, [object[]]$arguments=@()) {
    $type=$script:EEAssembly.GetType('DG_TonKhoBTP_v02.Models.Kho.XuatKho.'+$name,$true)
    return [Activator]::CreateInstance($type,$arguments)
}
function New-EEForm($query,[string]$productName) {
    $formType=$script:EEAssembly.GetType('DG_TonKhoBTP_v02.UI.NghiepVuKhac.Kho.XuatKho.Frm_ChiTietThucHienKeHoach',$true)
    $constructor=$formType.GetConstructor([Reflection.BindingFlags]'Instance,Public,NonPublic',$null,[type[]]@($query.GetType(),[string]),$null)
    return $constructor.Invoke([object[]]@($query,$productName))
}
function Invoke-EE([string]$method, [object[]]$arguments=@()) {
    $type=$script:EEAssembly.GetType('DG_TonKhoBTP_v02.Database.Kho.XuatKho.ExecutionEdit_DB',$true)
    try { return ,$type.GetMethod($method,[Reflection.BindingFlags]'Static,Public').Invoke($null,$arguments) }
    catch { if($_.Exception.InnerException) { throw $_.Exception.InnerException }; throw }
}
function Invoke-EEB3([string]$path,[string]$action,[hashtable]$payload) {
    $jsonPath=$path+'.json'
    try {
        [IO.File]::WriteAllText($jsonPath,($payload | ConvertTo-Json -Depth 15),(New-Object Text.UTF8Encoding($false)))
        $ini=$path+'.php.ini'
        $output=& php -c $ini (Join-Path $PSScriptRoot 'fixtures/execution-edit/b3-bridge.php') --db $path --action $action --payload $jsonPath
        if($LASTEXITCODE -ne 0) { throw "PHP bridge failed: $output" }
        return ($output | ConvertFrom-Json)
    } finally { Remove-Item -LiteralPath $jsonPath -ErrorAction SilentlyContinue }
}
function Add-EETestCut([string]$path,[int]$detail=60,[string]$token='11111111-1111-4111-8111-111111111111') {
    $r=Invoke-EEB3 $path 'cut' @{raw_qr='LE-001';ke_hoach_id=1;ke_hoach_hang_id=40;group_id=50;detail_id=$detail;operation_token=$token}
    Assert-EE $r.ok "B3 cut fixture: $($r.message)"
    return $r
}
function New-EERequest($snapshot) {
    $request=New-EEObject 'ExecutionEditRequest'
    $request.Query=$snapshot.Query
    foreach($key in $snapshot.Versions.Keys) { $request.ExpectedVersions.Add($key,$snapshot.Versions[$key]) }
    return $request
}
function Add-EEUndo($request,[string]$token) {
    $edit=New-EEObject 'ExecutionRowEdit'; $edit.OperationToken=$token; $edit.Undo=$true; $edit.Reason='MISTAKE'
    $request.RowEdits.Add($edit)
}
