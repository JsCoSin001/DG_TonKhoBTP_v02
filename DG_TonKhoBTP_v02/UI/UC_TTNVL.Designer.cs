namespace DG_TonKhoBTP_v02.UI
{
    partial class UC_TTNVL
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
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.dtgTTNVL = new System.Windows.Forms.DataGridView();
            this.panel1 = new System.Windows.Forms.Panel();
            this.lblTieuDe = new System.Windows.Forms.Label();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.cbxTimKiem = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dtgTTNVL)).BeginInit();
            this.panel1.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.dtgTTNVL);
            this.groupBox1.Controls.Add(this.panel1);
            this.groupBox1.Controls.Add(this.tableLayoutPanel1);
            this.groupBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.groupBox1.Location = new System.Drawing.Point(10, 10);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(0);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(10);
            this.groupBox1.Size = new System.Drawing.Size(776, 454);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Thông Tin NVL";
            // 
            // dtgTTNVL
            // 
            this.dtgTTNVL.AllowUserToAddRows = false;
            this.dtgTTNVL.BackgroundColor = System.Drawing.SystemColors.ButtonFace;
            this.dtgTTNVL.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.dtgTTNVL.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dtgTTNVL.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dtgTTNVL.Location = new System.Drawing.Point(10, 81);
            this.dtgTTNVL.Name = "dtgTTNVL";
            this.dtgTTNVL.RowHeadersVisible = false;
            this.dtgTTNVL.Size = new System.Drawing.Size(756, 363);
            this.dtgTTNVL.TabIndex = 1;
            this.dtgTTNVL.Visible = false;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.lblTieuDe);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(10, 60);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(756, 21);
            this.panel1.TabIndex = 4;
            // 
            // lblTieuDe
            // 
            this.lblTieuDe.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTieuDe.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTieuDe.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
            this.lblTieuDe.Location = new System.Drawing.Point(0, 0);
            this.lblTieuDe.Name = "lblTieuDe";
            this.lblTieuDe.Size = new System.Drawing.Size(756, 21);
            this.lblTieuDe.TabIndex = 0;
            this.lblTieuDe.Text = "Các ô màu vàng cần điền dữ liệu";
            this.lblTieuDe.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblTieuDe.UseCompatibleTextRendering = true;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 75F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.cbxTimKiem, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.label1, 0, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(10, 25);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(756, 35);
            this.tableLayoutPanel1.TabIndex = 3;
            // 
            // cbxTimKiem
            // 
            this.cbxTimKiem.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cbxTimKiem.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cbxTimKiem.FormattingEnabled = true;
            this.cbxTimKiem.Location = new System.Drawing.Point(78, 3);
            this.cbxTimKiem.Name = "cbxTimKiem";
            this.cbxTimKiem.Size = new System.Drawing.Size(675, 28);
            this.cbxTimKiem.TabIndex = 1;
            this.cbxTimKiem.TextUpdate += new System.EventHandler(this.tbxTimKiem_TextUpdate);
            // 
            // label1
            // 
            this.label1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(3, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(69, 35);
            this.label1.TabIndex = 0;
            this.label1.Text = "Tìm kiếm";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // UC_TTNVL
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupBox1);
            this.Name = "UC_TTNVL";
            this.Padding = new System.Windows.Forms.Padding(10);
            this.Size = new System.Drawing.Size(796, 474);
            this.groupBox1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dtgTTNVL)).EndInit();
            this.panel1.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox cbxTimKiem;
        private System.Windows.Forms.DataGridView dtgTTNVL;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label lblTieuDe;
    }
}
