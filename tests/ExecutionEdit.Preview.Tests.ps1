. "$PSScriptRoot/ExecutionEdit.TestSupport.ps1"
$path=$null;$form=$null
$output=Join-Path $PSScriptRoot '../artifacts/execution-edit'
$null=New-Item -ItemType Directory -Path $output -Force
try {
    $path=New-ExecutionEditFixture;$null=Add-EETestCut $path
    Invoke-EESql "UPDATE B3TinhNangDuLieu SET ChoPhepSua=1 WHERE Ten='EXECUTION_EDIT';"
    $type=$script:EEAssembly.GetType('DG_TonKhoBTP_v02.UI.NghiepVuKhac.Kho.XuatKho.FrmChiTietThucHien',$true)
    foreach($case in @('partial','convert','whole')) {
        if($case -eq 'convert') {
            $r=Invoke-EEB3 $path cut @{raw_qr='cuontp;MB001;1000;PREVIEW-CUT';ke_hoach_id=1;ke_hoach_hang_id=40;group_id=51;detail_id=62;end_value=1000;direction=1;operation_token='66666666-6666-4666-8666-666666666666'}
            Assert-EE $r.ok 'Conversion preview fixture'
        }
        if($case -eq 'whole') {
            $r=Invoke-EEB3 $path take @{raw_qr='cuontp;MB001;1000;PREVIEW-TAKE';ke_hoach_id=1;ke_hoach_hang_id=40;quy_cach_cuon_chan_id=80;operation_token='77777777-7777-4777-8777-777777777777'}
            Assert-EE $r.ok 'Whole preview fixture'
        }
        $q=if($case -eq 'partial'){New-EEObject 'ExecutionDetailQuery' @([long]1,[long]40,$null,[long]70)}else{New-EEObject 'ExecutionDetailQuery' @([long]1,[long]40,[long]80,$null)}
        $form=[Activator]::CreateInstance($type,[object[]]@($q,('Cap CV 1x6 - '+$case)))
        $form.ShowInTaskbar=$false;$form.StartPosition=[Windows.Forms.FormStartPosition]::Manual
        $form.Location=New-Object Drawing.Point(-30000,-30000);$form.Opacity=0
        $form.Show();[Windows.Forms.Application]::DoEvents()
        $grid=$form.Controls.Find('grvChiTietThucHien',$true)[0]
        Assert-EE $grid.IsHandleCreated 'Preview requires a fully laid out grid handle'
        $row=@($grid.Rows|Where-Object {$_.Tag.Done -and $(if($case -eq 'whole'){$_.Tag.Kind -eq 'TAKE'}else{$_.Tag.Kind -eq 'CUT'})})[0]
        $grid.CurrentCell=$row.Cells[0]
        $bitmap=New-Object Drawing.Bitmap($form.Width,$form.Height)
        try { $form.DrawToBitmap($bitmap,(New-Object Drawing.Rectangle(0,0,$form.Width,$form.Height)));$bitmap.Save((Join-Path $output ($case+'.png')),[Drawing.Imaging.ImageFormat]::Png) } finally {$bitmap.Dispose()}
        $form.Dispose();$form=$null
    }
    Write-Output 'PASS: rendered three actual WinForms cases using isolated fixtures'
} finally { if($form){$form.Dispose()};Close-ExecutionEditFixture $path }
