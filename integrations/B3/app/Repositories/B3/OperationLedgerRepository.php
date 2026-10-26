<?php
namespace App\Repositories\B3;

use App\Support\B3\B3Exception;
use Illuminate\Support\Facades\DB;
use Illuminate\Support\Facades\Schema;

class OperationLedgerRepository
{
    public function assertSchema(): void
    {
        if (!Schema::hasTable('B3TinhNangDuLieu') ||
            (int) DB::table('B3TinhNangDuLieu')->where('Ten','EXECUTION_EDIT')->value('PhienBan') !== 1) {
            throw B3Exception::make('DATABASE_SCHEMA_REQUIRED','Cần cập nhật cấu trúc dữ liệu B2/B3 trước khi thực hiện.',503);
        }
    }

    public function find(string $token): ?array
    {
        $this->assertSchema();
        $row=DB::table('B3ThaoTac')->where('MaThaoTac',$token)->first();
        return $row ? (array)$row : null;
    }

    public function assertNotCancelled(string $token): ?array
    {
        $operation=$this->find($token);
        if ($operation && $operation['TrangThai']==='HUY') {
            throw B3Exception::make('OPERATION_CANCELLED','Thao tác này đã được hủy tại B2. Vui lòng tải lại dữ liệu.',409);
        }
        return $operation;
    }

    public function assertUnused(string $token): void
    {
        if ($this->assertNotCancelled($token) ||
            DB::table('LichSuCat')->where('GhiChu','__B3_OP__:'.$token)->exists() ||
            DB::table('LichSuLayCuon')->where('GhiChu','__B3_OP__:'.$token)->exists()) {
            throw B3Exception::make('DUPLICATE_REQUEST','Thao tác này đã được ghi nhận trước đó.',409);
        }
    }

    public function record(array $operation): int
    {
        $this->assertSchema();
        return (int)DB::table('B3ThaoTac')->insertGetId($operation,'ThuTu');
    }
}
