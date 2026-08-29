-- Migration phế liệu 1-1 theo thiết kế ngày 2026-08-28.
-- Hãy BACKUP database trước khi chạy.
-- Phần DROP COLUMN yêu cầu SQLite >= 3.35.0.

PRAGMA foreign_keys = OFF;
BEGIN IMMEDIATE;

-- 1) Tạo PheLieu nếu database hiện tại chưa có bảng này.
CREATE TABLE IF NOT EXISTS PheLieu (
    id INTEGER NOT NULL PRIMARY KEY,
    TTThanhPham_ID INTEGER NOT NULL,
    DayPhe_NL REAL NOT NULL DEFAULT 0,
    NhuaPhe_NL REAL NOT NULL DEFAULT 0,
    DongPhe_NL REAL NOT NULL DEFAULT 0,
    GhiChuDayPhe_NL TEXT,
    GhiChuNhuaPhe_NL TEXT,
    GhiChuDongPhe_NL TEXT,
    DayPhe_TP REAL NOT NULL DEFAULT 0,
    NhuaPhe_TP REAL NOT NULL DEFAULT 0,
    DongPhe_TP REAL NOT NULL DEFAULT 0,
    GhiChuDayPhe_TP TEXT,
    GhiChuNhuaPhe_TP TEXT,
    GhiChuDongPhe_TP TEXT,
    FOREIGN KEY (TTThanhPham_ID) REFERENCES TTThanhPham(id)
        ON UPDATE CASCADE ON DELETE CASCADE
);

-- 2) Chuẩn hóa dữ liệu cũ: số NULL -> 0, bỏ dòng không gắn được với thành phẩm.
DELETE FROM PheLieu
WHERE TTThanhPham_ID IS NULL
   OR NOT EXISTS (
        SELECT 1 FROM TTThanhPham tp WHERE tp.id = PheLieu.TTThanhPham_ID
   );

UPDATE PheLieu
SET DayPhe_NL = COALESCE(DayPhe_NL, 0),
    NhuaPhe_NL = COALESCE(NhuaPhe_NL, 0),
    DongPhe_NL = COALESCE(DongPhe_NL, 0),
    DayPhe_TP = COALESCE(DayPhe_TP, 0),
    NhuaPhe_TP = COALESCE(NhuaPhe_TP, 0),
    DongPhe_TP = COALESCE(DongPhe_TP, 0);

-- 3) Nếu dữ liệu cũ từng có nhiều dòng phế cho cùng một thành phẩm,
--    giữ dòng có id lớn nhất rồi khóa quan hệ thành 1-1 ở phía PheLieu.
DELETE FROM PheLieu
WHERE id NOT IN (
    SELECT MAX(id)
    FROM PheLieu
    GROUP BY TTThanhPham_ID
);

CREATE UNIQUE INDEX IF NOT EXISTS UX_PheLieu_TTThanhPham_ID
    ON PheLieu(TTThanhPham_ID);

-- 4) Loại bỏ các cột nghiệp vụ cũ đã được thay thế bởi bảng PheLieu.
ALTER TABLE TTThanhPham DROP COLUMN Phe;
ALTER TABLE CaiDatCDBoc DROP COLUMN NhuaPhe;
ALTER TABLE CaiDatCDBoc DROP COLUMN GhiChuNhuaPhe;
ALTER TABLE CaiDatCDBoc DROP COLUMN DayPhe;
ALTER TABLE CaiDatCDBoc DROP COLUMN GhiChuDayPhe;

COMMIT;
PRAGMA foreign_keys = ON;
