using DevExpress.XtraEditors;
using System;
using System.Windows.Forms;
using KpiApplication.Common;

namespace KpiApplication.Forms
{
    public partial class ExportDirectionForm : XtraForm
    {
        public ExportOrientation SelectedOrientation { get; private set; } = ExportOrientation.Cancel;

        public ExportDirectionForm()
        {
            InitializeComponent();
        }

        private void btnHorizontal_Click(object sender, EventArgs e)
        {
            SelectedOrientation = ExportOrientation.Horizontal;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnVertical_Click(object sender, EventArgs e)
        {
            SelectedOrientation = ExportOrientation.Vertical;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            SelectedOrientation = ExportOrientation.Cancel;
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}