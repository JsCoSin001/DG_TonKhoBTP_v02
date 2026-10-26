<?php
namespace Tests\Feature\B3;

use Illuminate\Support\Facades\DB;
use Illuminate\Support\Facades\Schema;
use Tests\Support\CreatesFactoryFlowSchema;
use Tests\TestCase;

class ExecutionEditSchemaTest extends TestCase
{
    use CreatesFactoryFlowSchema;

    public function test_shared_schema_preserves_cancelled_tokens_and_immutable_identity(): void
    {
        $this->createFactoryFlowSchema();
        $this->assertTrue(Schema::hasTable('B3ThaoTac'), 'Missing shared execution ledger');
        DB::table('B3ThaoTac')->insert([
            'MaThaoTac'=>'test-token','Loai'=>'CUT','LichSu_ID'=>10,'KeHoach_ID'=>1,
            'KeHoachHang_ID'=>1,'TTCuonDay_ID'=>1,'RawQr'=>'QR','NguoiGuiGoc_ID'=>1,
            'NguoiGuiGoc'=>'worker','TruocJson'=>'{}','SauJson'=>'{}',
        ]);
        DB::table('B3ThaoTac')->where('MaThaoTac','test-token')->update(['TrangThai'=>'HUY']);
        $this->assertSame('HUY', DB::table('B3ThaoTac')->value('TrangThai'));
        $this->expectException(\Illuminate\Database\QueryException::class);
        DB::table('B3ThaoTac')->where('MaThaoTac','test-token')->update(['NguoiGuiGoc'=>'other']);
    }
}
