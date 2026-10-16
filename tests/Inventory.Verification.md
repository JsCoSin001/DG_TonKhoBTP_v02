# Kết quả kiểm thử nghiệp vụ tồn NVL và thành phẩm

Ngày: 16/10/2026. Phạm vi: các quyết định đã thống nhất cho tạo mới, edit và sao chép.

## Kết quả

| Hạng mục | Debug / Any CPU | Release / Any CPU |
|---|---:|---:|
| Rebuild toàn bộ solution | 0 lỗi, 1.189 cảnh báo | 0 lỗi, 1.189 cảnh báo |
| Inventory.NewEdit.Tests.ps1 | 34 đạt | 34 đạt |
| Inventory.SaveFlow.Tests.ps1 | 17 đạt | 17 đạt |
| UC_BaoCaoDungMay.ComboBox.Tests.ps1 | 4 đạt | 4 đạt |
| UC_TTCaLamViec.Ending.Tests.ps1 | 32 đạt | 32 đạt |
| UC_TTCaLamViec.ShiftHours.Tests.ps1 | 50 đạt | 50 đạt |
| UC_TTCaLamViec.StartDate.Tests.ps1 | 16 đạt | 16 đạt |
| **Tổng kiểm thử** | **153 đạt, 0 thất bại** | **153 đạt, 0 thất bại** |

Tổng cộng 306 lượt chạy cho 153 ca trên hai cấu hình. Số cảnh báo biên dịch bằng mức đã có trước thay đổi nghiệp vụ; lượt kiểm thử này không xử lý các cảnh báo đó.

## Đối chiếu quyết định

| Quyết định | Bằng chứng kiểm thử | Kết quả |
|---|---|---|
| Tạo mới tính từ mốc bắt đầu cố định | Đổi CD 200 → 400 → 200; kiểm tra KL ở kéo rút và chuyển đổi khác đơn vị | Đạt |
| Chưa nhập/xoá trắng được tính là 0 | Kiểm tra NumericUpDown ở tạo mới, edit, sao chép; NVL quay về mốc bắt đầu khi thiếu số bắt buộc | Đạt |
| Giữ điều kiện số lượng bắt buộc khi lưu | M cần CD > 0; KG cần KL > 0; trường còn lại được phép bằng 0 | Đạt |
| Xoá dòng không gây trừ lặp | Xoá A và kiểm tra B vẫn chỉ bị tính một lần tiêu hao | Đạt |
| Mở edit giữ dữ liệu lịch sử | Giữ nguyên 398 → 120 khi mở; thêm B không tự thay đổi lịch sử A | Đạt |
| Bin cũ xoá rồi thêm lại vẫn là bin cũ | Khôi phục mốc lịch sử, kể cả khi nguồn hiện đã hết tồn | Đạt |
| Nguồn đã dùng tiếp giữ tồn cuối và LastEdit_id | Lưu không đổi, sửa số còn lại, xoá dòng cũ; kiểm tra cả KL và CD | Đạt |
| Nguồn chưa dùng tiếp được hoàn/cập nhật tồn | Sửa lần sử dụng cuối, xoá lần sử dụng cuối, lưu lịch sử và tồn cùng giao dịch | Đạt |
| NVL thực sự thêm mới là lần sử dụng mới | Cùng phiếu giữ tồn A cũ nhưng cập nhật tồn B mới | Đạt |
| Tồn của NVL thêm mới thay đổi trước khi lưu | Chặn lưu và rollback thành phẩm, lịch sử sửa, danh sách NVL | Đạt |
| Thành phẩm đã được dùng tiếp giữ tồn hiện tại | Sửa sản lượng ban đầu, bảo toàn tồn hiện tại và dấu lần sử dụng cuối | Đạt |
| Kiểm tra trạng thái ngay lúc lưu | Thêm lần sử dụng sau khi đã tạo snapshot form, rồi xác nhận tồn mới vẫn được bảo toàn | Đạt |
| Khoá đổi LOT/Mã bin đã sử dụng, vẫn cho sửa số lượng | Kiểm tra control, thay đổi máy bằng code và chặn đổi mã tại giao dịch lưu; thành phẩm chưa dùng vẫn đổi mã được | Đạt |
| Công đoạn 9 giữ chính sách riêng | Bảo toàn tồn thành phẩm đã sử dụng, chặn đổi mã và không thay đổi TTNVL | Đạt |
| Sao chép giữ hành vi hiện tại | Máy không cho tái sử dụng có bảng NVL trống; máy cho phép dùng snapshot tồn hiện tại; lưu thành phiếu mới | Đạt |
| Giữ nguyên công thức công đoạn | Đối chiếu nguyên văn 10 hàm công thức, điều phối và điều kiện tính với HEAD; kiểm tra kết quả CD/KL | Đạt |

## Database và giới hạn

- Nguồn kiểm thử: `D:\Database\QLSX_v02.db`, luôn mở chỉ đọc.
- Các hàm cập nhật được chạy trên SQLite trong bộ nhớ hoặc file clone tạm; file clone được xoá sau kiểm thử.
- Ca thực tế: sửa phiếu 56771 vẫn giữ tồn nguồn ID 56073 ở 8.930 m và LastEdit_id = 56799.
- SHA-256 database nguồn giống nhau trước và sau lượt kiểm tra cuối: `33D28E3A06EF0610704B28DA49841244C46036C0DAB4D6C5905CEFAE23DB3E2E`.
- Các control WinForms và hàm giao dịch lưu thật đã được gọi bằng code. Chưa thao tác thủ công toàn bộ cửa sổ ứng dụng, hộp xác nhận tổ trưởng và in tem.
- Lượt kiểm thử này chỉ bổ sung test; không sửa thêm code nghiệp vụ.

## Chạy lại

Dùng Windows PowerShell ở chế độ STA sau khi build:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -STA -File tests/Inventory.NewEdit.Tests.ps1 -ReadOnlyDatabasePath D:/Database/QLSX_v02.db
powershell -NoProfile -ExecutionPolicy Bypass -STA -File tests/Inventory.SaveFlow.Tests.ps1 -ReadOnlyDatabasePath D:/Database/QLSX_v02.db
```

Để kiểm thử Release, thêm `-AssemblyPath DG_TonKhoBTP_v02/bin/Release/DG_TonKhoBTP_v02.exe`. Các ca dùng database thực tế yêu cầu đúng bộ dữ liệu có những ID nêu trên. Không truyền đường dẫn database thì bộ NewEdit bỏ qua ca snapshot thực tế, còn bộ SaveFlow báo SKIP.
