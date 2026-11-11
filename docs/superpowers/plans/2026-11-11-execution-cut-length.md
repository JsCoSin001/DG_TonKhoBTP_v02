# Điều chỉnh chiều dài cắt

**Mục tiêu** Cho sửa chiều dài của lần cắt đã thực hiện và đồng bộ chuỗi cắt, tồn và hoàn tác.

**Thiết kế** Tính các cặp mốc theo thứ tự thao tác B3. Dùng giao dịch xem trước và lưu hiện có. Ghi lịch sử điều chỉnh với dữ liệu trước và sau để hoàn tác dựa trên trạng thái có hiệu lực.

**Kỹ thuật** WinForms, C# 8, .NET Framework 4.8, SQLite và kiểm thử PowerShell.

**Đặc tả** `docs/superpowers/specs/2026-11-11-execution-cut-length-design.md`

## Các bước

1. Chạy kiểm thử lưu hai lần cắt từ `500 → 350 → 250`, sửa chiều dài lần đầu thành `140` và kiểm tra mốc `360 → 260` tương ứng. Thực thi cả trường hợp không khớp kế hoạch và hoàn tác lần sau. Nạp dữ liệu kế hoạch, chiều dài ghi nhận và thứ tự; dùng cùng phép tính cho xem trước và lưu.
2. Chạy kiểm thử WinForms cho hai chiều, đổi chiều trước và sau khi nhập, thay giá trị trong cùng dòng, khóa số cuối và chuyển giữa các dòng. Gắn các ô với dữ liệu dòng và bản nháp.
3. Biên dịch dự án và chạy các kiểm thử hiện có về đọc dữ liệu, sửa mốc, lịch sử, hoàn tác, xung đột phiên bản và dữ liệu liên ứng dụng.
