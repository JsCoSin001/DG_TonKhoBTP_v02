# Inventory Report Row Details Implementation Plan

> **For agentic workers** — Required sub-skill for execution is `superpowers:subagent-driven-development` or `superpowers:executing-plans`. Use `superpowers:test-driven-development` for the test cycle. Track steps with `- [ ]` checkboxes.

**Goal** Mở cửa sổ giải thích đúng dòng báo cáo tồn đã chọn và hiển thị lịch sử cắt từ mới đến cũ.

**Architecture** Mở rộng `BCTonKho_ExcelDong` để giữ dữ liệu đối chiếu từ cùng giao dịch đọc với tổng tồn. Nút **Xem** lấy đối tượng đã gắn với dòng bảng và đưa dữ liệu đó vào cửa sổ chỉ đọc.

**Tech Stack** C# 8, .NET Framework 4.8, WinForms, System.Data.SQLite, Windows PowerShell 5.1.

**Spec** [Thiết kế đã duyệt](../specs/2026-11-09-chi-tiet-dong-bao-cao-ton-kho-design.md).

## Global Constraints

- Điểm vào là cột `Button_ExcelMoi` trong chế độ **Báo cáo tồn (Bảng)**. Chế độ Excel giữ tập cột dữ liệu và chuỗi mã LOT đang xuất.
- **Nhiều LOT** gồm các `MaBin` đang góp tồn chẵn của cùng **sản phẩm + chiều dài mỗi cuộn**. **Đặt trước** giải thích đến cấp kế hoạch.
- Đọc qua `DB_Base.OpenConnection()` và giao dịch đọc hiện hành. Fixture chạy trên tệp tạm. Bản sao kiểm thử được mở chỉ đọc.
- Tạo thư mục làm việc Git riêng ở bước thực thi theo `superpowers:using-git-worktrees`. Các thay đổi đang có ở `UC_BCTonKho.Designer.cs`, dự án và thư mục nhập kho thuộc thư mục làm việc ban đầu. Khôi phục gói có sẵn bằng MSBuild với `RestorePackagesConfig=true` khi thư mục làm việc mới cần phụ thuộc.

## Review Focus

- Nhiều dòng nhập có cùng `MaBin`, mã có số cuộn bằng 0 và nguồn thiếu mã → đúng số mã hiển thị, mọi khoản góp tồn có thể đối chiếu. Bài 1.
- Một nhóm cắt có hai yêu cầu chưa cắt và một kế hoạch đặt ở quy cách khác → nhóm đóng góp một cuộn, nhu cầu được tách đúng quy cách. Bài 1.
- Dòng lẻ có mốc giảm và đặt trước vượt tồn, hoặc dòng chỉ có kế hoạch → số âm và cảnh báo hiện đúng phép tính. Bài 1 và Bài 3.
- Hai lần cắt trùng thời điểm, cùng `MaBin` có nhiều cuộn vật lý và một quy cách khác → thứ tự ổn định và lịch sử đúng tập nguồn. Bài 2.
- Dữ liệu đổi sau khi tải, bảng đã sắp xếp lại hoặc chuyển sang chế độ khác → nút mở đúng dòng và đúng dữ liệu đã tải. Bài 2 và Bài 3.

## Sơ đồ tệp

| Tệp | Trách nhiệm |
|---|---|
| `DG_TonKhoBTP_v02/Models/Kho/TonKho/BCTonKho_Model.cs` | Dòng tồn và dữ liệu giải thích nguồn, kế hoạch, lần cắt. |
| `DG_TonKhoBTP_v02/Database/Kho/TonKho/BC_TonKho_DB.cs` | Tính tồn và nạp chi tiết trong cùng giao dịch đọc. |
| `DG_TonKhoBTP_v02/UI/NghiepVuKhac/Kho/TonKho/Frm_ChiTietDongTonKho.cs` | Cửa sổ chỉ đọc, chọn LOT và hiển thị phần đối chiếu. |
| `DG_TonKhoBTP_v02/UI/NghiepVuKhac/Kho/TonKho/UC_BCTonKho.cs` và `DG_TonKhoBTP_v02/DG_TonKhoBTP_v02.csproj` | Gắn cửa sổ vào nút của báo cáo và đăng ký tệp Form. |
| `tests/UC_BCTonKho.RowDetails.Tests.ps1` | Kiểm tra SQL qua API thực và thao tác WinForms qua điểm vào của báo cáo. |

### Bài 1 — Tồn theo nguồn và đặt trước theo kế hoạch

**Tệp** `BCTonKho_Model.cs`, `BC_TonKho_DB.cs` và `tests/UC_BCTonKho.RowDetails.Tests.ps1` theo sơ đồ trên.

**Giao diện giữa các bước** `BC_TonKho_DB.LayTonKhoExcelMoi(bool kemChiTiet = false)` trả `List<BCTonKho_ExcelDong>`. Khi `kemChiTiet` là `true`, `BCTonKho_ExcelDong.ChiTiet` chứa `BCTonKho_ChiTietDong` với ba danh sách chỉ đọc `Nguon`, `LotGop` và `DatTruoc`. `BCTonKho_ExcelDong.LotHienThiBang` trả mã hiện có hoặc **Nhiều LOT** theo số mã góp tồn. `MaBinLot` là dữ liệu xuất Excel.

- [ ] **Viết kiểm thử thất bại.** Tạo `tests/UC_BCTonKho.RowDetails.Tests.ps1` nhận `-Suite Data|Ui|All` và `-SnapshotPath` tùy chọn. Dùng `System.Data.SQLite.dll` và `DG_TonKhoBTP_v02.exe` trong `DG_TonKhoBTP_v02/bin/Debug`, gọi API nội bộ qua phản chiếu, tạo cơ sở dữ liệu tạm từ `tests/fixtures/execution-edit/base-schema.sql`, thêm `DanhSachMaSP.Ten_KhongDau`, rồi chạy `tests/UC_BCTonKho.Acceptance.seed.sql`. Khôi phục cấu hình kết nối, dọn tệp tạm trong `finally` và dừng với lỗi khi một điều kiện sai. Thêm các ca sau.

  - `WholeSourcesAndPlans` — Đặt yêu cầu lấy của quy cách 41 thành 3 và thêm một nhóm chưa gắn cuộn có hai yêu cầu cắt chưa hoàn tất. Dòng `QA-P1` ở 200 m có `SoCuon=9`, tồn `1800`, đặt trước `400` gồm `1` cuộn phải lấy và `1` cuộn để cắt, khả dụng `1400`. Nguồn `E1-265081/5-03-00` có `10/2/2/6` và `QA-B` góp `3`. Thêm một kế hoạch góp `400 m` ở quy cách 400 m và kiểm tra nó thuộc dòng 400 m.
  - `ContributingLotsAndMissingCode` — Thêm nguồn cùng `MaBin` đầu tiên góp `2` cuộn, mã mới còn `0`, và nguồn có mã trống góp `1`, tất cả ở quy cách 200 m. Danh sách chỉ có hai mã đặt tên với số góp `8` và `3`. Nguồn thiếu mã góp thêm `1` để tổng bằng `12`. `LotHienThiBang` bằng **Nhiều LOT** và `MaBinLot` là `E1-265081/5-03-00 | QA-B`.
  - `PartialReservationsAndPlanOnly` — Với cuộn `QA-DIRECT` id `303` có mốc `996/600`, thêm một kế hoạch đặt `300 m` vào khoản `150 m` có sẵn. Mong đợi `1` cuộn, tồn `396`, đặt trước `450`, khả dụng `-54`, hai khoản theo kế hoạch và cảnh báo vượt tồn. Thêm sản phẩm chỉ có một yêu cầu lấy `1 × 200 m` và xác nhận tồn `0`, đặt trước `200`, khả dụng `-200` cùng danh sách LOT rỗng.
  - `ReportProjectionParity` — So sánh mã, số cuộn, số mét, mốc và cảnh báo giữa `LayTonKhoExcelMoi(true)` và `LayTonKhoExcelMoi(false)` trên cùng fixture. `MaBinLot` khớp ở cả hai lần đọc. Phần chi tiết chỉ có trong kết quả `true`.

  Các khẳng định chính của `WholeSourcesAndPlans` và `PartialReservationsAndPlanOnly`, với `Assert-Equal` nhận giá trị thực tế trước giá trị mong đợi.

  ```powershell
  Assert-Equal $whole.SoCuon 9
  Assert-Equal $whole.ChieuDaiTonThucTe 1800
  Assert-Equal $whole.ChiTiet.DatTruoc[0].SoCuonConPhaiLay 1
  Assert-Equal $whole.ChiTiet.DatTruoc[0].SoCuonDanhDeCat 1
  Assert-Equal $whole.ChieuDaiTonDatTruoc 400
  Assert-Equal $whole.ChieuDaiTonKhaDung 1400
  Assert-Equal $partial.ChieuDaiTonThucTe 396
  Assert-Equal $partial.ChieuDaiTonDatTruoc 450
  Assert-Equal $partial.ChieuDaiTonKhaDung -54
  Assert-Equal $planOnly.ChieuDaiTonKhaDung -200
  ```

- [ ] **Xác nhận thất bại.** Build bằng MSBuild với `/t:Build /p:Configuration=Debug /p:Platform="Any CPU" /v:minimal`. Chạy `powershell -NoProfile -STA -ExecutionPolicy Bypass -File tests/UC_BCTonKho.RowDetails.Tests.ps1 -Suite Data`. Kỳ vọng báo thiếu phương thức nhận `bool` hoặc dữ liệu chi tiết, sau khi khởi tạo fixture thành công.
- [ ] **Giữ số liệu nguồn.** Bổ sung `BCTonKho_NguonTon` với `TTCuonDayId`, `MaBin`, `SoCuonNhap`, `SoCuonDaLay`, `SoCuonChuyenLe`, `SoCuonCon` và `SoCuonGop`. `BCTonKho_LotGop` có `MaBin`, `SoCuon` và `SoMet`. Dùng các giá trị đang đọc từ `sqlCuon` và điều kiện góp tồn hiện có. Nhóm mã đã cắt khoảng trắng bằng `StringComparer.OrdinalIgnoreCase`, giống `ThemMaBin`. Giữ nguồn thiếu mã trong `Nguon` để cửa sổ đối chiếu riêng.
- [ ] **Giữ số liệu kế hoạch.** Bổ sung `BCTonKho_DatTruocKeHoach` với `KeHoachId`, `MaKeHoach`, `SoCuonConPhaiLay`, `SoCuonDanhDeCat` và `SoMet`. Với `sqlDatCuon`, đọc hai thành phần trước khi cộng và nhân chiều dài. Áp dụng `MAX(0, …)` cho từng quy cách rồi gom theo mã định danh kế hoạch trong đúng dòng báo cáo. Với `sqlDatLe`, giữ nhóm theo mã cuộn và sản phẩm, thêm nhóm theo mã định danh kế hoạch để lấy từng khoản. Duy trì điều kiện kế hoạch hoạt động, chưa cắt và đối chiếu sản phẩm. Số liệu chi tiết và số cộng vào dòng lấy từ cùng kết quả đọc.
- [ ] **Xác nhận và commit.** Build lại, chạy `-Suite Data` và kiểm tra bốn ca trên đều `PASS`. Chạy `git diff --check` rồi commit các tệp của Bài 1 với `feat: retain inventory report row calculations`.

### Bài 2 — Lịch sử cắt đúng cuộn và cùng dữ liệu báo cáo

**Tệp** `BCTonKho_Model.cs`, `BC_TonKho_DB.cs` và `tests/UC_BCTonKho.RowDetails.Tests.ps1`.

**Giao diện giữa các bước** `BCTonKho_ChiTietDong.LichSuCat` là `IReadOnlyList<BCTonKho_LanCat>`. `BCTonKho_LanCat` có `Id`, `TonCuonLeId`, `TTCuonDayId`, `MaCuon`, `LoaiNguon`, `ThoiGian`, `NguoiThucHien`, `MaKeHoach`, `ChieuDaiCat`, `SoDauTruoc`, `SoCuoiTruoc`, `SoDauCat`, `SoCuoiCat`, `SoDauSau`, `SoCuoiSau`.

- [ ] **Viết kiểm thử thất bại.** Thêm `CutHistoryBySourceAndReel`. Trong fixture, đặt thời gian lần 81 sớm hơn 82. Thêm lần 83 và 84 thuộc cuộn 302, trùng thời gian với nhau và mới hơn 82. Thêm một lần cắt thuộc nguồn 203 ở quy cách 400 m. Mong đợi lịch sử nguồn 201 là `84,83,82,81`, cuộn 302 là `84,83,82`, cuộn 303 có lịch sử rỗng, và lần cắt của nguồn 203 chỉ nằm trong phạm vi quy cách 400 m. Kiểm tra kiểu nguồn, mã cuộn, mã kế hoạch và các cặp mốc giữ đúng giá trị fixture.

  ```powershell
  Assert-Equal ($whole200.ChiTiet.LichSuCat.Id -join ',') '84,83,82,81'
  Assert-Equal ($partial302.ChiTiet.LichSuCat.Id -join ',') '84,83,82'
  Assert-Equal $partial303.ChiTiet.LichSuCat.Count 0
  ```

- [ ] **Viết kiểm thử ảnh dữ liệu.** Thêm `LoadedRowSurvivesDatabaseChange`. Đọc dòng trước khi thêm một lần cắt và cập nhật mốc cuộn trên fixture. Đối tượng đã tải giữ nguyên tồn, mốc và số lần cắt. Kết quả tải lại thấy lần cắt mới và mốc mới. Nếu có `-SnapshotPath`, mở bản sao ở chế độ chỉ đọc và kiểm tra nguồn `E1-265081/5-03-00` còn `6 × 200 = 1200`, có bốn lần cắt, và cuộn id `6` có ba lần theo thứ tự giảm dần. So sánh SHA-256 của bản sao trước và sau.
- [ ] **Xác nhận thất bại.** Build rồi chạy `-Suite Data`. Hai ca mới báo thiếu dữ liệu lịch sử. Các ca của Bài 1 vẫn `PASS`.
- [ ] **Đọc và gắn lịch sử.** Trong `LayTonKhoExcelMoi(true)`, đọc `LichSuCat` theo tập nguồn và cuộn liên quan trước khi giao dịch đọc kết thúc. Nối `TonCuonLe` theo mã định danh và nối các bảng kế hoạch để lấy mã kế hoạch. Giữ lịch sử còn hiệu lực kể cả khi dữ liệu mô tả kế hoạch thiếu. Tra bằng `TTCuonDay_ID` cho cuộn chẵn và `TonCuonLe_ID` cho cuộn lẻ. Giữ thứ tự `ThoiGianThucHien DESC, id DESC` trên mỗi dòng. Cấu hình đọc theo tập bản ghi, dùng tham số nếu truyền danh sách mã định danh.
- [ ] **Xác nhận và commit.** Build, chạy `-Suite Data` kèm `-SnapshotPath` trỏ đến bản sao kiểm thử trong thư mục làm việc ban đầu và kiểm tra mọi ca `PASS`. Chạy `git diff --check` rồi commit các tệp của Bài 2 với `feat: include cutting history in inventory report snapshot`.

### Bài 3 — Cửa sổ Xem trên dòng báo cáo

**Tệp** `Frm_ChiTietDongTonKho.cs`, `UC_BCTonKho.cs`, `DG_TonKhoBTP_v02.csproj` và `tests/UC_BCTonKho.RowDetails.Tests.ps1`.

**Giao diện giữa các bước** `internal Frm_ChiTietDongTonKho(BCTonKho_ExcelDong dong)` nhận đối tượng dòng. `UC_BCTonKho.TonKho_Excel(bool flgExcel = false)` giữ điểm vào và gọi `LayTonKhoExcelMoi(kemChiTiet: !flgExcel)`.

- [ ] **Viết kiểm thử giao diện thất bại.** Trong `-Suite Ui`, dùng fixture của `WholeSourcesAndPlans` và phần chuẩn bị cuộn lẻ của `PartialReservationsAndPlanOnly`. Dựng `UC_BCTonKho` trong Form chạy STA. Gọi nút `btnXemTonKho` và chờ cột `Button_ExcelMoi` với thời hạn hữu hạn. Sắp xếp bảng, kích hoạt nút của dòng `QA-P1` ở 200 m rồi đóng cửa sổ từ vòng lặp thông điệp kiểm thử. Khẳng định đúng tên sản phẩm, số liệu tóm tắt, cột **Nhiều LOT**, hai mã góp tồn và tổng đặt trước. Chọn từng mã LOT để kiểm tra nguồn và lịch sử thay đổi từ hai lần cắt của mã đầu tiên sang rỗng ở `QA-B`. Tổng dòng và bảng đặt trước giữ đúng giá trị `1800/400/1400`. Chạy thêm ca một LOT, cuộn lẻ `303` có cảnh báo `-54`, lịch sử rỗng, dòng chỉ đặt trước, và dòng lẻ bất thường bị thiếu bản ghi cuộn. Chuyển sang bảng tìm tồn rồi kích hoạt cột nút của bảng đó để kiểm tra cửa sổ này chỉ mở từ `Button_ExcelMoi`.

  ```powershell
  Assert-Equal $clickedRow.Cells['MaBinLot_ExcelMoi'].Value 'Nhiều LOT'
  Assert-Equal ($visibleLots -join '|') 'E1-265081/5-03-00|QA-B'
  Assert-Equal $historyCountFirstLot 2
  Assert-Equal $historyCountSecondLot 0
  Assert-Equal $reserveBeforeSwitch 400
  Assert-Equal $reserveAfterSwitch 400
  ```

- [ ] **Xác nhận thất bại.** Build rồi chạy `-Suite Ui`. Kỳ vọng cột nút tồn tại nhưng ca mở chi tiết báo hết thời gian chờ hoặc ô LOT đang chứa chuỗi mã ghép.
- [ ] **Tạo Form.** Đặt phần số liệu dòng và cảnh báo ở đầu. Với cuộn chẵn, trình bày các nguồn nhập và phép tính số cuộn. Dòng **Nhiều LOT** có danh sách chọn `MaBin`, mở đầu bằng danh sách góp tồn và chọn mã để lọc các nguồn cùng lịch sử theo mã đó. Với cuộn lẻ, mở ngay phần mốc dây và lịch sử của cuộn. Dùng bảng chỉ đọc cho lịch sử và đặt trước, tên cột tiếng Việt và định dạng số `N0` như báo cáo. Hiện trạng thái **Chưa có lịch sử cắt** cho nguồn hoặc cuộn chưa có lần cắt. Các thông tin cảnh báo vẫn đọc được khi dòng thiếu mốc, nguồn hoặc mã.
- [ ] **Nối nút.** Đăng ký `Frm_ChiTietDongTonKho.cs` trong tệp dự án. Trong `UC_BCTonKho.cs`, ghi `LotHienThiBang` vào cột LOT của bảng và giữ `MaBinLot` ở nhánh Excel. Gắn sự kiện nhấn ô một lần, kiểm tra chỉ số dòng và cột, tên cột `Button_ExcelMoi` và kiểu `BCTonKho_ExcelDong` của `Tag`, rồi mở Form với cửa sổ cha hiện tại. Dùng đối tượng gắn với dòng để kết quả đúng sau khi bảng được sắp xếp.
- [ ] **Xác nhận toàn luồng.** Build, chạy `-Suite All -SnapshotPath <đường dẫn bản sao kiểm thử>` và kiểm tra kết quả `PASS`. Đối chiếu cột LOT của dữ liệu Excel vẫn nhận danh sách mã hiện hành. Chạy hồi quy báo cáo tìm tồn trên fixture nếu kiểm thử gốc chạy được ở mốc cơ sở. Mở Form để kiểm tra độ đọc của các mốc dây và danh sách LOT trên màn hình thực tế, rồi chạy `git diff --check`.
- [ ] **Commit.** Commit các tệp của Bài 3 với `feat: open inventory report row details`. Rà soát toàn bộ thay đổi so với bản thiết kế trước khi báo hoàn tất.
