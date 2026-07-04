# Hướng dẫn triển khai cơ chế cấp số chứng từ

## Mục tiêu

- UI chỉ hiển thị mã dự kiến trước lần lưu đầu tiên.
- Mã chính thức được cấp trong cùng transaction với dữ liệu dòng đầu tiên.
- Transaction nào lấy được khóa ghi SQLite trước sẽ nhận số trước.
- Các dòng tiếp theo dùng lại mã chính thức của cùng đơn/phiếu.
- Edit không cấp số mới.
- Xóa phiếu đã lưu không hoàn lại số.

## File trong gói

- `001_Add_SoChungTu.sql`: tạo và seed bảng cấp số từ dữ liệu hiện tại.
- `002_Verify_SoChungTu.sql`: kiểm tra sau migration.
- `SoChungTu_DB.cs`: class cấp số dùng chung.
- `MuaVatTu_DB.cs`: DB logic mới cho mua vật tư/dịch vụ.
- `NhapXuatVatTu_DB.cs`: DB logic mới cho nhập/xuất.
- `MuaVatTu_Model.cs`: thêm kết quả lưu đơn mua.
- `NhapXuatVatTu_Model.cs`: thêm trạng thái tạo phiếu mới và ID trả về.
- `UC_MuaVatTu_v2.cs`: giữ `MaDon` và `DanhSachDatHangId` sau lần lưu đầu.
- `UC_NhapXuatVatTu_v2.cs`: giữ `TenPhieu` và `DanhSachDatHangId` của Nhập khác.

## Thứ tự triển khai

1. Dừng toàn bộ ứng dụng đang dùng file SQLite.
2. Sao lưu nguyên file database.
3. Chạy `001_Add_SoChungTu.sql` trên database thật.
4. Chạy `002_Verify_SoChungTu.sql`.
5. Thêm `SoChungTu_DB.cs` vào project, trong namespace `DG_TonKhoBTP_v02.Database`.
6. Thay thế sáu file C# còn lại bằng phiên bản trong gói.
7. Build toàn bộ solution.
8. Cập nhật tất cả máy trạm cùng lúc. Không để phiên bản cũ và phiên bản mới cùng ghi dữ liệu.
9. Kiểm thử đồng thời trước khi đưa vào vận hành.

## Hành vi sau cập nhật

### `UC_MuaVatTu_v2`

- Trước lần lưu đầu, `tbMaDon` là mã dự kiến.
- Lần lưu đầu tạo `DanhSachDatHang`, cấp `MaDon` thật và trả `DanhSachDatHangId`.
- Những dòng sau insert trực tiếp theo `DanhSachDatHangId`, không tìm lại header bằng mã.
- Ngày bị khóa sau dòng đầu; Edit vẫn mở ngày theo hành vi hiện tại.
- Nút Hoàn thành reset đơn và hiển thị mã dự kiến tiếp theo.

### `UC_NhapXuatVatTu_v2`

- Trước lần lưu đầu, `tbMaPhieu` là mã dự kiến.
- Lần lưu đầu cấp `KNK` hoặc `KXK` thật trong transaction.
- Những dòng sau dùng lại `_currentTenPhieu`.
- Riêng Nhập khác, UI giữ thêm `_currentDanhSachDatHangId`; DB không còn tìm hoặc tái sử dụng header theo `MaDon`.
- Edit giữ nguyên `TenPhieu`, không tăng `SoChungTu`.
- Nút Hoàn thành reset phiếu và hiển thị mã dự kiến tiếp theo.

## Kiểm thử bắt buộc

1. Mở hai phiên ứng dụng với cùng loại phiếu và cùng tháng.
2. Cả hai có thể thấy cùng mã dự kiến.
3. Bấm Lưu gần như đồng thời.
4. Xác nhận hai mã thực tế khác nhau và liên tiếp.
5. Thêm nhiều dòng trên mỗi phiên; tất cả dòng của mỗi phiên phải dùng chung mã của phiên đó.
6. Kiểm thử Nhập khác: các dòng cùng phiếu phải có chung `DanhSachDatHang_ID`.
7. Sửa một dòng: `SoChungTu.SoCuoi` không thay đổi.
8. Xóa toàn bộ phiếu: `SoChungTu.SoCuoi` không giảm.
9. Gây lỗi ở lần lưu đầu: transaction rollback và không để lại dữ liệu dở dang.

## Lưu ý vận hành

- Bảng `SoChungTu` phải được tạo trước khi chạy code mới.
- `busy_timeout` được cấu hình 10 giây trên các connection ghi liên quan.
- Số bị xóa sau khi phiếu đã lưu sẽ không được tái sử dụng.
- Số trong transaction thất bại được rollback cùng dữ liệu và có thể được cấp lại.
- Nếu có chương trình khác tự ghi `MaDon`/`TenPhieu` mà không đi qua `SoChungTu_DB`, phải sửa chương trình đó hoặc chạy lại script seed trước khi chuyển hoàn toàn sang phiên bản mới.
