# B2/B3 Execution Edit Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Sửa trực tiếp người thực hiện, mốc cuộn hiện tại và hoàn tác ghi nhận nhầm của B3 tại B2, giữ đúng tồn/đặt trước/QR/nhật ký.

**Architecture:** B2 C# và B3 PHP ghi SQLite độc lập; không gọi dịch vụ của nhau. Giữ bảng thực hiện có hiệu lực để các phép tính tồn hiện tại tiếp tục đúng; bổ sung sổ mã thao tác, nhật ký sửa và phiên bản đơn điệu. Một lần lưu B2 cập nhật tất cả hoặc không cập nhật gì.

**Tech Stack:** C# 8.0, .NET Framework 4.8 WinForms, System.Data.SQLite 1.0.119; PHP >=8.2, Laravel 12, PHPUnit 11; Windows PowerShell kiểm thử theo mẫu hiện có.

**Spec:** `docs/superpowers/specs/2026-10-26-b2-b3-execution-edit-design.md` — người dùng đã duyệt ngày 26/10/2026. Đọc cả tài liệu đó trước khi triển khai.

## Global Constraints

- B2 và B3 hoạt động độc lập, cùng truy cập SQLite. B2 không gọi máy chủ B3 để lưu.
- Chỉ sửa dữ liệu B3 đã ghi nhận. Không ghi nhận bổ sung Chưa thực hiện → Đã thực hiện tại B2 trong đợt này.
- Ai đã mở được form chi tiết đều được sửa; không bổ sung quyền riêng.
- Cho sửa kế hoạch còn hiệu lực, kể cả đã hoàn thành. Kế hoạch HUY chỉ xem.
- Thời gian thực hiện chỉ xem, không được sửa.
- Chọn người thực hiện trong tài khoản đang hoạt động hoặc đã ngừng hoạt động. Phải lưu tài khoản người sửa riêng.
- Lý do có sẵn là đủ; ghi chú thêm không bắt buộc. Không sửa QR, nguồn, chiều dài đã cắt hoặc số lượng đã lấy.
- Mốc hiện tại theo cuộn, độc lập với dòng lịch sử (7C); không làm đổi chiều dài tồn hoặc mốc lịch sử.
- Hoàn tác chỉ dành cho ghi nhận nhầm và thực tế chưa lấy/cắt; không tự hủy nghiệp vụ cắt/lấy/đặt trước độc lập chưa được chọn.
- Khi hủy chuỗi, khôi phục trạng thái trước chuỗi; sửa mốc phụ thuộc về sau mất hiệu lực nhưng còn nhật ký và được hiển thị trước khi lưu.
- Giữ bản nháp form cha; không tự lưu hoặc xóa bản nháp. Một dòng lỗi làm toàn bộ lần lưu quay lui.
- Không tự sửa `D:/Database/QLSX_v02.db`, `database/QLSX_v02.db` hoặc bản ZIP đính kèm. Mọi kiểm thử dùng tệp/memory riêng.
- Cập nhật cấu trúc SQLite bằng tập lệnh thủ công. Không chạy nâng cấp cấu trúc khi khởi động; không xây hỗ trợ dữ liệu cũ.

## Review Focus

1. B3 gửi lại mã cũ sau hoàn tác: không được ghi nhận lại; mã mới vẫn có thể thực hiện theo tồn hiện hành — Tasks 3, 7, 10.
2. Sửa người thực hiện rồi B3 phục hồi phiên: xác thực theo người gửi gốc, không nhầm với người đã sửa — Tasks 3, 5, 10.
3. Dữ liệu đổi rồi trở lại cùng mốc ban đầu: bản xem cũ vẫn phải bị từ chối do phiên bản tăng — Tasks 2, 5, 10.
4. Sửa mốc theo cuộn từ dòng cũ/kế hoạch khác và sau đó hoàn tác: không sửa lịch sử hoặc giữ nhầm hệ mốc — Tasks 6, 7, 8.
5. Bản nháp B2 đang sửa/chờ xóa khi tiến độ thay đổi: không mất nội dung và không được lưu đè dữ liệu không còn hợp lệ — Tasks 9, 10.

---

## 1. Bản đồ tệp và quy ước kiểm thử

Các đường dẫn dưới đây tính từ gốc kho mã B2. B3 sẽ nằm tại `integrations/B3`, là bản nguồn làm việc từ ZIP, không phải địa chỉ triển khai.

| Tệp/nhóm tệp | Trách nhiệm |
| --- | --- |
| `database/Migration_B2B3_ExecutionEdit_20261026.sql` | Cấu trúc chung, sổ mã, nhật ký, phiên bản và ràng buộc. |
| `DG_TonKhoBTP_v02/Models/Kho/XuatKho/ExecutionEdit_Model.cs` | Hợp đồng đọc/sửa; phân biệt dòng thực hiện và cuộn hiện tại. |
| `DG_TonKhoBTP_v02/Database/Kho/XuatKho/ExecutionEdit_DB.cs` | Đọc trạng thái, giao dịch lưu, kiểm tra phiên bản và nhật ký. |
| `DG_TonKhoBTP_v02/Database/Kho/XuatKho/ExecutionUndoPlanner.cs` | Lập tập thay đổi hoàn tác thuần dữ liệu, không tự mở kết nối/ghi DB. |
| `DG_TonKhoBTP_v02/Database/Kho/XuatKho/ExecutionEditAudit.cs` | Biểu diễn/đọc ghi JSON nhật ký có phiên bản, không đổi cấu trúc khi đọc. |
| `DG_TonKhoBTP_v02/UI/NghiepVuKhac/Kho/XuatKho/FrmChiTietThucHien.cs` | Form độc lập thay form lồng; sửa trực tiếp và mốc theo cuộn. |
| `DG_TonKhoBTP_v02/UI/NghiepVuKhac/Kho/XuatKho/ExecutionEditDraft.cs` | Bản nháp dòng/cuộn, phát hiện thay đổi, không ghi DB. |
| `DG_TonKhoBTP_v02/Database/Kho/XuatKho/LapKeHoachCatDay_DB.cs` | Kiểm tra xung đột và tải lại số liệu kế hoạch; không gom thêm toàn bộ tính năng vào tệp lớn này. |
| `DG_TonKhoBTP_v02/UI/NghiepVuKhac/Kho/XuatKho/UC_LapKeHoachCatDay.cs` | Mở form mới, hòa số liệu đã lưu vào bản nháp còn giữ. |
| `integrations/B3/app/Repositories/B3/OperationLedgerRepository.php` | Sổ mã thao tác và danh tính gốc; lịch sử hiệu lực không còn là nơi duy nhất chống trùng. |
| `integrations/B3/app/Repositories/B3/ExecutionRevisionRepository.php` | Đọc phiên bản chung để phát hiện trạng thái cũ. |
| `integrations/B3/app/Services/B3/ExecutionService.php` và `CutSessionService.php` | Ghi sổ mã cùng giao dịch; phục hồi phiên theo danh tính gốc/hiệu lực hiện tại. |
| `tests/fixtures/execution-edit/base-schema.sql`, `seed.sql` | Bộ dữ liệu chuẩn tối thiểu, giữ khóa ngoại/CHECK như cơ sở dữ liệu thật. |
| `tests/ExecutionEdit.*.Tests.ps1` | Kiểm thử tích hợp .NET và UI STA theo mẫu các kiểm thử hiện có. |
| `tests/fixtures/execution-edit/b3-bridge.php` | Cầu kiểm thử B3 trên tệp SQLite thử nghiệm do PowerShell tạo. |
| `docs/deployment/b2-b3-execution-edit.md` | Cách cập nhật phối hợp, chạy kiểm thử và kiểm tra sau cập nhật. |

Đăng ký mọi tệp `.cs` mới trong `DG_TonKhoBTP_v02/DG_TonKhoBTP_v02.csproj` vì dự án dùng danh sách Compile tường minh. Chỉ thêm tham chiếu nền tảng có sẵn nếu cần JSON (`System.Runtime.Serialization`); không đổi phiên bản framework hoặc gói hiện tại.

**Lệnh chuẩn:** sau khi xác định MSBuild qua `vswhere` hoặc đường dẫn đã tồn tại tại máy này, đặt `$ExecutionEditMsBuild` và chạy `& $ExecutionEditMsBuild DG_TonKhoBTP_v02.sln /t:Build /p:Configuration=Debug /p:Platform=AnyCPU /v:minimal`. Đường dẫn hiện có: `C:/Program Files/Microsoft Visual Studio/18/Community/MSBuild/Current/Bin/MSBuild.exe`.

Kiểm thử B2: `powershell.exe -NoProfile -STA -File tests/ExecutionEdit.<ten>.Tests.ps1`. Chúng tự nạp `DG_TonKhoBTP_v02/bin/Debug/DG_TonKhoBTP_v02.exe`, tạo DB tạm, cấu hình `DB_Base`, hoàn trả cấu hình và dọn đúng tệp mình tạo trong `finally`.

Kiểm thử B3: tại `integrations/B3`, `php artisan test --filter=<TenLop>`, luôn dùng cấu hình testing và DB thử nghiệm. Không chạy `composer setup` vì lệnh đó còn chạy migrate và các bước ngoài nhu cầu; khi thiếu vendor dùng `composer install --no-interaction --prefer-dist` từ lockfile. Không nâng cấp gói trong tác vụ này.

## 2. Hợp đồng dùng chung

### SQLite

- `B3ThaoTac`: `ThuTu INTEGER PRIMARY KEY AUTOINCREMENT`, `MaThaoTac TEXT UNIQUE NOT NULL`, `Loai` = TAKE/CUT, `LichSu_ID`, `KeHoach_ID`, `KeHoachHang_ID`, `TonCuonLe_ID` nullable, `TTCuonDay_ID`, `QuyCachCuonChan_ID` nullable, `RawQr`, `NguoiGuiGoc_ID`, `NguoiGuiGoc`, `TrangThai` = ACTIVE/HUY, `TruocJson`, `SauJson`, `ThoiGianGhiNhan`. Liên kết đối tượng đã hoàn tác là khóa nhận diện trong dữ liệu kiểm tra, không ON DELETE CASCADE.
- `B2DieuChinhLanLuu`: `MaLanLuu TEXT PRIMARY KEY`, `DauVanTayYeuCau`, `KeHoach_ID`, `NguoiSua_ID`, `NguoiSua`, `ThoiGianSua`, `KetQuaJson`. Chỉ tồn tại sau khi toàn bộ giao dịch thành công.
- `B2DieuChinhSuKien`: ID tăng, MaLanLuu, loại DOI_NGUOI/DOI_MOC/HOAN_TAC/HET_HIEU_LUC_MOC, đối tượng/ID gốc, MaThaoTac nếu có, MaThaoTacCoSo (lần cắt hiệu lực gần nhất khi sửa mốc), ID sự kiện mốc bị thay thế nếu có, lý do, ghi chú, `TruocJson`, `SauJson`. Chỉ thêm, không cập nhật/xóa lịch sử. Hết hiệu lực sửa mốc được biểu diễn bằng sự kiện mới tham chiếu sự kiện cũ.
- `B3PhienBanDuLieu`: `Khoa TEXT PRIMARY KEY`, `GiaTri INTEGER NOT NULL CHECK(GiaTri>=0)`. Giữ khóa sau khi đối tượng bị xóa; tăng phiên bản trên thay đổi liên quan. Có khóa toàn cục `FACTORY` để bảo vệ tập phụ thuộc và các khóa `PLAN:<id>`, `REEL:<id>`, `SOURCE:<id>` cho phiên B3; chấp nhận từ chối lưu bảo thủ khi có thay đổi khác trong tập đã xem.
- `B3TinhNangDuLieu`: `Ten TEXT PRIMARY KEY`, `PhienBan INTEGER`, `ChoPhepSua INTEGER CHECK IN(0,1)`. Migration tạo `EXECUTION_EDIT` phiên bản 1 với ChoPhepSua=0; bật thủ công sau khi cập nhật cả hai ứng dụng.

Các trường chứa snapshot dùng JSON định dạng phiên bản 1; ngày mới trong sổ dùng UTC với hậu tố Z, UI đổi sang giờ địa phương. Giữ nguyên các chuỗi thời gian thực hiện hiện có. Không dùng thời gian hiển thị để xếp thứ tự hoàn tác.

Phiên bản phải thay đổi khi sửa bảng nguồn nhập, cuộn, QR, kế hoạch/hàng/quy cách/nhóm/đoạn hoặc lịch sử liên quan. Tạo trigger trong migration để không phụ thuộc nơi gọi C#/PHP có nhớ tăng hay không. Không tăng phiên bản khi chỉ đọc hoặc thay bản nháp. Các thay đổi trong cùng giao dịch quay lui cùng phiên bản.

### C# — namespace Models.Kho.XuatKho

- `ExecutionDetailQuery(long planId, long itemId, long? specificationId, long? partialReelId)`; đúng một nguồn quy cách/cuộn lẻ.
- `ExecutionDetailSnapshot`: Query, PlanStatus, `List<ExecutionDetailRow> Rows`, `Dictionary<long,ExecutionReelState> Reels`, `Dictionary<string,long> Versions`, Users; chỉ thêm trạng thái cần kiểm tra, không lấy toàn bộ DB.
- `ExecutionDetailRow`: RowKey, Loai TAKE/CUT/PENDING, MaThaoTac, LichSuId, DetailId/GroupId/SpecificationId nullable, ReelId nullable, Quantity, Length, Done, PerformerUsername, PerformedAt, RawQr, các mốc lịch sử, các phụ thuộc/khóa sửa hiển thị.
- `ExecutionRowEdit`: MaThaoTac, NewPerformerUsername, Undo, Reason, Note. Không có trường sửa thời gian, QR, chiều dài hoặc số lượng.
- `ReelMarkerEdit`: ReelId, NewEnd, NewDirection, Reason, Note. Không nhận chiều dài tồn hoặc số đầu từ UI.
- `ExecutionEditRequest`: ChangeId (Guid), Query, ExpectedVersions, RowEdits, ReelEdits.
- `ExecutionEditResult`: Success, Conflict, ErrorsByKey, NewSnapshot, ChangedPlanIds, ChangedReelIds. Không lộ chi tiết ngoại lệ SQLite lên UI.
- `ExecutionEditPreview`: Valid, ErrorsByKey, UndoPlan, ImpactByRowKey, ImpactByReelId, MarkerEventsToSupersede; dữ liệu chỉ đọc để hiển thị trước ảnh hưởng, không phải lệnh ghi đã được xác thực vĩnh viễn.
- `ExecutionUndoContext`: trạng thái xác thực của tập dòng, toàn bộ lịch sử và tham chiếu trên các cuộn liên quan, sự kiện sửa mốc còn hiệu lực và phiên bản. Không tin tập phụ thuộc do UI gửi.
- `ExecutionUndoPlan`: UndoOperations theo thứ tự giảm dần, PerformerUpdates, ReelRestores, GroupsToUnbind, PartialReelsToRemove, QrUsageToRemove, MarkerEventsToSupersede. Có Before/After đã xác định cho từng tác động; không tự ghi DB.

### Điểm vào B2 — namespace Database.Kho.XuatKho

`ExecutionEdit_DB.Load(ExecutionDetailQuery query) : ExecutionDetailSnapshot`; `ExecutionEdit_DB.Preview(ExecutionEditRequest request) : ExecutionEditPreview` chỉ đọc trạng thái mới nhất và tính ảnh hưởng; `ExecutionEdit_DB.Save(ExecutionEditRequest request) : ExecutionEditResult` lấy người sửa từ `UserContext`, không nhận tài khoản tùy ý từ form; `ExecutionEdit_DB.LoadAudit(long planId, long? reelId, string operationToken) : DataTable`.

`ExecutionUndoPlanner.Build(ExecutionEditRequest request, ExecutionUndoContext context) : ExecutionUndoPlan` thuần dữ liệu, trả lỗi có khóa khi không hợp lệ. Snapshot trước chuỗi lấy từ sổ B3, bao gồm DB trước điều chỉnh mốc trong B3; không tái tạo bằng cộng trừ thiếu căn cứ.

### Điểm vào B3

`OperationLedgerRepository::find(string $token): ?array`, `assertUnused(string $token): void`, `record(array $operation): int`; `record` nhận toàn bộ snapshot chuẩn trước/sau và actor gốc; chạy trong B3Transaction hiện có.

`ExecutionRevisionRepository::sourceRevision(array $source): array` trả về phiên bản nguồn/cuộn liên quan. `CutSessionService::version()` đưa chúng vào chữ ký phiên. Yêu cầu dùng mã HUY trả `OPERATION_CANCELLED` (thao tác đã được hủy) với HTTP 409; các mã đang còn hiệu lực gửi lặp giữ `DUPLICATE_REQUEST`.

## Task 1: Nguồn B3 và nền kiểm thử an toàn

**Files:** Create `integrations/B3/**` từ ZIP; `tests/fixtures/execution-edit/base-schema.sql`, `seed.sql`, `tests/ExecutionEdit.TestSupport.ps1`, `tests/ExecutionEdit.Schema.Tests.ps1`; `.gitignore` chỉ thêm quy tắc riêng cho vendor/cache/env sinh ra của bản B3.

**Interfaces:** Produces `New-ExecutionEditFixture : string`, `Use-ExecutionEditFixture(string path)`, `Close-ExecutionEditFixture`, `Assert-ExecutionEdit(condition,message)` trong TestSupport; mỗi fixture có sentinel `EXECUTION_EDIT_TEST` để cầu PHP từ chối nhầm tệp dữ liệu thật.

- [x] **Step 1:** Kiểm tra worktree theo skill lúc bắt đầu triển khai; giữ tài liệu đã duyệt trong nơi thực hiện. Kiểm tra SHA256 ZIP = `07F6BF6E943AF0C1262A7528B3F85F6310571C356BB84558921A5E9F51775E9C`. Sao chép nguồn vào `integrations/B3`, chuẩn hóa và xác minh mọi đường dẫn đích nằm trong thư mục này. Bỏ `.env*` trừ `.env.example`, `.git`, vendor, DB, log, cache và các tệp vận hành có thể chứa cấu hình riêng. Không chạy script từ tài liệu đính kèm.
- [x] **Step 2:** Viết ca `ExecutionEditSchemaIsRequired`: fixture chưa migration phải bị kiểm tra chức năng từ chối; fixture hiện có không đọc tệp DB người dùng. Chạy `powershell.exe -NoProfile -STA -File tests/ExecutionEdit.Schema.Tests.ps1`; xác nhận thất bại vì chưa có cấu trúc tính năng, không phải do lỗi môi trường.
- [x] **Step 3:** Tạo fixture tối thiểu giữ nguyên các ràng buộc thật; không dùng schema nới lỏng để né khóa ngoại. Thêm người `worker` và `editor`, người đã ngừng hoạt động, kế hoạch ACTIVE/HUY, cuộn lẻ 800 m và nguồn chẵn 1.000 m; seed chưa có thực hiện, các test tạo thực hiện theo từng ca.
- [x] **Step 4:** Build B2 và chạy nền `tests/LapKeHoachCatDay.ChiTiet.Tests.ps1`, `tests/LapKeHoachCatDay.NguoiCat.Tests.ps1`; B3 chạy `php artisan test --filter=CutMarkerAdjustmentTest` và `TakeWholeReelTest`. Ghi chính xác lỗi nền nếu có; không sửa lỗi không liên quan trong tính năng. Chuẩn bị môi trường testing khi thiếu, không chạy lệnh setup tự migrate DB đang dùng.
- [x] **Step 5:** Commit nguồn nhập và nền kiểm thử riêng: `chore: import B3 source and execution edit fixtures`. Ca tính năng mới được đánh dấu đang chờ Task 2; không coi toàn bộ tính năng đã đạt.

## Task 2: Cấu trúc chung, nhật ký và phiên bản đơn điệu

**Files:** Create migration SQL; Modify `tests/ExecutionEdit.Schema.Tests.ps1`; Modify `integrations/B3/tests/Support/CreatesFactoryFlowSchema.php` để fixture PHP áp dụng cùng migration; Create `integrations/B3/tests/Feature/B3/ExecutionEditSchemaTest.php`.

**Interfaces:** Consumes fixture Task 1; Produces các bảng mục 2 và trigger phiên bản, cấu trúc chung phiên bản 1; thêm hàm TestSupport `Apply-ExecutionEditMigration(string fixturePath)`.

- [x] **Step 1:** Bổ sung assertions: mã thao tác trùng bị từ chối; sổ mã/audit không mất khi xóa lịch sử hiệu lực; sửa mốc rồi trả đúng giá trị cũ vẫn tăng phiên bản; xóa/tạo lại nguồn không làm phiên bản trở về 0; quay lui giao dịch không giữ audit/phiên bản dở dang.
- [x] **Step 2:** Chạy `ExecutionEdit.Schema.Tests.ps1` và `php artisan test --filter=ExecutionEditSchemaTest`; xác nhận ca mới thất bại đúng vì thiếu bảng/trigger.
- [x] **Step 3:** Viết migration theo hợp đồng SQLite. Ràng buộc bất biến cho mã/actor/snapshot gốc trong B3ThaoTac; chỉ trạng thái hiệu lực được phép chuyển ACTIVE→HUY. Nhật ký sửa chỉ thêm, không ON DELETE CASCADE từ nghiệp vụ. Đồng nhất fixture PHP với cột thời gian và ràng buộc thật trước khi áp dụng migration. Migration không xóa/bổ sung suy đoán dữ liệu hiện có; cấu trúc không phù hợp phải báo rõ. Không tự bật ChoPhepSua.
- [x] **Step 4:** Chạy hai bộ schema; kiểm tra `PRAGMA foreign_key_check` trên fixture trả 0 dòng, khóa phiên bản không giảm trong mọi ca. Thử áp dụng migration lần hai: không tạo cấu trúc trùng hoặc xóa dữ liệu tính năng.
- [x] **Step 5:** Commit `feat: add shared execution edit audit and revision schema`.

## Task 3: B3 ghi sổ mã, giữ danh tính gốc và phát hiện phiên cũ

**Files:** Create hai repository PHP ở bản đồ tệp; Modify B3 `ExecutionService.php`, `ExecutionRepository.php`, `CutSessionService.php`, `QrResolver.php` nếu cần đưa phiên bản vào scan; Modify `ConcurrencyAndIdempotencyTest.php`; Create `OperationLedgerTest.php` và `ExecutionEditRecoveryTest.php` trong `tests/Feature/B3`.

**Interfaces:** Produces repository PHP mục 2 và sổ snapshot nguyên trạng. Không đổi chữ ký công khai execute của B3 ngoài việc bổ sung thông tin lỗi/phiên bản tương thích giao diện.

- [x] **Step 1:** Viết kiểm thử chạy thực tế: TAKE/CUT tạo history+ledger trong một giao dịch; snapshot trước cắt lẻ chứa mốc DB gốc trước rebasing; snapshot chuyển lẻ có nhóm trước TonCuonLe=null. Mô phỏng sửa người lịch sử thành tài khoản khác, recovery của worker gốc vẫn nhận đúng thao tác; tài khoản không phải chủ thể gốc không chiếm được yêu cầu. Mã HUY luôn bị chặn dù history đã gỡ và QR đã mở lại.
- [x] **Step 2:** Chạy `php artisan test --filter='OperationLedgerTest|ExecutionEditRecoveryTest'`; xác nhận thất bại ở các quy tắc mới.
- [x] **Step 3:** Ghi sổ trong transaction execute hiện có; thay kiểm tra token chỉ dựa history bằng ledger. Giữ marker `__B3_OP__:` phục vụ đối chiếu. Khi recovery gặp mã đã HUY trả lỗi có thông điệp tải lại, không gọi lại execute; không báo NOT_FOUND rồi cho gửi lại. Source version phải gồm phiên bản đơn điệu, không chỉ mốc/MAX(id). Đọc/ghi kiểm tra schema/version; nếu chưa có schema tính năng, không tự migrate và không cho chạy lẫn chế độ có thể bỏ qua ledger.
- [x] **Step 4:** Chạy các test mới và B3 `CutMarkerAdjustmentTest`, `ContinuousCutSessionTest`, `TakeWholeReelTest`, `MultiLengthFlowTest`; chứng minh mã mới sau hoàn tác vẫn tạo thực hiện hợp lệ. Sửa test cũ chỉ tìm chuỗi trong tệp thành assertions hành vi thay vì giữ test bám cách viết mã.
- [x] **Step 5:** Commit `feat: persist immutable B3 operation identity and revisions`.

## Task 4: B2 đọc đúng phạm vi và mô hình dòng/cuộn độc lập

**Files:** Create `ExecutionEdit_Model.cs`, `ExecutionEdit_DB.cs`, `ExecutionEditAudit.cs`; Modify `.csproj`; Create `tests/ExecutionEdit.Read.Tests.ps1`.

**Interfaces:** Produces `ExecutionEdit_DB.Load`, `LoadAudit` và các mô hình mục 2. Dùng `DB_Base.OpenConnection()`, giữ `LayChiTietThucHien` hiện có cho đến khi form mới thay thế.

- [x] **Step 1:** Viết assertions đọc đúng ba nghiệp vụ; không lộ dòng ngoài query; dòng CUT gắn ReelId thật và phân biệt LoaiNguon của lần cắt với nguồn gốc nhóm. Chọn đoạn cũ sau hai lần cắt phải trả lịch sử 800 m của dòng cũ nhưng mốc cuộn hiện tại 650 m. Người ngừng hoạt động vẫn có trong lựa chọn; thời gian chỉ có ở snapshot, không có trong request sửa.
- [x] **Step 2:** Build rồi chạy `tests/ExecutionEdit.Read.Tests.ps1`; xác nhận thiếu kiểu/điểm vào mới gây FAIL đúng dự kiến.
- [x] **Step 3:** Cài đặt mô hình và loader với snapshot đọc nhất quán, lấy các lịch sử/nhóm phụ thuộc trên toàn cuộn kể cả kế hoạch khác. Dòng chưa thực hiện chỉ xem; người dùng không truyền được actor của lần sửa. Audit JSON có phiên bản và dữ liệu trước/sau, đọc qua thư viện nền tảng .NET.
- [x] **Step 4:** Build/chạy test; kiểm tra QR nguyên vẹn cả chuỗi dài, nguồn null không tự bịa; QUERY sai plan/item/qc/reel bị từ chối. HUY trả snapshot chỉ xem thay vì lỗi mơ hồ.
- [x] **Step 5:** Commit `feat: load editable execution details with reel scoped state`.

## Task 5: Lưu người thực hiện và mốc cuộn, nhật ký và chống lưu đè

**Files:** Modify `ExecutionEdit_DB.cs`, `ExecutionEditAudit.cs`; Create `tests/ExecutionEdit.Corrections.Tests.ps1`.

**Interfaces:** Implements `Save` cho RowEdits.Undo=false và ReelEdits. Save lấy `UserContext.UserId/UserName` ở biên được tin cậy, ghi cùng batch audit; chuẩn bị điểm apply dùng chung cho Task 7.

- [x] **Step 1:** Viết ca sửa worker→tài khoản ngừng hoạt động: tồn/time/actor gốc ledger giữ nguyên, batch lưu editor riêng. Mốc 0→650 đổi thành 1000→1650 giữ 650 m. Đổi chiều phải tính số đầu đúng và chặn âm/phân số/tràn số. Cùng cuộn từ nhiều dòng chỉ ghi một sự kiện mốc; HET không tăng tồn qua sửa mốc. Batch lặp cùng ChangeId/cùng payload trả kết quả cũ, khác payload hoặc actor bị từ chối.
- [x] **Step 2:** Build/chạy `tests/ExecutionEdit.Corrections.Tests.ps1`; xác nhận các ca chưa có Save thất bại đúng quy tắc.
- [x] **Step 3:** Cài transaction ghi với retry hữu hạn cho SQLITE_BUSY; đọc lại trạng thái/phiên bản trước ghi; xác thực người sửa đăng nhập, tài khoản đích tồn tại, lý do và trạng thái ACTIVE. Kết quả lỗi giữ ErrorsByKey. Lưu mốc theo ReelId, lý do thuộc cuộn, anchor MaThaoTacCoSo tới lần cắt có hiệu lực gần nhất toàn cuộn. Sổ nguồn gốc không bị sửa khi đổi PerformerUsername. Bắt mọi phép tính trong checked/range kiểm tra kiểu số nguyên.
- [x] **Step 4:** Chạy test gồm lỗi chèn audit sau cập nhật dữ liệu khiến toàn bộ quay lui; B3 thay đổi rồi đưa mốc về giá trị cũ khiến request B2 cũ vẫn bị từ chối. Không phải sửa thời gian để làm ca kiểm thử đó.
- [x] **Step 5:** Commit `feat: save performer and current reel marker corrections atomically`.

## Task 6: Lập chuỗi hoàn tác, kiểm tra phụ thuộc và mốc gốc

**Files:** Create `ExecutionUndoPlanner.cs`; Modify loader/context trong `ExecutionEdit_DB.cs`; Create `tests/ExecutionEdit.UndoPlan.Tests.ps1`.

**Interfaces:** Produces `ExecutionUndoPlanner.Build`, `ExecutionUndoPlan` và `ExecutionEdit_DB.Preview` mục 2; loader thu thập ExecutionUndoContext bên trong transaction. Planner không đọc DB và không được tự thêm dòng cắt/lấy chưa chọn. Preview dùng snapshot đọc nhất quán, không lưu header/audit hoặc tăng phiên bản.

- [x] **Step 1:** Viết assertions ba nghiệp vụ, suffix nhiều lần: 1000→800→650 chọn hai lần trả snapshot trước chuỗi 1000; chỉ chọn lần đầu bị chặn. Nếu lần sau thuộc kế hoạch khác, báo đúng ID/nhóm phụ thuộc và không tự hủy nó. Nếu trước cắt DB 200→800 được B3 rebase rồi cắt, hoàn tác trả DB 200→800, không lấy mốc đã rebase làm gốc.
- [x] **Step 2:** Chạy `ExecutionEdit.UndoPlan.Tests.ps1`, xác nhận FAIL vì thiếu planner hoặc chưa xử lý các nhánh.
- [x] **Step 3:** Cài planner xếp theo B3ThaoTac.ThuTu; xác minh chuỗi lịch sử/marker/nhóm/nguồn phù hợp snapshot. Marker events có anchor trong tập cut bị hủy trở thành hết hiệu lực và được đưa vào plan để UI xem trước; không sửa audit cũ. Chuyển lẻ chỉ được xóa cuộn khi không có nhóm/đặt trước/tham chiếu độc lập bên ngoài tập xử lý. Chặn hoàn tác+đổi mốc cùng cuộn trong một batch bằng lỗi có khóa cuộn.
- [x] **Step 4:** Chạy test với ví dụ đã duyệt: trước cut 0→800, cut nhầm150 còn0→650, sửa mốc1000→1650, undo trả0→800 và chỉ supersede marker tương ứng. Đổi người ở bước cũ không trở thành phụ thuộc vật lý ngăn undo nhưng lịch sử sửa vẫn còn. Gọi Preview phải trả đúng ảnh hưởng đó mà không thay đổi history, ledger, audit hoặc phiên bản.
- [x] **Step 5:** Commit `feat: plan validated execution undo chains across reel dependencies`.

## Task 7: Áp dụng hoàn tác đồng bộ tồn, QR, nhóm và lịch sử

**Files:** Modify `ExecutionEdit_DB.cs`, `ExecutionEditAudit.cs`; Create `tests/ExecutionEdit.Undo.Tests.ps1`.

**Interfaces:** Completes `Save` với planner Task 6; cùng cơ chế idempotent ChangeId và audit của Task 5. Trả NewSnapshot và changed IDs cho form/parent.

- [x] **Step 1:** Viết integration assertions: partial 650→800; full conversion trả đúng 1 cuộn nguồn không tăng SoCuon; TAKE trả một lịch sử/QR, số còn chờ tăng tương ứng. Lịch sử hiệu lực gỡ nhưng snapshot/audit/token HUY còn. Cố tình gây lỗi FK hoặc audit giữa batch phải giữ nguyên cả history, cuộn, nhóm, QR, audit, version.
- [x] **Step 2:** Chạy `ExecutionEdit.Undo.Tests.ps1`; xác nhận FAIL ở nhánh Save.Undo chưa cài.
- [x] **Step 3:** Trong cùng transaction: validate, lập plan, ghi audit trước/sau, gỡ QR usage đúng đối tượng, xử lý histories mới→cũ, unbind nhóm tạo cuộn, gỡ cuộn lẻ phát sinh sau khi hết tham chiếu, khôi phục partial có sẵn, chuyển sổ token sang HUY, thêm sự kiện marker hết hiệu lực. Không tăng SoCuon nguồn. Tính/kiểm tra tồn và đặt trước toàn cục liên quan trước commit; không âm thầm sửa đặt trước độc lập để vượt qua lỗi.
- [x] **Step 4:** Chạy test whole/convert/partial, nhiều quy cách/cùng sản phẩm/cùng nguồn; `PRAGMA foreign_key_check` rỗng. Kiểm tra `ACTIVE` dù hoàn thành vẫn sửa được; HUY không được ghi; pending→done không có đường gọi từ B2. Một row sai làm toàn batch không đổi.
- [x] **Step 5:** Commit `feat: undo mistaken B3 executions with consistent inventory and QR state`.

## Task 8: Form sửa trực tiếp, mốc theo cuộn 7C

**Files:** Create `FrmChiTietThucHien.cs`, `ExecutionEditDraft.cs`; Modify UC để bỏ form lồng cũ/mở form mới và `.csproj`; Modify `tests/LapKeHoachCatDay.ChiTiet.Tests.ps1`; Create `tests/ExecutionEdit.UI.Tests.ps1`.

**Interfaces:** `FrmChiTietThucHien(ExecutionDetailQuery query, string productName)`, `ExecutionEditResult LastSaveResult { get; }`; `ExecutionEditDraft(ExecutionDetailSnapshot baseline)`, `SetRowEdit(ExecutionRowEdit edit)`, `SetReelEdit(ReelMarkerEdit edit)`, `BuildRequest(Guid changeId)`, `HasChanges`, `Reset`. Form dùng Task 4/5/7 và cùng draft cho mọi dòng/cuộn.

- [x] **Step 1:** Viết UI assertions STA: không có nút Điều chỉnh/popup; trạng thái ComboBox sửa ngay, pending không thực hiện mới; Performer chọn được inactive; PerformedAt readonly. Chọn hai dòng cùng cuộn và sửa mốc từ dòng cũ chỉ tạo một ReelEdit. Đổi lý do không làm tự ghi DB; Save disabled khi không thay đổi.
- [x] **Step 2:** Build/chạy `ExecutionEdit.UI.Tests.ps1`, xác nhận form/kiểu mới chưa có hoặc hành vi cũ gây FAIL đúng kỳ vọng.
- [x] **Step 3:** Cài form giữ font Tahoma, trạng thái và bố cục hợp nền WinForms hiện tại; bảng có thông tin lịch sử và phần mốc hiện tại độc lập có mã cuộn/tồn rõ. Lý do mốc gắn ReelId, lý do sửa người/undo gắn row; một batch nhiều thay đổi cùng loại có thể áp dụng cùng lý do từ lựa chọn có sẵn, không buộc nhập lại văn bản. Hiển thị dự kiến khôi phục/sửa mốc mất hiệu lực trên form qua `ExecutionEdit_DB.Preview`; bỏ kết quả preview đã lỗi thời khi người dùng đổi tiếp bản nháp, revalidate khi Save. Khoá nút trong khi lưu, giữ draft khi lỗi.
- [x] **Step 4:** Chạy UI và hồi quy ChiTiet; cập nhật assertions layout cũ cho giao diện mới nhưng giữ kiểm tra QR/ghi chú/trạng thái và pending fields trống. Kiểm tra người sửa không chọn/nhập được, time readonly, HET không có đường tạo tồn, TAKE ẩn mốc, form HUY readonly, đóng có draft cho Lưu/Bỏ/Quay lại. Xem thực tế 3 kịch bản với dữ liệu test, không kết nối DB thật.
- [x] **Step 5:** Commit `feat: enable direct execution editing with reel scoped marker fields`.

## Task 9: Hòa tiến độ đã lưu với bản nháp kế hoạch còn giữ

**Files:** Modify UC, `LapKeHoachCatDay_DB.cs`, `ExecutionEditDraft.cs` nếu cần; Create `tests/ExecutionEdit.ParentDraft.Tests.ps1`.

**Interfaces:** Add `UC_LapKeHoachCatDay.RefreshExecutionFactsPreservingDraft(ExecutionEditResult result) : void` private/internal và `ExecutionEdit_DB.ReadPlanRevision(long planId) : long`. Add `long? ExpectedExecutionRevision` vào `LapKeHoachCatDay_BatchSaveRequest` trong `LapKeHoachCatDay_Model.cs`: null khi tạo mới; khi chức năng EXECUTION_EDIT đã bật, sửa kế hoạch đã tồn tại bắt buộc có phiên bản đối chiếu.

- [x] **Step 1:** Viết test: đang sửa chiều dài/nhóm, danh sách chờ xóa, người nhận/người cắt/ghi chú chưa lưu; lưu chi tiết người khác/mốc/undo rồi toàn bộ draft vẫn còn và vẫn được đánh dấu chưa lưu. Lưu kế hoạch sau thay đổi B3 không được ghi đè nhóm vừa được thực hiện hoặc xóa lịch sử.
- [x] **Step 2:** Chạy `ExecutionEdit.ParentDraft.Tests.ps1`; xác nhận việc chưa cập nhật/preserve gây FAIL ở ca mới.
- [x] **Step 3:** Thay vì `ResetVeTrangThaiMoi()` sau lưu chi tiết, chỉ tải các facts từ DB, hòa vào row đang có; giữ giá trị nhập và baseline đủ phát hiện xung đột. Đưa version kế hoạch vào batch save, kiểm tra lại ở transaction hiện có. Khi facts thay đổi, đánh dấu cần đối chiếu, không đánh dấu draft đã lưu. Không thay đổi semantics lưu kế hoạch khi chưa có thay đổi execution.
- [x] **Step 4:** Chạy test parent draft và `LapKeHoachCatDay.NguoiCat.Tests.ps1`; xác minh sửa người thực hiện lịch sử không đổi assignment `Users_id`, người cắt dự kiến hoặc quyền sở hữu B3.
- [x] **Step 5:** Commit `fix: preserve planning drafts when execution details change`.

## Task 10: Kiểm thử xuyên B2/B3 và hướng dẫn cập nhật

**Files:** Create `tests/fixtures/execution-edit/b3-bridge.php`, `tests/ExecutionEdit.CrossApp.Tests.ps1`; Create `docs/deployment/b2-b3-execution-edit.md`; update source-import provenance `integrations/B3/README.execution-edit.md`.

**Interfaces:** Bridge nhận `--db <fixturePath> --action <take|cut|recover|scan> --payload <jsonFile>`; chỉ mở tệp có sentinel TEST, cấu hình APP_ENV=testing/không dùng cache production, trả JSON kết quả và exit code. PowerShell tạo payload file bằng API ghi UTF-8, không ghép chuỗi JSON vào lệnh shell.

- [x] **Step 1:** Viết ca disk SQLite: B3 thực hiện → B2 đọc/sửa → B3 phục hồi/cắt tiếp; replay mã HUY; mã mới sau undo; stale source_version sau sửa rồi khôi phục mốc về giá trị cũ; hai connection giữ transaction để kiểm tra SQLITE_BUSY/retry. Mất kết nối sau commit mô phỏng bằng đọc lại cùng ChangeId; không tạo tác động hai lần.
- [x] **Step 2:** Chạy `ExecutionEdit.CrossApp.Tests.ps1`; xác nhận test thực sự gọi PHP và assembly C#, không mô phỏng hai ứng dụng bằng cùng một bộ SQL. Nếu runtime/dependency thiếu, giữ báo cáo thiếu và khắc phục môi trường được phép; không đổi assertion thành kiểm tra chuỗi mã nguồn.
- [x] **Step 3:** Hoàn thiện bridge và tài liệu: sao lưu/đóng ghi có kiểm soát, áp dụng migration thủ công, cập nhật B2/B3, xác minh schema+phiên bản, bật ChoPhepSua=1, thử bằng dữ liệu thử nghiệm. Có hướng dẫn tắt tính năng khi kiểm tra không đạt, không hạ cấp B3 về bản không biết ledger khi B2 vẫn sửa. Không chạy các bước triển khai này trên DB người dùng trong tác vụ lập trình.
- [x] **Step 4:** Build B2; chạy toàn bộ `ExecutionEdit.*.Tests.ps1` (trừ support), hai hồi quy ChiTiet/NguoiCat; B3 `php artisan test --testsuite=Unit` và `php artisan test --testsuite=Feature --filter=B3`. Chạy `git diff --check`; ghi kết quả thực tế, số test và giới hạn môi trường. Kiểm tra đủ 13 tiêu chí spec và 5 Review Focus có test đạt, xem UI ba trường hợp. Yêu cầu một lượt review toàn thay đổi trước khi tuyên bố hoàn thành.
- [x] **Step 5:** Commit `test: verify independent B2 B3 execution corrections and rollout`; chuẩn bị bản B2/B3 và tập lệnh để người dùng xem. Không tự triển khai máy chủ hoặc thay file DB thật.

## 3. Thứ tự và điều kiện hoàn tất

Thứ tự: 1→2→3→4→5→6→7→8→9→10. Đây là một tính năng tích hợp, không tách hai dự án độc lập có quy tắc SQLite khác nhau. Từng task có vòng test riêng và commit riêng; lỗi phát hiện phải sửa trước khi chuyển phần phụ thuộc.

Không bật chức năng sửa khi mới có UI hoặc mới có một phía B2/B3. Hoàn tất khi đạt tiêu chí nghiệm thu spec, có kết quả kiểm thử thực tế cho cả hai runtime, nhật ký/chống replay đã được kiểm tra, UI đúng các quyết định đã duyệt, và tài liệu cập nhật đã được chuẩn bị. Trạng thái hiện tại: đã triển khai và kiểm thử tính năng trên dữ liệu tạm; đã rà soát độc lập và sửa các phát hiện. Chưa triển khai lên DB/máy chạy thật. Xem docs/verification/b2-b3-execution-edit.md.

## 4. Tự rà soát kế hoạch

- Phủ yêu cầu: 13 quyết định tại spec mục 2 được gắn vào Task 3–9; ba hoàn tác ở Task 6–7; cập nhật schema/thử nghiệm/triển khai ở Task 1–2/10; toàn bộ tiêu chí spec mục 12 có assertions tại Task 2–10.
- Điểm vào/kiểu dùng thống nhất ở mục 2; Task 8 không tự tạo API khác với Task 4–7. Time không xuất hiện trong kiểu request; actor không nhận từ input form.
- 5 Review Focus đều đã gắn ca kiểm thử vào task sở hữu; không chỉ kiểm tra mã nguồn bằng tìm chuỗi.
- Các cập nhật có rủi ro giữa hai runtime được kiểm tra trên một tệp thử nghiệm thật ở Task 10; PHP hiện có tại máy và dự án B2 nhắm .NET Framework 4.8, không giả định `dotnet test` thay cho kiểm thử STA hiện hữu.
- Không chứa lệnh chạy trên DB người dùng, không dùng `.env.launcher-backup` từ ZIP và không thêm một quy trình duyệt nghiệp vụ mới cho người dùng sản phẩm.
