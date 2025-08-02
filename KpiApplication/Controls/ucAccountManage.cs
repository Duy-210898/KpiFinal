using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Repository;
using KpiApplication.DataAccess;
using KpiApplication.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace KpiApplication.Controls
{
    public partial class ucAccountManage : XtraUserControl, ISupportLoadAsync
    {
        private readonly Account_DAL account_DAL = new Account_DAL();

        public ucAccountManage()
        {
            InitializeComponent();
            InitUI();
        }

        private void InitUI()
        {
            layoutControl1.Visible = false;
            gridControl1.Visible = false;

            txtPassword.Properties.UseSystemPasswordChar = true;
            txtPassword.Properties.PasswordChar = '*';
        }

        public async Task LoadDataAsync()
        {
            await Task.Run(() =>
            {
                var accountList = account_DAL.GetAllAccounts();
                accountList.ForEach(acc => acc.Password = string.Empty);

                Invoke(new Action(() =>
                {
                    gridControl1.DataSource = accountList;
                    gridControl1.Visible = true;

                    HideSensitiveColumns();
                    SetupDepartmentComboBox();
                }));
            });
        }

        private void HideSensitiveColumns()
        {
            gridView1.Columns["UserID"]?.SetVisible(false);

            if (gridView1.Columns["Password"] is var col && col != null)
            {
                col.SetVisible(false);
                col.OptionsColumn.AllowEdit = true;
                col.OptionsColumn.ReadOnly = false;
                col.OptionsColumn.ShowInCustomizationForm = true;
                col.VisibleIndex = gridView1.Columns.Count - 1;
            }
        }

        private void SetupDepartmentComboBox()
        {
            try
            {
                var departments = GetDistinctDepartments();
                var comboBox = new RepositoryItemComboBox
                {
                    TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor
                };
                comboBox.Items.AddRange(departments);

                if (gridView1.Columns["Department"] != null)
                    gridView1.Columns["Department"].ColumnEdit = comboBox;
            }
            catch (Exception ex)
            {
                ShowError($"Lỗi khi thiết lập combobox Department: {ex.Message}");
            }
        }

        private List<string> GetDistinctDepartments()
        {
            return account_DAL.GetDistinctPlants();
        }

        private void LoadDepartmentsToComboBox()
        {
            try
            {
                var departments = GetDistinctDepartments();
                cbxDepartment.Properties.Items.Clear();
                cbxDepartment.Properties.Items.AddRange(departments);
            }
            catch (Exception ex)
            {
                ShowError($"Lỗi khi tải danh sách nhà máy: {ex.Message}");
            }
        }

        private void btnCreate_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            ClearInputFields();
            LoadDepartmentsToComboBox();
            ShowForm();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            HideForm();
        }

        private void ShowForm()
        {
            gridControl1.Visible = false;
            layoutControl1.Visible = true;
            layoutControl1.BringToFront();
            layoutControl1.Location = new Point(3, 35);
        }

        private void HideForm()
        {
            layoutControl1.Visible = false;
            gridControl1.Visible = true;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (!ValidateInputs(out var username, out var fullName, out var engName, out var empCode, out var password, out var department))
                return;

            var user = new Account_Model
            {
                Username = username,
                EmployeeName = fullName,
                EnglishName = engName,
                Password = HashHelper.HashSHA256(password),
                EmployeeID = empCode,
                Department = department,
                IsActive = true
            };

            try
            {
                account_DAL.InsertOrUpdateUser(user);
                XtraMessageBox.Show("✅ Đã lưu tài khoản thành công.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                HideForm();
                _ = LoadDataAsync(); // Không cần đợi
            }
            catch (Exception ex)
            {
                ShowError($"Lỗi khi lưu tài khoản: {ex.Message}");
            }
        }

        private bool ValidateInputs(out string username, out string fullName, out string engName, out string empCode, out string password, out string department)
        {
            username = txtUsername.Text.Trim();
            fullName = txtEmployeeName.Text.Trim();
            engName = txtEngName.Text.Trim();
            empCode = txtEmployeeCode.Text.Trim();
            password = txtPassword.Text;
            department = cbxDepartment.Text.Trim();

            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(fullName) ||
                string.IsNullOrWhiteSpace(empCode) ||
                string.IsNullOrWhiteSpace(password))
            {
                XtraMessageBox.Show("⚠ Vui lòng nhập đầy đủ thông tin.", "Thiếu dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        private async void btnRefresh_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            await LoadDataAsync();
        }

        private void gridView1_RowUpdated(object sender, DevExpress.XtraGrid.Views.Base.RowObjectEventArgs e)
        {
            if (e.Row is Account_Model account)
            {
                try
                {
                    account.Password = string.IsNullOrWhiteSpace(account.Password) ? null : HashHelper.HashSHA256(account.Password);
                    account_DAL.InsertOrUpdateUser(account);
                    XtraMessageBox.Show("✅ Cập nhật tài khoản thành công.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    ShowError($"Lỗi khi cập nhật tài khoản: {ex.Message}");
                }
            }
        }

        private void ClearInputFields()
        {
            txtUsername.Text = string.Empty;
            txtEmployeeName.Text = string.Empty;
            txtPassword.Text = string.Empty;
            txtEmployeeCode.Text = string.Empty;
            cbxDepartment.Text = string.Empty;
        }

        private void ShowError(string message)
        {
            XtraMessageBox.Show(message, "❌ Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    internal static class GridColumnExtensions
    {
        public static void SetVisible(this DevExpress.XtraGrid.Columns.GridColumn column, bool visible)
        {
            if (column != null)
                column.Visible = visible;
        }
    }
}
