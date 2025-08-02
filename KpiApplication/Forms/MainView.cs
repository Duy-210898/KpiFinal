using DevExpress.LookAndFeel;
using DevExpress.XtraBars;
using DevExpress.XtraBars.Navigation;
using DevExpress.XtraEditors;
using DevExpress.XtraSplashScreen;
using KpiApplication.Common;
using KpiApplication.Controls;
using KpiApplication.DataAccess;
using KpiApplication.Forms;
using KpiApplication.Services;
using KpiApplication.Utils;
using System;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace KpiApplication
{
    public partial class MainView : DevExpress.XtraBars.FluentDesignSystem.FluentDesignForm, ILoadingService
    {
        private AccordionControlElement previousSelectedElement;
        private BarButtonItem btnLogOut;
        private readonly Account_DAL account_DAL;
        private readonly UserControlServices _ucManager;

        private readonly (AccordionControlElement Element, Type ControlType, string LangText)[] _menuItems;

        public MainView()
        {
            InitializeComponent();
            account_DAL = new Account_DAL();
            _ucManager = new UserControlServices(navigationFrame);

            LangHelper.ApplyCulture();
            InitializeLogOutButton();
            this.KeyPreview = true;

            _menuItems = new (AccordionControlElement, Type, string)[]
            {
                (btnWorkingTime, typeof(ucWorkingTime), Lang.WorkingTime),
                (btnPPHData, typeof(ucPPHData), Lang.SearchIEPPH),
                (btnViewPPHData, typeof(ucViewPPHData), Lang.ViewIEPPH),
                (btnViewData, typeof(ucViewData), Lang.ViewDailyData),
                (btnWeeklyPlan, typeof(ucWeeklyPlan), Lang.WeeklyPlan),
                (btnAccountManage, typeof(ucAccountManage), Lang.AccountManagement),
                (btnViewTCT, typeof(ucViewTCTData), Lang.ViewTCTData),
                (btnTCT, typeof(ucTCTData), Lang.TCTData),
                (btnBonus, typeof(ucBonusDocument), Lang.BonusDocument),
                (btnViewBonusDocuments, typeof(ucViewBonusDocuments), Lang.ViewBonusDocuments),
                (btnCIDocument, typeof(ucCIDocument), "CI Document")
            };

            InitMenuItems();

            accordionControl1.ElementClick += AccordionControl1_ElementClick;
            UserLookAndFeel.Default.StyleChanged += (s, e) => SaveLastSkinName(UserLookAndFeel.Default.SkinName);
            previousSelectedElement = accordionControl1.SelectedElement;
        }
        public async Task ShowLoadingAsync(string caption, string description, Func<Task> loadAction)
        {
            try
            {
                SplashScreenManager.ShowForm(this, typeof(CustomWaitForm), true, true, false);
                SplashScreenManager.Default.SetWaitFormCaption(caption);
                SplashScreenManager.Default.SetWaitFormDescription(description);

                await loadAction();
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError("Loading failed", ex);
            }
            finally
            {
                if (SplashScreenManager.Default.IsSplashFormVisible)
                    SplashScreenManager.CloseForm();
            }
        }

        private void InitMenuItems()
        {
            foreach (var (element, type, text) in _menuItems)
            {
                element.Tag = type;
                element.Text = text;
            }

            accordion1.Text = Lang.SharedData;
            accordion2.Text = Lang.PC;
            accordion3.Text = Lang.ME;
            accordion4.Text = Lang.Management;
        }

        private void InitializeLogOutButton()
        {
            btnLogOut = new BarButtonItem
            {
                Caption = Lang.Logout,
                ImageOptions = { Image = Properties.Resources.logout }
            };
            btnLogOut.ItemClick += BtnLogOut_ItemClick;
            barSubItem1.AddItem(btnLogOut);
        }

        private void BtnLogOut_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (XtraMessageBox.Show(Lang.ConfirmLogout, Lang.Confirm, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                Hide();
                var loginForm = new Login();
                loginForm.Show();
                loginForm.FormClosed += (s, args) => Close();
            }
        }

        private async void AccordionControl1_ElementClick(object sender, ElementClickEventArgs e)
        {
            HighlightSelectedItem(e.Element);

            if (e.Element.Tag is Type controlType && typeof(UserControl).IsAssignableFrom(controlType))
            {
                await _ucManager.ShowAsync(controlType, this);
            }
        }

        private void HighlightSelectedItem(AccordionControlElement selectedElement)
        {
            if (selectedElement.Style != ElementStyle.Item || previousSelectedElement == selectedElement)
                return;

            ResetElementAppearance(previousSelectedElement);
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
            if (element == null) return;
            element.Appearance.Normal.BackColor = System.Drawing.Color.Transparent;
            element.Appearance.Normal.ForeColor = System.Drawing.Color.Empty;
            element.Appearance.Normal.Font = new System.Drawing.Font(element.Appearance.Normal.Font, System.Drawing.FontStyle.Regular);
        }

        private void MainView_Load(object sender, EventArgs e)
        {
            var emp = Global.CurrentEmployee;
            if (emp == null)
            {
                MessageBoxHelper.ShowError(Lang.UserNotFound);
                return;
            }

            barSubItem1.Caption = $"{emp.EmployeeName} - {emp.Department}";

            if (Global.Username == "Guest")
            {
                ApplyGuestRestrictions();
            }
            else if (emp.Department != "ME")
            {
                btnViewData.Enabled = false;
                btnPPHData.Enabled = false;
                btnWeeklyPlan.Enabled = false;
                btnAccountManage.Enabled = false;
                btnBonus.Enabled = false;
                btnTCT.Enabled = false;
            }
        }

        private void ApplyGuestRestrictions()
        {
            btnWorkingTime.Enabled = false;
            btnViewData.Enabled = false;
            btnPPHData.Enabled = false;
            btnAccountManage.Enabled = false;
            btnWeeklyPlan.Enabled = false;
            btnBonus.Enabled = false;
            btnTCT.Enabled = false;
            this.Text += $" - [{Lang.ReadOnly}]";
        }

        private void MainView_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_ucManager.Get<ucWorkingTime>()?.HasUnsavedChanges == true)
            {
                var result = XtraMessageBox.Show(Lang.UnsavedChangesWarning, Lang.Warning, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (result == DialogResult.No)
                {
                    e.Cancel = true;
                }
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            var wt = _ucManager.Get<ucWorkingTime>();
            var pph = _ucManager.Get<ucPPHData>();

            switch (keyData)
            {
                case Keys.Control | Keys.S:
                    if (wt?.Visible == true)
                    {
                        _ = RunSafeAsync(wt.SaveModifiedData);
                        return true;
                    }
                    break;

                case Keys.Control | Keys.F:
                    if (pph?.Visible == true)
                    {
                        pph.ShowFind();
                        return true;
                    }
                    break;

                case Keys.F5:
                    _ = ShowLoadingAsync("Refreshing...", string.Empty, ReloadCurrentUserControlAsync);
                    return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private async Task RunSafeAsync(Func<Task> asyncMethod)
        {
            try
            {
                await asyncMethod();
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError(Lang.UnexpectedError, ex);
            }
        }

        private void accordionControl1_SelectedElementChanged(object sender, SelectedElementChangedEventArgs e)
        {
            var wt = _ucManager.Get<ucWorkingTime>();
            if (wt?.HasUnsavedChanges == true)
            {
                var result = XtraMessageBox.Show(Lang.UnsavedChangesWarning, Lang.Notification, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (result == DialogResult.No)
                {
                    accordionControl1.SelectedElement = previousSelectedElement;
                    return;
                }
            }
            previousSelectedElement = e.Element;
        }

        private void SaveLastSkinName(string skinName)
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
        private async Task ReloadCurrentUserControlAsync()
        {
            var currentControl = navigationFrame.SelectedPage?
                .Controls.OfType<ISupportLoadAsync>().FirstOrDefault();

            if (currentControl != null)
            {
                await currentControl.LoadDataAsync();
            }
        }

        private async void btnRefresh_ItemClick(object sender, ItemClickEventArgs e)
        {
            await ShowLoadingAsync("Refreshing...", string.Empty, ReloadCurrentUserControlAsync);
        }
    }
}
