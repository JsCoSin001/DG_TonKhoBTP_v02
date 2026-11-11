# Thiết kế gán mã công đoạn khi lưu thành phẩm

Ngày 11/11/2026. Trạng thái **Đã duyệt thiết kế**. Các lựa chọn nghiệp vụ đã chốt là 1A, 2A, 3A, 4A và 5A.

## Cách ghi mã công đoạn

Một hàm nhận mã công đoạn của màn hình và mã sản phẩm được chọn. Hàm trả mã nghiệp vụ cần ghi vào `TTThanhPham.CongDoan`. Với màn hình Ghép lõi (`6`), mã sản phẩm bắt đầu bằng `TP.` trả về `5`. Các đầu vào còn lại trả về mã công đoạn của màn hình, gồm mã `BTP.` ở Ghép lõi. So tiền tố theo mã sản phẩm từ danh mục, bỏ khoảng trắng đầu cuối và bỏ qua khác biệt chữ hoa, chữ thường.

`TTThanhPham.CongDoan` lưu `MaCongDoan`. Hai giá trị ghi là `5` cho Bọc vỏ và `6` cho Ghép lõi. Trong database đã kiểm tra, hai dòng danh mục có `id` tương ứng là `6` và `7`.

| Thao tác | Xử lý |
|---|---|
| Lưu tạm | Ghi mã công đoạn của màn hình. Bản tạm Ghép lõi mang mã `6`. |
| Lưu chính thức hoặc lưu bản sao | Gọi hàm để xác định mã ghi. |
| Chốt bản tạm | Gọi hàm khi cập nhật bản ghi rồi chuyển `Temp` về `0` trong cùng giao dịch. |
| Sửa bản chính thức | So `DanhSachSP_ID` cũ và mới trong giao dịch lưu. Khi đổi sản phẩm trên màn hình Ghép lõi, gọi hàm với sản phẩm mới. Khi sửa các trường khác, giữ phân loại của bản ghi. |

Mã công đoạn của màn hình tiếp tục điều khiển kiểm tra dữ liệu, BOM và ghi chi tiết công đoạn. Thành phẩm nhập ở Ghép lõi lưu chi tiết vào `CD_GhepLoiQB` cùng giao dịch ghi thành phẩm.

## Nhận diện công đoạn thực hiện

Đã cân nhắc dùng dòng chi tiết công đoạn hiện có hoặc bổ sung cột lưu mã công đoạn thực hiện. Người dùng chọn dòng chi tiết hiện có. Với quy tắc duy nhất đang xử lý, cặp mã lưu `5` và chi tiết Ghép lõi đủ để nhận diện công đoạn thực hiện. Cột riêng sẽ biểu diễn nguồn trực tiếp và cần cập nhật cấu trúc database cùng các luồng ghi.

Khi xem, sửa hoặc sao chép từ một màn hình công đoạn, dùng mã công đoạn thực hiện để xác định quyền mở bản ghi theo các điều kiện hiện có. Với bản ghi có mã lưu `5`, kiểm tra sự tồn tại của `CD_GhepLoiQB` qua `CD_GhepLoiQB.TTThanhPham_ID = TTThanhPham.id`. Có dòng chi tiết thì công đoạn thực hiện là Ghép lõi (`6`). Các trường hợp khác dùng mã đã lưu. Bảng chi tiết này còn được dùng cho các công đoạn khác, nên điều kiện nhận diện chỉ xét dòng có mã lưu `5`. Điều kiện tồn tại dùng chung cho các truy vấn cần xác định công đoạn thực hiện.

Ví dụ, bản ghi `TP.31700100` nhập từ Ghép lõi có `TTThanhPham.CongDoan = 5` và chi tiết `CD_GhepLoiQB` gắn đúng ID bản ghi. Tra số thứ tự trên màn hình Ghép lõi mở đúng dữ liệu Ghép lõi. Màn hình thao tác Bọc vỏ chỉ mở các bản ghi có công đoạn thực hiện Bọc vỏ.

## Báo cáo và danh sách chọn

- Báo cáo sản xuất theo ngày hoặc tháng và kiểm tra báo cáo dùng công đoạn thực hiện. Một LOT Ghép lõi lưu mã `5` chỉ thuộc nhóm sản xuất Ghép lõi khi chọn đồng thời Ghép lõi và Bọc vỏ. Báo cáo chi tiết tiếp tục hiển thị từng dòng nguyên vật liệu của LOT.
- Báo cáo tồn kho dùng mã phân loại đã ghi. LOT đủ điều kiện tồn kho xuất hiện trong nhóm Bọc vỏ khi có mã `5`. Các cột chi tiết Bọc vỏ lấy từ dữ liệu Bọc vỏ nếu có.
- Gợi ý sản phẩm lấy theo quy tắc V3 của màn hình Ghép lõi. Các mã `TP.` cần được cho phép trong database. Điều kiện đủ để nhập kho tiếp tục dựa trên dữ liệu nguồn cuộn của luồng nhập kho hiện hành. Tìm nhập kho theo ngày bỏ qua LOT Ghép lõi được xếp Bọc vỏ khi chưa có nguồn cuộn. LOT Bọc vỏ thực tế thiếu nguồn cuộn vẫn được báo bất thường.
- Thiết kế dùng cấu trúc database hiện có và áp dụng ở các thao tác lưu nêu trên.

## Kiểm tra chấp nhận

Kiểm tra lưu trực tiếp `TP.` và `BTP.` ở Ghép lõi, lưu tạm rồi chốt `TP.`, mở lại bằng số thứ tự, đổi sản phẩm theo hai chiều, sửa ghi chú, sao chép và lưu ở công đoạn khác. Đối chiếu báo cáo sản xuất và tồn kho trên cùng một LOT, có cả trường hợp chọn hai công đoạn. Kiểm tra một bản ghi Bọc vỏ thực và một bản ghi Quấn băng để xác nhận điều kiện nhận diện chỉ áp dụng đúng bản ghi cần chuyển nhóm.
