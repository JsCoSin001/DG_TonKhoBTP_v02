using System;

namespace DG_TonKhoBTP_v02.UI.NghiepVuKhac.KeToan.VatTuPhu
{
    partial class UC_NhapXuatVatTu
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cbxTimDonHelper?.Dispose(); // ← thêm dòng này
                components?.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.panel1 = new System.Windows.Forms.Panel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.panel2 = new System.Windows.Forms.Panel();
            this.tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
            this.label1 = new System.Windows.Forms.Label();
            this.txtNguoiGiaoNhan = new System.Windows.Forms.ComboBox();
            this.label5 = new System.Windows.Forms.Label();
            this.tbxnguoiLam = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.cbxKhoHang = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.cbxNhaCungCap = new System.Windows.Forms.ComboBox();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.rdoLoai = new System.Windows.Forms.RadioButton();
            this.radioButton2 = new System.Windows.Forms.RadioButton();
            this.lblLoai = new System.Windows.Forms.Label();
            this.cbxTimDon = new System.Windows.Forms.ComboBox();
            this.label6 = new System.Windows.Forms.Label();
            this.cbxTimTen = new System.Windows.Forms.ComboBox();
            this.dtNgayNhapXuat = new System.Windows.Forms.DateTimePicker();
            this.label7 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.panel3 = new System.Windows.Forms.Panel();
            this.btnLuu = new System.Windows.Forms.Button();
            this.panel4 = new System.Windows.Forms.Panel();
            this.dgvChiTietDon = new System.Windows.Forms.DataGridView();
            this.id = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.MaDon = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ten = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ma = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.donVi = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.yeuCau = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.thucNhan = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.donGia = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.thanhTien = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ghiChu = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.xoa = new System.Windows.Forms.DataGridViewButtonColumn();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.tableLayoutPanel3.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.panel3.SuspendLayout();
            this.panel4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvChiTietDon)).BeginInit();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.lblTitle);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Margin = new System.Windows.Forms.Padding(6);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1360, 48);
            this.panel1.TabIndex = 1;
            // 
            // lblTitle
            // 
            this.lblTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTitle.Font = new System.Drawing.Font("Tahoma", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTitle.Location = new System.Drawing.Point(0, 0);
            this.lblTitle.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(1360, 48);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "NHẬP VẬT TƯ";
            this.lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.tableLayoutPanel3);
            this.panel2.Controls.Add(this.tableLayoutPanel1);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel2.Location = new System.Drawing.Point(0, 48);
            this.panel2.Name = "panel2";
            this.panel2.Padding = new System.Windows.Forms.Padding(0, 0, 0, 20);
            this.panel2.Size = new System.Drawing.Size(1360, 136);
            this.panel2.TabIndex = 2;
            // 
            // tableLayoutPanel3
            // 
            this.tableLayoutPanel3.ColumnCount = 8;
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 196F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 104F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 212F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 88F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 126F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel3.Controls.Add(this.label1, 0, 0);
            this.tableLayoutPanel3.Controls.Add(this.txtNguoiGiaoNhan, 1, 0);
            this.tableLayoutPanel3.Controls.Add(this.label5, 2, 0);
            this.tableLayoutPanel3.Controls.Add(this.tbxnguoiLam, 3, 0);
            this.tableLayoutPanel3.Controls.Add(this.label3, 4, 0);
            this.tableLayoutPanel3.Controls.Add(this.cbxKhoHang, 5, 0);
            this.tableLayoutPanel3.Controls.Add(this.label2, 6, 0);
            this.tableLayoutPanel3.Controls.Add(this.cbxNhaCungCap, 7, 0);
            this.tableLayoutPanel3.Dock = System.Windows.Forms.DockStyle.Top;
            this.tableLayoutPanel3.Location = new System.Drawing.Point(0, 55);
            this.tableLayoutPanel3.Name = "tableLayoutPanel3";
            this.tableLayoutPanel3.RowCount = 1;
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel3.Size = new System.Drawing.Size(1360, 62);
            this.tableLayoutPanel3.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(3, 22);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(144, 18);
            this.label1.TabIndex = 0;
            this.label1.Text = "Người giao hàng";
            // 
            // txtNguoiGiaoNhan
            // 
            this.txtNguoiGiaoNhan.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtNguoiGiaoNhan.FormattingEnabled = true;
            this.txtNguoiGiaoNhan.Location = new System.Drawing.Point(153, 20);
            this.txtNguoiGiaoNhan.Name = "txtNguoiGiaoNhan";
            this.txtNguoiGiaoNhan.Size = new System.Drawing.Size(190, 27);
            this.txtNguoiGiaoNhan.TabIndex = 3;
            // 
            // label5
            // 
            this.label5.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.Location = new System.Drawing.Point(349, 22);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(98, 18);
            this.label5.TabIndex = 0;
            this.label5.Text = "Người làm";
            this.label5.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // tbxnguoiLam
            // 
            this.tbxnguoiLam.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.tbxnguoiLam.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.tbxnguoiLam.FormattingEnabled = true;
            this.tbxnguoiLam.Items.AddRange(new object[] {
            "Người 1",
            "Người 2",
            "Người 3",
            "Người 4",
            "Người 5",
            "Người 6",
            "Người 7",
            "Người 8",
            "Người 9",
            "Người 10",
            "Người 11",
            "Người 12",
            "Người 13",
            "Người 14",
            "Người 15"});
            this.tbxnguoiLam.Location = new System.Drawing.Point(453, 20);
            this.tbxnguoiLam.Name = "tbxnguoiLam";
            this.tbxnguoiLam.Size = new System.Drawing.Size(206, 27);
            this.tbxnguoiLam.TabIndex = 7;
            // 
            // label3
            // 
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(665, 22);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(82, 18);
            this.label3.TabIndex = 0;
            this.label3.Text = "Kho hàng";
            this.label3.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // cbxKhoHang
            // 
            this.cbxKhoHang.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.cbxKhoHang.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.cbxKhoHang.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.cbxKhoHang.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbxKhoHang.FormattingEnabled = true;
            this.cbxKhoHang.Items.AddRange(new object[] {
            "Kho nguyên vật liệu",
            "Kho văn phòng phẩm NM Đông Giang",
            "Kho Công cụ dụng cụ NM Đông Giang",
            "Kho khuôn",
            "Kho phế liệu",
            "Kho kéo đại",
            "Kho kéo trung",
            "Kho bện tao lần 1",
            "Kho kéo đa đường",
            "Kho bện nhóm, bện tao",
            "Kho bện đồng",
            "Kho bọc cách điện",
            "Kho bện ghép lõi",
            "Kho bọc lót",
            "Kho quấn băng thép,băng nhôm",
            "Kho quấn Mica",
            "Kho bọc thành phẩm",
            "Kho TP Đông Giang",
            "Kho NVL gia công",
            "Kho TP gia công"});
            this.cbxKhoHang.Location = new System.Drawing.Point(753, 20);
            this.cbxKhoHang.Name = "cbxKhoHang";
            this.cbxKhoHang.Size = new System.Drawing.Size(236, 27);
            this.cbxKhoHang.TabIndex = 5;
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(995, 22);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(120, 18);
            this.label2.TabIndex = 1;
            this.label2.Text = "Nhà cung cấp";
            // 
            // cbxNhaCungCap
            // 
            this.cbxNhaCungCap.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.cbxNhaCungCap.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.cbxNhaCungCap.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.cbxNhaCungCap.FormattingEnabled = true;
            this.cbxNhaCungCap.Items.AddRange(new object[] {
            "Ban KH TC DN-CN Tổng Cty DV viễn thông",
            "Bưu điện Hưng Yên",
            "Cty TNHH CN Nhựa Phú Lâm",
            "Cảng Hải Phòng",
            "CH kim khí Hồng ánh",
            "CHXD số 2 - Cty Xăng dầu Quân đội",
            "CH xăng dầu số 85 và CH số 86 - Hà Nội",
            "CN Cty CP công nghiệp Vĩnh Tường",
            "CNCTCP đầu tư xây lắp TM I(XN KD VLXD I)",
            "XNKD hóa chất và vật tư khoa học Hà Nội",
            "CN Cty CP dược phẩm Hưng Yên",
            "CN Cty CP giao nhận vận tải & thương mại",
            "CN Cty CP giao nhận vận tải con ong",
            "CN Công ty cổ phần giao nhận vận tải ngoại thương tại Hải Phòng",
            "CN Cty CP hàng hải Macs tại Hải Phòng",
            "CN Cty CP Hàng hải Sài Gòn tại Miền Bắc",
            "CN CTCP kiểm định an toàn công nghiệp một tại HN",
            "CN Cty CP Navigos Group Việt Nam",
            "CN Cty CP Nhật Việt Tinh",
            "CNCTCP TB truyền thông giáo dục Dân Xuân",
            "CN Cty CP TBĐ Phước Thạnh (toà nhà)",
            "CN Cty CP Thái Minh",
            "CN Công ty cổ phần tiếp vận PL",
            "CN Cty CP TM dịch vụ Thang Long",
            "CN Cty CP vận tải & thuê tàu tại HP",
            "CN Cty CP VT dầu khí VN",
            "CN Cty điện máy - Tp. HCM tại Hà Nội",
            "CN Cty Đông Bắc tại Hà Nội",
            "CN Cty DV vận tải Tân Vĩnh Thịnh",
            "CN Cty hoá chất VLĐ Hải Phòng",
            "CN Cty liên doanh Phili-Orient line",
            "CN Cty TNHH Anh Đào - Nhà hàng SAKURA 3",
            "CN Cty TNHH APM-SAIGON SHIPPING -HP",
            "CN Cty TNHH dvụ thương mại Cát Sơn",
            "CN Cty TNHH điện cơ TECO (Việt Nam)",
            "CN Cty TNHH K Line VN tại Hải Phòng",
            "CN CT TNHH kiểm toán & DV tin học TP HCM",
            "CN CT TNHH KMTC (Việt Nam) tại HP",
            "CN Cty TNHH La Vie tại HN",
            "CN Cty TNHH MTV viễn thông quốc tế",
            "CN Cty TNHH MTV VT Trans VanLinks VN tại HP",
            "CN Cty TNHH N.G.V",
            "CN Cty TNHH NNR Global logistics (VN)",
            "CN Cty TNHH Quốc tế Sao Đỏ",
            "CN Cty TNHH Shipco transport Việt Nam",
            "CN Cty TNHH thương mại Song Hằng",
            "CN Cty TNHH TM&DV GN VT Qtế Trường",
            "CN Cty TNHH TM & SX Hải Đức tại HN",
            "CN Cty TNHH TM DV Tâm cảng tại HP",
            "CN Cty TNHH TM Dvụ Mắt Bão",
            "CN Cty TNHH TM Song Hằng",
            "CN Cty TNHH vận tải quốc tế ITI",
            "CN Cty TNHH Vina Qtế vận chuyển Hoà",
            "CN Cty TNHH Yang ming VN tại Hải Phòng",
            "CN GD Cty TNHH KINTETSU world express VN",
            "CN Công ty TNHH ôtô Ngôi Sao Việt Nam",
            "CN Hà Tây - Cty TNHH TM Việt Hồng",
            "CN HN-Cty TNHH VT và tiếp vận Toàn Cầu",
            "CN Long Biên - Công ty cổ phần ô tô Trường Hải",
            "CN tại HN Cty TNHH Hanjin Shipping",
            "CN Tổng Cty Cổ Phần May Việt Tiến",
            "CN T Cty hàng hải VN-Cty TNHH MTV tại HP",
            "Cty DV hàng hải Vinalines Hải Phòng",
            "CN xăng dầu Hải Dương",
            "CNCty TNHH TM DV XD điện&tự động Ace",
            "CNCtyTNHH thẩm định giá Hoàng Quân",
            "CNHN Cty TNHH PT QT Tân Đạt Dương",
            "CNXD Hưng Yên- Cty xăng dầu B12",
            "CHXD Yên Lý-Cty xăng dầu Nghệ An",
            "Cơ sở điện cơ Trí Tài",
            "Cty 76 - Bộ Quốc Phòng",
            "Cty bảo hiểm BIDV Hải Dương",
            "Công ty bảo hiểm bưu điện Sài Gòn",
            "Cty bảo hiểm dầu khí Đông Đô",
            "Cty bảo hiểm PJICO Thăng Long",
            "Cty bảo hiểm PVI Hà Nội",
            "Cty bảo hiểm PVI Hải Dương",
            "Cty bảo hiểm QBE (Việt Nam)",
            "Cty Bảo Việt Đông Đô",
            "Cty Bh dầu khí Đông Đô",
            "Cty Cấp Nước Hưng yên",
            "Công ty cổ phần cơ điện Trần Phú",
            "Cty CP AEL Việt Nam",
            "Công ty cổ phần ấn Hồng",
            "Công ty cổ phần Anh Linh",
            "CTCP bảo hiểm hàng không - CN Hà Nội",
            "Cty CP bê tông Alpha",
            "Cty CP Bình Khánh",
            "Cty CP bơm Châu Âu",
            "Cty CP bột giặt và hóa chất Đức Giang",
            "Cty CP cảng Nam Hải",
            "Công ty cổ phần cảng Nam Đình Vũ (bỏ)",
            "Công ty cổ phần chế tạo máy điện VN-Hungari",
            "Cty CP cơ điện lạnh Hoàng Đạt",
            "Cty CP cơ khí xây dựng Bình Lộc",
            "Cty CP công nghiệp và PT công nghệ",
            "CTCP ĐT hạ tầng KCN&Đthị Long An(Laico)",
            "Cty CP dịch vụ hàng hóa hàng không",
            "Cty CP dịch vụ kỹ thuật Bảo An",
            "Cty CP Fuco Hà Nội",
            "Cty CP Gia Anh",
            "Cty CP hàng hải liên kết Việt Nam",
            "Công ty Cổ phần kết cấu thép xây dựng",
            "CTCP kim loại màu&nhựa Đồng Việt(DOVINa)",
            "Cty CP kinh doanh dịch vụ Hà Đô",
            "Công ty cổ phần Kính Vạn Hoa",
            "Cty CP kỹ thương Đại Hồng",
            "Cty CP liên sơn Thăng Long",
            "Cty CP logistics Hàng Không",
            "Cty CP máy và thiết bị Lạc Hồng",
            "Cty CP Media Mart Việt Nam",
            "Cty CP Nam Thành",
            "Cty CP Nhật Việt",
            "Cty CP ô tô Kinh Bắc",
            "Cty CP OKS",
            "Cty cổ phẩn quốc tế Bảo Huy",
            "Cty CP Sakura",
            "Cty CP sản xuất Thái Hưng",
            "Công ty CP thương mại - đầu tư Long Biên",
            "CTCP TM&KT khoáng sản Dương Hiếu-CN",
            "Cty CP thương mại Hoàng Hải",
            "Cty CP thương mại TTA Việt Nam",
            "Cty CP thương mại và tiếp vận Hưng",
            "Cty CP thương mại vật liệu điện Nam Hà Nội",
            "Công ty cổ phần tiếp vận tân cảng Miền Bắc",
            "Cty CP tiết kiệm năng lượng Bách Khoa",
            "Cty CP tư vấn khảo sát HD",
            "Cty CP vận chuyển á châu",
            "Công ty TNHH vận tải Duyên Hải",
            "Cty CP vật tư thiết bị văn hoá",
            "Cty CP viễn thông tin học STAPHONE",
            "Cty CP VIETPAM công nghệ",
            "Cty CP Vina Sen",
            "Cty CP xây dựng & PT CN Thịnh Phát",
            "Cty CP xây dựng G7",
            "CTCP xây dựng và TM Vinh Quang 559",
            "CTCP XNK XD&PT công nghệ Hasintech",
            "Cty CP bánh mứt kẹo Hà Nội",
            "Cty CP bảo hiểm hàng không",
            "Cty CP bảo hiểm Petrolimex",
            "Cty CP Bảo hiểm Viễn Đông - VP II",
            "Cty CP bảo hiểm Viễn Đông ( VASS)-",
            "Công ty cổ phần Bền",
            "Cty CP Bluetech",
            "Cty CP Bưu chính Viettel",
            "Cty CP cảng Đoạn Xá",
            "Cty CP Chế tác đá Việt Nam",
            "Cty CP chế tạo bơm Hải Dương",
            "Cty CP chế tạo máy biến áp MiBa",
            "Cty CP CN Thái An Bình",
            "Cty CP CN và XD Techcons",
            "Cty CP cơ khí An Hà",
            "Công ty cổ phần cơ khí Bảo Linh",
            "Cty CP cơ khí điện Long Giang",
            "Cty CP cơ khí Kaisheng",
            "Cty CP cơ khí xây dựng Hùng Mạnh",
            "Cty CP công nghệ & dịch vụ Năng Lượng",
            "Cty CP công nghệ Bách Khoa",
            "Cty CP công nghệ Đỉnh Cao",
            "Cty CP Công Nghệ Nhiệt",
            "Cty CP công nghệ NTECH-I",
            "Cty CP công nghệ tin học EFY Việt N",
            "CTCP công nghệ và đầu tư Thái Dương",
            "Cty CP Công nghiệp Châu á (toà nhà)",
            "Cty CP công nghiệp M.E.C.I",
            "CTCP công nghiệp môi trường 9-URENC",
            "Cty CP công nghiệp Nhất Việt",
            "Cty CP công thương Đông Phương (An Phát Đạt)",
            "Công ty cổ phần container Việt Nam ( Viconship)",
            "Cty CP CPN Hợp Nhất miền Bắc",
            "Cty CP cửa sổ nhựa Châu Âu EUROWIND",
            "CTCP cung ứng và DV kỹ thuật Hàng H",
            "Cty CP đá Đồng Giao",
            "Cty TNHH đại lý vận tải Quốc tế Phía Bắc NORTHFREIGHT",
            "CTCP ĐL hàng hải VN-DV HH Phương Đông",
            "Cty CP đại siêu thị Mê Linh (toà nh",
            "Công ty cổ phần dầu khí Sài Gòn - Hà Nội",
            "Cty CP đầu tư & TM quốc tế Huy Hoàn",
            "Cty CP Đầu tư Công nghệ Ngôi Sao Ch",
            "CTCP Đầu tư lập dự án DL&KD khách sạn",
            "Cty CP đầu tư phát triển cảng Đình",
            "Cty CP đầu tư phát triển Hải Phòng",
            "Cty CP đầu tư phú khang",
            "Cty CP Đầu tư SX&TM Fujivietnam",
            "Cty CP đầu tư thương mại Hùng Hậu",
            "CTCP đầu tư TM in và quảng cáo T&T",
            "CTCP đầu tư TM mực in Phương Nam",
            "Cty CP đầu tư xây dựng & TM Đăng Quang",
            "Cty CP đầu tư xây dựng TKT",
            "Cty CP Đầu tư XD TM Hoàng Anh",
            "Cty CP dây và cáp điện Hàn Quốc",
            "Cty CP địa ốc và XD LANDCON",
            "Cty CP dịch vụ hàng hóa Nội Bài",
            "Cty CP điện công nghiệp Nam Việt",
            "Cty CP ĐT & PT TM ECO-MART Việt Nam",
            "Cty CP ĐT & XD Sao Bắc Việt",
            "Cty CP ĐT TM & XD Đức Phát",
            "Cty CP ĐT XD & TM Phách Duyên",
            "Cty CP ĐT XD khảo sát nền móng Vina",
            "Cty CP ĐTKH CN vật liệu & Kiểm Định",
            "Cty CP Đtư TM và Dịch vụ Minh Thanh",
            "Cty CP Đtư và PT công nghệ An Phúc",
            "Cty CP Đtư và TM Phú Gia VN",
            "Cty CP Đtư và XD Phát Đạt (toà nhà)",
            "Cty CP Đtư XD và KD nước sạch",
            "Cty CP du lịch DVTM Thiên Phú",
            "Cty CP du lịch và thương mại Việt Mỹ",
            "Cty CP DV Báo chí truyền hình VN",
            "Cty CP DV bảo vệ & TM SBC",
            "Cty CP DV du lịch bánh tôm Hồ Tây",
            "Cty CP DV TM và KD Than Hà Nội",
            "Cty CP E&G",
            "Cty CP EMIN Việt Nam",
            "Cty CP Epic Việt Nam",
            "Cty CP FALP Việt Nam",
            "Cty CP FUTURE LIGHT Việt Nam",
            "Cty CP giải pháp & DV tin học MLC",
            "CTCP giải pháp tiết kiệm NL&CN thông tin",
            "Cty CP giao nhận Phương Đông",
            "Cty CP HADDECOR",
            "Cty CP Hàm long",
            "Cty CP HC Thiên Hà",
            "Công ty cổ phần HĐB Hà Nội",
            "Cty CP Hồng Hạc Đại Lải",
            "Cty CP hợp tác & PT thương mại HS",
            "Cty CP huyền thoại bia IMI",
            "Cty CP IFO - ĐTư PT công nghệ",
            "Cty CP in Châu Long",
            "Cty CP in công nghệ cao Đức Phương",
            "Cty CP in hàng không",
            "Cty CP in phụ nữ",
            "Cty CP in và q/cáo mỹ thuật Việt",
            "Cty CP khách sạn Thái Nguyên VVMI",
            "Cty CP khí công nghiệp Việt Nam",
            "Cty CP khí cụ điện I",
            "CTCP kiến trúc - XD và TM Hà Nội",
            "Cty CP Kiến trúc và Xây dựng Hà Việt",
            "Cty CP kim khí Hà Nội",
            "Cty CP KOK Việt Nam",
            "Cty CP kỹ thương Thuận Phát",
            "Cty CP Lê Xuân Bros",
            "Công ty cổ phần liên kết vàng",
            "Cty CP Logistics cảng Sài Gòn",
            "Cty CP môi trường Tây Hồ Hà Nội",
            "Cty CP MT đô thị & CN 11 - URENCO11",
            "Cty CP N.D.C",
            "Cty CP Ngọc Diệp",
            "CTCP Nhà khung thép và thiết bị CN",
            "Cty CP Nhất Nam",
            "Cty CP nhựa Hà Nội",
            "Cty CP Nội Thất Vàng",
            "Cty CP ô tô Long Biên",
            "Cty CP PCS Việt Nam",
            "CN -Công ty CP phần mềm hiệu quả xanh tại Hà Nội",
            "Cty CP phát triển Đại Việt",
            "Cty CP phát triển Hàng Hải",
            "Cty CP Phú Gia - CHXD Thanh Quảng",
            "Cty CP Phương Bắc",
            "Công ty cổ phần PICO",
            "Cty CP PT văn hoá thông tin Đất Việ",
            "CTCP PT XD CN cấp thoát nước & MT",
            "Cty CP PT XD và TM Thuận An -39A",
            "CTCP Q/lý BĐS và Đtư Nhật Quang",
            "Cty CP Qtế Sơn Hà (toà nhà)",
            "Cty CP quốc tế Thạch Dương",
            "Cty CP quốc tế Thịnh Đạt",
            "CTCP sản xuất TM&DV khởi nguồn công",
            "Cty CP sơn tổng hợp Hà Nội",
            "Cty CP Sông La",
            "Cty CP SX & DV thương mại Thanh Hải",
            "Cty CP SX & TM Phạm Dương",
            "Cty CP SX TB công nghệ môi trường VN",
            "Cty CP SX và Phân Phối Minh Anh",
            "Công ty cổ phần sản xuất và thương mại EMIC (Công ty cổ phần đầu tư EPT)",
            "Công ty cổ phần Tân Cảng -189 Hải Phòng",
            "Cty CP tập đoàn Minh Tâm",
            "Cty CP tập đoàn Thái Bình - CN Long",
            "Cty CP tập đoàn Thiên Quang",
            "Cty CP tập đoàn vàng bạc đá quý Doji",
            "Cty CP tập đoàn y học Phúc Lâm",
            "Cty CP TB KT & công nghệ AVCO",
            "Cty CP TBĐ NANO - Phước Thạnh",
            "Cty CP Tbị chiếu sáng ánh Sao",
            "Cty CP Tbị kỹ thuật và công nghệ AV",
            "Cty CP Tbị văn phòng Sao Nam",
            "Cty CP TCMN & MT Hoàng Ngọc",
            "Cty CP TETECO thương mại",
            "Cty CP than sông Hồng",
            "Cty CP Thanh Khang Hà Nội",
            "Cty CP thế giới Bơm",
            "Cty CP thế giới số Trần Anh",
            "Cty CP Thiên Hà VN (toà nhà)",
            "Cty CP thiết bị điện Thảo Anh",
            "CTCP Thiết bị phụ tùng và DV MICO EPT",
            "Công ty cổ phần thiết bị Thắng Lợi",
            "CTCP thiết kế k/trúc kỹ thuật cao",
            "Công ty cổ phần THM",
            "Cty CP thời trang Việt Danh",
            "Cty CP thông tin TM VN",
            "Cty CP thương mại & dịch vụ Thành Gia",
            "Cty CP thương mại & tự động hóa ADI",
            "Cty CP TM & xây dựng Nhật Dương",
            "Cty CP thương mại Nhị Thanh",
            "Cty CP thương mại quốc tế á Châu",
            "Cty CP TM và công nghệ Tân Long",
            "Công ty cổ phần thương mại và phát triển công nghệ Hưng Thiện",
            "Cty CP Tiến Hà",
            "Cty CP tiếp vận QT Song Nguyễn",
            "Cty CP tiếp vận tân cảng Phương Đôn",
            "Cty CP tin học TM Giang Nam",
            "Cty CP TM & DV Bảo Gia VN",
            "Cty CP TM & DV D&T Việt Nam",
            "Cty CP TM & DV Lê ánh Hoa",
            "Cty CP TM & DV Máy XD KOMATSU Việt",
            "Cty CP TM & DV Nam Hà Nội",
            "Cty CP TM & SX bao bì Long Biên",
            "Cty CP TM CN Hoàng Hoa",
            "Cty CP TM điện máy Việt Long",
            "Cty CP TM Đức Việt",
            "Cty CP TM DV Cổng Vàng",
            "Cty CP TM kỹ thuật Đông Nam á",
            "Cty CP TM Sơn Đạt Quang",
            "Cty CP TM tổng hợp Minh Tâm",
            "Công ty cổ phần thương mại và công nghệ mạng Thành Tâm",
            "Cty CP TM và Công nghệ Thanh Xuân",
            "Cty CP TM và DV Ao - Ta",
            "Cty CP TM và DV Tân Việt Phương",
            "Cty CP TM và Dvụ Kha Bình",
            "CTCP TM và Dvụ tổng hợp Sao Việt",
            "Cty CP TM và kỹ thuật điện HN",
            "Cty CP TM và SX Đoàn Minh",
            "Cty CP TM và SX THE ONE (toà nhà)",
            "Cty CP TM và vận tải PETROLIMEX HN",
            "Cty CP TM Việt Hồng (39a)",
            "Cty CP TM XD và PT hệ thống PC",
            "Cty CP TM XD và Xây Lắp Điện",
            "Cty CP TM XNK Bình An",
            "Cty CP TM XNK vật tư kỹ thuật N.H.T",
            "Cty CP Trần Dương Đồng Tiến",
            "Cty CP truyền hình quảng cáo VN",
            "Cty CP Truyền thông Đông Nam",
            "Cty CP Truyền thông Giác quan Thứ Sáu",
            "Cty CP truyền thông thế giới Việt",
            "Cty CP truyền thông Thủ Đô",
            "Cty CP tự động hoá BOSSVN (toà nhà)",
            "Cty CP Tư vấn Thiết kế và XL CDS- C",
            "Cty CP vận tải biển Việt",
            "Cty CP vật liệu chịu lửa Bắc Trung Nam",
            "Cty CP vật tư hàng hải HPC",
            "Cty CP vật tư xăng dầu Hải Dương",
            "Cty CP VC QT và TM Vinh Vân Minh Vân",
            "Công ty cổ phần đầu tư thương mại VIC Việt Nam",
            "Cty CP viễn thông khu vực I",
            "Cty CP Việt An",
            "Cty CP VINLINKS",
            "Cty CP VN Logistics",
            "Cty CP xây dựng & TM Phúc Hưng (Bỏ)",
            "Cty CP xây dựng SX và thương mại Đạ",
            "Cty CP XD&KD thương mại Nghĩa Hoàng",
            "Cty CP xây lẵp thiết bị cơ điện Hà",
            "Cty CP XD & TM LEPRO Việt Nam",
            "Cty CP XD Tỉnh Lào Cai -39",
            "Cty CP XD TM & chế XK Chung Tín TCT",
            "Cty CP XD TM và du lịch Kim Thành",
            "Cty CP XD và kiểm định DHV",
            "Cty CP XD&PT vật liệu mới VN",
            "Cty CP XD và TM Phúc Minh Long Biên",
            "Cty CP XD và TM Tam Dương (toà nhà)",
            "Cty CP XD vận tải Trung á",
            "Cty CP xe nâng Komatsu Việt Nam",
            "Cty CP XNK công nghiệp Việt Nam",
            "Cty CP XNK lương thực-thực phẩm Hà",
            "CTCP XNK TB khoa học&tư vấn quốc tế",
            "Cty CP XNK Tiến Phát",
            "CTCP XNK PT ô tô tải và xe CDViệt T",
            "Cty CP xuất nhập khẩu TH Đức Anh",
            "Cty CPXD &TM PHúc Hưng (NVL) (Bỏ)",
            "Cty dây đồng Việt Nam CFT",
            "Cty điện lực Hoàng Mai",
            "Công ty điện lực Hưng Yên",
            "Công ty điện lực Long Biên",
            "Cty DV truyền thanh - truyền h",
            "Cty Hoàng Sơn",
            "Cty Hưng Thuận",
            "Cty in ấn Đa Sắc",
            "Cty LD TNHH Berjaya Hồ Tây",
            "Cty liên doanh cáp điện LG- Vi",
            "Cty liên doanh khai thác cont. Việt",
            "Cty LD TNHH Nippon Express(Việt Nam",
            "Cty Lữ hành Tourist",
            "Cty luật TNHH Hoàng Quân",
            "Cty PT du lịch hữu hạn làng Nghi Tà",
            "Cty quốc tế Hồ Tây",
            "Cty Siêu Thanh",
            "Cty thương mại Minh Yến",
            "Cty TM & CN tin học DANKO",
            "Công ty thương mại dịch vụ Tràng Thi",
            "Cty TM Hoàng Tiến",
            "Cty TNHH 1 TV Anh Minh",
            "Cty TNHH MTV kiểm định KTAT &tư vấn",
            "Cty TNHH 3H KORBLEND",
            "Cty TNHH 3H Vinacom",
            "Công ty TNHH ắc quy Thái Yến",
            "Cty TNHH An Hảo",
            "Cty TNHH An Hòa",
            "Công ty TNHH An Khanh",
            "Cty TNHH An Khánh (toà nhà)",
            "Cty TNHH An ninh mạng BKAV-CTCP BKA",
            "Cty TNHH An Phong",
            "Cty TNHH An Thảo",
            "Cty TNHH Ăn uống&TC sự kiện Nguyên Đình",
            "Công ty TNHH ánh Sao",
            "Cty TNHH Anh Tú",
            "Cty TNHH Arkema(Cty TNHH Resinoplas VN)",
            "Cty TNHH Bắc Hồng Hà",
            "Cty TNHH bảo hiểm AIG Việt Nam-CN T",
            "Cty TNHH BH Bảo Việt Tokio Marine-CN TPHCM",
            "Cty TNHH bảo hiểm Chartis Việt Nam",
            "Cty TNHH Bào Ngư Thịnh Vượng",
            "Cty TNHH bê tông Thăng Long",
            "Cty TNHH Bến Thuyền",
            "Cty TNHH Bình Giang",
            "Cty TNHH cân điện tử Pro VN",
            "Cty TNHH cảng Hải An",
            "Cty TNHH Cao Cường",
            "Cty TNHH CN và Dvụ Nhật Việt",
            "Cty TNHH CN và TM Hà Sơn",
            "Cty TNHH chuyển phát nhanh DHL - VNPT",
            "Cty TNHH Cơ Điện Đại Dương",
            "Cty TNHH cơ điện Tiến Dũng",
            "Cty TNHH cơ khí may Gia Long",
            "Cty TNHH cơ khí ô tô Đức Hòa",
            "Cty TNHH cơ khí và TM Thành Công",
            "Cty TNHH Cơ nhiệt Hà Nội",
            "Cty TNHH công nghệ lọc quốc tế",
            "Cty TNHH công nghệ Mai Vũ",
            "Cty TNHH công nghệ thông minh Intel",
            "Cty TNHH công nghệ&thiết bị điện Tuấn Hà",
            "Cty TNHH công nghệ và TM ánh Hào",
            "Cty TNHH công nghệ&thương mại Việt",
            "Cty TNHH công nghệ và TM VCOM",
            "Cty TNHH công nghiệp FUSHENG Việt N",
            "Cty TNHH công nghiệp SAKURA Việt Na",
            "Cty TNHH công nghiệp Việt Phát",
            "Cty TNHH Cường Vinh",
            "Cty TNHH Đại Việt",
            "Cty TNHH đầu tư phát triển Minh Tuấn",
            "Cty TNHH ĐTư phát triển SX&TM Hoàng",
            "Cty TNHH Đầu tư PT TM Thiên Hà",
            "Cty TNHH Đầu tư T & M Việt Nam",
            "Cty TNHH đầu tư Trang Anh",
            "Cty TNHH đầu tư và thương mại Topca",
            "Công ty TNHH dịch vụ kho vận ALS",
            "Cty TNHH dịch vụ Tam Anh",
            "Cty TNHH dịch vụ thương mại Hà Nội",
            "Cty TNHH Dịch vụ Thương mại Hoàng Hải",
            "Cty TNHH dịch vụ vận tải Trung Tín",
            "Công ty TNHH điện - điện tử Hùng Thanh",
            "Công ty TNHH điện công nghiệp Tam Anh",
            "Công ty TNHH điện Thành An",
            "Cty TNHH điện tử Hoàn Kiếm",
            "Cty TNHH diệt mối & KT Đất Việt",
            "Cty TNHH đôi cánh á Châu",
            "Cty TNHH ĐT XD và TM Việt Trung-Cty",
            "Cty TNHH đtư PT thương mại Toàn Phá",
            "Cty TNHH Đtư TM và vận tải Gia Kỳ",
            "Cty TNHH Đtư và Dvụ Việt á (toà nhà",
            "Cty TNHH Đtư và PT Phú Lâm",
            "Cty TNHH Đtư và PT XD Minh Huyền",
            "Cty TNHH du lịch - TM CĐ GTVT",
            "Cty TNHH Đức Hoà",
            "Cty TNHH Đức Thắng (toà nhà)",
            "Cty TNHH Dũng Nghiêm",
            "Cty TNHH Dũng Tám",
            "Cty TNHH Duy Nghĩa",
            "Cty TNHH DV & TM Khí Đốt Gia Định HN (Cty CP Khí đốt Gia Định)",
            "Cty TNHH DV & TM Phúc Nga",
            "Cty TNHH DV giao nhận vận tải Quyền Năng",
            "Cty TNHH DV hàng hóa Tân Sơn Nhất",
            "Cty TNHH DV Tiên Phong",
            "Cty TNHH DV tin học FPT",
            "Cty TNHH DV TM Minh Phát",
            "Công ty TNHH dịch vụ bảo vệ Đông á",
            "Cty TNHH Dvụ và PT TM Sao Việt",
            "Cty TNHH ETINCO",
            "Cty TNHH EUROPLAS Việt Nam",
            "Cty TNHH Fuji Carbon Việt Nam",
            "Cty TNHH Fuse Hà Nội -39A",
            "Cty TNHH GATTNER VN",
            "Công ty TNHH Gia Khoa",
            "Cty TNHH Gia Thịnh Hưng Yên",
            "Cty TNHH giải pháp thương hiệu Sao",
            "Cty TNHH giải pháp Tiên Phong",
            "Cty TNHH Giải Pháp Truyền Thông Thi",
            "Công ty TNHH giải trí & tổ chức sự kiện SEM 8 Hà Nội",
            "Cty TNHH giao nhận Chim Ưng Vàng",
            "Cty TNHH giao nhận VT cargonet VN",
            "Cty TNHH giao nhận VC quốc tế Nam Khải",
            "Cty TNHH giao nhận Vibtrans Việt Na",
            "Cty TNHH gốm sứ Bảo Khánh",
            "Cty TNHH gương kính Việt Hùng",
            "Cty TNHH Hạ Long",
            "Cty TNHH Hà Thành",
            "Cty TNHH hải sản Marina",
            "Công ty TNHH Hải Vân",
            "Cty TNHH hãng kiểm toán AASC",
            "Cty TNHH Hậu Giang",
            "Cty TNHH Hậu Giang (toà nhà)",
            "Cty TNHH hệ thống thông tin FPT",
            "Cty TNHH hoá chất Hồng Phát",
            "Cty TNHH Hoàng Trà",
            "Cty TNHH HSIN YUE HSING",
            "Cty TNHH Hữu Nghị á Châu",
            "Cty TNHH Huyền Nguyên Châu",
            "Cty TNHH in Đại Thành",
            "Cty TNHH in và quảng cáo Trần Anh",
            "Cty TNHH in và thương mại Gia Triệu",
            "Cty TNHH Inox Hiển Đạt",
            "Cty TNHH inox Thiên Hà",
            "Công ty TNHH kinh doanh ôtô NISU",
            "Công ty TNHH KDIC",
            "Cty TNHH Keyence Việt Nam",
            "Cty TNHH khách sạn nhà hát",
            "Cty TNHH Khánh Dư",
            "Cty TNHH khoa học và kỹ thuật Hà Nộ",
            "CT TNHH kiểm toán tư vấn định giá A",
            "Cty TNHH kiểm toán Vaco",
            "Công ty TNHH Kim Bàng",
            "Cty TNHH Kim khí Hoàng Gia",
            "Cty TNHH KOMASU Việt Nam",
            "Cty TNHH KSMC",
            "Cty TNHH KT & TM Tiến Thành",
            "Công ty TNHH kỹ thuật Trường Thịnh",
            "Cty TNHH kỹ nghệ Phúc Anh",
            "Cty TNHH KT công nghệ Đức Thành Đạt",
            "Cty TNHH kỹ thuật Hợp Nhất",
            "Cty TNHH kỹ thuật KENT",
            "Cty TNHH kỹ thuật quốc tế Thế Long",
            "Cty TNHH kỹ thuật Tín An",
            "Cty TNHH kỹ thuật và XD Việt Thiên",
            "Cty TNHH Lâm Linh",
            "CT TNHH LD Tiên Phong Transcontaine",
            "Cty TNHH LDKS thống nhất Metropole",
            "Cty TNHH Lê Xuân",
            "Cty TNHH LHC Việt Nam",
            "Cty TNHH Liên Đại Việt",
            "Cty TNHH liên doanh Việt Thái Plast",
            "Cty TNHH Linh Trung",
            "Cty TNHH LOGISTIC PANTOS Việt Nam",
            "Cty TNHH mạng lưới vận tải Trân Châ",
            "Cty TNHH máy tính Hà Nội",
            "Cty TNHH mây tre xuất khẩu Phú Minh",
            "Cty TNHH Metro cash & carry Việt Na",
            "Cty TNHH MGA Việt Nam",
            "Cty TNHH Minh Đức",
            "Cty TNHH Minh Hồng",
            "Cty TNHH Minh Hưng",
            "Cty TNHH Mitsui OSK Lines VN",
            "Cty TNHH MITUTOYO Việt Nam",
            "Cty TNHH MTV-Tổng Cty Tân Cảng Sài Gòn",
            "Cty TNHH Motor N.A VN",
            "Cty TNHH MSC Việt Nam",
            "Cty TNHH MTV Bến Xanh",
            "Cty TNHH MTV bưu chính Viettel HN",
            "Công ty cổ phần cảng Hải Phòng",
            "Cty TNHH MTV chiếu sáng & TB Đô Thị",
            "Cty TNHH MTV Cơ điện lạnh Hải Nam Việt",
            "Cty TNHH MTV du lịch Dvụ HN - HN Toseco",
            "Cty TNHH MTV DV & TM Hân Hoan",
            "Cty TNHH MTV DV Du Lịch & TM Hồng H",
            "Cty TNHH MTV Gemadept Hải Phòng",
            "Công ty TNHH MTV Hòa P.T",
            "Công ty cổ phần in tài chính",
            "CT TNHH MTV KĐ kỹ thuật an toàn Miề",
            "Cty TNHH MTV KS Sunway Hà Nội",
            "Cty TNHH MTV môi trường đô thị Hà N",
            "Cty TNHH MTV Ngọc Anh Bình",
            "Cty cổ phần nước sạch số 2 Hà Nội",
            "Cty TNHH MTV qlý nợ và khai thác TS",
            "Cty TNHH MTV SX TM XNK Tây Nam",
            "Cty TNHH MTV Tbị số DMART",
            "Công ty TNHH MTV thí nghiệm điện Miền Bắc",
            "Cty TNHH MTV TM DV&SX Thịnh Phát VN",
            "Cty TNHH MTV TM & DV Bảo Tuyên",
            "Cty TNHH MTV TM Dvụ Hào Yến",
            "Cty TNHH MTV trung tâm Logistics xanh",
            "Cty TNHH MTV tư vấn&kiểm định an to",
            "Cty TNHH MTV vận tải biển ngôi sao",
            "Công ty TNHH một thành viên VIPA",
            "Cty TNHH MTV VLXD Đông Dương",
            "Cty TNHH Mỹ An",
            "Cty TNHH N&T",
            "Cty TNHH Ngân Xuyến",
            "Cty TNHH ngôi sao xanh tương lai",
            "Công ty TNHH nguồn dự phòng APOLLO Việt Nam",
            "Cty TNHH Nguyên Hà",
            "Cty TNHH Nguyễn Phước Hoàng",
            "Cty TNHH Nhật Linh",
            "Cty TNHH Nhất Minh Sơn",
            "Cty TNHH Như ý ( toà nhà)",
            "Cty TNHH NN một TV Cơ Điện Trần Phú",
            "Cty TNHH NVC Việt Nam",
            "Cty TNHH NYK Line Việt Nam - CN",
            "Công ty TNHH ô tô & thiết bị chuyên dùng Sao Bắc",
            "Cty TNHH ô tô xe máy Khai Phát",
            "Cty TNHH ôtô Hoàng Trà",
            "Cty TNHH phân phối CN viễn thông FPT",
            "Cty TNHH phát triển công nghệ Thái",
            "Cty TNHH phát triển TM Minh Ngọc",
            "Cty TNHH phát triển TM&DV Minh Đức",
            "Cty TNHH Phú Tài",
            "Cty TNHH Phúc Anh",
            "Cty TNHH Phúc Hưng",
            "Cty TNHH Phúc Hưng Thịnh",
            "Cty TNHH Phúc Thịnh",
            "Cty TNHH Phương Anh",
            "Cty TNHH Phương Tuyến",
            "Cty TNHH PT DV TH Hồng Vân",
            "Cty TNHH PT TM Hà Nội",
            "Cty TNHH PTCN khí SH môi trường xan",
            "Cty TNHH q/c sáng tạo Thiên Hà",
            "Cty TNHH q/cáo và TM Trường Thịnh",
            "Cty TNHH QES (Việt Nam)",
            "Công ty TNHH MTV quản lý nợ và khai thác TS NH TMCP Quân đội",
            "Cty TNHH quảng cáo An Tiêm",
            "Cty TNHH Quảng cáo ánh sáng mặt trờ",
            "Cty TNHH Quảng cáo Tầm Nhìn Mới",
            "Cty TNHH quảng cáo Thăng Long",
            "Công ty TNHH quảng cáo trẻ Hoàng Hà",
            "Công ty TNHH Quang Minh",
            "Cty TNHH RINKI Việt Nam",
            "Cty TNHH Rồng Thiên Nga",
            "Cty TNHH SX cơ khí & XD Ngọc Tuyến",
            "Cty TNHH sản xuất dịch vụ TM Thanh",
            "Cty TNHH sản xuất TM và tư vấn Xuân",
            "Cty TNHH sản xuất TM Trung Anh",
            "Cty TNHH sản xuất & TM điện cơ Hoàn",
            "Cty TNHH sản xuất và TM Sơn Đồng",
            "Cty TNHH sản xuất và TM Vương Phú Thịnh",
            "Cty TNHH SX&TM Bảo Hộ LĐ Thuận Thành",
            "Công ty TNHH sản xuất và xuất nhập khẩu Gia Khánh",
            "Cty TNHH Sáu Thành",
            "Cty TNHH Savills Việt Nam",
            "Cty TNHH SC Việt Nam",
            "Cty TNHH Seiwa Kaiun Việt Nam",
            "Cty TNHH SITC LOGISTICS Việt Nam",
            "Công ty TNHH SITC Việt Nam",
            "Cty TNHH Sơn Hải",
            "Cty TNHH sơn Kova",
            "Cty TNHH Sơn Tùng",
            "Cty TNHH sơn Việt Mỹ",
            "Cty TNHH SX & TM Duyên Hải",
            "Cty TNHH SX & TM Hưng Phát",
            "Công ty TNHH sản xuất và thương mại Hương Thảo Hưng Yên",
            "Cty TNHH SX & TM kỹ thuật xanh",
            "Cty TNHH SX & TM Lâm Hùng",
            "Cty TNHH SX & TM Lương Duy",
            "Công ty TNHH sản xuất và thương mại Ngọc Anh Hưng Yên",
            "Cty TNHH SX & TM Sông Hằng",
            "Công ty TNHH sản xuất và thương mại Tiến Đạt Hưng Yên",
            "Cty TNHH SX & TM Tuấn Hùng",
            "Công ty TNHH SX nhựa Tùng Lâm",
            "Cty TNHH SX Tân Đại Thành",
            "Cty TNHH SX TBĐ Tân Quang",
            "Cty TNHH SX thương mại Hòa Bình",
            "Cty TNHH SX TM Anh Minh",
            "Cty TNHH SX TM DV Việt Đô",
            "Cty TNHH SX TM và Dịch vụ Tứ Linh",
            "Cty TNHH SX và TM Anh Thư",
            "Cty TNHH SX và TM DV Thiên Phúc",
            "Cty TNHH SX và TM Hoàng Long",
            "Cty TNHH SX và TM Long Quyền",
            "Cty TNHH SX và TM Mạnh Hùng",
            "Cty TNHH SX và TM máy Việt",
            "Cty TNHH SX VLXD Vinh Huy",
            "Cty TNHH SXTM Đức Phương",
            "Cty TNHH Sỹ Phú",
            "Cty TNHH Tân An",
            "Cty TNHH Tân Huy Hoàng",
            "Cty TNHH Tân Tiên Phong",
            "Cty TNHH EVD TB & PT chất lượng",
            "Cty TNHH TB BHLĐ & TM Đạt Phát",
            "Cty TNHH TBA Việt Nam",
            "Cty tnhh TBCNTân ViệtTiến",
            "Cty TNHH TBĐ Hải Anh (toà nhà)",
            "Cty TNHH Tbị và công nghệ Thiên Sơn",
            "Cty TNHH Thắng Lợi Hà Nội",
            "Cty TNHH thang máy vàTbị Thăng Long",
            "Cty TNHH tháp giải nhiệt công nghiệp",
            "Cty TNHH thép Nhật Quang",
            "Cty TNHH Thiên Hòa An",
            "Cty TNHH thiệp Đức Quyền",
            "Cty TNHH thiết bị & DV công nghệ",
            "Cty TNHH thiết bị công nghiệp G.T.G",
            "Cty TNHH thiết bị điện Phú An",
            "Cty TNHH thiết bị KHKT Mỹ Thành",
            "Công ty TNHH thiết bị phụ tùng Quang Minh",
            "Cty TNHH thiết bị xe nâng Thăng Lon",
            "Cty TNHH thực phẩm Thái Dương",
            "Cty TNHH thương mại - sản xuất Quỳnh Mai",
            "Cty TNHH thương mại & DV Anh Đạt",
            "Cty TNHH thương mại An Đô",
            "Cty TNHH thương mại Bích Lệ",
            "Cty TNHH thương mại Công Minh",
            "Cty TNHH thương mại đầu tư và phát triển",
            "Cty TNHH thương mại dịch vụ EZ LIFE",
            "Cty TNHH TM dịch vụ hàng hóa ANC",
            "Cty TNHH TM dịch vụ sản phẩm Sáng Tạo",
            "Cty TNHH thương mại dịch vụ Tinh Hà",
            "Cty TNHH TM DV và sản xuất Linh Anh",
            "Cty TNHH TM dịch vụ và sản xuất Tùng Lâm",
            "Cty TNHH TM dịch vụ vận tải á Châu",
            "Cty TNHH thương mại Dư Hợp",
            "Cty TNHH thương mại FORTUNE",
            "Công ty TNHH thương mại Hà Anh",
            "Cty TNHH thương mại KCV Việt Nam",
            "Cty TNHH thương mại Khánh Tâm",
            "Cty TNHH thương mại Quang Minh",
            "CT TNHH TMSX điện trở đốt nóng Việt Sinh",
            "Cty TNHH thương mại sản xuất Sơn Hải",
            "Công ty TNHH TM sản xuất và dịch vụ T&T",
            "Cty TNHH thượng mại SHM Hà Nội",
            "Cty TNHH thương mại Sơn Dương",
            "Cty TNHH thương mại Sơn Trang",
            "Cty TNHH thương mại T.N.T Miền Bắc",
            "Cty TNHH thương mại Tân Phát",
            "Cty TNHH TM thiết bị Hưng Tiến",
            "Cty TNHH thương mại Trí Thành",
            "Cty TNHH thương mại Triệu Thành",
            "Cty TNHH TM truyền thông TVCom VN",
            "Cty TNHH TM&DV điện công nghiệp EST",
            "Cty TNHH TM và dịch vụ KEN ĐÔ",
            "Cty TNHH TM và dịch vụ Nguyệt An",
            "Cty TNHH TM và du lịch Thành Việt",
            "Cty TNHH TM và sản xuất Vũ Đạt",
            "Cty TNHH TM và xây dựng Cường Việt",
            "Cty TNHH TM và XNK sơ mi rơ móoc CI",
            "Cty TNHH thương mại VHC",
            "Cty TNHH Thương mại VLXD Thiên Sơn",
            "Cty TNHH thủy lực khí nén Tiến Phát",
            "Cty TNHH Thuỷ Tiên Việt Nam",
            "Cty TNHH Tiến Anh",
            "Cty TNHH Tiến Dựng",
            "Công ty TNHH tiếp vận SITC- Đình Vũ",
            "Cty TNHH tiếp vận và thương mại xan",
            "Cty TNHH Tín Thành",
            "Cty TNHH TIS Việt Nam",
            "Cty TNHH TM & CN Hải Hà",
            "Cty TNHH TM & CN Hồng Dương",
            "Cty TNHH TM & ĐT phát triển công nghệ",
            "Cty TNHH TM & du lịch Hoàng Giang",
            "Cty TNHH TM & DV Bình Lâm",
            "Cty TNHH TM & DV CHC Hà Nội",
            "Cty TNHH TM & DV Hoàn Hảo",
            "Cty TNHH TM & DV Hoàng Hồng Ngọc",
            "Cty TNHH TM & DV Kỹ thuật Sáng Tạo",
            "Cty TNHH TM & DV Phúc Nguyên",
            "Cty TNHH TM & DV tổng hợp Ngọc Linh",
            "Cty TNHH TM & DV tổng hợp Thanh Thúy",
            "Cty TNHH TM & DV vận tải ASEAN",
            "Cty TNHH TM & KD VLĐ Thuận Phát",
            "Cty TNHH TM & SX Hùng Nam",
            "Cty TNHH TM & SX nội thất Đông á",
            "Cty TNHH TM & SX Sáu Thắm",
            "Cty TNHH TM & SX VLS Việt Nam",
            "Cty TNHH TM & XD Linh Đô",
            "Cty TNHH TM & XL điện Việt Nhật",
            "Cty TNHH TM & XNK Đại Phong",
            "Cty TNHH TM & XNK GINACO",
            "Cty TNHH TM & XNK Hoàng Long",
            "Công ty TNHH TM chế biến thuỷ sảnThanh Bình",
            "Cty TNHH TM điện tử và CN Việt Nam",
            "Cty TNHH TM DV bất động sản Hùng Hà",
            "Cty TNHH TM DV KT Hưng Đông Phát",
            "Cty TNHH TM Dvụ Thùy Linh",
            "Cty TNHH TM Dvụ tin học An Phát",
            "Cty TNHH TM Dvụ Trần Gia",
            "Cty TNHH TM Hà Việt",
            "Cty TNHH TM Hòa Thuận",
            "Cty TNHH TM Hồng Tiến",
            "Cty TNHH TM Lê Gia T&T VN",
            "Cty TNHH TM Lightinh Gia Hải",
            "Cty TNHH TM Minh Tiến",
            "Công ty TNHH TM MTV Lâm Long Hải Dương",
            "Cty TNHH TM Phương Hải",
            "Cty TNHH TM qtế & DV siêu thị big C",
            "Công ty TNHH TM quảng cáo & in Hoàng Gia",
            "Cty TNHH TM TN & MT Việt Đức",
            "Cty TNHH TM TBĐ Thái Sơn Bắc",
            "Cty TNHH TM Thái Hưng",
            "Cty TNHH TM Thăng Uy (HN)",
            "Cty TNHH TM Thanh Hồng",
            "Cty TNHH TM Thành Minh",
            "Cty TNHH TM Thành Thái",
            "Cty TNHH TM tổng hợp Thiên Tùng",
            "Cty TNHH TM tổng hợp Vinh Hải",
            "Cty TNHH TM Tri Giang",
            "Cty TNHH TM Trung Đức",
            "Cty TNHH TM & công nghệ thông tin T",
            "Công ty TNHH thương mại và dịch vụ ô tô Bắc Việt",
            "Cty TNHH TM và du lịch Đại Phước",
            "Cty TNHH TM và DV Cao Đạt",
            "Cty TNHH TM và DV du lịch Ngọc Tran",
            "Cty TNHH TM và DV Dũng Sỹ",
            "Cty TNHH TM và DV nội thất Mỹ Lan",
            "Cty TNHH TM và DV Sen",
            "Cty TNHH TM và DV Thắng Thủy",
            "Cty TNHH TM và DV Việt Hồng",
            "Cty TNHH TM và Dvụ Bắc Dương",
            "Cty TNHH TM và Dvụ Chu Minh Sơn",
            "Cty TNHH TM và Dvụ EVISION VN",
            "Cty TNHH TM và Dvụ Gia Thịnh",
            "Cty TNHH TM và Dvụ Hải Huệ",
            "Cty TNHH TM&DV kỹ thuật Nhật Cường",
            "Cty TNHH TM và Dvụ Nam Tân",
            "Cty TNHH TM&DV phát triển Minh Ngọc",
            "Cty TNHH TM và Dvụ Sen Nam Thanh",
            "Cty TNHH TM và Dvụ SX Tuấn Vũ",
            "Cty TNHH TM và Dvụ Thái An",
            "Cty TNHH TM và Dvụ Tính Chính",
            "Cty TNHH TM và Dvụ VSQ",
            "Cty TNHH TM và Dvụ XNK Sao Việt",
            "Cty TNHH TM&PT Công nghệ Quang Minh",
            "Cty TNHH TM và SX Đại Phú",
            "Cty TNHH TM và truyền thông Đại An",
            "Cty TNHH TM và truyền thông Thái Dương",
            "Cty TNHH TM và xây dựng Bình Minh",
            "Cty TNHH TM và XD Đức Việt",
            "Cty TNHH TM và XNK Hòa Bình",
            "Cty TNHH TM và XNK Nhân Phương",
            "Cty TNHH TM XNK Duy Anh",
            "Cty TNHH TMKT Xuân Hiếu",
            "Công ty TNHH toàn cầu Khải Minh",
            "Cty TNHH TOPQ",
            "Công ty TNHH TOYOTA Long Biên",
            "Công ty TNHH Trần Thành",
            "Cty TNHH Trang sức và phụ kiện Sài",
            "Cty TNHH Triều Nhật",
            "Cty TNHH Trúc Lâm",
            "Cty TNHH truyền thông Toàn Cầu",
            "Cty TNHH tự động hóa & tích hợp hệ",
            "Cty TNHH tư vấn C&M",
            "Cty TNHH tư vấn TM DV mặt trời mọc",
            "Cty TNHH tư vấn và thiết kế T.H.A",
            "Cty TNHH tư vấn XD & TM Phương Đông",
            "Công ty TNHH Tuệ Đăng",
            "Cty TNHH vận tải Việt Thanh",
            "Công ty TNHH vật liệu điện Duy Tân",
            "Cty TNHH Vật liệu nhiệt Phát Lộc",
            "Cty TNHH vật tư và xây lắp Minh Hùn",
            "Cty TNHH viễn thông G-L.I.N.K",
            "Công ty TNHH Việt Đức",
            "Cty TNHH Việt Nam EOC",
            "Công ty TNHH Vietskyline",
            "Cty TNHH Vinacompound",
            "Cty TNHH VINA-SAMONE",
            "Cty TNHH VLĐ Linh Trường Phát",
            "Cty TNHH VT TM Vạn Thiên Phúc",
            "Cty TNHH Vũ Minh",
            "Cty TNHH Vũ Tần",
            "Cty TNHH xây dựng Phương Thảo",
            "Cty TNHH xây dựng TM & DV Thái Châu",
            "Cty TNHH xây dựng và TM Việt Tín",
            "Cty TNHH xây dựng và TM Tài Anh",
            "Cty TNHH xây dựng Xuân Hùng",
            "Cty TNHH xây lắp công trình Thành Đ",
            "Cty TNHH xây lắp và SX thép triển p",
            "Cty TNHH XD & TM An Thành Phát",
            "Cty TNHH XD TM và DV Ngọc Ninh",
            "Cty TNHH XD Toàn Thắng Hà Nội",
            "Cty TNHH XD và TM Khánh Hưng",
            "Cty TNHH XNK DV và kỹ thuật Thăng L",
            "Cty TNHH Xuân Lộc Thọ",
            "Cty TNHH Yến Mai",
            "Công ty TNHH Yusen logistics (Việt Nam)",
            "Cty trách nhiệm hữa hạn Minh Hải",
            "Cty trách nhiệm hữu hạn Nhất Ly",
            "Cty văn hóa Hà Nội",
            "Cty xăng dầu Hà Nam Ninh - CN Hà Na",
            "Cty xăng dầu Hà Tĩnh",
            "Cty xăng dầu Nghệ An - CH XD Diễn A",
            "Cty xăng dầu Nghệ An-CHXD Hưng Lợi",
            "Cty xăng dầu Thanh Hóa",
            "Cty xăng dầu Tuyên Quang - Cty TNHH",
            "Cty xây dựng VIGLACERA",
            "Cty XNK Petrolimex",
            "Cửa hàng ăn uống Việt Nhật",
            "Cửa hàng ánh Dương",
            "Cửa hàng Hà Vân",
            "Cửa hàng Hương Thảo",
            "CH Quang Minh - Lê Thị Tuyết Nhung",
            "Cửa hàng Thắng Hiên",
            "Cửa hàng Trung Chính Hồ Thị Linh",
            "Cửa hàng VPP Hùng Anh",
            "Cửa hàng xăng dầu Hà Nội",
            "Cửa hàng xăng dầu Hồ Sen",
            "Cục thuế Hà Nội",
            "Đại Trường Phát - Chung Vĩ Cảnh",
            "Đặng Ngọc Dung",
            "Đặng Thị Thu Hằng",
            "Đào Thị Bích Thuận",
            "Đào Văn Khảm",
            "Dịch vụ đo lường Chính Thắng",
            "Điện lực Thái Nguyên",
            "Đinh Thị Diệp Bảo",
            "DNTN Chính Đức",
            "DNTN Đức Thắng",
            "DNTN Hằng Hải (Công ty TNHH thương mại và dịch vụ tổng hợp Hằng Hải)",
            "DNTN Hoàng Chiến",
            "DNTN Kỳ Phương",
            "DNTN phát triển TM Hoàng Phong",
            "DNTN Phương Đông",
            "DNTN sinh vật cảnh Trần Hạnh",
            "DNTN Thiên Phát 99",
            "DNTN Thuận Thành",
            "DNTN Thương Mại Dũng Ngọc",
            "DNTN thương mại Huy Hoàng",
            "DNTN thương mại Ngọc Toàn",
            "Công ty TNHH thương mại Tuấn Tài",
            "DNTN XN Tùng Linh",
            "Đỗ Công Thành",
            "Đỗ Đức Toàn",
            "Đỗ Quang Tú",
            "Đỗ Thanh Tùng",
            "Đỗ Thị Mùa",
            "Đỗ Thu Hà",
            "Đỗ Văn Hiếu",
            "Đỗ Văn Vĩnh",
            "Doãn Thị Hồng - Nhà hàng Mạnh Hùng",
            "Đoàn Tuấn - Hà Nội",
            "Doanh nghiệp Quang Trung",
            "DNTN Chung Hải",
            "DNTN Mai Kha",
            "DNTN Thành Đức",
            "DNTN Thịnh Anh",
            "DNTN Tuấn Ngọc",
            "DNTN Tuyết Hạnh",
            "DNTN Vạn Lộc",
            "Hà Thị Hải Yến",
            "Hồ Đại",
            "Hoàng Lan Anh",
            "Hoàng Thị Nhị",
            "Hoàng Thị Thịnh",
            "Hoàng Thu Hà",
            "Hoàng Tuấn Ngọc",
            "Hoàng Xuân Thủy",
            "Hội tư vấn thuế Việt Nam",
            "Hợp Tác Xã CN Long Biên",
            "Hợp tác xã công nghiệp 20-10",
            "Hợp tác xã Quyết Thắng",
            "HTX công nghiệp Việt Hưng",
            "HTX Long Biên",
            "ILASCOSAIGON - CN HP",
            "Khách sạn Đại á",
            "Kiều Đức Thành",
            "Lê Đình Hải",
            "Lê Đình Tuấn - HN",
            "Lê Hải",
            "Lê Thị Duyên",
            "Lê Thị Loan",
            "Lê Thị Sử",
            "Lê Thị Tuyến",
            "Lương Quốc Hùng",
            "Lương Thị Anh Hoa",
            "Nghiêm Thị Lan Hương",
            "Ngô Quốc Tuấn",
            "Ngô Thị Kim Hương",
            "Ngô Văn Bình",
            "Ngô Văn Luận",
            "Ngon Hải Sản Quang",
            "Nguyễn Bích Chung",
            "Nguyễn Đức Hải",
            "Nguyễn Duy Linh",
            "Nguyễn Ngọc Minh",
            "Nguyễn Ngọc Quỳnh Mai",
            "Nguyễn Thanh Hải -40 Hà Trung",
            "Nguyễn Thanh Hương",
            "Nguyễn Thị Bích Hạnh",
            "Nguyễn Thị Bình",
            "Nguyễn Thị Hậu",
            "Nguyễn Thị Hồng Hạnh",
            "Nguyễn Thị Kim Anh",
            "Nguyễn Thị Kim Chi",
            "Nguyễn Thị Kim Loan",
            "Nguyễn Thị Lệ Dung",
            "Nguyễn Thị Mai Phương",
            "Nguyễn Thị Thu Hương",
            "Nguyễn Thị Thuỷ - Hà Nội",
            "Nguyễn Thị Thúy Nga",
            "Nguyễn Thị Xuân",
            "Nguyễn Thu Giang",
            "Nguyễn Tuấn Anh",
            "Nguyễn Tuấn Hoàng",
            "Nguyễn Văn Đức - Long Biên - HN",
            "Nguyễn Văn Hiếu",
            "Nguyễn Văn Thành ( THM)",
            "Nguyễn Xuân Đức",
            "NH đầu tư & Ptriển Phố Nối - Hy",
            "Phạm Duy Anh",
            "Phạm Duy Mai",
            "Phạm Thị ánh Tuyết",
            "Phạm Thị Hiên",
            "Phan Đức Hồi",
            "Phòng QL ấn chỉ Cục thuế HY",
            "Quán cá Như Quỳnh",
            "Siêu thị Intimex Hưng Yên",
            "Tạp chí công nghiệp",
            "Tạp chí điện Việt Nam",
            "Tạp chí kiểm sát",
            "Tạp chí năng lượng VN",
            "Tạp chí VIETNAM BUSINESS FORUM",
            "Tập đoàn viễn thông QĐ - CN Viettel",
            "Thạch Văn Luận",
            "TNT-Vietrans Express Worldwide (VN)",
            "Tổng Cty BH Bảo Việt",
            "Tổng Cty CP bảo hiểm Quân Đội",
            "TCT hàng không Việt Nam Vietnam airline",
            "Trần Đức Hạnh",
            "Trần Như Xuyên",
            "Trần Thị Kim Huế",
            "Trần Thị Như Ngọc",
            "Trần Văn Hạnh",
            "Trần Văn Sẫm",
            "Trần Vũ Hoàng Linh",
            "Trần Xuân Phan",
            "Trung tâm chứng nhận phù hợp",
            "Trung tâm đăng kiểm xe cơ giới số 29-02V",
            "Công ty cổ phần đăng kiểm cơ giới Long Biên",
            "Trung tâm Đăng kiểm xe cơ giới số 2",
            "TT đăng ký giao dịch TS tại TP Đà N",
            "Trung tâm điện toán truyền số liệu",
            "Trung tâm Dvụ GTGT -VDC Online",
            "TT kỹ thuật tiêu chuẩn đo lường C.l",
            "TT quan trắc, phân tích tài nguyên",
            "Trung tâm sách pháp luật Tài Chính",
            "Trung tâm thí nghiệm điện",
            "Trung tâm thông tin tiêu chuẩn đo lường chất lượng",
            "Vũ Văn Tuyên",
            "Vũ Văn Quang",
            "TT in thêu QC Thanh Bình-Phạm Quốc",
            "TT KT mt & an toàn HC-CN viện hóa h",
            "TT KT MT và AT hoá chất-CN viện HHC",
            "TT kỹ thuật tiều chuẩn đo lường chất lượng 1",
            "TT nghiên cứu địa kỹ thuật",
            "TT quảng cáo và Dvụ phát thanh",
            "TT Tbị máy văn phòng Minh Thanh",
            "TT thiết bị PCCC Ba Đình Kiều Thị M",
            "TT Viễn Thông Di Động Điện Lực",
            "TT VT Văn Lâm - Viễn Thông Hưng Yên",
            "Nguyễn Thị Phượng",
            "TTKD Hoá chất & VTTH",
            "Văn Phòng công chứng Khoái Châu",
            "Văn phòng công chứng Văn Lâm",
            "Văn phòng đăng ký đất đai Hà Nội",
            "Văn phòng tư vấn và chuyển giao CN",
            "Viện hóa học công nghiệp Việt Nam",
            "Viện khoa học và kỹ thuật hạt nhân",
            "TT KDVNPT- HN-CN Tổng Cty DV Viễn thông",
            "Viện tiêu chuẩn chất lượng VN",
            "Viện tin học doanh nghiệp",
            "VIETFRACH Hải Phòng",
            "Vũ ánh Hồng - Hà Nội",
            "Vũ Thị Yến",
            "XN dịch vụ XD & CK",
            "XN Thành Đồng",
            "XN DV vận tải & TM đường sắt Phía Nam",
            "XN môi trường đô thị huyện Gia Lâm",
            "XN PCCC - Cty Thăng Long - Bộ Công",
            "XN TOYOTA Hoàn Kiếm Hà Nội",
            "Công ty TNHH DaiKimCo",
            "Đinh Thị Yến",
            "Công ty TNHH Mol Logistics Việt Nam",
            "Công ty TNHH tư vấn hiệu quả",
            "Cty TNHH TM và chuyển giao CN ánh D",
            "Nguyễn Anh Tuấn (52 Trần Cao Vân)",
            "Công ty TNHH thiết bị đo lường và kiểm nghiệm",
            "Cty TNHH Đtư phát triển SX&TM Hoàng",
            "HKD cửa hàng Hiệp Lực",
            "Công ty TNHH TVGP quản lý năng suất chất lượng",
            "Cty TNHH K.toán & Đ.giá Thăng Long-",
            "Cty CP đầu tư Cung ứng Việt",
            "Cty TNHH thiết bị An Duy",
            "Công ty CP TM & SX Đức Hùng",
            "Công ty TNHH môi trường Bắc Hà",
            "Cty thiết bị đo lường Kiểm Nghiệm (bỏ)",
            "Công ty TNHH kỹ thuật NAPO Việt Nam",
            "Cty CP TM Vĩnh Thạnh Hưng (vitahco)",
            "Công ty TNHH TM & SX Đăng Quang",
            "Cty TNHH bao bì gỗ Đại Trường An",
            "Công ty cổ phần cơ khí ô tô bắc á",
            "Công ty TNHH Vạn Đạt",
            "Cty CP tư vấn ĐT XD công nghiệp Incovina",
            "Công ty TNHH vinaepoxy Việt Nam",
            "Cty TNHH TM XD & xây lắp điện HN",
            "TT KD VNPT- Hưng Yên - CN Tổng công ty dịch vụ viễn thông",
            "Cty CP Tư Vấn Đầu Tư VINAHOUSE (tòa",
            "Công ty TNHH Mô Hình Việt",
            "Cty TNHH giải pháp thương hiệu Sao",
            "Công ty cổ phần phần mềm D2S",
            "Cty CP TM và công nghệ máy tính HN",
            "Chi cục Hải quan QL hàng ĐT gia công",
            "Chi cục Hải quan Hưng yên",
            "Cty TNHH trang tbị BHLĐ Đại An",
            "Cty TNHH cáp điện lực Kevin VN",
            "Cty THN"});
            this.cbxNhaCungCap.Location = new System.Drawing.Point(1121, 20);
            this.cbxNhaCungCap.Name = "cbxNhaCungCap";
            this.cbxNhaCungCap.Size = new System.Drawing.Size(236, 27);
            this.cbxNhaCungCap.TabIndex = 4;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 8;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 120F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 170F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 170F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 128F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 173F));
            this.tableLayoutPanel1.Controls.Add(this.flowLayoutPanel1, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.lblLoai, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.cbxTimDon, 5, 0);
            this.tableLayoutPanel1.Controls.Add(this.label6, 2, 0);
            this.tableLayoutPanel1.Controls.Add(this.cbxTimTen, 3, 0);
            this.tableLayoutPanel1.Controls.Add(this.dtNgayNhapXuat, 7, 0);
            this.tableLayoutPanel1.Controls.Add(this.label7, 6, 0);
            this.tableLayoutPanel1.Controls.Add(this.label4, 4, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 55F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(1360, 55);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.flowLayoutPanel1.Controls.Add(this.rdoLoai);
            this.flowLayoutPanel1.Controls.Add(this.radioButton2);
            this.flowLayoutPanel1.Location = new System.Drawing.Point(123, 13);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(253, 29);
            this.flowLayoutPanel1.TabIndex = 6;
            // 
            // rdoLoai
            // 
            this.rdoLoai.AutoSize = true;
            this.rdoLoai.Checked = true;
            this.rdoLoai.Location = new System.Drawing.Point(3, 3);
            this.rdoLoai.Name = "rdoLoai";
            this.rdoLoai.Size = new System.Drawing.Size(153, 23);
            this.rdoLoai.TabIndex = 0;
            this.rdoLoai.TabStop = true;
            this.rdoLoai.Text = "Theo đơn đề nghị";
            this.rdoLoai.UseVisualStyleBackColor = true;
            this.rdoLoai.CheckedChanged += new System.EventHandler(this.rdoLoai_CheckedChanged);
            // 
            // radioButton2
            // 
            this.radioButton2.AutoSize = true;
            this.radioButton2.Location = new System.Drawing.Point(162, 3);
            this.radioButton2.Name = "radioButton2";
            this.radioButton2.Size = new System.Drawing.Size(60, 23);
            this.radioButton2.TabIndex = 1;
            this.radioButton2.TabStop = true;
            this.radioButton2.Text = "Khác";
            this.radioButton2.UseVisualStyleBackColor = true;
            // 
            // lblLoai
            // 
            this.lblLoai.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblLoai.AutoSize = true;
            this.lblLoai.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblLoai.Location = new System.Drawing.Point(3, 18);
            this.lblLoai.Name = "lblLoai";
            this.lblLoai.Size = new System.Drawing.Size(114, 18);
            this.lblLoai.TabIndex = 1;
            this.lblLoai.Text = "Loại";
            // 
            // cbxTimDon
            // 
            this.cbxTimDon.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.cbxTimDon.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbxTimDon.FormattingEnabled = true;
            this.cbxTimDon.Items.AddRange(new object[] {
            "Người 1",
            "Người 2",
            "Người 3",
            "Người 4",
            "Người 5",
            "Người 6",
            "Người 7",
            "Người 8",
            "Người 9",
            "Người 10",
            "Người 11",
            "Người 12",
            "Người 13",
            "Người 14",
            "Người 15"});
            this.cbxTimDon.Location = new System.Drawing.Point(892, 17);
            this.cbxTimDon.Name = "cbxTimDon";
            this.cbxTimDon.Size = new System.Drawing.Size(164, 27);
            this.cbxTimDon.TabIndex = 7;
            // 
            // label6
            // 
            this.label6.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(382, 18);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(164, 19);
            this.label6.TabIndex = 1;
            this.label6.Text = "Tìm theo tên VT";
            this.label6.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // cbxTimTen
            // 
            this.cbxTimTen.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.cbxTimTen.FormattingEnabled = true;
            this.cbxTimTen.Location = new System.Drawing.Point(552, 14);
            this.cbxTimTen.Name = "cbxTimTen";
            this.cbxTimTen.Size = new System.Drawing.Size(164, 27);
            this.cbxTimTen.TabIndex = 1;
            // 
            // dtNgayNhapXuat
            // 
            this.dtNgayNhapXuat.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.dtNgayNhapXuat.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtNgayNhapXuat.Location = new System.Drawing.Point(1190, 14);
            this.dtNgayNhapXuat.Name = "dtNgayNhapXuat";
            this.dtNgayNhapXuat.Size = new System.Drawing.Size(167, 27);
            this.dtNgayNhapXuat.TabIndex = 8;
            // 
            // label7
            // 
            this.label7.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label7.AutoSize = true;
            this.label7.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label7.Location = new System.Drawing.Point(1062, 18);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(122, 18);
            this.label7.TabIndex = 0;
            this.label7.Text = "Ngày";
            this.label7.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // label4
            // 
            this.label4.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(722, 18);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(164, 19);
            this.label4.TabIndex = 1;
            this.label4.Text = "Tìm theo đơn";
            this.label4.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // panel3
            // 
            this.panel3.Controls.Add(this.btnLuu);
            this.panel3.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel3.Location = new System.Drawing.Point(0, 868);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(1360, 73);
            this.panel3.TabIndex = 3;
            // 
            // btnLuu
            // 
            this.btnLuu.Location = new System.Drawing.Point(672, 6);
            this.btnLuu.Name = "btnLuu";
            this.btnLuu.Size = new System.Drawing.Size(138, 45);
            this.btnLuu.TabIndex = 7;
            this.btnLuu.Text = "Lưu";
            this.btnLuu.UseVisualStyleBackColor = true;
            this.btnLuu.Click += new System.EventHandler(this.btnLuu_Click);
            // 
            // panel4
            // 
            this.panel4.Controls.Add(this.dgvChiTietDon);
            this.panel4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel4.Location = new System.Drawing.Point(0, 184);
            this.panel4.Name = "panel4";
            this.panel4.Size = new System.Drawing.Size(1360, 684);
            this.panel4.TabIndex = 4;
            // 
            // dgvChiTietDon
            // 
            this.dgvChiTietDon.AllowUserToAddRows = false;
            this.dgvChiTietDon.ColumnHeadersHeight = 35;
            this.dgvChiTietDon.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.dgvChiTietDon.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.id,
            this.MaDon,
            this.ten,
            this.ma,
            this.donVi,
            this.yeuCau,
            this.thucNhan,
            this.donGia,
            this.thanhTien,
            this.ghiChu,
            this.xoa});
            this.dgvChiTietDon.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvChiTietDon.Location = new System.Drawing.Point(0, 0);
            this.dgvChiTietDon.Name = "dgvChiTietDon";
            this.dgvChiTietDon.RowHeadersVisible = false;
            this.dgvChiTietDon.RowHeadersWidth = 50;
            this.dgvChiTietDon.RowTemplate.Height = 35;
            this.dgvChiTietDon.Size = new System.Drawing.Size(1360, 684);
            this.dgvChiTietDon.TabIndex = 0;
            this.dgvChiTietDon.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvChiTietDon_CellValueChanged);
            // 
            // id
            // 
            this.id.DataPropertyName = "id";
            this.id.HeaderText = "ID";
            this.id.Name = "id";
            this.id.ReadOnly = true;
            this.id.Width = 70;
            // 
            // MaDon
            // 
            this.MaDon.DataPropertyName = "MaDon";
            this.MaDon.HeaderText = "Mã đơn";
            this.MaDon.Name = "MaDon";
            this.MaDon.Width = 150;
            // 
            // ten
            // 
            this.ten.DataPropertyName = "ten";
            this.ten.HeaderText = "Tên vật tư";
            this.ten.Name = "ten";
            this.ten.ReadOnly = true;
            this.ten.Width = 300;
            // 
            // ma
            // 
            this.ma.DataPropertyName = "ma";
            this.ma.HeaderText = "Mã";
            this.ma.Name = "ma";
            this.ma.ReadOnly = true;
            this.ma.Width = 170;
            // 
            // donVi
            // 
            this.donVi.DataPropertyName = "donVi";
            this.donVi.HeaderText = "Đơn vị";
            this.donVi.Name = "donVi";
            this.donVi.ReadOnly = true;
            // 
            // yeuCau
            // 
            this.yeuCau.DataPropertyName = "yeuCau";
            this.yeuCau.HeaderText = "Yêu Cầu";
            this.yeuCau.Name = "yeuCau";
            this.yeuCau.ReadOnly = true;
            this.yeuCau.Width = 150;
            // 
            // thucNhan
            // 
            this.thucNhan.DataPropertyName = "thucNhan";
            this.thucNhan.HeaderText = "Thực nhận";
            this.thucNhan.Name = "thucNhan";
            this.thucNhan.Width = 150;
            // 
            // donGia
            // 
            this.donGia.DataPropertyName = "donGia";
            this.donGia.HeaderText = "Đơn Giá";
            this.donGia.Name = "donGia";
            this.donGia.Visible = false;
            this.donGia.Width = 150;
            // 
            // thanhTien
            // 
            this.thanhTien.DataPropertyName = "thanhTien";
            this.thanhTien.HeaderText = "Thành tiền";
            this.thanhTien.Name = "thanhTien";
            this.thanhTien.Visible = false;
            this.thanhTien.Width = 200;
            // 
            // ghiChu
            // 
            this.ghiChu.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.ghiChu.DataPropertyName = "ghiChu";
            this.ghiChu.HeaderText = "Ghi Chú";
            this.ghiChu.Name = "ghiChu";
            // 
            // xoa
            // 
            this.xoa.DataPropertyName = "xoa";
            this.xoa.HeaderText = "";
            this.xoa.Name = "xoa";
            this.xoa.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.xoa.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.xoa.Text = "Xoá";
            this.xoa.UseColumnTextForButtonValue = true;
            this.xoa.Width = 70;
            // 
            // UC_NhapXuatVatTu
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 19F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.panel4);
            this.Controls.Add(this.panel3);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "UC_NhapXuatVatTu";
            this.Size = new System.Drawing.Size(1360, 941);
            this.panel1.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.tableLayoutPanel3.ResumeLayout(false);
            this.tableLayoutPanel3.PerformLayout();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.flowLayoutPanel1.ResumeLayout(false);
            this.flowLayoutPanel1.PerformLayout();
            this.panel3.ResumeLayout(false);
            this.panel4.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvChiTietDon)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label lblLoai;
        private System.Windows.Forms.ComboBox txtNguoiGiaoNhan;
        private System.Windows.Forms.ComboBox cbxNhaCungCap;
        private System.Windows.Forms.ComboBox cbxKhoHang;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.Panel panel4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ComboBox cbxTimTen;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.DataGridView dgvChiTietDon;
        private System.Windows.Forms.Button btnLuu;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.RadioButton rdoLoai;
        private System.Windows.Forms.RadioButton radioButton2;
        private System.Windows.Forms.ComboBox tbxnguoiLam;
        private System.Windows.Forms.DataGridViewTextBoxColumn id;
        private System.Windows.Forms.DataGridViewTextBoxColumn MaDon;
        private System.Windows.Forms.DataGridViewTextBoxColumn ten;
        private System.Windows.Forms.DataGridViewTextBoxColumn ma;
        private System.Windows.Forms.DataGridViewTextBoxColumn donVi;
        private System.Windows.Forms.DataGridViewTextBoxColumn yeuCau;
        private System.Windows.Forms.DataGridViewTextBoxColumn thucNhan;
        private System.Windows.Forms.DataGridViewTextBoxColumn donGia;
        private System.Windows.Forms.DataGridViewTextBoxColumn thanhTien;
        private System.Windows.Forms.DataGridViewTextBoxColumn ghiChu;
        private System.Windows.Forms.DataGridViewButtonColumn xoa;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel3;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.DateTimePicker dtNgayNhapXuat;
        private System.Windows.Forms.ComboBox cbxTimDon;
        private System.Windows.Forms.Label label4;
    }
}
