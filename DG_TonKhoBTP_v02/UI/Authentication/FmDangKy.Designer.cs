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
            this.user = new System.Windows.Forms.TabPage();
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
            this.roles = new System.Windows.Forms.TabPage();
            this.panel1 = new System.Windows.Forms.Panel();
            this.grvRoles = new System.Windows.Forms.DataGridView();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.txtRoleName = new System.Windows.Forms.TextBox();
            this.txtRoleDescription = new System.Windows.Forms.RichTextBox();
            this.flowLayoutPanel3 = new System.Windows.Forms.FlowLayoutPanel();
            this.btnXoaRole = new System.Windows.Forms.Button();
            this.btnSuaRole = new System.Windows.Forms.Button();
            this.btnThemRole = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.permissions = new System.Windows.Forms.TabPage();
            this.panel2 = new System.Windows.Forms.Panel();
            this.grvPermissions = new System.Windows.Forms.DataGridView();
            this.tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
            this.flowLayoutPanel4 = new System.Windows.Forms.FlowLayoutPanel();
            this.btnXoaPermission = new System.Windows.Forms.Button();
            this.btnSuaPermission = new System.Windows.Forms.Button();
            this.btnThemPermission = new System.Windows.Forms.Button();
            this.txtPermissionCode = new System.Windows.Forms.TextBox();
            this.txtPermissionName = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.phanQuyen = new System.Windows.Forms.TabPage();
            this.grvPhanQuyen = new System.Windows.Forms.DataGridView();
            this.flowLayoutPanel6 = new System.Windows.Forms.FlowLayoutPanel();
            this.btnLuuPhanQuyen = new System.Windows.Forms.Button();
            this.btnTaiLaiPhanQuyen = new System.Windows.Forms.Button();
            this.label12 = new System.Windows.Forms.Label();
            this.tabControl1.SuspendLayout();
            this.user.SuspendLayout();
            this.tableLayoutPanel5.SuspendLayout();
            this.flowLayoutPanel2.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.flowLayoutPanel5.SuspendLayout();
            this.roles.SuspendLayout();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grvRoles)).BeginInit();
            this.tableLayoutPanel2.SuspendLayout();
            this.flowLayoutPanel3.SuspendLayout();
            this.permissions.SuspendLayout();
            this.panel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grvPermissions)).BeginInit();
            this.tableLayoutPanel3.SuspendLayout();
            this.flowLayoutPanel4.SuspendLayout();
            this.phanQuyen.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grvPhanQuyen)).BeginInit();
            this.flowLayoutPanel6.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.user);
            this.tabControl1.Controls.Add(this.roles);
            this.tabControl1.Controls.Add(this.permissions);
            this.tabControl1.Controls.Add(this.phanQuyen);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Font = new System.Drawing.Font("Tahoma", 9.75F);
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(800, 483);
            this.tabControl1.TabIndex = 0;
            // 
            // user
            // 
            this.user.Controls.Add(this.tableLayoutPanel5);
            this.user.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.user.Location = new System.Drawing.Point(4, 25);
            this.user.Name = "user";
            this.user.Padding = new System.Windows.Forms.Padding(3, 10, 3, 3);
            this.user.Size = new System.Drawing.Size(792, 454);
            this.user.TabIndex = 0;
            this.user.Text = "Người dùng";
            this.user.UseVisualStyleBackColor = true;
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
            // roles
            // 
            this.roles.Controls.Add(this.panel1);
            this.roles.Controls.Add(this.tableLayoutPanel2);
            this.roles.Controls.Add(this.label2);
            this.roles.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.roles.Location = new System.Drawing.Point(4, 25);
            this.roles.Name = "roles";
            this.roles.Padding = new System.Windows.Forms.Padding(3);
            this.roles.Size = new System.Drawing.Size(792, 454);
            this.roles.TabIndex = 2;
            this.roles.Text = "Nhóm";
            this.roles.UseVisualStyleBackColor = true;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.grvRoles);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(3, 170);
            this.panel1.Name = "panel1";
            this.panel1.Padding = new System.Windows.Forms.Padding(5);
            this.panel1.Size = new System.Drawing.Size(786, 281);
            this.panel1.TabIndex = 3;
            // 
            // grvRoles
            // 
            this.grvRoles.AllowUserToAddRows = false;
            this.grvRoles.AllowUserToDeleteRows = false;
            this.grvRoles.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.grvRoles.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.grvRoles.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grvRoles.Location = new System.Drawing.Point(5, 5);
            this.grvRoles.MultiSelect = false;
            this.grvRoles.Name = "grvRoles";
            this.grvRoles.ReadOnly = true;
            this.grvRoles.RowHeadersVisible = false;
            this.grvRoles.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.grvRoles.Size = new System.Drawing.Size(776, 271);
            this.grvRoles.TabIndex = 2;
            this.grvRoles.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.grvRoles_CellClick);
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 2;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 18.82952F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 81.17048F));
            this.tableLayoutPanel2.Controls.Add(this.label3, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.label4, 0, 1);
            this.tableLayoutPanel2.Controls.Add(this.txtRoleName, 1, 0);
            this.tableLayoutPanel2.Controls.Add(this.txtRoleDescription, 1, 1);
            this.tableLayoutPanel2.Controls.Add(this.flowLayoutPanel3, 1, 2);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Top;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(3, 28);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 3;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 58F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(786, 142);
            this.tableLayoutPanel2.TabIndex = 1;
            // 
            // label3
            // 
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(3, 7);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(142, 18);
            this.label3.TabIndex = 0;
            this.label3.Text = "Tên nhóm";
            // 
            // label4
            // 
            this.label4.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(3, 49);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(142, 18);
            this.label4.TabIndex = 0;
            this.label4.Text = "Mô tả";
            // 
            // txtRoleName
            // 
            this.txtRoleName.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtRoleName.Location = new System.Drawing.Point(151, 3);
            this.txtRoleName.Name = "txtRoleName";
            this.txtRoleName.Size = new System.Drawing.Size(632, 26);
            this.txtRoleName.TabIndex = 1;
            // 
            // txtRoleDescription
            // 
            this.txtRoleDescription.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtRoleDescription.Location = new System.Drawing.Point(151, 35);
            this.txtRoleDescription.Name = "txtRoleDescription";
            this.txtRoleDescription.Size = new System.Drawing.Size(632, 46);
            this.txtRoleDescription.TabIndex = 2;
            this.txtRoleDescription.Text = "";
            // 
            // flowLayoutPanel3
            // 
            this.flowLayoutPanel3.Controls.Add(this.btnXoaRole);
            this.flowLayoutPanel3.Controls.Add(this.btnSuaRole);
            this.flowLayoutPanel3.Controls.Add(this.btnThemRole);
            this.flowLayoutPanel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel3.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.flowLayoutPanel3.Location = new System.Drawing.Point(151, 87);
            this.flowLayoutPanel3.Name = "flowLayoutPanel3";
            this.flowLayoutPanel3.Padding = new System.Windows.Forms.Padding(0, 5, 0, 0);
            this.flowLayoutPanel3.Size = new System.Drawing.Size(632, 52);
            this.flowLayoutPanel3.TabIndex = 3;
            // 
            // btnXoaRole
            // 
            this.btnXoaRole.Location = new System.Drawing.Point(531, 8);
            this.btnXoaRole.Name = "btnXoaRole";
            this.btnXoaRole.Size = new System.Drawing.Size(98, 38);
            this.btnXoaRole.TabIndex = 0;
            this.btnXoaRole.Text = "Xoá";
            this.btnXoaRole.UseVisualStyleBackColor = true;
            this.btnXoaRole.Click += new System.EventHandler(this.btnXoaRole_Click);
            // 
            // btnSuaRole
            // 
            this.btnSuaRole.Location = new System.Drawing.Point(427, 8);
            this.btnSuaRole.Name = "btnSuaRole";
            this.btnSuaRole.Size = new System.Drawing.Size(98, 38);
            this.btnSuaRole.TabIndex = 1;
            this.btnSuaRole.Text = "Sửa";
            this.btnSuaRole.UseVisualStyleBackColor = true;
            this.btnSuaRole.Click += new System.EventHandler(this.btnSuaRole_Click);
            // 
            // btnThemRole
            // 
            this.btnThemRole.Location = new System.Drawing.Point(323, 8);
            this.btnThemRole.Name = "btnThemRole";
            this.btnThemRole.Size = new System.Drawing.Size(98, 38);
            this.btnThemRole.TabIndex = 2;
            this.btnThemRole.Text = "Thêm";
            this.btnThemRole.UseVisualStyleBackColor = true;
            this.btnThemRole.Click += new System.EventHandler(this.btnThemRole_Click);
            // 
            // label2
            // 
            this.label2.Dock = System.Windows.Forms.DockStyle.Top;
            this.label2.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(3, 3);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(786, 25);
            this.label2.TabIndex = 0;
            this.label2.Text = "DANH SÁCH NHÓM";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // permissions
            // 
            this.permissions.Controls.Add(this.panel2);
            this.permissions.Controls.Add(this.tableLayoutPanel3);
            this.permissions.Controls.Add(this.label6);
            this.permissions.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.permissions.Location = new System.Drawing.Point(4, 25);
            this.permissions.Name = "permissions";
            this.permissions.Padding = new System.Windows.Forms.Padding(3);
            this.permissions.Size = new System.Drawing.Size(792, 454);
            this.permissions.TabIndex = 3;
            this.permissions.Text = "QL chức năng";
            this.permissions.UseVisualStyleBackColor = true;
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.grvPermissions);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(3, 170);
            this.panel2.Name = "panel2";
            this.panel2.Padding = new System.Windows.Forms.Padding(5);
            this.panel2.Size = new System.Drawing.Size(786, 281);
            this.panel2.TabIndex = 4;
            // 
            // grvPermissions
            // 
            this.grvPermissions.AllowUserToAddRows = false;
            this.grvPermissions.AllowUserToDeleteRows = false;
            this.grvPermissions.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.grvPermissions.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.grvPermissions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grvPermissions.Location = new System.Drawing.Point(5, 5);
            this.grvPermissions.MultiSelect = false;
            this.grvPermissions.Name = "grvPermissions";
            this.grvPermissions.ReadOnly = true;
            this.grvPermissions.RowHeadersVisible = false;
            this.grvPermissions.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.grvPermissions.Size = new System.Drawing.Size(776, 271);
            this.grvPermissions.TabIndex = 3;
            this.grvPermissions.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.grvPermissions_CellClick);
            // 
            // tableLayoutPanel3
            // 
            this.tableLayoutPanel3.ColumnCount = 2;
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 18.82952F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 81.17048F));
            this.tableLayoutPanel3.Controls.Add(this.flowLayoutPanel4, 1, 2);
            this.tableLayoutPanel3.Controls.Add(this.txtPermissionCode, 1, 1);
            this.tableLayoutPanel3.Controls.Add(this.txtPermissionName, 1, 0);
            this.tableLayoutPanel3.Controls.Add(this.label7, 0, 1);
            this.tableLayoutPanel3.Controls.Add(this.label8, 0, 0);
            this.tableLayoutPanel3.Dock = System.Windows.Forms.DockStyle.Top;
            this.tableLayoutPanel3.Location = new System.Drawing.Point(3, 28);
            this.tableLayoutPanel3.Name = "tableLayoutPanel3";
            this.tableLayoutPanel3.RowCount = 3;
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 67F));
            this.tableLayoutPanel3.Size = new System.Drawing.Size(786, 142);
            this.tableLayoutPanel3.TabIndex = 2;
            // 
            // flowLayoutPanel4
            // 
            this.flowLayoutPanel4.Controls.Add(this.btnXoaPermission);
            this.flowLayoutPanel4.Controls.Add(this.btnSuaPermission);
            this.flowLayoutPanel4.Controls.Add(this.btnThemPermission);
            this.flowLayoutPanel4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel4.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.flowLayoutPanel4.Location = new System.Drawing.Point(151, 77);
            this.flowLayoutPanel4.Name = "flowLayoutPanel4";
            this.flowLayoutPanel4.Padding = new System.Windows.Forms.Padding(0, 5, 0, 0);
            this.flowLayoutPanel4.Size = new System.Drawing.Size(632, 62);
            this.flowLayoutPanel4.TabIndex = 3;
            // 
            // btnXoaPermission
            // 
            this.btnXoaPermission.Location = new System.Drawing.Point(531, 8);
            this.btnXoaPermission.Name = "btnXoaPermission";
            this.btnXoaPermission.Size = new System.Drawing.Size(98, 38);
            this.btnXoaPermission.TabIndex = 0;
            this.btnXoaPermission.Text = "Xoá";
            this.btnXoaPermission.UseVisualStyleBackColor = true;
            this.btnXoaPermission.Click += new System.EventHandler(this.btnXoaPermission_Click);
            // 
            // btnSuaPermission
            // 
            this.btnSuaPermission.Location = new System.Drawing.Point(427, 8);
            this.btnSuaPermission.Name = "btnSuaPermission";
            this.btnSuaPermission.Size = new System.Drawing.Size(98, 38);
            this.btnSuaPermission.TabIndex = 1;
            this.btnSuaPermission.Text = "Sửa";
            this.btnSuaPermission.UseVisualStyleBackColor = true;
            this.btnSuaPermission.Click += new System.EventHandler(this.btnSuaPermission_Click);
            // 
            // btnThemPermission
            // 
            this.btnThemPermission.Location = new System.Drawing.Point(323, 8);
            this.btnThemPermission.Name = "btnThemPermission";
            this.btnThemPermission.Size = new System.Drawing.Size(98, 38);
            this.btnThemPermission.TabIndex = 2;
            this.btnThemPermission.Text = "Thêm";
            this.btnThemPermission.UseVisualStyleBackColor = true;
            this.btnThemPermission.Click += new System.EventHandler(this.btnThemPermission_Click);
            // 
            // txtPermissionCode
            // 
            this.txtPermissionCode.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtPermissionCode.Location = new System.Drawing.Point(151, 42);
            this.txtPermissionCode.Name = "txtPermissionCode";
            this.txtPermissionCode.Size = new System.Drawing.Size(632, 26);
            this.txtPermissionCode.TabIndex = 4;
            // 
            // txtPermissionName
            // 
            this.txtPermissionName.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtPermissionName.Location = new System.Drawing.Point(151, 5);
            this.txtPermissionName.Name = "txtPermissionName";
            this.txtPermissionName.Size = new System.Drawing.Size(632, 26);
            this.txtPermissionName.TabIndex = 1;
            this.txtPermissionName.TextChanged += new System.EventHandler(this.txtPermissionName_TextChanged);
            // 
            // label7
            // 
            this.label7.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(3, 46);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(142, 18);
            this.label7.TabIndex = 0;
            this.label7.Text = "Code chức năng";
            // 
            // label8
            // 
            this.label8.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(3, 9);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(142, 18);
            this.label8.TabIndex = 0;
            this.label8.Text = "Tên chức năng";
            // 
            // label6
            // 
            this.label6.Dock = System.Windows.Forms.DockStyle.Top;
            this.label6.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.Location = new System.Drawing.Point(3, 3);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(786, 25);
            this.label6.TabIndex = 1;
            this.label6.Text = "DANH SÁCH CHỨC NĂNG";
            this.label6.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // phanQuyen
            // 
            this.phanQuyen.Controls.Add(this.grvPhanQuyen);
            this.phanQuyen.Controls.Add(this.flowLayoutPanel6);
            this.phanQuyen.Controls.Add(this.label12);
            this.phanQuyen.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.phanQuyen.Location = new System.Drawing.Point(4, 25);
            this.phanQuyen.Name = "phanQuyen";
            this.phanQuyen.Padding = new System.Windows.Forms.Padding(7);
            this.phanQuyen.Size = new System.Drawing.Size(792, 454);
            this.phanQuyen.TabIndex = 4;
            this.phanQuyen.Text = "Phân quyền";
            this.phanQuyen.UseVisualStyleBackColor = true;
            // 
            // grvPhanQuyen
            // 
            this.grvPhanQuyen.AllowUserToAddRows = false;
            this.grvPhanQuyen.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.grvPhanQuyen.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grvPhanQuyen.Location = new System.Drawing.Point(7, 32);
            this.grvPhanQuyen.Name = "grvPhanQuyen";
            this.grvPhanQuyen.RowHeadersVisible = false;
            this.grvPhanQuyen.Size = new System.Drawing.Size(778, 353);
            this.grvPhanQuyen.TabIndex = 5;
            // 
            // flowLayoutPanel6
            // 
            this.flowLayoutPanel6.Controls.Add(this.btnLuuPhanQuyen);
            this.flowLayoutPanel6.Controls.Add(this.btnTaiLaiPhanQuyen);
            this.flowLayoutPanel6.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.flowLayoutPanel6.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.flowLayoutPanel6.Location = new System.Drawing.Point(7, 385);
            this.flowLayoutPanel6.Name = "flowLayoutPanel6";
            this.flowLayoutPanel6.Padding = new System.Windows.Forms.Padding(0, 5, 0, 0);
            this.flowLayoutPanel6.Size = new System.Drawing.Size(778, 62);
            this.flowLayoutPanel6.TabIndex = 4;
            // 
            // btnLuuPhanQuyen
            // 
            this.btnLuuPhanQuyen.Location = new System.Drawing.Point(677, 8);
            this.btnLuuPhanQuyen.Name = "btnLuuPhanQuyen";
            this.btnLuuPhanQuyen.Size = new System.Drawing.Size(98, 38);
            this.btnLuuPhanQuyen.TabIndex = 0;
            this.btnLuuPhanQuyen.Text = "Lưu";
            this.btnLuuPhanQuyen.UseVisualStyleBackColor = true;
            this.btnLuuPhanQuyen.Click += new System.EventHandler(this.btnLuuPhanQuyen_Click);
            // 
            // btnTaiLaiPhanQuyen
            // 
            this.btnTaiLaiPhanQuyen.Location = new System.Drawing.Point(573, 8);
            this.btnTaiLaiPhanQuyen.Name = "btnTaiLaiPhanQuyen";
            this.btnTaiLaiPhanQuyen.Size = new System.Drawing.Size(98, 38);
            this.btnTaiLaiPhanQuyen.TabIndex = 1;
            this.btnTaiLaiPhanQuyen.Text = "Tải lại";
            this.btnTaiLaiPhanQuyen.UseVisualStyleBackColor = true;
            // 
            // label12
            // 
            this.label12.Dock = System.Windows.Forms.DockStyle.Top;
            this.label12.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label12.Location = new System.Drawing.Point(7, 7);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(778, 25);
            this.label12.TabIndex = 2;
            this.label12.Text = "BẢNG PHÂN QUYỀN";
            this.label12.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // FmDangKy
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 483);
            this.Controls.Add(this.tabControl1);
            this.Name = "FmDangKy";
            this.Text = "Quản Lý Nhân Sự";
            this.tabControl1.ResumeLayout(false);
            this.user.ResumeLayout(false);
            this.tableLayoutPanel5.ResumeLayout(false);
            this.tableLayoutPanel5.PerformLayout();
            this.flowLayoutPanel2.ResumeLayout(false);
            this.flowLayoutPanel1.ResumeLayout(false);
            this.flowLayoutPanel1.PerformLayout();
            this.flowLayoutPanel5.ResumeLayout(false);
            this.flowLayoutPanel5.PerformLayout();
            this.roles.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.grvRoles)).EndInit();
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            this.flowLayoutPanel3.ResumeLayout(false);
            this.permissions.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.grvPermissions)).EndInit();
            this.tableLayoutPanel3.ResumeLayout(false);
            this.tableLayoutPanel3.PerformLayout();
            this.flowLayoutPanel4.ResumeLayout(false);
            this.phanQuyen.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.grvPhanQuyen)).EndInit();
            this.flowLayoutPanel6.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage user;
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
        private System.Windows.Forms.TabPage roles;
        private System.Windows.Forms.TabPage permissions;
        private System.Windows.Forms.TabPage phanQuyen;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox txtRoleName;
        private System.Windows.Forms.RichTextBox txtRoleDescription;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.DataGridView grvRoles;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel3;
        private System.Windows.Forms.Button btnXoaRole;
        private System.Windows.Forms.Button btnSuaRole;
        private System.Windows.Forms.Button btnThemRole;
        private System.Windows.Forms.DataGridView grvPermissions;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel3;
        private System.Windows.Forms.TextBox txtPermissionCode;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox txtPermissionName;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel4;
        private System.Windows.Forms.Button btnXoaPermission;
        private System.Windows.Forms.Button btnSuaPermission;
        private System.Windows.Forms.Button btnThemPermission;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.DataGridView grvPhanQuyen;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel6;
        private System.Windows.Forms.Button btnLuuPhanQuyen;
        private System.Windows.Forms.Button btnTaiLaiPhanQuyen;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel2;
    }
}