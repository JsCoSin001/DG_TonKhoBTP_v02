-- Production-stage product search rules. Run on a BACKUP first.
-- Use a client that stops at the first error; roll back if a transaction is still open.
-- This migration does not modify BOM, SubmitForm, report history, or drop CongDoan.
-- foreign_keys must be enabled for every connection that edits these tables.
PRAGMA foreign_keys = ON;
BEGIN IMMEDIATE TRANSACTION;

-- The application addresses stage codes 0..10, whereas these FK columns store
-- DanhSachCongDoan.id. Reject a database with missing stage codes.
CREATE UNIQUE INDEX IF NOT EXISTS UX_DanhSachCongDoan_MaCongDoan
    ON DanhSachCongDoan(MaCongDoan);

CREATE TABLE IF NOT EXISTS CongDoan_MaTimKiem (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    DanhSachCongDoan_id INTEGER NOT NULL,
    MaTimKiem TEXT NOT NULL CHECK (length(trim(MaTimKiem)) > 0),
    Active INTEGER NOT NULL DEFAULT 1 CHECK (Active IN (0, 1)),
    GhiChu TEXT,
    UNIQUE (DanhSachCongDoan_id, MaTimKiem),
    FOREIGN KEY (DanhSachCongDoan_id)
        REFERENCES DanhSachCongDoan(id) ON UPDATE CASCADE ON DELETE RESTRICT
);
CREATE INDEX IF NOT EXISTS IX_CongDoan_MaTimKiem_CD_Active
    ON CongDoan_MaTimKiem(DanhSachCongDoan_id, Active);

CREATE TABLE IF NOT EXISTS CongDoan_DacBiet (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    DanhSachCongDoan_id INTEGER NOT NULL,
    DanhSachMaSP_ID INTEGER NOT NULL,
    LoaiQuyTac TEXT NOT NULL CHECK (LoaiQuyTac IN ('CHO_PHEP', 'LOAI_TRU')),
    Active INTEGER NOT NULL DEFAULT 1 CHECK (Active IN (0, 1)),
    GhiChu TEXT,
    UNIQUE (DanhSachCongDoan_id, DanhSachMaSP_ID),
    FOREIGN KEY (DanhSachCongDoan_id)
        REFERENCES DanhSachCongDoan(id) ON UPDATE CASCADE ON DELETE RESTRICT,
    FOREIGN KEY (DanhSachMaSP_ID)
        REFERENCES DanhSachMaSP(id) ON UPDATE CASCADE ON DELETE RESTRICT
);
CREATE INDEX IF NOT EXISTS IX_CongDoan_DacBiet_CD_Active_Loai
    ON CongDoan_DacBiet(DanhSachCongDoan_id, Active, LoaiQuyTac);

-- Seed by business-stage code; never hardcode DanhSachCongDoan.id.
-- OR IGNORE preserves administrator decisions on rerun, including Active=0.
WITH Nguon(MaCongDoan, MaTimKiem) AS (
    VALUES
    (0,'BTP.20101%'), (0,'BTP.20201%'),
    (1,'BTP.20102%'), (1,'BTP.20202%'),
    (2,'BTP.20107%'), (2,'BTP.20207%'),
    (3,'BTP.20103%'), (3,'BTP.20203%'),
    (4,'BTP.20105%'), (4,'BTP.20205%'),
    (5,'TP.%'),
    (6,'BTP.20104%'), (6,'BTP.20204%'),
    (7,'BTP.20106%'), (7,'BTP.20206%'),
    (8,'BTP.20107%'), (8,'BTP.20207%'),
    (9,'BTP.%'), (9,'TP.%'), (9,'NVL.%'),
    (10,'TP.356%'), (10,'GCCX.101%')
)
INSERT OR IGNORE INTO CongDoan_MaTimKiem (DanhSachCongDoan_id, MaTimKiem, Active)
SELECT cd.id, src.MaTimKiem, 1
FROM Nguon AS src
INNER JOIN DanhSachCongDoan AS cd ON cd.MaCongDoan = src.MaCongDoan;

-- Keep the rollback guard after all persistent writes so a client continuing
-- after a failed check cannot apply schema changes outside the transaction.
CREATE TEMP TABLE _ProductSearchCheck (
    Stages INTEGER NOT NULL CHECK (Stages = 11),
    DuplicateCodes INTEGER NOT NULL CHECK (DuplicateCodes = 0),
    Seeds INTEGER NOT NULL CHECK (Seeds = 22)
);
WITH Nguon(MaCongDoan, MaTimKiem) AS (
    VALUES
    (0,'BTP.20101%'), (0,'BTP.20201%'),
    (1,'BTP.20102%'), (1,'BTP.20202%'),
    (2,'BTP.20107%'), (2,'BTP.20207%'),
    (3,'BTP.20103%'), (3,'BTP.20203%'),
    (4,'BTP.20105%'), (4,'BTP.20205%'),
    (5,'TP.%'),
    (6,'BTP.20104%'), (6,'BTP.20204%'),
    (7,'BTP.20106%'), (7,'BTP.20206%'),
    (8,'BTP.20107%'), (8,'BTP.20207%'),
    (9,'BTP.%'), (9,'TP.%'), (9,'NVL.%'),
    (10,'TP.356%'), (10,'GCCX.101%')
)
INSERT OR ROLLBACK INTO _ProductSearchCheck (Stages, DuplicateCodes, Seeds)
SELECT
    (SELECT COUNT(DISTINCT MaCongDoan) FROM DanhSachCongDoan
     WHERE MaCongDoan BETWEEN 0 AND 10),
    (SELECT COUNT(*) FROM (
        SELECT MaCongDoan FROM DanhSachCongDoan WHERE MaCongDoan IS NOT NULL
        GROUP BY MaCongDoan HAVING COUNT(*) > 1
    )),
    (SELECT COUNT(*) FROM Nguon AS src
     JOIN DanhSachCongDoan AS cd ON cd.MaCongDoan = src.MaCongDoan
     JOIN CongDoan_MaTimKiem AS rule ON rule.DanhSachCongDoan_id = cd.id
                                   AND rule.MaTimKiem = src.MaTimKiem);
DROP TABLE _ProductSearchCheck;

COMMIT;
