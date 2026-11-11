# Gán mã công đoạn thành phẩm Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans to implement this plan task-by-task.

**Goal** — Ghi mã phân loại Bọc vỏ cho sản phẩm `TP.` ở Ghép lõi và giữ truy xuất theo công đoạn thực hiện.

**Architecture** — Tập trung tính mã ghi và điều kiện đọc công đoạn thực hiện trong một lớp ở tầng SQLite. Luồng lưu dùng mã phân loại ở cột `TTThanhPham.CongDoan` và lưu chi tiết theo công đoạn của màn hình. Màn hình thao tác và báo cáo sản xuất dùng điều kiện đọc, báo cáo tồn kho dùng giá trị đã ghi.

**Tech Stack** — .NET Framework 4.8, WinForms, System.Data.SQLite, PowerShell.

**Spec** — `docs/superpowers/specs/2026-11-11-phan-loai-cong-doan-thanh-pham-design.md`.

## Global Constraints

- `TTThanhPham.CongDoan` lưu `MaCongDoan`. Ghép lõi là `6`, Bọc vỏ là `5`.
- Mã `TP.` được so từ danh mục, bỏ khoảng trắng đầu cuối và bỏ qua khác biệt chữ hoa, chữ thường.
- Mã bản tạm theo màn hình. Phân loại chính thức được tính khi chốt trong cùng giao dịch với chi tiết.
- Sửa chính thức ở Ghép lõi tính lại khi đổi `DanhSachSP_ID`. Sửa trường khác giữ giá trị đã ghi.
- Dùng cấu trúc database hiện có, quy tắc chọn sản phẩm V3 và điều kiện nhập kho hiện hành.

## Review Focus

- Sửa LOT cũ mã `6` thiếu chi tiết phải tạo chi tiết trước khi lần sửa đó chuyển mã thành `5`.
- Nếu ghi chi tiết thất bại, rollback mã phân loại và các thay đổi của lượt lưu.
- Sản phẩm đổi từ `TP.` sang `BTP.` rồi đổi lại phải mở được ở Ghép lõi với cùng ID.
- Bản ghi Bọc vỏ có cùng mã sản phẩm và bản ghi Quấn băng có chi tiết cùng bảng vẫn thuộc công đoạn thực hiện riêng.
- So dữ liệu danh mục theo ID đã chọn và giữ các điều kiện mở bản tạm, sửa và báo cáo hiện hành.

### Task 1 — Lưu và sửa thành phẩm

**Tệp** — `Database/SanXuat/CongDoanThanhPham_DB.cs`, `SubmitForm_DB.cs`, `LuuTam_DB.cs`, mục Compile trong csproj và `tests/TTThanhPham.CongDoan.Tests.ps1`.

- [x] Tạo bài kiểm tra trên bản sao SQLite. Gọi điểm vào lưu trực tiếp và chốt bản tạm cho mã thật `TP.31700100` (5084), `BTP.20104001` (749), cùng đường sửa đang dùng. Kiểm tra mã `5` hoặc `6`, liên kết chi tiết, tiền tố chuẩn hóa, mã mặc định, đổi sản phẩm hai chiều và giữ phân loại khi giữ sản phẩm.
- [x] Chạy bài kiểm tra trước sửa, xác nhận lưu trực tiếp TP. ở Ghép lõi trả mã `6` và thất bại ở kiểm tra mong đợi mã `5`.
- [x] Thêm `internal static int XacDinhMaLuuChinhThuc(SQLiteConnection conn, SQLiteTransaction tx, int maCongDoan, int danhSachSpId, object? chiTietCongDoan)`. Ghép lõi đọc `DanhSachMaSP.Ma` theo `DanhSachSP_ID` trong giao dịch hiện có. Phân loại mã `5` yêu cầu chi tiết `CD_GhepLoiQB`.
- [x] Áp dụng hàm vào ghi chính thức, chốt bản tạm và sửa chính thức khi đổi sản phẩm ở Ghép lõi. Cập nhật chi tiết trong giao dịch lưu và chèn chi tiết nếu LOT cũ thiếu dòng khi chuyển sang mã `5`.
- [x] Kiểm tra rollback khi chèn chi tiết thất bại và trường hợp LOT cũ thiếu dòng chi tiết.

### Task 2 — Mở lại và báo cáo

**Tệp** — `Database/SanXuat/CongDoanThanhPham_DB.cs`, `DatabaseHelper.cs`, `Helper/Helper.cs`, `Database/SanXuat/MonthyReport_DB.cs`, lọc cảnh báo nhập kho trong `Database/Kho/NhapKho_DB.cs`, đường sản xuất tương đương trong `Database/DB_SanXuat.cs` và bài kiểm tra ở việc 1.

- [x] Thêm kiểm tra mở STT theo công đoạn, báo cáo sản xuất theo ngày và tháng, cùng báo cáo tồn kho. Một LOT Ghép lõi mã `5` chỉ thuộc nhóm sản xuất Ghép lõi và xuất hiện ở nhóm tồn kho Bọc vỏ. Đối chiếu số dòng theo nguyên vật liệu khi chọn riêng Ghép lõi và khi chọn cả hai công đoạn. Thêm đối chứng Bọc vỏ và Quấn băng.
- [x] Thêm `internal static string TaoDieuKienCongDoanThucHien(int maCongDoan)` dùng bí danh `ttp`. Bản ghi mã `5` có ID nằm trong `CD_GhepLoiQB.TTThanhPham_ID` thuộc công đoạn thực hiện `6`. Áp dụng ở truy vấn mở STT và truy vấn sản xuất. Truy vấn tồn kho lọc theo mã đã ghi. Thêm đối chứng Nhập kho theo ngày. LOT Ghép lõi thiếu nguồn cuộn được bỏ qua, LOT Bọc vỏ thực thiếu nguồn vẫn cảnh báo và LOT Bọc vỏ có nguồn hợp lệ vẫn được trả về.
- [x] Build Debug Any CPU và x64, chạy bài kiểm tra mới trên bản sao database và các bộ hồi quy V3 và báo cáo phù hợp. Rà diff và ghi lại kết quả trước khi commit.

## Kiểm chứng

- Build Debug Any CPU và x64. Chạy `tests/TTThanhPham.CongDoan.Tests.ps1 -SourceDatabase D:/Database/QLSX_v02.db` với hai bản build. Bài kiểm tra mở database nguồn chỉ đọc và thực hiện các ca ghi trên bản sao tự tạo.
- Chạy `tests/UC_TTThanhPham.ProductSearchV3.Tests.ps1`, `tests/UC_BCTonKho.RowDetails.Tests.ps1 -Suite All` và `python -m unittest discover -s tests -p test_*.py`.
- `UC_BCTonKho.Static.ps1` dùng đường dẫn cũ tới tệp giao diện và dừng ngay ở bước đọc tệp trên cả bản nền và nhánh thực hiện.
