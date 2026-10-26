<?php
namespace Tests\Support;

use Illuminate\Support\Facades\DB;
use Illuminate\Support\Facades\Schema;

trait CreatesExecutionEditFixture
{
    protected function createExecutionEditFixture(): void
    {
        $this->assertSame(':memory:', DB::connection()->getDatabaseName());
        Schema::dropAllTables();
        $root = dirname(__DIR__, 4);
        DB::unprepared(file_get_contents($root.'/tests/fixtures/execution-edit/base-schema.sql'));
        DB::unprepared(file_get_contents($root.'/tests/fixtures/execution-edit/seed.sql'));
        DB::unprepared(file_get_contents($root.'/database/Migration_B2B3_ExecutionEdit_20261026.sql'));
        $this->actingAs(\App\Models\User::query()->findOrFail(1));
    }
}
