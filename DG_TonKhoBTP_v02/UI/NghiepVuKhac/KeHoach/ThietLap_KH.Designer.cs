namespace DG_TonKhoBTP_v02.UI.NghiepVu.KeHoach
{
    partial class ThietLap_KH
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
            if (disposing && (components != null))
            {
                components.Dispose();
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
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.KieuKH = new System.Windows.Forms.ComboBox();
            this.cbxTinhTrang = new System.Windows.Forms.ComboBox();
            this.label11 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.tbLot = new System.Windows.Forms.ComboBox();
            this.label14 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.tbTenKhachHang = new System.Windows.Forms.TextBox();
            this.dtime = new System.Windows.Forms.DateTimePicker();
            this.label1 = new System.Windows.Forms.Label();
            this.cbxMucDoUuTien = new System.Windows.Forms.ComboBox();
            this.label13 = new System.Windows.Forms.Label();
            this.dtNgayGiao = new System.Windows.Forms.DateTimePicker();
            this.label8 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.cbTen = new System.Windows.Forms.ComboBox();
            this.tbMa = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.tbHangBan = new System.Windows.Forms.NumericUpDown();
            this.tbTong = new System.Windows.Forms.NumericUpDown();
            this.label7 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.tbHangDat = new System.Windows.Forms.NumericUpDown();
            this.label9 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.rtbGhiChu = new System.Windows.Forms.RichTextBox();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnUploadExcel = new System.Windows.Forms.Button();
            this.tbIDMaSP = new System.Windows.Forms.NumericUpDown();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.rtbMauSac = new System.Windows.Forms.RichTextBox();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.tbHangBan)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbTong)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbHangDat)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbIDMaSP)).BeginInit();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.AutoSize = true;
            this.groupBox1.Controls.Add(this.tableLayoutPanel1);
            this.groupBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox1.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.groupBox1.Location = new System.Drawing.Point(0, 0);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(5);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(10);
            this.groupBox1.Size = new System.Drawing.Size(933, 371);
            this.groupBox1.TabIndex = 1;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Thiết lập kế hoạch";
            // 
            // KieuKH
            // 
            this.KieuKH.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.KieuKH.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.KieuKH.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.KieuKH.FormattingEnabled = true;
            this.KieuKH.Items.AddRange(new object[] {
            "Tạo mới",
            "Chỉnh sửa"});
            this.KieuKH.Location = new System.Drawing.Point(3, 30);
            this.KieuKH.Name = "KieuKH";
            this.KieuKH.Size = new System.Drawing.Size(176, 26);
            this.KieuKH.TabIndex = 23;
            this.KieuKH.SelectedIndexChanged += new System.EventHandler(this.KieuKH_SelectedIndexChanged);
            // 
            // cbxTinhTrang
            // 
            this.cbxTinhTrang.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cbxTinhTrang.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbxTinhTrang.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cbxTinhTrang.FormattingEnabled = true;
            this.cbxTinhTrang.Items.AddRange(new object[] {
            "Tạm thời",
            "Ban hành",
            "Huỷ"});
            this.cbxTinhTrang.Location = new System.Drawing.Point(185, 30);
            this.cbxTinhTrang.Name = "cbxTinhTrang";
            this.cbxTinhTrang.Size = new System.Drawing.Size(176, 26);
            this.cbxTinhTrang.TabIndex = 1;
            // 
            // label11
            // 
            this.label11.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(185, 11);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(176, 16);
            this.label11.TabIndex = 1;
            this.label11.Text = "Tình trạng KH";
            // 
            // label4
            // 
            this.label4.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(367, 11);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(176, 16);
            this.label4.TabIndex = 0;
            this.label4.Text = "Mã hành trình";
            // 
            // tbLot
            // 
            this.tbLot.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbLot.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tbLot.FormattingEnabled = true;
            this.tbLot.Location = new System.Drawing.Point(367, 30);
            this.tbLot.Name = "tbLot";
            this.tbLot.Size = new System.Drawing.Size(176, 26);
            this.tbLot.TabIndex = 22;
            this.tbLot.SelectionChangeCommitted += new System.EventHandler(this.tbLot_SelectionChangeCommitted);
            // 
            // label14
            // 
            this.label14.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(3, 11);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(176, 16);
            this.label14.TabIndex = 1;
            this.label14.Text = "Kiểu thiết lập";
            // 
            // label12
            // 
            this.label12.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(731, 11);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(179, 16);
            this.label12.TabIndex = 0;
            this.label12.Text = "Tên khách";
            // 
            // tbTenKhachHang
            // 
            this.tbTenKhachHang.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbTenKhachHang.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tbTenKhachHang.Location = new System.Drawing.Point(731, 30);
            this.tbTenKhachHang.Name = "tbTenKhachHang";
            this.tbTenKhachHang.Size = new System.Drawing.Size(179, 26);
            this.tbTenKhachHang.TabIndex = 10;
            // 
            // dtime
            // 
            this.dtime.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dtime.CustomFormat = "dd/MM/yyyy";
            this.dtime.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.dtime.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtime.Location = new System.Drawing.Point(3, 89);
            this.dtime.Name = "dtime";
            this.dtime.Size = new System.Drawing.Size(176, 26);
            this.dtime.TabIndex = 3;
            this.dtime.Value = new System.DateTime(2025, 12, 25, 14, 21, 7, 0);
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 70);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(176, 16);
            this.label1.TabIndex = 0;
            this.label1.Text = "Ngày nhận";
            // 
            // cbxMucDoUuTien
            // 
            this.cbxMucDoUuTien.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cbxMucDoUuTien.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbxMucDoUuTien.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cbxMucDoUuTien.FormattingEnabled = true;
            this.cbxMucDoUuTien.Items.AddRange(new object[] {
            "Bình thường",
            "Gấp",
            "Đúng kế hoạch"});
            this.cbxMucDoUuTien.Location = new System.Drawing.Point(549, 30);
            this.cbxMucDoUuTien.Name = "cbxMucDoUuTien";
            this.cbxMucDoUuTien.Size = new System.Drawing.Size(176, 26);
            this.cbxMucDoUuTien.TabIndex = 19;
            // 
            // label13
            // 
            this.label13.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(549, 11);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(176, 16);
            this.label13.TabIndex = 20;
            this.label13.Text = "Độ ưu tiên đơn";
            // 
            // dtNgayGiao
            // 
            this.dtNgayGiao.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dtNgayGiao.CustomFormat = "dd/MM/yyyy";
            this.dtNgayGiao.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.dtNgayGiao.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtNgayGiao.Location = new System.Drawing.Point(185, 89);
            this.dtNgayGiao.Name = "dtNgayGiao";
            this.dtNgayGiao.Size = new System.Drawing.Size(176, 26);
            this.dtNgayGiao.TabIndex = 9;
            this.dtNgayGiao.Value = new System.DateTime(2025, 12, 25, 14, 20, 58, 0);
            // 
            // label8
            // 
            this.label8.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(185, 70);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(176, 16);
            this.label8.TabIndex = 0;
            this.label8.Text = "Ngày giao";
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(367, 70);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(176, 16);
            this.label2.TabIndex = 1;
            this.label2.Text = "Tên sản phẩm";
            // 
            // cbTen
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.cbTen, 2);
            this.cbTen.DisplayMember = "Ten";
            this.cbTen.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cbTen.FormattingEnabled = true;
            this.cbTen.Location = new System.Drawing.Point(367, 89);
            this.cbTen.Name = "cbTen";
            this.cbTen.Size = new System.Drawing.Size(358, 26);
            this.cbTen.TabIndex = 1;
            this.cbTen.ValueMember = "Ma";
            this.cbTen.SelectionChangeCommitted += new System.EventHandler(this.cbTen_SelectionChangeCommitted);
            // 
            // tbMa
            // 
            this.tbMa.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbMa.Enabled = false;
            this.tbMa.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tbMa.Location = new System.Drawing.Point(731, 89);
            this.tbMa.Name = "tbMa";
            this.tbMa.Size = new System.Drawing.Size(179, 26);
            this.tbMa.TabIndex = 5;
            // 
            // label3
            // 
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(731, 70);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(179, 16);
            this.label3.TabIndex = 2;
            this.label3.Text = "Mã sản phẩm";
            // 
            // label6
            // 
            this.label6.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(185, 129);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(176, 16);
            this.label6.TabIndex = 0;
            this.label6.Text = "Hàng bán";
            // 
            // tbHangBan
            // 
            this.tbHangBan.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbHangBan.DecimalPlaces = 1;
            this.tbHangBan.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tbHangBan.Location = new System.Drawing.Point(185, 148);
            this.tbHangBan.Maximum = new decimal(new int[] {
            -159383553,
            46653770,
            5421,
            0});
            this.tbHangBan.Name = "tbHangBan";
            this.tbHangBan.Size = new System.Drawing.Size(176, 26);
            this.tbHangBan.TabIndex = 7;
            this.tbHangBan.ValueChanged += new System.EventHandler(this.TinhKLConLai);
            this.tbHangBan.Leave += new System.EventHandler(this.TinhKLConLai);
            // 
            // tbTong
            // 
            this.tbTong.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbTong.DecimalPlaces = 1;
            this.tbTong.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tbTong.Location = new System.Drawing.Point(3, 148);
            this.tbTong.Maximum = new decimal(new int[] {
            -159383553,
            46653770,
            5421,
            0});
            this.tbTong.Name = "tbTong";
            this.tbTong.Size = new System.Drawing.Size(176, 26);
            this.tbTong.TabIndex = 6;
            this.tbTong.Leave += new System.EventHandler(this.TinhKLConLai);
            // 
            // label7
            // 
            this.label7.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(3, 129);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(176, 16);
            this.label7.TabIndex = 0;
            this.label7.Text = "Tổng";
            // 
            // label5
            // 
            this.label5.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(3, 188);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(176, 16);
            this.label5.TabIndex = 0;
            this.label5.Text = "Hàng đặt";
            // 
            // tbHangDat
            // 
            this.tbHangDat.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbHangDat.DecimalPlaces = 1;
            this.tbHangDat.Enabled = false;
            this.tbHangDat.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tbHangDat.Location = new System.Drawing.Point(3, 207);
            this.tbHangDat.Maximum = new decimal(new int[] {
            -159383553,
            46653770,
            5421,
            0});
            this.tbHangDat.Name = "tbHangDat";
            this.tbHangDat.Size = new System.Drawing.Size(176, 26);
            this.tbHangDat.TabIndex = 8;
            // 
            // label9
            // 
            this.label9.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(185, 188);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(176, 16);
            this.label9.TabIndex = 0;
            this.label9.Text = "Màu sắc";
            // 
            // label10
            // 
            this.label10.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(367, 129);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(176, 16);
            this.label10.TabIndex = 0;
            this.label10.Text = "Ghi chú";
            // 
            // rtbGhiChu
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.rtbGhiChu, 3);
            this.rtbGhiChu.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rtbGhiChu.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.rtbGhiChu.Location = new System.Drawing.Point(367, 148);
            this.rtbGhiChu.Name = "rtbGhiChu";
            this.tableLayoutPanel1.SetRowSpan(this.rtbGhiChu, 3);
            this.rtbGhiChu.Size = new System.Drawing.Size(543, 85);
            this.rtbGhiChu.TabIndex = 12;
            this.rtbGhiChu.Text = "";
            // 
            // btnSave
            // 
            this.btnSave.BackColor = System.Drawing.Color.SeaGreen;
            this.btnSave.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnSave.ForeColor = System.Drawing.SystemColors.Control;
            this.btnSave.Location = new System.Drawing.Point(731, 239);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(122, 55);
            this.btnSave.TabIndex = 9;
            this.btnSave.Text = "LƯU";
            this.btnSave.UseVisualStyleBackColor = false;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // btnUploadExcel
            // 
            this.btnUploadExcel.Location = new System.Drawing.Point(549, 239);
            this.btnUploadExcel.Name = "btnUploadExcel";
            this.btnUploadExcel.Size = new System.Drawing.Size(122, 53);
            this.btnUploadExcel.TabIndex = 16;
            this.btnUploadExcel.Text = "Upload excel";
            this.btnUploadExcel.UseVisualStyleBackColor = true;
            // 
            // tbIDMaSP
            // 
            this.tbIDMaSP.Location = new System.Drawing.Point(367, 239);
            this.tbIDMaSP.Maximum = new decimal(new int[] {
            1569325055,
            23283064,
            0,
            0});
            this.tbIDMaSP.Name = "tbIDMaSP";
            this.tbIDMaSP.Size = new System.Drawing.Size(120, 23);
            this.tbIDMaSP.TabIndex = 21;
            this.tbIDMaSP.Visible = false;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tableLayoutPanel1.ColumnCount = 5;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel1.Controls.Add(this.KieuKH, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.cbxTinhTrang, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.label11, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.label4, 2, 0);
            this.tableLayoutPanel1.Controls.Add(this.tbLot, 2, 1);
            this.tableLayoutPanel1.Controls.Add(this.label14, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.btnUploadExcel, 3, 8);
            this.tableLayoutPanel1.Controls.Add(this.label12, 4, 0);
            this.tableLayoutPanel1.Controls.Add(this.tbTenKhachHang, 4, 1);
            this.tableLayoutPanel1.Controls.Add(this.dtime, 0, 3);
            this.tableLayoutPanel1.Controls.Add(this.label1, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.cbxMucDoUuTien, 3, 1);
            this.tableLayoutPanel1.Controls.Add(this.label13, 3, 0);
            this.tableLayoutPanel1.Controls.Add(this.dtNgayGiao, 1, 3);
            this.tableLayoutPanel1.Controls.Add(this.label8, 1, 2);
            this.tableLayoutPanel1.Controls.Add(this.label2, 2, 2);
            this.tableLayoutPanel1.Controls.Add(this.cbTen, 2, 3);
            this.tableLayoutPanel1.Controls.Add(this.tbMa, 4, 3);
            this.tableLayoutPanel1.Controls.Add(this.label3, 4, 2);
            this.tableLayoutPanel1.Controls.Add(this.tbIDMaSP, 2, 8);
            this.tableLayoutPanel1.Controls.Add(this.label6, 1, 4);
            this.tableLayoutPanel1.Controls.Add(this.tbHangBan, 1, 5);
            this.tableLayoutPanel1.Controls.Add(this.tbTong, 0, 5);
            this.tableLayoutPanel1.Controls.Add(this.label7, 0, 4);
            this.tableLayoutPanel1.Controls.Add(this.label5, 0, 6);
            this.tableLayoutPanel1.Controls.Add(this.tbHangDat, 0, 7);
            this.tableLayoutPanel1.Controls.Add(this.label9, 1, 6);
            this.tableLayoutPanel1.Controls.Add(this.rtbMauSac, 1, 7);
            this.tableLayoutPanel1.Controls.Add(this.label10, 2, 4);
            this.tableLayoutPanel1.Controls.Add(this.rtbGhiChu, 2, 5);
            this.tableLayoutPanel1.Controls.Add(this.btnSave, 4, 8);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.tableLayoutPanel1.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tableLayoutPanel1.Location = new System.Drawing.Point(10, 26);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 9;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 27F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 27F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 27F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 27F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 80F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(913, 294);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // rtbMauSac
            // 
            this.rtbMauSac.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rtbMauSac.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.rtbMauSac.Location = new System.Drawing.Point(185, 207);
            this.rtbMauSac.Name = "rtbMauSac";
            this.rtbMauSac.Size = new System.Drawing.Size(176, 26);
            this.rtbMauSac.TabIndex = 11;
            this.rtbMauSac.Text = "";
            // 
            // ThietLap_KH
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupBox1);
            this.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "ThietLap_KH";
            this.Size = new System.Drawing.Size(933, 371);
            this.Load += new System.EventHandler(this.ThietLap_KH_Load);
            this.groupBox1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.tbHangBan)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbTong)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbHangDat)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbIDMaSP)).EndInit();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.ComboBox KieuKH;
        private System.Windows.Forms.ComboBox cbxTinhTrang;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox tbLot;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.RichTextBox rtbGhiChu;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.TextBox tbTenKhachHang;
        private System.Windows.Forms.DateTimePicker dtime;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox cbxMucDoUuTien;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.DateTimePicker dtNgayGiao;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox cbTen;
        private System.Windows.Forms.TextBox tbMa;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.NumericUpDown tbHangBan;
        private System.Windows.Forms.NumericUpDown tbHangDat;
        private System.Windows.Forms.NumericUpDown tbTong;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.NumericUpDown tbIDMaSP;
        private System.Windows.Forms.Button btnUploadExcel;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.RichTextBox rtbMauSac;
    }
}
