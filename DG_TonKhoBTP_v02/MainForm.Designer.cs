using DG_TonKhoBTP_v02.UI.Component;

namespace DG_TonKhoBTP_v02
{
    partial class MainForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.pnLeft = new System.Windows.Forms.Panel();
            this.fpnButton = new System.Windows.Forms.FlowLayoutPanel();
            this.pnLogo = new System.Windows.Forms.Panel();
            this.lblTenCty = new System.Windows.Forms.Label();
            this.imgLogo = new System.Windows.Forms.PictureBox();
            this.grbChucNang = new System.Windows.Forms.GroupBox();
            this.grbCongCu = new System.Windows.Forms.GroupBox();
            this.pnEdit = new System.Windows.Forms.FlowLayoutPanel();
            this.grbBaoCao = new System.Windows.Forms.GroupBox();
            this.pnBaoCao = new System.Windows.Forms.FlowLayoutPanel();
            this.pnSign = new System.Windows.Forms.Panel();
            this.lblAuthor = new System.Windows.Forms.Label();
            this.pnMain = new System.Windows.Forms.Panel();
            this.panel1 = new System.Windows.Forms.Panel();
            this.pnShow = new System.Windows.Forms.Panel();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.homeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.setiingToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.pnCongDoan = new System.Windows.Forms.FlowLayoutPanel();
            this.btnKeoRut = new DG_TonKhoBTP_v02.UI.Component.CustomButton();
            this.btnBenRuot = new DG_TonKhoBTP_v02.UI.Component.CustomButton();
            this.btnMica = new DG_TonKhoBTP_v02.UI.Component.CustomButton();
            this.btnBocMach = new DG_TonKhoBTP_v02.UI.Component.CustomButton();
            this.btnGhepLoi = new DG_TonKhoBTP_v02.UI.Component.CustomButton();
            this.btnBocLot = new DG_TonKhoBTP_v02.UI.Component.CustomButton();
            this.btnQuanBang = new DG_TonKhoBTP_v02.UI.Component.CustomButton();
            this.btnBocVo = new DG_TonKhoBTP_v02.UI.Component.CustomButton();
            this.btnCapNhatMaHang = new DG_TonKhoBTP_v02.UI.Component.CustomButton();
            this.btnBaoCaoTonKho = new DG_TonKhoBTP_v02.UI.Component.CustomButton();
            this.btnTruyVetDL = new DG_TonKhoBTP_v02.UI.Component.CustomButton();
            this.BtnKiemTraBc = new DG_TonKhoBTP_v02.UI.Component.CustomButton();
            this.pnLeft.SuspendLayout();
            this.fpnButton.SuspendLayout();
            this.pnLogo.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.imgLogo)).BeginInit();
            this.grbChucNang.SuspendLayout();
            this.grbCongCu.SuspendLayout();
            this.pnEdit.SuspendLayout();
            this.grbBaoCao.SuspendLayout();
            this.pnBaoCao.SuspendLayout();
            this.pnSign.SuspendLayout();
            this.pnMain.SuspendLayout();
            this.panel1.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.pnCongDoan.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnLeft
            // 
            this.pnLeft.Controls.Add(this.fpnButton);
            this.pnLeft.Dock = System.Windows.Forms.DockStyle.Left;
            this.pnLeft.Location = new System.Drawing.Point(0, 0);
            this.pnLeft.Margin = new System.Windows.Forms.Padding(4);
            this.pnLeft.Name = "pnLeft";
            this.pnLeft.Size = new System.Drawing.Size(176, 907);
            this.pnLeft.TabIndex = 0;
            // 
            // fpnButton
            // 
            this.fpnButton.Controls.Add(this.pnLogo);
            this.fpnButton.Controls.Add(this.grbChucNang);
            this.fpnButton.Controls.Add(this.grbCongCu);
            this.fpnButton.Controls.Add(this.grbBaoCao);
            this.fpnButton.Dock = System.Windows.Forms.DockStyle.Fill;
            this.fpnButton.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.fpnButton.Location = new System.Drawing.Point(0, 0);
            this.fpnButton.Name = "fpnButton";
            this.fpnButton.Size = new System.Drawing.Size(176, 907);
            this.fpnButton.TabIndex = 2;
            // 
            // pnLogo
            // 
            this.pnLogo.Controls.Add(this.lblTenCty);
            this.pnLogo.Controls.Add(this.imgLogo);
            this.pnLogo.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnLogo.Location = new System.Drawing.Point(3, 3);
            this.pnLogo.Name = "pnLogo";
            this.pnLogo.Size = new System.Drawing.Size(168, 70);
            this.pnLogo.TabIndex = 0;
            // 
            // lblTenCty
            // 
            this.lblTenCty.Cursor = System.Windows.Forms.Cursors.Hand;
            this.lblTenCty.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblTenCty.Font = new System.Drawing.Font("Tahoma", 17.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTenCty.Location = new System.Drawing.Point(67, 0);
            this.lblTenCty.Name = "lblTenCty";
            this.lblTenCty.Size = new System.Drawing.Size(101, 67);
            this.lblTenCty.TabIndex = 1;
            this.lblTenCty.Text = "ĐÔNG GIANG";
            this.lblTenCty.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblTenCty.Click += new System.EventHandler(this.lblTenCty_Click);
            // 
            // imgLogo
            // 
            this.imgLogo.Cursor = System.Windows.Forms.Cursors.Hand;
            this.imgLogo.Dock = System.Windows.Forms.DockStyle.Left;
            this.imgLogo.Image = ((System.Drawing.Image)(resources.GetObject("imgLogo.Image")));
            this.imgLogo.Location = new System.Drawing.Point(0, 0);
            this.imgLogo.Name = "imgLogo";
            this.imgLogo.Padding = new System.Windows.Forms.Padding(10);
            this.imgLogo.Size = new System.Drawing.Size(67, 70);
            this.imgLogo.TabIndex = 0;
            this.imgLogo.TabStop = false;
            this.imgLogo.Click += new System.EventHandler(this.imgLogo_Click);
            // 
            // grbChucNang
            // 
            this.grbChucNang.Controls.Add(this.pnCongDoan);
            this.grbChucNang.Dock = System.Windows.Forms.DockStyle.Top;
            this.grbChucNang.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.grbChucNang.Location = new System.Drawing.Point(3, 79);
            this.grbChucNang.Name = "grbChucNang";
            this.grbChucNang.Size = new System.Drawing.Size(168, 456);
            this.grbChucNang.TabIndex = 0;
            this.grbChucNang.TabStop = false;
            this.grbChucNang.Text = "CÔNG ĐOẠN";
            // 
            // grbCongCu
            // 
            this.grbCongCu.Controls.Add(this.pnEdit);
            this.grbCongCu.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.grbCongCu.Location = new System.Drawing.Point(3, 541);
            this.grbCongCu.Name = "grbCongCu";
            this.grbCongCu.Size = new System.Drawing.Size(168, 87);
            this.grbCongCu.TabIndex = 1;
            this.grbCongCu.TabStop = false;
            this.grbCongCu.Text = "CÔNG CỤ";
            // 
            // pnEdit
            // 
            this.pnEdit.Controls.Add(this.btnCapNhatMaHang);
            this.pnEdit.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnEdit.Location = new System.Drawing.Point(3, 17);
            this.pnEdit.Name = "pnEdit";
            this.pnEdit.Size = new System.Drawing.Size(162, 67);
            this.pnEdit.TabIndex = 2;
            // 
            // grbBaoCao
            // 
            this.grbBaoCao.Controls.Add(this.pnBaoCao);
            this.grbBaoCao.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.grbBaoCao.Location = new System.Drawing.Point(3, 634);
            this.grbBaoCao.Name = "grbBaoCao";
            this.grbBaoCao.Size = new System.Drawing.Size(168, 197);
            this.grbBaoCao.TabIndex = 2;
            this.grbBaoCao.TabStop = false;
            this.grbBaoCao.Text = "BÁO CÁO";
            // 
            // pnBaoCao
            // 
            this.pnBaoCao.Controls.Add(this.btnBaoCaoTonKho);
            this.pnBaoCao.Controls.Add(this.btnTruyVetDL);
            this.pnBaoCao.Controls.Add(this.BtnKiemTraBc);
            this.pnBaoCao.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnBaoCao.Location = new System.Drawing.Point(3, 17);
            this.pnBaoCao.Name = "pnBaoCao";
            this.pnBaoCao.Size = new System.Drawing.Size(162, 177);
            this.pnBaoCao.TabIndex = 3;
            // 
            // pnSign
            // 
            this.pnSign.Controls.Add(this.lblAuthor);
            this.pnSign.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnSign.Location = new System.Drawing.Point(0, 907);
            this.pnSign.Name = "pnSign";
            this.pnSign.Size = new System.Drawing.Size(1334, 30);
            this.pnSign.TabIndex = 4;
            // 
            // lblAuthor
            // 
            this.lblAuthor.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblAuthor.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblAuthor.Location = new System.Drawing.Point(0, 0);
            this.lblAuthor.Name = "lblAuthor";
            this.lblAuthor.Size = new System.Drawing.Size(1334, 19);
            this.lblAuthor.TabIndex = 3;
            this.lblAuthor.Text = "label1";
            this.lblAuthor.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // pnMain
            // 
            this.pnMain.Controls.Add(this.panel1);
            this.pnMain.Controls.Add(this.pnSign);
            this.pnMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnMain.Location = new System.Drawing.Point(0, 28);
            this.pnMain.Name = "pnMain";
            this.pnMain.Size = new System.Drawing.Size(1334, 937);
            this.pnMain.TabIndex = 1;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.pnShow);
            this.panel1.Controls.Add(this.pnLeft);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1334, 907);
            this.panel1.TabIndex = 5;
            // 
            // pnShow
            // 
            this.pnShow.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnShow.Location = new System.Drawing.Point(176, 0);
            this.pnShow.Name = "pnShow";
            this.pnShow.Size = new System.Drawing.Size(1158, 907);
            this.pnShow.TabIndex = 1;
            // 
            // menuStrip1
            // 
            this.menuStrip1.Font = new System.Drawing.Font("Segoe UI", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.homeToolStripMenuItem,
            this.setiingToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(1334, 28);
            this.menuStrip1.TabIndex = 2;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // homeToolStripMenuItem
            // 
            this.homeToolStripMenuItem.Name = "homeToolStripMenuItem";
            this.homeToolStripMenuItem.Size = new System.Drawing.Size(85, 24);
            this.homeToolStripMenuItem.Text = "Trang chủ";
            this.homeToolStripMenuItem.Click += new System.EventHandler(this.homeToolStripMenuItem_Click);
            // 
            // setiingToolStripMenuItem
            // 
            this.setiingToolStripMenuItem.Name = "setiingToolStripMenuItem";
            this.setiingToolStripMenuItem.Size = new System.Drawing.Size(68, 24);
            this.setiingToolStripMenuItem.Text = "Cài đặt";
            this.setiingToolStripMenuItem.Click += new System.EventHandler(this.setiingToolStripMenuItem_Click);
            // 
            // pnCongDoan
            // 
            this.pnCongDoan.Controls.Add(this.btnKeoRut);
            this.pnCongDoan.Controls.Add(this.btnBenRuot);
            this.pnCongDoan.Controls.Add(this.btnMica);
            this.pnCongDoan.Controls.Add(this.btnBocMach);
            this.pnCongDoan.Controls.Add(this.btnGhepLoi);
            this.pnCongDoan.Controls.Add(this.btnBocLot);
            this.pnCongDoan.Controls.Add(this.btnQuanBang);
            this.pnCongDoan.Controls.Add(this.btnBocVo);
            this.pnCongDoan.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnCongDoan.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.pnCongDoan.Location = new System.Drawing.Point(3, 17);
            this.pnCongDoan.Margin = new System.Windows.Forms.Padding(3, 3, 3, 10);
            this.pnCongDoan.Name = "pnCongDoan";
            this.pnCongDoan.Size = new System.Drawing.Size(162, 436);
            this.pnCongDoan.TabIndex = 1;
            // 
            // btnKeoRut
            // 
            this.btnKeoRut.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.btnKeoRut.AutoSize = true;
            this.btnKeoRut.BackColor = System.Drawing.Color.Snow;
            this.btnKeoRut.BorderRadius = 10;
            this.btnKeoRut.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnKeoRut.FlatAppearance.BorderSize = 0;
            this.btnKeoRut.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnKeoRut.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnKeoRut.ForeColor = System.Drawing.SystemColors.ControlText;
            this.btnKeoRut.Location = new System.Drawing.Point(3, 5);
            this.btnKeoRut.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            this.btnKeoRut.Name = "btnKeoRut";
            this.btnKeoRut.Size = new System.Drawing.Size(159, 44);
            this.btnKeoRut.TabIndex = 1;
            this.btnKeoRut.TabStop = false;
            this.btnKeoRut.Text = "CĐ KÉO - RÚT";
            this.btnKeoRut.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnKeoRut.UseVisualStyleBackColor = false;
            this.btnKeoRut.Click += new System.EventHandler(this.btnKeoRut_Click);
            // 
            // btnBenRuot
            // 
            this.btnBenRuot.AutoSize = true;
            this.btnBenRuot.BackColor = System.Drawing.Color.Snow;
            this.btnBenRuot.BorderRadius = 10;
            this.btnBenRuot.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnBenRuot.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBenRuot.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnBenRuot.ForeColor = System.Drawing.SystemColors.ControlText;
            this.btnBenRuot.Location = new System.Drawing.Point(3, 59);
            this.btnBenRuot.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            this.btnBenRuot.Name = "btnBenRuot";
            this.btnBenRuot.Size = new System.Drawing.Size(159, 44);
            this.btnBenRuot.TabIndex = 2;
            this.btnBenRuot.TabStop = false;
            this.btnBenRuot.Text = "CĐ BỆN CU - AL";
            this.btnBenRuot.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnBenRuot.UseVisualStyleBackColor = false;
            this.btnBenRuot.Click += new System.EventHandler(this.btnBenRuot_Click);
            // 
            // btnMica
            // 
            this.btnMica.AutoSize = true;
            this.btnMica.BackColor = System.Drawing.Color.Snow;
            this.btnMica.BorderRadius = 10;
            this.btnMica.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnMica.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnMica.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnMica.ForeColor = System.Drawing.SystemColors.ControlText;
            this.btnMica.Location = new System.Drawing.Point(3, 113);
            this.btnMica.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            this.btnMica.Name = "btnMica";
            this.btnMica.Size = new System.Drawing.Size(159, 44);
            this.btnMica.TabIndex = 3;
            this.btnMica.TabStop = false;
            this.btnMica.Text = "CĐ QB MICA";
            this.btnMica.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnMica.UseVisualStyleBackColor = false;
            this.btnMica.Click += new System.EventHandler(this.btnMica_Click);
            // 
            // btnBocMach
            // 
            this.btnBocMach.AutoSize = true;
            this.btnBocMach.BackColor = System.Drawing.Color.Snow;
            this.btnBocMach.BorderRadius = 10;
            this.btnBocMach.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnBocMach.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBocMach.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnBocMach.ForeColor = System.Drawing.SystemColors.ControlText;
            this.btnBocMach.Location = new System.Drawing.Point(3, 167);
            this.btnBocMach.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            this.btnBocMach.Name = "btnBocMach";
            this.btnBocMach.Size = new System.Drawing.Size(159, 44);
            this.btnBocMach.TabIndex = 4;
            this.btnBocMach.TabStop = false;
            this.btnBocMach.Text = "CĐ BỌC CÁCH ĐIỆN";
            this.btnBocMach.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnBocMach.UseVisualStyleBackColor = false;
            this.btnBocMach.Click += new System.EventHandler(this.btnBocMach_Click);
            // 
            // btnGhepLoi
            // 
            this.btnGhepLoi.AutoSize = true;
            this.btnGhepLoi.BackColor = System.Drawing.Color.Snow;
            this.btnGhepLoi.BorderRadius = 10;
            this.btnGhepLoi.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnGhepLoi.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnGhepLoi.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnGhepLoi.ForeColor = System.Drawing.SystemColors.ControlText;
            this.btnGhepLoi.Location = new System.Drawing.Point(3, 221);
            this.btnGhepLoi.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            this.btnGhepLoi.Name = "btnGhepLoi";
            this.btnGhepLoi.Size = new System.Drawing.Size(159, 44);
            this.btnGhepLoi.TabIndex = 5;
            this.btnGhepLoi.TabStop = false;
            this.btnGhepLoi.Text = "CĐ GHÉP LÕI";
            this.btnGhepLoi.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnGhepLoi.UseVisualStyleBackColor = false;
            this.btnGhepLoi.Click += new System.EventHandler(this.btnGhepLoi_Click);
            // 
            // btnBocLot
            // 
            this.btnBocLot.AutoSize = true;
            this.btnBocLot.BackColor = System.Drawing.Color.Snow;
            this.btnBocLot.BorderRadius = 10;
            this.btnBocLot.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnBocLot.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBocLot.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnBocLot.ForeColor = System.Drawing.SystemColors.ControlText;
            this.btnBocLot.Location = new System.Drawing.Point(3, 275);
            this.btnBocLot.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            this.btnBocLot.Name = "btnBocLot";
            this.btnBocLot.Size = new System.Drawing.Size(159, 44);
            this.btnBocLot.TabIndex = 6;
            this.btnBocLot.TabStop = false;
            this.btnBocLot.Text = "CĐ BỌC LÓT";
            this.btnBocLot.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnBocLot.UseVisualStyleBackColor = false;
            this.btnBocLot.Click += new System.EventHandler(this.btnBocLot_Click);
            // 
            // btnQuanBang
            // 
            this.btnQuanBang.AutoSize = true;
            this.btnQuanBang.BackColor = System.Drawing.Color.Snow;
            this.btnQuanBang.BorderRadius = 10;
            this.btnQuanBang.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnQuanBang.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnQuanBang.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnQuanBang.ForeColor = System.Drawing.SystemColors.ControlText;
            this.btnQuanBang.Location = new System.Drawing.Point(3, 329);
            this.btnQuanBang.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            this.btnQuanBang.Name = "btnQuanBang";
            this.btnQuanBang.Size = new System.Drawing.Size(159, 44);
            this.btnQuanBang.TabIndex = 7;
            this.btnQuanBang.TabStop = false;
            this.btnQuanBang.Text = "CĐ QB THÉP-CU-AL";
            this.btnQuanBang.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnQuanBang.UseVisualStyleBackColor = false;
            this.btnQuanBang.Click += new System.EventHandler(this.btnQuanBang_Click);
            // 
            // btnBocVo
            // 
            this.btnBocVo.AutoSize = true;
            this.btnBocVo.BackColor = System.Drawing.Color.Snow;
            this.btnBocVo.BorderRadius = 10;
            this.btnBocVo.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnBocVo.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBocVo.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnBocVo.ForeColor = System.Drawing.SystemColors.ControlText;
            this.btnBocVo.Location = new System.Drawing.Point(3, 383);
            this.btnBocVo.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            this.btnBocVo.Name = "btnBocVo";
            this.btnBocVo.Size = new System.Drawing.Size(159, 44);
            this.btnBocVo.TabIndex = 8;
            this.btnBocVo.TabStop = false;
            this.btnBocVo.Text = "CĐ BỌC VỎ";
            this.btnBocVo.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnBocVo.UseVisualStyleBackColor = false;
            this.btnBocVo.Click += new System.EventHandler(this.btnBocVo_Click);
            // 
            // btnCapNhatMaHang
            // 
            this.btnCapNhatMaHang.BackColor = System.Drawing.Color.Snow;
            this.btnCapNhatMaHang.BorderRadius = 10;
            this.btnCapNhatMaHang.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnCapNhatMaHang.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCapNhatMaHang.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnCapNhatMaHang.ForeColor = System.Drawing.SystemColors.ControlText;
            this.btnCapNhatMaHang.Location = new System.Drawing.Point(3, 5);
            this.btnCapNhatMaHang.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            this.btnCapNhatMaHang.Name = "btnCapNhatMaHang";
            this.btnCapNhatMaHang.Size = new System.Drawing.Size(159, 44);
            this.btnCapNhatMaHang.TabIndex = 9;
            this.btnCapNhatMaHang.TabStop = false;
            this.btnCapNhatMaHang.Text = "UPDATE MÃ HÀNG";
            this.btnCapNhatMaHang.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnCapNhatMaHang.UseVisualStyleBackColor = false;
            this.btnCapNhatMaHang.Click += new System.EventHandler(this.btnCapNhatMaHang_Click);
            // 
            // btnBaoCaoTonKho
            // 
            this.btnBaoCaoTonKho.BackColor = System.Drawing.Color.Snow;
            this.btnBaoCaoTonKho.BorderRadius = 10;
            this.btnBaoCaoTonKho.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnBaoCaoTonKho.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBaoCaoTonKho.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnBaoCaoTonKho.ForeColor = System.Drawing.SystemColors.ControlText;
            this.btnBaoCaoTonKho.Location = new System.Drawing.Point(3, 5);
            this.btnBaoCaoTonKho.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            this.btnBaoCaoTonKho.Name = "btnBaoCaoTonKho";
            this.btnBaoCaoTonKho.Size = new System.Drawing.Size(159, 44);
            this.btnBaoCaoTonKho.TabIndex = 8;
            this.btnBaoCaoTonKho.TabStop = false;
            this.btnBaoCaoTonKho.Text = "TỒN KHO";
            this.btnBaoCaoTonKho.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnBaoCaoTonKho.UseVisualStyleBackColor = false;
            this.btnBaoCaoTonKho.Click += new System.EventHandler(this.btnBaoCaoTonKho_Click);
            // 
            // btnTruyVetDL
            // 
            this.btnTruyVetDL.BackColor = System.Drawing.Color.Snow;
            this.btnTruyVetDL.BorderRadius = 10;
            this.btnTruyVetDL.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnTruyVetDL.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnTruyVetDL.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnTruyVetDL.ForeColor = System.Drawing.SystemColors.ControlText;
            this.btnTruyVetDL.Location = new System.Drawing.Point(3, 59);
            this.btnTruyVetDL.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            this.btnTruyVetDL.Name = "btnTruyVetDL";
            this.btnTruyVetDL.Size = new System.Drawing.Size(159, 44);
            this.btnTruyVetDL.TabIndex = 9;
            this.btnTruyVetDL.TabStop = false;
            this.btnTruyVetDL.Text = "TRUY VẾT DỮ LIỆU";
            this.btnTruyVetDL.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnTruyVetDL.UseVisualStyleBackColor = false;
            this.btnTruyVetDL.Click += new System.EventHandler(this.btnTruyVetDL_Click);
            // 
            // BtnKiemTraBc
            // 
            this.BtnKiemTraBc.BackColor = System.Drawing.Color.Snow;
            this.BtnKiemTraBc.BorderRadius = 10;
            this.BtnKiemTraBc.Cursor = System.Windows.Forms.Cursors.Hand;
            this.BtnKiemTraBc.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.BtnKiemTraBc.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.BtnKiemTraBc.ForeColor = System.Drawing.SystemColors.ControlText;
            this.BtnKiemTraBc.Location = new System.Drawing.Point(3, 113);
            this.BtnKiemTraBc.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            this.BtnKiemTraBc.Name = "BtnKiemTraBc";
            this.BtnKiemTraBc.Size = new System.Drawing.Size(159, 44);
            this.BtnKiemTraBc.TabIndex = 10;
            this.BtnKiemTraBc.TabStop = false;
            this.BtnKiemTraBc.Text = "KIỂM TRA BÁO CÁO";
            this.BtnKiemTraBc.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.BtnKiemTraBc.UseVisualStyleBackColor = false;
            this.BtnKiemTraBc.Click += new System.EventHandler(this.BtnKiemTraBc_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1334, 965);
            this.Controls.Add(this.pnMain);
            this.Controls.Add(this.menuStrip1);
            this.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Home";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.pnLeft.ResumeLayout(false);
            this.fpnButton.ResumeLayout(false);
            this.pnLogo.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.imgLogo)).EndInit();
            this.grbChucNang.ResumeLayout(false);
            this.grbCongCu.ResumeLayout(false);
            this.pnEdit.ResumeLayout(false);
            this.grbBaoCao.ResumeLayout(false);
            this.pnBaoCao.ResumeLayout(false);
            this.pnSign.ResumeLayout(false);
            this.pnMain.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.pnCongDoan.ResumeLayout(false);
            this.pnCongDoan.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel pnLeft;
        private System.Windows.Forms.Panel pnLogo;
        private System.Windows.Forms.PictureBox imgLogo;
        private System.Windows.Forms.Label lblTenCty;
        private System.Windows.Forms.Panel pnSign;
        private System.Windows.Forms.FlowLayoutPanel pnBaoCao;
        private System.Windows.Forms.FlowLayoutPanel pnEdit;

        //private System.Windows.Forms.Button btnKeoRut;
        //private System.Windows.Forms.Button btnBenRuot;
        //private System.Windows.Forms.Button btnMica;
        //private System.Windows.Forms.Button btnBocLot;
        //private System.Windows.Forms.Button btnBocMach;
        //private System.Windows.Forms.Button btnBocVo;
        //private System.Windows.Forms.Button btnBaoCaoTonKho;
        //private System.Windows.Forms.Button btnCapNhatMaHang;


        private CustomButton btnKeoRut;

        private CustomButton btnBenRuot;
        private CustomButton btnMica;
        private CustomButton btnBocLot;
        private CustomButton btnBocMach;
        private CustomButton btnBocVo;
        private CustomButton btnBaoCaoTonKho;
        private CustomButton btnCapNhatMaHang;


        private System.Windows.Forms.Panel pnMain;
        private System.Windows.Forms.Panel pnShow;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem homeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem setiingToolStripMenuItem;
        private System.Windows.Forms.GroupBox grbBaoCao;
        private System.Windows.Forms.GroupBox grbCongCu;
        private System.Windows.Forms.GroupBox grbChucNang;
        private System.Windows.Forms.FlowLayoutPanel fpnButton;

        //private System.Windows.Forms.Button btnGhepLoi;
        //private System.Windows.Forms.Button btnQuanBang;
        //private System.Windows.Forms.Button btnTruyVetDL;
        //private System.Windows.Forms.Button BtnKiemTraBc;


        private CustomButton btnGhepLoi;
        private CustomButton btnQuanBang;
        private CustomButton btnTruyVetDL;
        private CustomButton BtnKiemTraBc;


        private System.Windows.Forms.Label lblAuthor;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.FlowLayoutPanel pnCongDoan;
    }
}

