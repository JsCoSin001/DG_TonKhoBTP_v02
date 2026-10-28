# Chuyển chi tiết thực hiện sang form thiết kế sẵn — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Chuyển chức năng chi tiết thực hiện sang `Frm_ChiTietThucHienKeHoach`, giữ nghiệp vụ và thiết kế người dùng đã chốt.

**Architecture:** Form mới nhận đúng `ExecutionDetailQuery`, tên sản phẩm và `ExecutionDetailSnapshot` như form cũ; tiếp tục gọi `ExecutionEditDraft` và `ExecutionEdit_DB`. Control thông báo hiển thị lỗi; hộp thoại xác nhận thay vùng xem trước tác động, hộp thoại xung đột thay nút tải mới. Đóng không lưu thay nút bỏ bản nháp.

**Tech Stack:** C# 8, .NET Framework 4.8, WinForms, System.Data.SQLite, PowerShell STA integration tests.

**Spec:** `docs/superpowers/specs/2026-10-27-execution-detail-control-mapping.md`

## Global Constraints

- Không thay đổi nhãn mô tả, tiêu đề, tên thẻ, chú thích nút hoặc bố cục Designer. Theo yêu cầu bổ sung sau khi chuyển form: `lblTenSP`, `lblThongTinSP` là Label dữ liệu động của dòng cha; cùng `lblTacDong` báo lỗi là các ngoại lệ cập nhật nội dung.
- Không thêm `rtbThongTinThucHien`, hai nút bổ sung, hai ComboBox lý do hoặc Label động.
- Dùng tên thực tế `rtbLyDoSua`; lý do chung cho một lần lưu, bắt buộc không trống khi lưu. Giữ mã phân loại qua `Reason`, lưu nội dung qua `Note` hiện có.
- Không thay đổi lớp database, schema SQLite hoặc B3; không chạm database gốc.
- Giữ mã chống ghi trùng, phiên bản, giao dịch nguyên tử, nhật ký và tài khoản người sửa.
- Dùng `lblTacDong` kiểu Label để hiển thị lỗi theo giải thích cuối cùng của người dùng. Đây là Label động; chỉ nội dung Label này được cập nhật, các Label khác giữ nguyên. Không cần RichTextBox bổ sung.
- Giữ thay đổi sẵn có của người dùng trong csproj, Designer và resx; không ghi đè bằng file sinh lại.

## Review Focus

- Gõ số cuối chưa hoàn chỉnh, chữ, âm hoặc tràn số không được tạo thay đổi sai hoặc ghi database.
- Chọn dòng cũ không thay tồn hiện tại bằng mốc lịch sử; chuyển dòng cùng cuộn giữ đúng một bản nháp mốc.
- Lỗi lưu / từ chối tải mới / từ chối xác nhận không mất lý do, mốc hoặc người đang nhập.
- Dữ liệu đổi trong lúc người dùng đọc xác nhận phải bị phát hiện, không ghi theo kết quả kiểm tra cũ.
- Dòng chưa thực hiện / lấy nguyên cuộn / cuộn hết dây / kế hoạch HUY có quyền chỉnh sửa đúng như lớp nghiệp vụ đang bảo vệ.

## Files and responsibilities

- Create `DG_TonKhoBTP_v02/UI/NghiepVuKhac/Kho/XuatKho/Frm_ChiTietThucHienKeHoach.Execution.cs`: binding, sự kiện, kiểm tra nhập, lưu / đối chiếu / đóng, quản lý timer. Giữ nguyên constructor không tham số trong file `.cs` người dùng tạo.
- Modify `DG_TonKhoBTP_v02/UI/NghiepVuKhac/Kho/XuatKho/UC_LapKeHoachCatDay.cs`: đổi nơi tạo form, giữ refresh dữ kiện và bản nháp cha.
- Modify `DG_TonKhoBTP_v02/DG_TonKhoBTP_v02.csproj` và form cũ chỉ khi cần bỏ triển khai giao diện trùng sau chuyển; không bỏ đăng ký Designer / resx người dùng vừa thêm.
- Modify `tests/ExecutionEdit.UI.Tests.ps1`, `ExecutionEdit.Preview.Tests.ps1`, `ExecutionEdit.Review.Tests.ps1`, `ExecutionEdit.RealDatabase.Tests.ps1`: kiểm thử form đang được mở thực tế, thay tham chiếu kiểu / tên control cũ.
- New tests `tests/ExecutionEdit.DesignedForm.Tests.ps1`: các ca bảo toàn nhãn, đầu vào văn bản và lưu lý do, không sinh thêm control, xử lý lỗi / xung đột.
- No implementation edits to `.Designer.cs`, `.resx`, `ExecutionEdit_DB*`, `ExecutionEditDraft`, B3 hoặc SQL.

### Task 1: Chuyển binding, bản nháp và thao tác sang form mới

**Interfaces:**
- Consume `ExecutionEdit_DB.Load(query)`, `Preview(request)`, `Save(request)`, `LoadAudit(planId,reelId,token)` và `ExecutionEditDraft` không đổi.
- Produce constructors `Frm_ChiTietThucHienKeHoach()` và internal constructors `(ExecutionDetailQuery query, string productName)` / `(ExecutionDetailQuery query, string productName, ExecutionDetailSnapshot snapshot)`; giữ `LastSaveResult` kiểu `ExecutionEditResult?` để form cha đọc.
- `SaveAsync()` trả `Task<bool>`; `ReloadPreservingDraft()` giữ mục đích và kiểm tra đối tượng hiện có. Gắn sự kiện vào các control đã thiết kế, không dựng lại layout bằng code.

- [x] Resolve control lỗi với người dùng: dùng `lblTacDong` như Label động; không thêm RichTextBox.
- [x] Write failing tests: form hiển thị đủ dòng; Label.Text trước và sau binding giống nhau; không có control bị loại; thời gian khóa; pending không thực hiện mới; người ngừng hoạt động chọn được; một cuộn một bản nháp; số cuối sai không ghi; lý do giữ khi chuyển dòng; lỗi lưu giữ bản nháp.
- [x] Run new tests against current Debug binary and confirm failure because target form has no business binding.
- [x] Implement binding and event handlers using existing snapshot/draft and six grid columns. `tbxSoCuoi_Sua` parses integer safely; `_start = end - direction * currentLength` with bounds. Keep historical three fields independent. Date picker shows stored timestamp only; pending shows no date.
- [x] Implement save: end current cell edit; validate reason/input; build one request with unchanged internal reason codes and shared text Note; Preview latest request; show validation errors in error control. For undo/marker edits, confirm complete impact before Save. Cancel never writes; Save rechecks versions atomically. Do not show timer-driven modal dialogs.
- [x] Implement conflict prompt -> existing preserve-draft reload, keep reason and valid intended edits. Do not reload without user's choice. Existing Close uses Save/Discard/Cancel; save failure prevents close. Stop and dispose timer safely without adding a second Dispose override to the Designer partial class.
- [x] Run new tests and updated UI / review tests; use temporary fixture DB only. Expected: all relevant cases pass.

### Task 2: Chuyển điểm mở và kiểm tra hồi quy

**Interfaces:**
- Consume form mới, `LastSaveResult` và `UC_LapKeHoachCatDay.RefreshExecutionFactsPreservingDraft(result)` không đổi.
- Produce point of entry that opens only `Frm_ChiTietThucHienKeHoach` and refreshes execution facts while retaining parent planning drafts.

- [x] Verify the one-line parent opener substitution by diff, and run actual new-form integration/render tests rather than adding a source-text assertion that mirrors the implementation.
- [x] Switch form creation at parent call site; retain current query, loading indicator, error handling and post-save refresh.
- [x] Remove obsolete duplicate implementation only after replacing every test / production reference; preserve regression coverage rather than keeping tests passing against an unused form.
- [x] Build Debug and Release with Visual Studio MSBuild; run `ExecutionEdit.*.Tests.ps1` on appropriate fresh binaries, skipping optional real DB test unless a path is explicitly provided. Expected: feature suites pass; distinguish known unrelated baseline failures if broader checks are run.
- [x] Render actual partial / conversion / whole-reel forms using `ExecutionEdit.Preview.Tests.ps1`, inspect images for all three cases and unchanged Label captions, and inspect layout of actual error control.
- [x] Verify git diff: no implementation changes to Designer captions, B3, SQL, DB service or user DB; record results in verification doc. Update plan checkboxes and report any remaining limitations.
- [x] Independent final review and one verified fix pass: identical-marker reload drops phantom dirty state; confirmation-race test proves Save reaches version validation. Both regression assertions observed RED then GREEN; entire feature suite green after fixes.

## Self-review

The plan covers the approved control mapping, preserves existing data operations, assigns each boundary to the new form or parent opener, and tests all five Review Focus conditions. The user clarified the actual error control: use the existing Label `lblTacDong` for dynamic errors, preserving every other Label caption and the Designer.
