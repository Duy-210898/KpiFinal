using DevExpress.LookAndFeel;
using DevExpress.UserSkins;
using DevExpress.XtraEditors;
using System;
using System.Configuration;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;

namespace KpiApplication
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // 🔁 Thiết lập ngôn ngữ từ AppSettings
            string selectedCulture = KpiApplication.Properties.Settings.Default.AppCulture ?? "en";
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(selectedCulture);
            Thread.CurrentThread.CurrentCulture = new CultureInfo(selectedCulture);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // DevExpress skin
            BonusSkins.Register();
            string lastSkinName = ConfigurationManager.AppSettings["LastSkinName"];
            if (string.IsNullOrEmpty(lastSkinName))
            {
                lastSkinName = "WXI";
            }
            UserLookAndFeel.Default.SetSkinStyle(lastSkinName);
            WindowsFormsSettings.DefaultFont = new System.Drawing.Font("Arial", 9);

            // 👉 Khởi chạy form chính
            Application.Run(new Login());
        }
    }
}
