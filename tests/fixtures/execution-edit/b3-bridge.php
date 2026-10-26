<?php
// Test-only bridge. Never accepts an arbitrary production database.
$options=getopt('', ['db:','action:','payload:']);
$db=realpath($options['db']??'');
$temp=realpath(sys_get_temp_dir());
if (!$db || !$temp || !str_starts_with(strtolower($db),strtolower($temp.DIRECTORY_SEPARATOR)) ||
    !str_starts_with(basename($db),'ExecutionEdit.')) { fwrite(STDERR,"Unsafe test database\n"); exit(2); }
$probe=new PDO('sqlite:'.$db);
if ($probe->query("SELECT COUNT(*) FROM ExecutionEditTestSentinel WHERE Name='EXECUTION_EDIT_TEST'")->fetchColumn()!=1) exit(2);
$probe=null;
if (($options['action']??'')==='hold') {
    $lock=new PDO('sqlite:'.$db);
    $lock->exec('BEGIN IMMEDIATE');
    file_put_contents($db.'.locked','ready');
    usleep(2800000); // Longer than B2's 2-second timeout: exercise its retry.
    $lock->exec('ROLLBACK');
    exit(0);
}
$root=dirname(__DIR__,3);
require $root.'/integrations/B3/vendor/autoload.php';
putenv('APP_ENV=testing');
$app=require $root.'/integrations/B3/bootstrap/app.php';
$app->make(Illuminate\Contracts\Console\Kernel::class)->bootstrap();
config(['app.key'=>'base64:AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=',
    'database.default'=>'sqlite','database.connections.sqlite.database'=>$db,
    'database.connections.sqlite.url'=>null,'session.driver'=>'array','cache.default'=>'array']);
Illuminate\Support\Facades\DB::purge('sqlite');
$p=json_decode(file_get_contents($options['payload']),true,512,JSON_THROW_ON_ERROR);
$actor=App\Models\User::query()->findOrFail($p['actor_user_id']??1);
try {
    $service=$app->make(App\Services\B3\ExecutionService::class);
    $result=match($options['action']??'') {
        'scan'=>$app->make(App\Services\B3\ScanContextService::class)->resolve($p['raw_qr'],$p['ke_hoach_id']??null,(int)$actor->getAuthIdentifier()),
        'take'=>$service->takeWhole($p['raw_qr'],$p['ke_hoach_id'],$p['ke_hoach_hang_id'],$p['quy_cach_cuon_chan_id'],$p['operation_token'],$actor),
        'cut'=>$service->cut($p['raw_qr'],$p['ke_hoach_id'],$p['ke_hoach_hang_id'],$p['group_id'],$p['detail_id'],$p['end_value']??null,$p['direction']??null,$p['operation_token'],$actor,$p['source_version']??null,$p['expected_cut_length']??null),
        'recover'=>$app->make(App\Services\B3\CutSessionService::class)->recover($p['raw_qr'],$p['ke_hoach_id'],$p['ke_hoach_hang_id'],$p['group_id'],$actor,$p['operation_token']??null,$p['detail_id']??null),
        default=>throw new InvalidArgumentException('Unknown bridge action'),
    };
    echo json_encode(['ok'=>true,'result'=>$result],JSON_THROW_ON_ERROR);
} catch(App\Support\B3\B3Exception $e) {
    echo json_encode(['ok'=>false,'error_code'=>$e->errorCode,'message'=>$e->getMessage()],JSON_THROW_ON_ERROR);
}
