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
                _cbxTimDonHelper?.Dispose();
                _cbxTimTenHelper?.Dispose();
                _cbxNhaCungCapHelper?.Dispose();
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle5 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle6 = new System.Windows.Forms.DataGridViewCellStyle();
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
            this.fl = new System.Windows.Forms.FlowLayoutPanel();
            this.rdoLoai = new System.Windows.Forms.RadioButton();
            this.radioButton2 = new System.Windows.Forms.RadioButton();
            this.cbxTimDon = new System.Windows.Forms.ComboBox();
            this.label6 = new System.Windows.Forms.Label();
            this.cbxTimTen = new System.Windows.Forms.ComboBox();
            this.dtNgayNhapXuat = new System.Windows.Forms.DateTimePicker();
            this.label7 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.cbxKieu = new System.Windows.Forms.ComboBox();
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
            this.ngay = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.thucNhan = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.donGia = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.thanhTien = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.NhaCungCap = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ghiChu = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.xoa = new System.Windows.Forms.DataGridViewButtonColumn();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.tableLayoutPanel3.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.fl.SuspendLayout();
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
            this.txtNguoiGiaoNhan.Location = new System.Drawing.Point(153, 17);
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
            this.tbxnguoiLam.Location = new System.Drawing.Point(453, 17);
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
            this.cbxKhoHang.Location = new System.Drawing.Point(753, 17);
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
            this.cbxNhaCungCap.FormattingEnabled = true;
            this.cbxNhaCungCap.Location = new System.Drawing.Point(1121, 17);
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
            this.tableLayoutPanel1.Controls.Add(this.fl, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.cbxTimDon, 5, 0);
            this.tableLayoutPanel1.Controls.Add(this.label6, 2, 0);
            this.tableLayoutPanel1.Controls.Add(this.cbxTimTen, 3, 0);
            this.tableLayoutPanel1.Controls.Add(this.dtNgayNhapXuat, 7, 0);
            this.tableLayoutPanel1.Controls.Add(this.label7, 6, 0);
            this.tableLayoutPanel1.Controls.Add(this.label4, 4, 0);
            this.tableLayoutPanel1.Controls.Add(this.cbxKieu, 0, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 55F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(1360, 55);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // fl
            // 
            this.fl.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.fl.Controls.Add(this.rdoLoai);
            this.fl.Controls.Add(this.radioButton2);
            this.fl.Enabled = false;
            this.fl.Location = new System.Drawing.Point(123, 13);
            this.fl.Name = "fl";
            this.fl.Size = new System.Drawing.Size(253, 29);
            this.fl.TabIndex = 6;
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
            // cbxTimDon
            // 
            this.cbxTimDon.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.cbxTimDon.FormattingEnabled = true;
            this.cbxTimDon.Location = new System.Drawing.Point(892, 14);
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
            this.cbxTimTen.Location = new System.Drawing.Point(552, 17);
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
            // cbxKieu
            // 
            this.cbxKieu.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.cbxKieu.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbxKieu.FormattingEnabled = true;
            this.cbxKieu.Items.AddRange(new object[] {
            "Tạo mới",
            "Chỉnh sửa"});
            this.cbxKieu.Location = new System.Drawing.Point(3, 17);
            this.cbxKieu.Name = "cbxKieu";
            this.cbxKieu.Size = new System.Drawing.Size(114, 27);
            this.cbxKieu.TabIndex = 9;
            this.cbxKieu.SelectedIndexChanged += new System.EventHandler(this.cbxKieu_SelectedIndexChanged);
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
            dataGridViewCellStyle4.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle4.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle4.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle4.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle4.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle4.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle4.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvChiTietDon.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle4;
            this.dgvChiTietDon.ColumnHeadersHeight = 35;
            this.dgvChiTietDon.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.dgvChiTietDon.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.id,
            this.MaDon,
            this.ten,
            this.ma,
            this.donVi,
            this.yeuCau,
            this.ngay,
            this.thucNhan,
            this.donGia,
            this.thanhTien,
            this.NhaCungCap,
            this.ghiChu,
            this.xoa});
            dataGridViewCellStyle5.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle5.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle5.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle5.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle5.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle5.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle5.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvChiTietDon.DefaultCellStyle = dataGridViewCellStyle5;
            this.dgvChiTietDon.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvChiTietDon.Location = new System.Drawing.Point(0, 0);
            this.dgvChiTietDon.Name = "dgvChiTietDon";
            dataGridViewCellStyle6.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle6.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle6.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle6.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle6.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle6.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle6.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvChiTietDon.RowHeadersDefaultCellStyle = dataGridViewCellStyle6;
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
            this.donVi.Width = 70;
            // 
            // yeuCau
            // 
            this.yeuCau.DataPropertyName = "yeuCau";
            this.yeuCau.HeaderText = "Yêu Cầu";
            this.yeuCau.Name = "yeuCau";
            this.yeuCau.ReadOnly = true;
            // 
            // ngay
            // 
            this.ngay.DataPropertyName = "ngay";
            this.ngay.HeaderText = "Ngày";
            this.ngay.Name = "ngay";
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
            // 
            // thanhTien
            // 
            this.thanhTien.DataPropertyName = "thanhTien";
            this.thanhTien.HeaderText = "Thành tiền";
            this.thanhTien.Name = "thanhTien";
            this.thanhTien.Visible = false;
            // 
            // NhaCungCap
            // 
            this.NhaCungCap.DataPropertyName = "NhaCungCap";
            this.NhaCungCap.HeaderText = "Nhà cung cấp";
            this.NhaCungCap.Name = "NhaCungCap";
            this.NhaCungCap.Visible = false;
            this.NhaCungCap.Width = 250;
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
            this.fl.ResumeLayout(false);
            this.fl.PerformLayout();
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
        private System.Windows.Forms.FlowLayoutPanel fl;
        private System.Windows.Forms.RadioButton rdoLoai;
        private System.Windows.Forms.RadioButton radioButton2;
        private System.Windows.Forms.ComboBox tbxnguoiLam;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel3;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.DateTimePicker dtNgayNhapXuat;
        private System.Windows.Forms.ComboBox cbxTimDon;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox cbxKieu;
        private System.Windows.Forms.DataGridViewTextBoxColumn id;
        private System.Windows.Forms.DataGridViewTextBoxColumn MaDon;
        private System.Windows.Forms.DataGridViewTextBoxColumn ten;
        private System.Windows.Forms.DataGridViewTextBoxColumn ma;
        private System.Windows.Forms.DataGridViewTextBoxColumn donVi;
        private System.Windows.Forms.DataGridViewTextBoxColumn yeuCau;
        private System.Windows.Forms.DataGridViewTextBoxColumn ngay;
        private System.Windows.Forms.DataGridViewTextBoxColumn thucNhan;
        private System.Windows.Forms.DataGridViewTextBoxColumn donGia;
        private System.Windows.Forms.DataGridViewTextBoxColumn thanhTien;
        private System.Windows.Forms.DataGridViewTextBoxColumn NhaCungCap;
        private System.Windows.Forms.DataGridViewTextBoxColumn ghiChu;
        private System.Windows.Forms.DataGridViewButtonColumn xoa;
    }
}
