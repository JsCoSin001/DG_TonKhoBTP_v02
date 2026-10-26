<?php
namespace Tests\Feature\B3;

use Illuminate\Support\Facades\DB;
use Tests\Support\CreatesExecutionEditFixture;
use Tests\TestCase;

class OperationLedgerTest extends TestCase
{
    use CreatesExecutionEditFixture;
    private const TOKEN='11111111-1111-4111-8111-111111111111';
    protected function setUp(): void { parent::setUp(); $this->createExecutionEditFixture(); }
    private function cutPayload(): array
    {
        return ['raw_qr'=>'LE-001','ke_hoach_id'=>1,'ke_hoach_hang_id'=>40,'group_id'=>50,'detail_id'=>60,'operation_token'=>self::TOKEN];
    }
    public function test_cut_records_raw_before_markers_and_authenticated_origin(): void
    {
        $this->postJson('/api/b3/execute/cut',$this->cutPayload()+['end_value'=>1650,'direction'=>1])->assertOk();
        $this->assertDatabaseHas('B3ThaoTac',['MaThaoTac'=>self::TOKEN,'NguoiGuiGoc'=>'worker','NguoiGuiGoc_ID'=>1,'TrangThai'=>'ACTIVE']);
        $before=json_decode(DB::table('B3ThaoTac')->value('TruocJson'),true,512,JSON_THROW_ON_ERROR);
        $this->assertSame(0,$before['reel']['SoDau']);
        $this->assertSame(800,$before['reel']['SoCuoi']);
    }
    public function test_recovery_after_performer_edit_uses_original_requester(): void
    {
        $this->postJson('/api/b3/execute/cut',$this->cutPayload())->assertOk();
        DB::table('LichSuCat')->update(['NguoiThucHien'=>'inactive']);
        $this->getJson('/api/b3/cut-session?'.http_build_query($this->cutPayload()))->assertOk()
            ->assertJsonPath('cut_session.operation_status','SAVED');
    }
    public function test_cancelled_token_cannot_execute_or_recover_as_not_found(): void
    {
        $this->postJson('/api/b3/execute/cut',$this->cutPayload())->assertOk();
        // Equivalent persisted result of B2 undo; exercise the B3 boundary.
        DB::table('B3ThaoTac')->update(['TrangThai'=>'HUY']);
        DB::table('LichSuCat')->delete();
        DB::table('TonCuonLe')->where('id',70)->update(['SoDau'=>0,'SoCuoi'=>800]);
        $this->postJson('/api/b3/execute/cut',$this->cutPayload())->assertStatus(409)->assertJsonPath('error_code','OPERATION_CANCELLED');
        $this->getJson('/api/b3/cut-session?'.http_build_query($this->cutPayload()))->assertStatus(409)->assertJsonPath('error_code','OPERATION_CANCELLED');
        $this->assertSame(0,DB::table('LichSuCat')->count());
        $new=$this->cutPayload();$new['operation_token']='22222222-2222-4222-8222-222222222222';
        $this->postJson('/api/b3/execute/cut',$new)->assertOk();
    }
    public function test_marker_return_to_same_value_invalidates_old_session(): void
    {
        $v=$this->getJson('/api/b3/cut-session?'.http_build_query($this->cutPayload()))->assertOk()->json('cut_session.source_version');
        DB::table('TonCuonLe')->where('id',70)->update(['SoCuoi'=>700]);
        DB::table('TonCuonLe')->where('id',70)->update(['SoCuoi'=>800]);
        $this->postJson('/api/b3/execute/cut',$this->cutPayload()+['source_version'=>$v])->assertStatus(409)->assertJsonPath('error_code','SOURCE_STATE_CHANGED');
    }
    public function test_full_take_records_token_and_source_without_changing_imported_quantity(): void
    {
        $p=['raw_qr'=>'cuontp;MB001;1000;TAKE-ONE','ke_hoach_id'=>1,'ke_hoach_hang_id'=>40,'quy_cach_cuon_chan_id'=>80,'operation_token'=>self::TOKEN];
        $this->postJson('/api/b3/execute/take-whole',$p)->assertOk();
        $this->assertDatabaseHas('B3ThaoTac',['Loai'=>'TAKE','TTCuonDay_ID'=>30,'RawQr'=>$p['raw_qr']]);
        $this->assertDatabaseHas('TTCuonDay',['id'=>30,'SoCuon'=>10]);
    }
}
