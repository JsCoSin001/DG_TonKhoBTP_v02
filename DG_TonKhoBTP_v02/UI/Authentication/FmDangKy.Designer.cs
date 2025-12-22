namespace DG_TonKhoBTP_v02.UI.Setting
{
    partial class FmDangKy
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
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.tableLayoutPanel5 = new System.Windows.Forms.TableLayoutPanel();
            this.label11 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.txtPassword = new System.Windows.Forms.TextBox();
            this.clbDanhSachNhom = new System.Windows.Forms.CheckedListBox();
            this.flowLayoutPanel2 = new System.Windows.Forms.FlowLayoutPanel();
            this.btnDangKi = new System.Windows.Forms.Button();
            this.tbnXoa = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.rdoActive = new System.Windows.Forms.RadioButton();
            this.rdoDisActive = new System.Windows.Forms.RadioButton();
            this.label5 = new System.Windows.Forms.Label();
            this.tbName = new System.Windows.Forms.TextBox();
            this.userName = new System.Windows.Forms.ComboBox();
            this.flowLayoutPanel5 = new System.Windows.Forms.FlowLayoutPanel();
            this.rdoAddUser = new System.Windows.Forms.RadioButton();
            this.rdoEditUser = new System.Windows.Forms.RadioButton();
            this.tbPhanQuyen = new System.Windows.Forms.TabPage();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.flowLayoutPanel3 = new System.Windows.Forms.FlowLayoutPanel();
            this.btnLuu = new System.Windows.Forms.Button();
            this.grvQuyen = new System.Windows.Forms.DataGridView();
            this.cb = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.permission_code = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.permission_name = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.permission_2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.panel1 = new System.Windows.Forms.Panel();
            this.tvDanhSach = new System.Windows.Forms.TreeView();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tableLayoutPanel5.SuspendLayout();
            this.flowLayoutPanel2.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.flowLayoutPanel5.SuspendLayout();
            this.tbPhanQuyen.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.flowLayoutPanel3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grvQuyen)).BeginInit();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tbPhanQuyen);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Font = new System.Drawing.Font("Tahoma", 9.75F);
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(800, 362);
            this.tabControl1.TabIndex = 0;
            this.tabControl1.SelectedIndexChanged += new System.EventHandler(this.tabControl1_SelectedIndexChanged);
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.tableLayoutPanel5);
            this.tabPage1.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tabPage1.Location = new System.Drawing.Point(4, 25);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3, 10, 3, 3);
            this.tabPage1.Size = new System.Drawing.Size(792, 333);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Người dùng";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // tableLayoutPanel5
            // 
            this.tableLayoutPanel5.ColumnCount = 4;
            this.tableLayoutPanel5.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 104F));
            this.tableLayoutPanel5.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel5.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 91F));
            this.tableLayoutPanel5.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel5.Controls.Add(this.label11, 0, 2);
            this.tableLayoutPanel5.Controls.Add(this.label9, 0, 1);
            this.tableLayoutPanel5.Controls.Add(this.label10, 2, 1);
            this.tableLayoutPanel5.Controls.Add(this.txtPassword, 3, 1);
            this.tableLayoutPanel5.Controls.Add(this.clbDanhSachNhom, 1, 2);
            this.tableLayoutPanel5.Controls.Add(this.flowLayoutPanel2, 3, 4);
            this.tableLayoutPanel5.Controls.Add(this.label1, 2, 3);
            this.tableLayoutPanel5.Controls.Add(this.flowLayoutPanel1, 3, 3);
            this.tableLayoutPanel5.Controls.Add(this.label5, 2, 2);
            this.tableLayoutPanel5.Controls.Add(this.tbName, 3, 2);
            this.tableLayoutPanel5.Controls.Add(this.userName, 1, 1);
            this.tableLayoutPanel5.Controls.Add(this.flowLayoutPanel5, 0, 0);
            this.tableLayoutPanel5.Dock = System.Windows.Forms.DockStyle.Top;
            this.tableLayoutPanel5.Location = new System.Drawing.Point(3, 10);
            this.tableLayoutPanel5.Name = "tableLayoutPanel5";
            this.tableLayoutPanel5.RowCount = 5;
            this.tableLayoutPanel5.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 41F));
            this.tableLayoutPanel5.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 49F));
            this.tableLayoutPanel5.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 47F));
            this.tableLayoutPanel5.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 54F));
            this.tableLayoutPanel5.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel5.Size = new System.Drawing.Size(786, 247);
            this.tableLayoutPanel5.TabIndex = 3;
            // 
            // label11
            // 
            this.label11.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(3, 104);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(98, 18);
            this.label11.TabIndex = 4;
            this.label11.Text = "Thuộc nhóm";
            // 
            // label9
            // 
            this.label9.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(3, 56);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(98, 18);
            this.label9.TabIndex = 0;
            this.label9.Text = "Tài khoản";
            // 
            // label10
            // 
            this.label10.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(402, 56);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(85, 18);
            this.label10.TabIndex = 1;
            this.label10.Text = "Mật khẩu";
            this.label10.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // txtPassword
            // 
            this.txtPassword.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtPassword.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtPassword.Location = new System.Drawing.Point(493, 52);
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.Size = new System.Drawing.Size(290, 26);
            this.txtPassword.TabIndex = 3;
            this.txtPassword.UseSystemPasswordChar = true;
            // 
            // clbDanhSachNhom
            // 
            this.clbDanhSachNhom.Dock = System.Windows.Forms.DockStyle.Fill;
            this.clbDanhSachNhom.FormattingEnabled = true;
            this.clbDanhSachNhom.Location = new System.Drawing.Point(107, 93);
            this.clbDanhSachNhom.Name = "clbDanhSachNhom";
            this.tableLayoutPanel5.SetRowSpan(this.clbDanhSachNhom, 3);
            this.clbDanhSachNhom.Size = new System.Drawing.Size(289, 151);
            this.clbDanhSachNhom.TabIndex = 9;
            // 
            // flowLayoutPanel2
            // 
            this.flowLayoutPanel2.Controls.Add(this.btnDangKi);
            this.flowLayoutPanel2.Controls.Add(this.tbnXoa);
            this.flowLayoutPanel2.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.flowLayoutPanel2.Location = new System.Drawing.Point(493, 194);
            this.flowLayoutPanel2.Name = "flowLayoutPanel2";
            this.flowLayoutPanel2.Size = new System.Drawing.Size(290, 48);
            this.flowLayoutPanel2.TabIndex = 8;
            // 
            // btnDangKi
            // 
            this.btnDangKi.Location = new System.Drawing.Point(201, 3);
            this.btnDangKi.Name = "btnDangKi";
            this.btnDangKi.Size = new System.Drawing.Size(86, 37);
            this.btnDangKi.TabIndex = 0;
            this.btnDangKi.Text = "Lưu";
            this.btnDangKi.UseVisualStyleBackColor = true;
            this.btnDangKi.Click += new System.EventHandler(this.btnDangKi_Click);
            // 
            // tbnXoa
            // 
            this.tbnXoa.Location = new System.Drawing.Point(109, 3);
            this.tbnXoa.Name = "tbnXoa";
            this.tbnXoa.Size = new System.Drawing.Size(86, 37);
            this.tbnXoa.TabIndex = 1;
            this.tbnXoa.Text = "Xoá";
            this.tbnXoa.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(402, 155);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(85, 18);
            this.label1.TabIndex = 6;
            this.label1.Text = "Tình trạng";
            this.label1.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.flowLayoutPanel1.AutoSize = true;
            this.flowLayoutPanel1.Controls.Add(this.rdoActive);
            this.flowLayoutPanel1.Controls.Add(this.rdoDisActive);
            this.flowLayoutPanel1.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.flowLayoutPanel1.Location = new System.Drawing.Point(493, 150);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(290, 28);
            this.flowLayoutPanel1.TabIndex = 7;
            // 
            // rdoActive
            // 
            this.rdoActive.AutoSize = true;
            this.rdoActive.Checked = true;
            this.rdoActive.Location = new System.Drawing.Point(3, 3);
            this.rdoActive.Name = "rdoActive";
            this.rdoActive.Size = new System.Drawing.Size(86, 22);
            this.rdoActive.TabIndex = 0;
            this.rdoActive.TabStop = true;
            this.rdoActive.Text = "Kích hoạt";
            this.rdoActive.UseVisualStyleBackColor = true;
            // 
            // rdoDisActive
            // 
            this.rdoDisActive.AutoSize = true;
            this.rdoDisActive.Location = new System.Drawing.Point(95, 3);
            this.rdoDisActive.Name = "rdoDisActive";
            this.rdoDisActive.Size = new System.Drawing.Size(95, 22);
            this.rdoDisActive.TabIndex = 1;
            this.rdoDisActive.Text = "Tạm dừng";
            this.rdoDisActive.UseVisualStyleBackColor = true;
            // 
            // label5
            // 
            this.label5.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(402, 104);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(85, 18);
            this.label5.TabIndex = 1;
            this.label5.Text = "Tên hiển thị";
            this.label5.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // tbName
            // 
            this.tbName.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.tbName.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tbName.Location = new System.Drawing.Point(493, 100);
            this.tbName.Name = "tbName";
            this.tbName.Size = new System.Drawing.Size(290, 26);
            this.tbName.TabIndex = 3;
            // 
            // userName
            // 
            this.userName.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.userName.FormattingEnabled = true;
            this.userName.IntegralHeight = false;
            this.userName.Location = new System.Drawing.Point(107, 55);
            this.userName.MaxDropDownItems = 12;
            this.userName.Name = "userName";
            this.userName.Size = new System.Drawing.Size(289, 26);
            this.userName.TabIndex = 10;
            // 
            // flowLayoutPanel5
            // 
            this.tableLayoutPanel5.SetColumnSpan(this.flowLayoutPanel5, 4);
            this.flowLayoutPanel5.Controls.Add(this.rdoAddUser);
            this.flowLayoutPanel5.Controls.Add(this.rdoEditUser);
            this.flowLayoutPanel5.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel5.Location = new System.Drawing.Point(3, 3);
            this.flowLayoutPanel5.Name = "flowLayoutPanel5";
            this.flowLayoutPanel5.Padding = new System.Windows.Forms.Padding(3);
            this.flowLayoutPanel5.Size = new System.Drawing.Size(780, 35);
            this.flowLayoutPanel5.TabIndex = 11;
            // 
            // rdoAddUser
            // 
            this.rdoAddUser.AutoSize = true;
            this.rdoAddUser.Checked = true;
            this.rdoAddUser.Location = new System.Drawing.Point(6, 6);
            this.rdoAddUser.Name = "rdoAddUser";
            this.rdoAddUser.Size = new System.Drawing.Size(93, 22);
            this.rdoAddUser.TabIndex = 0;
            this.rdoAddUser.TabStop = true;
            this.rdoAddUser.Text = "Thêm mới";
            this.rdoAddUser.UseVisualStyleBackColor = true;
            this.rdoAddUser.CheckedChanged += new System.EventHandler(this.rdoAddUser_CheckedChanged);
            // 
            // rdoEditUser
            // 
            this.rdoEditUser.AutoSize = true;
            this.rdoEditUser.Location = new System.Drawing.Point(105, 6);
            this.rdoEditUser.Name = "rdoEditUser";
            this.rdoEditUser.Size = new System.Drawing.Size(90, 22);
            this.rdoEditUser.TabIndex = 1;
            this.rdoEditUser.Text = "Chỉnh sửa";
            this.rdoEditUser.UseVisualStyleBackColor = true;
            // 
            // tbPhanQuyen
            // 
            this.tbPhanQuyen.Controls.Add(this.groupBox1);
            this.tbPhanQuyen.Controls.Add(this.panel1);
            this.tbPhanQuyen.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tbPhanQuyen.Location = new System.Drawing.Point(4, 25);
            this.tbPhanQuyen.Name = "tbPhanQuyen";
            this.tbPhanQuyen.Padding = new System.Windows.Forms.Padding(3);
            this.tbPhanQuyen.Size = new System.Drawing.Size(792, 333);
            this.tbPhanQuyen.TabIndex = 1;
            this.tbPhanQuyen.Text = "Phân quyền";
            this.tbPhanQuyen.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.flowLayoutPanel3);
            this.groupBox1.Controls.Add(this.grvQuyen);
            this.groupBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox1.Location = new System.Drawing.Point(282, 3);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(10);
            this.groupBox1.Size = new System.Drawing.Size(507, 327);
            this.groupBox1.TabIndex = 1;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Danh sách quyền";
            // 
            // flowLayoutPanel3
            // 
            this.flowLayoutPanel3.Controls.Add(this.btnLuu);
            this.flowLayoutPanel3.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.flowLayoutPanel3.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.flowLayoutPanel3.Location = new System.Drawing.Point(10, 272);
            this.flowLayoutPanel3.Name = "flowLayoutPanel3";
            this.flowLayoutPanel3.Size = new System.Drawing.Size(487, 45);
            this.flowLayoutPanel3.TabIndex = 2;
            // 
            // btnLuu
            // 
            this.btnLuu.Location = new System.Drawing.Point(401, 3);
            this.btnLuu.Name = "btnLuu";
            this.btnLuu.Size = new System.Drawing.Size(83, 37);
            this.btnLuu.TabIndex = 0;
            this.btnLuu.Text = "Lưu";
            this.btnLuu.UseVisualStyleBackColor = true;
            this.btnLuu.Click += new System.EventHandler(this.btnLuu_Click);
            // 
            // grvQuyen
            // 
            this.grvQuyen.AllowUserToAddRows = false;
            this.grvQuyen.AllowUserToDeleteRows = false;
            this.grvQuyen.AllowUserToResizeColumns = false;
            this.grvQuyen.AllowUserToResizeRows = false;
            this.grvQuyen.BackgroundColor = System.Drawing.SystemColors.Control;
            this.grvQuyen.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.grvQuyen.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.grvQuyen.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.cb,
            this.permission_code,
            this.permission_name,
            this.permission_2});
            this.grvQuyen.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grvQuyen.Location = new System.Drawing.Point(10, 29);
            this.grvQuyen.Name = "grvQuyen";
            this.grvQuyen.RowHeadersVisible = false;
            this.grvQuyen.RowTemplate.Height = 30;
            this.grvQuyen.Size = new System.Drawing.Size(487, 288);
            this.grvQuyen.TabIndex = 0;
            this.grvQuyen.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.grvQuyen_CellContentClick);
            // 
            // cb
            // 
            this.cb.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.cb.HeaderText = "#";
            this.cb.Name = "cb";
            this.cb.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.cb.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.cb.Width = 44;
            // 
            // permission_code
            // 
            this.permission_code.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.permission_code.HeaderText = "Quyền";
            this.permission_code.Name = "permission_code";
            this.permission_code.ReadOnly = true;
            this.permission_code.Width = 76;
            // 
            // permission_name
            // 
            this.permission_name.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.permission_name.HeaderText = "Chi tiết";
            this.permission_name.Name = "permission_name";
            this.permission_name.ReadOnly = true;
            // 
            // permission_2
            // 
            this.permission_2.HeaderText = "Column1";
            this.permission_2.Name = "permission_2";
            this.permission_2.Visible = false;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.tvDanhSach);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Left;
            this.panel1.Location = new System.Drawing.Point(3, 3);
            this.panel1.Name = "panel1";
            this.panel1.Padding = new System.Windows.Forms.Padding(10);
            this.panel1.Size = new System.Drawing.Size(279, 327);
            this.panel1.TabIndex = 0;
            // 
            // tvDanhSach
            // 
            this.tvDanhSach.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.tvDanhSach.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tvDanhSach.Location = new System.Drawing.Point(10, 10);
            this.tvDanhSach.Name = "tvDanhSach";
            this.tvDanhSach.Size = new System.Drawing.Size(259, 307);
            this.tvDanhSach.TabIndex = 0;
            this.tvDanhSach.NodeMouseClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.tvDanhSach_NodeMouseClick);
            // 
            // FmDangKy
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 362);
            this.Controls.Add(this.tabControl1);
            this.Name = "FmDangKy";
            this.Text = "Quản Lý Nhân Sự";
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tableLayoutPanel5.ResumeLayout(false);
            this.tableLayoutPanel5.PerformLayout();
            this.flowLayoutPanel2.ResumeLayout(false);
            this.flowLayoutPanel1.ResumeLayout(false);
            this.flowLayoutPanel1.PerformLayout();
            this.flowLayoutPanel5.ResumeLayout(false);
            this.flowLayoutPanel5.PerformLayout();
            this.tbPhanQuyen.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.flowLayoutPanel3.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.grvQuyen)).EndInit();
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel5;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.TextBox txtPassword;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.RadioButton rdoActive;
        private System.Windows.Forms.RadioButton rdoDisActive;
        private System.Windows.Forms.CheckedListBox clbDanhSachNhom;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel2;
        private System.Windows.Forms.Button btnDangKi;
        private System.Windows.Forms.Button tbnXoa;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox tbName;
        private System.Windows.Forms.ComboBox userName;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel5;
        private System.Windows.Forms.RadioButton rdoAddUser;
        private System.Windows.Forms.RadioButton rdoEditUser;
        private System.Windows.Forms.TabPage tbPhanQuyen;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.TreeView tvDanhSach;
        private System.Windows.Forms.DataGridView grvQuyen;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel3;
        private System.Windows.Forms.Button btnLuu;
        private System.Windows.Forms.DataGridViewCheckBoxColumn cb;
        private System.Windows.Forms.DataGridViewTextBoxColumn permission_code;
        private System.Windows.Forms.DataGridViewTextBoxColumn permission_name;
        private System.Windows.Forms.DataGridViewTextBoxColumn permission_2;
    }
}