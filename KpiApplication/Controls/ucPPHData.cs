using DevExpress.XtraGrid.Views.Grid;
using KpiApplication.DataAccess;
using KpiApplication.Models;
using KpiApplication.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;

namespace KpiApplication.Controls
{
    public partial class ucPPHData : DevExpress.XtraEditors.XtraUserControl
    {
        private BindingList<IEPPHDataForUser> iepphDataList;
        private readonly IEPPHData_DAL iePPHData_DAL = new IEPPHData_DAL();

        public ucPPHData()
        {
            InitializeComponent();
            dgvPPHData.CellMerge += dgvPPHData_CellMerge;
            dgvPPHData.CustomDrawCell += dgvPPHData_CustomDrawCell;
        }

        private void DgvPPHData_CellMerge(object sender, CellMergeEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void LoadDataToGrid(BindingList<IEPPHDataForUser> data)
        {
            iepphDataList = data;
            gridControl1.DataSource = iepphDataList;
        }

        private BindingList<IEPPHDataForUser> FetchData()
        {
            return iePPHData_DAL.GetIEPPHDataForUser();
        }
        private async void ucPPHData_Load(object sender, EventArgs e)
        {
            await AsyncLoaderHelper.LoadDataWithSplashAsync(
                this,
                FetchData,
                data =>
                {
                    LoadDataToGrid(data);

                    // Cấu hình GridView
                    GridViewHelper.ApplyDefaultFormatting(dgvPPHData);
                    GridViewHelper.EnableWordWrapForGridView(dgvPPHData);

                    dgvPPHData.OptionsView.AllowCellMerge = true;
                    dgvPPHData.OptionsSelection.MultiSelect = false;
                    dgvPPHData.OptionsSelection.MultiSelectMode = GridMultiSelectMode.CheckBoxRowSelect;
                    dgvPPHData.ShowPopupEditForm();

                    // Ẩn cột không cần
                    GridViewHelper.HideColumns(dgvPPHData,
                        "OutsourcingStitchingBool",
                        "OutsourcingAssemblingBool",
                        "OutsourcingStockFittingBool"
                    );

                    // Đổi tên cột dễ đọc
                    var captions = new Dictionary<string, string>
                    {
            { "IEPPHValue", "IE PPH" },
            { "IsSigned", "Production Sign" },
            { "OutsourcingAssembling", "Outsourcing\nAssembling" },
            { "OutsourcingStitching", "Outsourcing\nStitching" },
            { "OutsourcingStockFitting", "Outsourcing\nStockFitting" },
            { "AdjustOperatorNo", "Adjust\nOperator" },
            { "TargetOutputPC", "Target\nOutput" },
            { "StageName", "Process" },
            { "THTValue", "THT" },
            { "TypeName", "Type" },
            { "PersonIncharge", "Person\nIncharge" }
                    };
                    GridViewHelper.SetColumnCaptions(dgvPPHData, captions);

                    // Tối ưu hiển thị
                    GridViewHelper.AdjustGridColumnWidthsAndRowHeight(dgvPPHData);
                    GridViewHelper.EnableCopyFunctionality(dgvPPHData);
                },
                "Loading..."
            );
        }
        private void dgvPPHData_CellMerge(object sender, CellMergeEventArgs e)
        {
            // Các cột muốn gộp, bao gồm cả ArticleName
            string[] mergeableColumns = { "ArticleName", "ModelName", "PCSend",
                "PersonIncharge", "OutsourcingAssembling", "NoteForPC",
                "OutsourcingStitching", "OutsourcingStockFitting", "DataStatus" };

            if (!mergeableColumns.Contains(e.Column.FieldName))
            {
                e.Merge = false;
                e.Handled = true;
                return;
            }
            dgvPPHData.Columns["ModelName"].AppearanceCell.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;

            // Lấy giá trị ArticleName để làm điều kiện gộp
            string article1 = dgvPPHData.GetRowCellValue(e.RowHandle1, "ArticleName")?.ToString();
            string article2 = dgvPPHData.GetRowCellValue(e.RowHandle2, "ArticleName")?.ToString();

            if (e.Column.FieldName == "ArticleName")
            {
                e.Merge = article1 == article2;
            }
            else
            {
                e.Merge = article1 == article2;
            }

            e.Handled = true;
        }

        private void dgvPPHData_CustomDrawCell(object sender, DevExpress.XtraGrid.Views.Base.RowCellCustomDrawEventArgs e)
        {
            if (e.RowHandle < 0 || e.Column.VisibleIndex < 0)
                return;

            if (e.Column.VisibleIndex >= 9)
            {
                bool isEvenRow = e.RowHandle % 2 == 0;

                Color backColor = isEvenRow ? Color.White : Color.AliceBlue;

                e.Appearance.BackColor = backColor;
            }
        }
    }
}
