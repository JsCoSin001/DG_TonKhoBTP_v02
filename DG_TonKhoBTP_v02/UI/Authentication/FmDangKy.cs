using BCrypt.Net;
using DG_TonKhoBTP_v02.Database;
using DG_TonKhoBTP_v02.Models;
using DG_TonKhoBTP_v02.UI.Helper;
using CoreHelper = DG_TonKhoBTP_v02.Helper.Helper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.UI.Setting
{
    public partial class FmDangKy : Form
    {
        readonly System.Windows.Forms.Timer _t = new System.Windows.Forms.Timer() { Interval = 200 };
        CancellationTokenSource _cts = null;
        bool _updating = false;

        public FmDangKy()
        {
            InitializeComponent();
            //FirstLoad();

            this.Load += FmDangKy_Load;

            _t.Tick += async (_, __) =>
            {
                if (rdoAddUser.Checked) { _t.Stop(); return; }
                _t.Stop();
                await LoadUsernamesAsync(userName.Text);
            };

            userName.TextUpdate += (_, __) =>
            {
                if (_updating) return;
                if (rdoAddUser.Checked) { _t.Stop(); userName.DroppedDown = false; return; }

                _t.Stop();
                _t.Start();
            };

            userName.SelectionChangeCommitted += async (_, __) =>
            {
                if (rdoAddUser.Checked) return;

                // Kiểm tra có item được chọn không
                if (userName.SelectedItem == null) return;

                // Lấy giá trị được chọn
                string selectedUsername = userName.SelectedItem.ToString();

                // Hoặc nếu bạn bind object vào ComboBox
                // string selectedUsername = userName.SelectedValue?.ToString();

                var u = await DatabaseHelper.GetUserWithRolesByUsernameAsync(selectedUsername, CancellationToken.None);

                if (u == null) return;

                tbName.Text = u.Name;
                rdoActive.Checked = u.IsActive;
                rdoDisActive.Checked = !u.IsActive;

                ApplyCheckedRolesByName(u.Roles);
            };
        }

        private async void FmDangKy_Load(object sender, EventArgs e)
        {
            await WaitingHelper.RunWithWaiting(async () =>
            {
                await FirstLoadAsync();
            }, "ĐANG TẢI DỮ LIỆU...");
        }

        private async Task FirstLoadAsync()
        {
            // Chạy query database trên background thread
            DataTable dsNhom = await Task.Run(() =>
            {
                string query = "SELECT role_id, role_name, description FROM Roles";
                return DatabaseHelper.GetData(query);
            });

            // Phần dưới đây tự động chạy trên UI thread
            // (vì await trong async method tự động quay về context gốc)

            if (dsNhom == null || dsNhom.Rows.Count == 0)
            {
                MessageBox.Show("Không có nhóm/quyền nào trong hệ thống. Vui lòng thêm nhóm/quyền trước.");
                return;
            }

            clbDanhSachNhom.DataSource = dsNhom;
            clbDanhSachNhom.DisplayMember = "role_name";
            clbDanhSachNhom.ValueMember = "role_id";
            clbDanhSachNhom.CheckOnClick = true;
        }

        private void FirstLoad()
        {
            string query = "SELECT role_id, role_name, description FROM Roles";

            DataTable dsNhom = DatabaseHelper.GetData(query);

            // check if data table is not null and has rows
            if (dsNhom == null || dsNhom.Rows.Count == 0)
            {
                MessageBox.Show("Không có nhóm/quyền nào trong hệ thống. Vui lòng thêm nhóm/quyền trước.");
                return;
            }
            clbDanhSachNhom.DataSource = dsNhom;
            clbDanhSachNhom.DisplayMember = "role_name";
            clbDanhSachNhom.ValueMember = "role_id";
            clbDanhSachNhom.CheckOnClick = true;
        }

        async Task LoadUsernamesAsync(string typed)
        {
            if (_cts != null) _cts.Cancel();
            _cts = new CancellationTokenSource();
            var ct = _cts.Token;

            var items = await DatabaseHelper.QueryAsync(typed ?? "", ct);
            if (ct.IsCancellationRequested) return;

            var text = userName.Text;
            var caret = userName.SelectionStart;

            _updating = true;
            userName.BeginUpdate();
            userName.Items.Clear();
            userName.Items.AddRange(items.ToArray());
            userName.EndUpdate();
            _updating = false;

            userName.DroppedDown = items.Count > 0;
            userName.Text = text;
            userName.SelectionStart = caret;
            userName.SelectionLength = 0;
        }

        private List<int> GetCheckedRoleIds(CheckedListBox clbDanhSachNhom)
        {
            List<int> roleIds = new List<int>();

            foreach (var item in clbDanhSachNhom.CheckedItems)
            {
                if (item is DataRowView drv)
                {
                    roleIds.Add(Convert.ToInt32(drv["role_id"]));
                }
            }

            return roleIds;
        }

        private void ApplyCheckedRolesByName(IEnumerable<string> roleNames)
        {
            var set = new HashSet<string>(roleNames ?? Enumerable.Empty<string>(),
                                          StringComparer.OrdinalIgnoreCase);

            clbDanhSachNhom.BeginUpdate();
            try
            {
                for (int i = 0; i < clbDanhSachNhom.Items.Count; i++)
                {
                    var item = clbDanhSachNhom.Items[i];

                    // Vì DataSource là DataTable => item thường là DataRowView
                    string roleName = item is DataRowView drv
                        ? drv["role_name"]?.ToString()
                        : clbDanhSachNhom.GetItemText(item);

                    clbDanhSachNhom.SetItemChecked(i, set.Contains(roleName));
                }
            }
            finally
            {
                clbDanhSachNhom.EndUpdate();
            }
        }


        private void btnDangKi_Click(object sender, EventArgs e)
        {
            btnDangKi.Enabled = false;

            try
            {
                string username = userName.Text.Trim();
                string password = txtPassword.Text;
                string name = tbName.Text;
                List<int> roleIds = GetCheckedRoleIds(clbDanhSachNhom);
                bool active = rdoActive.Checked;

                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(name) || roleIds.Count == 0)
                {
                    MessageBox.Show("Vui lòng nhập đầy đủ thông tin");
                    return;
                }

                if (rdoAddUser.Checked && string.IsNullOrEmpty(password))
                {
                    MessageBox.Show("Vui lòng nhập đầy đủ thông tin");
                    return;
                }

                string hash = string.IsNullOrWhiteSpace(password)
                ? null
                : BCrypt.Net.BCrypt.HashPassword(password);

                bool success = false;

                if (rdoAddUser.Checked)
                    success = DatabaseHelper.CreateUserWithRoles(username, hash, name, roleIds, active);
                else
                    success = DatabaseHelper.UpdateUserWithRoles(username, hash, name, roleIds, active);

                string mess = "Thao tác thất bại. Vui lòng thử lại.";
                string icon = EnumStore.Icon.Warning;
                if (success)
                {
                    mess = "Thao tác thành công!";
                    icon = EnumStore.Icon.Success;
                    this.Close();
                }

                FrmWaiting.ShowGifAlert(mess, "THÔNG BÁO", icon);
            }
            catch (Exception ex)
            {
                var mess = CoreHelper.ShowErrorDatabase(ex, "TÀI KHOẢN");
                FrmWaiting.ShowGifAlert(mess, "THÔNG BÁO", EnumStore.Icon.Warning);
            }
            finally
            {
                btnDangKi.Enabled = true;
            }


        }

        private void rdoAddUser_CheckedChanged(object sender, EventArgs e)
        {
            userName.Text = "";
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            string tabName = tabControl1.SelectedTab?.Name;
            if (tabName != "tbPhanQuyen") return;

            CoreHelper.LoadUsersWithSameRoles(tvDanhSach);
        }

        private int idRole = 0;
        private string roleName = "";
        private string fatherRoleName = "";
        private void tvDanhSach_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            idRole = 0;
            // Xử lý khi click vào node
            if (e.Node.Tag is RoleInfo role)
            {         
                DatabaseHelper.LoadQuyenTheoRole(role.RoleId, grvQuyen);
                this.idRole = role.RoleId;
                this.roleName = role.RoleName;
                this.fatherRoleName = e.Node.Parent?.Text ?? "";
                lblDoiTuongSetQuen.Text = $"Đang gán quyền cho {this.fatherRoleName} ở nhóm {role.RoleName} ";
            }
        }

        private async void btnLuu_Click(object sender, EventArgs e)
        {
            if (idRole == 0)
            {
                FrmWaiting.ShowGifAlert("Không tìm thấy đối tượng cần đặt quyền");
                return;
            }

            btnLuu.Enabled = false; // Disable button khi đang xử lý

            try
            {
                await WaitingHelper.RunWithWaiting(async () =>
                {
                    await Task.Run(() =>
                    {
                        DatabaseHelper.SaveRolePermissions_ByGrid(idRole, grvQuyen);
                    });
                }, "ĐANG LƯU QUYỀN...");

                // Thông báo thành công
                FrmWaiting.ShowGifAlert("Áp quyền thành công cho nhóm: " + roleName, "THÔNG BÁO", EnumStore.Icon.Success);
            }
            catch (Exception ex)
            {
                FrmWaiting.ShowGifAlert($"Lỗi khi lưu quyền: {ex.Message}", "THÔNG BÁO", EnumStore.Icon.Warning);
            }
            finally
            {
                btnLuu.Enabled = true; // Enable lại button
            }
        }

        private void grvQuyen_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != 0) return; // chỉ cột checkbox

            var code = grvQuyen.Rows[e.RowIndex].Cells[3].Value?.ToString(); // cột ẩn chứa permission_code
            if (string.Equals(code, "can_delete", StringComparison.OrdinalIgnoreCase))
            {
                // không cho chuyển sang true
                grvQuyen.EndEdit();
                grvQuyen.Rows[e.RowIndex].Cells[0].Value = false;
                FrmWaiting.ShowGifAlert("QUYỀN NÀY KHÔNG ĐƯỢC CẤP!");
            }
        }
    }
}
