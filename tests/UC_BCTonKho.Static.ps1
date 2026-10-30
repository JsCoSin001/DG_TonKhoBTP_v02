param([string]$WorkspaceRoot=(Split-Path $PSScriptRoot -Parent))
$ErrorActionPreference='Stop'
$project=Join-Path $WorkspaceRoot 'DG_TonKhoBTP_v02'
$xml=[xml](Get-Content (Join-Path $project 'DG_TonKhoBTP_v02.csproj') -Raw)
$items=@($xml.Project.ItemGroup.Compile.Include)
$db=Get-Content (Join-Path $project 'Database/Kho/TonKho/BC_TonKho_DB.cs') -Raw -Encoding UTF8
$model=Get-Content (Join-Path $project 'Models/Kho/TonKho/BCTonKho_Model.cs') -Raw -Encoding UTF8
$ui=Get-Content (Join-Path $project 'UI/NghiepVuKhac/Kho/UC_BCTonKho.cs') -Encoding UTF8
$sql=[regex]::Matches($db,'private const string (\w+Sql) = @"([\s\S]*?)";')
$result=[ordered]@{
    RegisteredModel=($items -contains 'Models\Kho\TonKho\BCTonKho_Model.cs')
    RegisteredDatabase=($items -contains 'Database\Kho\TonKho\BC_TonKho_DB.cs')
    QueryCount=$sql.Count
    EveryQuerySelect=(@($sql|Where-Object {$_.Groups[2].Value.Trim() -notmatch '^SELECT\b'}).Count -eq 0)
    NoWriteOrSchemaStatements=(@($sql|Where-Object {$_.Groups[2].Value -match '\b(INSERT|UPDATE|DELETE|ALTER|CREATE|DROP|REPLACE)\b'}).Count -eq 0)
    UsesSharedConnection=($db.Contains('DB_Base.OpenConnection()') -and -not $db.Contains('new SQLiteConnection'))
    ModelHasNoSQLite=($model -notmatch 'SQLite|SELECT|OpenConnection')
    NewUiFlowHasNoSql=(($ui[50..210] -join "`n") -notmatch '\b(SELECT|INSERT|UPDATE|DELETE)\b')
    Parameterized=($db.Contains('Parameters.AddWithValue') -and $db.Contains('LikePattern(keyword.Trim())'))
    TransactionForBothApis=([regex]::Matches($db,'conn.BeginTransaction\(true\)').Count -eq 2)
}
$pass=$result.RegisteredModel -and $result.RegisteredDatabase -and $result.QueryCount -eq 8 -and $result.EveryQuerySelect -and $result.NoWriteOrSchemaStatements -and $result.UsesSharedConnection -and $result.ModelHasNoSQLite -and $result.NewUiFlowHasNoSql -and $result.Parameterized -and $result.TransactionForBothApis
$result.Status=if($pass){'PASS'}else{'FAIL'}
$result|ConvertTo-Json|Tee-Object -FilePath (Join-Path $WorkspaceRoot 'docs/verification/uc-bctonkho-evidence/static.json')
if(-not $pass){exit 1}
