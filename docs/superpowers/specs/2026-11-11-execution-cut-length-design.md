# Điều chỉnh chiều dài cắt trong chi tiết thực hiện

`textBox1` hiển thị chiều dài đã ghi nhận. `textBox2` dùng để sửa chiều dài của dòng cắt đang chọn. Khi lưu, chiều dài sửa phải bằng chiều dài kế hoạch của dòng đó. Số cuối là kết quả tính và có thể sao chép. Số đầu cố định trong lúc nhập chiều dài.

Mỗi lần tính dùng cặp mốc trước lần cắt đang chọn. Với cuộn còn `0 → 350`, cắt `140 m` cho `0 → 210`. Nếu đổi chiều trước khi nhập, cắt cùng chiều dài cho `350 → 140`. Đổi chiều sau khi nhập hoán đổi hai mốc. Muốn nhập lại chiều dài, người dùng đổi về chiều vừa tính. Việc sửa cùng một dòng dùng lại mốc trước cắt.

Lần cắt sai chiều dài sớm nhất của cuộn được xử lý trước. Tính lại các mốc bị ảnh hưởng theo thứ tự thực hiện của cuộn và lưu toàn bộ trong giao dịch hiện có. Chiều dài đã ghi nhận ban đầu và mốc gốc vẫn có trong sổ thao tác. Kiểm tra hoàn tác đọc dữ liệu điều chỉnh có kiểm chứng để khôi phục đúng tồn.

Các dòng sai chiều dài trong phạm vi form đang sửa cần được điều chỉnh trong cùng lần lưu. Mốc của lần cắt thuộc kế hoạch khác được tính lại theo chiều dài đã ghi nhận của lần đó. Ô sửa hiển thị cặp mốc trước khi cắt làm căn cứ nhập. Ba ô lịch sử vẫn hiển thị các mốc đã lưu của lần cắt.

## Phương án đã duyệt

Áp dụng điều chỉnh vào lịch sử hiệu lực và tồn kho. Phần mềm tự tính các mốc phía sau. Đặt số cuối chỉ đọc và áp dụng quy tắc đổi chiều 3B.
