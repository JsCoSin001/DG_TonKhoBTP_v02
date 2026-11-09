# Thiết kế chi tiết dòng báo cáo tồn kho và lịch sử cắt dây

Ngày 09/11/2026. Trạng thái **Đã được người dùng duyệt**.

## Mục tiêu và phạm vi

Nút **Xem** trên mỗi dòng do `UC_BCTonKho.btnXemTonKho` tạo trong `grvShowBaoCao` mở một cửa sổ chỉ đọc. Cửa sổ hiển thị đúng số liệu của dòng được chọn, giải thích cách tính các cột tồn kho và cho phép tra lịch sử cắt. Phạm vi là chế độ **Báo cáo tồn (Bảng)** của `TonKho_Excel` và dữ liệu đã xác nhận của luồng thực hiện hiện tại.

Hai quyết định người dùng đã chốt là **2A**, danh sách **Nhiều LOT** chỉ gồm các mã nguồn góp tồn chẵn, và **A**, giải thích đặt trước đến cấp kế hoạch.

## Các hướng đã cân nhắc

| Hướng | Phù hợp và đánh đổi |
|---|---|
| Tra lịch sử theo mã trên dòng | Mở nhanh danh sách lần cắt. Cách này trả lời cuộn đã cắt khi nào, nhưng thiếu dữ liệu để đối chiếu số tồn và số đặt trước. |
| Giải thích toàn bộ dòng báo cáo từ cùng một lần tải | Cho biết từng khoản góp vào số tồn và đặt trước, đồng thời truy được lịch sử đúng nguồn. Cần thu thập thêm chi tiết trong giao dịch đọc báo cáo. |

Thiết kế dùng hướng thứ hai theo yêu cầu của người dùng rằng cửa sổ **Xem** phải giải thích đúng dòng đang hiển thị.

Với **Đặt trước**, đã cân nhắc trình bày theo kế hoạch hoặc mở sâu xuống từng lệnh lấy và từng yêu cầu cắt. Người dùng chọn cấp kế hoạch, với các thành phần riêng đủ để cộng lại đúng tổng trên bảng.

## Cửa sổ chi tiết

Phần đầu hiển thị sản phẩm, mã sản phẩm, đơn vị, loại, LOT, số cuộn, chiều dài mỗi cuộn, tồn kho thực, đặt trước, tồn khả dụng và số đầu/cuối khi có. Hiển thị cảnh báo tương ứng với dòng báo cáo. Số liệu dùng cùng đơn vị và định dạng với bảng tồn.

### Cuộn chẵn

- Quy cách của dòng là **sản phẩm + chiều dài mỗi cuộn**. Mỗi nguồn nhập giữ mã `TTThanhPham.MaBin`, mã định danh `TTCuonDay` và các thành phần **Cuộn nhập**, **Đã lấy nguyên**, **Đã chuyển lẻ**, **Cuộn còn lại**. Số cuộn đóng góp và số mét đóng góp áp dụng đúng điều kiện hiện có của báo cáo.
- Một `MaBin` góp tồn hiện mã nguồn trên cột LOT và mở trực tiếp chi tiết nguồn. Từ hai `MaBin` góp tồn trở lên, cột LOT hiện **Nhiều LOT**. Danh sách trong cửa sổ có một dòng cho mỗi mã nguồn, kèm số cuộn và số mét đóng góp. Nhiều nguồn nhập cùng `MaBin` trong cùng quy cách được cộng chung khi trình bày danh sách.
- Với **Nhiều LOT**, người dùng chọn một `MaBin` để xem các nguồn nhập và lịch sử cắt của mã đó trong cùng cửa sổ. Phần đầu tiếp tục hiển thị tổng của cả dòng báo cáo. Các nguồn hết tồn chẵn thuộc mã khác nằm ngoài danh sách. Số liệu đóng góp của nguồn thiếu mã, nếu có, được giải thích riêng để tổng chi tiết khớp số trên bảng.
- Lịch sử theo `MaBin` gồm các lần cắt của những cuộn vật lý có liên kết nguồn nhập thuộc mã và quy cách đang xem. Phân biệt lần cắt đầu tiên từ cuộn chẵn với các lần cắt tiếp trên cuộn lẻ. Khoản giảm tồn cuộn chẵn lấy theo đúng số lần chuyển lẻ mà báo cáo đã tính.

### Cuộn lẻ

- Cột LOT hiện `TonCuonLe.MaCuon`, mã duy nhất của cuộn vật lý. Lịch sử lấy theo mã định danh của đúng cuộn, gồm lần đầu chuyển lẻ nếu có và các lần cắt tiếp.
- Hiện số đầu, số cuối và chiều dài còn lại theo `|Số cuối − Số đầu|`. Dòng có bản ghi `TonCuonLe` đại diện cho đúng một cuộn.
- Nếu dữ liệu nguồn hoặc các mốc bị thiếu, phần giải thích hiển thị dữ liệu sẵn có và cảnh báo đã tạo khi lập dòng.

### Lịch sử, đặt trước và tồn khả dụng

- Lịch sử cắt hiển thị thời gian, mã cuộn, người thực hiện, mã kế hoạch, chiều dài cắt và các cặp số đầu/cuối trước cắt, đoạn cắt và sau cắt. Sắp xếp theo thời gian thực hiện giảm dần, rồi theo mã định danh lần cắt giảm dần khi trùng thời gian. Nguồn hoặc cuộn có lịch sử rỗng hiện thông báo **Chưa có lịch sử cắt**.
- Với cuộn chẵn, mỗi kế hoạch góp đặt trước hiện riêng **Cuộn còn phải lấy**, **Cuộn dành để cắt** và số mét đặt trước. Cuộn còn phải lấy áp dụng phép tính `MAX(0, số cần lấy − số đã lấy)` ở từng quy cách của kế hoạch trước khi cộng theo kế hoạch. Cuộn dành để cắt là các nhóm còn yêu cầu cắt và chưa gắn cuộn lẻ. Tính số mét theo chiều dài mỗi cuộn của dòng. Bảng kế hoạch tiếp tục giải thích tổng đặt trước của cả nhóm khi người dùng đổi `MaBin` trong danh sách.
- Với cuộn lẻ, mỗi kế hoạch góp đặt trước hiện số mét từ các yêu cầu chưa thực hiện của đúng cuộn. Với dòng chỉ có nhu cầu kế hoạch, phần tổng hợp và danh sách kế hoạch vẫn có thể mở từ **Xem**.
- Phần tổng hợp hiển thị **Tồn kho thực = Số cuộn × Chiều dài mỗi cuộn** và **Tồn khả dụng = Tồn kho thực − Đặt trước**. Giá trị âm và cảnh báo được trình bày theo đúng kết quả báo cáo.

## Kết nối với báo cáo hiện tại

- Mở từ cột nút `Button_ExcelMoi` và lấy đối tượng dòng đang gắn trong `DataGridViewRow.Tag`. Liên kết nguồn nhập và cuộn vật lý dùng mã định danh trong dữ liệu. Chuỗi LOT hiển thị phục vụ người đọc.
- Tập hợp chi tiết nguồn, lịch sử cắt và các thành phần đặt trước từ cùng giao dịch đọc SQLite với lần tính báo cáo. Giữ kết quả chi tiết theo đối tượng dòng. Khi người dùng tải lại báo cáo, số liệu tóm tắt và chi tiết được thay cùng nhau.
- Tính phần tổng hợp và phần giải thích từ cùng dữ liệu đầu vào, dùng các công thức tồn hiện hành. Đọc lịch sử theo tập bản ghi liên quan để tránh truy vấn riêng cho từng dòng. Chế độ Excel dùng các cột dữ liệu và chuỗi mã LOT hiện có. Nhãn **Nhiều LOT** thuộc chế độ bảng tương tác.
- Cửa sổ chi tiết dùng WinForms và kết nối đọc hiện hành. Điểm xử lý nút được giới hạn theo cột và kiểu dữ liệu của báo cáo này vì `grvShowBaoCao` được dùng cho nhiều chế độ.

## Tiêu chí nghiệm thu

1. Mỗi dòng của **Báo cáo tồn (Bảng)** mở đúng chi tiết sau khi sắp xếp lại bảng. Mã, số cuộn, số mét, mốc dây và cảnh báo khớp dòng đã chọn.
2. Với LOT nguồn `E1-265081/5-03-00` trong bản sao kiểm thử, chi tiết giải thích `10 − 2 − 2 = 6` cuộn và `6 × 200 = 1.200 m`. Lịch sử của nguồn có bốn lần cắt thuộc hai cuộn vật lý. Mở dòng của cuộn có ba lần cắt trả đúng ba lần theo thứ tự mới nhất trước.
3. Với hai `MaBin` góp tồn cùng quy cách, bảng hiện **Nhiều LOT**. Danh sách có một mục cho mỗi `MaBin` góp tồn, số cuộn chi tiết cộng lại bằng tổng của dòng và chọn từng mục chỉ hiển thị lịch sử của nguồn tương ứng. Kiểm tra thêm mã đã hết tồn và mã giống nhau ở các nguồn nhập khác nhau.
4. Các khoản đặt trước theo kế hoạch cộng lại đúng số trên dòng cho cả hai loại cuộn. Ví dụ một kế hoạch còn phải lấy một cuộn và còn một nhóm cắt chẵn ở quy cách 200 m giải thích đúng `400 m`. Kiểm tra thêm kế hoạch ở quy cách khác, dòng chỉ có đặt trước và tồn khả dụng âm.
5. Dữ liệu thay đổi sau khi bảng được tải vẫn cho phép đối chiếu đúng số liệu đang hiển thị. Sau khi tải lại, bảng và chi tiết phản ánh cùng bộ dữ liệu mới. Kiểm tra lịch sử trống, mốc dây thiếu và chuyển giữa báo cáo bảng, tìm tồn và xuất Excel.

Nguồn đối chiếu trong dự án gồm [đường tính báo cáo](../../../DG_TonKhoBTP_v02/Database/Kho/TonKho/BC_TonKho_DB.cs), [cách tạo cuộn lẻ khi cắt lần đầu](../../../integrations/B3/app/Services/B3/ExecutionService.php) và [dữ liệu kiểm thử](../../verification/uc-bctonkho-evidence/real-independent-sql.txt).
