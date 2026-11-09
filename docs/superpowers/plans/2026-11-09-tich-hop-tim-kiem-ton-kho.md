# Inventory Search Integration Implementation Plan

> **For agentic workers** — Required execution skill is superpowers:executing-plans or superpowers:subagent-driven-development. Follow the test-driven-development cycle and track steps with checkboxes.

**Goal** Tích hợp tìm kiếm tồn kho theo LOT, mã cuộn và tên sản phẩm vào UC_BCTonKho, giữ cách tính báo cáo tổng và giải thích đúng dòng tìm kiếm.

**Architecture** Tìm kiếm chạy theo định danh trong BC_TonKho_DB. Cuộn chẵn phân bổ đặt trước dự kiến trên toàn bộ nguồn liên quan trước khi lọc. Bảng, lịch sử và chi tiết đặt trước dùng cùng bộ dữ liệu, còn luồng Excel nhận cùng kết quả lọc.

**Tech Stack** C# 8, .NET Framework 4.8, WinForms, System.Data.SQLite, Windows PowerShell 5.1.

**Spec** [Thiết kế đã duyệt](../specs/2026-11-09-tich-hop-tim-kiem-ton-kho-design.md).

## Global Constraints

- Nguồn A gồm các tệp chỉnh sửa cục bộ ở checkout master và cấu trúc hiện tại. Dùng Git worktree riêng để triển khai. Sao đúng trạng thái tệp sửa, xóa và chưa theo dõi cần cho build từ A vào worktree kiểm thử, gồm Designer đã có comboBox1. Kiểm tra SHA-256 của các tệp sao và lưu danh sách đối chiếu. Chỉ commit các tệp thuộc tính năng.
- Dùng phần nghiệp vụ trong ZIP làm tham chiếu để tích hợp từng đoạn. Các file sản phẩm thay đổi phải nằm đúng thư mục tương đối hiện hành. Dùng BC_TonKho_DB.cs và DB_Base. Các thao tác kiểm thử ghi dùng tệp tạm.
- Đường tính và hai đầu ra của btnXemTonKho, điểm vào xuất Word và các thao tác ghi B1/B2/B3 giữ hợp đồng hiện hành. So sánh kết quả báo cáo tổng trước và sau trên cùng bản sao dữ liệu.
- Nếu phát hiện xung đột nghiệp vụ mới ngoài X1A–X6A, dừng phần liên quan, ghi bằng chứng và báo người dùng quyết định.
- Tất cả lượt tìm kiếm dùng định danh, hủy tác vụ cũ, giới hạn sau khi xét điều kiện gợi ý và đúng một đầu ra cho mỗi lượt.

## Review Focus

- Hai sản phẩm trùng tên và một sản phẩm có nguồn ở nhiều công đoạn → gợi ý phân biệt ID, chọn ID lấy toàn bộ nguồn của đúng sản phẩm. Task 1.
- Đổi ngày hoặc trùng ngày kế hoạch, phân bổ vượt tồn và tra riêng LOT thứ hai → số tổng không bị tính trùng, chi tiết theo kế hoạch cộng đúng. Task 1.
- Cuộn lẻ khác chủ sở hữu nguồn, quy cách lỗi và LOT có số tồn bằng 0 → hiện cảnh báo đúng, giữ đặt trước của cuộn thật và không sinh tồn khả dụng giả. Task 1 và 2.
- Kết quả đến sai thứ tự, bảng đang sắp xếp, thay đổi chế độ hoặc Gồm hết tồn giữa lần tải → đúng gợi ý và đối tượng mới nhất. Task 2.
- Tích Excel rồi chọn, hủy hộp lưu hoặc đổi thao tác trong lúc tải → một đầu ra cho lượt còn hiệu lực, báo cáo tổng và Word dùng đúng đường cũ. Task 3.

## Tệp và trách nhiệm

| Tệp | Trách nhiệm |
|---|---|
| DG_TonKhoBTP_v02/Database/Kho/TonKho/BC_TonKho_DB.cs | Gợi ý theo chế độ, nạp nguồn và cuộn đúng ID, phân bổ dự kiến và chụp dữ liệu chi tiết trong giao dịch đọc. |
| DG_TonKhoBTP_v02/Models/Kho/TonKho/BCTonKho_Model.cs | Bổ sung dữ liệu cần cho dòng tìm kiếm và cửa sổ hiện có. |
| DG_TonKhoBTP_v02/UI/Helper/AutoSearchWithCombobox/ComboBoxSearchHelper.cs | Làm mới gợi ý theo lời gọi riêng từ màn hình tồn kho. |
| DG_TonKhoBTP_v02/UI/NghiepVuKhac/Kho/TonKho/UC_BCTonKho.cs | Chế độ tìm kiếm, vòng đời yêu cầu, bảng và Excel. |
| DG_TonKhoBTP_v02/UI/NghiepVuKhac/Kho/TonKho/Frm_ChiTietDongTonKho.cs | Thể hiện nguồn góp tồn và nguồn hết, trạng thái cuộn, lịch sử và đặt trước dự kiến. |
| tests/UC_BCTonKho.SearchIntegration.Tests.ps1 | Ca dữ liệu, vòng đời WinForms và xác minh đầu ra Excel. |

## Chuẩn bị và mốc hồi quy

- [ ] Khởi tạo worktree ở commit chứa kế hoạch. Lập danh sách trạng thái git và mã băm tệp của nguồn A. Áp các chỉnh sửa A vào worktree kiểm thử, kiểm tra có comboBox1, đường dẫn NhapKho đã chuyển và đăng ký Frm_ChiTietDongTonKho. Không đưa các thay đổi A vào phạm vi stage.
- [ ] Build Debug Any CPU trong worktree bằng MSBuild với /restore /p:RestorePackagesConfig=true /p:Configuration=Debug /p:Platform="Any CPU". Chạy tests/UC_BCTonKho.RowDetails.Tests.ps1 -Suite All -SnapshotPath với bản sao kiểm thử đang có. Dùng tệp kết quả tạm lưu bộ cột, số liệu và cảnh báo từ LayTonKhoExcelMoi(false) theo thứ tự ổn định để đối chiếu sau. Ghi riêng lỗi nền đã biết của harness cũ nếu tái hiện.

### Task 1 — Dữ liệu tìm kiếm, nguồn và phân bổ theo kế hoạch

**Tệp** BC_TonKho_DB.cs, BCTonKho_Model.cs và tests/UC_BCTonKho.SearchIntegration.Tests.ps1.

**Giao diện** BC_TonKho_DB.TimGoiYTheoCheDoAsync(string keyword, bool theoTen, bool gomHetTon, CancellationToken ct) trả Task<DataTable> có Loai, Id, GiaTri. BC_TonKho_DB.LayTonKhoTimKiem(BCTonKho_LoaiTimKiem loai, long id, bool gomHetTon, bool kemChiTiet, CancellationToken ct) trả List<BCTonKho_ExcelDong>. Dòng tìm kiếm bổ sung DatTruocDuKien, ChieuDaiBanDau và TrangThaiCuon. BCTonKho_ChiTietDong.LotTraCuu là danh sách MaBin được phép mở trong dòng tìm kiếm, tách khỏi LotGop. Kết quả tổng có thể dùng LotGop theo hành vi hiện tại.

- [ ] Viết fixture PowerShell -Suite Data|Ui|Excel|All, tái dùng SQL tests/fixtures/execution-edit/base-schema.sql và tests/UC_BCTonKho.Acceptance.seed.sql. Thêm cột Ten_KhongDau và CongDoan khi tạo SQLite tạm, gọi các API thực qua phản chiếu. Dọn tệp tạm trong finally và chạy báo lỗi từng ca.
- [ ] Thêm các ca gợi ý theo ID. Đặt nguồn 101 ở công đoạn 5, nguồn 102 cùng sản phẩm ở công đoạn 4, và nguồn 103 cùng tên nhưng khác ID ở công đoạn 5. Tên trả hai ID riêng. Chọn sản phẩm 1 giữ 101 và 102, không lẫn 103. Tra LOT 101 hiện cuộn 301 và 302 phát sinh. Tra mã cuộn 302 chỉ trả đúng cuộn đó. Tra từ khóa chứa %, _ và dấu nháy đúng nghĩa ký tự. Tạo trên 100 ứng viên hết tồn xếp trước một ứng viên còn tồn và xác minh ứng viên hợp lệ vẫn xuất hiện.
- [ ] Thêm các ca phân bổ. Dùng nguồn 201 và 202 mỗi nguồn còn 2 cuộn quy cách 200 m, nguồn 201 nhập sớm hơn. K1 cần 1 cuộn, K2 cần 2 cuộn, K1 có ngày sớm hơn. Đối chiếu riêng LOT 101 và 102 sau khi tra sản phẩm, thử trùng ngày theo ID, kế hoạch thiếu ngày và nhu cầu vượt tồn gắn nguồn cuối. Kiểm tra nhãn dự kiến và giữ quy cách khác tách riêng.

  ~~~powershell
  Assert-Equal $lotA.ChieuDaiTonDatTruoc 400
  Assert-Equal ($lotA.ChiTiet.DatTruoc.SoMet -join ',') '200,200'
  Assert-Equal $lotB.ChieuDaiTonDatTruoc 200
  Assert-Equal ($lotB.ChiTiet.DatTruoc.SoMet -join ',') '200'
  ~~~

- [ ] Thêm các ca dữ liệu bất thường. Nguồn 202 hết tồn hiện trong LotTraCuu khi tích Gồm hết tồn, LotGop chỉ chứa 101. Kiểm tra cuộn HET có chiều dài 0 và nguồn nhập bị thiếu vẫn có cảnh báo thích hợp. Gắn cuộn 303 của sản phẩm khác với TTCuonDay 201 và xác minh LOT 101 giữ cảnh báo cùng 150 m đặt trước theo cuộn. Thêm quy cách -200 m còn nhu cầu để kiểm tra đặt trước và khả dụng bằng 0 cùng cảnh báo. Với quy cách 200 m có nhu cầu mà thiếu nguồn chẵn, kiểm tra dòng nhu cầu theo phạm vi sản phẩm.
- [ ] Chạy -Suite Data trên mốc nền. Mong đợi lỗi do API tìm kiếm và dữ liệu đối soát chưa có.
- [ ] Thêm API trong BC_TonKho_DB.cs, dùng các nguồn và khóa đã xác minh. Gợi ý tên lấy DanhSachMaSP.id và kiểm tra công đoạn ở bước gợi ý. Dùng MaBin gốc và MaCuon theo ID ở bước tra cứu. Giữ hai quy tắc tìm kiếm có nhãn phân biệt khi mã lẻ nhập trực tiếp bằng MaBin.
- [ ] Tính nhu cầu mỗi quy cách kế hoạch với cách làm tròn của A, giữ số cần lấy và số nhóm cắt riêng. Sắp xếp kế hoạch theo ngày hợp lệ, kế hoạch thiếu ngày ở sau rồi theo ID. Phân bổ trên toàn bộ nguồn phù hợp theo ngày nhập và ID nguồn trước khi lọc. Ghi lại khoản của từng kế hoạch cho từng nguồn và đánh dấu dự kiến. Phần vượt tồn theo nguồn cuối, nhu cầu không có nguồn theo sản phẩm, quy cách không dương chỉ ghi cảnh báo. Với cuộn lẻ dùng cuộn vật lý để tìm các khoản đặt trước và đối chiếu sản phẩm của cuộn.
- [ ] Dựng dòng theo cùng quy tắc báo cáo tổng, giữ nguyên điểm vào và kết quả LayTonKhoExcelMoi. Khi kemChiTiet bật, tái dùng cơ chế đọc lịch sử của A trong giao dịch đọc, gắn theo ID nguồn đã nối hoặc ID cuộn. Lấy chiều dài ban đầu và trạng thái từ dữ liệu có căn cứ. Tạo LotTraCuu theo phạm vi tìm kiếm và giữ LotGop cho phép tính nhãn.
- [ ] Build lại và chạy -Suite Data. Đối chiếu đầu ra tổng với mốc nền, chạy git diff --check rồi commit phần dữ liệu và ca kiểm thử của Task 1.

### Task 2 — Chế độ tìm kiếm, bảng và cửa sổ Xem

**Tệp** ComboBoxSearchHelper.cs, UC_BCTonKho.cs, Frm_ChiTietDongTonKho.cs và tests/UC_BCTonKho.SearchIntegration.Tests.ps1.

**Giao diện** ComboBoxSearchHelper.RefreshSuggestionsAsync(string keyword, bool openDropDown = true) là phương thức gọi riêng để hủy lần cũ và làm mới gợi ý. Tìm kiếm dùng các API Task 1. Cửa sổ Frm_ChiTietDongTonKho tiếp tục nhận BCTonKho_ExcelDong.

- [ ] Viết -Suite Ui chạy WinForms STA qua btnTimKiem, comboBox1 và cbxKieuTon. Kiểm tra gõ cùng từ khóa rồi đổi chế độ cập nhật gợi ý không cần gõ lại. Đổi Gồm hết tồn khi đã chọn giữ đúng ID, thử bỏ tích khi mục hết tồn không còn phù hợp. Dùng tác vụ chậm có kiểm soát để chứng minh lựa chọn cũ không ghi đè lựa chọn mới, gồm trường hợp báo cáo tổng đang tải.
- [ ] Thêm kiểm tra bảng tìm kiếm có cùng cột với btnXemTonKho. Sắp xếp bảng rồi bấm Xem của một LOT và một cuộn lẻ. So sánh số đặt trước với Task 1, trạng thái, chiều dài ban đầu và thứ tự lần cắt. Kiểm tra dòng chỉ có một LOT góp tồn và một LOT hết vẫn có danh sách chọn hai mã ở cửa sổ, đồng thời nhãn LOT của bảng chỉ phản ánh mã góp tồn. Mục cảnh báo giữ cả thông tin dự kiến.
- [ ] Chạy -Suite Ui và ghi ca lỗi dự kiến. Mốc cũ chưa có tìm kiếm theo chế độ và không mở đúng cửa sổ từ bảng đã lọc.
- [ ] Bổ sung khả năng làm mới riêng của ComboBoxSearchHelper, giữ Reset và đường gõ hiện hành. Khi thay đổi chế độ, nhớ từ khóa người dùng đã gõ, xóa lựa chọn cũ và tải lại gợi ý. Khi đổi Gồm hết tồn, hủy tác vụ cũ và tải lại lựa chọn hợp lệ theo ID, đồng thời làm mới gợi ý.
- [ ] Nối hai chế độ vào UC_BCTonKho. Dùng phiên bản và mã hủy cho gợi ý và kết quả. Kiểm tra lại lựa chọn, chế độ và bộ lọc trước khi hiển thị. Chỉ xóa bảng tìm kiếm đang thuộc lượt tìm cũ. Ưu tiên lượt mới ngay cả khi báo cáo tổng còn đang chạy.
- [ ] Dùng chung cấu hình cột báo cáo tổng và bảng tìm kiếm. Gắn BCTonKho_ExcelDong vào Tag. Cửa sổ Xem dùng LotTraCuu để cho tra LOT hết, dùng LotGop để giải thích đóng góp, và hiện chiều dài ban đầu, trạng thái cuộn và các khoản FIFO dự kiến. Báo cáo tổng vẫn dùng chi tiết hiện có.
- [ ] Build và chạy -Suite Data cùng -Suite Ui, rồi chạy bộ UC_BCTonKho.RowDetails.Tests.ps1. So sánh báo cáo tổng với mốc nền. Chạy git diff --check rồi commit Task 2.

### Task 3 — Đầu ra Excel, hồi quy và ZIP bàn giao

**Tệp** UC_BCTonKho.cs và tests/UC_BCTonKho.SearchIntegration.Tests.ps1. Gói bàn giao nằm dưới artifacts/ và chứa các tệp mã sản phẩm đã chỉnh sửa trong sơ đồ.

- [ ] Viết ca -Suite Excel cho lựa chọn có dữ liệu ở hai trạng thái checkbox. Khi bỏ tích, bảng hiện đúng đối tượng và không yêu cầu đường dẫn tệp. Khi tích, thử chọn đường dẫn tạm qua luồng xuất hiện có, kiểm tra bảng cũ giữ nguyên và nội dung XLSX chỉ gồm kết quả đã lọc. Đọc tệp bằng ClosedXML để kiểm tra mã sản phẩm, LOT, số liệu và bộ cột. Kiểm tra hủy hộp lưu, tên tệp có FIFO_du_kien khi áp dụng, và thao tác cũ bị hủy trước khi xuất. Dùng fixture có tài khoản thử hợp lệ cho kiểm tra phân quyền. Nếu môi trường khó điều khiển hộp lưu Windows, tách kiểm tra nhánh đầu ra và kiểm chứng thao tác lưu trên bản build WinForms, ghi riêng mức kiểm chứng của từng ca.
- [ ] Chạy -Suite Excel trước khi nối chức năng. Kỳ vọng ca xuất kết quả tìm kiếm báo thiếu đầu ra hoặc bảng vẫn được cập nhật thay vì xuất.
- [ ] Chuyển một lượt tìm kiếm vào nhánh bảng hoặc nhánh Excel theo trạng thái đã chụp, rồi kiểm tra lại phiên bản trước khi mở hộp lưu và trước khi ghi. Tạo bộ cột dữ liệu như B và dùng ExcelExporter.ExportToPath. Giữ đường chạy tổng, báo cáo Word và các thông báo quyền/lỗi hiện hành.
- [ ] Build Debug Any CPU và x64. Chạy -Suite All, UC_BCTonKho.RowDetails.Tests.ps1 và UC_BCTonKho.RealSnapshot.ps1 với bản sao chỉ đọc và đầu ra kiểm thử tạm. Kiểm tra báo cáo Word qua BaoCaoTonKhoLo_DB và TonKhoLoReportDocxWriter trên đường dẫn tạm, rồi kiểm tra hồi quy B1 và B2 bằng Inventory.SaveFlow.Tests.ps1 và LapKeHoachCatDay.ChiTiet.Tests.ps1. Chạy CutReelTest và QrMaBinSegmentFlowTest của B3 nếu môi trường PHP và phụ thuộc đã sẵn sàng. Phân biệt lỗi nền, giới hạn môi trường và lỗi tính năng mới.
- [ ] Đối chiếu diff với các tệp đã sửa. Chạy git diff --check. Xác nhận các chỉnh sửa cục bộ của người dùng và dữ liệu cấu hình bằng mã băm. Commit các tệp Task 3 và rà soát toàn bộ nhánh trước bàn giao.
- [ ] Đóng gói chỉ tệp mã sản phẩm đã đổi vào artifacts/DG_TonKhoBTP_v02_TichHopTimKiemTonKho_2026-11-09.zip theo đường dẫn tương đối từ gốc dự án. Đối chiếu danh sách mục trong ZIP với diff tệp sản phẩm và mã băm tệp trong ZIP với mã nguồn đã kiểm chứng. Bàn giao danh sách tệp và phương thức, các chức năng giữ được, kết quả kiểm thử và giới hạn xác minh.
