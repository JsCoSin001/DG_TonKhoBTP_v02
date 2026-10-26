. "$PSScriptRoot/ExecutionEdit.TestSupport.ps1"
$path=$null
try {
    $path=New-ExecutionEditFixture -WithoutMigration
    Assert-EE ((Get-EEScalar "SELECT COUNT(*) FROM sqlite_master WHERE name='B3ThaoTac'") -eq 0) 'Fixture starts without feature schema'
    Apply-ExecutionEditMigration $path
    Assert-EE ((Get-EEScalar "SELECT COUNT(*) FROM B3TinhNangDuLieu WHERE Ten='EXECUTION_EDIT' AND PhienBan=1 AND ChoPhepSua=0") -eq 1) 'Migration is gated off initially'
    $v=Get-EEScalar "SELECT GiaTri FROM B3PhienBanDuLieu WHERE Khoa='FACTORY'"
    Invoke-EESql 'UPDATE TonCuonLe SET SoCuoi=700 WHERE id=70; UPDATE TonCuonLe SET SoCuoi=800 WHERE id=70;'
    Assert-EE ((Get-EEScalar "SELECT GiaTri FROM B3PhienBanDuLieu WHERE Khoa='FACTORY'") -gt $v) 'Returning markers to prior values must advance revision'
    $v=Get-EEScalar "SELECT GiaTri FROM B3PhienBanDuLieu WHERE Khoa='FACTORY'"
    Invoke-EESql 'BEGIN IMMEDIATE; UPDATE TonCuonLe SET SoCuoi=600 WHERE id=70; ROLLBACK;'
    Assert-EE ((Get-EEScalar "SELECT GiaTri FROM B3PhienBanDuLieu WHERE Khoa='FACTORY'") -eq $v) 'Revision rolls back with data'
    Apply-ExecutionEditMigration $path
    Assert-EE ((Get-EEScalar "SELECT GiaTri FROM B3PhienBanDuLieu WHERE Khoa='FACTORY'") -eq $v) 'Reapplying schema cannot reset versions'
    Write-Output 'PASS: schema gate, monotonic revisions, rollback and reapply'
} finally { Close-ExecutionEditFixture $path }
