namespace DG_TonKhoBTP_v02.UI.NghiepVuKhac.Kho.NhapKho
{
    partial class UC_NhapKhoNVL
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            this.panel1 = new System.Windows.Forms.Panel();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.label2 = new System.Windows.Forms.Label();
            this.tbxTimQr = new System.Windows.Forms.TextBox();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.btnLuu = new System.Windows.Forms.Button();
            this.btnSua = new System.Windows.Forms.Button();
            this.btnTaoQr = new System.Windows.Forms.Button();
            this.panel2 = new System.Windows.Forms.Panel();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.dgvDsNhapNVL = new System.Windows.Forms.DataGridView();
            this.stt = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.congDoan = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.ten = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.khoiLuong = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.chieuDaiNhap = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.qr = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ghiChu = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colXoa = new System.Windows.Forms.DataGridViewButtonColumn();
            this.panel1.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvDsNhapNVL)).BeginInit();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.tableLayoutPanel1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel1.Location = new System.Drawing.Point(0, 558);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1106, 60);
            this.panel1.TabIndex = 1;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 3;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 13.51792F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 86.48209F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 500F));
            this.tableLayoutPanel1.Controls.Add(this.label2, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.tbxTimQr, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.flowLayoutPanel1, 2, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(1106, 60);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // label2
            // 
            this.label2.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(3, 21);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(54, 18);
            this.label2.TabIndex = 0;
            this.label2.Text = "Tìm Qr";
            // 
            // tbxTimQr
            // 
            this.tbxTimQr.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.tbxTimQr.Location = new System.Drawing.Point(84, 17);
            this.tbxTimQr.Name = "tbxTimQr";
            this.tbxTimQr.Size = new System.Drawing.Size(445, 26);
            this.tbxTimQr.TabIndex = 1;
            this.tbxTimQr.KeyDown += new System.Windows.Forms.KeyEventHandler(this.tbxTimQr_KeyDown);
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Controls.Add(this.btnLuu);
            this.flowLayoutPanel1.Controls.Add(this.btnSua);
            this.flowLayoutPanel1.Controls.Add(this.btnTaoQr);
            this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel1.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(608, 3);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Padding = new System.Windows.Forms.Padding(3);
            this.flowLayoutPanel1.Size = new System.Drawing.Size(495, 54);
            this.flowLayoutPanel1.TabIndex = 2;
            // 
            // btnLuu
            // 
            this.btnLuu.Location = new System.Drawing.Point(366, 6);
            this.btnLuu.Name = "btnLuu";
            this.btnLuu.Size = new System.Drawing.Size(120, 48);
            this.btnLuu.TabIndex = 0;
            this.btnLuu.Text = "In - Lưu";
            this.btnLuu.UseVisualStyleBackColor = true;
            this.btnLuu.Click += new System.EventHandler(this.btnLuu_Click);
            // 
            // btnSua
            // 
            this.btnSua.Location = new System.Drawing.Point(240, 6);
            this.btnSua.Name = "btnSua";
            this.btnSua.Size = new System.Drawing.Size(120, 48);
            this.btnSua.TabIndex = 0;
            this.btnSua.Text = "Sửa";
            this.btnSua.UseVisualStyleBackColor = true;
            this.btnSua.Visible = false;
            this.btnSua.Click += new System.EventHandler(this.btnSua_Click);
            // 
            // btnTaoQr
            // 
            this.btnTaoQr.Location = new System.Drawing.Point(114, 6);
            this.btnTaoQr.Name = "btnTaoQr";
            this.btnTaoQr.Size = new System.Drawing.Size(120, 48);
            this.btnTaoQr.TabIndex = 0;
            this.btnTaoQr.Text = "Tạo Qr";
            this.btnTaoQr.UseVisualStyleBackColor = true;
            this.btnTaoQr.Click += new System.EventHandler(this.btnTaoQr_Click);
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.groupBox1);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(0, 0);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(1106, 558);
            this.panel2.TabIndex = 2;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.dgvDsNhapNVL);
            this.groupBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox1.Location = new System.Drawing.Point(0, 0);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(10);
            this.groupBox1.Size = new System.Drawing.Size(1106, 558);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Danh sách nhập";
            // 
            // dgvDsNhapNVL
            // 
            this.dgvDsNhapNVL.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvDsNhapNVL.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.stt,
            this.congDoan,
            this.ten,
            this.khoiLuong,
            this.chieuDaiNhap,
            this.qr,
            this.ghiChu,
            this.colXoa});
            this.dgvDsNhapNVL.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvDsNhapNVL.Location = new System.Drawing.Point(10, 29);
            this.dgvDsNhapNVL.Name = "dgvDsNhapNVL";
            this.dgvDsNhapNVL.Size = new System.Drawing.Size(1086, 519);
            this.dgvDsNhapNVL.TabIndex = 0;
            // 
            // stt
            // 
            this.stt.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            this.stt.DataPropertyName = "stt";
            this.stt.Frozen = true;
            this.stt.HeaderText = "STT";
            this.stt.Name = "stt";
            this.stt.ReadOnly = true;
            this.stt.Width = 61;
            // 
            // congDoan
            // 
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.congDoan.DefaultCellStyle = dataGridViewCellStyle1;
            this.congDoan.HeaderText = "Tên công đoạn";
            this.congDoan.Name = "congDoan";
            this.congDoan.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.congDoan.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.congDoan.Width = 250;
            // 
            // ten
            // 
            this.ten.DataPropertyName = "ten";
            this.ten.HeaderText = "Tên";
            this.ten.Name = "ten";
            this.ten.Width = 250;
            // 
            // khoiLuong
            // 
            this.khoiLuong.DataPropertyName = "khoiLuong";
            this.khoiLuong.HeaderText = "Khối lượng";
            this.khoiLuong.Name = "khoiLuong";
            this.khoiLuong.Width = 120;
            // 
            // chieuDaiNhap
            // 
            this.chieuDaiNhap.DataPropertyName = "chieuDaiNhap";
            this.chieuDaiNhap.HeaderText = "Chiều dài nhập";
            this.chieuDaiNhap.Name = "chieuDaiNhap";
            this.chieuDaiNhap.Visible = false;
            // 
            // qr
            // 
            this.qr.DataPropertyName = "qr";
            this.qr.HeaderText = "Qr";
            this.qr.Name = "qr";
            this.qr.Width = 250;
            // 
            // ghiChu
            // 
            this.ghiChu.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.ghiChu.DataPropertyName = "ghiChu";
            this.ghiChu.HeaderText = "Ghi chú";
            this.ghiChu.Name = "ghiChu";
            // 
            // colXoa
            // 
            this.colXoa.DataPropertyName = "colXoa";
            this.colXoa.HeaderText = "";
            this.colXoa.Name = "colXoa";
            this.colXoa.Width = 70;
            // 
            // UC_NhapKhoNVL
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "UC_NhapKhoNVL";
            this.Size = new System.Drawing.Size(1106, 618);
            this.panel1.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.flowLayoutPanel1.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvDsNhapNVL)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.DataGridView dgvDsNhapNVL;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox tbxTimQr;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.Button btnLuu;
        private System.Windows.Forms.Button btnSua;
        private System.Windows.Forms.Button btnTaoQr;
        private System.Windows.Forms.DataGridViewTextBoxColumn stt;
        private System.Windows.Forms.DataGridViewComboBoxColumn congDoan;
        private System.Windows.Forms.DataGridViewTextBoxColumn ten;
        private System.Windows.Forms.DataGridViewTextBoxColumn khoiLuong;
        private System.Windows.Forms.DataGridViewTextBoxColumn chieuDaiNhap;
        private System.Windows.Forms.DataGridViewTextBoxColumn qr;
        private System.Windows.Forms.DataGridViewTextBoxColumn ghiChu;
        private System.Windows.Forms.DataGridViewButtonColumn colXoa;
    }
}
