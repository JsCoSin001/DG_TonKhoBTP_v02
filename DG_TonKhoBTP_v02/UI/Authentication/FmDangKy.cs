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
using DG_TonKhoBTP_v02.Database.User;

namespace DG_TonKhoBTP_v02.UI.Setting
{
    public partial class FmDangKy : Form
    {
        readonly System.Windows.Forms.Timer _t = new System.Windows.Forms.Timer() { Interval = 200 };
        CancellationTokenSource _cts = null;
        bool _updating = false;

        private async Task RunBusyAsync(Func<Task> action, string waitingText)
        {
            if (_updating) return;

            _updating = true;
            UseWaitCursor = true;

            try
            {
                await WaitingHelper.RunWithWaiting(async () =>
                {
                    await action();
                }, waitingText);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                UseWaitCursor = false;
                _updating = false;
            }
        }

        public FmDangKy()
        {
            InitializeComponent();
            this.Load += FmDangKy_Load;

            grvRoles.CellClick += grvRoles_CellClick;
            grvPermissions.CellClick += grvPermissions_CellClick;

            btnThemRole.Click += btnThemRole_Click;
            btnSuaRole.Click += btnSuaRole_Click;
            btnXoaRole.Click += btnXoaRole_Click;

            btnThemPermission.Click += btnThemPermission_Click;
            btnSuaPermission.Click += btnSuaPermission_Click;
            btnXoaPermission.Click += btnXoaPermission_Click;

            btnLuuPhanQuyen.Click += btnLuuPhanQuyen_Click;
        }

        private async void FmDangKy_Load(object sender, EventArgs e)
        {
            await WaitingHelper.RunWithWaiting(async () =>
            {
                await FirstLoadAsync();
            }, "ĐANG TẢI DỮ LIỆU...");

            LoadRoles();
            LoadPermissions();
            LoadPermissionMatrix();
        }

        private async Task FirstLoadAsync()
        {
            DataTable dsNhom = await Task.Run(() =>
            {
                return DatabaseHelper.GetData("SELECT role_id, role_name FROM roles");
            });

            clbDanhSachNhom.DataSource = dsNhom;
            clbDanhSachNhom.DisplayMember = "role_name";
            clbDanhSachNhom.ValueMember = "role_id";
            clbDanhSachNhom.CheckOnClick = true;
        }

        // ================= USER =================

        private List<int> GetCheckedRoleIds()
        {
            var list = new List<int>();
            foreach (DataRowView item in clbDanhSachNhom.CheckedItems)
                list.Add(Convert.ToInt32(item["role_id"]));
            return list;
        }

        private async void btnDangKi_Click(object sender, EventArgs e)
        {
            await RunBusyAsync(async () =>
            {
                string username = userName.Text.Trim();
                string password = txtPassword.Text;
                string name = tbName.Text;
                bool active = rdoActive.Checked;
                var roleIds = GetCheckedRoleIds();
                bool isAdd = rdoAddUser.Checked;

                bool success = await Task.Run(() =>
                {
                    string hash = string.IsNullOrWhiteSpace(password)
                        ? null
                        : BCrypt.Net.BCrypt.HashPassword(password);

                    return isAdd
                        ? DatabaseHelper.CreateUserWithRoles(username, hash, name, roleIds, active)
                        : DatabaseHelper.UpdateUserWithRoles(username, hash, name, roleIds, active);
                });

                MessageBox.Show(success ? "Thành công" : "Thất bại");

            }, "ĐANG XỬ LÝ ĐĂNG KÝ...");
        }
        // ================= ROLES =================

        void LoadRoles()
        {
            grvRoles.DataSource = PermissionDbHelper.GetData("SELECT * FROM roles");
            grvRoles.Columns["role_id"].Visible = false;
        }

        private void grvRoles_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var r = grvRoles.Rows[e.RowIndex];

            txtRoleName.Text = r.Cells["role_name"].Value?.ToString();
            txtRoleDescription.Text = r.Cells["description"].Value?.ToString();
        }

        private async void btnThemRole_Click(object sender, EventArgs e)
        {
            await RunBusyAsync(async () =>
            {
                string roleName = txtRoleName.Text;
                string description = txtRoleDescription.Text;

                await Task.Run(() =>
                {
                    PermissionDbHelper.Execute(
                        "INSERT INTO roles(role_name, description) VALUES(@n,@d)",
                        new SQLiteParameter("@n", roleName),
                        new SQLiteParameter("@d", description)
                    );
                });

                LoadRoles();
                LoadPermissionMatrix();
                ClearRoleInput();

            }, "ĐANG THÊM NHÓM...");
        }

        private async void btnSuaRole_Click(object sender, EventArgs e)
        {
            if (grvRoles.CurrentRow == null) return;

            await RunBusyAsync(async () =>
            {
                int id = Convert.ToInt32(grvRoles.CurrentRow.Cells["role_id"].Value);
                string roleName = txtRoleName.Text;
                string description = txtRoleDescription.Text;

                await Task.Run(() =>
                {
                    PermissionDbHelper.Execute(
                        "UPDATE roles SET role_name=@n, description=@d WHERE role_id=@id",
                        new SQLiteParameter("@n", roleName),
                        new SQLiteParameter("@d", description),
                        new SQLiteParameter("@id", id)
                    );
                });

                LoadRoles();
                LoadPermissionMatrix();
                ClearRoleInput();

            }, "ĐANG SỬA NHÓM...");
        }

        private async void btnXoaRole_Click(object sender, EventArgs e)
        {
            if (grvRoles.CurrentRow == null) return;

            await RunBusyAsync(async () =>
            {
                int id = Convert.ToInt32(grvRoles.CurrentRow.Cells["role_id"].Value);

                await Task.Run(() =>
                {
                    PermissionDbHelper.Execute(
                        "DELETE FROM roles WHERE role_id=@id",
                        new SQLiteParameter("@id", id)
                    );
                });

                LoadRoles();
                LoadPermissionMatrix();

            }, "ĐANG XÓA NHÓM...");
        }

        // ================= PERMISSIONS =================

        void LoadPermissions()
        {
            grvPermissions.DataSource = PermissionDbHelper.GetData("SELECT * FROM permissions");
            grvPermissions.Columns["permission_id"].Visible = false;
        }

        private void grvPermissions_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var r = grvPermissions.Rows[e.RowIndex];

            txtPermissionName.Text = r.Cells["permission_name"].Value?.ToString();
            txtPermissionCode.Text = r.Cells["permission_code"].Value?.ToString();
        }

        private async void btnThemPermission_Click(object sender, EventArgs e)
        {
            await RunBusyAsync(async () =>
            {
                string code = txtPermissionCode.Text.ToUpper();
                string name = txtPermissionName.Text;

                await Task.Run(() =>
                {
                    PermissionDbHelper.Execute(
                        "INSERT INTO permissions(permission_code, permission_name) VALUES(@c,@n)",
                        new SQLiteParameter("@c", code),
                        new SQLiteParameter("@n", name)
                    );
                });

                LoadPermissions();
                LoadPermissionMatrix();
                ClearPermissionInput();

            }, "ĐANG THÊM QUYỀN...");
        }

        private async void btnSuaPermission_Click(object sender, EventArgs e)
        {
            if (grvPermissions.CurrentRow == null) return;

            await RunBusyAsync(async () =>
            {
                int id = Convert.ToInt32(grvPermissions.CurrentRow.Cells["permission_id"].Value);
                string code = txtPermissionCode.Text;
                string name = txtPermissionName.Text;

                await Task.Run(() =>
                {
                    PermissionDbHelper.Execute(
                        "UPDATE permissions SET permission_code=@c, permission_name=@n WHERE permission_id=@id",
                        new SQLiteParameter("@c", code),
                        new SQLiteParameter("@n", name),
                        new SQLiteParameter("@id", id)
                    );
                });

                LoadPermissions();
                LoadPermissionMatrix();
                ClearPermissionInput();

            }, "ĐANG SỬA QUYỀN...");
        }

        private async void btnXoaPermission_Click(object sender, EventArgs e)
        {
            if (grvPermissions.CurrentRow == null) return;

            await RunBusyAsync(async () =>
            {
                int id = Convert.ToInt32(grvPermissions.CurrentRow.Cells["permission_id"].Value);

                await Task.Run(() =>
                {
                    PermissionDbHelper.Execute(
                        "DELETE FROM permissions WHERE permission_id=@id",
                        new SQLiteParameter("@id", id)
                    );
                });

                LoadPermissions();
                LoadPermissionMatrix();

            }, "ĐANG XÓA QUYỀN...");
        }

        // ================= MATRIX =================

        void LoadPermissionMatrix()
        {
            grvPhanQuyen.Columns.Clear();
            grvPhanQuyen.Rows.Clear();

            var roles = PermissionDbHelper.GetData("SELECT * FROM roles");
            var perms = PermissionDbHelper.GetData("SELECT * FROM permissions");

            grvPhanQuyen.Columns.Add("role_name", "Nhóm");

            foreach (DataRow p in perms.Rows)
            {
                var col = new DataGridViewCheckBoxColumn();
                col.Name = p["permission_id"].ToString();
                col.HeaderText = p["permission_name"].ToString();
                grvPhanQuyen.Columns.Add(col);
            }

            foreach (DataRow r in roles.Rows)
            {
                int idx = grvPhanQuyen.Rows.Add();
                var row = grvPhanQuyen.Rows[idx];

                row.Cells[0].Value = r["role_name"];
                row.Tag = r["role_id"];

                foreach (DataRow p in perms.Rows)
                {
                    bool has = Convert.ToInt32(PermissionDbHelper.ExecuteScalar(
                        "SELECT COUNT(*) FROM role_permissions WHERE role_id=@r AND permission_id=@p",
                        new SQLiteParameter("@r", r["role_id"]),
                        new SQLiteParameter("@p", p["permission_id"])
                    )) > 0;

                    row.Cells[p["permission_id"].ToString()].Value = has;
                }
            }
        }


        //private void ClearUserInput()
        //{
        //    userName.Clear();
        //    txtPassword.Clear();
        //    tbName.Clear();

        //    for (int i = 0; i < clbDanhSachNhom.Items.Count; i++)
        //        clbDanhSachNhom.SetItemChecked(i, false);

        //    rdoActive.Checked = true;
        //    rdoAddUser.Checked = true;
        //}

        private void ClearRoleInput()
        {
            txtRoleName.Clear();
            txtRoleDescription.Clear();
            grvRoles.ClearSelection();
        }

        private void ClearPermissionInput()
        {
            txtPermissionName.Clear();
            txtPermissionCode.Clear();
            grvPermissions.ClearSelection();
        }
        private async void btnLuuPhanQuyen_Click(object sender, EventArgs e)
        {
            await RunBusyAsync(async () =>
            {
                var saveList = new List<(int RoleId, int PermissionId, bool IsChecked)>();

                foreach (DataGridViewRow row in grvPhanQuyen.Rows)
                {
                    if (row.Tag == null) continue;

                    int roleId = Convert.ToInt32(row.Tag);

                    foreach (DataGridViewColumn col in grvPhanQuyen.Columns)
                    {
                        if (col.Name == "role_name") continue;

                        int pid = Convert.ToInt32(col.Name);
                        bool isChecked = Convert.ToBoolean(row.Cells[col.Name].Value ?? false);

                        saveList.Add((roleId, pid, isChecked));
                    }
                }

                await Task.Run(() =>
                {
                    foreach (var item in saveList)
                    {
                        if (item.IsChecked)
                        {
                            PermissionDbHelper.Execute(@"
                                INSERT INTO role_permissions(role_id, permission_id)
                                SELECT @r,@p
                                WHERE NOT EXISTS(
                                    SELECT 1 FROM role_permissions 
                                    WHERE role_id=@r AND permission_id=@p)",
                                new SQLiteParameter("@r", item.RoleId),
                                new SQLiteParameter("@p", item.PermissionId));
                        }
                        else
                        {
                            PermissionDbHelper.Execute(
                                "DELETE FROM role_permissions WHERE role_id=@r AND permission_id=@p",
                                new SQLiteParameter("@r", item.RoleId),
                                new SQLiteParameter("@p", item.PermissionId));
                        }
                    }
                });

                FrmWaiting.ShowGifAlert("Đã lưu phân quyền thành công", myIcon: EnumStore.Icon.Success);

            }, "ĐANG LƯU PHÂN QUYỀN...");
        }

        private void txtPermissionName_TextChanged(object sender, EventArgs e)
        {
            txtPermissionCode.Text = CoreHelper.BoDauTiengViet(txtPermissionName.Text.Trim()).ToUpper().Replace(" ", "_");
        }
    }
}