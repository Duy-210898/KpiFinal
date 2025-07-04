using DevExpress.LookAndFeel;
using DevExpress.XtraBars;
using DevExpress.XtraBars.Navigation;
using DevExpress.XtraEditors;
using KpiApplication.Common;
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
        private readonly Account_DAL account_DAL;
        private ucWorkingTime ucWorkingTime;
        private ucViewPPHData ucViewPPHData;


        public MainView()
        {
            InitializeComponent();
            InitializeLogOutButton();
            this.KeyPreview = true;
            account_DAL = new Account_DAL();
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
        private void btnAccountManage_Click(object sender, EventArgs e)
        {
            ShowUserControl<ucAccountManage>("Account Management");
        }
        private void btnTCT_Click(object sender, EventArgs e)
        {
            ShowUserControl<ucTCTData>("TCT Data");
        }

        private Dictionary<Type, UserControl> _userControls = new Dictionary<Type, UserControl>();

        private void ShowUserControl<T>(string status) where T : UserControl, new()
        {
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

                if (typeof(T) == typeof(ucViewPPHData))
                {
                    ucViewPPHData = userControl as ucViewPPHData;
                }
            }

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
            var emp = Global.CurrentEmployee;

            if (emp == null)
            {
                MessageBox.Show("Không tìm thấy thông tin người dùng!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            barSubItem1.Caption = $"{emp.EmployeeName} - {emp.Department}";

            if (emp.Department != "ME")
            {
                btnViewData.Enabled = false;
                btnViewPPH.Enabled = false;
                btnWeeklyPlan.Enabled = false;
                btnAccountManage.Enabled = false;
            }
        }
        private void MainView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.S)
            {
                e.Handled = true;
                ucWorkingTime?.SaveModifiedData();
            }
            if (e.Control && e.KeyCode == Keys.F)
            {
                ucViewPPHData?.ShowFind(); 
                e.Handled = true;
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
            }

            previousSelectedElement = e.Element;
        }
    }
}