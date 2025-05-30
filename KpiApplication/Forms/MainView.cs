using DevExpress.LookAndFeel;
using DevExpress.XtraBars;
using DevExpress.XtraBars.Navigation;
using DevExpress.XtraEditors;
using KpiApplication.Controls;
using KpiApplication.DataAccess;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Windows.Forms;

namespace KpiApplication
{
    public partial class MainView : DevExpress.XtraBars.FluentDesignSystem.FluentDesignForm
    {
        private AccordionControlElement previousSelectedElement;
        private BarButtonItem btnLogOut;
        private User_DAL dbManager;
        private ucWorkingTime ucWorkingTime;  

        public MainView()
        {
            InitializeComponent();
            InitializeLogOutButton();
            dbManager = new User_DAL();
            accordionControl1.ElementClick += AccordionControl1_ElementClick;
            UserLookAndFeel.Default.StyleChanged += (s, e) =>
            {
                string skin = UserLookAndFeel.Default.SkinName;
                SaveLastSkinName(skin);
            };
            previousSelectedElement = accordionControl1.SelectedElement;
        }
        void SaveLastSkinName(string skinName)
        {
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var settings = config.AppSettings.Settings;

            if (settings["LastSkinName"] == null)
                settings.Add("LastSkinName", skinName);
            else
                settings["LastSkinName"].Value = skinName;

            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }
        private void InitializeLogOutButton()
        {
            // Tạo nút Log out
            btnLogOut = new BarButtonItem();
            btnLogOut.Caption = "Log out";
            btnLogOut.ItemClick += BtnLogOut_ItemClick;

            // Thêm nút Log out vào barSubItem1
            barSubItem1.AddItem(btnLogOut);
        }

        private void BtnLogOut_ItemClick(object sender, ItemClickEventArgs e)
        {
            DialogResult result = XtraMessageBox.Show("Are you sure you want to log out?", "Confirm Log out", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                this.Hide();

                Login loginForm = new Login();
                loginForm.Show();

                // Đóng form chính khi form đăng nhập được đóng
                loginForm.FormClosed += (s, args) => this.Close();
            }
        }

        private void btnWorkingTime_Click(object sender, EventArgs e)
        {
            ShowUserControl<ucWorkingTime>("Enter Working Time");
        }

        private void btnPPHData_Click(object sender, EventArgs e)
        {
            ShowUserControl<ucPPHData>("Search IE PPH");
        }

        private void btnViewData_Click(object sender, EventArgs e)
        {
            ShowUserControl<ucViewData>("View Daily KPI Data");
        }

        private void btnViewIEPPH_Click(object sender, EventArgs e)
        {
            ShowUserControl<ucViewPPHData>("View Total IE PPH");
        }

        private void btnWeekly_Click(object sender, EventArgs e)
        {
            ShowUserControl<ucWeeklyPlan>("Weekly Production Plan");
        }

        private Dictionary<Type, UserControl> _userControls = new Dictionary<Type, UserControl>();

        private void ShowUserControl<T>(string status) where T : UserControl, new()
        {
            // Nếu control T chưa tồn tại thì tạo mới và thêm vào panel
            if (!_userControls.TryGetValue(typeof(T), out UserControl userControl))
            {
                userControl = new T
                {
                    Dock = DockStyle.Fill
                };

                _userControls[typeof(T)] = userControl;
                pnlControl.Controls.Add(userControl);

                if (typeof(T) == typeof(ucWorkingTime))
                {
                    ucWorkingTime = userControl as ucWorkingTime;
                }
                else
                {
                    ucWorkingTime = null;
                }
            }

            // Ẩn tất cả control khác trong panel
            foreach (Control ctrl in pnlControl.Controls)
            {
                ctrl.Visible = ctrl == userControl;
            }
        }
        private void HighlightSelectedItem(AccordionControlElement selectedElement)
        {
            if (selectedElement.Style != ElementStyle.Item)
                return;

            if (previousSelectedElement != null)
            {
                ResetElementAppearance(previousSelectedElement);
            }

            SetElementAppearance(selectedElement);

            previousSelectedElement = selectedElement;
        }

        private void SetElementAppearance(AccordionControlElement element)
        {
            element.Appearance.Normal.BackColor = System.Drawing.Color.LightSteelBlue;
            element.Appearance.Normal.ForeColor = System.Drawing.Color.Black;
            element.Appearance.Normal.Font = new System.Drawing.Font(element.Appearance.Normal.Font, System.Drawing.FontStyle.Bold);
        }

        private void ResetElementAppearance(AccordionControlElement element)
        {
            element.Appearance.Normal.BackColor = System.Drawing.Color.Transparent;
            element.Appearance.Normal.ForeColor = System.Drawing.Color.Empty;
            element.Appearance.Normal.Font = new System.Drawing.Font(element.Appearance.Normal.Font, System.Drawing.FontStyle.Regular);
        }

        private void AccordionControl1_ElementClick(object sender, ElementClickEventArgs e)
        {
            HighlightSelectedItem(e.Element);
        }

        private void MainView_Load(object sender, EventArgs e)
        {
            string department = dbManager.GetDepartmentByUsername(KpiApplication.Common.Global.Username);
            barSubItem1.Caption = KpiApplication.Common.Global.Username + " - " + department;

            if (department != "ME")
            {
                btnViewData.Enabled = false;
                btnViewPPH.Enabled = false;
                btnWeeklyPlan.Enabled = false;
            }
        }

        private void MainView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.S)
            {
                e.Handled = true;
                ucWorkingTime?.SaveModifiedData();
            }
        }

        private void MainView_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (ucWorkingTime != null && ucWorkingTime.HasUnsavedChanges)
            {
                var result = XtraMessageBox.Show(
                    "You have unsaved changes. Do you want to exit without saving?",
                    "Warning",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.No)
                {
                    // Người dùng không muốn thoát mà không lưu -> hủy đóng form
                    e.Cancel = true;
                }
            }
        }

        private void accordionControl1_SelectedElementChanged(object sender, SelectedElementChangedEventArgs e)
        {
            if (ucWorkingTime != null && ucWorkingTime.HasUnsavedChanges)
            {
                var result = XtraMessageBox.Show(
                    "You have unsaved changes. Do you want to exit without saving?",
                    "Warning",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.No)
                {
                    // Người dùng chọn No -> giữ nguyên tab hiện tại, không chuyển
                    if (previousSelectedElement != null)
                        accordionControl1.SelectedElement = previousSelectedElement;

                    return;
                }
                // Nếu chọn Yes -> chuyển tab luôn mà không lưu
            }

            // Cập nhật tab hiện tại nếu chuyển thành công
            previousSelectedElement = e.Element;
        }
    }
}