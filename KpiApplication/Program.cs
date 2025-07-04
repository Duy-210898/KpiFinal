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
            string lastSkinName = ConfigurationManager.AppSettings["LastSkinName"];
            if (string.IsNullOrEmpty(lastSkinName))
            {
                lastSkinName = "WXI";
            }

            UserLookAndFeel.Default.SetSkinStyle(lastSkinName);
            WindowsFormsSettings.DefaultFont = new System.Drawing.Font("Arial", 9);

            Application.Run(new Login());
        }
    }
}
