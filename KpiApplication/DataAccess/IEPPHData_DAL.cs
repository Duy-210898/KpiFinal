using KpiApplication.Excel;
using KpiApplication.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Media;

namespace KpiApplication.DataAccess
{
    public static class LinqExtensions
    {
        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            HashSet<TKey> seenKeys = new HashSet<TKey>();
            foreach (TSource element in source)
            {
                if (seenKeys.Add(keySelector(element)))
                    yield return element;
            }
        }
    }

    public class IEPPHData_DAL
    {
        private static readonly string connectionString = ConfigurationManager.ConnectionStrings["strCon"].ConnectionString;

        public int InsertIfNotExists(List<ExcelImporter.ArticleDto> articles)
        {
            if (articles == null || articles.Count == 0)
                return 0;

            // B1: Lấy danh sách ArticleName đã tồn tại
            var articleNames = articles.Select(x => x.ArticleName).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var existingNames = GetExistingArticleNames(articleNames);

            // B2: Lọc danh sách mới chưa tồn tại
            var newArticles = articles
                .Where(x => !existingNames.Contains(x.ArticleName, StringComparer.OrdinalIgnoreCase))
                .DistinctBy(x => x.ArticleName) // loại trùng trong file Excel
                .ToList();

            if (newArticles.Count == 0)
                return 0;

            // B3: Insert bằng SqlBulkCopy
            BulkInsertArticles(newArticles);
            return newArticles.Count;
        }

        private HashSet<string> GetExistingArticleNames(List<string> articleNames)
        {
            HashSet<string> existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (articleNames == null || articleNames.Count == 0)
                return existing;

            // Tách thành các batch 1000 để tránh lỗi nếu quá nhiều tham số
            const int batchSize = 1000;
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();

                for (int i = 0; i < articleNames.Count; i += batchSize)
                {
                    var batch = articleNames.Skip(i).Take(batchSize).ToList();
                    string inClause = string.Join(",", batch.Select((_, idx) => $"@p{idx}"));
                    string query = $"SELECT ArticleName FROM Articles WHERE ArticleName IN ({inClause})";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        for (int j = 0; j < batch.Count; j++)
                        {
                            cmd.Parameters.AddWithValue($"@p{j}", batch[j]);
                        }

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                existing.Add(reader.GetString(0));
                            }
                        }
                    }
                }
            }

            return existing;
        }

        private void BulkInsertArticles(List<ExcelImporter.ArticleDto> articles)
        {
            var table = new DataTable();
            table.Columns.Add("ArticleName", typeof(string));
            table.Columns.Add("ModelName", typeof(string));

            foreach (var a in articles)
            {
                table.Rows.Add(a.ArticleName, a.ModelName);
            }

            using (var conn = new SqlConnection(connectionString))
            using (var bulk = new SqlBulkCopy(conn))
            {
                bulk.DestinationTableName = "Articles";
                bulk.ColumnMappings.Add("ArticleName", "ArticleName");
                bulk.ColumnMappings.Add("ModelName", "ModelName");

                conn.Open();
                bulk.WriteToServer(table);
            }
        }
        #region === Helper Methods ===

        private static void AddParameterSafe(SqlCommand cmd, string name, object value)
        {
            cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }

        public List<string> GetProcessList()
        {
            var result = new List<string>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = "SELECT DISTINCT ProcessName, ProcessID FROM Process WHERE ISNULL(ProcessName, '') <> '' ORDER BY ProcessID ASC";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string process = reader.GetString(0)?.Trim();
                            if (!string.IsNullOrWhiteSpace(process))
                                result.Add(process);
                        }
                    }
                }
            }

            return result;
        }

        private bool ExecuteInsert(string tableName, Dictionary<string, object> parameters)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                var cols = string.Join(", ", parameters.Keys);
                var paramNames = string.Join(", ", parameters.Keys.Select(k => "@" + k));

                string query = $"INSERT INTO {tableName} ({cols}) VALUES ({paramNames})";

                using (var cmd = new SqlCommand(query, conn))
                {
                    foreach (var kv in parameters)
                    {
                        cmd.Parameters.AddWithValue("@" + kv.Key, kv.Value ?? DBNull.Value);
                    }

                    conn.Open();
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }
        private bool ExecuteMerge(string tableName, string[] keyColumns, Dictionary<string, object> parameters)
        {
            var updateColumns = parameters.Keys
                .Where(k => !keyColumns.Contains(k))
                .Select(k => $"{k} = @{k}");

            var mergeCondition = string.Join(" AND ", keyColumns.Select(k => $"t.{k} = s.{k}"));
            var insertColumns = string.Join(", ", parameters.Keys);
            var insertValues = string.Join(", ", parameters.Keys.Select(k => "s." + k));

            string query = $@"
        MERGE {tableName} AS t
        USING (SELECT {string.Join(", ", parameters.Keys.Select(k => "@" + k + " AS " + k))}) AS s
        ON {mergeCondition}
        WHEN MATCHED THEN
            UPDATE SET {string.Join(", ", updateColumns)}
        WHEN NOT MATCHED THEN
            INSERT ({insertColumns}) VALUES ({insertValues});";

            using (var conn = new SqlConnection(connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                foreach (var kv in parameters)
                {
                    cmd.Parameters.AddWithValue("@" + kv.Key, kv.Value ?? DBNull.Value);
                }

                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        #endregion

        #region === Delete Methods ===
        public void DeletePPH(int articleID, int? processID, int? typeID)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (var tran = conn.BeginTransaction())
                {
                    try
                    {
                        string sql = @"
DELETE FROM ArticleProcessTypeData 
WHERE ArticleID = @ArticleID 
  AND (@ProcessID IS NULL OR ProcessID = @ProcessID)
  AND (@TypeID IS NULL OR TypeID = @TypeID)";

                        using (var cmd = new SqlCommand(sql, conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@ArticleID", articleID > 0 ? (object)articleID : DBNull.Value);
                            cmd.Parameters.AddWithValue("@ProcessID", processID > 0 ? (object)processID : DBNull.Value);
                            cmd.Parameters.AddWithValue("@TypeID", typeID > 0 ? (object)typeID : DBNull.Value);

                            int affected = cmd.ExecuteNonQuery();

                        }

                        tran.Commit();
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();

                        Debug.WriteLine($"[DeletePPH] ERROR: {ex.Message}");
                        throw;
                    }
                }
            }
        }
        public bool SoftDeleteArticle(int articleID, string currentUser)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (var tran = conn.BeginTransaction())
                {
                    try
                    {
                        // 1. Lấy tên Article để ghi log
                        string articleName;
                        using (var cmd = new SqlCommand("SELECT ArticleName FROM Articles WHERE ArticleID = @ArticleID", conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@ArticleID", articleID);
                            var result = cmd.ExecuteScalar();
                            if (result == null)
                                throw new Exception("Article not found");

                            articleName = result.ToString();
                        }

                        // 2. Soft delete Article (cập nhật IsDeleted = 1)
                        using (var cmd = new SqlCommand(@"
                    UPDATE Articles
                    SET IsDeleted = 1,
                        DeletedBy = @DeletedBy,
                        DeletedAt = @DeletedAt
                    WHERE ArticleID = @ArticleID", conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@DeletedBy", currentUser);
                            cmd.Parameters.AddWithValue("@DeletedAt", DateTime.Now);
                            cmd.Parameters.AddWithValue("@ArticleID", articleID);
                            cmd.ExecuteNonQuery();
                        }
                        tran.Commit();
                        return true;
                    }
                    catch (Exception)
                    {
                        tran.Rollback();
                        return false;
                    }
                }
            }
        }

        #endregion

        #region === Lookup Methods ===

        public int? GetProcessID(string processName)
        {
            const string sql = "SELECT ProcessID FROM Process WHERE ProcessName = @ProcessName";
            using (var conn = new SqlConnection(connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                AddParameterSafe(cmd, "@ProcessName", processName);
                conn.Open();
                var result = cmd.ExecuteScalar();
                return result != null ? Convert.ToInt32(result) : (int?)null;
            }
        }

        public int? GetTypeID(string typeName)
        {
            const string sql = "SELECT TypeID FROM ArtType WHERE TypeName = @TypeName";
            using (var conn = new SqlConnection(connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                AddParameterSafe(cmd, "@TypeName", typeName);
                conn.Open();
                var result = cmd.ExecuteScalar();
                return result != null ? Convert.ToInt32(result) : (int?)null;
            }
        }
        public bool Exists_ArticlePCIncharge(int articleID)
        {
            string query = "SELECT COUNT(*) FROM Article_PCIncharge WHERE ArticleID = @ArticleID";
            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@ArticleID", articleID);
                conn.Open();
                return (int)cmd.ExecuteScalar() > 0;
            }
        }

        public bool Exists_ArticleOutsourcing(int articleID)
        {
            string query = "SELECT COUNT(*) FROM Article_Outsourcing WHERE ArticleID = @ArticleID";
            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@ArticleID", articleID);
                conn.Open();
                return (int)cmd.ExecuteScalar() > 0;
            }
        }
        public bool Exists_ArticleProcessTypeData(int articleID, int? processID, int? typeID)
        {
            string query = @"
        SELECT COUNT(*) 
        FROM ArticleProcessTypeData 
        WHERE ArticleID = @ArticleID
          AND (@ProcessID IS NULL AND ProcessID IS NULL OR ProcessID = @ProcessID)
          AND (@TypeID IS NULL AND TypeID IS NULL OR TypeID = @TypeID)";

            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@ArticleID", articleID);
                cmd.Parameters.AddWithValue("@ProcessID", processID.HasValue ? (object)processID.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@TypeID", typeID.HasValue ? (object)typeID.Value : DBNull.Value);

                conn.Open();
                return (int)cmd.ExecuteScalar() > 0;
            }
        }
        public bool Exists_TCTData(string modelName, string typeName, string process)
        {
            string query = @"
    SELECT COUNT(*) FROM [TCTData]
    WHERE ModelName = @ModelName
      AND TypeName = @TypeName
      AND Process = @Process";

            using (var conn = new SqlConnection(connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.Add("@ModelName", SqlDbType.NVarChar).Value = modelName.Trim().ToUpper();
                cmd.Parameters.Add("@TypeName", SqlDbType.NVarChar).Value = typeName.Trim().ToUpper();
                cmd.Parameters.Add("@Process", SqlDbType.NVarChar).Value = process.Trim().ToUpper();

                conn.Open();
                return (int)cmd.ExecuteScalar() > 0;
            }
        }
        public bool Update_ArticleModelName(int articleId, string newModelName)
        {
            string query = @"
        UPDATE [Articles]
        SET ModelName = @ModelName
        WHERE ArticleID = @ArticleID";

            using (var conn = new SqlConnection(connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.Add("@ModelName", SqlDbType.NVarChar).Value = newModelName;
                cmd.Parameters.Add("@ArticleID", SqlDbType.Int).Value = articleId;

                conn.Open();
                int rowsAffected = cmd.ExecuteNonQuery();
                return rowsAffected > 0;
            }
        }

        public void Insert_TCTData(string modelName, string typeName, string process, double? tctValue)
        {
            string query = @"
        INSERT INTO [TCTData] (ModelName, TypeName, Process, TCTValue, CreatedAt)
        VALUES (@ModelName, @TypeName, @Process, @TCTValue, GETDATE())";

            using (var conn = new SqlConnection(connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.Add("@ModelName", SqlDbType.NVarChar).Value = modelName;
                cmd.Parameters.Add("@TypeName", SqlDbType.NVarChar).Value = typeName;
                cmd.Parameters.Add("@Process", SqlDbType.NVarChar).Value = process;

                if (tctValue.HasValue)
                    cmd.Parameters.Add("@TCTValue", SqlDbType.Float).Value = tctValue.Value;
                else
                    cmd.Parameters.Add("@TCTValue", SqlDbType.Float).Value = DBNull.Value;

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void Update_TCTData(string modelName, string typeName, string process, double? tctValue, string updatedBy)
        {
            string query = @"
        UPDATE [TCTData]
        SET TCTValue = @TCTValue, UpdatedBy = @UpdatedBy, UpdatedAt = GETDATE()
        WHERE ModelName = @ModelName AND TypeName = @TypeName AND Process = @Process";

            using (var conn = new SqlConnection(connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.Add("@ModelName", SqlDbType.NVarChar).Value = modelName;
                cmd.Parameters.Add("@TypeName", SqlDbType.NVarChar).Value = typeName;
                cmd.Parameters.Add("@Process", SqlDbType.NVarChar).Value = process;
                cmd.Parameters.Add("@UpdatedBy", SqlDbType.NVarChar).Value = updatedBy;

                if (tctValue.HasValue)
                    cmd.Parameters.Add("@TCTValue", SqlDbType.Float).Value = tctValue.Value;
                else
                    cmd.Parameters.Add("@TCTValue", SqlDbType.Float).Value = DBNull.Value;

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        #endregion

        #region === Insert Methods ===
        public int? Insert_Article(string articleName, string modelName)
        {
            const string query = @"
        INSERT INTO Articles (ArticleName, ModelName, CreatedAt)
        OUTPUT INSERTED.ArticleID
        VALUES (@ArticleName, @ModelName, GETDATE())";

            using (var conn = new SqlConnection(connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.Add("@ArticleName", SqlDbType.NVarChar).Value = articleName.Trim();
                cmd.Parameters.Add("@ModelName", SqlDbType.NVarChar).Value = modelName.Trim();

                conn.Open();
                object result = cmd.ExecuteScalar();
                return result != null ? Convert.ToInt32(result) : (int?)null;
            }
        }

        public bool Insert_ArticleProcessTypeData(IETotal_Model item, string createdBy, DateTime? createdAt)
        {
            if (!item.ProcessID.HasValue || !item.TypeID.HasValue)
                return false;

            var parameters = new Dictionary<string, object>
            {
                ["ArticleID"] = item.ArticleID,
                ["ProcessID"] = item.ProcessID.Value,
                ["TypeID"] = item.TypeID.Value,
                ["Target"] = item.TargetOutputPC,
                ["AdjustOperator"] = item.AdjustOperatorNo,
                ["ReferenceModel"] = item.ReferenceModel,
                ["OperatorAdjust"] = item.OperatorAdjust,
                ["ReferenceOperator"] = item.ReferenceOperator,
                ["Notes"] = item.Notes,
                ["CreatedBy"] = createdBy,
                ["CreatedAt"] = createdAt,
                ["IsSigned"] = item.IsSigned
            };

            return ExecuteInsert("ArticleProcessTypeData", parameters);
        }

        public bool Insert_ArticlePCIncharge(IETotal_Model item)
        {
            var parameters = new Dictionary<string, object>
            {
                ["ArticleID"] = item.ArticleID,
                ["PersonIncharge"] = item.PersonIncharge,
                ["PCSend"] = item.PCSend
            };

            return ExecuteInsert("Article_PCIncharge", parameters);
        }

        public bool Insert_ArticleOutsourcing(IETotal_Model item)
        {
            var parameters = new Dictionary<string, object>
            {
                ["ArticleID"] = item.ArticleID,
                ["GCN_Stitching"] = item.OutsourcingStitchingBool,
                ["GCN_Assembling"] = item.OutsourcingAssemblingBool,
                ["GCN_StockFitting"] = item.OutsourcingStockFittingBool,
                ["NoteForPC"] = item.NoteForPC,
                ["DataStatus"] = item.Status
            };

            return ExecuteInsert("Article_Outsourcing", parameters);
        }
        public int? GetArticleIDByName(string articleName)
        {
            const string query = "SELECT ArticleID FROM Articles WHERE RTRIM(LTRIM(ArticleName)) = @ArticleName";

            using (var conn = new SqlConnection(connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.Add("@ArticleName", SqlDbType.NVarChar).Value = articleName.Trim();

                conn.Open();
                object result = cmd.ExecuteScalar();
                return result != null ? Convert.ToInt32(result) : (int?)null;
            }
        }

        #endregion


        #region === Update Methods ===

        public bool Update_ArticleProcessTypeData(IETotal_Model item, string updatedBy, DateTime? updatedAt = null)
        {
            var parameters = new Dictionary<string, object>
            {
                ["ArticleID"] = item.ArticleID,
                ["ProcessID"] = item.ProcessID,
                ["TypeID"] = item.TypeID,
                ["Target"] = item.TargetOutputPC,
                ["AdjustOperator"] = item.AdjustOperatorNo,
                ["ReferenceModel"] = item.ReferenceModel,
                ["OperatorAdjust"] = item.OperatorAdjust,
                ["ReferenceOperator"] = item.ReferenceOperator,
                ["Notes"] = item.Notes,
                ["IsSigned"] = item.IsSigned,
                ["LastUpdatedBy"] = updatedBy,
                ["LastUpdatedAt"] = updatedAt ?? DateTime.Now
            };

            return ExecuteMerge("ArticleProcessTypeData", new[] { "ArticleID", "ProcessID", "TypeID" }, parameters);
        }
        public bool Update_ArticlePCIncharge(IETotal_Model item)
        {
            var parameters = new Dictionary<string, object>
            {
                ["ArticleID"] = item.ArticleID,
                ["PCSend"] = item.PCSend,
                ["PersonIncharge"] = item.PersonIncharge
            };

            return ExecuteMerge("Article_PCIncharge", new[] { "ArticleID" }, parameters);
        }

        public bool Update_ArticleOutsourcing(IETotal_Model item)
        {
            var parameters = new Dictionary<string, object>
            {
                ["ArticleID"] = item.ArticleID,
                ["GCN_Stitching"] = item.OutsourcingStitchingBool,
                ["GCN_Assembling"] = item.OutsourcingAssemblingBool,
                ["GCN_StockFitting"] = item.OutsourcingStockFittingBool,
                ["NoteForPC"] = item.NoteForPC,
                ["DataStatus"] = item.Status
            };

            return ExecuteMerge("Article_Outsourcing", new[] { "ArticleID" }, parameters);
        }

        #endregion
        public BindingList<IETotal_Model> GetIEPPHData()
        {
            var ieTotal = new BindingList<IETotal_Model>();

            try
            {
                var tctDataList = GetAllTCTData();

                var tctDict = tctDataList
                    .GroupBy(t => (
                        Model: t.ModelName?.Trim().ToLowerInvariant() ?? "",
                        Process: t.Process?.Trim().ToLowerInvariant() ?? "",
                        Type: t.Type?.Trim().ToLowerInvariant() ?? ""
                    ))
                    .ToDictionary(
                        g => g.Key,
                        g => g.First().TCTValue 
                    );

                // Ghi log các key trùng (nếu có)
                var duplicates = tctDataList
                    .GroupBy(t => (
                        Model: t.ModelName?.Trim().ToLowerInvariant() ?? "",
                        Process: t.Process?.Trim().ToLowerInvariant() ?? "",
                        Type: t.Type?.Trim().ToLowerInvariant() ?? ""
                    ))
                    .Where(g => g.Count() > 1)
                    .ToList();

                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    string query = @"
                SELECT 
                    a.ArticleID,
                    a.ArticleName,
                    a.ModelName,
                    aptd.ProcessID,
                    p.ProcessName,
                    aptd.TypeID,
                    at.TypeName,
                    aptd.Target,
                    aptd.AdjustOperator,
                    aptd.ReferenceModel,
                    aptd.IsSigned,
                    aptd.OperatorAdjust,
                    aptd.ReferenceOperator,
                    aptd.Notes AS Notes_ArticleProcessType,
                    apc.PCSend,
                    apc.PersonIncharge,
                    ao.GCN_Stitching,
                    ao.GCN_Assembling,
                    ao.GCN_StockFitting,
                    ao.NoteForPC,
                    ao.DataStatus
                FROM Articles a
                LEFT JOIN ArticleProcessTypeData aptd 
                    ON a.ArticleID = aptd.ArticleID
                LEFT JOIN Process p 
                    ON aptd.ProcessID = p.ProcessID
                LEFT JOIN ArtType at 
                    ON aptd.TypeID = at.TypeID
                LEFT JOIN Article_PCIncharge apc 
                    ON a.ArticleID = apc.ArticleID
                LEFT JOIN Article_Outsourcing ao 
                    ON a.ArticleID = ao.ArticleID Where a.IsDeleted = 0
                ORDER BY a.ArticleName, aptd.ProcessID;
            ";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.CommandTimeout = 300;

                        using (var reader = cmd.ExecuteReader())
                        {
                            int rowCount = 0;

                            while (reader.Read())
                            {
                                var data = new IETotal_Model
                                {
                                    ArticleID = reader["ArticleID"] != DBNull.Value ? Convert.ToInt32(reader["ArticleID"]) : 0,
                                    ArticleName = reader["ArticleName"] as string ?? "",
                                    ModelName = reader["ModelName"] as string ?? "",

                                    PCSend = reader["PCSend"] as string ?? "",
                                    PersonIncharge = reader["PersonIncharge"] as string ?? "",
                                    NoteForPC = reader["NoteForPC"] as string ?? "",

                                    OutsourcingStitchingBool = reader["GCN_Stitching"] != DBNull.Value && Convert.ToBoolean(reader["GCN_Stitching"]),
                                    OutsourcingAssemblingBool = reader["GCN_Assembling"] != DBNull.Value && Convert.ToBoolean(reader["GCN_Assembling"]),
                                    OutsourcingStockFittingBool = reader["GCN_StockFitting"] != DBNull.Value && Convert.ToBoolean(reader["GCN_StockFitting"]),

                                    Status = reader["DataStatus"] as string ?? "",
                                    IsSigned = reader["IsSigned"] as string ?? "",

                                    ProcessID = reader["ProcessID"] != DBNull.Value ? Convert.ToInt32(reader["ProcessID"]) : 0,
                                    Process = reader["ProcessName"] as string ?? "",

                                    TypeID = reader["TypeID"] != DBNull.Value ? Convert.ToInt32(reader["TypeID"]) : 0,
                                    TypeName = reader["TypeName"] as string ?? "",

                                    TargetOutputPC = reader["Target"] != DBNull.Value ? Convert.ToInt32(reader["Target"]) : (int?)null,
                                    AdjustOperatorNo = reader["AdjustOperator"] != DBNull.Value ? Convert.ToInt32(reader["AdjustOperator"]) : (int?)null,

                                    ReferenceModel = reader["ReferenceModel"] as string ?? "",
                                    OperatorAdjust = reader["OperatorAdjust"] != DBNull.Value ? Convert.ToInt32(reader["OperatorAdjust"]) : (int?)null,
                                    ReferenceOperator = reader["ReferenceOperator"] != DBNull.Value ? Convert.ToInt32(reader["ReferenceOperator"]) : (int?)null,
                                    Notes = reader["Notes_ArticleProcessType"] as string ?? ""
                                };

                                // Tạo key để tra cứu TCT
                                var key = (
                                    Model: data.ModelName.Trim().ToLowerInvariant(),
                                    Process: data.Process.Trim().ToLowerInvariant(),
                                    Type: data.TypeName.Trim().ToLowerInvariant()
                                );

                                if (tctDict.TryGetValue(key, out var tctValue))
                                {
                                    data.TCTValue = tctValue;
                                }
                                rowCount++;
                                ieTotal.Add(data);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Lỗi khi lấy dữ liệu IEPPH:\n{ex}");
            }

            return ieTotal;
        }

        public List<TCTData_Model> GetAllTCTData()
        {
            var list = new List<TCTData_Model>();

            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    string query = "SELECT * FROM TCTData";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var item = new TCTData_Model
                                {
                                    TCTID = reader["TCTID"] != DBNull.Value ? Convert.ToInt32(reader["TCTID"]) : 0,
                                    ModelName = reader["ModelName"] as string ?? string.Empty,
                                    Type = reader["TypeName"] as string ?? string.Empty,
                                    Process = reader["Process"] as string ?? string.Empty,
                                    TCTValue = reader["TCTValue"] != DBNull.Value ? (double?)Convert.ToDouble(reader["TCTValue"]) : null
                                };
                                list.Add(item);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"⚠️ Lỗi khi lấy dữ liệu TCTData: {ex.Message}");
            }

            return list;
        }

        public BindingList<IEPPHDataForUser_Model> GetIEPPHDataForUser(string modelName = null, int? processId = null, string dataStatus = null)
        {
            var iePPHList = new BindingList<IEPPHDataForUser_Model>();

            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // Tạo câu truy vấn động dựa trên tham số truyền vào
                    var queryBuilder = new StringBuilder(@"
                SELECT 
                    a.ArticleID,
                    a.ArticleName,
                    a.ModelName,
                    aptd.ProcessID,
                    p.ProcessName,
                    aptd.TypeID,
                    at.TypeName,
                    aptd.Target,
                    aptd.AdjustOperator,
                    aptd.ReferenceModel,
                    aptd.IsSigned,
                    aptd.OperatorAdjust,
                    aptd.ReferenceOperator,
                    aptd.Notes AS Notes_ArticleProcessType,
                    apc.PCSend,
                    apc.PersonIncharge,
                    ao.GCN_Stitching,
                    ao.GCN_Assembling,
                    ao.GCN_StockFitting,
                    ao.NoteForPC,
                    ao.DataStatus
                FROM Articles a
                LEFT JOIN ArticleProcessTypeData aptd ON a.ArticleID = aptd.ArticleID
                LEFT JOIN Process p ON aptd.ProcessID = p.ProcessID
                LEFT JOIN ArtType at ON aptd.TypeID = at.TypeID
                LEFT JOIN Article_PCIncharge apc ON a.ArticleID = apc.ArticleID
                LEFT JOIN Article_Outsourcing ao ON a.ArticleID = ao.ArticleID
                Where a.IsDeleted = 0  
                    ");

                    var cmd = new SqlCommand();
                    if (!string.IsNullOrEmpty(modelName))
                    {
                        queryBuilder.Append(" AND a.ModelName = @ModelName");
                        cmd.Parameters.AddWithValue("@ModelName", modelName);
                    }

                    if (processId.HasValue)
                    {
                        queryBuilder.Append(" AND aptd.ProcessID = @ProcessID");
                        cmd.Parameters.AddWithValue("@ProcessID", processId.Value);
                    }

                    if (!string.IsNullOrEmpty(dataStatus))
                    {
                        queryBuilder.Append(" AND ao.DataStatus = @DataStatus");
                        cmd.Parameters.AddWithValue("@DataStatus", dataStatus);
                    }

                    queryBuilder.Append(" ORDER BY a.ArticleName, aptd.ProcessID;");

                    cmd.CommandText = queryBuilder.ToString();
                    cmd.Connection = conn;
                    cmd.CommandTimeout = 300;

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var data = new IEPPHDataForUser_Model
                            {
                                ArticleName = reader["ArticleName"] as string ?? "",
                                ModelName = reader["ModelName"] as string ?? "",
                                PCSend = reader["PCSend"] as string ?? "",
                                PersonIncharge = reader["PersonIncharge"] as string ?? "",
                                NoteForPC = reader["NoteForPC"] as string ?? "",
                                DataStatus = reader["DataStatus"] as string ?? "",
                                Process = reader["ProcessName"] as string ?? "",
                                TypeName = reader["TypeName"] as string ?? "",
                                IsSigned = reader["IsSigned"] as string ?? "",

                                TargetOutputPC = reader["Target"] != DBNull.Value ? Convert.ToInt32(reader["Target"]) : (int?)null,
                                AdjustOperatorNo = reader["AdjustOperator"] != DBNull.Value ? Convert.ToInt32(reader["AdjustOperator"]) : (int?)null,

                                OutsourcingAssemblingBool = reader["GCN_Assembling"] != DBNull.Value && Convert.ToBoolean(reader["GCN_Assembling"]),
                                OutsourcingStitchingBool = reader["GCN_Stitching"] != DBNull.Value && Convert.ToBoolean(reader["GCN_Stitching"]),
                                OutsourcingStockFittingBool = reader["GCN_StockFitting"] != DBNull.Value && Convert.ToBoolean(reader["GCN_StockFitting"]),
                            };

                            // Tính IE_PPH nếu có đủ dữ liệu
                            if (data.TargetOutputPC.HasValue && data.AdjustOperatorNo.HasValue && data.AdjustOperatorNo.Value != 0)
                            {
                                data.IEPPHValue = Math.Round((double)data.TargetOutputPC.Value / data.AdjustOperatorNo.Value, 2);
                            }

                            iePPHList.Add(data);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"⚠️ Lỗi khi lấy dữ liệu IEPPH: {ex}");
            }

            return iePPHList;
        }
    }
}
