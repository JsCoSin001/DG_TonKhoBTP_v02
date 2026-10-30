# UC_BCTonKho Focused Fixes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Sửa INV-001, INV-002, INV-003, UI-001, UI-002 và REG-001, chứng minh tồn, định danh và các luồng liên quan đúng bằng kiểm thử mới.

**Architecture:** Giữ cấu trúc hiện tại. BC_TonKho_DB nhận diện nguồn bằng mốc nhập và liên kết cuộn; lịch sử dùng để kiểm tra sai lệch. Bổ sung refresh cho helper gợi ý, xử lý màu nút tại grid tồn và đưa preview vào hộp xác nhận hiện có; không viết lại nghiệp vụ ghi dữ liệu.

**Tech Stack:** C# 8.0, .NET Framework 4.8, WinForms STA, System.Data.SQLite 1.0.119, Windows PowerShell, bộ fixture/harness hiện có.

**Spec:** `docs/superpowers/specs/2026-10-30-uc-bctonkho-fixes-design.md` — người dùng duyệt ngày 30/10/2026.

## Global Constraints

- Giữ .NET Framework 4.8, WinForms, System.Data.SQLite và cấu trúc Model/Database/UI hiện tại.
- Không sửa database gốc, không migration, không tự sửa chữa dữ liệu và không thay schema. Chức năng xem tồn chỉ SELECT qua DB_Base.
- Không commit, push hoặc merge. Không ghi đè các thay đổi người dùng đã có trước phiên làm việc.
- Không viết lại thuật toán FIFO hoặc cơ chế lưu nhập/lấy/cắt/hoàn tác B2/B3 trong đợt này.
- Giữ 10 cột grid, giới hạn 50 gợi ý, nội dung mỗi item riêng lẻ và quy tắc đặt chỗ không giảm tồn vật lý.
- Giữ nguyên báo cáo kiểm thử trước sửa và dữ liệu bằng chứng của nó. Kết quả sau sửa được lưu riêng.
- DB gốc cần bảo toàn: `database/QLSX_v02.db` và `D:/Database/QLSX_v02.db`. Chỉ đọc hash của bản gốc; test truy vấn dùng snapshot riêng với Read Only=True.
- Các bước “checkpoint” dưới đây chỉ kiểm tra diff/test; không stage/commit, dù workflow mặc định có hướng dẫn commit.
- Chưa chọn cách thực thi: Native hoặc Subagent-driven. Không triển khai trước khi người dùng xem kế hoạch và chọn cách thực thi.

## Review Focus

1. Cuộn chuyển lẻ đã HET hoặc lịch sử chuyển đã mất vẫn phải trừ nguồn đúng một lần; hoàn tác hợp lệ không bị trừ tiếp — Task 1, FX-SRC-01/02.
2. Cuộn khai báo chủ sở hữu khác product của nguồn không được biến mất khi chọn đúng mã cuộn/Bin; không tự chuyển tồn giữa Bin — Task 2, FX-ORPHAN-02.
3. Checkbox đổi liên tục khi gợi ý đang chờ hoặc BeginInvoke đã xếp hàng chỉ được hiển thị kết quả mới nhất, giữ text — Task 3, FX-FILTER-01/02.
4. Nút Xem của dòng lỗi đang được chọn vẫn đỏ/trắng; grid báo cáo cũ không bị vẽ theo kiểu tồn — Task 4, FX-PAINT-01/02.
5. Người dùng bấm Đồng ý sau khi DB đã đổi vẫn bị từ chối lưu; bấm Không giữ nguyên bản nháp/DB — Task 5, các ca MarkerConfirm/Conflict và FX-CONFIRM-01.

---

## Bản đồ file và thứ tự

Đường dẫn sản phẩm bên dưới tính từ root workspace. Tasks chạy theo thứ tự; Task 1/2 cùng sửa BC_TonKho_DB nên không giao sửa song song.

| File | Trách nhiệm trong đợt sửa |
|---|---|
| `DG_TonKhoBTP_v02/Database/Kho/TonKho/BC_TonKho_DB.cs` | Task 1/2: loại nguồn, số chuyển lẻ, lỗi nguồn/mã, tìm cuộn thiếu header |
| `DG_TonKhoBTP_v02/UI/Helper/AutoSearchWithCombobox/ComboBoxSearchHelper.cs` | Task 3: invalidate/refresh và hủy/Dispose an toàn |
| `DG_TonKhoBTP_v02/UI/NghiepVuKhac/Kho/UC_BCTonKho.cs` | Task 3/4: filter và vẽ nút lỗi |
| `DG_TonKhoBTP_v02/UI/NghiepVuKhac/Kho/XuatKho/Frm_ChiTietThucHienKeHoach.Execution.cs` | Task 5: nội dung xác nhận |
| `tests/UC_BCTonKho.Acceptance.cs`, `.Acceptance.ps1` | Ca lỗi/biên, chọn subset và output riêng |
| `tests/UC_BCTonKho.RealSnapshot.ps1`, `.Static.ps1` | Cho phép ghi bằng chứng vào thư mục sau sửa; đối chiếu thật/kiến trúc |
| `tests/UC_BCTonKho.Retest.Report.ps1` (mới) | Tổng hợp kết quả thực, không sửa generator/báo cáo mốc trước sửa |
| `docs/verification/UC_BCTonKho_Retest_Report.md` (mới) | Báo cáo A–G sau sửa |

Không cần file sản phẩm mới, sửa csproj hoặc Designer. Model hiện đủ định danh và cờ lỗi; ưu tiên metadata truy vấn cục bộ, không mở rộng model nếu không cần.

## Lệnh chuẩn

```powershell
$InventoryMsBuild='C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe'
& $InventoryMsBuild DG_TonKhoBTP_v02/DG_TonKhoBTP_v02.csproj /t:Build /p:Configuration=Debug /nologo /v:quiet
```

Đặt `$InventoryEvidence` thành đường dẫn tuyệt đối tới `docs/verification/uc-bctonkho-retest-20261030`; đặt `$InventorySnapshot` thành đường dẫn tuyệt đối tới bản sao đã có `docs/verification/uc-bctonkho-evidence/configured-snapshot.db`. Không gán hai biến này thành đường dẫn database gốc.

### Task 1: Sửa nhận diện nguồn và tính chuyển lẻ — INV-001 và INV-003

**Files:** Modify `BC_TonKho_DB.cs`, `tests/UC_BCTonKho.Acceptance.cs`, `tests/UC_BCTonKho.Acceptance.ps1`.

**Interfaces:**
- Giữ `TimGoiYAsync(string keyword, bool gomHetTon, CancellationToken ct) : Task<DataTable>` và `LayChiTietTonAsync(BCTonKho_LoaiTimKiem loai, long? id, string giaTri, bool gomHetTon, CancellationToken ct) : Task<BCTonKho_KetQua>`.
- Giữ `ReadProduct(SQLiteConnection conn, SQLiteTransaction tx, long productId, CancellationToken ct) : List<BCTonKho_DongTon>`.
- Thêm tham số script `-EvidenceDirectory`, `-SnapshotPath`, `-CaseFilter` (regex trên ID ca). Driver truyền chúng vào harness; mặc định output là thư mục retest mới, snapshot là bản sao trước sửa. Không chạy lại vào thư mục bằng chứng cũ.
- `Fresh`, `Test`, `Row`, `Reel`, `P`, `N`, `Eq` trong harness tiếp tục dùng fixture riêng. Chạy interactive vẫn giữ được bằng argument rõ ràng.

- [ ] **Step 1: Chuẩn bị chạy lại an toàn và thêm test nguồn.** Ghi hash DB gốc; lưu manifest các đầu vào sản phẩm hiện tại và diff đang có. Đọc using-git-worktrees ở thời điểm thực thi; không dùng checkout HEAD sạch làm mất mã chưa commit đã nghiệm thu. Nếu cô lập, phải sao chép đúng đầu vào working tree và xác minh hash trước khi sửa. Output red/green vào các thư mục con của retest, giữ nguyên evidence trước sửa.
- [ ] **Step 2: Giữ ca 10-09/10-12/10-13 và thêm FX-SRC-01/02/03.** Mất lịch sử id 81 phải thỏa:

```csharp
Eq(6, N(Row(), "SoLuong"));
Eq(true, P(Row(), "LaBatThuong"));
Eq(true, P(Reel(301), "LaBatThuong"));
Eq(null, P(Reel(301), "ChieuDaiBanDau"));
```

FX-SRC-01: chuyển cuộn 301 sang HET, mốc bằng nhau, xóa lịch sử chuyển; tồn chẵn vẫn 6. FX-SRC-02: tạo rồi hoàn tác chuyển bằng nghiệp vụ fixture B3/B2 hiện có; kiểm tra TonCuonLe đã gỡ thì báo cáo trả đúng tồn phục hồi, không đổi SoCuon nhập. FX-SRC-03: nguồn nhập trực tiếp có lịch sử CUON_CHAN mâu thuẫn hoặc chỉ một mốc nguồn; báo lỗi, không nhận sai là cuộn phát sinh hợp lệ. Mã đúng không báo nhầm, mã QR phát sinh khác Bin không bị ép bằng Bin.
- [ ] **Step 3: Chạy RED.** Build, rồi chạy Acceptance với CaseFilter `^(10-(09|12|13)$|FX-SRC-)`; lưu log. Kỳ vọng các lỗi đã biết FAIL vì 7 cuộn/không cảnh báo, không phải do lỗi compile/harness.
- [ ] **Step 4: Sửa SQL và ReadProduct tại BC_TonKho_DB.** WholeSql đếm TonCuonLe_ID hiện còn liên kết nguồn chẵn, bao gồm HET. PartialSql đọc thêm mốc nguồn, có/không lịch sử CUON_CHAN và chủ sở hữu theo nguồn. Phân loại nguồn theo hai mốc nhập; lịch sử thiếu/mâu thuẫn chỉ tạo cờ lỗi, không thay loại nguồn. Đánh dấu cả nguồn chẵn và dòng lẻ khi thiếu lịch sử hoặc sai liên kết. Giữ ABS mốc hiện tại; không clamp số âm. So mã lẻ nhập trực tiếp với Bin nguồn bằng Trim + Ordinal, giữ nguyên dữ liệu hiển thị.
- [ ] **Step 5: Chạy GREEN và hồi quy nguồn.** Chạy cùng subset và nhóm 04/05/06/07/08. Kỳ vọng tất cả ca trong subset PASS, FIFO/đặt chỗ không đổi, không SQL ghi/schema mới. Ca của lỗi chưa sửa không được tính vào checkpoint này.
- [ ] **Step 6: Checkpoint diff.** Chỉ các file đã nêu thay đổi; đối chiếu source input với manifest, không ghi DB gốc, không commit.

### Task 2: Giữ cuộn thiếu nguồn trong tìm kiếm/chi tiết — INV-002

**Files:** Modify `BC_TonKho_DB.cs`, `tests/UC_BCTonKho.Acceptance.cs`.

**Interfaces:** Tiêu thụ hai API và cách phân loại nguồn của Task 1; giữ cấu trúc bảng gợi ý `Loai:int, Id:long nullable, GiaTri:string`, model kết quả và các ID nguồn hiện có.

- [ ] **Step 1: Thêm FX-ORPHAN-01/02/03 và giữ 10-08.** Xóa header nhập fixture id 14: detail chọn cuộn 303, Bin 104 và tên đều có đúng một dòng cuộn 303, dài 396, LaBatThuong=true, chiều dài ban đầu null. Gợi ý mã cuộn/Bin/tên đều truy xuất được. FX-ORPHAN-01 xóa TTCuonDay nhưng giữ chủ sở hữu; FX-ORPHAN-02 cho cuộn thuộc product 2 nhưng nguồn thuộc product 1, chọn cuộn phải trả chính nó, không mất dữ liệu và cảnh báo hai phía; FX-ORPHAN-03 cuộn orphan hết tồn vẫn hiện khi checkbox false. Không gợi ý TTThanhPham không có nguồn/cuộn; không trùng TonCuonLe_ID.
- [ ] **Step 2: Chạy RED.** CaseFilter `^(10-08$|FX-ORPHAN-)`; kỳ vọng lỗi dữ liệu bị loại bởi join, không phải fixture không tạo được.
- [ ] **Step 3: Sửa SuggestionSql, ProductIdsSql, PartialSql và OrphanPartialSql.** Gợi ý/giải quyết product từ chủ sở hữu còn xác định và tồn tại nguồn nhập hoặc TonCuonLe; giữ NhapKho=1, Temp=0. Dòng lẻ lấy chủ sở hữu báo cáo từ TonCuonLe.TTThanhPham_ID, đối chiếu riêng chủ sở hữu nguồn. Hai nhánh thường/orphan loại trừ nhau để không trùng ID. Giữ mã/mốc/ID khai báo khi thiếu nguồn; không tự gán Bin hoặc sản phẩm mới.
- [ ] **Step 4: Chạy GREEN và lọc định danh.** Subset trên cùng 02, 03, 09-01/02 và 10-10 phải PASS; xác minh chọn Bin không kéo đặt chỗ chưa có nguồn của Bin khác vào.
- [ ] **Step 5: Checkpoint diff và chỉ đọc.** Ép connection Read Only, cả hai API vẫn chạy; hash fixture truy vấn không đổi. Không commit.

### Task 3: Refresh dropdown theo checkbox — UI-001

**Files:** Modify `ComboBoxSearchHelper.cs`, `UC_BCTonKho.cs`, `tests/UC_BCTonKho.Acceptance.cs`.

**Interfaces:** Thêm `public void InvalidateSuggestions()` và `public Task RefreshSuggestionsAsync()` trên ComboBoxSearchHelper. Invalidate hủy truy vấn/BeginInvoke cũ, xóa Items và đóng dropdown nhưng giữ nguyên text, không phát Cleared/ItemSelected. Refresh dùng text hiện tại, không debounce bổ sung, không Waiting; cancellation/dispose được xử lý trong helper. Các caller cũ không gọi API mới giữ nguyên behavior.

- [ ] **Step 1: Giữ 09-04, thêm FX-FILTER-01/02/03.** 09-04 kỳ vọng số item hết tồn `1→0` ngay khi bỏ checkbox. FX-FILTER-01 dùng query stub trả chậm, toggle true/false/true rồi hoàn thành theo thứ tự ngược: chỉ bộ lọc cuối được render. FX-FILTER-02 hủy sau khi callback đã xếp BeginInvoke, text giữ nguyên và callback không mở dropdown cũ. FX-FILTER-03 đã chọn item thì giữ ID/text, chỉ reload detail; text rỗng hoặc Dispose không query/cập nhật control đã hủy. Đếm Cleared/ItemSelected không tăng vì refresh.
- [ ] **Step 2: Chạy RED.** CaseFilter `^(09-04$|FX-FILTER-)`, kỳ vọng lỗi thiếu API/stale items được ghi nhận sau khi harness compile đúng.
- [ ] **Step 3: Thêm hai API và guard lifecycle trong helper.** Dùng cancellation/generation để vô hiệu hóa query lẫn BeginInvoke. Kiểm tra IsDisposed/Disposing trước khi đọc/ghi control và trước BeginInvoke; không nuốt lỗi bằng thao tác tiếp trên control đã Dispose.
- [ ] **Step 4: Sửa OnTonKhoFilterChanged(object sender, EventArgs e).** Invalidate gợi ý mọi lần đổi. Nếu có selection thì gọi ReloadTonKhoAsync hiện tại; nếu chưa chọn và text không rỗng thì await RefreshSuggestionsAsync. Giữ version/cancellation của detail.
- [ ] **Step 5: Chạy GREEN và regression helper.** Subset trên, các ca 02-12/09-03/12 và `powershell -STA -File tests/UC_BaoCaoDungMay.ComboBox.Tests.ps1` phải PASS; không Waiting khi chỉ gõ/refresh gợi ý. Không commit.

### Task 4: Vẽ nút Xem đúng style dòng lỗi — UI-002

**Files:** Modify `UC_BCTonKho.cs`, `tests/UC_BCTonKho.Acceptance.cs`.

**Interfaces:** Thêm `private void OnTonKhoCellPainting(object sender, DataGridViewCellPaintingEventArgs e)`; đăng ký đúng một lần trong InitializeTonKhoSearch, hủy đăng ký khi Dispose. Chỉ can thiệp khi _showingInventory, row hợp lệ có Tag BCTonKho_DongTon.LaBatThuong và cột tên LichSu.

- [ ] **Step 1: Giữ 10-16, thêm FX-PAINT-01/02.** Bitmap thân nút thường/selected phải RGB(177,48,59), text trắng; tooltip có nguyên nhân. Bấm Xem không ghi DB/crash. Chuyển ShowResult cũ rồi render lại tồn: không sót custom paint/style, vẫn đúng 10 cột.
- [ ] **Step 2: Chạy RED.** CaseFilter `^(10-16$|FX-PAINT-)`; trên bản cũ pixel thân nút là 253/253/253.
- [ ] **Step 3: Vẽ cell nút lỗi tại handler mới.** Giữ DataGridViewButtonColumn và text Xem, vẽ nền/viền/text với màu danger/trắng theo style đã duyệt; không can thiệp row header, header hoặc báo cáo cũ. Không bổ sung hành động nghiệp vụ cho nút.
- [ ] **Step 4: Chạy GREEN và mở ảnh render.** Subset trên cùng 10-11/11/13-01 phải PASS. Kiểm tra trực quan ảnh ở output retest, không chỉ DefaultCellStyle. Không commit.

### Task 5: Đưa tác động preview vào xác nhận — REG-001

**Files:** Modify `Frm_ChiTietThucHienKeHoach.Execution.cs`; Test `tests/ExecutionEdit.DesignedForm.Tests.ps1` và test bổ sung riêng nếu cần lưu evidence.

**Interfaces:** Giữ `ConfirmationText(ExecutionEditRequest request, ExecutionEditPreview preview) : string`, `Ask(string text, MessageBoxButtons buttons) : DialogResult`, `SaveAsync() : Task<bool>` và giao dịch Save hiện có.

- [ ] **Step 1: Dùng hai ca MarkerConfirm/Undo hiện có; thêm FX-CONFIRM-01 cho batch.** Popup phải có mã LE-001, mốc mới 1650, tồn giữ nguyên 650; hoàn tác phải nêu tồn phục hồi 800. Batch nhiều thay đổi chỉ một popup. Không: giữ draft và hash/data; chỉ sửa người thực hiện không thêm popup mới.
- [ ] **Step 2: Chạy RED.** `powershell -NoProfile -ExecutionPolicy Bypass -STA -File tests/ExecutionEdit.DesignedForm.Tests.ps1 -Case MarkerConfirm`; kỳ vọng FAIL vì popup chỉ hỏi chung. Chạy thêm `-Case Undo` để ghi bằng chứng lỗi cũ.
- [ ] **Step 3: Sửa duy nhất nội dung Ask trong SaveAsync.** Dùng `_batchImpact` vừa tính từ ConfirmationText(request, preview), thêm dòng trống và câu hỏi hiện tại. Giữ điều kiện cần xác nhận, snapshot request, generation/version checks và hành vi Cancel.
- [ ] **Step 4: Chạy GREEN.** Chạy toàn bộ DesignedForm (12 ca hiện có), Corrections, UndoPlan, Integrity và ParentDraft. MarkerConfirm đã có tình huống version đổi trong lúc xác nhận: vẫn phải từ chối ghi. Không sửa Preview/Save/Undo để làm các assertion PASS. Không commit.

### Task 6: Nghiệm thu lại và báo cáo bằng chứng mới

**Files:** Modify `tests/UC_BCTonKho.RealSnapshot.ps1`, `tests/UC_BCTonKho.Static.ps1`; Create `tests/UC_BCTonKho.Retest.Report.ps1`, `docs/verification/UC_BCTonKho_Retest_Report.md`; cập nhật test cũ chỉ khi cần đúng scope/revision, không thay nghiệp vụ.

**Interfaces:** Hai script đối chiếu nhận `-EvidenceDirectory`; RealSnapshot thêm `-SnapshotPath`, chỉ mở bản sao với Read Only/FailIfMissing. Generator retest đọc TSV/log/exit code thực và manifest vào báo cáo mới; không hardcode PASS/FAIL hoặc số ca. Static kiểm tra 8 nhóm SQL nếu vẫn giữ cấu trúc đó nhưng không coi số truy vấn là yêu cầu nghiệp vụ; phần UI mới được tìm theo phương thức, không dựa khoảng dòng cố định 50..210.

- [ ] **Step 1: Chạy full build và acceptance.** Lệnh build chuẩn, rồi `powershell -File tests/UC_BCTonKho.Acceptance.ps1 -EvidenceDirectory $InventoryEvidence -SnapshotPath $InventorySnapshot`. Kỳ vọng tất cả ca lỗi/biên và 84 ca gốc PASS; nếu lỗi ngoài phạm vi, báo riêng, không tự sửa rộng.
- [ ] **Step 2: Chạy đối chiếu thật/kiến trúc.** `powershell -File tests/UC_BCTonKho.RealSnapshot.ps1 -EvidenceDirectory $InventoryEvidence -SnapshotPath $InventorySnapshot`; `powershell -File tests/UC_BCTonKho.Static.ps1 -EvidenceDirectory $InventoryEvidence`. Mẫu thật phải là 6/100/20/396; API snapshot/readonly, tham số hóa, lọc đúng ID và hash dữ liệu vẫn đạt.
- [ ] **Step 3: Chạy các script hồi quy cũ và phân loại bằng chứng.** Tái chạy 14 script trong regression-assessment.tsv, log vào thư mục retest. Nếu sửa test Undo thì chọn PartialReelId đã phát sinh thay scope quy cách; nếu sửa NguoiCat thì nạp ExpectedExecutionRevision từ context hiện tại. Inventory.SaveFlow phải có fixture xác định đúng các tiền điều kiện, không áp số hardcode cho snapshot khác. Không sửa các assert số liệu nghiệp vụ để PASS; phần chưa tạo được fixture đúng phải giữ BLOCKED và nêu rõ.
- [ ] **Step 4: Thử GUI desktop trên form fixture riêng.** Gõ có/không dấu, chọn bằng chuột/Down/Enter, toggle checkbox, bấm Xem, kiểm tra xác nhận và xuất báo cáo vào thư mục test. Nếu công cụ không có quyền, ghi BLOCKED; không dùng ảnh DrawToBitmap thay bằng chứng desktop.
- [ ] **Step 5: Rà soát thay đổi toàn bộ và xử lý phát hiện trong phạm vi.** Rà tính đúng nguồn, dữ liệu mồ côi, filter race/Dispose, style và preview/version. Đối chiếu SQL độc lập, mở ảnh render và kiểm tra không đổi schema/DB gốc. Hướng thực thi quyết định cách review; không commit.
- [ ] **Step 6: Xuất báo cáo A–G sau sửa.** Ghi commit nền + working tree, môi trường, ca PASS/FAIL/BLOCKED/NOT RUN, từng lỗi trước/sau, điểm có bằng chứng và hash hai DB gốc. Giữ nguyên báo cáo trước sửa. Chỉ kết luận ĐẠT nếu đạt ngưỡng 95/100, mọi nghiệp vụ trọng yếu PASS và không còn Critical/High; chưa đủ bằng chứng thì nêu đúng giới hạn.

## Self-review kế hoạch

- [x] Spec coverage: mục 3/5 → Task 1; mục 4 → Task 2; mục 6 → Task 3; mục 7 → Task 4; mục 8 → Task 5; mục 9–11 và ràng buộc → Task 6/toàn cục.
- [x] Mỗi task có ca lỗi trước sửa, thay đổi được giới hạn và cách chạy xác minh sau sửa; không có bước commit.
- [x] API public/internal hiện có giữ chữ ký; hai API helper mới được định nghĩa ở Task 3 và chỉ Task 3 tiêu thụ trong UI.
- [x] Năm Review Focus đều có test định danh gắn với task sở hữu.
- [x] Không lấp kết quả chưa chạy bằng PASS; báo cáo mới không viết đè bằng chứng ban đầu.

**Trạng thái:** kế hoạch đã tự rà soát; chờ người dùng xem và chọn Native hoặc Subagent-driven trước triển khai. Đây chưa phải kết quả sửa mã.
