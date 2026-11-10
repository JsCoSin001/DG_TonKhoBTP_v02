"""Integration contract for configurable production-stage product search.
Runs with Python stdlib sqlite3 against isolated SQLite fixtures, never a production DB.
"""
import pathlib
import re
import sqlite3
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]
MIGRATION = ROOT / 'database' / 'migrations' / '2026-11-10_product_search_rules.sql'
SERVICE = ROOT / 'DG_TonKhoBTP_v02' / 'Database' / 'CongDoanTimKiem_DB.cs'
SOURCE_DIR = ROOT / 'DG_TonKhoBTP_v02'


class ProductSearchContractTests(unittest.TestCase):
    def setUp(self):
        self.conn = sqlite3.connect(':memory:')
        self.conn.execute('PRAGMA foreign_keys=ON')
        self.conn.executescript('''
            CREATE TABLE DanhSachCongDoan (
                id INTEGER PRIMARY KEY,
                MaCongDoan INTEGER,
                TenCongDoan TEXT
            );
            CREATE TABLE DanhSachMaSP (
                id INTEGER PRIMARY KEY,
                Ten TEXT NOT NULL,
                Ma TEXT NOT NULL UNIQUE,
                DonVi TEXT NOT NULL DEFAULT 'kg',
                ChuyenDoi REAL NOT NULL DEFAULT 1,
                Active INTEGER NOT NULL DEFAULT 1
            );
            CREATE TABLE CongDoan (Id INTEGER PRIMARY KEY, BaseCongDoanId INTEGER);
            INSERT INTO CongDoan VALUES(6, NULL);
        ''')
        for code in range(11):
            # Purposefully use non-sequential IDs to reject any hidden +1 mapping
            self.conn.execute('INSERT INTO DanhSachCongDoan VALUES (?, ?, ?)', (100 + code * 7, code, f'Stage {code}'))
        self.conn.executemany('INSERT INTO DanhSachMaSP(id,Ten,Ma,Active) VALUES (?,?,?,?)', [
            (101,'CX sample','BTP.20104001',1),
            (102,'CX sample','BTP.20204009',1),
            (103,'CX sample','TP.30100100',1),
            (104,'CX sample','BTP.20104000',1),
            (105,'CX inactive','BTP.20104008',0),
            (106,'Another name','BTP.20104002',1),
            (107,'CX sample','BTP.20106001',1),
        ])
        self.conn.commit()

    def tearDown(self):
        self.conn.close()

    def migrate(self):
        self.conn.executescript(MIGRATION.read_text(encoding='utf-8'))

    def get_search_sql(self):
        source = SERVICE.read_text(encoding='utf-8-sig')
        match = re.search(r'private const string SearchSql\s*=\s*@"(.*?)"\s*;', source, re.S)
        self.assertIsNotNone(match, 'SearchSql C# verbatim string is missing')
        return match.group(1).replace('""', '"')

    def search(self, code=6, keyword='CX'):
        catalog_id, = self.conn.execute('SELECT id FROM DanhSachCongDoan WHERE MaCongDoan=?', (code,)).fetchone()
        return set(row[0] for row in self.conn.execute(self.get_search_sql(), {
            'TuKhoa': keyword, 'DanhSachCongDoan_id': catalog_id
        }).fetchall())

    def test_schema_seeds_all_22_patterns_and_preserves_legacy_table(self):
        self.migrate()
        self.assertEqual(self.conn.execute('SELECT COUNT(*) FROM CongDoan_MaTimKiem').fetchone()[0],22)
        self.assertEqual(self.conn.execute('SELECT COUNT(*) FROM CongDoan_DacBiet').fetchone()[0],0)
        self.assertEqual([row[1] for row in self.conn.execute('PRAGMA table_info(CongDoan_MaTimKiem)')],
            ['Id','DanhSachCongDoan_id','MaTimKiem','Active','GhiChu'])
        self.assertEqual([row[1] for row in self.conn.execute('PRAGMA table_info(CongDoan_DacBiet)')],
            ['Id','DanhSachCongDoan_id','DanhSachMaSP_ID','LoaiQuyTac','Active','GhiChu'])
        self.assertIsNotNone(self.conn.execute("SELECT 1 FROM sqlite_master WHERE type='table' AND name='CongDoan'").fetchone())
        self.assertEqual(self.conn.execute('PRAGMA foreign_key_check').fetchall(),[])
        stage6id, = self.conn.execute('SELECT id FROM DanhSachCongDoan WHERE MaCongDoan=6').fetchone()
        self.assertEqual(set(r[0] for r in self.conn.execute('SELECT MaTimKiem FROM CongDoan_MaTimKiem WHERE DanhSachCongDoan_id=?',(stage6id,))), {'BTP.20104%','BTP.20204%'})
        self.assertEqual(set(r[0] for r in self.conn.execute('SELECT MaTimKiem FROM CongDoan_MaTimKiem m JOIN DanhSachCongDoan d ON d.id=m.DanhSachCongDoan_id WHERE d.MaCongDoan=8')), {'BTP.20107%', 'BTP.20207%'})

    def assert_failed_migration_leaves_no_schema_changes(self):
        # Simulate a SQLite client that attempts every statement even after an error.
        errors = []
        statement = ''
        for line in MIGRATION.read_text(encoding='utf-8').splitlines(keepends=True):
            statement += line
            if sqlite3.complete_statement(statement):
                try:
                    self.conn.execute(statement)
                except sqlite3.DatabaseError as error:
                    errors.append(error)
                statement = ''
        self.assertTrue(errors)
        self.assertFalse(self.conn.in_transaction)
        self.assertEqual(self.conn.execute("""
            SELECT name FROM sqlite_schema
            WHERE name IN ('CongDoan_MaTimKiem', 'CongDoan_DacBiet',
                           'UX_DanhSachCongDoan_MaCongDoan')
        """).fetchall(), [])

    def test_missing_stage_rolls_back_even_if_runner_continues(self):
        self.conn.execute('DELETE FROM DanhSachCongDoan WHERE MaCongDoan=10')
        self.conn.commit()
        self.assert_failed_migration_leaves_no_schema_changes()

    def test_duplicate_stage_rolls_back_even_if_runner_continues(self):
        self.conn.execute("INSERT INTO DanhSachCongDoan VALUES(999,6,'Duplicate')")
        self.conn.commit()
        self.assert_failed_migration_leaves_no_schema_changes()

    def test_seed_idempotent_and_does_not_reset_deactivated_rules(self):
        self.migrate()
        self.conn.execute("UPDATE CongDoan_MaTimKiem SET Active=0 WHERE MaTimKiem='BTP.20104%'")
        self.conn.commit()
        self.migrate()
        self.assertEqual(self.conn.execute('SELECT COUNT(*) FROM CongDoan_MaTimKiem').fetchone()[0],22)
        self.assertEqual(self.conn.execute("SELECT Active FROM CongDoan_MaTimKiem WHERE MaTimKiem='BTP.20104%'").fetchone()[0],0)

    def test_fks_restrict_invalid_catalog_and_product_and_unique_rules(self):
        self.migrate()
        with self.assertRaises(sqlite3.IntegrityError):
            self.conn.execute("INSERT INTO CongDoan_DacBiet(DanhSachCongDoan_id,DanhSachMaSP_ID,LoaiQuyTac) VALUES(99999,101,'CHO_PHEP')")
        with self.assertRaises(sqlite3.IntegrityError):
            self.conn.execute("INSERT INTO CongDoan_DacBiet(DanhSachCongDoan_id,DanhSachMaSP_ID,LoaiQuyTac) VALUES(142,99999,'LOAI_TRU')")
        with self.assertRaises(sqlite3.IntegrityError):
            self.conn.execute("INSERT INTO CongDoan_DacBiet(DanhSachCongDoan_id,DanhSachMaSP_ID,LoaiQuyTac) VALUES(142,101,'UNKNOWN')")
        catalog_id = self.conn.execute('SELECT id FROM DanhSachCongDoan WHERE MaCongDoan=6').fetchone()[0]
        self.conn.execute("INSERT INTO CongDoan_DacBiet(DanhSachCongDoan_id,DanhSachMaSP_ID,LoaiQuyTac) VALUES(?,?,?)",(catalog_id,101,'LOAI_TRU'))
        with self.assertRaises(sqlite3.IntegrityError):
            self.conn.execute("INSERT INTO CongDoan_DacBiet(DanhSachCongDoan_id,DanhSachMaSP_ID,LoaiQuyTac) VALUES(?,?,?)",(catalog_id,101,'CHO_PHEP'))
        self.conn.rollback()

    def test_search_combines_pattern_allow_deny_and_active_without_duplicates(self):
        self.migrate()
        self.assertEqual(self.search(),{101,102,104})
        catalog_id = self.conn.execute('SELECT id FROM DanhSachCongDoan WHERE MaCongDoan=6').fetchone()[0]
        self.conn.execute("INSERT INTO CongDoan_DacBiet(DanhSachCongDoan_id,DanhSachMaSP_ID,LoaiQuyTac) VALUES(?,?,?)",(catalog_id,103,'CHO_PHEP'))
        self.conn.execute("INSERT INTO CongDoan_DacBiet(DanhSachCongDoan_id,DanhSachMaSP_ID,LoaiQuyTac) VALUES(?,?,?)",(catalog_id,104,'LOAI_TRU'))
        self.assertEqual(self.search(),{101,102,103})
        # Each stage must be isolated from the others.
        self.assertEqual(self.search(7),{107})
        # Active=0 on the special rule disables its effects immediately.
        self.conn.execute("UPDATE CongDoan_DacBiet SET Active=0 WHERE DanhSachMaSP_ID=104")
        self.assertEqual(self.search(),{101,102,103,104})
        # Product Active=0 never exposed even if explicitly allowed.
        self.conn.execute("INSERT INTO CongDoan_DacBiet(DanhSachCongDoan_id,DanhSachMaSP_ID,LoaiQuyTac) VALUES(?,?,?)",(catalog_id,105,'CHO_PHEP'))
        self.assertNotIn(105,self.search(keyword='CX'))

    def test_configuration_error_is_checked_by_service(self):
        src = SERVICE.read_text(encoding='utf-8-sig')
        self.assertIn('HasAllowingRuleSql',src)
        self.assertIn("LoaiQuyTac = 'CHO_PHEP'",src)
        self.assertIn('InvalidOperationException',src)
        self.assertIn('MaCongDoan = @MaCongDoan',src)

    def test_ui_and_reports_no_longer_use_patterns_in_source(self):
        files = {
            'Dictionary': SOURCE_DIR / 'Dictionary' / 'ThongTinChungCongDoan.cs',
            'Model': SOURCE_DIR / 'Models' / 'OtherClass.cs',
            'UI': SOURCE_DIR / 'UI' / 'ThanhPhamCD' / 'UC_TTThanhPham.cs',
            'Report': SOURCE_DIR / 'Helper' / 'ReportHelper.cs',
            'Project': SOURCE_DIR / 'DG_TonKhoBTP_v02.csproj',
        }
        contents = {k:v.read_text(encoding='utf-8-sig') for k,v in files.items()}
        self.assertNotIn('ListMa_Accept', ''.join(contents[k] for k in ('Dictionary','Model','UI','Report')))
        self.assertNotIn('BTP.20104%',contents['Dictionary'])
        self.assertIn('CongDoanTimKiem_DB.TimSanPham(congDoan.Id, keyword)',contents['UI'])
        self.assertIn('CongDoanTimKiem_DB.cs',contents['Project'])
        self.assertIn('TTThanhPham.CongDoan = @CongDoanId',contents['Report'])
        self.assertNotIn('DanhSachMaSP.Ma LIKE',contents['Report'])

if __name__ == '__main__':
    unittest.main()
