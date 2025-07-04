using DevExpress.XtraEditors;
using KpiApplication.Common;
using KpiApplication.DataAccess;
using KpiApplication.Forms;
using KpiApplication.Models;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace KpiApplication
{
    public partial class Login : XtraForm
    {
        private readonly Account_DAL account_DAL;

        public static bool isLoggedIn = false;
        private bool isMouseDown = false;

        public Login()
        {
            InitializeComponent();
            EnableDoubleBufferingForForm();
            LookAndFeel.UseWindowsXPTheme = true;
            txtPassword.UseSystemPasswordChar = true;
            account_DAL = new Account_DAL();
            SetRoundedRegion(50);
        }
        private void SetRoundedRegion(int radius)
        {
            var path = new GraphicsPath();

            path.StartFigure();
            path.AddArc(new Rectangle(0, 0, radius, radius), 180, 90);
            path.AddLine(radius, 0, this.Width - radius, 0);
            path.AddArc(new Rectangle(this.Width - radius, 0, radius, radius), -90, 90);
            path.AddLine(this.Width, radius, this.Width, this.Height - radius);
            path.AddArc(new Rectangle(this.Width - radius, this.Height - radius, radius, radius), 0, 90);
            path.AddLine(this.Width - radius, this.Height, radius, this.Height);
            path.AddArc(new Rectangle(0, this.Height - radius, radius, radius), 90, 90);
            path.CloseFigure();

            this.Region = new Region(path);
        }


        private void EnableDoubleBufferingForForm()
        {
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
            this.UpdateStyles();
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text;
            string password = txtPassword.Text;

            if (string.IsNullOrEmpty(username))
            {
                XtraMessageBox.Show("Please enter your username!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtUsername.Focus();
                return;
            }

            bool doesUserExist = account_DAL.UserExists(username);
            if (!doesUserExist)
            {
                XtraMessageBox.Show("Username does not exist!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtPassword.Clear();
                txtPassword.Focus();
                return;
            }

            bool isActive = account_DAL.IsActive(username);
            if (!isActive)
            {
                XtraMessageBox.Show("The account has been disabled!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtPassword.Clear();
                txtPassword.Focus();
                return;
            }

            bool isUserValid = account_DAL.ValidateUser(username, password);
            if (isUserValid)
            {
                Global.Username = username;

                EmployeeInfo_Model empInfo = account_DAL.GetEmployeeInfo(username);
                if (empInfo != null)
                {
                    Global.CurrentEmployee = empInfo;
                }

                txtPassword.Clear();
                this.Hide();
                MainView mainView = new MainView();
                mainView.Show();
                mainView.FormClosed += (s, args) => this.Close();
            }
            else
            {
                XtraMessageBox.Show("Incorrect password!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtPassword.Clear();
                txtPassword.Focus();
            }
        }

        private void LblShowPassword_MouseEnter(object sender, EventArgs e)
        {
            lblShowPassword.ForeColor = System.Drawing.Color.Black;
        }

        private void LblChangePassword_MouseEnter(object sender, EventArgs e)
        {
            lblChangePassword.ForeColor = System.Drawing.Color.Black;
        }

        private void LblShowPassword_MouseDown(object sender, MouseEventArgs e)
        {
            isMouseDown = true;
            TogglePasswordVisibility();
        }

        private void LblShowPassword_MouseUp(object sender, MouseEventArgs e)
        {
            isMouseDown = false;
            TogglePasswordVisibility();
        }

        private void TogglePasswordVisibility()
        {
            txtPassword.UseSystemPasswordChar = !isMouseDown;
        }

        private void LblShowPassword_MouseLeave(object sender, EventArgs e)
        {
            lblShowPassword.ForeColor = System.Drawing.Color.Silver;
        }

        private void lblChangePassword_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            if (string.IsNullOrEmpty(username))
            {
                XtraMessageBox.Show("Please enter your username before changing the password!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!account_DAL.UserExists(username))
            {
                XtraMessageBox.Show("Username does not exist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            frmChangePassword frm = new frmChangePassword(username);
            frm.ShowDialog();
        }
        private void frmLogin_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }

        private void lblChangePassword_MouseLeave(object sender, EventArgs e)
        {
            lblChangePassword.ForeColor = System.Drawing.Color.Silver;
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}
