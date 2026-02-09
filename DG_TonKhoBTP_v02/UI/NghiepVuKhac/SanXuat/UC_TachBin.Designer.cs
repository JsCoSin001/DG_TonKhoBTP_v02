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
            this.panel3 = new System.Windows.Forms.Panel();
            this.btnInTem = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.tbxLot = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.tbNguoiTach = new System.Windows.Forms.TextBox();
            this.panel2 = new System.Windows.Forms.Panel();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.dgvDsTach = new System.Windows.Forms.DataGridView();
            this.dsMaSP_ID_c = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.LOT = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.bin = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ChieuDaiSau = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.KhoiLuongSau = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.qc_c = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.panel1.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nbrSLTach)).BeginInit();
            this.panel3.SuspendLayout();
            this.panel2.SuspendLayout();
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
            this.panel1.Size = new System.Drawing.Size(617, 602);
            this.panel1.TabIndex = 0;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.tableLayoutPanel1);
            this.groupBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox1.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.groupBox1.Location = new System.Drawing.Point(0, 0);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(3, 7, 3, 3);
            this.groupBox1.Size = new System.Drawing.Size(617, 602);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Bảng cấu hình";
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 4;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 102F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 93F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Controls.Add(this.cbxTimKiem, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.label1, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.label2, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.nbrSLTach, 1, 2);
            this.tableLayoutPanel1.Controls.Add(this.panel3, 2, 3);
            this.tableLayoutPanel1.Controls.Add(this.label5, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.tbxLot, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.label6, 2, 2);
            this.tableLayoutPanel1.Controls.Add(this.tbNguoiTach, 3, 2);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(3, 23);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 4;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 70F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(611, 172);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // cbxTimKiem
            // 
            this.cbxTimKiem.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel1.SetColumnSpan(this.cbxTimKiem, 3);
            this.cbxTimKiem.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cbxTimKiem.FormattingEnabled = true;
            this.cbxTimKiem.Location = new System.Drawing.Point(105, 3);
            this.cbxTimKiem.Name = "cbxTimKiem";
            this.cbxTimKiem.Size = new System.Drawing.Size(503, 26);
            this.cbxTimKiem.TabIndex = 1;
            this.cbxTimKiem.TextUpdate += new System.EventHandler(this.cbxTimKiem_TextUpdate);
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 8);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(96, 16);
            this.label1.TabIndex = 0;
            this.label1.Text = "Tìm kiếm";
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(3, 72);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(96, 16);
            this.label2.TabIndex = 2;
            this.label2.Text = "Số lượng tách";
            // 
            // nbrSLTach
            // 
            this.nbrSLTach.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.nbrSLTach.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.nbrSLTach.Location = new System.Drawing.Point(105, 67);
            this.nbrSLTach.Name = "nbrSLTach";
            this.nbrSLTach.Size = new System.Drawing.Size(202, 26);
            this.nbrSLTach.TabIndex = 3;
            // 
            // panel3
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.panel3, 2);
            this.panel3.Controls.Add(this.btnInTem);
            this.panel3.Controls.Add(this.button1);
            this.panel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel3.Location = new System.Drawing.Point(313, 99);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(295, 70);
            this.panel3.TabIndex = 6;
            // 
            // btnInTem
            // 
            this.btnInTem.BackColor = System.Drawing.Color.Crimson;
            this.btnInTem.Enabled = false;
            this.btnInTem.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnInTem.ForeColor = System.Drawing.SystemColors.Control;
            this.btnInTem.Location = new System.Drawing.Point(155, 12);
            this.btnInTem.Name = "btnInTem";
            this.btnInTem.Size = new System.Drawing.Size(126, 46);
            this.btnInTem.TabIndex = 1;
            this.btnInTem.Text = "IN TEM";
            this.btnInTem.UseVisualStyleBackColor = false;
            this.btnInTem.Click += new System.EventHandler(this.btnInTem_Click);
            // 
            // button1
            // 
            this.button1.BackColor = System.Drawing.Color.DarkGreen;
            this.button1.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button1.ForeColor = System.Drawing.SystemColors.Control;
            this.button1.Location = new System.Drawing.Point(20, 12);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(119, 46);
            this.button1.TabIndex = 0;
            this.button1.Text = "TÁCH";
            this.button1.UseVisualStyleBackColor = false;
            this.button1.Click += new System.EventHandler(this.AutoTaoMaBin);
            // 
            // label5
            // 
            this.label5.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(3, 40);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(96, 16);
            this.label5.TabIndex = 0;
            this.label5.Text = "LOT";
            // 
            // tbxLot
            // 
            this.tbxLot.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel1.SetColumnSpan(this.tbxLot, 3);
            this.tbxLot.Enabled = false;
            this.tbxLot.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tbxLot.Location = new System.Drawing.Point(105, 35);
            this.tbxLot.Name = "tbxLot";
            this.tbxLot.Size = new System.Drawing.Size(503, 26);
            this.tbxLot.TabIndex = 7;
            // 
            // label6
            // 
            this.label6.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(313, 72);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(87, 16);
            this.label6.TabIndex = 2;
            this.label6.Text = "Người tách";
            // 
            // tbNguoiTach
            // 
            this.tbNguoiTach.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.tbNguoiTach.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tbNguoiTach.Location = new System.Drawing.Point(406, 67);
            this.tbNguoiTach.Name = "tbNguoiTach";
            this.tbNguoiTach.Size = new System.Drawing.Size(202, 26);
            this.tbNguoiTach.TabIndex = 7;
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.groupBox2);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(617, 0);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(358, 602);
            this.panel2.TabIndex = 1;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.dgvDsTach);
            this.groupBox2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox2.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.groupBox2.Location = new System.Drawing.Point(0, 0);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(358, 602);
            this.groupBox2.TabIndex = 0;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Kết quả tách";
            // 
            // dgvDsTach
            // 
            this.dgvDsTach.AllowUserToAddRows = false;
            this.dgvDsTach.AllowUserToDeleteRows = false;
            this.dgvDsTach.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
            this.dgvDsTach.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
            this.dgvDsTach.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvDsTach.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.dsMaSP_ID_c,
            this.LOT,
            this.bin,
            this.ChieuDaiSau,
            this.KhoiLuongSau,
            this.qc_c});
            this.dgvDsTach.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvDsTach.Location = new System.Drawing.Point(3, 19);
            this.dgvDsTach.Name = "dgvDsTach";
            this.dgvDsTach.RowTemplate.Height = 30;
            this.dgvDsTach.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvDsTach.Size = new System.Drawing.Size(352, 580);
            this.dgvDsTach.TabIndex = 0;
            // 
            // dsMaSP_ID_c
            // 
            this.dsMaSP_ID_c.DataPropertyName = "DanhSachSP_ID";
            this.dsMaSP_ID_c.HeaderText = "ID";
            this.dsMaSP_ID_c.Name = "dsMaSP_ID_c";
            this.dsMaSP_ID_c.Width = 44;
            // 
            // LOT
            // 
            this.LOT.DataPropertyName = "Lot";
            this.LOT.HeaderText = "Lot";
            this.LOT.Name = "LOT";
            this.LOT.Width = 49;
            // 
            // bin
            // 
            this.bin.DataPropertyName = "Bin";
            this.bin.HeaderText = "Bin";
            this.bin.Name = "bin";
            this.bin.Width = 49;
            // 
            // ChieuDaiSau
            // 
            this.ChieuDaiSau.DataPropertyName = "ChieuDaiSau";
            this.ChieuDaiSau.HeaderText = "Chiều dài";
            this.ChieuDaiSau.Name = "ChieuDaiSau";
            this.ChieuDaiSau.Width = 85;
            // 
            // KhoiLuongSau
            // 
            this.KhoiLuongSau.DataPropertyName = "KhoiLuongSau";
            this.KhoiLuongSau.HeaderText = "Khối lượng";
            this.KhoiLuongSau.Name = "KhoiLuongSau";
            this.KhoiLuongSau.Width = 92;
            // 
            // qc_c
            // 
            this.qc_c.DataPropertyName = "qc";
            this.qc_c.HeaderText = "QC";
            this.qc_c.Name = "qc_c";
            this.qc_c.Width = 49;
            // 
            // UC_TachBin
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.Name = "UC_TachBin";
            this.Size = new System.Drawing.Size(975, 602);
            this.panel1.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nbrSLTach)).EndInit();
            this.panel3.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
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
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.Button btnInTem;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.DataGridView dgvDsTach;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox tbxLot;
        private System.Windows.Forms.DataGridViewTextBoxColumn dsMaSP_ID_c;
        private System.Windows.Forms.DataGridViewTextBoxColumn LOT;
        private System.Windows.Forms.DataGridViewTextBoxColumn bin;
        private System.Windows.Forms.DataGridViewTextBoxColumn ChieuDaiSau;
        private System.Windows.Forms.DataGridViewTextBoxColumn KhoiLuongSau;
        private System.Windows.Forms.DataGridViewTextBoxColumn qc_c;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox tbNguoiTach;
    }
}
