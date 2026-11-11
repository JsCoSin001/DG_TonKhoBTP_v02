. "$PSScriptRoot/ExecutionEdit.TestSupport.ps1"
$path=$null
try {
    $path=New-ExecutionEditFixture
    Invoke-EESql 'UPDATE TonCuonLe SET SoCuoi=500 WHERE id=70'
    $first='11111111-1111-4111-8111-111111111111'
    $second='22222222-2222-4222-8222-222222222222'
    $null=Add-EETestCut $path 60 $first
    $null=Add-EETestCut $path 61 $second
    $original=Get-EEScalar "SELECT SauJson FROM B3ThaoTac WHERE MaThaoTac='$first'"
    Invoke-EESql 'UPDATE KeHoachCatChiTiet SET ChieuDaiCanCat=140 WHERE id=60'

    $query=New-EEObject 'ExecutionDetailQuery' @([long]1,[long]40,$null,[long]70)
    $snapshot=Invoke-EE 'Load' @($query)
    Assert-EE ($snapshot.Rows[0].ActualCutLength -eq 150) 'Load returns the actual cut length separately from the planned length'
    $form=New-EEForm $query 'Wire'
    try {
        $form.ShowInTaskbar=$false
        $form.StartPosition=[Windows.Forms.FormStartPosition]::Manual
        $form.Location=[Drawing.Point]::new(-30000,-30000)
        $form.Opacity=0
        $form.Show()
        [Windows.Forms.Application]::DoEvents()
        $grid=$form.Controls.Find('grvDsChiTietCuon',$true)[0]
        $actual=$form.Controls.Find('textBox1',$true)[0]
        $length=$form.Controls.Find('textBox2',$true)[0]
        $start=$form.Controls.Find('tbxSoDau_Sua',$true)[0]
        $end=$form.Controls.Find('tbxSoCuoi_Sua',$true)[0]
        $direction=$form.Controls.Find('cbxChieu',$true)[0]
        $impact=$form.Controls.Find('lblTacDong',$true)[0]
        $reason=$form.Controls.Find('rtbLyDoSua',$true)[0]
        $warning='Các thay đổi này sẽ khiến mốc đầu/cuối các đoạn khác có thể thay đổi. Kiểm tra kỹ trên dây trước khi lưu.'
        $grid.CurrentCell=$grid.Rows[0].Cells[0]
        Assert-EE ($actual.Text -eq '150' -and $length.Text -eq '150' -and $end.ReadOnly) 'Opening a cut shows both lengths and locks the computed end'
        Assert-EE ($impact.Text -eq '') 'Opening the form shows no cut impact warning'
        $direction.SelectedValue=-1
        Assert-EE ($impact.Text -ceq $warning) 'Changing direction before typing shows the agreed warning'
        $direction.SelectedValue=1
        Assert-EE ($impact.Text -eq '') 'Returning to the original direction clears the warning before editing'
        $grid.CurrentCell=$grid.Rows[1].Cells[0]
        Assert-EE $length.ReadOnly 'The later cut waits for the earlier length correction'
        $grid.CurrentCell=$grid.Rows[0].Cells[0]
        $length.Text='140'
        Assert-EE ($start.Text -eq '0' -and $end.Text -eq '360') 'Editing the first cut keeps the start'
        Assert-EE ($impact.Text -ceq $warning) 'Changing cut length shows the agreed warning'
        $reason.Text='Check the physical reel'
        [Windows.Forms.Application]::DoEvents()
        Assert-EE ($impact.Text -ceq $warning) 'Entering a reason preserves the cut impact warning'
        $wrapped=[Windows.Forms.TextRenderer]::MeasureText($warning,$impact.Font,
            [Drawing.Size]::new($impact.ClientSize.Width,[int]::MaxValue),[Windows.Forms.TextFormatFlags]::WordBreak)
        Assert-EE ($impact.ClientSize.Height -ge $wrapped.Height) 'The warning fits the label at the default form size'
        $direction.SelectedValue=-1
        Assert-EE ($start.Text -eq '360' -and $end.Text -eq '0' -and $length.ReadOnly) '3B swaps existing markers and locks the length'
        Assert-EE ($impact.Text -ceq $warning) 'The agreed warning stays visible after a 3B reversal'
        $direction.SelectedValue=1
        $length.Text='130'
        Assert-EE ($start.Text -eq '0' -and $end.Text -eq '370' -and -not $length.ReadOnly) 'A second edit replaces the earlier length from the same base'
        $length.Text='140'
        $grid.CurrentCell=$grid.Rows[1].Cells[0]
        Assert-EE ($end.Text -eq '360' -and -not $length.ReadOnly) 'The second cut starts from the corrected first cut'
        $direction.SelectedValue=-1
        Assert-EE ($start.Text -eq '360' -and $end.Text -eq '0') 'A direction change before typing swaps the base'
        $length.Text='110'
        Assert-EE ($start.Text -eq '360' -and $end.Text -eq '110') 'A cut entered after reversal keeps the reversed start'
        $grid.CurrentCell=$grid.Rows[0].Cells[0]
        Assert-EE ($length.Text -eq '140' -and $end.Text -eq '360') 'Each cut keeps its own draft when switching rows'
    } finally { $form.Dispose() }
    $request=New-EERequest $snapshot
    $edit=New-EEObject 'CutLengthEdit'
    $edit.OperationToken=$first; $edit.Length=130; $edit.Direction=1; $edit.Note='Correct against plan'
    $request.CutEdits.Add($edit)
    Assert-EE (-not (Invoke-EE 'Save' @($request)).Success) 'Length must match the plan'
    Assert-EE ((Get-EEScalar 'SELECT SoCuoi FROM TonCuonLe WHERE id=70') -eq 250) 'Rejected length leaves stock intact'

    $edit.Length=140
    Assert-EE (Invoke-EE 'Preview' @($request)).Valid 'Preview accepts the correction and dependent cut'
    $result=Invoke-EE 'Save' @($request)
    Assert-EE $result.Success "Cut length save: $($result.Message)"
    Assert-EE ((Get-EEScalar 'SELECT ChieuDaiCat FROM LichSuCat WHERE KeHoachCatChiTiet_ID=60') -eq 140) 'Actual length is corrected'
    Assert-EE ((Get-EEScalar 'SELECT SoCuoiSau FROM LichSuCat WHERE KeHoachCatChiTiet_ID=60') -eq 360) 'First cut retains its start and ends at 360'
    Assert-EE ((Get-EEScalar 'SELECT SoCuoiTruoc FROM LichSuCat WHERE KeHoachCatChiTiet_ID=61') -eq 360) 'Second cut starts from the corrected prior range'
    Assert-EE ((Get-EEScalar 'SELECT SoCuoi FROM TonCuonLe WHERE id=70') -eq 260) 'Both cuts are reflected in current stock'
    Assert-EE ((Get-EEScalar "SELECT SauJson FROM B3ThaoTac WHERE MaThaoTac='$first'") -ceq $original) 'Original operation audit stays immutable'
    Assert-EE (Invoke-EE 'Save' @($request)).Success 'Retry is idempotent'

    $undo=New-EERequest (Invoke-EE 'Load' @($query))
    Add-EEUndo $undo $second
    $result=Invoke-EE 'Save' @($undo)
    Assert-EE $result.Success "Undo after correction: $($result.Message)"
    Assert-EE ((Get-EEScalar 'SELECT SoCuoi FROM TonCuonLe WHERE id=70') -eq 360) 'Undo restores the corrected previous range'

    $third='33333333-3333-4333-8333-333333333333'
    $null=Add-EETestCut $path 61 $third
    Assert-EE ((Get-EEScalar 'SELECT SoCuoi FROM TonCuonLe WHERE id=70') -eq 260) 'B3 continues from the corrected stock'
    $reverse=New-EERequest (Invoke-EE 'Load' @($query))
    $edit=New-EEObject 'CutLengthEdit'
    $edit.OperationToken=$third; $edit.Length=100; $edit.Direction=-1; $edit.Note='Reverse before cutting'
    $reverse.CutEdits.Add($edit)
    $result=Invoke-EE 'Save' @($reverse)
    Assert-EE $result.Success "Reverse before cut: $($result.Message)"
    Assert-EE ((Get-EEScalar 'SELECT SoDau FROM TonCuonLe WHERE id=70') -eq 360 -and
        (Get-EEScalar 'SELECT SoCuoi FROM TonCuonLe WHERE id=70') -eq 100) 'Reverse before cut holds the new start at 360'

    $reverse=New-EERequest (Invoke-EE 'Load' @($query))
    $edit.ReverseAfter=$true
    $reverse.CutEdits.Add($edit)
    $performer=New-EEObject 'ExecutionRowEdit'
    $performer.OperationToken=$third; $performer.NewPerformerUsername='inactive'; $performer.Reason='INFO'
    $reverse.RowEdits.Add($performer)
    $result=Invoke-EE 'Save' @($reverse)
    Assert-EE $result.Success "Reverse after cut: $($result.Message)"
    Assert-EE ((Get-EEScalar 'SELECT SoCuoiSau FROM LichSuCat WHERE KeHoachCatChiTiet_ID=61') -eq 360 -and
        (Get-EEScalar 'SELECT SoCuoi FROM TonCuonLe WHERE id=70') -eq 360) 'The saved 3B reversal is reflected in history and stock'
    $audit=Get-EEScalar "SELECT SauJson FROM B2DieuChinhSuKien WHERE MaThaoTac='$third' AND LyDo='CUT_LENGTH' ORDER BY id DESC LIMIT 1"
    Assert-EE (($audit | ConvertFrom-Json).after.history.NguoiThucHien -eq 'inactive') 'A combined save records the actual performer in the cut audit'

    $undo=New-EERequest (Invoke-EE 'Load' @($query))
    Add-EEUndo $undo $third
    Invoke-EESql @"
INSERT INTO B2DieuChinhSuKien(MaLanLuu,Loai,DoiTuong,DoiTuong_ID,MaThaoTac,LyDo,GhiChu,TruocJson,SauJson)
SELECT MaLanLuu,Loai,DoiTuong,DoiTuong_ID,MaThaoTac,LyDo,GhiChu,SauJson,json_set(SauJson,'$.before.reel.SoCuoi',500)
FROM B2DieuChinhSuKien WHERE MaThaoTac='$third' AND LyDo='CUT_LENGTH' ORDER BY id DESC LIMIT 1;
"@
    Assert-EE (-not (Invoke-EE 'Save' @($undo)).Success) 'Malformed cut adjustment cannot drive an undo'
    Assert-EE ((Get-EEScalar 'SELECT SoCuoi FROM TonCuonLe WHERE id=70') -eq 360) 'Rejected undo preserves stock'
    Write-Output 'PASS: cut length validation, chronology, direction, audit integrity, retry and undo'
} finally { Close-ExecutionEditFixture $path }

try {
    $path=New-ExecutionEditFixture
    Invoke-EESql 'UPDATE TonCuonLe SET SoCuoi=500 WHERE id=70'
    $null=Add-EETestCut $path 60 $first
    $b3=Invoke-EEB3 $path 'cut' @{raw_qr='LE-001';ke_hoach_id=1;ke_hoach_hang_id=40;group_id=50;detail_id=61;operation_token=$second;end_value=0;direction=-1}
    Assert-EE $b3.ok 'Prepare a later cut with an explicit reversed direction'
    Invoke-EESql 'UPDATE KeHoachCatChiTiet SET ChieuDaiCanCat=140 WHERE id=60; UPDATE KeHoachCatChiTiet SET ChieuDaiCanCat=90 WHERE id=61;'
    $request=New-EERequest (Invoke-EE 'Load' @($query))
    $edit=New-EEObject 'CutLengthEdit'
    $edit.OperationToken=$first; $edit.Length=140; $edit.Direction=1; $edit.ReverseAfter=$true
    $request.CutEdits.Add($edit)
    Assert-EE (-not (Invoke-EE 'Preview' @($request)).Valid) 'A later mismatch in the same plan needs correction before the batch can save'
    Invoke-EESql 'UPDATE KeHoachCatChiTiet SET ChieuDaiCanCat=100 WHERE id=61'
    $request=New-EERequest (Invoke-EE 'Load' @($query))
    $request.CutEdits.Add($edit)
    $result=Invoke-EE 'Save' @($request)
    Assert-EE $result.Success "Replay explicit B3 direction: $($result.Message)"
    Assert-EE ((Get-EEScalar 'SELECT HeSoChieu FROM LichSuCat WHERE KeHoachCatChiTiet_ID=61') -eq -1 -and
        (Get-EEScalar 'SELECT SoDau FROM TonCuonLe WHERE id=70') -eq 360 -and
        (Get-EEScalar 'SELECT SoCuoi FROM TonCuonLe WHERE id=70') -eq 100) 'A later B3 cut keeps the explicitly selected end when its new base is already reversed'
    Write-Output 'PASS: earlier 3B reversal preserves the later explicit B3 direction'
} finally { Close-ExecutionEditFixture $path }
