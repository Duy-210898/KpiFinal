using DevExpress.XtraEditors.Repository;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Base;
using DevExpress.XtraGrid.Views.Grid;
using KpiApplication.Common;
using KpiApplication.DataAccess;
using KpiApplication.Excel;
using KpiApplication.Extensions;
using KpiApplication.Forms;
using KpiApplication.Models;
using KpiApplication.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace KpiApplication.Controls
{
    public partial class ucPPHData : DevExpress.XtraEditors.XtraUserControl, ISupportLoadAsync
    {
        private BindingList<IETotal_Model> ieTotalList;
        private List<IETotal_Model> ieTotalListOriginalClone;
        private readonly HashSet<string> mergeByArticleCols;
        private IEPPHData_DAL iePPHData_DAL = new IEPPHData_DAL();
        private OverlayHelper overlayHelper;
        private float currentZoomFactor = 1.0f;
        private const float MinZoom = 0.6f;
        private const float MaxZoom = 2.0f;

        // Thứ tự cột hiển thị
        private readonly string[] desiredColumnOrder = {
            "ArticleName", "ModelName", "PCSend", "PersonIncharge", "NoteForPC",
            "OutsourcingAssemblingBool", "OutsourcingStitchingBool", "OutsourcingStockFittingBool",
            "Status", "Process", "StageName", "TypeName", "TargetOutputPC", "AdjustOperatorNo",
            "IEPPHValue", "TCTValue", "THTValue", "IsSigned",
            "SectionName", "ReferenceModel", "OperatorAdjust", "ReferenceOperator",
            "FinalOperator", "Notes"
        };

        public ucPPHData()
        {
            InitializeComponent();

            ApplyLocalizedText();
            InitZoomOverlay();

            dgvIEPPH.RowUpdated += dgvIEPPH_RowUpdated;
            dgvIEPPH.CellMerge += dgvIEPPH_CellMerge;
            dgvIEPPH.MouseWheel += dgvIEPPH_MouseWheel;
            dgvIEPPH.ShowingPopupEditForm += dgvIEPPH_ShowingPopupEditForm;

            mergeByArticleCols = new HashSet<string> {
        "ArticleName", "ModelName", "PCSend", "PersonIncharge", "OutsourcingAssemblingBool",
        "NoteForPC", "OutsourcingStitchingBool", "OutsourcingStockFittingBool", "Status"
    };
        }
        private void ApplyLocalizedText()
        {
            btnExport.Caption = Lang.Export;
        }
        public async Task LoadDataAsync()
        {
            try
            {
                UseWaitCursor = true;

                var data = await Task.Run(() => FetchData());
                LoadDataToGrid(data);
                ConfigureGridAfterDataBinding();
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError(Lang.LoadDataError, ex);
            }
            finally
            {
                UseWaitCursor = false;
            }
        }

        private async void btnImport_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            using (var openDialog = new OpenFileDialog())
            {
                openDialog.Filter = "Excel Files|*.xlsx;*.xls";

                if (openDialog.ShowDialog() != DialogResult.OK)
                    return;

                string filePath = openDialog.FileName;

                try
                {
                    // Lấy danh sách sheet
                    var sheetNames = ExcelImporter.GetSheetNames(filePath);

                    // Hiển thị form chọn sheet
                    string selectedSheet = null;
                    using (var form = new SheetSelectionForm(sheetNames))
                    {
                        if (form.ShowDialog() != DialogResult.OK || string.IsNullOrEmpty(form.SelectedSheet))
                            return;

                        selectedSheet = form.SelectedSheet;
                    }
                    int added = 0;
                    int modelInserted = 0;

                    await AsyncLoaderHelper.LoadDataWithSplashAsync(
                        this,
                        () => Task.Run(() =>
                        {
                            // Đọc dữ liệu từ Excel
                            var articles = ExcelImporter.ReadArticlesFromExcel(filePath, selectedSheet);

                            // Thêm vào bảng Articles
                            added = iePPHData_DAL.InsertIfNotExists(articles);

                            // Lấy danh sách ModelName duy nhất từ articles
                            var distinctModelNames = articles
                                .Where(x => !string.IsNullOrWhiteSpace(x.ModelName))
                                .Select(x => x.ModelName.Trim())
                                .Distinct()
                                .ToList();

                            // Thêm vào bảng TCTData nếu chưa có
                            modelInserted = TCT_DAL.InsertMissingModelNames(distinctModelNames, Common.Global.CurrentEmployee.Username);
                        }),
                        Lang.Importing
                    );
                    // Thông báo kết quả
                    var sb = new StringBuilder();
                    sb.AppendLine($"✅ {Lang.ImportSuccess}");
                    sb.AppendLine($"🆕 {Lang.Inserted}: {added} Articles.");
                    sb.AppendLine($"📦 {Lang.NewModelsAdded}: {modelInserted} Model(s) vào bảng TCT.");

                    MessageBoxHelper.ShowInfo(sb.ToString());

                    await LoadDataAsync();
                }
                catch (Exception ex)
                {
                    MessageBoxHelper.ShowError(Lang.ImportFailed, ex);
                }
            }
        }
        private void ConfigureGridAfterDataBinding()
        {
            GridViewHelper.SetupMemoEditColumn(gridControl1, dgvIEPPH, "NoteForPC");
            GridViewHelper.SetupMemoEditColumn(gridControl1, dgvIEPPH, "ModelName");
            GridViewHelper.SetupMemoEditColumn(gridControl1, dgvIEPPH, "Notes");

            GridViewHelper.SetupComboBoxColumn(
                gridControl1,
                dgvIEPPH,
                "PersonIncharge",
                ieTotalList
                    .Select(x => x.PersonIncharge?.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct()
                    .OrderBy(x => x)
            );
            GridViewHelper.SetupComboBoxColumn(gridControl1, dgvIEPPH, "Status", ieTotalList.Select(x => x.Status));
            GridViewHelper.SetupComboBoxColumn(gridControl1, dgvIEPPH, "TypeName", new[]
            {
    "Trial Pattern", "CR1", "CR2", "Presell", "SMS", "MCS", "MSO", "FGT",
    "CS1", "CS2", "CS3",
    "Production Trial", "First Production", "Mass Production"
});
            GridViewHelper.SetupComboBoxColumn(gridControl1, dgvIEPPH, "IsSigned", new[] { "Signed", "Not Sign Yet" });
            GridViewHelper.SetupComboBoxColumn(gridControl1, dgvIEPPH, "Process", iePPHData_DAL.GetProcessList());

            GridViewHelper.SetColumnCaptions(dgvIEPPH, GetCaptions());

            // Cố định chiều rộng cho NoteForPC và ModelName
            var fixedWidthCols = new Dictionary<string, int>
    {
        { "NoteForPC", 220 },
        { "ModelName", 220 }
    };

            foreach (var kvp in fixedWidthCols)
            {
                var col = dgvIEPPH.Columns[kvp.Key];
                if (col != null)
                {
                    col.Width = kvp.Value;
                    col.OptionsColumn.FixedWidth = true;
                }
            }

            dgvIEPPH.LayoutChanged();

            Task.Delay(100).ContinueWith(_ =>
            {
                BeginInvoke(new Action(() =>
                {
                    foreach (GridColumn col in dgvIEPPH.Columns)
                    {
                        if (col.FieldName != "NoteForPC" && col.FieldName != "ModelName")
                        {
                            col.BestFit();
                        }
                    }
                }));
            });

            GridViewHelper.HideColumns(dgvIEPPH, "TypeID", "IEID", "ProcessID", "StageID", "ArticleID");

            GridViewHelper.ReorderColumns(dgvIEPPH, desiredColumnOrder);
            dgvIEPPH.NewItemRowText = Lang.AddNewRowHint; 

            GridViewHelper.ConfigureGrid(
                dgvIEPPH,
                gridControl1,
                new[] { dgvIEPPH.Columns["ArticleName"], dgvIEPPH.Columns["ModelName"] },
                new[] { "" }
            );
        }
        private Dictionary<string, string> GetCaptions()
        {
            return new Dictionary<string, string>
            {
                ["IEPPHValue"] = "IE PPH",
                ["OutsourcingAssemblingBool"] = Lang.OutsourcingAssembling,
                ["OutsourcingStitchingBool"] = Lang.OutsourcingStitching,
                ["OutsourcingStockFittingBool"] = Lang.OutsourcingStockFitting,
                ["AdjustOperatorNo"] = Lang.AdjustOperator,
                ["Process"] = Lang.Process,
                ["TargetOutputPC"] = Lang.TargetOutput,
                ["IsSigned"] = Lang.ProductionSign,
                ["THTValue"] = "THT",
                ["ReferenceModel"] = Lang.ReferenceModel,
                ["OperatorAdjust"] = Lang.OperatorAdjust,
                ["ReferenceOperator"] = Lang.ReferenceOperator,
                ["FinalOperator"] = Lang.FinalOperator,
                ["TypeName"] = Lang.Type,
                ["PersonIncharge"] = Lang.PersonIncharge,
                ["ArticleName"] = Lang.ArticleName,
                ["Status"] = Lang.DataStatus,
                ["NoteForPC"] = Lang.NoteForPC,
                ["ModelName"] = Lang.ModelName,
                ["PCSend"] = Lang.PCSend,
                ["Notes"] = Lang.Notes,
                ["TCTValue"] = "TCT"
            };
        }

        private BindingList<IETotal_Model> FetchData()
        {
            var data = iePPHData_DAL.GetIEPPHData();

            ieTotalList = data; 

            foreach (var item in ieTotalList)
            {
                item.PersonIncharge = NormalizeHelper.NormalizeString(item.PersonIncharge);
            }

            ieTotalListOriginalClone = data.Select(IETotal_Model.Clone).ToList();
            return data;
        }
        private void LoadDataToGrid(BindingList<IETotal_Model> data)
        {
            ieTotalList = data;
            gridControl1.DataSource = ieTotalList;

        }
        public static int GetTypePriority(string typeName)
        {
            switch (typeName)
            {
                case "Mass Production":
                    return 0;
                case "First Production":
                    return 1;
                case "Production Trial":
                    return 2;
                default:
                    return 3;
            }
        }

        private async void btnExport_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (ieTotalList == null || ieTotalList.Count == 0)
            {
                MessageBoxHelper.ShowInfo(Lang.NoDataToExport);
                return;
            }

            var result = MessageBoxHelper.ShowConfirmYesNoCancel(
                Lang.IncludeTCTInExport,
                Lang.ExcelExportOptions);

            if (result == DialogResult.Cancel)
                return;

            bool includeTCT = (result == DialogResult.Yes);

            using (var saveDialog = new SaveFileDialog())
            {
                saveDialog.Filter = "Excel files (*.xlsx)|*.xlsx";
                saveDialog.FileName = "PPHData.xlsx";

                if (saveDialog.ShowDialog() != DialogResult.OK)
                    return;

                string originalPath = saveDialog.FileName;
                string filePath = originalPath;
                string dir = Path.GetDirectoryName(originalPath);
                string baseName = Path.GetFileNameWithoutExtension(originalPath);
                string ext = Path.GetExtension(originalPath);

                int count = 1;
                while (File.Exists(filePath))
                {
                    filePath = Path.Combine(dir, $"{baseName}_{count}{ext}");
                    count++;
                }

                await AsyncLoaderHelper.LoadDataWithSplashAsync(
                    this,
                    () =>
                    {
                        var filteredData = ieTotalList
                            .GroupBy(x => new { x.ArticleName, x.Process })
                            .Select(group =>
                            {
                                var best = group
                                    .OrderBy(x => GetTypePriority(x.TypeName?.Trim() ?? string.Empty))
                                    .First();

                                return best;
                            })
                            .ToList();
                        var convertedData = filteredData.Select(item => new IETotal_Model
                        {
                            ArticleName = item.ArticleName,
                            ModelName = item.ModelName,
                            Process = item.Process,
                            IEPPHValue = item.IEPPHValue,
                            THTValue = item.THTValue,
                            TargetOutputPC = item.TargetOutputPC,
                            AdjustOperatorNo = item.AdjustOperatorNo,
                            IsSigned = item.IsSigned,
                            TypeName = item.TypeName,
                            PersonIncharge = item.PersonIncharge,
                            NoteForPC = item.NoteForPC
                        }).ToList();
                        ExcelExporter.ExportIETotalPivoted(convertedData, filePath, includeTCT);
                        return true;
                    },
                    _ => { },
                    Lang.Exporting
                );

                MessageBoxHelper.ShowInfo(Lang.ExcelExportSuccess);

                try
                {
                    Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
                }
                catch
                {
                    MessageBoxHelper.ShowError(Lang.CannotOpenExportedFile);
                }
            }
        }
        private async void dgvIEPPH_RowUpdated(object sender, RowObjectEventArgs e)
        {
            var current = e.Row as IETotal_Model;
            if (current == null)
            {
                MessageBoxHelper.ShowError("❌ " + Lang.InvalidRow);
                return;
            }

            try
            {
                ProcessRowUpdate(current);
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError("❌ " + Lang.UpdateFailed + ": " + ex.Message);
            }
        }

        private void ProcessRowUpdate(IETotal_Model current)
        {
            int? existingArticleID = iePPHData_DAL.GetArticleIDByName(Normalize(current.ArticleName));
            bool articleExists = existingArticleID.HasValue;

            if (articleExists)
            {
                current.ArticleID = existingArticleID.Value;
                var original = FindOriginal(current);
                HandleUpdatedRecord(current, original);
            }
            else
            {
                HandleNewRecord(current);
            }

            UpdateOriginalClone(current);
        }

        private void HandleNewRecord(IETotal_Model item)
        {
            Debug.WriteLine($"[DEBUG] HandleNewRecord - ArticleName: {item.ArticleName}, ModelName: {item.ModelName}");

            if (!EnsureArticleInserted(item))
            {
                Debug.WriteLine("[DEBUG] HandleNewRecord - EnsureArticleInserted FAILED");
                throw new Exception(Lang.Error_InsertArticleFailed);
            }

            bool isMinimalData = !string.IsNullOrWhiteSpace(item.ArticleName) &&
                                 !string.IsNullOrWhiteSpace(item.ModelName) &&
                                 string.IsNullOrWhiteSpace(item.TypeName) &&
                                 string.IsNullOrWhiteSpace(item.Process);

            if (isMinimalData)
            {
                Debug.WriteLine("[DEBUG] HandleNewRecord - Minimal data, skipping dependencies");
                UserLogHelper.Log("Insert", "Articles", item.ArticleID,
                    $"Inserted Article: {item.ArticleName}, Model: {item.ModelName} (basic info)");
                return;
            }

            Debug.WriteLine("[DEBUG] HandleNewRecord - Inserting dependencies and TCT");
            EnsureArticleDependenciesInserted(item);
            EnsureTCTInsertedOrUpdated(item);

            UserLogHelper.Log("Insert", "IETotal", item.ArticleID,
                $"Inserted Article: {item.ArticleName}, Model: {item.ModelName} (with full data)");
        }
        private void HandleUpdatedRecord(IETotal_Model current, IETotal_Model original)
        {
            string username = Common.Global.CurrentEmployee.Username;
            DateTime now = DateTime.UtcNow;

            var changedProps = original != null
                ? current.GetChangedProperties(original)
                : current.GetNonEmptyProperties();

            if (changedProps.Count == 0)
            {
                Debug.WriteLine($"🔍 Không có thuộc tính nào thay đổi cho ArticleID: {current.ArticleID}");
                return;
            }

            var changedSet = new HashSet<string>(changedProps.Keys);
            LogChangedFields(current.ArticleID, changedProps, original);

            try
            {
                if (changedSet.Contains("ModelName"))
                {
                    if (!iePPHData_DAL.Update_ArticleModelName(current.ArticleID, current.ModelName))
                        throw new Exception(string.Format(Lang.Error_UpdateModelNameFailed, current.ArticleID));
                }

                if (changedSet.Contains("TypeName")) SetTypeID(current);
                if (changedSet.Contains("Process")) SetProcessID(current);

                var updateActions = new[]
                {
            (new Func<IETotal_Model, bool>(item => Upsert_ArticleProcessTypeData(item, username, now)),
             ArticleProcessFields, Lang.Error_UpdateArticleProcess),

            (new Func<IETotal_Model, bool>(item => UpdatePCIncharge(item)),
             new[] { "PersonIncharge", "PCSend" }, Lang.Error_UpdatePCIncharge),

            (new Func<IETotal_Model, bool>(iePPHData_DAL.Update_ArticleOutsourcing),
             new[] { "OutsourcingStitchingBool", "OutsourcingAssemblingBool", "OutsourcingStockFittingBool", "NoteForPC", "Status" },
             Lang.Error_UpdateOutsourcing),

            (new Func<IETotal_Model, bool>(item => { EnsureTCTInsertedOrUpdated(item); return true; }),
             TCTFields, Lang.Error_UpdateTCT)
        };

                foreach (var (func, props, msg) in updateActions)
                {
                    if (props.Any(changedSet.Contains))
                    {
                        Debug.WriteLine($"➡️ Gọi hàm cập nhật: {msg} cho ArticleID: {current.ArticleID}");
                        if (!func(current))
                            throw new Exception(string.Format(msg, current.ArticleID));
                    }
                }

                UserLogHelper.Log("Update", "IETotal", current.ArticleID,
                    $"Changes: {string.Join("; ", changedProps.Select(kv => $"{kv.Key}: {kv.Value}"))}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Lỗi khi cập nhật ArticleID={current.ArticleID}: {ex.Message}\n{ex}");
                throw;
            }
        }
        private static readonly string[] ArticleProcessFields = new[]
{
    "TargetOutputPC", "AdjustOperatorNo", "TypeName", "TCTValue", "IsSigned", "Process",
    "ReferenceModel", "OperatorAdjust", "ReferenceOperator", "FinalOperator", "Notes"
};

        private static readonly string[] TCTFields = new[]
        {
    "TCTValue", "ModelName", "TypeName", "Process"
};

        private bool UpdatePCIncharge(IETotal_Model item)
        {
            var result = iePPHData_DAL.Update_ArticlePCIncharge(item);
            if (!result) return false;

            var col = dgvIEPPH.Columns["PersonIncharge"];
            if (col?.ColumnEdit is RepositoryItemComboBox combo)
            {
                string newVal = item.PersonIncharge?.Trim();
                if (!string.IsNullOrWhiteSpace(newVal) && !combo.Items.Contains(newVal))
                    combo.Items.Add(newVal);
            }

            return true;
        }
        private void LogChangedFields(int articleID, Dictionary<string, object> changedProps, IETotal_Model original)
        {
            Debug.WriteLine($"📝 ArticleID: {articleID} - Các trường thay đổi:");
            foreach (var kv in changedProps)
            {
                var oldVal = original?.GetType().GetProperty(kv.Key)?.GetValue(original)?.ToString() ?? "(null)";
                var newVal = kv.Value?.ToString() ?? "(null)";
                Debug.WriteLine($"   🔸 {kv.Key}: [{oldVal}] → [{newVal}]");
            }
        }


        private bool Upsert_ArticleProcessTypeData(IETotal_Model item, string user, DateTime time)
        {
            return iePPHData_DAL.Exists_ArticleProcessTypeData(item.ArticleID, item.ProcessID, item.TypeID)
                ? iePPHData_DAL.Update_ArticleProcessTypeData(item, user, time)
                : iePPHData_DAL.Insert_ArticleProcessTypeData(item, user, time);
        }

        private bool ValidateArticleInput(IETotal_Model item, out string error)
        {
            error = null;

            if (string.IsNullOrWhiteSpace(item.ArticleName))
            {
                error = Lang.InvalidArticleName;
                return false;
            }

            if (string.IsNullOrWhiteSpace(item.ModelName))
            {
                error = Lang.InvalidModelName;
                return false;
            }

            return true;
        }

        private bool EnsureArticleInserted(IETotal_Model item)
        {
            Debug.WriteLine($"[DEBUG] EnsureArticleInserted - Input: ArticleName='{item.ArticleName}', ModelName='{item.ModelName}'");

            string normName = Normalize(item.ArticleName);
            Debug.WriteLine($"[DEBUG] EnsureArticleInserted - Normalized name: '{normName}'");

            int? existingID = iePPHData_DAL.GetArticleIDByName(normName);
            if (existingID.HasValue)
            {
                Debug.WriteLine($"[DEBUG] EnsureArticleInserted - Article already exists. ID = {existingID.Value}");
                item.ArticleID = existingID.Value;
                return true;
            }

            int? newID = iePPHData_DAL.Insert_Article(item.ArticleName, item.ModelName);
            Debug.WriteLine($"[DEBUG] EnsureArticleInserted - Insert result: ID = {(newID.HasValue ? newID.ToString() : "null")}");

            if (newID == null || newID <= 0)
            {
                Debug.WriteLine($"[DEBUG] EnsureArticleInserted - Insert_Article failed for '{item.ArticleName}'");
                return false;
            }

            item.ArticleID = newID.Value;
            Debug.WriteLine($"[DEBUG] EnsureArticleInserted - Success. Assigned ArticleID = {item.ArticleID}");
            return true;
        }
        private void dgvIEPPH_ValidateRow(object sender, DevExpress.XtraGrid.Views.Base.ValidateRowEventArgs e)
        {
            var view = sender as GridView;
            var item = view.GetRow(e.RowHandle) as IETotal_Model;

            if (item == null)
                return;

            if (!ValidateArticleInput(item, out string error))
            {
                e.Valid = false;
                view.SetColumnError(view.Columns[nameof(item.ArticleName)], error);
                return;
            }

            // Insert nếu hợp lệ
            if (!EnsureArticleInserted(item))
            {
                e.Valid = false;
                view.SetColumnError(view.Columns[nameof(item.ArticleName)], Lang.InsertArticleFailed);
            }
        }

        private bool EnsureArticleDependenciesInserted(IETotal_Model item)
        {
            string updatedBy = Common.Global.CurrentEmployee.Username;
            string createdBy = Common.Global.CurrentEmployee.Username;
            DateTime createdAt = DateTime.UtcNow;

            if (item.ArticleID <= 0)
            {
                MessageBoxHelper.ShowError(Lang.InvalidArticleID);
                return false;
            } 

            bool updatedOrInserted = false;

            updatedOrInserted |= InsertIfNotExist(
                () => iePPHData_DAL.Exists_ArticlePCIncharge(item.ArticleID),
                () => iePPHData_DAL.Insert_ArticlePCIncharge(item));

            updatedOrInserted |= InsertIfNotExist(
                () => iePPHData_DAL.Exists_ArticleOutsourcing(item.ArticleID),
                () => iePPHData_DAL.Insert_ArticleOutsourcing(item));

            if (!item.ProcessID.HasValue || !item.TypeID.HasValue)
            {
                MessageBoxHelper.ShowError(Lang.MissingProcessOrTypeID);
                return updatedOrInserted;
            }

            bool existsAPT = iePPHData_DAL.Exists_ArticleProcessTypeData(item.ArticleID, item.ProcessID.Value, item.TypeID.Value);
            if (existsAPT)
            {
                bool success = iePPHData_DAL.Update_ArticleProcessTypeData(item, Common.Global.CurrentEmployee.Username, DateTime.UtcNow);
                if (!success)
                {
                    MessageBoxHelper.ShowError($"❌ {Lang.Error_UpdateArticleProcess}");
                }
                else
                {
                    updatedOrInserted |= true;
                }
            }
            else
            {
                iePPHData_DAL.Insert_ArticleProcessTypeData(item, createdBy, createdAt);
                updatedOrInserted |= true;
            }

            return updatedOrInserted;
        }

        private void EnsureTCTInsertedOrUpdated(IETotal_Model item)
        {
            Debug.WriteLine($"🛠️ EnsureTCTInsertedOrUpdated gọi cho ArticleID: {item.ArticleID}");
            Debug.WriteLine($"🔹 ModelName: '{item.ModelName}'");
            Debug.WriteLine($"🔹 TypeName: '{item.TypeName}'");
            Debug.WriteLine($"🔹 Process: '{item.Process}'");

            if (string.IsNullOrWhiteSpace(item.ModelName) ||
                string.IsNullOrWhiteSpace(item.TypeName) ||
                string.IsNullOrWhiteSpace(item.Process))
            {
                Debug.WriteLine("❌ TCT: Missing required field. ModelName, TypeName, or Process is null/empty.");
                return;
            }

            string model = Normalize(item.ModelName);
            string type = Normalize(item.TypeName);
            string process = Normalize(item.Process);
            string updatedBy = Common.Global.CurrentEmployee.Username;

            if (iePPHData_DAL.Exists_TCTData(model, type, process))
            {
                Debug.WriteLine("🔄 Đã tồn tại TCTData → Gọi Update");
                iePPHData_DAL.Update_TCTData(item.ModelName, item.TypeName, item.Process, item.TCTValue, updatedBy);
            }
            else
            {
                Debug.WriteLine("🆕 Chưa có TCTData → Gọi Insert");
                iePPHData_DAL.Insert_TCTData(item.ModelName, item.TypeName, item.Process, item.TCTValue);
            }
        }

        private bool InsertIfNotExist(Func<bool> existsCheck, Action insertAction)
        {
            if (!existsCheck())
            {
                insertAction();
                return true;
            }
            return false;
        }

        private void UpdateOriginalClone(IETotal_Model item)
        {
            var index = ieTotalListOriginalClone.FindIndex(x =>
                x.ArticleID == item.ArticleID &&
                x.ProcessID == item.ProcessID &&
                x.TypeID == item.TypeID);

            if (index >= 0)
                ieTotalListOriginalClone[index] = IETotal_Model.Clone(item);
            else
                ieTotalListOriginalClone.Add(IETotal_Model.Clone(item));
        }

        private IETotal_Model FindOriginal(IETotal_Model item)
        {
            return ieTotalListOriginalClone.FirstOrDefault(x =>
                x.ArticleID == item.ArticleID &&
                x.ProcessID == item.ProcessID &&
                x.TypeID == item.TypeID);
        }

        private string Normalize(string value) => value?.Trim().ToUpper() ?? string.Empty;

        private void SetTypeID(IETotal_Model item)
        {
            if (string.IsNullOrWhiteSpace(item.TypeName))
            {
                item.TypeID = null;
                return;
            }

            var typeID = iePPHData_DAL.GetTypeID(item.TypeName);
            if (typeID == null)
            {
                MessageBoxHelper.ShowError(string.Format(Lang.TypeIDNotFound, item.TypeName));
                item.TypeID = null;
            }
            else
            {
                item.TypeID = typeID.Value;
            }
        }

        private bool SetProcessID(IETotal_Model item)
        {
            if (string.IsNullOrWhiteSpace(item.Process))
            {
                item.ProcessID = null;
                return true;
            }

            var processID = iePPHData_DAL.GetProcessID(item.Process);
            if (processID == null)
            {
                MessageBoxHelper.ShowError(string.Format(Lang.ProcessIDNotFound, item.Process));
                return false;
            }

            item.ProcessID = processID.Value;
            return true;
        }

        private void dgvIEPPH_CellMerge(object sender, CellMergeEventArgs e)
        {
            if (mergeByArticleCols.Contains(e.Column.FieldName))
            {
                var val1 = dgvIEPPH.GetRowCellValue(e.RowHandle1, "ArticleName")?.ToString();
                var val2 = dgvIEPPH.GetRowCellValue(e.RowHandle2, "ArticleName")?.ToString();
                e.Merge = val1 == val2;
            }
            else
            {
                e.Merge = false;
            }

            e.Handled = true;
        }
        private void InitZoomOverlay()
        {
            overlayHelper = new OverlayHelper(this);
        }
        private void ApplyZoom(float zoom)
        {
            var oldFont = dgvIEPPH.Appearance.Row.Font;
            float newFontSize = oldFont.Size * zoom / currentZoomFactor;

            if (Math.Abs(newFontSize - oldFont.Size) < 0.1f)
                return;

            Font zoomedFont = new Font(oldFont.FontFamily, newFontSize, oldFont.Style);

            dgvIEPPH.BeginUpdate();
            try
            {
                dgvIEPPH.Appearance.Row.Font = zoomedFont;
                dgvIEPPH.Appearance.HeaderPanel.Font = zoomedFont;
                dgvIEPPH.Appearance.FooterPanel.Font = zoomedFont;
                dgvIEPPH.Appearance.GroupRow.Font = zoomedFont;
                dgvIEPPH.Appearance.GroupFooter.Font = zoomedFont;
                dgvIEPPH.Appearance.Preview.Font = zoomedFont;

                dgvIEPPH.RowHeight = (int)(22 * zoom);
            }
            finally
            {
                dgvIEPPH.EndUpdate();
            }

            dgvIEPPH.Invalidate();
        }

        private void dgvIEPPH_MouseWheel(object sender, MouseEventArgs e)
        {
            if (Control.ModifierKeys == Keys.Control)
            {
                float zoomStep = e.Delta > 0 ? 1.05f : 0.95f;
                float newZoom = currentZoomFactor * zoomStep;
                newZoom = Math.Max(MinZoom, Math.Min(MaxZoom, newZoom));

                if (Math.Abs(newZoom - currentZoomFactor) > 0.01f)
                {
                    ApplyZoom(newZoom);
                    overlayHelper.Show($"Zoom: {(int)(newZoom * 100)}%");
                    currentZoomFactor = newZoom;
                }
            }
        }
        private void dgvIEPPH_ShowingPopupEditForm(object sender, ShowingPopupEditFormEventArgs e)
        {
            // Lấy form popup edit
            Form editForm = e.EditForm;
            if (editForm == null) return;

            editForm.StartPosition = FormStartPosition.Manual;

            // Tính toán vị trí giữa màn hình
            Rectangle screenBounds = Screen.FromControl(editForm).Bounds;
            int x = screenBounds.Left + (screenBounds.Width - editForm.Width) / 2;
            int y = screenBounds.Top + (screenBounds.Height - editForm.Height) / 2;

            // Cập nhật vị trí
            editForm.Location = new Point(x, y);
        }

        private void dgvIEPPH_KeyDown(object sender, KeyEventArgs e)
        {
            var view = sender as GridView;
            if (view == null) return;

            if (HandleDeleteKey(e, view))
                return;

            if (HandleFindShortcut(e))
                return;
        }

        private bool HandleDeleteKey(KeyEventArgs e, GridView view)
        {
            bool isDeleteKey = e.KeyCode == Keys.Delete;
            bool isCtrlMinus = e.Control && e.KeyCode == Keys.OemMinus;

            if (!(isDeleteKey || isCtrlMinus)) return false;

            int rowHandle = view.FocusedRowHandle;

            if (rowHandle < 0 || view.IsNewItemRow(rowHandle)) return false;

            string focusedColumn = view.FocusedColumn?.FieldName;
            if (string.IsNullOrEmpty(focusedColumn)) return false;

            if (focusedColumn == "ArticleName" || focusedColumn == "ModelName")
            {
                return HandleDeleteArticle(view, rowHandle);
            }
            else
            {
                return HandleDeleteOtherColumn(view, focusedColumn, rowHandle);
            }
        }

        private bool HandleDeleteArticle(GridView view, int rowHandle)
        {
            // Xác nhận xóa
            DialogResult confirm = MessageBoxHelper.ShowConfirm(
                Lang.ConfirmDeleteArticle,
                Lang.Confirm
            );
            if (confirm != DialogResult.Yes) return true;

            // Lấy dòng hiện tại
            var targetData = view.GetRow(rowHandle) as IETotal_Model;
            if (targetData == null) return true;

            // Gọi DAL để xóa mềm
            bool deleted = iePPHData_DAL.SoftDeleteArticle(targetData.ArticleID, Common.Global.CurrentEmployee.Username);
            if (!deleted)
            {
                MessageBoxHelper.ShowError(Lang.DeleteFailed);
                return true;
            }

            // Ghi log người dùng
            UserLogHelper.Log(
                action: "Soft Delete",
                table: "Articles",
                targetID: targetData.ArticleID,
                description: $"Soft deleted data of Artilce = {targetData.ArticleName}"
            );

            // Xoá toàn bộ dòng có cùng ArticleID khỏi danh sách
            var filteredList = ieTotalList
                .Where(x => x.ArticleID != targetData.ArticleID)
                .ToList();

            ieTotalList = new BindingList<IETotal_Model>(filteredList);
            gridControl1.DataSource = ieTotalList;

            // Thông báo thành công
            MessageBoxHelper.ShowInfo(Lang.DeletedSuccess);
            return true;
        }
        private bool HandleDeleteOtherColumn(GridView view, string focusedColumn, int rowHandle)
        {
            int focusedIndex = Array.IndexOf(desiredColumnOrder, focusedColumn);
            int statusIndex = Array.IndexOf(desiredColumnOrder, "Status");

            if (focusedIndex < statusIndex)
            {
                MessageBoxHelper.ShowWarning(Lang.CannotDeleteFromThisColumn);
                return true;
            }

            DialogResult confirm = MessageBoxHelper.ShowConfirm(
                Lang.ConfirmDelete,
                Lang.Confirm
            );

            if (confirm == DialogResult.Yes)
            {
                view.DeleteRow(rowHandle);
            }

            return true;
        }

        private bool HandleFindShortcut(KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.F)
            {
                dgvIEPPH.ShowFindPanel();
                dgvIEPPH.Focus();
                e.Handled = true;
                return true;
            }
            return false;
        }

        private void dgvIEPPH_RowDeleted(object sender, DevExpress.Data.RowDeletedEventArgs e)
        {
            try
            {
                if (e.Row is IETotal_Model deletedRow)
                {
                    string currentUser = Common.Global.CurrentEmployee?.Username ?? "Unknown";

                    iePPHData_DAL.DeletePPH(
                        deletedRow.ArticleID,
                        deletedRow.ProcessID,
                        deletedRow.TypeID
                    );

                    UserLogHelper.Log(
                        action: "Delete",
                        table: "ArticleProcessTypeData",
                        targetID: deletedRow.ArticleID,
                        description: $"Deleted data of Article: {deletedRow.ArticleName}, Process: {deletedRow.Process}, Type: {deletedRow.TypeName}"
                    );
                }
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError(Lang.ErrorWhileDeletingRow, ex);
            }
        }
        public void ShowFind()
        {
            dgvIEPPH.ShowFindPanel();
            dgvIEPPH.Focus();
        }


        private void ucViewPPHData_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.F)
            {
                dgvIEPPH.ShowFindPanel();
                dgvIEPPH.Focus();
                e.Handled = true;
            }
        }

        private void dgvIEPPH_EditFormPrepared(object sender, EditFormPreparedEventArgs e)
        {
            EditFormLocalizer.LocalizeButtons(e.Panel, Lang.Update, Lang.Cancel);
        }
    }
}


