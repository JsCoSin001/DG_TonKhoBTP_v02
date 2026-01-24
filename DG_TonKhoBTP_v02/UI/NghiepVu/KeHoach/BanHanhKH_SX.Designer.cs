namespace DG_TonKhoBTP_v02.UI.NghiepVu.KeHoach
{
    partial class BanHanhKH_SX
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
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
            this.cbxTimTenSP = new System.Windows.Forms.ComboBox();
            this.label29 = new System.Windows.Forms.Label();
            this.dtgDSKeHoach = new System.Windows.Forms.DataGridView();
            this.flowLayoutPanel2 = new System.Windows.Forms.FlowLayoutPanel();
            this.btnInKHSX = new System.Windows.Forms.Button();
            this.btnClear = new System.Windows.Forms.Button();
            this.lot = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ten = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ghiChuKH = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ghiChuSX = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.rut = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.ben = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.qb = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.bocLot = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.bocMach = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.bocVo = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.groupBox2.SuspendLayout();
            this.tableLayoutPanel3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dtgDSKeHoach)).BeginInit();
            this.flowLayoutPanel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.tableLayoutPanel3);
            this.groupBox2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox2.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.groupBox2.Location = new System.Drawing.Point(0, 0);
            this.groupBox2.Margin = new System.Windows.Forms.Padding(4);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Padding = new System.Windows.Forms.Padding(7);
            this.groupBox2.Size = new System.Drawing.Size(908, 289);
            this.groupBox2.TabIndex = 1;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Ban hành KH - SX";
            // 
            // tableLayoutPanel3
            // 
            this.tableLayoutPanel3.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tableLayoutPanel3.ColumnCount = 2;
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 12.80724F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 87.19276F));
            this.tableLayoutPanel3.Controls.Add(this.cbxTimTenSP, 1, 0);
            this.tableLayoutPanel3.Controls.Add(this.label29, 0, 0);
            this.tableLayoutPanel3.Controls.Add(this.dtgDSKeHoach, 0, 1);
            this.tableLayoutPanel3.Controls.Add(this.flowLayoutPanel2, 1, 2);
            this.tableLayoutPanel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel3.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tableLayoutPanel3.Location = new System.Drawing.Point(7, 23);
            this.tableLayoutPanel3.Margin = new System.Windows.Forms.Padding(4);
            this.tableLayoutPanel3.Name = "tableLayoutPanel3";
            this.tableLayoutPanel3.RowCount = 3;
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 60F));
            this.tableLayoutPanel3.Size = new System.Drawing.Size(894, 259);
            this.tableLayoutPanel3.TabIndex = 0;
            // 
            // cbxTimTenSP
            // 
            this.cbxTimTenSP.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.cbxTimTenSP.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cbxTimTenSP.FormattingEnabled = true;
            this.cbxTimTenSP.Location = new System.Drawing.Point(118, 4);
            this.cbxTimTenSP.Margin = new System.Windows.Forms.Padding(4);
            this.cbxTimTenSP.Name = "cbxTimTenSP";
            this.cbxTimTenSP.Size = new System.Drawing.Size(772, 26);
            this.cbxTimTenSP.TabIndex = 23;
            this.cbxTimTenSP.SelectionChangeCommitted += new System.EventHandler(this.cbxTimTenSP_SelectionChangeCommitted);
            // 
            // label29
            // 
            this.label29.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label29.AutoSize = true;
            this.label29.Location = new System.Drawing.Point(4, 9);
            this.label29.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label29.Name = "label29";
            this.label29.Size = new System.Drawing.Size(106, 16);
            this.label29.TabIndex = 2;
            this.label29.Text = "Tìm tên SP";
            // 
            // dtgDSKeHoach
            // 
            this.dtgDSKeHoach.AllowUserToAddRows = false;
            this.dtgDSKeHoach.AllowUserToDeleteRows = false;
            this.dtgDSKeHoach.BackgroundColor = System.Drawing.SystemColors.Control;
            this.dtgDSKeHoach.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dtgDSKeHoach.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.lot,
            this.ten,
            this.ghiChuKH,
            this.ghiChuSX,
            this.rut,
            this.ben,
            this.qb,
            this.bocLot,
            this.bocMach,
            this.bocVo});
            this.tableLayoutPanel3.SetColumnSpan(this.dtgDSKeHoach, 2);
            this.dtgDSKeHoach.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dtgDSKeHoach.Location = new System.Drawing.Point(4, 38);
            this.dtgDSKeHoach.Margin = new System.Windows.Forms.Padding(4);
            this.dtgDSKeHoach.Name = "dtgDSKeHoach";
            this.dtgDSKeHoach.RowHeadersVisible = false;
            this.dtgDSKeHoach.Size = new System.Drawing.Size(886, 157);
            this.dtgDSKeHoach.TabIndex = 24;
            // 
            // flowLayoutPanel2
            // 
            this.flowLayoutPanel2.Controls.Add(this.btnInKHSX);
            this.flowLayoutPanel2.Controls.Add(this.btnClear);
            this.flowLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel2.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.flowLayoutPanel2.Location = new System.Drawing.Point(118, 203);
            this.flowLayoutPanel2.Margin = new System.Windows.Forms.Padding(4);
            this.flowLayoutPanel2.Name = "flowLayoutPanel2";
            this.flowLayoutPanel2.Size = new System.Drawing.Size(772, 52);
            this.flowLayoutPanel2.TabIndex = 25;
            // 
            // btnInKHSX
            // 
            this.btnInKHSX.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.btnInKHSX.Location = new System.Drawing.Point(648, 4);
            this.btnInKHSX.Margin = new System.Windows.Forms.Padding(4);
            this.btnInKHSX.Name = "btnInKHSX";
            this.btnInKHSX.Size = new System.Drawing.Size(120, 48);
            this.btnInKHSX.TabIndex = 0;
            this.btnInKHSX.Text = "BAN HÀNH KH";
            this.btnInKHSX.UseVisualStyleBackColor = true;
            this.btnInKHSX.Click += new System.EventHandler(this.btnInKHSX_Click);
            // 
            // btnClear
            // 
            this.btnClear.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClear.Location = new System.Drawing.Point(520, 4);
            this.btnClear.Margin = new System.Windows.Forms.Padding(4);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(120, 48);
            this.btnClear.TabIndex = 1;
            this.btnClear.Text = "Làm lại";
            this.btnClear.UseVisualStyleBackColor = true;
            this.btnClear.Click += new System.EventHandler(this.btnClear_Click);
            // 
            // lot
            // 
            this.lot.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.lot.HeaderText = "Mã KH";
            this.lot.Name = "lot";
            this.lot.Width = 70;
            // 
            // ten
            // 
            this.ten.HeaderText = "Tên TP";
            this.ten.Name = "ten";
            this.ten.Width = 160;
            // 
            // ghiChuKH
            // 
            this.ghiChuKH.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.ghiChuKH.HeaderText = "Ghi chú KH";
            this.ghiChuKH.Name = "ghiChuKH";
            // 
            // ghiChuSX
            // 
            this.ghiChuSX.HeaderText = "Ghi chú SX";
            this.ghiChuSX.Name = "ghiChuSX";
            this.ghiChuSX.Width = 150;
            // 
            // rut
            // 
            this.rut.HeaderText = "Rút";
            this.rut.Name = "rut";
            this.rut.Visible = false;
            this.rut.Width = 50;
            // 
            // ben
            // 
            this.ben.HeaderText = "Bện";
            this.ben.Name = "ben";
            this.ben.Visible = false;
            this.ben.Width = 50;
            // 
            // qb
            // 
            this.qb.HeaderText = "GL-QB";
            this.qb.Name = "qb";
            this.qb.Width = 70;
            // 
            // bocLot
            // 
            this.bocLot.HeaderText = "B.Lót";
            this.bocLot.Name = "bocLot";
            this.bocLot.Width = 70;
            // 
            // bocMach
            // 
            this.bocMach.HeaderText = "B.Mạch";
            this.bocMach.Name = "bocMach";
            this.bocMach.Width = 70;
            // 
            // bocVo
            // 
            this.bocVo.HeaderText = "B.Vỏ";
            this.bocVo.Name = "bocVo";
            this.bocVo.Width = 70;
            // 
            // BanHanhKH_SX
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupBox2);
            this.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "BanHanhKH_SX";
            this.Size = new System.Drawing.Size(908, 289);
            this.Load += new System.EventHandler(this.BanHanhKH_SX_Load);
            this.groupBox2.ResumeLayout(false);
            this.tableLayoutPanel3.ResumeLayout(false);
            this.tableLayoutPanel3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dtgDSKeHoach)).EndInit();
            this.flowLayoutPanel2.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel3;
        private System.Windows.Forms.ComboBox cbxTimTenSP;
        private System.Windows.Forms.Label label29;
        private System.Windows.Forms.DataGridView dtgDSKeHoach;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel2;
        private System.Windows.Forms.Button btnInKHSX;
        private System.Windows.Forms.Button btnClear;
        private System.Windows.Forms.DataGridViewTextBoxColumn lot;
        private System.Windows.Forms.DataGridViewTextBoxColumn ten;
        private System.Windows.Forms.DataGridViewTextBoxColumn ghiChuKH;
        private System.Windows.Forms.DataGridViewTextBoxColumn ghiChuSX;
        private System.Windows.Forms.DataGridViewCheckBoxColumn rut;
        private System.Windows.Forms.DataGridViewCheckBoxColumn ben;
        private System.Windows.Forms.DataGridViewCheckBoxColumn qb;
        private System.Windows.Forms.DataGridViewCheckBoxColumn bocLot;
        private System.Windows.Forms.DataGridViewCheckBoxColumn bocMach;
        private System.Windows.Forms.DataGridViewCheckBoxColumn bocVo;
    }
}
