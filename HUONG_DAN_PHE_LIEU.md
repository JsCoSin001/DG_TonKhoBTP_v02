# Hướng dẫn hoàn thiện UI và DB cho thay đổi Phế liệu

## 1. Các thay đổi UI nên làm bằng WinForms Designer

### UC_DieuKienBoc
Mở `UI/CD_Boc/UC_DieuKienBoc.cs` bằng **View Designer** và xóa bằng UI:

- control `nhuaPhe`
- control `dayPhe`
- control `ghiChuNhuaPhe`
- control `ghiChuDayPhe`
- các Label tương ứng của 4 control trên

Sau đó chỉnh lại `TableLayoutPanel`/khoảng cách bằng Properties hoặc kéo-thả trên Designer. Không cần sửa tay `UC_DieuKienBoc.Designer.cs`.

Code-behind đã được sửa để không còn dùng 4 control này, vì vậy sau khi xóa bằng Designer sẽ không cần sửa thêm code.

### UC_TTThanhPham
`btnNhapPhe` đã tồn tại trong Designer. Không cần tạo control mới và không cần gắn event Click trong cửa sổ Events; code đã gắn event trong constructor để tránh phải sửa Designer.

### Frm_PheLieu
Class đã đổi thành `Frm_PheLieu`. Không cần thay UI. Nếu muốn chuẩn hóa tên 3 NumericUpDown phía Thành phẩm (`rtbDayPhe_TP`, `rtbNhuaPhe_TP`, `rtbDongPhe_TP`) thì nên để một bước refactor riêng, không cần làm cho chức năng hiện tại.

## 2. DB phải cập nhật trước khi test

Backup DB trước. Chạy `database/Migration_PheLieu_20260828.sql` trên DB mục tiêu.

Migration sẽ:

- tạo `PheLieu` nếu chưa có;
- chuẩn hóa 6 giá trị số NULL thành 0;
- giữ tối đa 1 dòng `PheLieu` cho mỗi `TTThanhPham`;
- tạo UNIQUE index trên `PheLieu(TTThanhPham_ID)`;
- xóa `TTThanhPham.Phe`;
- xóa `CaiDatCDBoc.NhuaPhe`, `GhiChuNhuaPhe`, `DayPhe`, `GhiChuDayPhe`.

Lưu ý: `DROP COLUMN` yêu cầu SQLite >= 3.35. Nếu công cụ DB đang dùng SQLite cũ hơn, xóa 5 cột trên bằng chức năng **Modify Table / Delete field** của DB Browser for SQLite rồi chỉ chạy phần tạo/chuẩn hóa `PheLieu` và UNIQUE index.

## 3. Checklist test trong Visual Studio

1. Clean + Rebuild solution.
2. Thêm mới: mở Nhập phế, nhập 12 trường, bấm Lưu, mở lại kiểm tra draft còn nguyên; submit form chính và kiểm tra `PheLieu` có đúng 1 dòng.
3. Nhập phế rồi đóng X: mở lại và xác nhận thay đổi chưa Lưu bị bỏ.
4. Bấm Xóa: các ô về 0/rỗng nhưng form vẫn mở; bấm Lưu rồi submit và kiểm tra DB lưu 0.
5. Sửa bản ghi: dữ liệu phế cũ phải được nạp lại; sửa và submit phải vẫn chỉ có 1 dòng `PheLieu`.
6. Sao chép: dữ liệu phế phải bắt đầu bằng 0/rỗng, không copy từ bản nguồn.
7. Report: không còn cột `Phe`; có đúng 6 cột `DayPhe_NL`, `NhuaPhe_NL`, `DongPhe_NL`, `DayPhe_TP`, `NhuaPhe_TP`, `DongPhe_TP`; không hiển thị ghi chú phế.
8. Test riêng các công đoạn bóc sau khi đã xóa cột DB cũ để chắc chắn INSERT/UPDATE `CaiDatCDBoc` chạy bình thường.
