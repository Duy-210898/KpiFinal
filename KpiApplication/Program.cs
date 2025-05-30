using DevExpress.LookAndFeel;
using DevExpress.UserSkins;
using DevExpress.XtraEditors;
using System;
using System.Configuration;
using System.Windows.Forms;

namespace KpiApplication
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            BonusSkins.Register();
            // Đọc tên skin lưu trong appSettings
            string lastSkinName = ConfigurationManager.AppSettings["LastSkinName"];
            if (string.IsNullOrEmpty(lastSkinName))
            {
                lastSkinName = "WXI"; // skin mặc định nếu không có trong config
            }

            UserLookAndFeel.Default.SetSkinStyle(lastSkinName);
            WindowsFormsSettings.DefaultFont = new System.Drawing.Font("Arial", 9);

            Application.Run(new Login());
        }
    }
}
