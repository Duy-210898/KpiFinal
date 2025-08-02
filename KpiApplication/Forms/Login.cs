using DevExpress.LookAndFeel;
using DevExpress.Skins;
using DevExpress.XtraEditors;
using DevExpress.XtraLayout;
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
            LangHelper.ApplyCulture();
            ApplyLocalizedText();

            // DevExpress Skin
            // DevExpress Skin
            LookAndFeel.UseDefaultLookAndFeel = true;
            LookAndFeel.Style = LookAndFeelStyle.Skin;

            // Cấu hình mật khẩu ẩn
            txtPassword.UseSystemPasswordChar = true;
            layoutControlItem1.ImageOptions.Image = Properties.Resources.translate;

            // Hover label
            LabelHoverHelper.ApplyHoverStyleToAllLabels(this);

            cbxLanguage.Properties.Items.Clear();
            cbxLanguage.Properties.Items.AddRange(new[] { "English", "Tiếng Việt", "中文" });

            // Gán ngôn ngữ đang lưu
            string currentCulture = Properties.Settings.Default.AppCulture ?? "en";
            switch (currentCulture)
            {
                case "vi":
                    cbxLanguage.SelectedItem = "Tiếng Việt";
                    break;
                case "zh":
                    cbxLanguage.SelectedItem = "中文";
                    break;
                default:
                    cbxLanguage.SelectedItem = "English";
                    break;
            }
            chkRememberMe.Checked = Properties.Settings.Default.RememberAccount;

            if (Properties.Settings.Default.RememberAccount)
            {
                txtUsername.Text = Properties.Settings.Default.SavedUsername;
                txtPassword.Text = Properties.Settings.Default.SavedPassword;
            }

            this.Shown += (s, e) => SetRoundedRegion(50);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x02000000; 
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
                MessageBoxHelper.ShowError(Lang.Username + " " + Lang.Required); 
                txtUsername.Focus();
                return;
            }

            if (!account_DAL.UserExists(username))
            {
                MessageBoxHelper.ShowError(Lang.UserNotFound);
                txtPassword.Clear();
                txtPassword.Focus();
                return;
            }

            if (!account_DAL.IsActive(username))
            {
                MessageBoxHelper.ShowError(Lang.AccountDisabled); 
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
                // Nếu chọn nhớ tài khoản thì lưu
                if (chkRememberMe.Checked)
                {
                    Properties.Settings.Default.RememberAccount = true;
                    Properties.Settings.Default.SavedUsername = username;
                    Properties.Settings.Default.SavedPassword = password;
                }
                else
                {
                    Properties.Settings.Default.RememberAccount = false;
                    Properties.Settings.Default.SavedUsername = "";
                    Properties.Settings.Default.SavedPassword = "";
                }
                Properties.Settings.Default.Save();

            }
            else
            {
                MessageBoxHelper.ShowError(Lang.WrongPassword); 
                txtPassword.Clear();
                txtPassword.Focus();
            }
        }

        private void btnGuestLogin_Click(object sender, EventArgs e)
        {
            Global.Username = "Guest";
            Global.CurrentEmployee = new EmployeeInfo_Model
            {
                EmployeeName = Lang.Guest,
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
                MessageBoxHelper.ShowError(Lang.Username + " " + Lang.RequiredBeforeChangePassword);
                return;
            }

            if (!account_DAL.UserExists(username))
            {
                MessageBoxHelper.ShowError(Lang.UsernameNotExists);
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


        private void ApplyLocalizedText()
        {
            layoutControlItem1.Text = Lang.Language;
            Text = Lang.Login;
            lblUsername.Text = Lang.Username + ":";
            lblPassword.Text = Lang.Password + ":";
            btnLogin.Text = Lang.Login;
            lblShowPassword.Text = Lang.Show;
            btnGuestLogin.Text = Lang.GuestLogin;
            btnExit.Text = Lang.Exit;
            lblChangePassword.Text = Lang.ChangePassword;
            cbxLanguage.Properties.NullText = Lang.Language;
            chkRememberMe.Text = Lang.RememberMe;
        }


        private void cbxLanguage_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selectedCulture = "en";
            string selectedText = cbxLanguage.SelectedItem.ToString();

            switch (selectedText)
            {
                case "Tiếng Việt":
                    selectedCulture = "vi";
                    break;
                case "中文":
                    selectedCulture = "zh";
                    break;
                default:
                    selectedCulture = "en";
                    break;
            }

            // Lưu cấu hình ngôn ngữ
            Properties.Settings.Default.AppCulture = selectedCulture;
            Properties.Settings.Default.Save();

            LangHelper.ApplyCulture();    
            ApplyLocalizedText();            

            this.Refresh();
        }
    }
}
