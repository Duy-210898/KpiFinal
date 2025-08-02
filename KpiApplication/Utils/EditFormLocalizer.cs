using DevExpress.XtraEditors;
using System.Windows.Forms;

namespace KpiApplication.Utils
{
    public static class EditFormLocalizer
    {
        public static void LocalizeButtons(Control root, string updateText, string cancelText)
        {
            ApplyRecursive(root, updateText, cancelText);
        }

        private static void ApplyRecursive(Control ctrl, string updateText, string cancelText)
        {
            if (ctrl is SimpleButton btn)
            {
                if (btn.Text == "Update" || btn.Name == "btnUpdate")
                    btn.Text = updateText;
                else if (btn.Text == "Cancel" || btn.Name == "btnCancel")
                    btn.Text = cancelText;
            }

            foreach (Control child in ctrl.Controls)
            {
                ApplyRecursive(child, updateText, cancelText);
            }
        }
    }
}
