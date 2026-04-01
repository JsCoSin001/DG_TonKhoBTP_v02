namespace DG_TonKhoBTP_v02.UI.NghiepVuKhac.KeToan
{
    partial class UC_MuaVatTu
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
                _vatTuSearchHelper?.Dispose(); // ← thêm dòng này
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.panel1 = new System.Windows.Forms.Panel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.cbxTimTenVatTu = new System.Windows.Forms.ComboBox();
            this.lblTieuDeTimKiem = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.panel4 = new System.Windows.Forms.Panel();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.rdoTaoMoi = new System.Windows.Forms.RadioButton();
            this.radioButton2 = new System.Windows.Forms.RadioButton();
            this.tbMaDon = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.nguoiDat = new System.Windows.Forms.ComboBox();
            this.panel2 = new System.Windows.Forms.Panel();
            this.dgvDSMua = new System.Windows.Forms.DataGridView();
            this.ID = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ma = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ten = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.donVi = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.soLuong = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.mucDich = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ngayGiao = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.slTon = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colXoa = new System.Windows.Forms.DataGridViewButtonColumn();
            this.panel3 = new System.Windows.Forms.Panel();
            this.btnDatHang = new System.Windows.Forms.Button();
            this.panel1.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.panel4.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.panel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvDSMua)).BeginInit();
            this.panel3.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.lblTitle);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Margin = new System.Windows.Forms.Padding(4);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1140, 33);
            this.panel1.TabIndex = 0;
            // 
            // lblTitle
            // 
            this.lblTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTitle.Font = new System.Drawing.Font("Tahoma", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTitle.Location = new System.Drawing.Point(0, 0);
            this.lblTitle.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(1140, 33);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "ĐỀ NGHỊ MUA VẬT TƯ";
            this.lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 8;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 61F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 190F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 94F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 169F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 139F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 103F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 215F));
            this.tableLayoutPanel1.Controls.Add(this.cbxTimTenVatTu, 5, 0);
            this.tableLayoutPanel1.Controls.Add(this.lblTieuDeTimKiem, 4, 0);
            this.tableLayoutPanel1.Controls.Add(this.label2, 6, 0);
            this.tableLayoutPanel1.Controls.Add(this.label5, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.panel4, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.tbMaDon, 7, 0);
            this.tableLayoutPanel1.Controls.Add(this.label1, 2, 0);
            this.tableLayoutPanel1.Controls.Add(this.nguoiDat, 3, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 33);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.Size = new System.Drawing.Size(1140, 62);
            this.tableLayoutPanel1.TabIndex = 1;
            // 
            // cbxTimTenVatTu
            // 
            this.cbxTimTenVatTu.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.cbxTimTenVatTu.FormattingEnabled = true;
            this.cbxTimTenVatTu.Location = new System.Drawing.Point(656, 20);
            this.cbxTimTenVatTu.Name = "cbxTimTenVatTu";
            this.cbxTimTenVatTu.Size = new System.Drawing.Size(163, 26);
            this.cbxTimTenVatTu.TabIndex = 2;
            // 
            // lblTieuDeTimKiem
            // 
            this.lblTieuDeTimKiem.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblTieuDeTimKiem.AutoSize = true;
            this.lblTieuDeTimKiem.Location = new System.Drawing.Point(517, 22);
            this.lblTieuDeTimKiem.Name = "lblTieuDeTimKiem";
            this.lblTieuDeTimKiem.Size = new System.Drawing.Size(133, 18);
            this.lblTieuDeTimKiem.TabIndex = 0;
            this.lblTieuDeTimKiem.Text = "Tìm tên vật tư";
            this.lblTieuDeTimKiem.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(825, 22);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(97, 18);
            this.label2.TabIndex = 0;
            this.label2.Text = "Mã đơn";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label5
            // 
            this.label5.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(3, 22);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(55, 18);
            this.label5.TabIndex = 0;
            this.label5.Text = "Loại";
            // 
            // panel4
            // 
            this.panel4.Controls.Add(this.flowLayoutPanel1);
            this.panel4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel4.Location = new System.Drawing.Point(64, 3);
            this.panel4.Name = "panel4";
            this.panel4.Size = new System.Drawing.Size(184, 56);
            this.panel4.TabIndex = 3;
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Controls.Add(this.rdoTaoMoi);
            this.flowLayoutPanel1.Controls.Add(this.radioButton2);
            this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Padding = new System.Windows.Forms.Padding(0, 12, 0, 0);
            this.flowLayoutPanel1.Size = new System.Drawing.Size(184, 56);
            this.flowLayoutPanel1.TabIndex = 0;
            // 
            // rdoTaoMoi
            // 
            this.rdoTaoMoi.AutoSize = true;
            this.rdoTaoMoi.Checked = true;
            this.rdoTaoMoi.Location = new System.Drawing.Point(3, 15);
            this.rdoTaoMoi.Name = "rdoTaoMoi";
            this.rdoTaoMoi.Size = new System.Drawing.Size(80, 22);
            this.rdoTaoMoi.TabIndex = 0;
            this.rdoTaoMoi.TabStop = true;
            this.rdoTaoMoi.Text = "Tạo mới";
            this.rdoTaoMoi.UseVisualStyleBackColor = true;
            // 
            // radioButton2
            // 
            this.radioButton2.AutoSize = true;
            this.radioButton2.Location = new System.Drawing.Point(89, 15);
            this.radioButton2.Name = "radioButton2";
            this.radioButton2.Size = new System.Drawing.Size(81, 22);
            this.radioButton2.TabIndex = 1;
            this.radioButton2.Text = "Sửa đơn";
            this.radioButton2.UseVisualStyleBackColor = true;
            // 
            // tbMaDon
            // 
            this.tbMaDon.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.tbMaDon.Location = new System.Drawing.Point(928, 18);
            this.tbMaDon.Name = "tbMaDon";
            this.tbMaDon.Size = new System.Drawing.Size(209, 26);
            this.tbMaDon.TabIndex = 4;
            this.tbMaDon.Text = "PRM26/02-011";
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(254, 22);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(88, 18);
            this.label1.TabIndex = 0;
            this.label1.Text = "Người làm";
            this.label1.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // nguoiDat
            // 
            this.nguoiDat.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.nguoiDat.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.nguoiDat.FormattingEnabled = true;
            this.nguoiDat.Items.AddRange(new object[] {
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
            this.nguoiDat.Location = new System.Drawing.Point(348, 18);
            this.nguoiDat.Name = "nguoiDat";
            this.nguoiDat.Size = new System.Drawing.Size(163, 26);
            this.nguoiDat.TabIndex = 5;
            // 
            // panel2
            // 
            this.panel2.BackColor = System.Drawing.SystemColors.Control;
            this.panel2.Controls.Add(this.dgvDSMua);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(0, 95);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(1140, 448);
            this.panel2.TabIndex = 2;
            // 
            // dgvDSMua
            // 
            this.dgvDSMua.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvDSMua.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.ID,
            this.ma,
            this.ten,
            this.donVi,
            this.soLuong,
            this.mucDich,
            this.ngayGiao,
            this.slTon,
            this.colXoa});
            this.dgvDSMua.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvDSMua.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnKeystroke;
            this.dgvDSMua.Location = new System.Drawing.Point(0, 0);
            this.dgvDSMua.Name = "dgvDSMua";
            this.dgvDSMua.RowHeadersVisible = false;
            this.dgvDSMua.RowTemplate.Height = 25;
            this.dgvDSMua.Size = new System.Drawing.Size(1140, 448);
            this.dgvDSMua.TabIndex = 0;
            this.dgvDSMua.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvDSMua_CellClick);
            // 
            // ID
            // 
            this.ID.Frozen = true;
            this.ID.HeaderText = "ID";
            this.ID.Name = "ID";
            // 
            // ma
            // 
            this.ma.Frozen = true;
            this.ma.HeaderText = "Mã vật tư";
            this.ma.Name = "ma";
            this.ma.Width = 150;
            // 
            // ten
            // 
            this.ten.Frozen = true;
            this.ten.HeaderText = "Tên vật tư";
            this.ten.Name = "ten";
            this.ten.Width = 250;
            // 
            // donVi
            // 
            this.donVi.Frozen = true;
            this.donVi.HeaderText = "Đơn vị";
            this.donVi.Name = "donVi";
            // 
            // soLuong
            // 
            this.soLuong.Frozen = true;
            this.soLuong.HeaderText = "Số lượng";
            this.soLuong.Name = "soLuong";
            // 
            // mucDich
            // 
            this.mucDich.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.mucDich.HeaderText = "Mục đích";
            this.mucDich.Name = "mucDich";
            // 
            // ngayGiao
            // 
            this.ngayGiao.HeaderText = "Ngày giao";
            this.ngayGiao.Name = "ngayGiao";
            // 
            // slTon
            // 
            this.slTon.HeaderText = "SL Tồn";
            this.slTon.Name = "slTon";
            // 
            // colXoa
            // 
            this.colXoa.HeaderText = "Xóa";
            this.colXoa.Name = "colXoa";
            this.colXoa.Text = "Xoá";
            this.colXoa.UseColumnTextForButtonValue = true;
            this.colXoa.Width = 50;
            // 
            // panel3
            // 
            this.panel3.BackColor = System.Drawing.SystemColors.Control;
            this.panel3.Controls.Add(this.btnDatHang);
            this.panel3.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel3.Location = new System.Drawing.Point(0, 543);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(1140, 82);
            this.panel3.TabIndex = 3;
            // 
            // btnDatHang
            // 
            this.btnDatHang.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.btnDatHang.BackColor = System.Drawing.Color.Firebrick;
            this.btnDatHang.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnDatHang.ForeColor = System.Drawing.SystemColors.Control;
            this.btnDatHang.Location = new System.Drawing.Point(508, 6);
            this.btnDatHang.Name = "btnDatHang";
            this.btnDatHang.Size = new System.Drawing.Size(151, 55);
            this.btnDatHang.TabIndex = 0;
            this.btnDatHang.Text = "Đặt hàng";
            this.btnDatHang.UseVisualStyleBackColor = false;
            this.btnDatHang.Click += new System.EventHandler(this.btnDatHang_Click);
            // 
            // UC_MuaVatTu
            // 
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel3);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Controls.Add(this.panel1);
            this.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "UC_MuaVatTu";
            this.Size = new System.Drawing.Size(1140, 625);
            this.Load += new System.EventHandler(this.UC_MuaVatTu_Load);
            this.panel1.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.panel4.ResumeLayout(false);
            this.flowLayoutPanel1.ResumeLayout(false);
            this.flowLayoutPanel1.PerformLayout();
            this.panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvDSMua)).EndInit();
            this.panel3.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.DataGridView dgvDSMua;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.Button btnDatHang;
        private System.Windows.Forms.Label lblTieuDeTimKiem;
        private System.Windows.Forms.ComboBox cbxTimTenVatTu;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Panel panel4;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.RadioButton rdoTaoMoi;
        private System.Windows.Forms.RadioButton radioButton2;
        private System.Windows.Forms.TextBox tbMaDon;
        private System.Windows.Forms.DataGridViewTextBoxColumn ID;
        private System.Windows.Forms.DataGridViewTextBoxColumn ma;
        private System.Windows.Forms.DataGridViewTextBoxColumn ten;
        private System.Windows.Forms.DataGridViewTextBoxColumn donVi;
        private System.Windows.Forms.DataGridViewTextBoxColumn soLuong;
        private System.Windows.Forms.DataGridViewTextBoxColumn mucDich;
        private System.Windows.Forms.DataGridViewTextBoxColumn ngayGiao;
        private System.Windows.Forms.DataGridViewTextBoxColumn slTon;
        private System.Windows.Forms.DataGridViewButtonColumn colXoa;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox nguoiDat;
    }
}