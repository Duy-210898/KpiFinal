using DevExpress.LookAndFeel;
using DevExpress.Skins;
using DevExpress.XtraEditors;
using KpiApplication.Common;
using KpiApplication.DataAccess;
using KpiApplication.Forms;
using KpiApplication.Models;
using KpiApplication.Utils;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace KpiApplication
{
    public partial class Login : XtraForm
    {
        private readonly Account_DAL account_DAL = new Account_DAL();
        private bool isMouseDown = false;

        public Login()
        {
            // CHỐNG CHỚP NHÁY
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            UpdateStyles();

            InitializeComponent();

            // DevExpress Skin
            LookAndFeel.UseDefaultLookAndFeel = true;
            LookAndFeel.Style = LookAndFeelStyle.Skin;

            // Mật khẩu dạng ẩn
            txtPassword.UseSystemPasswordChar = true;

            // Hover label
            LabelHoverHelper.ApplyHoverStyleToAllLabels(this);

            // Chỉ bo góc sau khi form hiển thị (tránh redraw liên tục)
            this.Shown += (s, e) => SetRoundedRegion(50);
        }

        // ✅ KHÔNG CHỚP NHÁY do redraw control con
        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x02000000; // WS_EX_COMPOSITED
                return cp;
            }
        }

        private void SetRoundedRegion(int radius)
        {
            var path = new GraphicsPath();
            path.StartFigure();
            path.AddArc(new Rectangle(0, 0, radius, radius), 180, 90);
            path.AddLine(radius, 0, Width - radius, 0);
            path.AddArc(new Rectangle(Width - radius, 0, radius, radius), -90, 90);
            path.AddLine(Width, radius, Width, Height - radius);
            path.AddArc(new Rectangle(Width - radius, Height - radius, radius, radius), 0, 90);
            path.AddLine(Width - radius, Height, radius, Height);
            path.AddArc(new Rectangle(0, Height - radius, radius, radius), 90, 90);
            path.CloseFigure();

            Region = new Region(path);
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text;

            if (string.IsNullOrEmpty(username))
            {
                MessageBoxHelper.ShowError("Please enter your username!");
                txtUsername.Focus();
                return;
            }

            if (!account_DAL.UserExists(username))
            {
                MessageBoxHelper.ShowError("Username does not exist!");
                txtPassword.Clear();
                txtPassword.Focus();
                return;
            }

            if (!account_DAL.IsActive(username))
            {
                MessageBoxHelper.ShowError("The account has been disabled!");
                txtPassword.Clear();
                txtPassword.Focus();
                return;
            }

            if (account_DAL.ValidateUser(username, password))
            {
                Global.Username = username;
                Global.CurrentEmployee = account_DAL.GetEmployeeInfo(username) ?? new EmployeeInfo_Model();

                txtPassword.Clear();
                Hide();

                var mainView = new MainView();
                mainView.FormClosed += (s, args) => Close();
                mainView.Show();
            }
            else
            {
                MessageBoxHelper.ShowError("Incorrect password!");
                txtPassword.Clear();
                txtPassword.Focus();
            }
        }

        private void btnGuestLogin_Click(object sender, EventArgs e)
        {
            Global.Username = "Guest";
            Global.CurrentEmployee = new EmployeeInfo_Model
            {
                EmployeeName = "Guest User",
                Department = "N/A"
            };

            Hide();
            var mainView = new MainView();
            mainView.FormClosed += (s, args) => Close();
            mainView.Show();
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void lblChangePassword_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            if (string.IsNullOrEmpty(username))
            {
                MessageBoxHelper.ShowError("Please enter your username before changing the password!");
                return;
            }

            if (!account_DAL.UserExists(username))
            {
                MessageBoxHelper.ShowError("Username does not exist.");
                return;
            }

            new frmChangePassword(username).ShowDialog();
        }

        private void frmLogin_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }

        private void TogglePasswordVisibility()
        {
            txtPassword.UseSystemPasswordChar = !isMouseDown;
        }

        private void lblShowPassword_MouseDown(object sender, MouseEventArgs e)
        {
            isMouseDown = true;
            TogglePasswordVisibility();
        }

        private void lblShowPassword_MouseUp(object sender, MouseEventArgs e)
        {
            isMouseDown = false;
            TogglePasswordVisibility();
        }

        private void ConfigureHoverAppearance(LabelControl label)
        {
            var skin = CommonSkins.GetSkin(UserLookAndFeel.Default);
            Color hoverColor = Color.Blue;

            if (skin != null && skin["Hyperlink"] != null)
            {
                hoverColor = skin["Hyperlink"].Color.BackColor;
            }

            var originalColor = label.ForeColor;
            label.Cursor = Cursors.Hand;

            label.MouseEnter += (s, e) => label.ForeColor = hoverColor;
            label.MouseLeave += (s, e) => label.ForeColor = originalColor;
        }
    }
}
