# Ánh xạ control sang Frm_ChiTietThucHienKeHoach

Ngày: 27/10/2026. Trạng thái: **ĐÃ CHỐT PHƯƠNG ÁN, ĐANG TRIỂN KHAI.** Dùng `lblTacDong` kiểu Label hiện có để hiển thị lỗi; không cần RichTextBox. Người dùng đã làm rõ đây là Label hiển thị thông báo động, được cập nhật nội dung lỗi; các Label mô tả khác giữ nguyên.

## Phạm vi và cơ sở đối chiếu

- Form đang hoạt động: `DG_TonKhoBTP_v02/UI/NghiepVuKhac/Kho/XuatKho/FrmChiTietThucHien.cs`.
- Form đích: `DG_TonKhoBTP_v02/UI/NghiepVuKhac/Kho/XuatKho/Frm_ChiTietThucHienKeHoach.cs` và `.Designer.cs`.
- Điểm mở form: `UC_LapKeHoachCatDay.MoChiTietThucHienAsync`.
- Dữ liệu và nghiệp vụ: `ExecutionDetailSnapshot`, `ExecutionEditDraft`, `ExecutionEdit_DB.Load/Preview/Save/LoadAudit`.
- Form đích hiện chỉ gọi `InitializeComponent()`. Hai bảng chưa có cột thiết kế và chưa gắn sự kiện nghiệp vụ. Form cha vẫn mở form cũ.

Yêu cầu của người dùng: chuyển toàn bộ chức năng sang form được thiết kế sẵn, giữ nguyên toàn bộ logic; liệt kê control thiếu để người dùng bổ sung trong Designer; chỉ code sau khi duyệt ánh xạ.

**Ràng buộc bổ sung của người dùng:** không thay đổi nội dung các Label; chỉ cập nhật control hiển thị dữ liệu hoặc nhận dữ liệu người dùng nhập. Rút lại các đề xuất đổi nhãn trong bản trước. Không tự đổi tiêu đề form, GroupBox, TabPage hay nội dung nút hiện có. Giữ nguyên cả `lblTenSP`, `lblThongTinSP`, `lblTenNhomNghiepVu`; chưa có ngoại lệ cho các Label này. Không bổ sung `rtbThongTinThucHien`, không tự thêm control vào Designer.

## Phương án đề xuất để duyệt

Giữ bố cục danh sách bên trái, chi tiết và lịch sử bên phải. Gắn lại các sự kiện hiện có với control thiết kế, tiếp tục dùng nguyên lớp bản nháp và lớp nghiệp vụ dữ liệu.

**Cập nhật theo người dùng:** ô lý do dùng để nhập và lưu lý do sửa, không hiển thị tác động hoặc lỗi. Tên thực tế Designer đã lưu là `rtbLyDoSua`; sử dụng đúng tên này, không tự đổi tên control theo cách viết khác trong cuộc trò chuyện.

Nội dung `rtbLyDoSua` là lý do chi tiết chung cho các thay đổi trong một lần lưu; kiểm tra không trống khi lưu; chuyển dòng không xóa nội dung. Không cần hai ComboBox chọn lý do trong bản ánh xạ trước. Hệ thống xác định mã phân loại theo thao tác: đổi người → `INFO`, hoàn tác → `MISTAKE`, sửa mốc → `MARKERS`. Đây là thay đổi cách nhập trên giao diện; giữ nguyên kiểm tra nghiệp vụ ở lớp dữ liệu.

Cơ sở dữ liệu hiện lưu mã phân loại tại `B2DieuChinhSuKien.LyDo`, và hỗ trợ văn bản tại `GhiChu` qua thuộc tính `Note`. Đề xuất giữ nguyên mã phân loại để các kiểm tra hiện tại tiếp tục hoạt động, truyền nguyên văn lý do người dùng nhập qua `Note` vào `GhiChu` của từng sự kiện người dùng sửa. Trên bảng lịch sử đặt nhãn phần văn bản này là “Lý do chi tiết”. Không ghi văn bản tự do đè vào cột mã `LyDo` hoặc suy đoán nghiệp vụ bằng cách đọc nội dung văn bản. Các sự kiện tự sinh như sửa mốc mất hiệu lực vẫn giữ giải thích hệ thống hiện có.

Control thông báo dùng **hiển thị lỗi khi lưu** theo chỉ dẫn mới, không dùng nhập dữ liệu, không tự ghi đè nội dung lý do. Nội dung xem trước khi sửa mốc hoặc hoàn tác được chuyển sang hộp thoại xác nhận trước khi ghi; nêu đủ mã cuộn, tồn, mốc trước/sau và sửa mốc sẽ mất hiệu lực. Hoàn tác vẫn chỉ áp dụng khi ghi nhận nhầm và thực tế chưa lấy/chưa cắt.

Không dựng form cũ bên trong form mới, vì cách đó bỏ qua bố cục người dùng đã thiết kế. Không viết lại xử lý lưu hoặc hoàn tác: việc đó không cần thiết để chuyển giao diện và làm tăng nguy cơ thay đổi nghiệp vụ.

## A. Control đã có

| Control đích | Chức năng tương ứng ở form cũ | Nội dung / cách sử dụng đề xuất |
| --- | --- | --- |
| Form `Frm_ChiTietThucHienKeHoach` | `FrmChiTietThucHien` | Giữ tiêu đề thiết kế; mở theo đúng kế hoạch, hàng, quy cách cuộn chẵn hoặc cuộn lẻ đang chọn. Giữ kết quả `LastSaveResult` trả về form cha. |
| `groupBox1` | Khu vực bảng thực hiện | Giữ nguyên tiêu đề thiết kế “Danh sách cắt cuộn”. |
| `lblTenSP` | Label hiện có | Giữ nguyên nội dung thiết kế; không bổ sung vùng tên sản phẩm thay thế. Phạm vi dữ liệu vẫn theo truy vấn từ form cha. |
| `lblThongTinSP` | Label hiện có | Giữ nguyên nội dung thiết kế; không tạo vùng thông tin tổng hợp thay thế. |
| `grvDsChiTietCuon` | `_grid` | Các dòng thực hiện và dòng đang chờ trong cùng phạm vi. Chỉ cột trạng thái được sửa trực tiếp theo điều kiện nghiệp vụ. Chọn dòng cập nhật chi tiết bên phải; chuyển dòng giữ bản nháp. |
| `groupBox2` | Khu vực chi tiết | Giữ tiêu đề “Chi tiết”. |
| `tabControl1` | Hai thẻ thông tin và nhật ký | Giữ hai thẻ, tải nhật ký theo phạm vi dòng/cuộn đang chọn như hiện tại. |
| `tabPage1` | Thẻ thông tin thực hiện | Giữ nguyên tên thiết kế “Thông tin cuộn”. |
| `lblTenNhomNghiepVu` | Label hiện có | Giữ nguyên nội dung thiết kế. Nhóm / đoạn / nghiệp vụ thực tế có trên các cột tương ứng của bảng; không tạo nhóm giả. |
| `cbxNguoiThucHien` | `_person` | Chọn tài khoản có sẵn, gồm tài khoản ngừng hoạt động có chú thích. `DropDownList`, không nhập tự do. Không dùng control này để chọn tài khoản người sửa. Khóa khi dòng chưa thực hiện, không có mã thao tác, đang hoàn tác hoặc form chỉ xem. |
| `dtNgayThucHien` | `_time` | Chỉ hiển thị thời gian đã ghi, gồm ngày và giờ (`dd/MM/yyyy HH:mm:ss`). `Enabled=false`, không ghi ngược vào dữ liệu. Dòng chưa thực hiện hiển thị trống hoặc “—”, không lấy ngày hiện tại làm ngày thực hiện. |
| `tbxQr` | `_qr` | QR / mã cuộn của dòng đang chọn; `ReadOnly=true`. |
| `tbxNguonNhap` | `_source` | Giữ đúng nguồn đang hiển thị ở form cũ: `SourceLabel` / mã bin, không mở rộng thành thông tin phiếu nhập chưa có trong dữ liệu hiện tại. `ReadOnly=true`. |
| `tbxSoDau_SoCuoi_DB` | Phần trước cắt của `_history` | `BeforeStart → BeforeEnd` của lần cắt đang chọn; chỉ xem. |
| `tbxSoDau_SoCuoi_DaCat_DB` | Phần đoạn cắt của `_history` | `CutStart → CutEnd` của lần cắt đang chọn; chỉ xem. |
| `tbxSoDau_SoCuoi_ConLai_DB` | Phần sau cắt của `_history` | `AfterStart → AfterEnd` của lần cắt đang chọn; chỉ xem. Đây là mốc ngay sau lần cắt đó, không phải mốc tồn hiện tại. |
| `label8` | Nhãn khu vực sửa | Giữ nguyên nội dung “Thông tin sửa chữa”. Mã cuộn, tồn và mốc đang lưu được nêu trong hộp thoại xác nhận sửa mốc; không thêm vùng tổng hợp. |
| `tbxSoDau_Sua` | `_start` | Số đầu hiện tại / dự kiến, chỉ xem và tự tính theo chiều dài tồn hiện tại. |
| `tbxSoCuoi_Sua` | `_end` | Nhập số cuối mới, số nguyên từ 0 đến `Int32.MaxValue`; kiểm tra nhập sai, số âm và vượt giới hạn trước khi tạo bản nháp. Không đổi kiểu dữ liệu nghiệp vụ. |
| `cbxChieu` | `_direction` | `DropDownList`; “Tăng (+1)” / “Giảm (-1)”, tương ứng giá trị 1 / -1 hiện có. |
| `rtbLyDoSua` | Nội dung lý do chi tiết; chưa có ô tương đương ở form cũ | Nhập và lưu lý do sửa chung cho một lần lưu; kiểm tra không trống khi lưu; giữ khi chuyển dòng hoặc lưu lỗi. Văn bản đi qua `Note` vào cột `GhiChu`, mã phân loại `LyDo` vẫn theo loại thao tác. Không dùng ô này hiển thị lỗi / tác động. |
| `lblTacDong` | Phần báo lỗi của `_impact` | Label động hiển thị lỗi nhập liệu, kiểm tra và lưu; giữ bản nháp. Không dùng làm vùng nhập lý do hay vùng thông tin tổng hợp. Các Label khác giữ nguyên nội dung thiết kế. |
| `btnLuuThayDoi` | `_save` | Lưu toàn bộ thay đổi dòng và cuộn. Cho bấm khi có bản nháp để chạy kiểm tra và hiển thị lỗi; chỉ gọi ghi dữ liệu khi kiểm tra mới nhất hợp lệ, phiên bản không thay đổi và đã xác nhận tác động cần thiết. |
| `btnĐóng` | `_close` | Đóng form; giữ hỏi Lưu / Bỏ thay đổi / Quay lại khi có bản nháp; không đóng trong lúc lưu. |
| `tabPage2` | Thẻ nhật ký điều chỉnh | Giữ tên “Lịch sử thay đổi”. |
| `dataGridView2` | `_audit` | Chỉ xem kết quả `ExecutionEdit_DB.LoadAudit` như form cũ, không lọc bỏ sự kiện đã mất hiệu lực hoặc lịch sử liên quan từ kế hoạch khác của cuộn. Hiển thị tên cột tiếng Việt. |
| `panel1`, `panel2`, `panel3`, `tableLayoutPanel1/2/3` | Bố trí giao diện | Giữ thiết kế của người dùng. Control thiếu được người dùng bổ sung trong Designer; không tự sắp lại bố cục hoặc gắn nghiệp vụ ghi dữ liệu vào control bố trí. |

### Các nhãn giữ nguyên nội dung thiết kế

| Nhãn | Nội dung hiện có — không sửa |
| --- | --- |
| `label1` | Người thực hiện |
| `label2` | Thời gian thực hiện |
| `label3` | Qr/Mã cuộn |
| `label4` | Nguồn nhập |
| `label5` | Mốc trước cắt |
| `label6` | Mốc đã cắt |
| `label7` | Mốc còn lại |
| `label8` | Thông tin sửa chữa |
| `label9` | Số đầu |
| `label10` | Mốc đã cắt |
| `label11` | Chiều đánh số |
| `label12` | Lý do sửa |
| `label13` | Thông tin trong hệ thống |

Ví dụ `label10` vẫn là “Mốc đã cắt”, còn `tbxSoCuoi_Sua` vẫn dùng nhập số cuối mới theo logic hiện có. Hộp thoại xác nhận phân biệt dữ liệu lịch sử và mốc cuộn hiện tại; không tự sửa Label để giải thích.

## B. Cột cần bổ sung vào grvDsChiTietCuon

Hiện bảng chưa có cột thiết kế. Đề xuất giữ đúng sáu cột của form đang hoạt động. Nếu người dùng muốn thiết kế cột trực tiếp, bổ sung các cột sau trong Designer:

| Tên cột | Loại | Tiêu đề | Dữ liệu / quyền sửa |
| --- | --- | --- | --- |
| `colSoLuong` | DataGridViewTextBoxColumn | Số lượng | `Quantity`; chỉ xem. |
| `colChieuDai` | DataGridViewTextBoxColumn | Chiều dài (m) | `Length`; chỉ xem. |
| `colNghiepVu` | DataGridViewTextBoxColumn | Nghiệp vụ | `Description`: cắt cuộn lẻ / cắt từ cuộn chẵn / lấy nguyên cuộn; chỉ xem. |
| `colNhom` | DataGridViewTextBoxColumn | Nhóm / đoạn | `GroupId / DetailId` hoặc “—”; chỉ xem. |
| `colTrangThai` | DataGridViewComboBoxColumn | Trạng thái | “Đã thực hiện” / “Chưa thực hiện”; sửa theo điều kiện bên dưới. |
| `colMaCuon` | DataGridViewTextBoxColumn | QR / Mã cuộn | `RawQr`; chỉ xem, cho phép xem đầy đủ chuỗi dài. |

- Không cho thêm hoặc xóa dòng trên bảng; chọn một dòng để xem, nhưng vẫn sửa nhiều dòng lần lượt rồi lưu chung.
- Dòng chưa thực hiện đọc từ cơ sở dữ liệu không được đổi thành đã thực hiện.
- Dòng đã thực hiện có mã thao tác hợp lệ được đổi về chưa thực hiện để đề nghị hoàn tác; điều kiện phụ thuộc tiếp tục được kiểm tra ở `Preview/Save`.
- Đổi ngược trạng thái của chính bản nháp chưa lưu chỉ là bỏ ý định hoàn tác, không tạo lần thực hiện mới.
- Giữ màu đánh dấu dòng có bản nháp. Bảng bên trái hẹp hơn bảng cũ: bố trí độ rộng hợp lý, xuống dòng hoặc cuộn ngang để không bỏ cột.

`dataGridView2` có thể tiếp tục tự tạo cột từ kết quả nhật ký như form cũ; không bắt buộc bổ sung cột trong Designer. Đổi nhãn hiển thị sang tiếng Việt, giữ đầy đủ dữ liệu trước/sau, lý do, ghi chú, người sửa, thời gian và các khóa đối chiếu mà truy vấn hiện tại trả về.

## C. Phương án không bổ sung control đã được người dùng chọn

Không thêm `rtbThongTinThucHien`, `btnBoThayDoi`, `btnDoiChieuDuLieuMoi`, hai ComboBox lý do hoặc Label động.

| Thành phần | Cách xử lý đã thống nhất |
| --- | --- | --- |
| Không có nút Bỏ thay đổi | Nhấn Đóng, chọn không lưu để bỏ bản nháp rồi đóng; không còn thao tác đặt lại toàn bộ và ở lại form. |
| Không có nút Tải mới | Khi lưu có xung đột, hộp thoại hỏi có tải dữ liệu mới để đối chiếu; gọi lại xử lý giữ bản nháp hiện có. Nếu đối tượng đã mất hiệu lực thì giữ bản nháp và hiển thị lỗi, không tự áp vào đối tượng khác. |
| Không có vùng thông tin tổng hợp | Dùng bảng và các ô hiện có; thông tin cuộn / tồn / mốc cần thiết hiển thị trong xác nhận sửa mốc hoặc hoàn tác. |
| Control lỗi | Dùng `lblTacDong` kiểu Label theo giải thích mới nhất của người dùng. Không thay bằng RichTextBox. Chỉ Label này được cập nhật nội dung để hiển thị lỗi. |

Giữ màu đánh dấu dòng thay đổi; không thêm nhãn đếm bản nháp. Lỗi chỉ xuất hiện khi người dùng lưu hoặc không thể tải dữ liệu cần thiết. Không mở hộp thoại lỗi liên tục trong lúc gõ hoặc khi bộ hẹn giờ kiểm tra chạy.

Không tự thêm panel, di chuyển nút hoặc sửa nội dung nhãn. Không ẩn cả `tableLayoutPanel3` khi lấy nguyên cuộn vì còn chứa lý do, thông báo và nút. Chỉ khóa / ẩn các ô sửa mốc không áp dụng; không sửa nội dung Label.

## D. Giữ nguyên logic khi chuyển

1. Mở form đọc lại dữ liệu đã lưu, không tự lưu bản nháp lập kế hoạch. Giữ nguyên đối tượng truy vấn, bản chụp dữ liệu và kết quả trả về.
2. Giữ `ExecutionEditDraft`, một bản nháp mốc cho mỗi cuộn vật lý; chuyển dòng cùng cuộn vẫn giữ mốc đang nhập. Người sửa lấy từ phiên đăng nhập và ghi riêng với người thực hiện.
3. Mốc lịch sử chỉ xem. Mốc sửa lấy từ `ExecutionReelState` hiện tại, không lấy từ `AfterStart/AfterEnd` của dòng lịch sử. Công thức không đổi: `Số đầu mới = Số cuối mới - Chiều mới × Chiều dài tồn`.
4. Giữ nguyên điều kiện chỉ xem: kế hoạch HUY, chức năng sửa chưa bật; không sửa người/trạng thái ở dòng chưa thực hiện hoặc không có mã thao tác. Sửa mốc giữ đúng điều kiện cuộn ACTIVE, còn chiều dài và không đang hoàn tác trên cùng cuộn. Không tự thêm điều kiện khóa mốc chỉ vì chọn một dòng lịch sử khác, trái quyết định 7C.
5. Giữ kiểm tra toàn bộ bản nháp; cho bấm Lưu để biết lỗi, nhưng chỉ ghi khi kết quả kiểm tra mới nhất hợp lệ. Xác nhận tác động không chỉ giới hạn dòng đang chọn. Sau hộp thoại phải kiểm tra thế hệ bản nháp và phiên bản khi lưu, không ghi kết quả đã cũ.
6. Giữ nguyên kiểm tra phụ thuộc hoàn tác, thứ tự các lần cắt, liên kết khác kế hoạch, QR và sửa mốc mất hiệu lực. Không tự chọn thêm dòng để hoàn tác.
7. Lưu nguyên tử, kiểm tra phiên bản, chống ghi trùng, nhật ký và tài khoản người sửa không thay đổi. Lỗi hoặc xung đột giữ nguyên dữ liệu đang nhập.
8. Sau lưu, cập nhật dữ kiện thực hiện ở form cha và giữ bản nháp kế hoạch. Nếu hoàn tác làm phạm vi cuộn không còn tồn tại, giữ hành vi đóng form sau lưu thành công như hiện tại.
9. Giữ xử lý đóng form có bản nháp, khóa thao tác lúc đang lưu và dừng / giải phóng bộ hẹn giờ kiểm tra khi hủy form.
10. Chỉ chuyển giao diện B2; không cần đổi cấu trúc SQLite hoặc mã B3 cho việc ánh xạ này.

Điểm cập nhật cách nhập: ô văn bản nhập lý do thay cho hai danh sách chọn, áp dụng chung trong một lần lưu. Form tạo đúng mã `INFO/MISTAKE/MARKERS` theo từng thao tác và giữ nguyên `ExecutionEdit_DB` cùng toàn bộ kiểm tra tồn, phụ thuộc, phiên bản và giao dịch. Yêu cầu nhập văn bản không phải hành vi đã có ở form cũ.

## E. Ví dụ để duyệt

### 1. Cắt tiếp cuộn lẻ

Chọn lần cắt 150 m có mốc lịch sử `0 → 800`, đoạn cắt `650 → 800`, sau cắt `0 → 650`. Sau đó cuộn đã cắt tiếp nên tồn hiện tại chỉ còn 550 m.

- Ba ô lịch sử vẫn hiện `0 → 800`, `650 → 800`, `0 → 650`.
- Các Label giữ nguyên. Nhập số cuối 1550, chiều tăng thì số đầu tự tính 1000, dựa trên tồn hiện tại 550 m.
- Nhập vào `rtbLyDoSua`: “Đối chiếu thực tế: mốc cuối là 1550, tồn vẫn 550 m”. Khi nhấn Lưu, hộp thoại xác nhận nêu đúng mã cuộn, mốc trước/sau và tồn vẫn 550 m; control thông báo chỉ hiển thị lỗi nếu có.
- Nếu đồng thời đổi người thực hiện của lần cắt cũ, bổ sung giải thích vào cùng ô lý do; một lần Lưu xử lý cả hai loại thay đổi, mỗi sự kiện có mã phân loại riêng và cùng nội dung giải thích đã nhập.

### 2. Cuộn chẵn cắt thành lẻ

Cuộn chẵn 1000 m đã cắt 200 m, phát sinh cuộn lẻ còn 800 m. Chọn dòng đó: thông tin người, QR và lịch sử cắt hiển thị như hiện tại; khu vực mốc hiện tại áp dụng cho cuộn lẻ phát sinh còn 800 m.

Đổi trạng thái về chưa thực hiện chỉ là đề nghị hoàn tác. Khi nhấn Lưu, lỗi phụ thuộc hiển thị tại control lỗi; nếu hợp lệ, hộp thoại xác nhận nêu đủ kết quả xử lý cuộn phát sinh trước khi ghi. Không thay bằng sửa số lượng hoặc chiều dài.

### 3. Lấy nguyên cuộn chẵn

Đã lấy hai cuộn, mỗi cuộn 1000 m. Bảng hiện số lượng 2, chiều dài 1000 m và nghiệp vụ lấy nguyên cuộn. Cho sửa người hoặc đề nghị hoàn tác cả lần lấy theo logic hiện có; không sửa số lượng 2 thành 1 tại form này.

Ba ô lịch sử cắt hiện “—”; khóa / ẩn các ô sửa mốc không áp dụng. Giữ thông tin người, ô nhập lý do chung, lịch sử, Lưu và Đóng. Hoàn tác vẫn cần xác nhận tác động trước khi ghi.

## Trạng thái chốt phương án

- Ánh xạ control và sáu cột danh sách như trên.
- Người dùng đã đồng ý không thêm hai nút, dùng xử lý thay thế đã mô tả, bỏ vùng thông tin tổng hợp, dùng control thông báo hiển thị lỗi khi lưu.
- Giữ nguyên nội dung Label, tên thẻ, tiêu đề và bố cục; không tự thêm hoặc di chuyển control.
- `rtbLyDoSua` nhập và lưu lý do; mã phân loại giữ nguyên, văn bản lưu qua `Note/GhiChu` hiện có.
- Đã thống nhất dùng Label `lblTacDong` hiển thị lỗi, không thêm RichTextBox.

Chỉ thay mã nối giao diện theo kế hoạch triển khai; không sửa Designer, truy vấn, cấu trúc database hoặc B3.
