. "$PSScriptRoot/ExecutionEdit.TestSupport.ps1"
$path=$null
try {
    $path=New-ExecutionEditFixture
    $null=Add-EETestCut $path
    $query=New-EEObject 'ExecutionDetailQuery' @([long]1,[long]40,$null,[long]70)
    $snapshot=Invoke-EE 'Load' @($query)
    Assert-EE ($snapshot.Rows.Count -eq 2) 'Partial query returns exactly the two planned segments'
    Assert-EE ($snapshot.Rows[0].Done -and $snapshot.Rows[0].ReelId -eq 70) 'Executed cut links its physical reel'
    Assert-EE ($snapshot.Rows[0].AfterEnd -eq 650) 'Historical after marker is the cut snapshot'
    Assert-EE ($snapshot.Reels[[long]70].End -eq 650) 'Current reel is loaded separately'
    Assert-EE (@($snapshot.Users | Where-Object Username -eq 'inactive').Count -eq 1) 'Inactive user remains selectable'
    Assert-EE (-not $snapshot.Rows[1].Done) 'Pending row stays pending'
    $null=Add-EETestCut $path 61 '22222222-2222-4222-8222-222222222222'
    $snapshot=Invoke-EE 'Load' @($query)
    Assert-EE ($snapshot.Rows[0].AfterEnd -eq 650 -and $snapshot.Reels[[long]70].End -eq 550) 'Old history and current reel must not be confused'
    $full=Invoke-EE 'Load' @((New-EEObject 'ExecutionDetailQuery' @([long]1,[long]40,[long]80,$null)))
    Assert-EE (@($full.Rows | Where-Object Done).Count -eq 0) 'Specification query excludes cuts from another reel group'
    Write-Output 'PASS: detail scope, historical/current markers, pending rows and inactive performers'
} finally { Close-ExecutionEditFixture $path }
