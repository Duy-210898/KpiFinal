using KpiApplication.Models;
using KpiApplication.Services;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;

namespace KpiApplication.DataAccess
{
    public class ProductionData_DAL
    {
        private static readonly string connectionString = ConfigurationManager.ConnectionStrings["strCon"].ConnectionString;
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

        public void UpdateProductionData(ProductionData_Model data)
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
        public void BatchSetMergeInfo(List<ProductionData_Model> items)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                using (SqlCommand cmd = conn.CreateCommand())
                {
                    foreach (var item in items)
                    {
                        cmd.CommandText = "UPDATE ProductionData_New SET MergeGroupID = @GroupID, IsMerged = 1 WHERE ProductionID = @ProdID";
                        cmd.Parameters.Clear();
                        cmd.Parameters.AddWithValue("@GroupID", item.MergeGroupID.Value);
                        cmd.Parameters.AddWithValue("@ProdID", item.ProductionID);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        // Lấy tất cả dữ liệu
        public List<ProductionData_Model> GetAllData()
        {
            List<ProductionData_Model> productionDataList = new List<ProductionData_Model>();

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"
WITH RankedAPTD AS (
    SELECT
        APTD.ArticleID,
        APTD.ProcessID,
        PP.ProcessName,
        APTD.Target AS APTDTarget,
        APTD.AdjustOperator,
        APTD.IsSigned,
        AT.TypeID,
        ROW_NUMBER() OVER (
            PARTITION BY APTD.ArticleID, PP.ProcessName
            ORDER BY
                CASE 
                    WHEN APTD.IsSigned = 'Signed' AND AT.TypeID = 3 THEN 1
                    WHEN AT.TypeID = 4 THEN 2
                    WHEN AT.TypeID = 2 THEN 3
                    ELSE 99
                END
        ) AS RowNum
    FROM [KPI-DATA].[dbo].ArticleProcessTypeData APTD
    LEFT JOIN [KPI-DATA].[dbo].Process PP ON APTD.ProcessID = PP.ProcessID
    LEFT JOIN [KPI-DATA].[dbo].ArtType AT ON APTD.TypeID = AT.TypeID
)
SELECT 
    P.ProductionID,
    P.SCAN_DATE,
    DP.DEPARTMENT_CODE,
    P.Process,
    DP.Factory,
    DP.Plant,
    DP.LineName,
    A.ArticleName,
    A.ModelName,
    P.TARGET AS ProductionTarget,
    P.QTY,
    P.TOTAL_WORKER,
    P.Working_Time,
    R.APTDTarget AS [Target],
    R.AdjustOperator,
    P.IsMerged,
    P.MergeGroupID,
    R.IsSigned,
    AT.TypeName
FROM [KPI-DATA].[dbo].ProductionData_New P
LEFT JOIN [KPI-DATA].[dbo].Department DP ON P.DepartmentID = DP.DepartmentID
LEFT JOIN [KPI-DATA].[dbo].Articles A ON P.ArticleID = A.ArticleID
LEFT JOIN RankedAPTD R 
    ON P.ArticleID = R.ArticleID 
    AND R.RowNum = 1
    AND (
        (P.Process = 'C' AND R.ProcessName = 'Cutting') OR
        (P.Process = 'S' AND R.ProcessName = 'Stitching') OR
        (P.Process = 'L' AND R.ProcessName = 'Assembly') OR
        (P.Process = 'T' AND R.ProcessName = 'Stock Fitting')
    )
LEFT JOIN [KPI-DATA].[dbo].ArtType AT ON R.TypeID = AT.TypeID
ORDER BY P.ProductionID;

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
                                var productionDataListFromReader = ProductionDataMapperService.MapFromReader(reader);
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
        public static List<Slides_Model> GetAllSlidesModels()
        {
            var result = new List<Slides_Model>();
            string query = "SELECT SlidesModelID, SlidesModelName FROM SlidesModelKeywords";

            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new Slides_Model
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
