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
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
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
            this.dataGridView2 = new System.Windows.Forms.DataGridView();
            this.panel1.SuspendLayout();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvLayDL)).BeginInit();
            this.flowLayoutPanel1.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView2)).BeginInit();
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
            this.panel1.Size = new System.Drawing.Size(1375, 455);
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
            this.groupBox1.Size = new System.Drawing.Size(1375, 455);
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
            this.dgvLayDL.Size = new System.Drawing.Size(1365, 319);
            this.dgvLayDL.TabIndex = 1;
            // 
            // ttNhapKho_ID
            // 
            this.ttNhapKho_ID.HeaderText = "id";
            this.ttNhapKho_ID.Name = "ttNhapKho_ID";
            this.ttNhapKho_ID.Width = 50;
            // 
            // tongCD
            // 
            this.tongCD.HeaderText = "Tổng CD";
            this.tongCD.Name = "tongCD";
            // 
            // soCuon
            // 
            this.soCuon.HeaderText = "Số cuộn";
            this.soCuon.Name = "soCuon";
            // 
            // soDau
            // 
            this.soDau.HeaderText = "Số đầu";
            this.soDau.Name = "soDau";
            // 
            // soCuoi
            // 
            this.soCuoi.HeaderText = "Số cuối";
            this.soCuoi.Name = "soCuoi";
            // 
            // ghiChu
            // 
            this.ghiChu.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.ghiChu.HeaderText = "Ghi chú";
            this.ghiChu.Name = "ghiChu";
            // 
            // soCuon_user
            // 
            this.soCuon_user.HeaderText = "Số cuộn lấy";
            this.soCuon_user.Name = "soCuon_user";
            this.soCuon_user.Width = 130;
            // 
            // soDau_user
            // 
            this.soDau_user.HeaderText = "Số đầu lấy";
            this.soDau_user.Name = "soDau_user";
            this.soDau_user.Width = 130;
            // 
            // socuoi_user
            // 
            this.socuoi_user.HeaderText = "Số cuối lấy";
            this.socuoi_user.Name = "socuoi_user";
            this.socuoi_user.Width = 130;
            // 
            // ghiChu_user
            // 
            this.ghiChu_user.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
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
            this.flowLayoutPanel1.Controls.Add(this.button1);
            this.flowLayoutPanel1.Controls.Add(this.button2);
            this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.flowLayoutPanel1.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(5, 385);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Padding = new System.Windows.Forms.Padding(0, 10, 0, 0);
            this.flowLayoutPanel1.Size = new System.Drawing.Size(1365, 65);
            this.flowLayoutPanel1.TabIndex = 2;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(1258, 13);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(104, 44);
            this.button1.TabIndex = 0;
            this.button1.Text = "button1";
            this.button1.UseVisualStyleBackColor = true;
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(1148, 13);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(104, 44);
            this.button2.TabIndex = 1;
            this.button2.Text = "button2";
            this.button2.UseVisualStyleBackColor = true;
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
            this.cbxTimLOT.Location = new System.Drawing.Point(547, 12);
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
            this.tbLot.Location = new System.Drawing.Point(718, 9);
            this.tbLot.Name = "tbLot";
            this.tbLot.Size = new System.Drawing.Size(194, 26);
            this.tbLot.TabIndex = 3;
            // 
            // tbTenSP
            // 
            this.tbTenSP.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
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
            this.panel2.Location = new System.Drawing.Point(0, 508);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(1375, 238);
            this.panel2.TabIndex = 3;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.dataGridView2);
            this.groupBox2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox2.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.groupBox2.Location = new System.Drawing.Point(0, 0);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(1375, 238);
            this.groupBox2.TabIndex = 0;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Tổng hợp xuất kho";
            // 
            // dataGridView2
            // 
            this.dataGridView2.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridView2.Location = new System.Drawing.Point(3, 19);
            this.dataGridView2.Name = "dataGridView2";
            this.dataGridView2.Size = new System.Drawing.Size(1369, 216);
            this.dataGridView2.TabIndex = 0;
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
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView2)).EndInit();
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
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.DataGridView dataGridView2;
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
    }
}
