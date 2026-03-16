namespace DG_TonKhoBTP_v02.UI.Actions
{
    partial class UC_TachBin
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
            this.panel1 = new System.Windows.Forms.Panel();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.cbxTimKiem = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.nbrSLTach = new System.Windows.Forms.NumericUpDown();
            this.label6 = new System.Windows.Forms.Label();
            this.tbNguoiTach = new System.Windows.Forms.TextBox();
            this.tbxLot = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.nbrKhoiLuong = new System.Windows.Forms.NumericUpDown();
            this.label7 = new System.Windows.Forms.Label();
            this.nbrChieuDai = new System.Windows.Forms.NumericUpDown();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.lblTen = new System.Windows.Forms.Label();
            this.btnTachBin = new System.Windows.Forms.Button();
            this.btnInTem = new System.Windows.Forms.Button();
            this.panel2 = new System.Windows.Forms.Panel();
            this.panel5 = new System.Windows.Forms.Panel();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.panel4 = new System.Windows.Forms.Panel();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.dgvDsTach = new System.Windows.Forms.DataGridView();
            this.DanhSachSP_ID = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.LOT = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.bin = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ChieuDai = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.KhoiLuong = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.QC = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.panel1.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nbrSLTach)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nbrKhoiLuong)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nbrChieuDai)).BeginInit();
            this.panel2.SuspendLayout();
            this.panel5.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.panel4.SuspendLayout();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvDsTach)).BeginInit();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.groupBox1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Left;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(785, 602);
            this.panel1.TabIndex = 0;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.tableLayoutPanel1);
            this.groupBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox1.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.groupBox1.Location = new System.Drawing.Point(0, 0);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(3, 7, 3, 3);
            this.groupBox1.Size = new System.Drawing.Size(785, 602);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Bảng cấu hình";
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 4;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 119F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 136F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Controls.Add(this.cbxTimKiem, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.label1, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.label2, 0, 3);
            this.tableLayoutPanel1.Controls.Add(this.nbrSLTach, 1, 3);
            this.tableLayoutPanel1.Controls.Add(this.label6, 2, 3);
            this.tableLayoutPanel1.Controls.Add(this.tbNguoiTach, 3, 3);
            this.tableLayoutPanel1.Controls.Add(this.tbxLot, 3, 0);
            this.tableLayoutPanel1.Controls.Add(this.label5, 2, 0);
            this.tableLayoutPanel1.Controls.Add(this.nbrKhoiLuong, 1, 2);
            this.tableLayoutPanel1.Controls.Add(this.label7, 2, 2);
            this.tableLayoutPanel1.Controls.Add(this.nbrChieuDai, 3, 2);
            this.tableLayoutPanel1.Controls.Add(this.label4, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.label3, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.lblTen, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.btnTachBin, 3, 4);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.tableLayoutPanel1.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tableLayoutPanel1.Location = new System.Drawing.Point(3, 27);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 5;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 56F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(779, 252);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // cbxTimKiem
            // 
            this.cbxTimKiem.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.cbxTimKiem.Font = new System.Drawing.Font("Tahoma", 12.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cbxTimKiem.FormattingEnabled = true;
            this.cbxTimKiem.Location = new System.Drawing.Point(122, 5);
            this.cbxTimKiem.Name = "cbxTimKiem";
            this.cbxTimKiem.Size = new System.Drawing.Size(256, 29);
            this.cbxTimKiem.TabIndex = 1;
            this.cbxTimKiem.KeyDown += new System.Windows.Forms.KeyEventHandler(this.cbxTimKiem_KeyDown);
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(3, 10);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(113, 19);
            this.label1.TabIndex = 0;
            this.label1.Text = "Tìm kiếm";
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(3, 130);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(113, 19);
            this.label2.TabIndex = 2;
            this.label2.Text = "Số lượng tách";
            // 
            // nbrSLTach
            // 
            this.nbrSLTach.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.nbrSLTach.Font = new System.Drawing.Font("Tahoma", 12.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.nbrSLTach.Location = new System.Drawing.Point(122, 126);
            this.nbrSLTach.Name = "nbrSLTach";
            this.nbrSLTach.Size = new System.Drawing.Size(256, 28);
            this.nbrSLTach.TabIndex = 3;
            // 
            // label6
            // 
            this.label6.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.Location = new System.Drawing.Point(384, 130);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(130, 19);
            this.label6.TabIndex = 2;
            this.label6.Text = "Người tách";
            this.label6.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // tbNguoiTach
            // 
            this.tbNguoiTach.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.tbNguoiTach.Font = new System.Drawing.Font("Tahoma", 12.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tbNguoiTach.Location = new System.Drawing.Point(520, 126);
            this.tbNguoiTach.Name = "tbNguoiTach";
            this.tbNguoiTach.Size = new System.Drawing.Size(256, 28);
            this.tbNguoiTach.TabIndex = 7;
            // 
            // tbxLot
            // 
            this.tbxLot.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.tbxLot.Enabled = false;
            this.tbxLot.Font = new System.Drawing.Font("Tahoma", 12.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tbxLot.Location = new System.Drawing.Point(520, 6);
            this.tbxLot.Name = "tbxLot";
            this.tbxLot.Size = new System.Drawing.Size(256, 28);
            this.tbxLot.TabIndex = 7;
            // 
            // label5
            // 
            this.label5.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.Location = new System.Drawing.Point(384, 10);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(130, 19);
            this.label5.TabIndex = 0;
            this.label5.Text = "LOT";
            this.label5.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // nbrKhoiLuong
            // 
            this.nbrKhoiLuong.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.nbrKhoiLuong.Enabled = false;
            this.nbrKhoiLuong.Font = new System.Drawing.Font("Tahoma", 12.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.nbrKhoiLuong.Location = new System.Drawing.Point(122, 86);
            this.nbrKhoiLuong.Maximum = new decimal(new int[] {
            -559939585,
            902409669,
            54,
            0});
            this.nbrKhoiLuong.Minimum = new decimal(new int[] {
            -559939585,
            902409669,
            54,
            -2147483648});
            this.nbrKhoiLuong.Name = "nbrKhoiLuong";
            this.nbrKhoiLuong.Size = new System.Drawing.Size(256, 28);
            this.nbrKhoiLuong.TabIndex = 8;
            // 
            // label7
            // 
            this.label7.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label7.AutoSize = true;
            this.label7.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label7.Location = new System.Drawing.Point(384, 90);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(130, 19);
            this.label7.TabIndex = 0;
            this.label7.Text = "Chiều dài";
            this.label7.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // nbrChieuDai
            // 
            this.nbrChieuDai.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.nbrChieuDai.Enabled = false;
            this.nbrChieuDai.Font = new System.Drawing.Font("Tahoma", 12.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.nbrChieuDai.Location = new System.Drawing.Point(520, 86);
            this.nbrChieuDai.Maximum = new decimal(new int[] {
            -559939585,
            902409669,
            54,
            0});
            this.nbrChieuDai.Minimum = new decimal(new int[] {
            -559939585,
            902409669,
            54,
            -2147483648});
            this.nbrChieuDai.Name = "nbrChieuDai";
            this.nbrChieuDai.Size = new System.Drawing.Size(256, 28);
            this.nbrChieuDai.TabIndex = 8;
            // 
            // label4
            // 
            this.label4.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(3, 50);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(113, 19);
            this.label4.TabIndex = 0;
            this.label4.Text = "Tên sản phẩm";
            // 
            // label3
            // 
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(3, 90);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(113, 19);
            this.label3.TabIndex = 0;
            this.label3.Text = "Khối lượng";
            // 
            // lblTen
            // 
            this.lblTen.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblTen.AutoSize = true;
            this.tableLayoutPanel1.SetColumnSpan(this.lblTen, 3);
            this.lblTen.Font = new System.Drawing.Font("Tahoma", 12.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTen.Location = new System.Drawing.Point(122, 49);
            this.lblTen.Name = "lblTen";
            this.lblTen.Size = new System.Drawing.Size(654, 21);
            this.lblTen.TabIndex = 0;
            // 
            // btnTachBin
            // 
            this.btnTachBin.BackColor = System.Drawing.Color.DarkGreen;
            this.btnTachBin.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnTachBin.ForeColor = System.Drawing.SystemColors.Control;
            this.btnTachBin.Location = new System.Drawing.Point(520, 163);
            this.btnTachBin.Name = "btnTachBin";
            this.btnTachBin.Size = new System.Drawing.Size(150, 57);
            this.btnTachBin.TabIndex = 0;
            this.btnTachBin.Text = "TÁCH";
            this.btnTachBin.UseVisualStyleBackColor = false;
            this.btnTachBin.Click += new System.EventHandler(this.AutoTaoMaBin);
            // 
            // btnInTem
            // 
            this.btnInTem.BackColor = System.Drawing.Color.Crimson;
            this.btnInTem.Enabled = false;
            this.btnInTem.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnInTem.ForeColor = System.Drawing.SystemColors.Control;
            this.btnInTem.Location = new System.Drawing.Point(230, 3);
            this.btnInTem.Name = "btnInTem";
            this.btnInTem.Size = new System.Drawing.Size(163, 57);
            this.btnInTem.TabIndex = 1;
            this.btnInTem.Text = "LƯU VÀ IN TEM";
            this.btnInTem.UseVisualStyleBackColor = false;
            this.btnInTem.Click += new System.EventHandler(this.btnInTem_Click);
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.panel5);
            this.panel2.Controls.Add(this.panel4);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(785, 0);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(396, 602);
            this.panel2.TabIndex = 1;
            // 
            // panel5
            // 
            this.panel5.Controls.Add(this.flowLayoutPanel1);
            this.panel5.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel5.Location = new System.Drawing.Point(0, 519);
            this.panel5.Name = "panel5";
            this.panel5.Size = new System.Drawing.Size(396, 83);
            this.panel5.TabIndex = 2;
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Controls.Add(this.btnInTem);
            this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel1.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(396, 83);
            this.flowLayoutPanel1.TabIndex = 2;
            // 
            // panel4
            // 
            this.panel4.Controls.Add(this.groupBox2);
            this.panel4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel4.Location = new System.Drawing.Point(0, 0);
            this.panel4.Name = "panel4";
            this.panel4.Size = new System.Drawing.Size(396, 602);
            this.panel4.TabIndex = 1;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.dgvDsTach);
            this.groupBox2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox2.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.groupBox2.Location = new System.Drawing.Point(0, 0);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(396, 602);
            this.groupBox2.TabIndex = 0;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Kết quả tách";
            // 
            // dgvDsTach
            // 
            this.dgvDsTach.AllowUserToAddRows = false;
            this.dgvDsTach.AllowUserToDeleteRows = false;
            this.dgvDsTach.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
            this.dgvDsTach.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvDsTach.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.DanhSachSP_ID,
            this.LOT,
            this.bin,
            this.ChieuDai,
            this.KhoiLuong,
            this.QC});
            this.dgvDsTach.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvDsTach.Location = new System.Drawing.Point(3, 22);
            this.dgvDsTach.Name = "dgvDsTach";
            this.dgvDsTach.RowHeadersVisible = false;
            this.dgvDsTach.RowTemplate.Height = 30;
            this.dgvDsTach.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvDsTach.Size = new System.Drawing.Size(390, 577);
            this.dgvDsTach.TabIndex = 0;
            // 
            // DanhSachSP_ID
            // 
            this.DanhSachSP_ID.DataPropertyName = "DanhSachSP_ID";
            this.DanhSachSP_ID.HeaderText = "ID";
            this.DanhSachSP_ID.Name = "DanhSachSP_ID";
            this.DanhSachSP_ID.ReadOnly = true;
            this.DanhSachSP_ID.Width = 60;
            // 
            // LOT
            // 
            this.LOT.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.LOT.DataPropertyName = "Lot";
            this.LOT.HeaderText = "Lot";
            this.LOT.Name = "LOT";
            this.LOT.ReadOnly = true;
            // 
            // bin
            // 
            this.bin.DataPropertyName = "Bin";
            this.bin.HeaderText = "Bin";
            this.bin.Name = "bin";
            this.bin.ReadOnly = true;
            this.bin.Width = 70;
            // 
            // ChieuDai
            // 
            this.ChieuDai.DataPropertyName = "ChieuDai";
            this.ChieuDai.HeaderText = "Chiều dài";
            this.ChieuDai.Name = "ChieuDai";
            this.ChieuDai.Width = 120;
            // 
            // KhoiLuong
            // 
            this.KhoiLuong.DataPropertyName = "KhoiLuong";
            this.KhoiLuong.HeaderText = "Khối lượng";
            this.KhoiLuong.Name = "KhoiLuong";
            this.KhoiLuong.Width = 120;
            // 
            // QC
            // 
            this.QC.DataPropertyName = "QC";
            this.QC.HeaderText = "QC";
            this.QC.Name = "QC";
            this.QC.ReadOnly = true;
            this.QC.Width = 120;
            // 
            // UC_TachBin
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.Name = "UC_TachBin";
            this.Size = new System.Drawing.Size(1181, 602);
            this.panel1.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nbrSLTach)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nbrKhoiLuong)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nbrChieuDai)).EndInit();
            this.panel2.ResumeLayout(false);
            this.panel5.ResumeLayout(false);
            this.flowLayoutPanel1.ResumeLayout(false);
            this.panel4.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvDsTach)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.ComboBox cbxTimKiem;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown nbrSLTach;
        private System.Windows.Forms.Button btnInTem;
        private System.Windows.Forms.Button btnTachBin;
        private System.Windows.Forms.DataGridView dgvDsTach;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox tbxLot;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox tbNguoiTach;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.NumericUpDown nbrKhoiLuong;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label lblTen;
        private System.Windows.Forms.Panel panel4;
        private System.Windows.Forms.Panel panel5;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.NumericUpDown nbrChieuDai;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.DataGridViewTextBoxColumn DanhSachSP_ID;
        private System.Windows.Forms.DataGridViewTextBoxColumn LOT;
        private System.Windows.Forms.DataGridViewTextBoxColumn bin;
        private System.Windows.Forms.DataGridViewTextBoxColumn ChieuDai;
        private System.Windows.Forms.DataGridViewTextBoxColumn KhoiLuong;
        private System.Windows.Forms.DataGridViewTextBoxColumn QC;
    }
}
