using DevExpress.Skins;
using DevExpress.LookAndFeel;
using DevExpress.XtraEditors;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace KpiApplication.Utils
{
    public static class LabelHoverHelper
    {
        /// <summary>
        /// Áp dụng hiệu ứng hover cho tất cả LabelControl trong container (form, panel, groupbox, ...)
        /// </summary>
        public static void ApplyHoverStyleToAllLabels(Control parent, bool underlineOnHover = true)
        {
            foreach (Control control in parent.Controls)
            {
                if (control is LabelControl label)
                {
                    ApplyHoverStyle(label, underlineOnHover);
                }
                else if (control.HasChildren)
                {
                    ApplyHoverStyleToAllLabels(control, underlineOnHover); // đệ quy
                }
            }
        }

        /// <summary>
        /// Áp dụng hover riêng cho một LabelControl
        /// </summary>
        public static void ApplyHoverStyle(LabelControl label, bool underlineOnHover = true)
        {
            var skin = CommonSkins.GetSkin(UserLookAndFeel.Default);
            Color hoverColor = Color.Blue;
            if (skin != null)
            {
                var hyperlink = skin["Hyperlink"];
                if (hyperlink != null)
                {
                    hoverColor = hyperlink.Color.BackColor;
                }
            }

            Color originalColor = label.ForeColor;
            Font originalFont = label.Font;

            label.Cursor = Cursors.Hand;

            label.MouseEnter += (s, e) =>
            {
                label.ForeColor = hoverColor;
                if (underlineOnHover)
                    label.Font = new Font(originalFont, FontStyle.Underline);
            };

            label.MouseLeave += (s, e) =>
            {
                label.ForeColor = originalColor;
                label.Font = originalFont;
            };
        }
    }
}
