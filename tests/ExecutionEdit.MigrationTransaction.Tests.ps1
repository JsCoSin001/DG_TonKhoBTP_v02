. "$PSScriptRoot/ExecutionEdit.TestSupport.ps1"
$path=$null;$conn=$null;$tx=$null
$gui=Join-Path $PSScriptRoot '../database/Migration_B2B3_ExecutionEdit_20261026_DBBrowser.sql'
Assert-EE (Test-Path -LiteralPath $gui) 'GUI migration must support a transaction already opened by the SQL editor'
function Run-Sql($connection,$transaction,[string]$sql) {
    $cmd=$connection.CreateCommand();$cmd.CommandText=$sql
    if($transaction){$cmd.Transaction=$transaction}
    try{$null=$cmd.ExecuteNonQuery()}finally{$cmd.Dispose()}
}
function Count-NewTables($connection,$transaction) {
    $cmd=$connection.CreateCommand();$cmd.CommandText="SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name IN ('B3TinhNangDuLieu','B3ThaoTac','B3PhienBanDuLieu','B2DieuChinhLanLuu','B2DieuChinhSuKien','B2DieuChinhKetQua')"
    if($transaction){$cmd.Transaction=$transaction}
    try{return [int]$cmd.ExecuteScalar()}finally{$cmd.Dispose()}
}
try {
    $path=New-ExecutionEditFixture -WithoutMigration
    $conn=[DG_TonKhoBTP_v02.Database.DB_Base]::OpenConnection()
    $tx=$conn.BeginTransaction()
    $nested=$false
    try{Run-Sql $conn $tx ([IO.File]::ReadAllText((Join-Path $PSScriptRoot '../database/Migration_B2B3_ExecutionEdit_20261026.sql')))}
    catch{$nested=$_.Exception.Message.Contains('cannot start a transaction within a transaction')}
    Assert-EE $nested 'Reproduce original SQL inside a GUI-owned transaction'
    $tx.Rollback();$tx.Dispose();$tx=$null
    $sql=[IO.File]::ReadAllText($gui)
    $tx=$conn.BeginTransaction();Run-Sql $conn $tx $sql
    Assert-EE ((Count-NewTables $conn $tx) -eq 6) 'GUI variant creates all six tables inside the existing transaction'
    $tx.Rollback();$tx.Dispose();$tx=$null
    Assert-EE ((Count-NewTables $conn $null) -eq 0) 'Reverting GUI changes rolls back the complete migration'
    $tx=$conn.BeginTransaction();Run-Sql $conn $tx $sql;$tx.Commit();$tx.Dispose();$tx=$null
    Assert-EE ((Count-NewTables $conn $null) -eq 6) 'Writing GUI changes persists the complete migration'
    $tx=$conn.BeginTransaction();Run-Sql $conn $tx $sql;$tx.Commit();$tx.Dispose();$tx=$null
    Assert-EE ((Get-EEScalar "SELECT ChoPhepSua FROM B3TinhNangDuLieu WHERE Ten='EXECUTION_EDIT'") -eq 0) 'Reapply remains safe and leaves editing disabled'
    $conn.Dispose();$conn=$null;Close-ExecutionEditFixture $path
    $path=New-ExecutionEditFixture -WithoutMigration
    $conn=[DG_TonKhoBTP_v02.Database.DB_Base]::OpenConnection();Run-Sql $conn $null $sql
    Assert-EE ((Count-NewTables $conn $null) -eq 6) 'GUI variant also runs without an existing transaction'
    Write-Output 'PASS: original error reproduced; GUI savepoint migration, rollback, commit, reapply and standalone execution verified'
} finally {
    if($tx){$tx.Dispose()};if($conn){$conn.Dispose()};Close-ExecutionEditFixture $path
}
