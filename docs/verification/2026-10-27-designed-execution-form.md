# Kiểm tra chuyển sang form chi tiết thiết kế sẵn

Ngày: 27/10/2026. Phạm vi: chuyển giao diện chi tiết thực hiện từ form dựng bằng code sang `Frm_ChiTietThucHienKeHoach`.

## Hành vi đã chuyển

- Nút Chi tiết ở form lập kế hoạch mở form mới; giữ truy vấn, đọc dữ liệu mới nhất và refresh dữ kiện sau lưu mà không tự lưu bản nháp cha.
- Bảng giữ sáu cột, chọn dòng để xem người, thời gian, QR, nguồn và ba mốc lịch sử; trạng thái được sửa theo nghiệp vụ cũ.
- Sửa người, mốc cuộn hiện tại và hoàn tác vẫn qua `ExecutionEditDraft` / `ExecutionEdit_DB`, giữ giao dịch, kiểm tra phiên bản, phụ thuộc, QR và nhật ký bất biến.
- `rtbLyDoSua` dùng nhập lý do chung cho một lần lưu. Mỗi sự kiện giữ mã INFO/MISTAKE/MARKERS tại LyDo và văn bản nguyên gốc tại GhiChu qua Note; tài khoản người sửa giữ riêng.
- `lblTacDong` là Label động hiển thị lỗi theo giải thích cuối cùng của người dùng. Không cần RichTextBox và không có hai nút bổ sung hay vùng thông tin tổng hợp.
- Đóng không lưu thay nút bỏ bản nháp. Hộp thoại xung đột hỏi tải mới, giữ bản nháp; không tự lưu sau tải mới. Sửa mốc / hoàn tác có xác nhận tác động trước khi ghi.
- Có thể bấm Lưu khi bản nháp chưa hợp lệ để xem lỗi, nhưng không ghi database khi kiểm tra thất bại.

## Kiểm thử đã thực hiện

- Debug build: đạt; Release build: đạt, còn các cảnh báo có sẵn của dự án.
- Toàn bộ 14 script `ExecutionEdit.*.Tests.ps1`: 13 script đạt, 1 script kiểm tra database thật bỏ qua vì không truyền đường dẫn. Tất cả dữ liệu kiểm thử là fixture SQLite tạm.
- Bộ mới `ExecutionEdit.DesignedForm.Tests.ps1`: 12 trường hợp đạt: binding/nhãn tĩnh, đầu vào số, lưu người và lý do, từ chối xác nhận/xung đột trong lúc xác nhận, lưu mốc, tải lại giữ bản nháp, tải lại mốc trùng với dữ liệu người khác đã lưu, hoàn tác lẻ, hoàn tác chuyển chẵn thành lẻ từ phạm vi cuộn phát sinh, hoàn tác lấy nguyên cuộn, đóng và lỗi nhật ký rollback.
- Bộ UI/review/preview đã chuyển sang kiểm tra đúng form mới, không giữ kiểm tra trên form cũ không còn sử dụng.
- Render và xem ba cửa sổ WinForms thật: `artifacts/execution-edit/partial.png`, `convert.png`, `whole.png`. Label và bố cục giữ theo thiết kế người dùng; các ô mốc không áp dụng được ẩn khi lấy nguyên cuộn.
- RED trước triển khai: form mới không có constructor nhận truy vấn / binding, test Binding thất bại đúng điều kiện này. GREEN sau triển khai kiểm tra cả ghi dữ liệu qua dịch vụ thật.

Log cục bộ: `artifacts/execution-edit/designed-ExecutionEdit.*.Tests.ps1.log`, `designed-form-build.log`, `designed-form-release-build.log`.

## Phần được giữ nguyên

- Không sửa lớp database, B3 hoặc SQL; không mở/chỉnh sửa database người dùng trong đợt chuyển form này.
- Không sửa Designer hoặc resx sau khi người dùng lưu thiết kế. SHA256 trước/sau:
  - Designer: `C85201A8EEC46EC64727F23401BF032653D511D5770ADE25AC2EC36DEEEAB49E`.
  - resx: `4363CD7D5B8671C72442CE1A1BFC10D64EBD24B2D718B54BD4FCD025E4967298`.
- Các Label tĩnh được so sánh nội dung trước/sau binding; chỉ `lblTacDong` được cập nhật lỗi. Nội dung mẫu của nhãn thiết kế cũng giữ nguyên theo yêu cầu.

## Quyết định triển khai

- Giữ checkout/nhánh hiện tại theo lựa chọn trước đó của người dùng; không tạo worktree hoặc reset các file Designer vừa bổ sung.
- Tách logic nối giao diện vào partial `.Execution.cs` để giữ nguyên file constructor / Designer của người dùng; csproj thêm đúng một file code.
- Lý do là văn bản chung của lần lưu. Nếu sau này cần lời giải thích khác nhau cho từng dòng phải bổ sung giao diện tương ứng; không suy đoán lý do từ đoạn văn nhập.
- Điểm mở form chỉ đổi kiểu cửa sổ; xác minh bằng diff và bộ kiểm thử form thật, không thêm test soi chuỗi mã nguồn.
- Label báo lỗi giữ hàng thiết kế; nội dung dài hiển thị dấu ba chấm và có thể xem bằng tooltip tiêu chuẩn. Các tác động quan trọng hiển thị trong xác nhận trước lưu, không đưa vào Label lỗi.

## Giới hạn kiểm tra

Chưa chạy lại kiểm tra với database thật trong đợt này. Bộ kiểm tra rộng hơn chạy 9 script: 5 đạt, 2 bỏ qua do không truyền database thật, 2 script có lỗi thuộc giờ ca. Mã nguồn và test giờ ca không có thay đổi trong đợt này. Báo cáo nền trước đó nằm trong `docs/verification/b2-b3-execution-edit.md`; kết quả hiện tại là:

- `UC_TTCaLamViec.Ending.Tests.ps1`: 19 đạt, 13 lỗi. Ngoài 12 tên đã ghi trong báo cáo nền, lần chạy này còn lỗi `Native focus returns to invalid ending time with only one alert` (mong đợi 1 thông báo, thực tế 0). Không coi kiểm tra này đã đạt hoặc đã sửa.
- `UC_TTCaLamViec.StartDate.Tests.ps1`: 20 đạt, 1 lỗi `New form has no shift, today as start date, and a disabled date input`.
- Hai script bỏ qua: `Inventory.SaveFlow.Tests.ps1`, `LapKeHoachCatDay.NguoiCat.Tests.ps1`.

Danh sách đầy đủ 13 lỗi Ending hiện tại:

1. later date ignores lower shift and earlier hour
2. same date higher shift ignores clock order
3. shift 3 23 to midnight
4. shift 3 midnight to 01
5. shift 3 evening to exact 06
6. shift 2 crosses midnight
7. identical tuple allowed
8. missing end shift skips unavailable comparisons
9. missing end hour skips hour comparison
10. Existing save requirements remain when all ending fields are missing
11. Selecting an end shift retains invalid time, alerts and requires correction
12. Native focus returns to invalid ending time with only one alert
13. End date defaults follow end shift/time and manual changes survive until reset

## Rà soát độc lập và sửa sau rà soát

Một reviewer độc lập đọc range `6900707..87c6101`, xác nhận việc giữ dữ liệu lịch sử / hiện tại riêng, kiểm tra số trước ghi, đóng băng thao tác lúc lưu và gọi nguyên dịch vụ giao dịch cũ. Không phát hiện lỗi Critical/Important theo phân loại ban đầu; có hai điểm được tác giả đánh giá cần sửa trước khi kết thúc vì ảnh hưởng luồng lưu và bằng chứng kiểm tra:

1. Nếu người khác đã lưu cùng số cuối / chiều, tải mới phải loại cả bản nháp số lẫn đầu vào văn bản trùng. Trước sửa: nút Lưu còn bật, báo chưa có thay đổi và đóng vẫn hỏi lưu. Test `IdenticalReload` tái hiện RED; sau sửa GREEN, các đầu vào sai hoặc thực sự khác vẫn giữ nguyên.
2. Callback test xác nhận phải trả câu trả lời chụp trước khi chạy hành động mô phỏng dữ liệu đổi. Trước sửa test vô tình từ chối xác nhận, không tới giao dịch. Thêm kiểm tra số hộp thoại để chứng minh RED; sau sửa `MarkerConfirm` GREEN, thực sự đồng ý xác nhận, đi vào kiểm tra phiên bản và hiện hộp thoại xung đột. Dịch vụ lưu không thay đổi.

Sau một lượt sửa: build Debug/Release đạt; chạy lại toàn bộ 14 script ExecutionEdit: 13 đạt, 1 bỏ qua database thật; 12/12 trường hợp của bộ form mới đạt. Không chạy thêm reviewer thứ hai. Không còn finding trong phạm vi chuyển form bị hoãn.

Reviewer đặt ngoài phạm vi: triển khai B2/B3 trước range này; nhãn mẫu / bố cục / khoảng trắng Designer theo yêu cầu người dùng; lỗi giờ ca không liên quan; database gốc; xác minh lại bằng chạy test độc lập; các checkbox kế hoạch đang được tác giả cập nhật. Tác giả đã chạy test, ghi đầy đủ lỗi ngoài phạm vi và giữ nguyên database gốc. Nhật ký quyết định đầy đủ được lưu trong `2026-10-27-designed-execution-form-decisions.md`.
