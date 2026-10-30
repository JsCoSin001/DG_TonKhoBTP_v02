# Thiết kế sửa lỗi UC_BCTonKho — phương án 1

Ngày: 30/10/2026. Trạng thái: **Đã rà soát độc lập và được người dùng duyệt thiết kế**.

## 1. Mục tiêu và quyết định đã có

Người dùng yêu cầu dùng Superpowers để làm rõ mục tiêu xử lý lỗi, đã đồng ý đưa REG-001 vào phạm vi và chọn **phương án 1: sửa tập trung trong cấu trúc hiện tại**.

Mục tiêu là xử lý 5 lỗi trực tiếp của UC_BCTonKho cùng chênh lệch xác nhận REG-001, rồi kiểm thử nghiệm thu lại bằng bằng chứng mới. Tồn kho phải phản ánh nguồn vật lý hiện tại; dữ liệu bất thường không được âm thầm biến mất hoặc được coi hợp lệ.

Đầu vào: `docs/verification/UC_BCTonKho_Test_Report.md` và evidence tương ứng. Kết quả ban đầu là 84/100, có 2 lỗi High. Đây là mốc trước sửa, không phải kết quả sau sửa.

### Ràng buộc được giữ

- Giữ .NET Framework 4.8, WinForms, System.Data.SQLite và cấu trúc Model/Database/UI hiện tại.
- Không sửa database gốc, không migration, không tự sửa chữa dữ liệu và không thay schema. Chức năng xem tồn chỉ SELECT qua DB_Base.
- Không commit, push hoặc merge. Không ghi đè các thay đổi người dùng đã có trước phiên làm việc.
- Không viết lại thuật toán FIFO hoặc cơ chế lưu nhập/lấy/cắt/hoàn tác B2/B3 trong đợt này.
- Giữ 10 cột grid, giới hạn 50 gợi ý, nội dung mỗi item riêng lẻ và quy tắc đặt chỗ không giảm tồn vật lý.
- Giữ nguyên báo cáo kiểm thử trước sửa và dữ liệu bằng chứng của nó. Kết quả sau sửa được lưu riêng.

## 2. Các hướng đã cân nhắc

| Hướng | Nội dung | Đánh đổi | Quyết định |
|---|---|---|---|
| 1 | Bổ sung kiểm tra nguồn và sửa các điểm UI tại lớp hiện tại | Phạm vi thay đổi nhỏ; phải kiểm thử nhất quán giữa gợi ý và detail | **Người dùng chọn** |
| 2 | Hợp nhất toàn bộ truy vấn tồn thành một truy vấn thống nhất | Tập trung quy tắc nguồn nhưng thay đổi và hồi quy rộng hơn | Không chọn |

Không bổ sung framework, package hoặc tầng tồn kho dùng chung cho các màn hình khác.

## 3. Quy tắc nhận diện nguồn và tính tồn — INV-001

### 3.1 Thứ tự tin cậy

Quy ước đã xác minh từ `NhapKho_DB`: TTCuonDay có cả SoDau/SoCuoi NULL là nguồn cuộn chẵn; có cả hai mốc là nhập lẻ trực tiếp; có một mốc NULL là nguồn bất thường.

**Nguồn nhập và liên kết cuộn hiện tại xác định loại nguồn. Lịch sử dùng để đối chiếu tính nhất quán.** Không coi cuộn phát sinh là nhập trực tiếp chỉ vì mất dòng lịch sử đầu tiên.

| Nguồn TTCuonDay | Có TonCuonLe liên kết | Cách xử lý báo cáo |
|---|---|---|
| Hai mốc NULL | Có | Mỗi TonCuonLe_ID khác nhau đại diện một cuộn đã chuyển lẻ; chiều dài ban đầu của dòng lẻ trống |
| Hai mốc có giá trị | Có | Lẻ nhập trực tiếp; chiều dài ban đầu lấy từ ChieuDai_1cuon |
| Chỉ một mốc có giá trị | Có | Giữ dòng lẻ hiện tại, báo nguồn nhập có mốc không hợp lệ; chiều dài ban đầu không được tự suy diễn |
| Không còn nguồn/header hợp lệ | Có | Giữ cuộn ở nhánh thiếu nguồn, báo lỗi; không suy đoán kiểu nhập ban đầu |

Tồn chẵn theo TTCuonDay_ID:

`SoCuon nhập − SUM(SoLuong đã lấy) − COUNT(DISTINCT TonCuonLe_ID hiện còn liên kết nguồn chẵn)`.

- Đếm cả cuộn lẻ ACTIVE và HET: cắt hết phần lẻ không trả lại một cuộn chẵn.
- Nhiều lịch sử của cùng cuộn lẻ chỉ trừ một cuộn.
- Mất lịch sử chuyển đầu tiên nhưng TonCuonLe vẫn tồn tại: không hoàn lại cuộn chẵn. Cảnh báo trên cả dòng lẻ và nguồn chẵn liên quan.
- Hoàn tác chuyển lẻ hợp lệ hiện xóa TonCuonLe phát sinh và gỡ liên kết nhóm; vì vậy không còn bị tính là đã chuyển. Lịch sử/audit của thao tác bị hủy không được trừ tồn thêm.
- Nếu liên kết chủ sở hữu của cuộn và nguồn không khớp: giữ dấu vết và cảnh báo cả hai phía. Phép trừ vẫn dựa TTCuonDay_ID được khai báo, không tự chuyển số lượng sang Bin khác.
- Không clamp tồn âm thành 0. Nhập/lấy/chuyển không hợp lệ vẫn giữ số liệu và nguyên nhân lỗi.

Thiếu lịch sử chuyển, hoặc có lịch sử CUON_CHAN trên nguồn nhập lẻ trực tiếp, là bất thường phải được phát hiện. Không khôi phục/xóa lịch sử trong chức năng báo cáo.

### 3.2 Tồn lẻ hiện tại

Luôn lấy SoDau/SoCuoi từ TonCuonLe, chiều dài còn lại = ABS(SoCuoi − SoDau) nếu đủ mốc. Giữ các kiểm tra ACTIVE/HET, thiếu mốc và đặt chỗ vượt tồn đã có. Đảo chiều hợp lệ không phải lỗi.

## 4. Cuộn thiếu nguồn và phạm vi chọn — INV-002

Cho phép tìm sản phẩm/Bin/cuộn từ TTThanhPham và TonCuonLe ngay cả khi header nhập của cuộn đã mất; không để INNER JOIN header loại cuộn trước bước phát hiện lỗi.

- Điều kiện kho hiện tại vẫn giữ `TTThanhPham.NhapKho=1` và `Temp=0`. Bản ghi ngoài kho hợp lệ không được tự kéo vào.
- Gợi ý phải là hợp của nguồn nhập hợp lệ và các cuộn còn liên kết chủ sở hữu đủ xác định nhưng thiếu nguồn nhập. Không gợi ý hàng hoàn toàn không có nguồn/cuộn.
- Với dòng lẻ, chủ sở hữu báo cáo lấy từ `TonCuonLe.TTThanhPham_ID`. Chủ sở hữu theo nguồn nhập được đọc riêng để đối chiếu, không âm thầm thay mã Bin khi hai liên kết sai khác.
- Mất TTCuonDay hoặc TTNhapKhoTP: vẫn giữ TonCuonLe_ID, TTCuonDay_ID khai báo, mã, mốc hiện tại và lý do; chiều dài ban đầu để trống khi không có nguồn xác minh được.
- Chọn Bin chỉ lấy dòng thuộc Bin được xác định; chọn mã cuộn trả đúng ID đó; chọn tên gom đủ product ID cùng tên. Không phát sinh hai dòng cho cùng TonCuonLe_ID giữa nhánh thường và nhánh orphan.
- Chưa có đủ TTThanhPham/product để định danh thì không bịa Bin, sản phẩm hoặc nguồn; mở rộng màn hình kiểm kê mồ côi toàn cục nằm ngoài đợt sửa này.
- Bộ lọc vẫn giữ dòng bất thường dù chiều dài bằng 0, thiếu mốc hay checkbox không bật.

Không thay quy tắc đã xác minh ở ca 10-10: đặt chỗ không có nguồn được cảnh báo ở phạm vi sản phẩm, không gán cho một Bin không liên quan.

## 5. Đối chiếu mã nhập trực tiếp — INV-003

Khi nguồn xác định là nhập lẻ trực tiếp, MaCuon phải khớp MaBin của chủ sở hữu nguồn. Khác mã hoặc khác liên kết chủ sở hữu đều có nguyên nhân riêng trong tooltip.

Lựa chọn kỹ thuật đề xuất: trim để so sánh, dùng so sánh Ordinal cho định danh; giữ nguyên nội dung gốc hiển thị. Không chuẩn hóa/ghi lại database. Mã QR của cuộn chuyển lẻ không bị ép bằng MaBin.

## 6. Đồng bộ checkbox và dropdown — UI-001

Thay đổi checkbox phải làm mất hiệu lực truy vấn gợi ý cũ và các Items đang hiển thị, đồng thời giữ nguyên text.

- Đã có item được chọn: giữ định danh lựa chọn, tải lại detail theo checkbox mới; không tự bật dropdown tìm kiếm.
- Chưa chọn, từ khóa có nội dung: tải lại gợi ý với checkbox mới, không cần gõ thêm ký tự.
- Text rỗng: giữ dropdown/grid ở trạng thái rỗng theo luồng hiện tại.
- Toggle nhanh hoặc truy vấn trả chậm: chỉ kết quả của lần đổi mới nhất được cập nhật.
- Không hiện Waiting khi chỉ tải gợi ý; detail tiếp tục dùng Waiting và version/cancellation hiện tại.
- Refresh gợi ý không phát sự kiện Cleared/ItemSelected và không làm mất text đã chọn.

Bổ sung API refresh/invalidate tối thiểu vào ComboBoxSearchHelper; hành vi mặc định của những màn hình không gọi API mới phải được giữ. Callback đang chờ và BeginInvoke phải kiểm tra hủy/Dispose trước khi cập nhật control.

## 7. Style toàn dòng — UI-002

Cell Lịch sử vẫn là nút Xem, không thực hiện nghiệp vụ. Với dòng lỗi, cả trạng thái thường/được chọn của nút phải tuân màu nền đỏ hiện tại RGB(177,48,59), chữ trắng như các cell dữ liệu, có tooltip nguyên nhân.

Chỉ áp dụng khi đang hiển thị grid tồn mới. Khi chuyển về báo cáo cũ, không giữ handler/cách vẽ hoặc style đặc thù. Chọn cấu hình button cell/CellPainting phù hợp khi triển khai; kiểm chứng bằng render control thật.

## 8. Xác nhận sửa/hoàn tác — REG-001

Tái sử dụng nội dung đã được ConfirmationText(request, preview) tính từ preview vừa kiểm tra, đưa vào chính hộp xác nhận hiện có trước lời hỏi lưu.

- Sửa mốc: mã cuộn, mốc hiện tại/mới, chiều dài tồn giữ nguyên và lý do.
- Hoàn tác: thao tác, QR/mã cuộn, tác động khôi phục tồn/nhóm và các mốc liên quan từ preview.
- Giữ một lần xác nhận cho một batch có sửa mốc/hoàn tác. Không thêm popup cho gõ chữ, preview nền hoặc sửa người thực hiện nếu trước đó không cần.
- Chọn Không: không ghi dữ liệu, giữ đầy đủ bản nháp.
- Đồng ý nhưng dữ liệu đã đổi: kiểm tra version tại Save vẫn từ chối; không tái dùng preview như bảo đảm dữ liệu chưa đổi.
- Không sửa thuật toán Preview/Save/Undo hoặc quyền truy cập trong đợt này.

## 9. Phạm vi file dự kiến

| File | Trách nhiệm |
|---|---|
| Database/Kho/TonKho/BC_TonKho_DB.cs | Nhận diện nguồn, tính chuyển lẻ, đối chiếu lịch sử/mã, truy xuất thiếu nguồn |
| UI/NghiepVuKhac/Kho/UC_BCTonKho.cs | Refresh filter và style nút trong dòng lỗi |
| UI/Helper/AutoSearchWithCombobox/ComboBoxSearchHelper.cs | API invalidate/refresh, cancellation và guard Dispose cho luồng này |
| UI/NghiepVuKhac/Kho/XuatKho/Frm_ChiTietThucHienKeHoach.Execution.cs | Nội dung xác nhận từ preview |
| Models/Kho/TonKho/BCTonKho_Model.cs | Chỉ bổ sung metadata nội bộ nếu cần đối chiếu/giữ ID nguồn; không thêm cột UI |
| tests/UC_BCTonKho.* và test hồi quy liên quan | Ca tái hiện, ca biên và bằng chứng nghiệm thu lại |

Các file sản phẩm trên tính từ DG_TonKhoBTP_v02. Không cần sửa Designer, csproj hoặc schema nếu không thêm file sản phẩm.

## 10. Kế hoạch chứng minh đúng

Tái dùng kỳ vọng nghiệp vụ, không đổi thành hành vi hiện tại để làm test PASS. Mở rộng fixture, không ghi dữ liệu thử lên DB gốc.

| Lỗi | Ca cũ phải PASS | Ca biên cần bổ sung |
|---|---|---|
| INV-001 | 10-09, 10-12; tồn 6 và ban đầu lẻ trống | Lẻ HET vẫn trừ nguồn; hoàn tác chuyển gỡ cuộn; nhiều lịch sử chỉ trừ một; lịch sử/source mâu thuẫn |
| INV-002 | 10-08: detail/gợi ý đều giữ dòng thiếu header | Chọn Bin/tên/cuộn; thiếu TTCuonDay; owner/source khác product; không trùng dòng; orphan hết tồn vẫn hiện |
| INV-003 | 10-13 | Mã nhập trực tiếp đúng không báo nhầm; mã QR phát sinh khác Bin vẫn hợp lệ; sai cả mã và liên kết có đủ lý do |
| UI-001 | 09-04 | Toggle khi truy vấn chậm, đã chọn/chưa chọn, giữ text, từ khóa rỗng, Dispose; helper của màn hình cũ giữ hành vi |
| UI-002 | 10-16 | Nút thường/selected có màu đúng; Xem không ghi DB/crash; chuyển báo cáo không sót style |
| REG-001 | DesignedForm/MarkerConfirm và Undo | Từ chối giữ draft/DB; chấp nhận với version cũ bị từ chối; một popup cho batch |

Chạy build Debug và bộ 14 nhóm acceptance cùng đối chiếu snapshot chỉ đọc. Mẫu thật phải giữ 6 cuộn chẵn, hai cuộn lẻ 100/20 m và cuộn 996→600 còn 396 m. FIFO A6/B2 và đặt chỗ lẻ 150/396 vẫn phải đúng; đọc đồng thời vẫn cùng snapshot.

Các script cũ bị BLOCKED vì tiền điều kiện/scope/revision phải được đánh giá rõ. Nếu cập nhật test, chỉ nạp đúng hợp đồng hiện có hoặc dựng fixture xác định; không thay yêu cầu hoặc sửa nghiệp vụ sản phẩm ngoài 6 lỗi. Lỗi mới ngoài phạm vi được báo riêng, không âm thầm mở rộng sửa.

Không gán PASS cho GUI desktop nếu công cụ vẫn bị chặn quyền. Kiểm thử STA/render và thao tác desktop được ghi riêng. Báo cáo nghiệm thu lại tách biệt mốc trước sửa, dùng số đo mới, nêu các phần chưa xác minh và đối chiếu hash database gốc trước/sau.

## 11. Điều kiện hoàn tất và chốt trước triển khai

Hoàn tất sửa mã khi cả 6 lỗi có bằng chứng tái hiện trước sửa và ca tương ứng PASS sau sửa, build và hồi quy liên quan đạt, không sửa database gốc và không phát sinh lỗi tồn/định danh mới.

Chỉ kết luận ĐẠT NGHIỆM THU theo tiêu chí gốc: ít nhất 95/100, mọi nghiệp vụ trọng yếu PASS, không còn Critical/High, các kiểm thử bắt buộc được chứng minh. Nếu còn BLOCKED thì báo đúng giới hạn, không tự cho đủ điểm.

Người dùng đã duyệt phạm vi, hướng 1 và văn bản thiết kế bằng yêu cầu “duyệt thiết kế”. Bước kế tiếp là Superpowers writing-plans; chưa chỉnh mã sản phẩm ở bước thiết kế.

## 12. Kết quả rà soát trước khi trình duyệt

Đã có một lượt builder check độc lập, chỉ đọc thiết kế và mã tham chiếu. Kết quả: **“nothing important left”** — không còn câu hỏi quan trọng phải giải quyết trước khi lập kế hoạch. Đây là đánh giá khả năng lập kế hoạch, không thay thế phê duyệt của người dùng hoặc kiểm thử sau triển khai.
