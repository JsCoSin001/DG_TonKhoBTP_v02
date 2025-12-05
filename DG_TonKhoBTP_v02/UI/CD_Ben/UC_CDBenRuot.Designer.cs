namespace DG_TonKhoBTP_v02.UI
{
    partial class UC_CDBenRuot
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
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.buocBen = new System.Windows.Forms.NumericUpDown();
            this.ChieuXoan = new System.Windows.Forms.ComboBox();
            this.soSoi = new System.Windows.Forms.NumericUpDown();
            this.dkSoi = new System.Windows.Forms.NumericUpDown();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.buocBen)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.soSoi)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dkSoi)).BeginInit();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.AutoSize = true;
            this.tableLayoutPanel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tableLayoutPanel1.ColumnCount = 7;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 14.28F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 14.28F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 14.28F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 14.28F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 14.28F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 14.28F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 14.32F));
            this.tableLayoutPanel1.Controls.Add(this.label4, 3, 0);
            this.tableLayoutPanel1.Controls.Add(this.label3, 2, 0);
            this.tableLayoutPanel1.Controls.Add(this.label2, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.label1, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.buocBen, 3, 1);
            this.tableLayoutPanel1.Controls.Add(this.ChieuXoan, 2, 1);
            this.tableLayoutPanel1.Controls.Add(this.soSoi, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.dkSoi, 0, 1);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 35F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(777, 65);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label4.Location = new System.Drawing.Point(333, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(104, 30);
            this.label4.TabIndex = 18;
            this.label4.Text = "Bước bện";
            this.label4.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label3.Location = new System.Drawing.Point(223, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(104, 30);
            this.label3.TabIndex = 2;
            this.label3.Text = "Chiều xoắn";
            this.label3.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label2.Location = new System.Drawing.Point(113, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(104, 30);
            this.label2.TabIndex = 1;
            this.label2.Text = "Số sợi TP";
            this.label2.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label1.Location = new System.Drawing.Point(3, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(104, 30);
            this.label1.TabIndex = 0;
            this.label1.Text = "ĐK sợi TP";
            this.label1.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            // 
            // buocBen
            // 
            this.buocBen.DecimalPlaces = 1;
            this.buocBen.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buocBen.Location = new System.Drawing.Point(333, 33);
            this.buocBen.Maximum = new decimal(new int[] {
            1569325055,
            23283064,
            0,
            0});
            this.buocBen.Name = "buocBen";
            this.buocBen.Size = new System.Drawing.Size(104, 24);
            this.buocBen.TabIndex = 17;
            // 
            // ChieuXoan
            // 
            this.ChieuXoan.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ChieuXoan.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ChieuXoan.FormattingEnabled = true;
            this.ChieuXoan.Items.AddRange(new object[] {
            "Z",
            "S"});
            this.ChieuXoan.Location = new System.Drawing.Point(223, 33);
            this.ChieuXoan.Name = "ChieuXoan";
            this.ChieuXoan.Size = new System.Drawing.Size(104, 26);
            this.ChieuXoan.TabIndex = 16;
            // 
            // soSoi
            // 
            this.soSoi.Dock = System.Windows.Forms.DockStyle.Fill;
            this.soSoi.Location = new System.Drawing.Point(113, 33);
            this.soSoi.Maximum = new decimal(new int[] {
            1569325055,
            23283064,
            0,
            0});
            this.soSoi.Name = "soSoi";
            this.soSoi.Size = new System.Drawing.Size(104, 24);
            this.soSoi.TabIndex = 19;
            // 
            // dkSoi
            // 
            this.dkSoi.DecimalPlaces = 1;
            this.dkSoi.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dkSoi.Location = new System.Drawing.Point(3, 33);
            this.dkSoi.Maximum = new decimal(new int[] {
            1569325055,
            23283064,
            0,
            0});
            this.dkSoi.Name = "dkSoi";
            this.dkSoi.Size = new System.Drawing.Size(104, 24);
            this.dkSoi.TabIndex = 20;
            // 
            // UC_CDBenRuot
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "UC_CDBenRuot";
            this.Size = new System.Drawing.Size(777, 65);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.buocBen)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.soSoi)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dkSoi)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox ChieuXoan;
        private System.Windows.Forms.NumericUpDown buocBen;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.NumericUpDown soSoi;
        private System.Windows.Forms.NumericUpDown dkSoi;
    }
}
