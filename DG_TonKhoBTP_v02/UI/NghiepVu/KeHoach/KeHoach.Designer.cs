namespace DG_TonKhoBTP_v02.UI.KeHoach
{
    partial class KeHoach
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.panel1 = new System.Windows.Forms.Panel();
            this.dgPlan = new System.Windows.Forms.DataGridView();
            this.status = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.ngayNhan = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.TenSP = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ma = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.lot = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.hangDat = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.hangBan = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.tong = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.mauSac = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ngayGiao = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ghiChu = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.panel2 = new System.Windows.Forms.Panel();
            this.btnSavePlan = new System.Windows.Forms.Button();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgPlan)).BeginInit();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Controls.Add(this.tabPage3);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(1147, 675);
            this.tabControl1.TabIndex = 0;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.panel1);
            this.tabPage1.Location = new System.Drawing.Point(4, 27);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(1139, 644);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Tạo";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.dgPlan);
            this.panel1.Controls.Add(this.panel2);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(3, 3);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1133, 638);
            this.panel1.TabIndex = 0;
            // 
            // dgPlan
            // 
            this.dgPlan.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
            this.dgPlan.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgPlan.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.status,
            this.ngayNhan,
            this.TenSP,
            this.ma,
            this.lot,
            this.hangDat,
            this.hangBan,
            this.tong,
            this.mauSac,
            this.ngayGiao,
            this.ghiChu});
            this.dgPlan.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgPlan.Location = new System.Drawing.Point(0, 0);
            this.dgPlan.Name = "dgPlan";
            this.dgPlan.RowHeadersVisible = false;
            this.dgPlan.RowTemplate.Height = 25;
            this.dgPlan.Size = new System.Drawing.Size(1133, 577);
            this.dgPlan.TabIndex = 0;
            // 
            // status
            // 
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            this.status.DefaultCellStyle = dataGridViewCellStyle2;
            this.status.HeaderText = "Tình trạng";
            this.status.Items.AddRange(new object[] {
            "Tạm thời",
            "Ban hành",
            "Huỷ"});
            this.status.MinimumWidth = 100;
            this.status.Name = "status";
            // 
            // ngayNhan
            // 
            this.ngayNhan.HeaderText = "Ngày nhận";
            this.ngayNhan.MinimumWidth = 120;
            this.ngayNhan.Name = "ngayNhan";
            this.ngayNhan.Width = 120;
            // 
            // TenSP
            // 
            this.TenSP.HeaderText = "Tên SP";
            this.TenSP.MinimumWidth = 170;
            this.TenSP.Name = "TenSP";
            this.TenSP.Width = 170;
            // 
            // ma
            // 
            this.ma.HeaderText = "Mã";
            this.ma.MinimumWidth = 100;
            this.ma.Name = "ma";
            // 
            // lot
            // 
            this.lot.HeaderText = "LOT";
            this.lot.MinimumWidth = 100;
            this.lot.Name = "lot";
            // 
            // hangDat
            // 
            this.hangDat.HeaderText = "Hàng đặt";
            this.hangDat.MinimumWidth = 100;
            this.hangDat.Name = "hangDat";
            // 
            // hangBan
            // 
            this.hangBan.HeaderText = "Hàng bán";
            this.hangBan.MinimumWidth = 100;
            this.hangBan.Name = "hangBan";
            // 
            // tong
            // 
            this.tong.HeaderText = "Tổng";
            this.tong.MinimumWidth = 100;
            this.tong.Name = "tong";
            // 
            // mauSac
            // 
            this.mauSac.HeaderText = "Màu";
            this.mauSac.MinimumWidth = 100;
            this.mauSac.Name = "mauSac";
            // 
            // ngayGiao
            // 
            this.ngayGiao.HeaderText = "Ngày giao";
            this.ngayGiao.MinimumWidth = 120;
            this.ngayGiao.Name = "ngayGiao";
            this.ngayGiao.Width = 120;
            // 
            // ghiChu
            // 
            this.ghiChu.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.ghiChu.HeaderText = "Ghi chú";
            this.ghiChu.MinimumWidth = 200;
            this.ghiChu.Name = "ghiChu";
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.btnSavePlan);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel2.Location = new System.Drawing.Point(0, 577);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(1133, 61);
            this.panel2.TabIndex = 1;
            // 
            // btnSavePlan
            // 
            this.btnSavePlan.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSavePlan.Location = new System.Drawing.Point(1026, 6);
            this.btnSavePlan.Name = "btnSavePlan";
            this.btnSavePlan.Size = new System.Drawing.Size(102, 44);
            this.btnSavePlan.TabIndex = 0;
            this.btnSavePlan.Text = "Lưu";
            this.btnSavePlan.UseVisualStyleBackColor = true;
            this.btnSavePlan.Click += new System.EventHandler(this.btnSavePlan_Click);
            // 
            // tabPage2
            // 
            this.tabPage2.Location = new System.Drawing.Point(4, 27);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(1139, 644);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Kiểm tra";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // tabPage3
            // 
            this.tabPage3.Location = new System.Drawing.Point(4, 27);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Size = new System.Drawing.Size(1139, 644);
            this.tabPage3.TabIndex = 2;
            this.tabPage3.Text = "Giả định SX";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // KeHoach
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1147, 675);
            this.Controls.Add(this.tabControl1);
            this.Name = "KeHoach";
            this.Text = "CreatePlan";
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgPlan)).EndInit();
            this.panel2.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.TabPage tabPage3;
        private System.Windows.Forms.DataGridView dgPlan;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Button btnSavePlan;
        private System.Windows.Forms.DataGridViewComboBoxColumn status;
        private System.Windows.Forms.DataGridViewTextBoxColumn ngayNhan;
        private System.Windows.Forms.DataGridViewTextBoxColumn TenSP;
        private System.Windows.Forms.DataGridViewTextBoxColumn ma;
        private System.Windows.Forms.DataGridViewTextBoxColumn lot;
        private System.Windows.Forms.DataGridViewTextBoxColumn hangDat;
        private System.Windows.Forms.DataGridViewTextBoxColumn hangBan;
        private System.Windows.Forms.DataGridViewTextBoxColumn tong;
        private System.Windows.Forms.DataGridViewTextBoxColumn mauSac;
        private System.Windows.Forms.DataGridViewTextBoxColumn ngayGiao;
        private System.Windows.Forms.DataGridViewTextBoxColumn ghiChu;
    }
}