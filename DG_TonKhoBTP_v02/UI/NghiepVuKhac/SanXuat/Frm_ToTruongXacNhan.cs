using DG_TonKhoBTP_v02.Database;
using DG_TonKhoBTP_v02.UI.Helper;
using System;
using System.Data;
using System.Data.SQLite;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.UI.NghiepVuKhac.SanXuat
{
    public partial class Frm_ToTruongXacNhan : Form
    {
        private bool _isChecking;

        /// <summary>
        /// Username lấy được sau khi xác nhận Code thành công.
        /// Chỉ sử dụng khi ShowDialog trả về DialogResult.OK.
        /// </summary>
        public string ConfirmedUsername { get; private set; }

        public Frm_ToTruongXacNhan()
        {
            InitializeComponent();

            tbToTruongXacNhan.KeyDown += tbToTruongXacNhan_KeyDown;
            Shown += Frm_ToTruongXacNhan_Shown;
            FormClosing += Frm_ToTruongXacNhan_FormClosing;
        }

        private void Frm_ToTruongXacNhan_Shown(object sender, EventArgs e)
        {
            tbToTruongXacNhan.Focus();
        }

        private void Frm_ToTruongXacNhan_FormClosing(
            object sender,
            FormClosingEventArgs e)
        {
            // Không cho đóng form trong lúc truy vấn đang thực hiện.
            if (_isChecking)
                e.Cancel = true;
        }

        private async void tbToTruongXacNhan_KeyDown(
            object sender,
            KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter)
                return;

            e.Handled = true;
            e.SuppressKeyPress = true;

            // Chống Enter liên tiếp tạo nhiều truy vấn song song.
            if (_isChecking)
                return;

            _isChecking = true;
            tbToTruongXacNhan.Enabled = false;

            string code = tbToTruongXacNhan.Text;
            CodeCheckResult checkResult;

            try
            {
                checkResult = await WaitingHelper.RunWithWaiting(
                    () => Task.Run(() => GetUsernameByCode(code)),
                    "ĐANG KIỂM TRA MÃ ...");
            }
            catch
            {
                checkResult = CodeCheckResult.DatabaseError();
            }

            if (checkResult.Status == CodeCheckStatus.Valid)
            {
                ConfirmedUsername = checkResult.Username;
                CloseAfterCheck(DialogResult.OK);
                return;
            }

            if (checkResult.Status == CodeCheckStatus.Invalid)
            {
                FrmWaiting.ShowGifAlert("Mã không hợp lệ.");
            }
            else
            {
                FrmWaiting.ShowGifAlert(
                    "Không thể kiểm tra mã do lỗi dữ liệu.\n" +
                    "Vui lòng thử lại hoặc liên hệ người quản lý.",
                    "LỖI");
            }

            // Mã sai hoặc lỗi database đều đóng form và không cho update.
            CloseAfterCheck(DialogResult.Cancel);
        }

        private void CloseAfterCheck(DialogResult result)
        {
            // Mở khóa cờ trước khi đặt DialogResult/Close để FormClosing
            // không hủy thao tác đóng nội bộ.
            _isChecking = false;
            DialogResult = result;
            Close();
        }

        /// <summary>
        /// Tìm dòng đầu tiên trong bảng users có Code khớp chính xác.
        /// So sánh phân biệt chữ hoa/thường và không Trim dữ liệu.
        /// </summary>
        private static CodeCheckResult GetUsernameByCode(string code)
        {
            try
            {
                const string query = @"
                    SELECT ""username""
                    FROM ""users""
                    WHERE ""Code"" COLLATE BINARY = @Code
                    ORDER BY ""user_id"" ASC
                    LIMIT 1;";

                using (SQLiteConnection conn = DB_Base.OpenConnection())
                using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.Add("@Code", DbType.String).Value =
                        code ?? string.Empty;

                    object result = cmd.ExecuteScalar();

                    if (result == null || result == DBNull.Value)
                        return CodeCheckResult.Invalid();

                    string username = Convert.ToString(result);

                    return username == null
                        ? CodeCheckResult.Invalid()
                        : CodeCheckResult.Valid(username);
                }
            }
            catch
            {
                return CodeCheckResult.DatabaseError();
            }
        }

        private enum CodeCheckStatus
        {
            Valid,
            Invalid,
            DatabaseError
        }

        private sealed class CodeCheckResult
        {
            public CodeCheckStatus Status { get; private set; }
            public string Username { get; private set; }

            public static CodeCheckResult Valid(string username)
            {
                return new CodeCheckResult
                {
                    Status = CodeCheckStatus.Valid,
                    Username = username
                };
            }

            public static CodeCheckResult Invalid()
            {
                return new CodeCheckResult
                {
                    Status = CodeCheckStatus.Invalid
                };
            }

            public static CodeCheckResult DatabaseError()
            {
                return new CodeCheckResult
                {
                    Status = CodeCheckStatus.DatabaseError
                };
            }
        }
    }
}
