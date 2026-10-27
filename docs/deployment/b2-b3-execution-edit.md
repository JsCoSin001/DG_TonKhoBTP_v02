# Cập nhật chức năng sửa chi tiết thực hiện B2/B3

Chức năng sửa trực tiếp người thực hiện, mốc hiện tại và hoàn tác ghi nhận nhầm đã được xây dựng trong B2. B3 ghi nhật ký giao dịch bất biến và nhận biết mã thao tác đã hủy. Hai ứng dụng tiếp tục truy cập SQLite độc lập.

## Thành phần

- B2: build solution `DG_TonKhoBTP_v02.sln`, cấu hình `Release`, nền tảng `Any CPU`. Phân phối toàn bộ thư mục đầu ra, gồm thư viện SQLite và thư mục native x86/x64; giữ cấu hình đường dẫn DB của môi trường đích.
- B3: nguồn tại `integrations/B3`, phụ thuộc cố định trong `composer.lock`. Cài bằng `composer install --no-dev --optimize-autoloader` ở môi trường đích; giữ `.env`, APP_KEY và cấu hình đăng nhập của môi trường đó. Không sao chép cấu hình kiểm thử.
- SQLite: `database/Migration_B2B3_ExecutionEdit_20261026.sql`. Chỉ áp dụng thủ công có kiểm soát. B2/B3 không tự chạy migration này.
- Nếu chạy trong DB Browser for SQLite, dùng `database/Migration_B2B3_ExecutionEdit_20261026_DBBrowser.sql`. Bản này dùng `SAVEPOINT` để tương thích với giao dịch do trình biên tập mở sẵn; các bảng, dữ liệu cấu hình, chỉ mục và trigger giống bản chính.

## Trình tự cập nhật

1. Dừng mọi tác vụ ghi của cả B2 và B3; đóng các kết nối còn ghi. Sao lưu DB bằng công cụ backup SQLite hoặc sao chép khi mọi kết nối đã đóng. Nếu dùng WAL, xử lý checkpoint/backup đầy đủ trước khi sao chép; không chỉ sao chép riêng file `.db` đang mở.
2. Trên bản sao thử nghiệm, chạy toàn bộ SQL cập nhật bằng công cụ SQLite có hỗ trợ giao dịch và dừng khi lỗi. SQL chứa `BEGIN IMMEDIATE`/`COMMIT`; không bọc trong giao dịch khác. Khi lỗi phải rollback, không tiếp tục các câu còn lại.
3. Kiểm tra `PRAGMA integrity_check;` trả `ok` và `PRAGMA foreign_key_check;` không trả dòng. Kiểm tra bảng tính năng có `Ten='EXECUTION_EDIT', PhienBan=1, ChoPhepSua=0`.
4. Cập nhật cả B2 và B3 trước khi cho phép ghi trở lại. B3 mới yêu cầu schema phiên bản 1 cho các thao tác thực hiện; B3 cũ không biết nhật ký và mã HUY, không tương thích với việc sửa mới.
5. Thử ba luồng trên dữ liệu thử: cắt cuộn lẻ, chuyển chẵn thành lẻ, lấy toàn cuộn. Mỗi thao tác B3 phải có dòng `B3ThaoTac` và phiên bản dữ liệu tăng. Kiểm tra thực hiện mới vẫn hoạt động khi cờ sửa đang tắt.
6. Bật sửa trên bản thử bằng câu SQL dưới đây. Kiểm tra B2 sửa người thực hiện, mốc giữ nguyên chiều dài, hoàn tác ghi nhận nhầm, nhật ký tài khoản người sửa và B3 từ chối gửi lại mã đã HUY.
7. Khi thử đạt, lặp lại quy trình đã kiểm chứng trên DB đích trong thời gian dừng ghi. Mở lại B2/B3 sau khi cùng tương thích và bật cờ theo quyết định vận hành.

### Chạy trong DB Browser và xử lý lỗi giao dịch lồng

DB Browser có thể mở giao dịch trước khi chạy SQL, gây lỗi `cannot start a transaction within a transaction` tại `BEGIN IMMEDIATE` của bản chính. Dùng bản `_DBBrowser.sql` để tránh lồng `BEGIN`.

1. Xử lý các chỉnh sửa khác đang chờ theo ý định của người vận hành. Nếu chỉ vừa thử file cập nhật và gặp lỗi, dùng **Revert Changes** để bỏ lần thử lỗi; thao tác này cũng bỏ mọi thay đổi chưa lưu khác nếu có.
2. Trong **Execute SQL**, thay toàn bộ nội dung bằng bản `_DBBrowser.sql`, chọn tất cả và chạy.
3. Chỉ khi toàn bộ file chạy thành công, nhấn **Write Changes** nếu nút đang bật. Sau đó chạy các kiểm tra toàn vẹn/khóa ngoại/phiên bản như trên.
4. Nếu lỗi giữa chừng, không nhấn Write Changes. Quay lui phần migration bằng `ROLLBACK TO SAVEPOINT B2B3_ExecutionEdit_Migration; RELEASE SAVEPOINT B2B3_ExecutionEdit_Migration;` nếu savepoint còn tồn tại; hoặc dùng Revert Changes khi không có thay đổi khác cần giữ.

Bản chính vẫn dùng cho công cụ chạy SQL độc lập, không có giao dịch mở sẵn. Không chạy hai bản nối tiếp nhau để khắc phục cùng một lần lỗi; chọn bản phù hợp với công cụ.

```sql
UPDATE B3TinhNangDuLieu SET ChoPhepSua=1
WHERE Ten='EXECUTION_EDIT' AND PhienBan=1;
SELECT Ten,PhienBan,ChoPhepSua FROM B3TinhNangDuLieu;
SELECT Khoa,GiaTri FROM B3PhienBanDuLieu WHERE Khoa='FACTORY';
```

## Khi kiểm tra không đạt

```sql
UPDATE B3TinhNangDuLieu SET ChoPhepSua=0 WHERE Ten='EXECUTION_EDIT';
```

Cờ tắt làm B2 chỉ xem chi tiết; B3 mới tiếp tục ghi nhật ký. Giữ nguyên các bảng nhật ký/phiên bản. Không hạ B3 xuống bản cũ sau khi đã có sửa/hoàn tác. Nếu cần khôi phục backup, dừng ghi cả hai ứng dụng và khôi phục một bộ DB/phần mềm đồng nhất; mọi ghi sau thời điểm backup cần được xử lý riêng.

## Quy tắc vận hành

- Chỉ hoàn tác khi thực tế chưa cắt/chưa lấy và việc thực hiện là ghi nhận nhầm. Không dùng để ghép dây đã cắt thật hoặc điều chỉnh kiểm kê.
- Chọn đủ các lần cắt cuối cùng cần hoàn tác của cùng cuộn. Phụ thuộc từ kế hoạch khác làm yêu cầu bị từ chối; không tự hủy công việc ngoài tập đã chọn.
- Sửa mốc hiện tại giữ nguyên chiều dài tồn; lịch sử mốc lúc cắt chỉ xem. Mốc phụ thuộc lần cắt bị hoàn tác được ghi hết hiệu lực, vẫn còn nhật ký.
- Giao diện kiểm tra tác động trước khi bật nút Lưu. Nếu dữ liệu đổi, dùng “Tải mới để đối chiếu”; bản nháp vẫn còn. Dòng đã mất hiệu lực cần bỏ thay đổi rồi mở lại.
- Bản nháp kế hoạch cha được giữ. Dòng có tiến độ mới phải được đối chiếu bằng ô số lượng/chiều dài trước khi lưu kế hoạch. Với dòng chờ xóa bị ảnh hưởng, tìm lại dòng để kiểm tra và chọn xóa lại.
- Bộ dữ liệu đang phát triển phải dùng cấu trúc mới. Không có cơ chế đoán hoặc chuyển đổi lịch sử thiếu nhật ký.

## Kiểm thử

Các bài kiểm thử `tests/ExecutionEdit.*.Tests.ps1` tạo SQLite tạm, gọi assembly B2 và PHP B3 thật. Hai DB người dùng cung cấp không được ghi trong quá trình phát triển. Chi tiết kết quả và giới hạn tại `docs/verification/b2-b3-execution-edit.md`.
