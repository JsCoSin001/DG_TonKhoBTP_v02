namespace DG_TonKhoBTP_v02.UI.NghiepVuKhac.KeToan
{
    partial class UC_KiemTraDuLieu
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
            this.label10 = new System.Windows.Forms.Label();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.btnLayDS = new System.Windows.Forms.Button();
            this.grvBangSoSanhBom = new System.Windows.Forms.DataGridView();
            this.id_KhacBietBom = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ttthanhpham_id = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.congDoanTP = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.lotTP = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.maTP = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.tenTP = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.congDoanTTe = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.lotNVL = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.maNVL = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.tenNVL = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.confirm = new System.Windows.Forms.DataGridViewButtonColumn();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grvBangSoSanhBom)).BeginInit();
            this.SuspendLayout();
            // 
            // label10
            // 
            this.label10.Dock = System.Windows.Forms.DockStyle.Top;
            this.label10.Font = new System.Drawing.Font("Tahoma", 15F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label10.Location = new System.Drawing.Point(0, 0);
            this.label10.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(1009, 49);
            this.label10.TabIndex = 2;
            this.label10.Text = "BẢNG SO SÁNH VỚI BOM";
            this.label10.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Controls.Add(this.btnLayDS, 1, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 655);
            this.tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(1009, 86);
            this.tableLayoutPanel1.TabIndex = 4;
            // 
            // btnLayDS
            // 
            this.btnLayDS.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnLayDS.Location = new System.Drawing.Point(508, 4);
            this.btnLayDS.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnLayDS.Name = "btnLayDS";
            this.btnLayDS.Size = new System.Drawing.Size(141, 57);
            this.btnLayDS.TabIndex = 0;
            this.btnLayDS.Text = "Lấy dữ liệu";
            this.btnLayDS.UseVisualStyleBackColor = true;
            this.btnLayDS.Click += new System.EventHandler(this.btnLayDS_Click);
            // 
            // grvBangSoSanhBom
            // 
            this.grvBangSoSanhBom.AllowUserToAddRows = false;
            this.grvBangSoSanhBom.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.grvBangSoSanhBom.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.id_KhacBietBom,
            this.ttthanhpham_id,
            this.congDoanTP,
            this.lotTP,
            this.maTP,
            this.tenTP,
            this.congDoanTTe,
            this.lotNVL,
            this.maNVL,
            this.tenNVL,
            this.confirm});
            this.grvBangSoSanhBom.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grvBangSoSanhBom.Location = new System.Drawing.Point(0, 49);
            this.grvBangSoSanhBom.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.grvBangSoSanhBom.Name = "grvBangSoSanhBom";
            this.grvBangSoSanhBom.Size = new System.Drawing.Size(1009, 606);
            this.grvBangSoSanhBom.TabIndex = 5;
            // 
            // id_KhacBietBom
            // 
            this.id_KhacBietBom.DataPropertyName = "id_KhacBietBom";
            this.id_KhacBietBom.HeaderText = "id_KhacBietBom";
            this.id_KhacBietBom.Name = "id_KhacBietBom";
            // 
            // ttthanhpham_id
            // 
            this.ttthanhpham_id.DataPropertyName = "ttthanhpham_id";
            this.ttthanhpham_id.HeaderText = "STT";
            this.ttthanhpham_id.Name = "ttthanhpham_id";
            // 
            // congDoanTP
            // 
            this.congDoanTP.DataPropertyName = "congDoanTP";
            this.congDoanTP.HeaderText = "Công Đoạn TP";
            this.congDoanTP.Name = "congDoanTP";
            this.congDoanTP.Width = 150;
            // 
            // lotTP
            // 
            this.lotTP.DataPropertyName = "lotTP";
            this.lotTP.HeaderText = "LOT Thảnh phẩm";
            this.lotTP.Name = "lotTP";
            this.lotTP.Width = 150;
            // 
            // maTP
            // 
            this.maTP.DataPropertyName = "maTP";
            this.maTP.HeaderText = "Mã Thành phẩm";
            this.maTP.Name = "maTP";
            this.maTP.Width = 200;
            // 
            // tenTP
            // 
            this.tenTP.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.tenTP.DataPropertyName = "tenTP";
            this.tenTP.HeaderText = "Tên Thành phẩm";
            this.tenTP.Name = "tenTP";
            // 
            // congDoanTTe
            // 
            this.congDoanTTe.DataPropertyName = "congDoanTTe";
            this.congDoanTTe.HeaderText = "Công Đoạn T.Tế";
            this.congDoanTTe.Name = "congDoanTTe";
            this.congDoanTTe.Visible = false;
            this.congDoanTTe.Width = 150;
            // 
            // lotNVL
            // 
            this.lotNVL.DataPropertyName = "lotNVL";
            this.lotNVL.HeaderText = "LOT Nguyên liệu";
            this.lotNVL.Name = "lotNVL";
            this.lotNVL.Width = 150;
            // 
            // maNVL
            // 
            this.maNVL.DataPropertyName = "maNVL";
            this.maNVL.HeaderText = "Mã Nguyên liệu";
            this.maNVL.Name = "maNVL";
            this.maNVL.Width = 200;
            // 
            // tenNVL
            // 
            this.tenNVL.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.tenNVL.DataPropertyName = "tenNVL";
            this.tenNVL.HeaderText = "Tên Nguyên liệu";
            this.tenNVL.Name = "tenNVL";
            // 
            // confirm
            // 
            this.confirm.DataPropertyName = "confirm";
            this.confirm.HeaderText = "";
            this.confirm.Name = "confirm";
            this.confirm.Width = 70;
            // 
            // UC_KiemTraDuLieu
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.grvBangSoSanhBom);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Controls.Add(this.label10);
            this.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Name = "UC_KiemTraDuLieu";
            this.Size = new System.Drawing.Size(1009, 741);
            this.tableLayoutPanel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.grvBangSoSanhBom)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Button btnLayDS;
        private System.Windows.Forms.DataGridView grvBangSoSanhBom;
        private System.Windows.Forms.DataGridViewTextBoxColumn id_KhacBietBom;
        private System.Windows.Forms.DataGridViewTextBoxColumn ttthanhpham_id;
        private System.Windows.Forms.DataGridViewTextBoxColumn congDoanTP;
        private System.Windows.Forms.DataGridViewTextBoxColumn lotTP;
        private System.Windows.Forms.DataGridViewTextBoxColumn maTP;
        private System.Windows.Forms.DataGridViewTextBoxColumn tenTP;
        private System.Windows.Forms.DataGridViewTextBoxColumn congDoanTTe;
        private System.Windows.Forms.DataGridViewTextBoxColumn lotNVL;
        private System.Windows.Forms.DataGridViewTextBoxColumn maNVL;
        private System.Windows.Forms.DataGridViewTextBoxColumn tenNVL;
        private System.Windows.Forms.DataGridViewButtonColumn confirm;
    }
}
