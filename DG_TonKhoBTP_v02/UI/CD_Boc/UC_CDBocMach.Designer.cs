namespace DG_TonKhoBTP_v02.UI
{
    partial class UC_CDBocMach
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
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.soMet = new System.Windows.Forms.NumericUpDown();
            this.ngoaiQuan = new System.Windows.Forms.ComboBox();
            this.lanDanhThung = new System.Windows.Forms.NumericUpDown();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.soMet)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lanDanhThung)).BeginInit();
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
            this.tableLayoutPanel1.Controls.Add(this.label3, 2, 0);
            this.tableLayoutPanel1.Controls.Add(this.label2, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.label1, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.soMet, 2, 1);
            this.tableLayoutPanel1.Controls.Add(this.ngoaiQuan, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.lanDanhThung, 1, 1);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 35F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(798, 65);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label3.Location = new System.Drawing.Point(229, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(107, 30);
            this.label3.TabIndex = 2;
            this.label3.Text = "Số m máy báo";
            this.label3.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label2.Location = new System.Drawing.Point(116, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(107, 30);
            this.label2.TabIndex = 1;
            this.label2.Text = "Lần đánh thủng";
            this.label2.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label1.Location = new System.Drawing.Point(3, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(107, 30);
            this.label1.TabIndex = 0;
            this.label1.Text = "Ngoại quan";
            this.label1.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            // 
            // soMet
            // 
            this.soMet.DecimalPlaces = 1;
            this.soMet.Dock = System.Windows.Forms.DockStyle.Fill;
            this.soMet.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.soMet.Location = new System.Drawing.Point(229, 33);
            this.soMet.Maximum = new decimal(new int[] {
            1569325055,
            23283064,
            0,
            0});
            this.soMet.Name = "soMet";
            this.soMet.Size = new System.Drawing.Size(107, 24);
            this.soMet.TabIndex = 16;
            // 
            // ngoaiQuan
            // 
            this.ngoaiQuan.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ngoaiQuan.FormattingEnabled = true;
            this.ngoaiQuan.Items.AddRange(new object[] {
            "Tốt",
            "Xấu"});
            this.ngoaiQuan.Location = new System.Drawing.Point(3, 33);
            this.ngoaiQuan.Name = "ngoaiQuan";
            this.ngoaiQuan.Size = new System.Drawing.Size(107, 26);
            this.ngoaiQuan.TabIndex = 17;
            // 
            // lanDanhThung
            // 
            this.lanDanhThung.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lanDanhThung.Location = new System.Drawing.Point(116, 33);
            this.lanDanhThung.Maximum = new decimal(new int[] {
            1569325055,
            23283064,
            0,
            0});
            this.lanDanhThung.Name = "lanDanhThung";
            this.lanDanhThung.Size = new System.Drawing.Size(107, 24);
            this.lanDanhThung.TabIndex = 18;
            // 
            // UC_CDBocMach
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "UC_CDBocMach";
            this.Size = new System.Drawing.Size(798, 65);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.soMet)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lanDanhThung)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown soMet;
        private System.Windows.Forms.ComboBox ngoaiQuan;
        private System.Windows.Forms.NumericUpDown lanDanhThung;
    }
}
