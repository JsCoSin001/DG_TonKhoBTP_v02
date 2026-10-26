# Xác minh tính năng sửa chi tiết thực hiện B2/B3

Ngày kiểm thử: 26/10/2026. Nhánh: `codex/b2-b3-execution-edit`.

## Kết quả trước rà soát độc lập

- B2 Debug / Any CPU: build thành công; còn các cảnh báo sẵn có của dự án.
- 10 bộ PowerShell `ExecutionEdit.*.Tests.ps1`: đạt trên SQLite tạm. Bridge gọi PHP B3 thật và assembly C# B2 thật, không mô phỏng hai phía bằng SQL.
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
