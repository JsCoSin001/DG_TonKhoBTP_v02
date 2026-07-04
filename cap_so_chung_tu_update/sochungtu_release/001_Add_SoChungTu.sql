-- BẮT BUỘC: sao lưu file SQLite trước khi chạy.
-- Script có thể chạy lại; SoCuoi chỉ tăng lên nếu dữ liệu hiện tại có số lớn hơn.

BEGIN IMMEDIATE;

CREATE TABLE IF NOT EXISTS SoChungTu
(
    TienTo TEXT NOT NULL COLLATE NOCASE,
    Nam INTEGER NOT NULL CHECK (Nam BETWEEN 2000 AND 9999),
    Thang INTEGER NOT NULL CHECK (Thang BETWEEN 1 AND 12),
    SoCuoi INTEGER NOT NULL DEFAULT 0 CHECK (SoCuoi >= 0),
    NgayCapNhat TEXT NOT NULL
        DEFAULT (strftime('%Y-%m-%d %H:%M:%S', 'now', 'localtime')),
    PRIMARY KEY (TienTo, Nam, Thang)
);

DROP TABLE IF EXISTS temp.Temp_SeedSoChungTu;

CREATE TEMP TABLE Temp_SeedSoChungTu
(
    TienTo TEXT NOT NULL COLLATE NOCASE,
    Nam INTEGER NOT NULL,
    Thang INTEGER NOT NULL,
    SoCuoi INTEGER NOT NULL,
    PRIMARY KEY (TienTo, Nam, Thang)
);

WITH MaHienCo AS
(
    SELECT TRIM(MaDon) AS MaChungTu
    FROM DanhSachDatHang
    WHERE MaDon IS NOT NULL
      AND TRIM(MaDon) <> ''

    UNION ALL

    SELECT TRIM(TenPhieu) AS MaChungTu
    FROM LichSuXuatNhap
    WHERE TenPhieu IS NOT NULL
      AND TRIM(TenPhieu) <> ''
),
MaHopLe AS
(
    SELECT
        MaChungTu,
        INSTR(MaChungTu, '/') AS ViTriGachCheo,
        INSTR(MaChungTu, '-') AS ViTriGachNgang
    FROM MaHienCo
    WHERE INSTR(MaChungTu, '/') > 3
      AND INSTR(MaChungTu, '-') > INSTR(MaChungTu, '/')
),
MaDaTach AS
(
    SELECT
        UPPER(TRIM(SUBSTR(MaChungTu, 1, ViTriGachCheo - 3))) AS TienTo,
        2000 + CAST(SUBSTR(MaChungTu, ViTriGachCheo - 2, 2) AS INTEGER) AS Nam,
        CAST(SUBSTR(MaChungTu, ViTriGachCheo + 1, 2) AS INTEGER) AS Thang,
        CAST(SUBSTR(MaChungTu, ViTriGachNgang + 1) AS INTEGER) AS SoThuTu
    FROM MaHopLe
)
INSERT INTO Temp_SeedSoChungTu
(
    TienTo,
    Nam,
    Thang,
    SoCuoi
)
SELECT
    TienTo,
    Nam,
    Thang,
    MAX(SoThuTu)
FROM MaDaTach
WHERE TienTo <> ''
  AND Nam BETWEEN 2000 AND 9999
  AND Thang BETWEEN 1 AND 12
  AND SoThuTu > 0
GROUP BY TienTo, Nam, Thang;

INSERT OR IGNORE INTO SoChungTu
(
    TienTo,
    Nam,
    Thang,
    SoCuoi,
    NgayCapNhat
)
SELECT
    TienTo,
    Nam,
    Thang,
    SoCuoi,
    strftime('%Y-%m-%d %H:%M:%S', 'now', 'localtime')
FROM Temp_SeedSoChungTu;

UPDATE SoChungTu
SET
    SoCuoi = MAX
    (
        SoCuoi,
        COALESCE
        (
            (
                SELECT seed.SoCuoi
                FROM Temp_SeedSoChungTu seed
                WHERE seed.TienTo = SoChungTu.TienTo
                  AND seed.Nam = SoChungTu.Nam
                  AND seed.Thang = SoChungTu.Thang
            ),
            SoCuoi
        )
    ),
    NgayCapNhat = strftime('%Y-%m-%d %H:%M:%S', 'now', 'localtime')
WHERE EXISTS
(
    SELECT 1
    FROM Temp_SeedSoChungTu seed
    WHERE seed.TienTo = SoChungTu.TienTo
      AND seed.Nam = SoChungTu.Nam
      AND seed.Thang = SoChungTu.Thang
      AND seed.SoCuoi > SoChungTu.SoCuoi
);

DROP TABLE Temp_SeedSoChungTu;

COMMIT;

-- Kiểm tra sau khi chạy:
SELECT TienTo, Nam, Thang, SoCuoi, NgayCapNhat
FROM SoChungTu
ORDER BY Nam, Thang, TienTo;
