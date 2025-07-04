using System;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using KpiApplication.DataAccess;
using KpiApplication.Common;

namespace KpiApplication.Forms
{
    public partial class frmChangePassword : XtraForm
    {
        private readonly Account_DAL account_DAL;
        private readonly string username;

        public frmChangePassword(string username)
        {
            InitializeComponent();
            account_DAL = new Account_DAL();
            this.username = username;

            txtConfirm.Properties.PasswordChar = '*';
            txtOldPassword.Properties.PasswordChar = '*';
            txtNewPassword.Properties.PasswordChar = '*';
        }

        private void btnChange_Click(object sender, EventArgs e)
        {
            string oldPassword = txtOldPassword.Text;
            string newPassword = txtNewPassword.Text;
            string confirmPassword = txtConfirm.Text;

            if (string.IsNullOrEmpty(oldPassword) || string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
            {
                XtraMessageBox.Show("Please fill in all fields.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!account_DAL.ValidateUser(username, oldPassword))
            {
                XtraMessageBox.Show("Incorrect old password!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (newPassword != confirmPassword)
            {
                XtraMessageBox.Show("New passwords do not match!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            bool result = account_DAL.UpdatePassword(username, newPassword);
            if (result)
            {
                XtraMessageBox.Show("Password changed successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            }
            else
            {
                XtraMessageBox.Show("Failed to change password!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
