# Xác minh tính năng sửa chi tiết thực hiện B2/B3

Ngày kiểm thử: 26/10/2026. Nhánh: `codex/b2-b3-execution-edit`.

## Kết quả cuối sau rà soát độc lập

- B2 Debug / Any CPU: build thành công; còn các cảnh báo sẵn có của dự án.
- 11 bộ PowerShell `ExecutionEdit.*.Tests.ps1`: đạt trên SQLite tạm, gồm bảy ca tái hiện từ rà soát độc lập. Bridge gọi PHP B3 thật và assembly C# B2 thật, không mô phỏng hai phía bằng SQL.
- Hai hồi quy `LapKeHoachCatDay.ChiTiet.Tests.ps1` và `LapKeHoachCatDay.NguoiCat.Tests.ps1`: đạt. Bài người cắt chỉ đọc DB được cung cấp để tạo bản sao tạm.
- B3 Unit: 29 đạt, 121 phép kiểm tra. B3 Feature lọc B3: 161 đạt, 1.048 phép kiểm tra.
- `git diff --check`: đạt.
- Ảnh ba trường hợp được dựng từ WinForms thật, kiểm tra trường dữ liệu và bố cục.

Các ca B3 cũ ban đầu có 26 lỗi: QR thiếu chiều dài, MaBin chưa theo thành phần được phân tách, thiếu quy cách, còn kỳ vọng lấy cuộn từ bin không khớp hoặc tái dùng QR vật lý. Đã cập nhật fixture và kỳ vọng theo mã nguồn B3 gốc hiện hành, giữ kiểm tra tồn/giữ chỗ/rollback/định danh; không đổi quy tắc QR/nguồn của sản phẩm để làm kiểm thử đạt. Một kỳ vọng phiên cắt đổi từ `STALE_DATA` sang `SOURCE_STATE_CHANGED` vì phiên bản đơn điệu phát hiện thay đổi sớm hơn.

## Đối chiếu 13 tiêu chí được duyệt

| Tiêu chí | Bằng chứng chính |
|---|---|
| 1. Sửa người, giữ thời gian, lưu người sửa riêng | Corrections, UI; tài khoản ngừng hoạt động vẫn chọn được |
| 2. Mốc 7C theo cuộn, lịch sử không đổi, tồn không đổi | Read, Corrections, UI |
| 3. Hoàn tác cắt lẻ về mốc gốc | UndoPlan, Undo; B3 ghi snapshot trước khi đổi mốc |
| 4. Chuỗi hoàn tác và phụ thuộc kế hoạch khác | UndoPlan; thiếu đoạn cuối bị chặn |
| 5. Hoàn tác chuyển chẵn thành lẻ | Undo; gỡ QR/cuộn sinh, giữ nhóm/đoạn kế hoạch |
| 6. Hoàn tác lấy nguyên | Undo; không sửa số lượng nguồn nhập |
| 7. Mã HUY, mã mới, người gửi gốc | CrossApp, OperationLedgerTest |
| 8. Đồng thời, phiên cũ, đổi rồi về lại, gửi lại sau commit | CrossApp, Schema, Corrections; PHP giữ khóa ghi SQLite thật trên kết nối khác |
| 9. Mốc độc lập, cả từ kế hoạch khác | UndoPlan, CrossApp, Integrity; sự kiện cũ còn, thêm sự kiện hết hiệu lực |
| 10. Lỗi một phần quay lui toàn bộ | Corrections, Undo; trigger gây lỗi nhật ký kiểm tra rollback |
| 11. Giữ bản nháp và phát hiện xung đột lưu kế hoạch | ParentDraft; giữ header, chiều dài, chờ xóa và dấu vết bản gốc |
| 12. ACTIVE hoàn thành, HUY, HET, chưa thực hiện | Integrity, UI; kiểm tra cả lớp dữ liệu |
| 13. UI và hồi quy | Preview, UI, hai hồi quy B2, toàn bộ Unit/Feature B3 |

Năm trọng tâm rà soát trong kế hoạch lần lượt có bằng chứng CrossApp (1–3), Integrity/UndoPlan/UI (4), ParentDraft (5).

## Giới hạn và lựa chọn kỹ thuật

- Chưa triển khai lên máy chạy thật, chưa bật cờ sửa hoặc migration trên DB người dùng.
- Đối chiếu bằng phiên bản FACTORY để chặn cả phụ thuộc cuộn giữa kế hoạch; một thay đổi không liên quan cũng có thể yêu cầu tải mới.
- Dữ liệu cũ thiếu nhật ký không được suy đoán; đây là phạm vi phát triển đã được duyệt.
- Việc hoàn tác chỉ đúng khi thao tác được ghi nhận nhầm và thực tế chưa cắt/chưa lấy; hệ thống kiểm tra phụ thuộc dữ liệu, không xác minh vật lý thay người dùng.
- Kiểm thử mất xác nhận sau commit dùng việc gửi lại cùng mã lần lưu; không mô phỏng mất điện/phần cứng của môi trường đích.

Hướng dẫn cập nhật có kiểm soát tại `../deployment/b2-b3-execution-edit.md`.

## Rà soát độc lập và sửa phát hiện

Rà soát trên nhánh từ `5a4131c` đến `38da659`, tách thay đổi B3 khỏi lần nhập nguồn `3650c29`. Đánh giá: cần sửa sáu vấn đề quan trọng. Đã sửa một lượt và chạy lại toàn bộ các bộ kiểm thử liên quan; không có lượt rà soát trùng lặp.

| Phát hiện | Kết quả sửa / bằng chứng |
|---|---|
| Hoàn tác chuyển chẵn thành lẻ bị rollback khi mở qua dòng cuộn lẻ sinh ra | Xử lý rõ phạm vi bị xóa có chủ đích; lưu thành công, form cũ đóng. Ca Conversion thất bại trên bản trước sửa, đạt trên bản sau sửa |
| Mất nội dung đang nhập của dòng chờ xóa khi cập nhật tiến độ | Chỉ cập nhật facts, giữ số lượng, chuỗi chiều dài, nhóm đang nhập và bản gốc. Ca PendingDelete thất bại → đạt |
| Bản nháp cuộn vừa hoàn tác làm kẹt lưu kế hoạch | Giữ dòng như bản nháp cục bộ; nút Xóa chỉ bỏ dòng đó và cờ đối chiếu, giữ bản nháp khác. Ca Orphan thất bại → đạt |
| Bảng tác động chỉ đếm sửa mốc và biến mất khi đổi dòng | Liệt kê từng cuộn, mốc trước/sau, kế hoạch, người sửa, thời gian; giữ bảng tác động cả lô khi chọn dòng khác. MarkerPreview và PreviewSelection thất bại → đạt |
| Nhật ký sau hoàn tác không hiện do dòng đang chờ không còn mã thực hiện | Tra mã lịch sử qua ledger bất biến, gồm mã HUY và sửa mốc từ kế hoạch khác. AuditView thất bại → đạt |
| Thiếu phiên bản trước/sau và tác động đầy đủ trong nhật ký | Bảng bất biến `B2DieuChinhKetQua` lưu phiên bản yêu cầu/trước/sau, ảnh trước/sau của lịch sử, cuộn, nhóm, QR, nguồn nhập và ledger; ghi cùng transaction. AuditEffects thất bại → đạt; lỗi ghi kết quả cuối làm rollback toàn bộ |

Đã chạy lại cả bảy ca trên bản Release trước sửa để xác nhận chúng thực sự thất bại, sau đó chạy đạt trên bản mới. Bộ kiểm thử không còn phụ thuộc cấu hình PHP trong thư mục ghi chép tạm; mỗi fixture tự tạo và dọn cấu hình riêng.

**Điểm nhỏ chưa làm:** chưa có ô ghi chú tùy chọn trên form cho từng dòng/cuộn. Lý do bắt buộc, tài khoản người sửa và nhật ký vẫn có; trường ghi chú đã được hỗ trợ ở lớp dữ liệu. Đây là phần cải thiện giao diện được hoãn, không phải lỗi tồn kho.

## Các quyết định trong quá trình thực hiện

1. Dùng PowerShell/Python thay tập lệnh Git Bash vì môi trường sandbox không khởi tạo được namespace Windows; giữ nguyên thứ tự công việc và chứng cứ kiểm thử. Rủi ro nếu sai: sai ghi chép quy trình, không đổi nghiệp vụ.
2. Sửa tham số build solution thành `Any CPU`; project vẫn là `AnyCPU`. Rủi ro nếu sai: build sai cấu hình; đã xác nhận Debug và Release.
3. Cho phép chạy script kiểm thử với `ExecutionPolicy Bypass` chỉ trong tiến trình; cấu hình PHP riêng trỏ tới extension đã cài, không đổi cấu hình máy. Rủi ro: cần PHP/extension có sẵn để chạy lại kiểm thử.
4. Giữ quy tắc B3 hiện hành về QR, quy cách và nguồn quét; sửa hai fixture lấy cuộn đã lỗi thời. Rủi ro: không còn kiểm tra kỳ vọng cũ lấy từ bin không khớp; thay bằng ca từ chối.
5. Tạo thư mục cache Laravel và APP_KEY chỉ cho kiểm thử; không nhập `.env` môi trường chạy. Rủi ro: gói nguồn cần được cấu hình đúng khi triển khai.
6. Gom ca phục hồi vào OperationLedgerTest để dùng cùng một fixture, không tạo lớp kiểm thử trùng. Rủi ro: tên lớp khác kế hoạch, phạm vi kiểm tra giữ nguyên.
7. Dùng JavaScriptSerializer sẵn trong .NET Framework thay vì thêm thư viện JSON. Rủi ro: giới hạn 8 MB cho bản ghi JSON; nếu quá giới hạn, giao dịch bị từ chối và rollback.
8. Đưa bridge PHP kiểm thử lên sớm để mọi bài B2 dùng dữ liệu do B3 thật ghi. Rủi ro: kiểm thử B2 tích hợp cần runtime PHP.
9. Dùng phiên bản FACTORY để bảo vệ cả phụ thuộc cuộn giữa kế hoạch. Rủi ro: thay đổi không liên quan cũng có thể yêu cầu tải mới; không chấp nhận dữ liệu tồn cũ.
10. Dòng bản nháp có facts thay đổi cần đối chiếu lại qua ô số lượng/chiều dài; dòng chờ xóa bị ảnh hưởng cần tìm lại và chọn xóa lại. Rủi ro: thêm thao tác kiểm tra khi thật sự có xung đột.
11. Chuẩn hóa thêm fixture B3 cũ về chiều dài QR, MaBin được phân tách, quy cách rõ ràng và QR vật lý khác nhau. Rủi ro: bỏ kỳ vọng lỗi thời; giữ kiểm tra số tồn, giữ chỗ, rollback, định danh và từ chối nguồn không phù hợp.
12. Chỉ bật Lưu sau khi bảng tác động mới nhất hợp lệ; bổ sung tải mới để đối chiếu và giữ bản nháp. Rủi ro: chờ kiểm tra ngắn; cần tải lại khi xung đột.
13. Giữ các lỗi ở ba bộ hồi quy ngoài phạm vi vì đã xác nhận giống hệt trên mã gốc. Rủi ro: các nghiệp vụ đó vẫn có lỗi kiểm thử được liệt kê bên dưới, không tuyên bố toàn dự án B2 đều đạt.
14. Dùng bản ghi kết quả bất biến riêng, ghi cuối cùng trong cùng giao dịch, để lưu phiên bản sau và toàn bộ tác động. Rủi ro: cần áp dụng đầy đủ file SQL mới; thiếu bảng sẽ bị từ chối, không ghi một phần.
15. Mã B3 được nhập nguyên trạng chỉ được rà soát ở phần tích hợp; dữ liệu lịch sử thiếu ledger không thuộc phạm vi, đúng quyết định không có dữ liệu cũ. Rủi ro: lỗi ở phần B3 nguyên trạng hoặc một bộ dữ liệu cũ chưa được chuyển đổi cần xử lý riêng.
16. Việc thực tế chưa cắt/chưa lấy là xác nhận nghiệp vụ của người dùng; không thể suy ra từ SQLite. Rủi ro: chọn lý do sai với thực tế vẫn có thể làm số liệu khác vật lý.
17. Chỉ thử sự cố trên dữ liệu tạm và gửi lại mã sau commit; không triển khai hoặc gây lỗi điện/mạng trên hệ thống thật. Rủi ro: cần kiểm tra vận hành tại môi trường đích theo hướng dẫn cập nhật.

## Hồi quy mở rộng ngoài phạm vi tính năng

Đã build độc lập mã gốc `5a4131cb2f94b3b62417b6a7928b180d2f98ce51` trong thư mục tạm, không đổi HEAD. Ba bộ dưới đây có cùng 25 ca lỗi trên cả mã gốc và nhánh mới; tên lỗi và kết quả trùng nhau. Năm bộ B2 còn lại đạt. Chưa sửa các nghiệp vụ tồn kho/ca làm việc này vì không thuộc yêu cầu sửa chi tiết B3. Vì vậy không tuyên bố toàn bộ kiểm thử dự án B2 đều đạt.

### Inventory.SaveFlow — 6 đạt, 12 lỗi

- Unchanged edit commits history and preserves later source stock
- Historical remaining amount changes while later physical stock stays unchanged
- Deleting an old material commits row deletion without restoring later stock
- Editing the latest material use updates its source and history together
- Deleting the latest material use restores its historical start
- A protected original bin and a newly added bin save independently
- Stale new-source stock rolls back product, audit and material changes
- A consumed finished product preserves current stock when original output changes
- An unused finished product can be renamed and quantity edited
- New/copy save commits a new product and consumes current source stock (verifyInventory=False)
- New/copy save commits a new product and consumes current source stock (verifyInventory=True)
- Direct new/copy save rejects changed inventory without creating a product

### UC_TTCaLamViec.Ending — 20 đạt, 12 lỗi

- later date ignores lower shift and earlier hour
- same date higher shift ignores clock order
- shift 3 23 to midnight
- shift 3 midnight to 01
- shift 3 evening to exact 06
- shift 2 crosses midnight
- identical tuple allowed
- missing end shift skips unavailable comparisons
- missing end hour skips hour comparison
- Existing save requirements remain when all ending fields are missing
- Selecting an end shift retains invalid time, alerts and requires correction
- End date defaults follow end shift/time and manual changes survive until reset

### UC_TTCaLamViec.StartDate — 20 đạt, 1 lỗi

- New form has no shift, today as start date, and a disabled date input
