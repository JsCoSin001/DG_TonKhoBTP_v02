param([string]$WorkspaceRoot=(Split-Path $PSScriptRoot -Parent))
$ErrorActionPreference='Stop'
$bin=Join-Path $WorkspaceRoot 'DG_TonKhoBTP_v02/bin/Debug'
$evidence=Join-Path $WorkspaceRoot 'docs/verification/uc-bctonkho-evidence'
$compiler='C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\Roslyn\csc.exe'
& $compiler /nologo /target:exe /platform:x64 /langversion:8.0 "/out:$bin/UC_BCTonKho.Acceptance.exe" "/reference:$bin/DG_TonKhoBTP_v02.exe" "/reference:$bin/System.Data.SQLite.dll" /reference:System.Data.dll /reference:System.Core.dll /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll (Join-Path $PSScriptRoot 'UC_BCTonKho.Acceptance.cs')
if($LASTEXITCODE -ne 0){throw 'Acceptance harness compilation failed'}
Copy-Item -LiteralPath (Join-Path $bin 'DG_TonKhoBTP_v02.exe.config') -Destination (Join-Path $bin 'UC_BCTonKho.Acceptance.exe.config') -Force
& (Join-Path $bin 'UC_BCTonKho.Acceptance.exe') $WorkspaceRoot 2>&1 | Tee-Object -FilePath (Join-Path $evidence 'acceptance.log')
exit $LASTEXITCODE
