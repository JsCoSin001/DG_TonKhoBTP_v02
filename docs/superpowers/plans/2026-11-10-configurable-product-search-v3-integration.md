# Tích hợp tìm kiếm thành phẩm theo công đoạn V3

Dùng Superpowers executing-plans và test-driven-development. Nhánh thực hiện là codex/configurable-product-search-v3, nền 20ff1a9. Phạm vi là các hành vi V3 đã được duyệt trong cuộc trao đổi, lấy thiết kế và SQL từ gói Goi_cap_nhat_CongDoan_TimKiem_V3_chi_file_thay_doi.zip.

## Phạm vi

- Màn hình UC_TTThanhPham chuyển từ mẫu mã C# sang hai bảng SQLite theo mã công đoạn. Giữ tải BOM và xử lý sau khi chọn.
- ReportHelper lọc theo công đoạn đã lưu. Giữ điểm vào báo cáo và đường tính tồn kho hiện tại.
- Thêm duy nhất mục Compile của CongDoanTimKiem_DB.cs vào project. Giữ đăng ký các form và đường dẫn nhập kho của nhánh.
- Kiểm thử SQL trên dữ liệu cô lập. Nhánh chứa tệp nâng cấp, không chứa database tham khảo.

## Thực hiện

- [x] Tái hiện trường hợp gợi ý cũ còn chọn được bằng fixture WinForms. Chạy ca SQL của gói trước khi thêm migration để xác minh test phát hiện phần thiếu.
- [x] Thêm migration và truy vấn đọc theo MaCongDoan, ngoại lệ theo ID và báo cấu hình thiếu. Gắn ô tìm thành phẩm, hủy tác vụ cũ và xóa gợi ý ngay khi sửa từ khóa hoặc xóa ô.
- [x] Đổi điều kiện ReportHelper và loại các mẫu cứng đã hết chỗ dùng. Đối chiếu trước/sau với dữ liệu thử có mã sản phẩm và công đoạn lưu khác nhau.
- [x] Build Any CPU và x64. Chạy 6 ca SQLite của gói, kiểm thử C#/WinForms, 55 lượt đối chiếu danh sách mặc định trên bản sao chỉ đọc nguồn, và bộ hồi quy tìm kiếm tồn kho.
- [x] Rà soát nhánh, kiểm tra diff, commit các tệp có chủ đích và xác nhận thư mục nguồn cũ.

## Kiểm chứng

- Build Debug Any CPU và x64 thành công.
- 8/8 ca SQLite, 3/3 ca C#/WinForms V3, 14/14 ca tìm kiếm tồn kho và 4/4 ca báo cáo dừng máy.
- 55/55 tập kết quả khớp với tìm kiếm cũ khi nạp 22 mẫu mặc định. Bản sao kiểm thử toàn vẹn và bảng ngoại lệ có 0 dòng.
- Bộ kiểm thử lưu tồn B1 có 6 đạt, 12 lỗi trùng bản nền.
- Rà soát đã xác minh ca công đoạn bị tháo khỏi màn hình và ca migration gặp danh mục thiếu hoặc trùng mã.

## Thử trên máy

Chạy tệp SQL trong database/migrations trên bản sao của database hiện hành bằng công cụ SQLite dừng khi gặp lỗi. Trỏ ứng dụng nhánh V3 tới bản sao đã nâng cấp rồi thử ô tìm thành phẩm tại Ghép lõi. Kết quả tìm theo 22 mẫu ban đầu được đối chiếu trước khi thêm ngoại lệ.
