# Kiểm thử QLSX_v02.db ngày 27/10/2026

## Kết luận

Bản Release của mã sản phẩm `558f640` đạt **47/47 kiểm tra** trên bản sao đầy đủ của `D:/Database/QLSX_v02.db`, sau khi áp dụng SQL cập nhật tính năng. Không phát hiện lỗi mới trong các luồng đã thử. Cả ba giao diện được mở từ assembly WinForms thật; PHP B3 được gọi bằng bridge kiểm thử vào cùng file SQLite sao lưu.

**File database gốc hiện chưa dùng ngay được với tính năng mới:** chưa có bảng tính năng, ledger, phiên bản và nhật ký sửa. B2 trả thông báo cần cập nhật cấu trúc; B3 mới trả `DATABASE_SCHEMA_REQUIRED`. Hai trường hợp đã được kiểm tra thực tế trên bản sao chưa cập nhật.

Chỉ bản sao tạm nhận migration, cờ bật tính năng và dữ liệu kế hoạch thử mới. Giữ toàn bộ dữ liệu, khóa ngoại, ràng buộc và trigger của bản gốc; không dựng lại DB bằng schema tối giản. Bản sao tạm đã được dọn sau kiểm thử.

## Tình trạng dữ liệu được cung cấp

| Nội dung | Kết quả |
|---|---|
| Dung lượng | 36.651.008 byte |
| Kiểm tra toàn vẹn trước và sau trên bản sao | `integrity_check = ok` |
| Kiểm tra khóa ngoại trước và sau | Không có vi phạm |
| Kế hoạch có sẵn | 1 kế hoạch ACTIVE, 2 dòng hàng, 1 quy cách, 3 nhóm, 6 đoạn |
| Thực hiện có sẵn | 4 lần cắt, 3 lần lấy nguyên |
| Nguồn và cuộn có sẵn | 2 nguồn nhập, 3 cuộn lẻ |
| Bảng mới phục vụ sửa | Chưa có trong file gốc |
| Khóa bảo trì mới nhất | Cho phép thao tác |

Bảy lần thực hiện có sẵn được ghi trước ledger mới. Sau migration, B2 đọc và hiển thị được chúng, nhưng không tự sinh thông tin nguồn gốc/ảnh trước đó để sửa hoặc hoàn tác. Đây là giới hạn dữ liệu cũ đã được duyệt trong thiết kế. Cần dùng B3 mới để các lần thực hiện tiếp theo có đủ nhật ký. Nếu muốn chỉnh sửa chính bảy lần có sẵn, cần một yêu cầu chuyển đổi dữ liệu riêng; không thể suy đoán snapshot rồi tuyên bố hoàn tác an toàn.

## Ba nghiệp vụ trên bản sao có đầy đủ ràng buộc thật

| Nghiệp vụ | Thực hiện và chỉnh sửa | Kết quả |
|---|---|---|
| Lẻ cắt lẻ | 800 − 150 = 650 m; sửa mốc sang 1.000 → 1.650 vẫn 650 m; cắt thêm 100 còn 550 m | Hoàn tác đủ hai lần về 0 → 800; thiếu lần cuối bị chặn; sửa mốc phụ thuộc hiện trong preview và hết hiệu lực có nhật ký |
| Chẵn cắt thành lẻ | Cuộn 1.000 m cắt 200 m còn cuộn lẻ 800 m | Hoàn tác từ chính dòng cuộn lẻ sinh ra thành công; gỡ QR/cuộn lẻ, gỡ liên kết nhóm, giữ cả hai đoạn kế hoạch; không cộng tăng số lượng nguồn nhập |
| Lấy toàn cuộn chẵn | Nguồn nhập 10 cuộn, lấy 1 thì tồn tính toán còn 9 | Hoàn tác về tồn 10, giải phóng QR; số lượng nhập gốc vẫn 10 |

Các bảo vệ khác đã đạt: tài khoản thực hiện ngừng hoạt động; lưu người sửa riêng; thời gian thực hiện chỉ đọc; B3 phục hồi theo người gửi gốc; mã đã HUY bị từ chối; mã mới được thực hiện; đổi mốc rồi về giá trị cũ vẫn làm phiên cũ hết hiệu lực; bản xem B2 cũ bị từ chối sau B3 ghi; phụ thuộc kế hoạch khác chặn hoàn tác; HET không được tăng tồn bằng sửa mốc; HUY chỉ xem; lỗi nhật ký làm rollback cả lịch sử/cuộn/QR/ledger; gửi lại cùng mã lần lưu không ghi trùng; bản nháp cha giữ nội dung đang nhập và nhận tiến độ mới.

Các trigger bảo vệ quan hệ quy cách của DB gốc vẫn đủ ba trigger sau thử. Dữ liệu thực hiện gốc vẫn đủ 4 lần cắt và 3 lần lấy trên bản sao; DB gốc được mở chỉ đọc để backup, không có lệnh ghi vào đường dẫn nguồn.

## B3 có thay đổi gì?

So với lần nhập nguyên ZIP tại commit `3650c29`, có đúng **4 file mã chạy B3** thay đổi:

| File | Thay đổi |
|---|---|
| `app/Services/B3/ExecutionService.php` | Mỗi lần cắt/lấy ghi ledger bất biến, mã thao tác, người gửi gốc, snapshot trước/sau; kiểm tra trùng và mã đã hủy trong giao dịch hiện có |
| `app/Services/B3/CutSessionService.php` | Đưa phiên bản tăng đơn điệu vào dấu phiên; phục hồi theo người gửi gốc thay vì người thực hiện đã sửa; từ chối phục hồi mã bị hủy |
| `app/Repositories/B3/OperationLedgerRepository.php` — mới | Kiểm tra schema, đọc/ghi ledger và từ chối mã trùng hoặc HUY, kể cả khi lịch sử hiệu lực đã được B2 gỡ |
| `app/Repositories/B3/ExecutionRevisionRepository.php` — mới | Đọc phiên bản FACTORY/cuộn để phiên cũ không hợp lệ sau khi dữ liệu đổi rồi trở lại cùng giá trị |

Có thêm/sửa các bài kiểm thử, cấu hình kiểm thử và tài liệu. Không có thay đổi ở routes, controllers, mã JavaScript, giao diện Blade hoặc bộ tính cắt/giải mã QR so với ZIP nhập ban đầu. B2 vẫn ghi SQLite độc lập, không gọi B3 để lưu sửa.

Bản nguồn B3 đã sửa nằm trong `integrations/B3`. ZIP đính kèm ban đầu giữ nguyên, SHA-256 vẫn là `07F6BF6E943AF0C1262A7528B3F85F6310571C356BB84558921A5E9F51775E9C`. Cần cập nhật nguồn B3 cùng SQL trước khi bật sửa tại B2; chưa triển khai lên một máy chủ B3 đang chạy.

## Tái hiện

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -STA -File tests/ExecutionEdit.RealDatabase.Tests.ps1 -ReadOnlyDatabasePath D:/Database/QLSX_v02.db
```

Script mở nguồn `Read Only=True`, backup vào tên tạm `ExecutionEdit.<guid>.db`, thêm dữ liệu thử với ID mới và mã duy nhất, gọi C#/PHP thật, kiểm tra hash nguồn trước/sau rồi dọn bản sao. Không sửa mã sản phẩm trong đợt xác minh này.

## Chứng cứ

SHA-256 database gốc trước và sau đều là:

`7bb58bb0c033fcb01ec4001a595d85fefaa2d058c699d9b09f76f8aa0848a666`

Kết quả máy đọc: `artifacts/execution-edit/database-validation-20261027/real-database-results.json`. Nhật ký: `run.log`; ảnh: `partial.png`, `conversion.png`, `whole.png` cùng thư mục.

### Toàn bộ 47 kiểm tra

1. Đạt — Original database backup passes integrity_check
2. Đạt — Original four cuts and three takes retained in backup
3. Đạt — Unmigrated database is refused with a controlled schema message
4. Đạt — New B3 explicitly refuses execution before shared SQL migration
5. Đạt — Migration reapplies safely and keeps editing disabled
6. Đạt — Migrated copy displays existing executed partial rows read-only
7. Đạt — Original full-reel scope displays three takes and two conversion cuts without inventing ledger identity
8. Đạt — Actual detail form safely shows existing history with editing disabled
9. Đạt — B3 cuts existing partial with edit feature still disabled
10. Đạt — B2 reads actual B3 cut 800 minus 150 equals 650
11. Đạt — Pre-ledger history is not guessed or rewritten when editing is enabled
12. Đạt — B2 corrects performer to inactive account without altering time
13. Đạt — Editor is audited separately from immutable original B3 requester
14. Đạt — B3 recovers original request after performer correction
15. Đạt — B2 marker end 1650 preserves 650m
16. Đạt — B2 marker end 650 preserves 650m
17. Đạt — ABA marker change invalidates old B3 session
18. Đạt — Actual UI binds selected partial execution
19. Đạt — B3 cuts second segment using current data
20. Đạt — Parent refresh preserves draft and updates real progress to 550m
21. Đạt — Undo refuses an older cut without selected dependent later cut
22. Đạt — Undo previews exact dependent marker corrections
23. Đạt — Selected suffix undo restores original 0 to 800 markers
24. Đạt — B3 refuses cancelled operation token after B2 undo
25. Đạt — B3 accepts a genuinely new operation after undo
26. Đạt — B3 can exhaust exactly the remaining 650m
27. Đạt — Marker editing cannot create stock on HET reel
28. Đạt — Mistaken exhausted cut can be undone with proper state restoration
29. Đạt — B3 converts whole reel into partial with original database constraints and triggers
30. Đạt — Actual UI binds selected conversion execution
31. Đạt — Audit failure rolls back conversion undo including QR and ledger
32. Đạt — Conversion undo via generated partial commits and preserves both planned segments
33. Đạt — Conversion undo releases QR without incrementing imported quantity
34. Đạt — B3 takes a whole reel with explicit specification and derived stock becomes nine
35. Đạt — Actual UI binds selected whole execution
36. Đạt — Whole take undo restores derived stock to ten and QR without changing imports
37. Đạt — Committed ChangeId replay never duplicates effects or audit
38. Đạt — Independent B3 write makes stale B2 snapshot fail safely
39. Đạt — Another plan can cut its reserved segment on the shared reel
40. Đạt — Undo blocks unselected later execution from another plan
41. Đạt — HUY plan remains read-only at the database boundary
42. Đạt — Cancelled operations remain visible in reel audit
43. Đạt — All original execution rows retained in test copy
44. Đạt — Full copied database still passes foreign_key_check after all operations
45. Đạt — Full copied database still passes integrity_check after all operations
46. Đạt — All original specification-parent protection triggers remain present
47. Đạt — Original database SHA256 unchanged
