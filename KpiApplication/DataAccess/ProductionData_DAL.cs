using KpiApplication.Services;
using KpiApplication.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;
using System.Threading.Tasks;
using KpiApplication.Common;

namespace KpiApplication.DataAccess
{
    public class ProductionData_DAL
    {
        private static readonly string connectionString = ConfigurationManager.ConnectionStrings["strCon"].ConnectionString;
        public event Action OnDataChanged;

        public ProductionData_DAL()
        {
            StartSqlDependency();
        }
        public void SetUnmergeInfo(int mergeGroupId)
        {
            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    string sql = @"
                UPDATE ProductionData_New
                SET IsMerged = 0,
                    MergeGroupID = NULL
                WHERE MergeGroupID = @MergeGroupID";

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@MergeGroupID", mergeGroupId);

                        conn.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"Lỗi khi Unmerge dữ liệu: {ex.Message}");
            }
        }

        public void SetMergeInfo(int productionId, int mergeGroupId)
        {
            using (var conn = new SqlConnection(connectionString))
            using (var cmd = new SqlCommand(
                "UPDATE ProductionData_New SET IsMerged = 1, MergeGroupID = @MergeGroupID WHERE ProductionID = @ProductionID", conn))
            {
                cmd.Parameters.AddWithValue("@MergeGroupID", mergeGroupId);
                cmd.Parameters.AddWithValue("@ProductionID", productionId);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void UpdateProductionData(ProductionData data)
        {
            Debug.WriteLine($"Value: {data.IsMerged}{data.MergeGroupID} {data.ProductionID}");

            string whereClause;
            List<SqlParameter> parameters = new List<SqlParameter>();

            // Add fields to update
            var updates = new List<string>
    {
        "Total_Worker = @TotalWorker",
        "Working_Time = @WorkingTime"
    };
            parameters.Add(new SqlParameter("@TotalWorker", data.TotalWorker ?? (object) DBNull.Value));
            parameters.Add(new SqlParameter("@WorkingTime", data.WorkingTime ?? (object) DBNull.Value));

            if (data.IsMerged && data.MergeGroupID.HasValue)
            {
                whereClause = "MergeGroupID = @MergeGroupID";
                parameters.Add(new SqlParameter("@MergeGroupID", data.MergeGroupID.Value));
            }
            else
            {
                whereClause = "ProductionID = @ProductionID";
                parameters.Add(new SqlParameter("@ProductionID", data.ProductionID));
            }

            string sql = $"UPDATE ProductionData_New SET {string.Join(", ", updates)} WHERE {whereClause}";
            Debug.WriteLine("=== SQL Command ===");
            Debug.WriteLine(sql);

            foreach (var p in parameters)
                Debug.WriteLine($"{p.ParameterName} = {(p.Value == DBNull.Value ? "NULL" : p.Value)}");

            using (var conn = new SqlConnection(connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddRange(parameters.ToArray());
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }
        private void StartSqlDependency()
        {
            try
            {
                SqlDependency.Stop(connectionString);
                SqlDependency.Start(connectionString);
                ListenForChanges();
            }
            catch (Exception ex)
            {
                LogError($"Lỗi khi khởi động SqlDependency: {ex.Message}");
            }
        }

        private bool isListening = false;

        private void ListenForChanges()
        {
            if (isListening) return;

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT ArticleID, SO, PO, PROCESS, SIZE_NO, TARGET, QTY, SCAN_DATE, PRODUCTION_ORDER, IsMerged, MergeGroupID FROM dbo.ProductionData_New", conn))
                    {
                        SqlDependency dependency = new SqlDependency(cmd);
                        dependency.OnChange += new OnChangeEventHandler(OnDatabaseChange);
                        isListening = true;

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            Debug.WriteLine("📡 SqlDependency đăng ký thành công.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"❌ Lỗi khi đăng ký SqlDependency: {ex.Message}");
                isListening = false;
            }
        }

        private void OnDatabaseChange(object sender, SqlNotificationEventArgs e)
        {
            Debug.WriteLine($"🔔 Dữ liệu thay đổi: Type={e.Type}, Source={e.Source}, Info={e.Info}");
            isListening = false;
            
            if (e.Type == SqlNotificationType.Change)
            {
                OnDataChanged?.Invoke();
                ListenForChanges(); 
            }
        }
        public void Dispose()
        {
            try
            {
                SqlDependency.Stop(connectionString);
                Debug.WriteLine("🛑 SqlDependency đã dừng.");
            }
            catch (Exception ex)
            {
                LogError($"❌ Lỗi khi dừng SqlDependency: {ex.Message}");
            }
        }

        // Log lỗi
        private void LogError(string message)
        {
            Debug.WriteLine($"❌ {message}");
        }

        // Lấy tất cả dữ liệu
        public List<ProductionData> GetAllData()
        {
            List<ProductionData> productionDataList = new List<ProductionData>();

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"
                WITH RankedData AS ( 
                    SELECT 
                        P.ProductionID,
                        DP.DepartmentID,
                        P.SCAN_DATE,
                        DP.DEPARTMENT_CODE,
                        P.Process,
                        DP.Factory,
                        DP.Plant,
                        DP.LineName,
                        A.ArticleName,
                        A.ModelName,
                        P.TARGET,
                        P.QTY,
                        P.TOTAL_WORKER,
                        P.Working_Time,
                        ROUND(PS.IE_PPH_Value, 1) AS IE_PPH_Value,  
                        P.IsMerged,
                        AT.TypeName,
                        P.MergeGroupID,
                        PS.IsSigned,
                        AT.TypeID,
                        PP.ProcessName,
                        -- Ánh xạ Process thành tên chuẩn
                        CASE 
                            WHEN P.Process = 'L' THEN 'Assembly'
                            WHEN P.Process = 'S' THEN 'Stitching'
                            WHEN P.Process = 'T' THEN 'Stock Fitting'
                            ELSE NULL
                        END AS ExpectedProcessName,
                        ROW_NUMBER() OVER (
                            PARTITION BY P.ProductionID
                            ORDER BY 
                                CASE WHEN PS.IsSigned = 'Signed' THEN 0 ELSE 1 END,
                                CASE AT.TypeID
                                    WHEN 3 THEN 1 WHEN 6 THEN 2 WHEN 4 THEN 3
                                    WHEN 2 THEN 4 WHEN 7 THEN 5 WHEN 5 THEN 6
                                    WHEN 1 THEN 7 ELSE 99
                                END
                        ) AS RowNum
                    FROM [KPI-DATA].[dbo].[ProductionData_New] P
                    LEFT JOIN [KPI-DATA].[dbo].[Department] DP ON P.DepartmentID = DP.DepartmentID
                    LEFT JOIN [KPI-DATA].[dbo].[Articles] A ON P.ArticleID = A.ArticleID
                    LEFT JOIN [KPI-DATA].[dbo].[IE_PPH_Data] IE ON P.ArticleID = IE.ArticleID
                    LEFT JOIN [KPI-DATA].[dbo].[Production_Stages] PS ON PS.IE_PPH_ID = IE.IE_ID
                    LEFT JOIN [KPI-DATA].[dbo].[Process] PP ON PP.ProcessID = PS.ProcessID  
                    LEFT JOIN [KPI-DATA].[dbo].[ArtType] AT ON PS.TypeID = AT.TypeID
                    WHERE P.Process NOT IN ('IMD', 'AC', 'FI', 'CS', 'S020')
                )
                SELECT 
                    ProductionID, SCAN_DATE, DEPARTMENT_CODE, Process, Factory, Plant, LineName,
                    ArticleName, ModelName, TARGET, QTY, TOTAL_WORKER, Working_Time, 
                    IE_PPH_Value, IsMerged, TypeName, MergeGroupID, IsSigned, TypeID, ProcessName
                FROM RankedData 
                WHERE RowNum = 1 
                  AND ExpectedProcessName IS NOT NULL  -- Bỏ NULL ánh xạ (nếu có)
                  AND ProcessName = ExpectedProcessName
                ORDER BY ProductionID;
                 ";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.CommandTimeout = 300;

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            int rowCount = 0;
                            while (reader.Read())
                            {
                                rowCount++;
                                var productionDataListFromReader = ProductionDataMapper.MapFromReader(reader);
                                productionDataList.AddRange(productionDataListFromReader);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"Lỗi khi lấy dữ liệu từ SQL: {ex.Message}");
            }

            return productionDataList; 
        }
        public static List<SlidesModel> GetAllSlidesModels()
        {
            var result = new List<SlidesModel>();
            string query = "SELECT SlidesModelID, SlidesModelName FROM SlidesModelKeywords";

            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new SlidesModel
                        {
                            SlidesModelID = reader.GetInt32(0),
                            SlidesModelName = reader.GetString(1)
                        });
                    }
                }
            }

            return result;
        }
    }
}
