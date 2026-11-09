# Thiết kế tích hợp tìm kiếm tồn kho

Ngày 09/11/2026. Trạng thái **Đã được người dùng duyệt**.

## Phạm vi và quyết định

Nguồn A là mã đang làm việc trên master 573d396, gồm các chỉnh sửa cục bộ của người dùng. Nguồn B là bốn tệp trong gói DG_TonKhoBTP_v02_CHI_FILE_THAY_DOI_DUNG_LAYOUT_2026-11-09.zip. Tích hợp tìm kiếm trên UC_BCTonKho và dùng kết quả đã lọc để hiển thị bảng hoặc xuất Excel.

| Quyết định | Hành vi đã chốt |
|---|---|
| X1A | Dùng chung cửa sổ Xem. Kết quả tìm kiếm giữ chiều dài ban đầu khi xác định được và trạng thái cuộn trong chi tiết. |
| X2A | Phân bổ dự kiến cho cuộn chẵn theo từng kế hoạch. Kế hoạch có ngày sớm đi trước, cùng ngày thì ưu tiên ID nhỏ hơn. |
| X3A | Nhãn Nhiều LOT chỉ tính các MaBin góp tồn chẵn. LOT hết tồn vẫn tra được trong chi tiết khi bật Gồm hết tồn. |
| X4A | Cuộn lẻ liên kết về LOT nguồn nhưng khác chủ sở hữu hiện ngay trên bảng kèm cảnh báo. Số đặt trước lấy theo đúng cuộn vật lý. |
| X5A | Quy cách đặt trước không hợp lệ hiện dòng cảnh báo theo cách báo cáo tổng đang xử lý. |
| X6A | Thao tác mới được xử lý ngay. Kết quả tra cứu cũ bị loại nếu đã có lựa chọn hoặc chế độ mới. |

## Cách tích hợp

| Hướng đã cân nhắc | Đánh đổi |
|---|---|
| Giữ bảng và cửa sổ tra cứu riêng cho nguồn B | Tách rõ phần tìm kiếm, nhưng tạo hai giao diện và hai bộ quy tắc đối soát số tồn. |
| Tích hợp từng chức năng vào luồng hiện tại và dùng chung cửa sổ Xem | Giữ trải nghiệm đối chiếu của A. Cần dựng chi tiết theo đúng phạm vi kết quả tìm kiếm và giải thích được số đặt trước dự kiến. |

Thiết kế dùng hướng thứ hai theo X1A. Phần dữ liệu tích hợp trong BC_TonKho_DB.cs theo cấu trúc Model, Database và UI hiện hành. Sử dụng comboBox1 và đăng ký Form đã có trong dự án. Báo cáo tổng tiếp tục chạy đường tính hiện hành. Phạm vi đọc dữ liệu sử dụng các bảng B1, B2 và B3 đã xác minh, qua DB_Base và giao dịch đọc SQLite.

## Gợi ý và lọc theo định danh

- **Tìm theo Tên SP** gợi ý từng sản phẩm có nguồn nhập B1 hợp lệ và CongDoan = 5. Hiển thị tên và mã để phân biệt các ID trùng tên. Khi đã chọn ID, lấy mọi nguồn nhập hợp lệ của sản phẩm, gồm nguồn hợp lệ ở công đoạn khác.
- **Tìm theo LOT** phân biệt MaBin nguồn và MaCuon của cuộn lẻ. Tra MaBin theo TTThanhPham.id của nguồn và lấy các cuộn phát sinh liên kết qua TTCuonDay. Tra MaCuon theo TonCuonLe.id của đúng cuộn vật lý. Áp dụng phạm vi trước khi gộp kết quả.
- Gồm hết tồn áp dụng cho gợi ý và kết quả. Khi bật, cho tra các nguồn hoặc cuộn xác định được đã hết. Khi tắt, áp dụng điều kiện tồn và giữ các dòng bất thường theo cách báo cáo hiện hành. Giới hạn số gợi ý sau khi xét điều kiện hiển thị, để các kết quả hợp lệ ở phía sau vẫn có thể xuất hiện.
- Giữ các ký tự tìm kiếm đặc biệt dưới dạng ký tự thường trong mẫu SQLite và kiểm tra hủy trong lúc đọc dữ liệu.

## Tồn, đặt trước và cảnh báo

- Dùng quy tắc tính tồn của báo cáo tổng. Dòng chẵn gộp theo ID sản phẩm và chiều dài mỗi cuộn. Dòng lẻ lấy mốc hiện tại và số đặt trước theo ID cuộn vật lý. Lịch sử lấy đúng phạm vi nguồn hoặc cuộn và sắp từ mới đến cũ.
- Cuộn chẵn lấy nhu cầu còn phải lấy và số nhóm cắt chẵn chưa gắn cuộn lẻ của từng kế hoạch đang hoạt động. Giữ cách tính và làm tròn tổng nhu cầu theo từng quy cách kế hoạch của A.
- Xếp toàn bộ nguồn chẵn liên quan theo ngày nhập rồi ID nguồn. Với từng kế hoạch theo thứ tự X2A, cấp nhu cầu từ phần tồn dự kiến còn trống theo FIFO (nguồn nhập trước được phân bổ trước). Lưu số đã phân theo kế hoạch và nguồn để giải thích chính xác số trên từng dòng. Lọc LOT sau bước phân bổ. Số dự kiến được ghi rõ trong bảng chi tiết và khi hiển thị cột đặt trước.
- Nếu nhu cầu vượt số cuộn, giữ quy tắc B ghi phần vượt ở nguồn cuối theo FIFO và kèm cảnh báo. Nếu cả quy cách thiếu nguồn chẵn, giữ dòng nhu cầu chưa có nguồn trong phạm vi sản phẩm. Với quy cách bằng 0 hoặc âm, giữ cảnh báo của A và giá trị đặt trước bằng 0 theo X5A.
- Khi cuộn lẻ thuộc chủ sở hữu khác với LOT nguồn, hiển thị mã sản phẩm thực tế, cảnh báo liên kết và số liệu theo ID cuộn. Số đặt trước lấy từ kế hoạch gắn đúng cuộn và đối chiếu sản phẩm của cuộn. Mốc thiếu hoặc lịch sử thiếu hiển thị theo dữ liệu sẵn có và cảnh báo tương ứng.
- Quy tắc dự phòng đề xuất cho dữ liệu cũ là xếp kế hoạch thiếu ngày hoặc có ngày không đọc được sau kế hoạch có ngày hợp lệ, rồi dùng ID để xếp thứ tự. Các kế hoạch vẫn góp đủ nhu cầu vào tổng.

Ví dụ quy cách 200 m có LOT A và LOT B mỗi LOT còn 2 cuộn. K1 có ngày sớm hơn, cần 1 cuộn. K2 cần 2 cuộn. Kết quả dự kiến ở LOT A là K1 góp 200 m và K2 góp 200 m. LOT B có K2 góp 200 m. Tìm riêng LOT B vẫn phải tính trên cả A và B trước khi lọc, nên dòng B hiện đặt trước 200 m.

## Bảng, cửa sổ Xem và Excel

- Kết quả tìm kiếm sử dụng cùng cấu trúc cột với Báo cáo tồn (Bảng). Nút Xem mở cửa sổ chi tiết hiện tại bằng đối tượng gắn với dòng đã chọn. Số liệu tóm tắt, nguồn nhập, lịch sử và đặt trước cùng xuất phát từ một giao dịch đọc.
- Cột LOT giữ quy tắc Nhiều LOT của báo cáo tổng. Chi tiết phân biệt LOT góp tồn và LOT đã hết hoặc cần đối soát. Khi người dùng xem kết quả Gồm hết tồn, các LOT hết vẫn truy cập được kể cả khi không tính vào nhãn Nhiều LOT. Cuộn lẻ hiện trạng thái, mốc dây và chiều dài ban đầu khi có căn cứ từ dữ liệu nhập.
- Mỗi lượt tra cứu đi theo đúng một đầu ra. Khi btnXuatExcel bỏ tích, cập nhật grvShowBaoCao. Khi btnXuatExcel được tích, dùng luồng chọn tệp và xuất Excel cho kết quả tìm kiếm, với bộ cột xuất hiện hành của B và dấu hiệu dự kiến trong tên tệp theo gói đề xuất. Các nút báo cáo tổng, xuất Excel tổng và báo cáo Word tiếp tục dùng điểm vào của chúng.

## Cập nhật giao diện và tác vụ bất đồng bộ

- Đổi comboBox1 hủy truy vấn cũ, xóa lựa chọn và kết quả thuộc lượt tìm kiếm cũ, rồi tải gợi ý lại từ từ khóa đang gõ. Đổi Gồm hết tồn làm mới gợi ý và tải lại đối tượng đã chọn theo ID nếu còn phù hợp.
- Dùng phiên bản yêu cầu và hủy tác vụ để chặn kết quả cũ cập nhật bảng hoặc mở xuất tệp. Xác minh lại chế độ tìm kiếm, bộ lọc, lựa chọn và chế độ Excel trước khi áp dụng kết quả. Lượt mới ưu tiên ngay cả khi báo cáo tổng đang tải.
- Nếu cần bổ sung khả năng làm mới gợi ý cho ComboBoxSearchHelper, dùng lời gọi riêng cho màn hình này. Các hành vi mặc định và đăng ký của các màn hình dùng chung giữ theo hợp đồng hiện hành.

## Kiểm chứng và bàn giao

1. So sánh gợi ý LOT nguồn, MaCuon và tên trùng nhau theo ID. Kiểm tra tên có nguồn công đoạn 5, sau khi chọn vẫn lấy nguồn hợp lệ ở công đoạn khác. Kiểm tra tìm LOT có cuộn phát sinh và nhiều LOT cùng sản phẩm.
2. Đối chiếu số FIFO theo ngày nhập, quy cách và từng kế hoạch. Kiểm tra phân bổ trước khi lọc LOT, phần vượt tồn, dòng thiếu nguồn, quy cách lỗi, liên kết cuộn khác chủ sở hữu và LOT đã hết.
3. Kiểm tra nút Xem và số liệu chi tiết trên dòng đã sắp xếp, lịch sử mới nhất trước, chiều dài ban đầu và trạng thái. Kiểm tra đổi chế độ liên tục, đổi bộ lọc trong lúc tải và kết quả mới nhất thắng.
4. Kiểm tra chỉ bảng hoặc chỉ Excel theo checkbox. So sánh báo cáo tổng trước và sau trên cùng dữ liệu. Kiểm tra các điểm vào Excel tổng và Word, cùng các hồi quy liên quan B1, B2 và B3.
5. Build WinForms và kiểm thử trên dữ liệu mô phỏng cùng bản sao SQLite mở chỉ đọc. Khi đủ bằng chứng, bàn giao báo cáo thay đổi và ZIP gồm các tệp thực sự thay đổi, theo đúng thư mục tương đối của nguồn A.

Nguồn đối chiếu gồm [đường tính báo cáo](../../../DG_TonKhoBTP_v02/Database/Kho/TonKho/BC_TonKho_DB.cs), [màn hình báo cáo](../../../DG_TonKhoBTP_v02/UI/NghiepVuKhac/Kho/TonKho/UC_BCTonKho.cs) và [thiết kế cửa sổ chi tiết đã duyệt](2026-11-09-chi-tiet-dong-bao-cao-ton-kho-design.md).
