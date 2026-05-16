namespace DG_TonKhoBTP_v02.UI.NghiepVuKhac.Kho
{
    partial class UC_XuatKho
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
            this.label1 = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.dgvLayDL = new System.Windows.Forms.DataGridView();
            this.ttNhapKho_ID = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.tongCD = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.soCuon = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.soDau = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.soCuoi = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ghiChu = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.soCuon_user = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.soDau_user = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.socuoi_user = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ghiChu_user = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.getAll = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.btnXoa = new System.Windows.Forms.Button();
            this.btnSua = new System.Windows.Forms.Button();
            this.btnLuuXuatKho = new System.Windows.Forms.Button();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.label2 = new System.Windows.Forms.Label();
            this.dtNgayXuatKho = new System.Windows.Forms.DateTimePicker();
            this.label3 = new System.Windows.Forms.Label();
            this.cbxTimLOT = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.tbNguoiLam = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.tbLot = new System.Windows.Forms.TextBox();
            this.tbTenSP = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.panel2 = new System.Windows.Forms.Panel();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.dgvLayDL_preview = new System.Windows.Forms.DataGridView();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.label18 = new System.Windows.Forms.Label();
            this.label17 = new System.Windows.Forms.Label();
            this.tbTimKiem = new System.Windows.Forms.TextBox();
            this.id_preview = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.tongCD_preview = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ten_preview = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.lot_preview = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.soCuon_preview = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.soDau_preview = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.soCuoi_preview = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ghiChu_preview = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.SoCuon_user_preview = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.soDau_user_preview = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.soCuoi_user_preview = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ghiChu_user_preview = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.panel1.SuspendLayout();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvLayDL)).BeginInit();
            this.flowLayoutPanel1.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvLayDL_preview)).BeginInit();
            this.tableLayoutPanel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.Dock = System.Windows.Forms.DockStyle.Top;
            this.label1.Font = new System.Drawing.Font("Tahoma", 12.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(0, 0);
            this.label1.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(1375, 53);
            this.label1.TabIndex = 1;
            this.label1.Text = "BÁO CÁO XUẤT KHO";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.groupBox1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 53);
            this.panel1.Margin = new System.Windows.Forms.Padding(4);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1375, 364);
            this.panel1.TabIndex = 2;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.dgvLayDL);
            this.groupBox1.Controls.Add(this.flowLayoutPanel1);
            this.groupBox1.Controls.Add(this.tableLayoutPanel1);
            this.groupBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox1.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.groupBox1.Location = new System.Drawing.Point(0, 0);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(4);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(5);
            this.groupBox1.Size = new System.Drawing.Size(1375, 364);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "groupBox1";
            // 
            // dgvLayDL
            // 
            this.dgvLayDL.AllowUserToAddRows = false;
            this.dgvLayDL.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvLayDL.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.ttNhapKho_ID,
            this.tongCD,
            this.soCuon,
            this.soDau,
            this.soCuoi,
            this.ghiChu,
            this.soCuon_user,
            this.soDau_user,
            this.socuoi_user,
            this.ghiChu_user,
            this.getAll});
            this.dgvLayDL.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvLayDL.Location = new System.Drawing.Point(5, 66);
            this.dgvLayDL.Name = "dgvLayDL";
            this.dgvLayDL.RowHeadersVisible = false;
            this.dgvLayDL.Size = new System.Drawing.Size(1365, 228);
            this.dgvLayDL.TabIndex = 1;
            // 
            // ttNhapKho_ID
            // 
            this.ttNhapKho_ID.DataPropertyName = "ttNhapKho_ID";
            this.ttNhapKho_ID.HeaderText = "id";
            this.ttNhapKho_ID.Name = "ttNhapKho_ID";
            this.ttNhapKho_ID.Width = 50;
            // 
            // tongCD
            // 
            this.tongCD.DataPropertyName = "tongCD";
            this.tongCD.HeaderText = "Tổng CD";
            this.tongCD.Name = "tongCD";
            this.tongCD.ReadOnly = true;
            // 
            // soCuon
            // 
            this.soCuon.DataPropertyName = "soCuon";
            this.soCuon.HeaderText = "Số cuộn";
            this.soCuon.Name = "soCuon";
            this.soCuon.ReadOnly = true;
            // 
            // soDau
            // 
            this.soDau.DataPropertyName = "soDau";
            this.soDau.HeaderText = "Số đầu";
            this.soDau.Name = "soDau";
            this.soDau.ReadOnly = true;
            // 
            // soCuoi
            // 
            this.soCuoi.DataPropertyName = "soCuoi";
            this.soCuoi.HeaderText = "Số cuối";
            this.soCuoi.Name = "soCuoi";
            this.soCuoi.ReadOnly = true;
            // 
            // ghiChu
            // 
            this.ghiChu.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.ghiChu.DataPropertyName = "ghiChu";
            this.ghiChu.HeaderText = "Ghi chú";
            this.ghiChu.Name = "ghiChu";
            this.ghiChu.ReadOnly = true;
            // 
            // soCuon_user
            // 
            this.soCuon_user.DataPropertyName = "soCuon_user";
            this.soCuon_user.HeaderText = "Số cuộn lấy";
            this.soCuon_user.Name = "soCuon_user";
            this.soCuon_user.Width = 130;
            // 
            // soDau_user
            // 
            this.soDau_user.DataPropertyName = "soDau_user";
            this.soDau_user.HeaderText = "Số đầu lấy";
            this.soDau_user.Name = "soDau_user";
            this.soDau_user.Width = 130;
            // 
            // socuoi_user
            // 
            this.socuoi_user.DataPropertyName = "socuoi_user";
            this.socuoi_user.HeaderText = "Số cuối lấy";
            this.socuoi_user.Name = "socuoi_user";
            this.socuoi_user.Width = 130;
            // 
            // ghiChu_user
            // 
            this.ghiChu_user.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.ghiChu_user.DataPropertyName = "ghiChu_user";
            this.ghiChu_user.HeaderText = "Ghi chú";
            this.ghiChu_user.Name = "ghiChu_user";
            // 
            // getAll
            // 
            this.getAll.HeaderText = "Lấy tất";
            this.getAll.Name = "getAll";
            this.getAll.Width = 60;
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Controls.Add(this.btnXoa);
            this.flowLayoutPanel1.Controls.Add(this.btnSua);
            this.flowLayoutPanel1.Controls.Add(this.btnLuuXuatKho);
            this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.flowLayoutPanel1.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(5, 294);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Padding = new System.Windows.Forms.Padding(0, 10, 0, 0);
            this.flowLayoutPanel1.Size = new System.Drawing.Size(1365, 65);
            this.flowLayoutPanel1.TabIndex = 2;
            // 
            // btnXoa
            // 
            this.btnXoa.Location = new System.Drawing.Point(1258, 13);
            this.btnXoa.Name = "btnXoa";
            this.btnXoa.Size = new System.Drawing.Size(104, 44);
            this.btnXoa.TabIndex = 0;
            this.btnXoa.Text = "Xoá";
            this.btnXoa.UseVisualStyleBackColor = true;
            // 
            // btnSua
            // 
            this.btnSua.Location = new System.Drawing.Point(1148, 13);
            this.btnSua.Name = "btnSua";
            this.btnSua.Size = new System.Drawing.Size(104, 44);
            this.btnSua.TabIndex = 2;
            this.btnSua.Text = "Sửa";
            this.btnSua.UseVisualStyleBackColor = true;
            // 
            // btnLuuXuatKho
            // 
            this.btnLuuXuatKho.Location = new System.Drawing.Point(1038, 13);
            this.btnLuuXuatKho.Name = "btnLuuXuatKho";
            this.btnLuuXuatKho.Size = new System.Drawing.Size(104, 44);
            this.btnLuuXuatKho.TabIndex = 1;
            this.btnLuuXuatKho.Text = "Lưu";
            this.btnLuuXuatKho.UseVisualStyleBackColor = true;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 10;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 52F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 128F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 92F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 179F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 93F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 100F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 200F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 130F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 320F));
            this.tableLayoutPanel1.Controls.Add(this.label2, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.dtNgayXuatKho, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.label3, 2, 0);
            this.tableLayoutPanel1.Controls.Add(this.cbxTimLOT, 5, 0);
            this.tableLayoutPanel1.Controls.Add(this.label4, 4, 0);
            this.tableLayoutPanel1.Controls.Add(this.tbNguoiLam, 3, 0);
            this.tableLayoutPanel1.Controls.Add(this.label5, 6, 0);
            this.tableLayoutPanel1.Controls.Add(this.tbLot, 7, 0);
            this.tableLayoutPanel1.Controls.Add(this.tbTenSP, 9, 0);
            this.tableLayoutPanel1.Controls.Add(this.label6, 8, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.tableLayoutPanel1.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tableLayoutPanel1.Location = new System.Drawing.Point(5, 21);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(1365, 45);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(3, 13);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(46, 18);
            this.label2.TabIndex = 0;
            this.label2.Text = "Ngày";
            // 
            // dtNgayXuatKho
            // 
            this.dtNgayXuatKho.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.dtNgayXuatKho.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtNgayXuatKho.Location = new System.Drawing.Point(55, 9);
            this.dtNgayXuatKho.Name = "dtNgayXuatKho";
            this.dtNgayXuatKho.Size = new System.Drawing.Size(122, 26);
            this.dtNgayXuatKho.TabIndex = 1;
            // 
            // label3
            // 
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(183, 13);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(86, 18);
            this.label3.TabIndex = 0;
            this.label3.Text = "Người làm";
            this.label3.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // cbxTimLOT
            // 
            this.cbxTimLOT.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.cbxTimLOT.FormattingEnabled = true;
            this.cbxTimLOT.Location = new System.Drawing.Point(547, 9);
            this.cbxTimLOT.Name = "cbxTimLOT";
            this.cbxTimLOT.Size = new System.Drawing.Size(65, 26);
            this.cbxTimLOT.TabIndex = 2;
            // 
            // label4
            // 
            this.label4.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(454, 13);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(87, 18);
            this.label4.TabIndex = 0;
            this.label4.Text = "Tìm LOT";
            this.label4.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // tbNguoiLam
            // 
            this.tbNguoiLam.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.tbNguoiLam.Location = new System.Drawing.Point(275, 9);
            this.tbNguoiLam.Name = "tbNguoiLam";
            this.tbNguoiLam.Size = new System.Drawing.Size(173, 26);
            this.tbNguoiLam.TabIndex = 3;
            // 
            // label5
            // 
            this.label5.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(618, 13);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(94, 18);
            this.label5.TabIndex = 0;
            this.label5.Text = "LOT";
            this.label5.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // tbLot
            // 
            this.tbLot.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.tbLot.Enabled = false;
            this.tbLot.Location = new System.Drawing.Point(718, 9);
            this.tbLot.Name = "tbLot";
            this.tbLot.Size = new System.Drawing.Size(194, 26);
            this.tbLot.TabIndex = 3;
            // 
            // tbTenSP
            // 
            this.tbTenSP.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.tbTenSP.Enabled = false;
            this.tbTenSP.Location = new System.Drawing.Point(1048, 9);
            this.tbTenSP.Name = "tbTenSP";
            this.tbTenSP.Size = new System.Drawing.Size(314, 26);
            this.tbTenSP.TabIndex = 3;
            // 
            // label6
            // 
            this.label6.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(918, 13);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(124, 18);
            this.label6.TabIndex = 0;
            this.label6.Text = "Tên sản phẩm";
            this.label6.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.groupBox2);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(0, 417);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(1375, 329);
            this.panel2.TabIndex = 3;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.dgvLayDL_preview);
            this.groupBox2.Controls.Add(this.tableLayoutPanel2);
            this.groupBox2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox2.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.groupBox2.Location = new System.Drawing.Point(0, 0);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(1375, 329);
            this.groupBox2.TabIndex = 0;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Tổng hợp xuất kho";
            // 
            // dgvLayDL_preview
            // 
            this.dgvLayDL_preview.AllowUserToAddRows = false;
            this.dgvLayDL_preview.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvLayDL_preview.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.id_preview,
            this.tongCD_preview,
            this.ten_preview,
            this.lot_preview,
            this.soCuon_preview,
            this.soDau_preview,
            this.soCuoi_preview,
            this.ghiChu_preview,
            this.SoCuon_user_preview,
            this.soDau_user_preview,
            this.soCuoi_user_preview,
            this.ghiChu_user_preview});
            this.dgvLayDL_preview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvLayDL_preview.Location = new System.Drawing.Point(3, 19);
            this.dgvLayDL_preview.Name = "dgvLayDL_preview";
            this.dgvLayDL_preview.RowHeadersVisible = false;
            this.dgvLayDL_preview.Size = new System.Drawing.Size(1369, 239);
            this.dgvLayDL_preview.TabIndex = 2;
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 2;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 5.697589F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 94.30241F));
            this.tableLayoutPanel2.Controls.Add(this.label18, 0, 1);
            this.tableLayoutPanel2.Controls.Add(this.label17, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.tbTimKiem, 1, 0);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(3, 258);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 2;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(1369, 68);
            this.tableLayoutPanel2.TabIndex = 1;
            // 
            // label18
            // 
            this.label18.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label18.AutoSize = true;
            this.tableLayoutPanel2.SetColumnSpan(this.label18, 2);
            this.label18.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label18.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
            this.label18.Location = new System.Drawing.Point(3, 43);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(1363, 16);
            this.label18.TabIndex = 4;
            this.label18.Text = "Tìm kiếm theo: Tên sản phẩm; Ngày ; số biên bản; LOT";
            // 
            // label17
            // 
            this.label17.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label17.AutoSize = true;
            this.label17.Location = new System.Drawing.Point(3, 9);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(71, 16);
            this.label17.TabIndex = 3;
            this.label17.Text = "Tìm kiếm";
            // 
            // tbTimKiem
            // 
            this.tbTimKiem.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.tbTimKiem.Location = new System.Drawing.Point(80, 5);
            this.tbTimKiem.Name = "tbTimKiem";
            this.tbTimKiem.Size = new System.Drawing.Size(427, 23);
            this.tbTimKiem.TabIndex = 2;
            // 
            // id_preview
            // 
            this.id_preview.DataPropertyName = "id_preview";
            this.id_preview.HeaderText = "id";
            this.id_preview.Name = "id_preview";
            this.id_preview.ReadOnly = true;
            this.id_preview.Width = 50;
            // 
            // tongCD_preview
            // 
            this.tongCD_preview.DataPropertyName = "tongCD_preview";
            this.tongCD_preview.HeaderText = "Tổng CD";
            this.tongCD_preview.Name = "tongCD_preview";
            // 
            // ten_preview
            // 
            this.ten_preview.DataPropertyName = "ten_preview";
            this.ten_preview.HeaderText = "Tên sản phẩm";
            this.ten_preview.Name = "ten_preview";
            this.ten_preview.ReadOnly = true;
            // 
            // lot_preview
            // 
            this.lot_preview.DataPropertyName = "lot_preview";
            this.lot_preview.HeaderText = "LOT";
            this.lot_preview.Name = "lot_preview";
            this.lot_preview.ReadOnly = true;
            // 
            // soCuon_preview
            // 
            this.soCuon_preview.DataPropertyName = "soCuon_preview";
            this.soCuon_preview.HeaderText = "Số cuộn";
            this.soCuon_preview.Name = "soCuon_preview";
            this.soCuon_preview.ReadOnly = true;
            // 
            // soDau_preview
            // 
            this.soDau_preview.DataPropertyName = "soDau_preview";
            this.soDau_preview.HeaderText = "Số đầu";
            this.soDau_preview.Name = "soDau_preview";
            this.soDau_preview.ReadOnly = true;
            // 
            // soCuoi_preview
            // 
            this.soCuoi_preview.DataPropertyName = "soCuoi_preview";
            this.soCuoi_preview.HeaderText = "Số cuối";
            this.soCuoi_preview.Name = "soCuoi_preview";
            this.soCuoi_preview.ReadOnly = true;
            // 
            // ghiChu_preview
            // 
            this.ghiChu_preview.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.ghiChu_preview.DataPropertyName = "ghiChu_preview";
            this.ghiChu_preview.HeaderText = "Ghi chú";
            this.ghiChu_preview.Name = "ghiChu_preview";
            this.ghiChu_preview.ReadOnly = true;
            // 
            // SoCuon_user_preview
            // 
            this.SoCuon_user_preview.DataPropertyName = "SoCuon_user_preview";
            this.SoCuon_user_preview.HeaderText = "Số cuộn lấy";
            this.SoCuon_user_preview.Name = "SoCuon_user_preview";
            this.SoCuon_user_preview.ReadOnly = true;
            this.SoCuon_user_preview.Width = 130;
            // 
            // soDau_user_preview
            // 
            this.soDau_user_preview.DataPropertyName = "soDau_user_preview";
            this.soDau_user_preview.HeaderText = "Số đầu lấy";
            this.soDau_user_preview.Name = "soDau_user_preview";
            this.soDau_user_preview.ReadOnly = true;
            this.soDau_user_preview.Width = 130;
            // 
            // soCuoi_user_preview
            // 
            this.soCuoi_user_preview.DataPropertyName = "soCuoi_user_preview";
            this.soCuoi_user_preview.HeaderText = "Số cuối lấy";
            this.soCuoi_user_preview.Name = "soCuoi_user_preview";
            this.soCuoi_user_preview.ReadOnly = true;
            this.soCuoi_user_preview.Width = 130;
            // 
            // ghiChu_user_preview
            // 
            this.ghiChu_user_preview.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.ghiChu_user_preview.DataPropertyName = "ghiChu_user_preview";
            this.ghiChu_user_preview.HeaderText = "Ghi chú";
            this.ghiChu_user_preview.Name = "ghiChu_user_preview";
            this.ghiChu_user_preview.ReadOnly = true;
            // 
            // UC_XuatKho
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.label1);
            this.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "UC_XuatKho";
            this.Size = new System.Drawing.Size(1375, 746);
            this.panel1.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvLayDL)).EndInit();
            this.flowLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvLayDL_preview)).EndInit();
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.DateTimePicker dtNgayXuatKho;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox cbxTimLOT;
        private System.Windows.Forms.DataGridView dgvLayDL;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox tbNguoiLam;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox tbLot;
        private System.Windows.Forms.TextBox tbTenSP;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.Button btnXoa;
        private System.Windows.Forms.Button btnLuuXuatKho;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button btnSua;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.TextBox tbTimKiem;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.Label label18;
        private System.Windows.Forms.DataGridView dgvLayDL_preview;
        private System.Windows.Forms.DataGridViewTextBoxColumn ttNhapKho_ID;
        private System.Windows.Forms.DataGridViewTextBoxColumn tongCD;
        private System.Windows.Forms.DataGridViewTextBoxColumn soCuon;
        private System.Windows.Forms.DataGridViewTextBoxColumn soDau;
        private System.Windows.Forms.DataGridViewTextBoxColumn soCuoi;
        private System.Windows.Forms.DataGridViewTextBoxColumn ghiChu;
        private System.Windows.Forms.DataGridViewTextBoxColumn soCuon_user;
        private System.Windows.Forms.DataGridViewTextBoxColumn soDau_user;
        private System.Windows.Forms.DataGridViewTextBoxColumn socuoi_user;
        private System.Windows.Forms.DataGridViewTextBoxColumn ghiChu_user;
        private System.Windows.Forms.DataGridViewCheckBoxColumn getAll;
        private System.Windows.Forms.DataGridViewTextBoxColumn id_preview;
        private System.Windows.Forms.DataGridViewTextBoxColumn tongCD_preview;
        private System.Windows.Forms.DataGridViewTextBoxColumn ten_preview;
        private System.Windows.Forms.DataGridViewTextBoxColumn lot_preview;
        private System.Windows.Forms.DataGridViewTextBoxColumn soCuon_preview;
        private System.Windows.Forms.DataGridViewTextBoxColumn soDau_preview;
        private System.Windows.Forms.DataGridViewTextBoxColumn soCuoi_preview;
        private System.Windows.Forms.DataGridViewTextBoxColumn ghiChu_preview;
        private System.Windows.Forms.DataGridViewTextBoxColumn SoCuon_user_preview;
        private System.Windows.Forms.DataGridViewTextBoxColumn soDau_user_preview;
        private System.Windows.Forms.DataGridViewTextBoxColumn soCuoi_user_preview;
        private System.Windows.Forms.DataGridViewTextBoxColumn ghiChu_user_preview;
    }
}
