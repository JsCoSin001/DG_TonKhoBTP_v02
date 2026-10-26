$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms
$script:EEAssembly = [Reflection.Assembly]::LoadFrom((Join-Path $PSScriptRoot '../DG_TonKhoBTP_v02/bin/Debug/DG_TonKhoBTP_v02.exe'))
$null = [Reflection.Assembly]::LoadFrom((Join-Path $PSScriptRoot '../DG_TonKhoBTP_v02/bin/Debug/System.Data.SQLite.dll'))
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
    if($path -and (Split-Path $path -Leaf) -like 'ExecutionEdit.*.db') { Remove-Item -LiteralPath $path -ErrorAction SilentlyContinue }
}
function New-EEObject([string]$name, [object[]]$arguments=@()) {
    $type=$script:EEAssembly.GetType('DG_TonKhoBTP_v02.Models.Kho.XuatKho.'+$name,$true)
    return [Activator]::CreateInstance($type,$arguments)
}
function Invoke-EE([string]$method, [object[]]$arguments=@()) {
    $type=$script:EEAssembly.GetType('DG_TonKhoBTP_v02.Database.Kho.XuatKho.ExecutionEdit_DB',$true)
    try { return $type.GetMethod($method,[Reflection.BindingFlags]'Static,Public,NonPublic').Invoke($null,$arguments) }
    catch { if($_.Exception.InnerException) { throw $_.Exception.InnerException }; throw }
}
