namespace DG_TonKhoBTP_v02.UI.NghiepVuKhac.KeToan.VatTuPhu
{
    partial class FrmVatTuPhu
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
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.đềNghịToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.muaVậtTưToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.muaDịchVụToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.nhậpKhoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.xuấtKhoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.pnMainVatTuPhu = new System.Windows.Forms.Panel();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.đềNghịToolStripMenuItem,
            this.nhậpKhoToolStripMenuItem,
            this.xuấtKhoToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Padding = new System.Windows.Forms.Padding(9, 2, 0, 2);
            this.menuStrip1.Size = new System.Drawing.Size(1558, 26);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // đềNghịToolStripMenuItem
            // 
            this.đềNghịToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.muaVậtTưToolStripMenuItem,
            this.muaDịchVụToolStripMenuItem});
            this.đềNghịToolStripMenuItem.Name = "đềNghịToolStripMenuItem";
            this.đềNghịToolStripMenuItem.Size = new System.Drawing.Size(69, 22);
            this.đềNghịToolStripMenuItem.Text = "Đề nghị";
            // 
            // muaVậtTưToolStripMenuItem
            // 
            this.muaVậtTưToolStripMenuItem.Name = "muaVậtTưToolStripMenuItem";
            this.muaVậtTưToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.muaVậtTưToolStripMenuItem.Text = "Mua vật tư";
            this.muaVậtTưToolStripMenuItem.Click += new System.EventHandler(this.muaVậtTưToolStripMenuItem_Click);
            // 
            // muaDịchVụToolStripMenuItem
            // 
            this.muaDịchVụToolStripMenuItem.Name = "muaDịchVụToolStripMenuItem";
            this.muaDịchVụToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.muaDịchVụToolStripMenuItem.Text = "Mua dịch vụ";
            this.muaDịchVụToolStripMenuItem.Click += new System.EventHandler(this.muaDịchVụToolStripMenuItem_Click);
            // 
            // nhậpKhoToolStripMenuItem
            // 
            this.nhậpKhoToolStripMenuItem.Name = "nhậpKhoToolStripMenuItem";
            this.nhậpKhoToolStripMenuItem.Size = new System.Drawing.Size(84, 22);
            this.nhậpKhoToolStripMenuItem.Text = "Nhập Kho";
            // 
            // xuấtKhoToolStripMenuItem
            // 
            this.xuấtKhoToolStripMenuItem.Name = "xuấtKhoToolStripMenuItem";
            this.xuấtKhoToolStripMenuItem.Size = new System.Drawing.Size(80, 22);
            this.xuấtKhoToolStripMenuItem.Text = "Xuất Kho";
            // 
            // pnMainVatTuPhu
            // 
            this.pnMainVatTuPhu.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnMainVatTuPhu.Location = new System.Drawing.Point(0, 26);
            this.pnMainVatTuPhu.Margin = new System.Windows.Forms.Padding(4);
            this.pnMainVatTuPhu.Name = "pnMainVatTuPhu";
            this.pnMainVatTuPhu.Padding = new System.Windows.Forms.Padding(15, 14, 15, 14);
            this.pnMainVatTuPhu.Size = new System.Drawing.Size(1558, 750);
            this.pnMainVatTuPhu.TabIndex = 1;
            // 
            // FrmVatTuPhu
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1558, 776);
            this.Controls.Add(this.pnMainVatTuPhu);
            this.Controls.Add(this.menuStrip1);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.MainMenuStrip = this.menuStrip1;
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "FrmVatTuPhu";
            this.Text = "FrmVatTuPhu";
            this.Load += new System.EventHandler(this.FrmVatTuPhu_Load);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem đềNghịToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem muaVậtTưToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem muaDịchVụToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem nhậpKhoToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem xuấtKhoToolStripMenuItem;
        private System.Windows.Forms.Panel pnMainVatTuPhu;
    }
}