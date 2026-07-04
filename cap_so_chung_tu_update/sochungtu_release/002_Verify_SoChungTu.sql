-- Kiểm tra bảng cấp số sau khi chạy 001_Add_SoChungTu.sql

SELECT
    TienTo,
    Nam,
    Thang,
    SoCuoi,
    NgayCapNhat
FROM SoChungTu
ORDER BY Nam, Thang, TienTo;

-- Kết quả phải bằng 0: không có bộ đếm âm hoặc tháng/năm sai.
SELECT COUNT(*) AS SoDongKhongHopLe
FROM SoChungTu
WHERE SoCuoi < 0
   OR Thang NOT BETWEEN 1 AND 12
   OR Nam NOT BETWEEN 2000 AND 9999
   OR TRIM(TienTo) = '';

-- So sánh số cuối với dữ liệu nghiệp vụ hiện có.
-- Kết quả phải không có dòng nào có SoTrongDuLieu > SoCuoi.
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
SoTrongDuLieu AS
(
    SELECT
        UPPER(TRIM(SUBSTR(MaChungTu, 1, ViTriGachCheo - 3))) AS TienTo,
        2000 + CAST(SUBSTR(MaChungTu, ViTriGachCheo - 2, 2) AS INTEGER) AS Nam,
        CAST(SUBSTR(MaChungTu, ViTriGachCheo + 1, 2) AS INTEGER) AS Thang,
        MAX(CAST(SUBSTR(MaChungTu, ViTriGachNgang + 1) AS INTEGER)) AS SoTrongDuLieu
    FROM MaHopLe
    GROUP BY
        UPPER(TRIM(SUBSTR(MaChungTu, 1, ViTriGachCheo - 3))),
        2000 + CAST(SUBSTR(MaChungTu, ViTriGachCheo - 2, 2) AS INTEGER),
        CAST(SUBSTR(MaChungTu, ViTriGachCheo + 1, 2) AS INTEGER)
)
SELECT
    d.TienTo,
    d.Nam,
    d.Thang,
    d.SoTrongDuLieu,
    IFNULL(s.SoCuoi, 0) AS SoCuoi
FROM SoTrongDuLieu d
LEFT JOIN SoChungTu s
    ON s.TienTo = d.TienTo
   AND s.Nam = d.Nam
   AND s.Thang = d.Thang
WHERE d.SoTrongDuLieu > IFNULL(s.SoCuoi, 0);
