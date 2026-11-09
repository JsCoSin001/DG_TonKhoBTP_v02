namespace DG_TonKhoBTP_v02.UI.NghiepVuKhac.Kho
{
    partial class UC_NhapKho
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
            this.tabThanhPham = new System.Windows.Forms.TabControl();
            this.tabNhapNVL = new System.Windows.Forms.TabPage();
            this.tabNhapTP = new System.Windows.Forms.TabPage();
            this.tabThanhPham.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.Dock = System.Windows.Forms.DockStyle.Top;
            this.label1.Font = new System.Drawing.Font("Tahoma", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(0, 0);
            this.label1.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(1132, 46);
            this.label1.TabIndex = 1;
            this.label1.Text = "THÔNG TIN NHẬP KHO";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // tabThanhPham
            // 
            this.tabThanhPham.Controls.Add(this.tabNhapNVL);
            this.tabThanhPham.Controls.Add(this.tabNhapTP);
            this.tabThanhPham.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabThanhPham.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tabThanhPham.Location = new System.Drawing.Point(0, 46);
            this.tabThanhPham.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tabThanhPham.Name = "tabThanhPham";
            this.tabThanhPham.SelectedIndex = 0;
            this.tabThanhPham.Size = new System.Drawing.Size(1132, 722);
            this.tabThanhPham.TabIndex = 2;
            // 
            // tabNhapNVL
            // 
            this.tabNhapNVL.Location = new System.Drawing.Point(4, 27);
            this.tabNhapNVL.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tabNhapNVL.Name = "tabNhapNVL";
            this.tabNhapNVL.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tabNhapNVL.Size = new System.Drawing.Size(1124, 691);
            this.tabNhapNVL.TabIndex = 0;
            this.tabNhapNVL.Text = "Nguyên vật liệu";
            this.tabNhapNVL.UseVisualStyleBackColor = true;
            // 
            // tabNhapTP
            // 
            this.tabNhapTP.Location = new System.Drawing.Point(4, 27);
            this.tabNhapTP.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tabNhapTP.Name = "tabNhapTP";
            this.tabNhapTP.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tabNhapTP.Size = new System.Drawing.Size(1124, 691);
            this.tabNhapTP.TabIndex = 1;
            this.tabNhapTP.Text = "Thành Phẩm";
            this.tabNhapTP.UseVisualStyleBackColor = true;
            // 
            // UC_NhapKho
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tabThanhPham);
            this.Controls.Add(this.label1);
            this.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Name = "UC_NhapKho";
            this.Size = new System.Drawing.Size(1132, 768);
            this.tabThanhPham.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TabControl tabThanhPham;
        private System.Windows.Forms.TabPage tabNhapNVL;
        private System.Windows.Forms.TabPage tabNhapTP;
    }
}
