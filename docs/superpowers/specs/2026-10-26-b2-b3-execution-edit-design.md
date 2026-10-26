# Thiết kế sửa thông tin thực hiện kế hoạch tại B2

Ngày: 26/10/2026. Trạng thái: người dùng đã duyệt tài liệu trong cuộc trò chuyện; chuyển sang lập kế hoạch triển khai.

## 1. Mục tiêu

Cho người dùng sửa trực tiếp thông tin B3 đã thực hiện trong cửa sổ “Chi tiết thực hiện kế hoạch” của B2. Người dùng sửa trên bảng và các trường bên dưới, chọn lý do, bấm một nút “Lưu thay đổi”. Không có nút chuyển chế độ chỉnh sửa hoặc popup điều chỉnh trung gian.

Ba nghiệp vụ: cắt tiếp cuộn lẻ; lần đầu cắt cuộn chẵn thành lẻ; lấy nguyên cuộn chẵn. Kết quả sửa phải phản ánh đúng tồn kho, phần đặt trước, tiến độ, QR đang sử dụng và lịch sử kiểm tra.

## 2. Các quyết định người dùng đã chốt

| Câu | Quyết định |
| --- | --- |
| 1 | Cập nhật B2 và những phần liên quan của B3; không yêu cầu đổi giao diện B3. |
| 2 | B2 và B3 hoạt động độc lập, cùng truy cập SQLite. B2 không gọi máy chủ B3 để lưu. |
| 3 | Chỉ sửa dữ liệu B3 đã ghi nhận. Không ghi nhận bổ sung Chưa thực hiện → Đã thực hiện tại B2 trong đợt này. |
| 4 | Ai đã mở được form chi tiết đều được sửa; không bổ sung quyền riêng. Giữ cơ chế đăng nhập và khóa dữ liệu hiện có. |
| 5 | Cho sửa kế hoạch còn hiệu lực, kể cả đã hoàn thành. Kế hoạch HUY chỉ xem. |
| 6 | Được hoàn tác nhiều dòng trong một lần lưu. Chỉ xử lý các dòng người dùng đổi, theo thứ tự mới nhất về trước. |
| 7 | Phần mốc cuộn hiện tại độc lập với dòng lịch sử được chọn (7C). Chọn một dòng cũ vẫn có thể sửa mốc hiện tại của đúng cuộn; không thay đổi mốc lịch sử. |
| 8 | Chọn người thực hiện trong tài khoản đang hoạt động hoặc đã ngừng hoạt động. Phải lưu tài khoản người sửa riêng. |
| 9 | Thời gian thực hiện chỉ xem, không được sửa. |
| 10 | Dự án đang phát triển, không xây dựng chức năng xử lý dữ liệu cũ. |
| 11 | Chọn lý do có sẵn là đủ; ghi chú bổ sung không bắt buộc. |
| 12 | Giữ bản nháp chưa lưu trên form lập kế hoạch. Lưu chi tiết không tự lưu hoặc xóa bản nháp; kiểm tra xung đột khi lưu kế hoạch sau đó. |
| 13 | Lưu nhiều dòng theo nguyên tắc tất cả thành công hoặc không ghi dòng nào. Giữ nội dung nhập khi có lỗi. |

Các quy tắc đã thống nhất: hoàn tác chỉ dành cho ghi nhận nhầm và thực tế chưa lấy/cắt; không tự trả chiều dài đã cắt thật vào cuộn; không tự đoán QR; sửa mốc không tăng hoặc giảm chiều dài tồn.

## 3. Cơ sở từ mã hiện tại

- `DG_TonKhoBTP_v02/UI/NghiepVuKhac/Kho/XuatKho/UC_LapKeHoachCatDay.cs`: sự kiện nút Chi tiết gọi `LayChiTietThucHien`, mở form lồng `FrmChiTietThucHien` chỉ xem. Việc mở chi tiết hiện không ghi đè nội dung người dùng đang sửa trên bảng lập kế hoạch.
- `DG_TonKhoBTP_v02/Database/Kho/XuatKho/LapKeHoachCatDay_DB.cs`: truy vấn dùng các ID kế hoạch/hàng/quy cách/cuộn lẻ; tồn cuộn chẵn suy ra từ nguồn nhập trừ lấy nguyên và chuyển lẻ; phần chưa thực hiện được dùng tính đặt trước. Cập nhật cấu trúc cơ sở dữ liệu được thực hiện thủ công, không tự đổi cấu trúc khi khởi động.
- `DG_TonKhoBTP_v02/Models/Kho/XuatKho/LapKeHoachCatDay_Model.cs`: mô hình chi tiết hiện chưa mang đủ khóa bản ghi, thông số cắt và phiên bản để sửa an toàn.
- `DG_TonKhoBTP_v02/Models/LoginClass.cs`: đã có tài khoản đăng nhập, tên đăng nhập, ID và tài khoản ngừng hoạt động.
- B3 trong `C:/Users/NK-Group/Desktop/CatDay_DG-main.zip`: `ExecutionService`, `ExecutionRepository`, `InventoryRepository`, `QrUsageRepository`, `CutSessionService`, `B3Transaction`.
- B3 kiểm tra đã thực hiện bằng lịch sử có hiệu lực; dùng `GhiChu` chứa `__B3_OP__:` để chống ghi trùng. Phục hồi phiên cắt hiện so người thực hiện trong lịch sử với tài khoản gửi yêu cầu. Do đó sửa người thực hiện hoặc gỡ lịch sử cần cập nhật cơ chế này.
- B3 dùng `BEGIN IMMEDIATE` để bảo vệ việc ghi. `LichSuCat.DieuChinhThongSo` lưu mốc gốc trước khi B3 điều chỉnh mốc và cắt. `TonCuonLe` chỉ có trạng thái ACTIVE/HET. `KeHoach` chỉ có ACTIVE/HUY; hoàn thành là kết quả tính toán.

## 4. Phương án kiến trúc đã chọn và phương án không dùng

**Đã chọn:** B2 đọc/ghi trực tiếp SQLite qua lớp xử lý riêng, B3 tiếp tục đọc/ghi độc lập. Hai bên có cùng quy ước bảng lịch sử hiệu lực, nhật ký sửa, mã chống ghi trùng và kiểm tra phiên bản. Chia sẻ quy tắc dữ liệu và bộ ca kiểm thử, không bắt buộc chia sẻ thư viện giữa C# và PHP.

**Không dùng:** B2 gửi yêu cầu lưu qua máy chủ B3. Cách này tập trung được xử lý nhưng tạo phụ thuộc vận hành mới; người dùng đã chọn hai ứng dụng hoạt động độc lập.

**Không dùng trong đợt này:** thực hiện mới/ghi nhận bổ sung B3 tại B2. Việc đó cần toàn bộ quy trình quét, chọn nguồn và phân bổ tồn; không thuộc yêu cầu sửa dữ liệu đã ghi nhận.

## 5. Hợp đồng giao diện

### 5.1 Bảng thông tin thực hiện

Hiển thị nghiệp vụ, nhóm/đoạn cắt, số lượng, chiều dài, trạng thái, QR/mã cuộn, thời gian thực hiện, người thực hiện và ghi chú phù hợp. Giữ các ID liên kết trong mô hình nền, không cho người dùng nhập ID.

- Dòng đã thực hiện: trạng thái có thể đổi về Chưa thực hiện nếu là ứng viên hoàn tác. Điều kiện cuối cùng được kiểm tra khi lưu.
- Cho phép đổi nhiều dòng có phụ thuộc với nhau rồi lưu một lần; không khóa vĩnh viễn dòng cũ chỉ vì dòng mới hơn đang còn hiệu lực. Hiển thị phụ thuộc và yêu cầu các dòng liên quan cũng được đổi trong cùng lần lưu.
- Dòng chưa thực hiện: chỉ xem; không cho đổi thành Đã thực hiện. Nếu đây là bản nháp hoàn tác chưa lưu, người dùng có thể đổi ngược về Đã thực hiện để bỏ thay đổi của chính bản nháp.
- Dòng bị chặn chắc chắn bởi nghiệp vụ ngoài phạm vi form hiển thị lý do và không cho lưu hoàn tác.
- Số lượng, chiều dài kế hoạch/đã cắt, nguồn, QR và nhóm chỉ xem. Không bổ sung sửa nguồn hoặc sửa đoạn đã cắt thật.
- Đánh dấu các dòng có thay đổi chưa lưu.

### 5.2 Thông tin dòng đang chọn

- Người thực hiện: chọn tài khoản có trong hệ thống, gồm tài khoản ngừng hoạt động và ghi rõ trạng thái đó. Lưu tên đăng nhập theo quy ước B3; không cho nhập tên tự do.
- Thời gian thực hiện: chỉ xem.
- Các mốc trước cắt, đoạn đã cắt, sau cắt: chỉ xem, gắn với lần thực hiện đang chọn.
- Lý do sửa: danh sách gồm “Ghi nhận nhầm — thực tế chưa lấy/chưa cắt”, “Sai người thực hiện”, “Sai mốc số — giữ nguyên chiều dài tồn”. Ghi chú bổ sung không bắt buộc.
- Tài khoản người sửa luôn lấy từ phiên đăng nhập, không nhập hoặc chọn trên form. Lưu ID, tên đăng nhập và thời điểm sửa riêng với người thực hiện gốc/đã chỉnh.

### 5.3 Mốc cuộn hiện tại — 7C

Là một khu vực sửa theo **cuộn vật lý**, không phải ô sửa lịch sử của dòng đang chọn. Tiêu đề ghi rõ mã cuộn, chiều dài tồn hiện tại. Chọn dòng lịch sử chỉ giúp xác định cuộn; chuyển qua dòng khác của cùng cuộn giữ nguyên bản nháp mốc đang sửa.

- Một cuộn chỉ có một bản nháp mốc cho mỗi lần lưu; nhiều dòng cùng cuộn không tạo nhiều thay đổi mốc cạnh tranh.
- Cho nhập số cuối, chọn chiều +1/-1; số đầu tính từ chiều dài tồn đã đọc từ cơ sở dữ liệu. Tất cả mốc là số nguyên không âm.
- `Số đầu mới = Số cuối mới - Chiều mới × Chiều dài tồn`. Không cho nhập chiều dài tồn hoặc đổi ACTIVE/HET trực tiếp.
- Dùng mốc và phiên bản hiện tại của `TonCuonLe`, kể cả khi dòng được chọn là một lần cắt cũ hoặc có lần cắt thuộc kế hoạch khác.
- Cuộn còn dây: được sửa mốc. Cuộn HET: hiển thị mốc, không sửa mốc để tạo tồn. Hoàn tác đủ điều kiện vẫn có thể làm cuộn có dây trở lại.
- Lấy nguyên cuộn chẵn: không hiển thị khu vực mốc. Không còn xác định được cuộn do nó đã bị hoàn tác ở nơi khác: thông báo dữ liệu thay đổi, không tự gán cuộn khác.

### 5.4 Nút và đóng form

Một nút “Lưu thay đổi” cho toàn bộ bản nháp trong form, “Bỏ thay đổi” và “Đóng”. Không có popup điều chỉnh trung gian. Sau lưu thành công tải lại dữ liệu đã ghi, cập nhật tiến độ/tồn ở form cha và xóa bản nháp đã lưu. Khi đóng với nội dung chưa lưu, hỏi Lưu/Bỏ thay đổi/Quay lại; không tự bỏ nội dung.

## 6. Quy tắc sửa theo nghiệp vụ

### 6.1 Sửa người thực hiện

Chỉ cập nhật thông tin người thực hiện có hiệu lực và nhật ký trước/sau. Không đổi thời gian, số lượng, mốc, tồn, QR hoặc `KeHoach.Users_id` (người được giao kế hoạch). Danh tính người ban đầu gửi yêu cầu B3 được giữ bất biến cho việc kiểm tra mã yêu cầu và phục hồi phiên.

### 6.2 Cuộn lẻ cắt lẻ

Hoàn tác chuỗi cuối của các lần cắt trên cùng cuộn, chỉ với các dòng đã được người dùng đổi trạng thái. Kiểm tra toàn bộ lịch sử trên cuộn, không giới hạn kế hoạch đang mở; không tự hoàn tác dòng khác hoặc yêu cầu người dùng sửa dữ liệu của kế hoạch khác từ form hiện tại.

Khôi phục tồn/mốc đúng trạng thái trước chuỗi bị hoàn tác. Nếu B3 có điều chỉnh mốc ngay trong lần cắt, dùng cả `DieuChinhThongSo.truoc_dieu_chinh`, không nhầm mốc đã xác nhận để cắt với trạng thái gốc trong cơ sở dữ liệu. Tính lại ACTIVE/HET và phần đặt trước từ các đoạn trở về chờ cắt.

**Quy tắc bổ sung đã được duyệt cùng thiết kế:** nếu sau đầu chuỗi cắt bị hủy đã có các lần sửa mốc độc lập, khôi phục toàn bộ mốc gốc trước chuỗi; các lần sửa mốc phụ thuộc về sau không còn áp dụng nhưng giữ đầy đủ trong nhật ký. Trên chính form phải hiển thị trước mốc sẽ khôi phục và danh sách sửa mốc mất hiệu lực, kể cả sửa mốc đã mở từ kế hoạch khác. Không thêm bước popup. Đây là tác động của hoàn tác được người dùng thấy trước khi lưu, không phải thay đổi ngầm.

Ví dụ: trước lần ghi nhầm cuộn có mốc `0 → 800`; sau lần ghi nhầm cắt 150 m còn `0 → 650`; sau đó sửa mốc thành `1.000 → 1.650`. Khi hủy lần cắt nhầm, khôi phục `0 → 800`, và lần sửa mốc `1.000 → 1.650` được ghi rõ không còn áp dụng. Nếu cần xác nhận lại số trên cuộn thật, người dùng sửa mốc hiện tại trong lần lưu riêng sau đó, vẫn giữ chiều dài 800 m.

### 6.3 Cuộn chẵn cắt thành lẻ

Chỉ hoàn tác lần chuyển chẵn thành lẻ khi tất cả lần cắt phía sau đã được hoàn tác hợp lệ trong cùng lần lưu hoặc không có lần phía sau; cuộn lẻ phát sinh không có liên kết sử dụng khác ngoài chuỗi được xử lý.

Gỡ các lịch sử hiệu lực trong chuỗi, gỡ liên kết nhóm tạo cuộn, xử lý bản ghi `TonCuonLe` phát sinh và QR chuyển lẻ trong cùng giao dịch. Nhóm cắt giữ ID quy cách và các đoạn kế hoạch, trở lại chờ cắt từ cuộn chẵn. Chặn nếu có nhóm khác, đặt trước hoặc nghiệp vụ khác đang tham chiếu cuộn mà không thể gỡ đúng phạm vi đã chọn.

Tồn cuộn chẵn tăng do không còn bản ghi chuyển lẻ có hiệu lực. **Không cộng thêm vào `TTCuonDay.SoCuon`**, vì đây là nguồn nhập và tồn đang được suy ra từ lịch sử.

### 6.4 Lấy toàn cuộn chẵn

Hoàn tác đúng lịch sử lấy có ID và QR, không sửa một số lượng gộp tùy ý. Trả nhu cầu về chờ lấy, giải phóng sử dụng QR hiện tại sau khi kiểm tra liên kết, lưu vết hủy mã yêu cầu để yêu cầu cũ không được thực hiện lại.

Tồn tăng bằng việc bỏ hiệu lực lấy nguyên, không tăng số cuộn nguồn nhập. Không xử lý nghiệp vụ mang trả cuộn đã lấy thật trong tính năng này.

## 7. Phương án kỹ thuật để bảo toàn lịch sử (đã duyệt cùng tài liệu)

Người dùng đã chốt cần nhật ký và chống ghi trùng; tên bảng và cách tổ chức sau đây là lựa chọn kỹ thuật đề xuất, không phải quyết định người dùng đã tự nêu.

1. Giữ các bảng `LichSuCat`/`LichSuLayCuon` phản ánh **thực hiện còn hiệu lực**, để các truy vấn tồn/đặt trước hiện có tiếp tục có ý nghĩa. Trước khi sửa hoặc gỡ bản ghi hiệu lực, lưu nguyên dữ liệu trước/sau trong nhật ký điều chỉnh bất biến. Bản ghi đã hoàn tác không biến mất khỏi lịch sử kiểm tra.
2. Bổ sung sổ mã thao tác B3 bất biến: mã chống ghi trùng, loại thao tác, khóa nghiệp vụ gốc, kế hoạch, cuộn/QR, danh tính người gửi ban đầu, thứ tự ghi nhận, trạng thái còn hiệu lực/đã hoàn tác. Mã đã hoàn tác vẫn bị từ chối khi gửi lại; một lần thực hiện thật sau đó phải có mã mới.
3. Nhật ký sửa có ID lần lưu duy nhất, tài khoản người sửa, thời điểm sửa, lý do/ghi chú, ID kế hoạch/lịch sử/cuộn, dữ liệu trước/sau đầy đủ, các tác động liên quan và phiên bản trước/sau. Nhật ký không phụ thuộc xóa dây chuyền từ bản ghi đã gỡ.
4. Theo dõi phiên bản đơn điệu theo cuộn/nguồn và các đối tượng nghiệp vụ bị sửa. Mọi thay đổi liên quan ở B2/B3 làm thay đổi phiên bản. Không chỉ dùng giá trị mốc hoặc `MAX(id)` vì hoàn tác có thể đưa mốc trở về giá trị trước đó.
5. Nhật ký thay đổi mốc độc lập là sự kiện theo cuộn, không sửa mốc lịch sử cắt. Lưu đầy đủ mốc trước/sau, thứ tự sự kiện và phiên bản. Khi hoàn tác chuỗi cắt, khôi phục bản ghi trạng thái gốc trước chuỗi và làm hết hiệu lực các sự kiện sửa mốc phụ thuộc về sau theo mục 6.2; lưu quan hệ với lần hoàn tác. Không cần tự suy diễn phép đổi hệ mốc để giữ một điều chỉnh dựa trên chiều dài tồn không còn đúng.
6. Khi sửa người thực hiện, B3 xác thực yêu cầu đã gửi theo danh tính bất biến trong sổ mã thao tác; không lấy người thực hiện đã được B2 sửa làm chủ thể yêu cầu ban đầu.

Không dùng phương án chỉ thêm cờ “đã hủy” rồi để các truy vấn hiện tại tiếp tục cộng cả bản ghi đó. Không ghi đè mã `__B3_OP__:` bằng lý do sửa.

## 8. Ràng buộc và lưu đồng thời

- Mỗi lần lưu dùng một giao dịch ghi SQLite tương đương `BEGIN IMMEDIATE`; có thời gian chờ/thử lại hữu hạn. Bật khóa ngoại trên mọi kết nối ghi liên quan.
- B2 lấy tài khoản người sửa từ ngữ cảnh đăng nhập, kiểm tra kế hoạch ACTIVE và các điều kiện nghiệp vụ ở lớp dữ liệu, không chỉ ở ô nhập.
- Đọc lại dữ liệu/phiên bản và tập phụ thuộc trước khi ghi. Nếu khác bản đã xem, trả lỗi theo dòng/cuộn, không âm thầm ghi đè. Giữ bản nháp cho người dùng đối chiếu; tải dữ liệu mới không tự gộp hai chỉnh sửa mâu thuẫn.
- Xử lý chuỗi hoàn tác theo thứ tự ghi nhận bất biến, không theo thời gian hiển thị. Không hoàn tác bước cũ nếu còn bước sau phụ thuộc chưa nằm trong tập hoàn tác.
- Việc làm hết hiệu lực sửa mốc phụ thuộc theo mục 6.2 là ngoại lệ được định nghĩa rõ; không tự hủy thêm bất kỳ lần cắt, lần lấy, nhóm hoặc đặt trước độc lập nào người dùng chưa chọn. Tính lại phần đặt trước của chính các đoạn được hoàn tác là tác động bắt buộc của nghiệp vụ, không phải hủy đặt trước khác.
- Sau xử lý phải thỏa mãn tồn không âm, tồn cuộn lẻ bằng trị tuyệt đối chênh lệch hai mốc, ACTIVE khi còn dây và HET khi hết, phần đặt trước còn hiệu lực không vượt tồn phù hợp, không có liên kết mồ côi và không đếm đôi một cuộn chẵn/chuyển lẻ.
- Nếu người dùng vừa hoàn tác vừa sửa mốc của cùng cuộn trong một lần lưu, ưu tiên yêu cầu người dùng chọn một loại thay đổi, báo ngay trên form; không âm thầm bỏ bản nháp mốc hoặc tự suy diễn mốc cho chiều dài tồn đã thay đổi. Đây là quy tắc kỹ thuật đề xuất để tránh hai ý định mâu thuẫn.
- Toàn bộ bản ghi nghiệp vụ, QR, phiên bản và nhật ký cùng thành công hoặc cùng quay lui. ID lần lưu giúp tra lại kết quả nếu mất kết nối sau khi ghi; thử lại không tạo tác động lần hai.

## 9. Giữ bản nháp form lập kế hoạch

Lưu chi tiết là thao tác độc lập với bản nháp lập kế hoạch. Cập nhật các thông tin từ DB như đã lấy/đã cắt/tồn/đặt trước, nhưng giữ giá trị người dùng đang nhập, danh sách chờ xóa, người nhận/người cắt/ghi chú và dấu vết bản gốc cần đối chiếu.

Không đánh dấu bản nháp thành đã lưu chỉ vì tải lại tiến độ. Khi sau đó lưu kế hoạch, kiểm tra lại phiên bản/thực hiện và các ràng buộc; nếu bản nháp không còn hợp lệ, nêu phần xung đột để người dùng sửa. Không tự lưu kế hoạch từ nút lưu chi tiết.

## 10. Phạm vi ngoài đợt này

- Thực hiện mới tại B2, nhập tự do danh tính người thực hiện, sửa thời gian thực hiện.
- Đổi QR/nguồn cuộn, chiều dài đoạn đã cắt, số lượng cuộn đã lấy hoặc cấu trúc nhóm đã thực hiện.
- Xử lý dây đã cắt thật bị hỏng/mất, nhập trả cuộn đã lấy thật, kiểm kê tăng/giảm chiều dài.
- Tự hoàn tác các nghiệp vụ cắt/lấy/đặt trước độc lập người dùng chưa chọn, sửa kế hoạch đã HUY, quy trình duyệt/phân quyền mới. Các sửa mốc phụ thuộc có thể hết hiệu lực theo quy tắc rõ ở mục 6.2, được hiển thị trước và giữ nhật ký.
- Hỗ trợ dữ liệu cũ thiếu liên kết. Không vì vậy mà cho phép suy đoán dữ liệu thiếu: bộ dữ liệu phát triển phải tuân thủ cấu trúc mới và lỗi bất toàn vẫn bị từ chối.

## 11. Cập nhật cấu trúc và vị trí B3

B2 đang ở kho mã làm việc; B3 chỉ được cung cấp qua ZIP. Không sửa trực tiếp file ZIP hoặc suy đoán vị trí hệ thống đang chạy. Kế hoạch triển khai phải tạo bản nguồn B3 làm việc trong thư mục riêng thuộc không gian làm việc, giữ nguyên bản đính kèm; không đưa ra yêu cầu triển khai máy chủ ở giai đoạn thiết kế.

Chuẩn bị tập lệnh cập nhật cấu trúc SQLite thủ công, có phiên bản, kiểm tra điều kiện và hướng dẫn áp dụng phối hợp B2/B3. Chỉ bật chức năng ghi sửa sau khi cấu trúc và các bản B2/B3 tương thích. Không tự sửa `D:/Database/QLSX_v02.db`, không tự xóa dữ liệu mẫu để thay cho cập nhật cấu trúc. Không xây cơ chế nâng cấp dữ liệu cũ chưa được yêu cầu.

## 12. Tiêu chí nghiệm thu và kiểm thử

1. Sửa trực tiếp người thực hiện, gồm tài khoản ngừng hoạt động; thời gian thực hiện chỉ xem; tài khoản người sửa không nhập được và luôn lưu riêng.
2. Mốc hiện tại theo 7C: chọn đoạn cũ vẫn sửa đúng cuộn hiện tại; cùng cuộn chỉ có một bản nháp; mốc lịch sử không đổi; chiều dài tồn không đổi.
3. Hoàn tác lần cắt lẻ mới nhất: 800 - 150 = 650 về đúng tồn 800 và đúng hệ mốc gốc, gồm trường hợp B3 điều chỉnh mốc khi cắt.
4. Hoàn tác chuỗi nhiều lần trong một lần lưu; chọn thiếu bước sau hoặc có phụ thuộc ngoài kế hoạch thì không ghi dòng nào.
5. Hoàn tác chuyển chẵn thành lẻ: trả đúng một cuộn chẵn, gỡ đúng nguồn lẻ/nhóm/QR; không mất các đoạn kế hoạch hoặc đếm đôi tồn.
6. Hoàn tác lấy nguyên: trả đúng một lần lấy/QR, tính lại đặt trước; không sửa số lượng nguồn nhập.
7. Gửi lại mã B3 đã hoàn tác bị từ chối; mã mới thực hiện sau đó được kiểm tra theo tồn hiện hành. Sửa người thực hiện không làm sai danh tính người gửi yêu cầu gốc.
8. B2/B3 ghi đồng thời, dữ liệu đổi sau khi mở form, chuyển trạng thái rồi trở lại cùng giá trị, mất kết nối sau khi ghi: không mất cập nhật hoặc ghi trùng.
9. Sửa mốc độc lập rồi hoàn tác: với ví dụ mục 6.2 phải trở lại `0 → 800`; sự kiện đổi sang `1.000 → 1.650` mất hiệu lực nhưng còn nhật ký và được hiển thị trước khi lưu. Kiểm thử cả sửa mốc từ kế hoạch khác; không tự hoàn tác thêm lần cắt/lấy hay đặt trước độc lập và không cộng trả dây vật lý đã cắt.
10. Một dòng trong lô lỗi khiến toàn bộ lô quay lui, gồm nhật ký và QR. Giữ nội dung người dùng đang sửa.
11. Giữ bản nháp form cha và kiểm tra xung đột khi lưu kế hoạch sau đó; không tự lưu/xóa bản nháp.
12. Kế hoạch hoàn thành còn ACTIVE sửa được, HUY chỉ xem; dòng chưa thực hiện không ghi nhận mới tại B2; cuộn HET không tăng tồn qua sửa mốc.
13. Kiểm tra giao diện và hồi quy các kiểm thử hiện có, đặc biệt `tests/LapKeHoachCatDay.ChiTiet.Tests.ps1`; dùng cơ sở dữ liệu thử nghiệm riêng, không ghi vào file dữ liệu được người dùng cung cấp.

## 13. Phần để người triển khai quyết định

Tên chính xác của bảng/lớp và định dạng dữ liệu nhật ký; cách tách form lồng thành tệp riêng; cơ chế ánh xạ giao diện; giới hạn thử lại khi SQLite bận; tổ chức bộ kiểm thử C#/PHP dùng cùng dữ liệu mẫu. Các lựa chọn này không được đổi quy tắc nghiệp vụ hoặc thêm thao tác người dùng ngoài thiết kế.

Người dùng đã duyệt tài liệu. Bước tiếp theo là lập kế hoạch triển khai bằng Superpowers writing-plans; việc duyệt tài liệu không có nghĩa là chức năng đã được lập trình hoặc kiểm thử.

## 14. Kết quả rà soát thiết kế

Đã có một lượt rà soát độc lập theo hướng người triển khai. Điểm còn thiếu là mốc sau hoàn tác khi có sửa mốc độc lập; đã bổ sung quy tắc, ngoại lệ, ảnh hưởng hiển thị và ví dụ nghiệm thu tại mục 6.2, 7, 8, 10 và 12. Sau khi bổ sung quy tắc này, không còn câu hỏi quan trọng cần mở thêm trước khi lập kế hoạch. Người dùng đã duyệt toàn bộ tài liệu, gồm quy tắc bổ sung và các lựa chọn kỹ thuật ở mục 7.
