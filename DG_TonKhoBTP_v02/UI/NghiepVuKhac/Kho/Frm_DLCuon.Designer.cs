namespace DG_TonKhoBTP_v02.UI.NghiepVuKhac.Kho
{
    partial class Frm_DLCuon
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
            this.label1 = new System.Windows.Forms.Label();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.btnLuuTTCuonDay = new System.Windows.Forms.Button();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.slCuon = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.tongChieuDai = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.soDau = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.soCuoi = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ghiChu = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.Dock = System.Windows.Forms.DockStyle.Top;
            this.label1.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(10, 10);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(720, 35);
            this.label1.TabIndex = 0;
            this.label1.Text = "BẢNG NHẬP LIỆU";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 3;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 142F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Controls.Add(this.btnLuuTTCuonDay, 1, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(10, 325);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(720, 65);
            this.tableLayoutPanel1.TabIndex = 1;
            // 
            // btnLuuTTCuonDay
            // 
            this.btnLuuTTCuonDay.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnLuuTTCuonDay.Location = new System.Drawing.Point(292, 3);
            this.btnLuuTTCuonDay.Name = "btnLuuTTCuonDay";
            this.btnLuuTTCuonDay.Size = new System.Drawing.Size(136, 59);
            this.btnLuuTTCuonDay.TabIndex = 0;
            this.btnLuuTTCuonDay.Text = "Lưu";
            this.btnLuuTTCuonDay.UseVisualStyleBackColor = true;
            this.btnLuuTTCuonDay.Click += new System.EventHandler(this.btnLuuTTCuonDay_Click);
            // 
            // dataGridView1
            // 
            this.dataGridView1.BackgroundColor = System.Drawing.SystemColors.Control;
            this.dataGridView1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.dataGridView1.ColumnHeadersHeight = 30;
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.dataGridView1.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.slCuon,
            this.tongChieuDai,
            this.soDau,
            this.soCuoi,
            this.ghiChu});
            this.dataGridView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridView1.Location = new System.Drawing.Point(10, 45);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.RowHeadersVisible = false;
            this.dataGridView1.RowTemplate.Height = 30;
            this.dataGridView1.Size = new System.Drawing.Size(720, 280);
            this.dataGridView1.TabIndex = 2;
            // 
            // slCuon
            // 
            this.slCuon.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            this.slCuon.HeaderText = "SL Cuộn";
            this.slCuon.Name = "slCuon";
            this.slCuon.Width = 86;
            // 
            // tongChieuDai
            // 
            this.tongChieuDai.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            this.tongChieuDai.HeaderText = "Tổng CD";
            this.tongChieuDai.Name = "tongChieuDai";
            this.tongChieuDai.Width = 91;
            // 
            // soDau
            // 
            this.soDau.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            this.soDau.HeaderText = "Số đầu";
            this.soDau.Name = "soDau";
            this.soDau.Width = 79;
            // 
            // soCuoi
            // 
            this.soCuoi.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            this.soCuoi.HeaderText = "Số cuối";
            this.soCuoi.Name = "soCuoi";
            this.soCuoi.Width = 79;
            // 
            // ghiChu
            // 
            this.ghiChu.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.ghiChu.HeaderText = "Ghi chú";
            this.ghiChu.Name = "ghiChu";
            // 
            // Frm_DLCuon
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(740, 400);
            this.Controls.Add(this.dataGridView1);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Controls.Add(this.label1);
            this.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "Frm_DLCuon";
            this.Padding = new System.Windows.Forms.Padding(10);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Frm_DLCuon";
            this.tableLayoutPanel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Button btnLuuTTCuonDay;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.DataGridViewTextBoxColumn slCuon;
        private System.Windows.Forms.DataGridViewTextBoxColumn tongChieuDai;
        private System.Windows.Forms.DataGridViewTextBoxColumn soDau;
        private System.Windows.Forms.DataGridViewTextBoxColumn soCuoi;
        private System.Windows.Forms.DataGridViewTextBoxColumn ghiChu;
    }
}