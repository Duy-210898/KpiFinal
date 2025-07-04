using KpiApplication.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;

namespace KpiApplication.DataAccess
{
    public class WeeklyPlan_DAL
    {
        private static readonly string connectionString = ConfigurationManager.ConnectionStrings["strCon"].ConnectionString;

        public HashSet<(string ArticleName, string ModelName)> GetAllArticles()
        {
            var result = new HashSet<(string, string)>();

            using (var conn = new SqlConnection(connectionString))
            using (var cmd = new SqlCommand("SELECT ArticleName, ModelName FROM Articles", conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string articleName = reader.GetString(0);
                        string modelName = reader.GetString(1);
                        result.Add((articleName, modelName));
                    }
                }
            }

            return result;
        }
        public List<(int ArticleID, string ArticleName, string ModelName)> BulkInsertArticles(List<(string ArticleName, string ModelName)> newArticles)
        {
            var insertedArticles = new List<(int, string, string)>();

            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();

                foreach (var article in newArticles)
                {   
                    string insertQuery = @"
                INSERT INTO Articles (ArticleName, ModelName) 
                VALUES (@ArticleName, @ModelName);
            ";

                    using (var cmd = new SqlCommand(insertQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@ArticleName", article.ArticleName);
                        cmd.Parameters.AddWithValue("@ModelName", article.ModelName);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int articleID = reader.GetInt32(0);
                                string articleName = reader.GetString(1);
                                string modelName = reader.GetString(2);

                                insertedArticles.Add((articleID, articleName, modelName));
                            }
                        }
                    }
                }
            }

            return insertedArticles;
        }
        public List<int> InsertArticleIDsToIE_PPH_Data(List<int> articleIDs)
        {
            var insertedIEIDs = new List<int>();

            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
            INSERT INTO IE_PPH_Data (ArticleID)
            OUTPUT INSERTED.IE_ID
            VALUES (@ArticleID);
        ";

                using (var cmd = new SqlCommand(query, conn))
                {
                    var param = cmd.Parameters.Add("@ArticleID", SqlDbType.Int);

                    foreach (int articleID in articleIDs)
                    {
                        param.Value = articleID;
                        var insertedId = (int)cmd.ExecuteScalar();
                        insertedIEIDs.Add(insertedId);
                    }
                }
            }

            return insertedIEIDs;
        }
        public void InsertIEIDsToProductionStages(List<int> ieIDs)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();

                // Step 1: Lấy toàn bộ ProcessID 1 lần
                List<int> allProcessIDs = new List<int>();
                using (var getProcCmd = new SqlCommand("SELECT ProcessID FROM Process", conn))
                using (var reader = getProcCmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        allProcessIDs.Add(reader.GetInt32(0));
                    }
                }
            }
        }

        public List<(string ArticleName, string ModelName, int Week, int Month, int Year)> GetAllKeys()
        {
            var keys = new List<(string, string, int, int, int)>();

            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT ArticleName, ModelName, Week, Month, Year FROM WeeklyPlan";
                using (var cmd = new SqlCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var key = (
                            reader["ArticleName"]?.ToString() ?? "",
                            reader["ModelName"]?.ToString() ?? "",
                            Convert.ToInt32(reader["Week"]),
                            Convert.ToInt32(reader["Month"]),
                            Convert.ToInt32(reader["Year"])
                        );
                        keys.Add(key);
                    }
                }
            }

            return keys;
        }
        public void BulkInsertWeeklyPlans(List<WeeklyPlanData_Model> newItems)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();

                foreach (var item in newItems)
                {
                    string insertQuery = @"
                INSERT INTO WeeklyPlan
                    (ArticleName, ModelName, Week, Month, Year, Stitching, Assembling, StockFitting, BPFC)
                VALUES
                    (@ArticleName, @ModelName, @Week, @Month, @Year, @Stitching, @Assembling, @StockFitting, @BPFC)";

                    using (var insertCmd = new SqlCommand(insertQuery, conn))
                    {
                        insertCmd.Parameters.AddWithValue("@ArticleName", item.ArticleName ?? (object)DBNull.Value);
                        insertCmd.Parameters.AddWithValue("@ModelName", item.ModelName ?? (object)DBNull.Value);
                        insertCmd.Parameters.AddWithValue("@Week", item.Week);
                        insertCmd.Parameters.AddWithValue("@Month", item.Month);
                        insertCmd.Parameters.AddWithValue("@Year", item.Year);
                        insertCmd.Parameters.AddWithValue("@Stitching", item.Stitching ?? (object)DBNull.Value);
                        insertCmd.Parameters.AddWithValue("@Assembling", item.Assembling ?? (object)DBNull.Value);
                        insertCmd.Parameters.AddWithValue("@StockFitting", item.StockFitting ?? (object)DBNull.Value);
                        insertCmd.Parameters.AddWithValue("@BPFC", item.BPFC ?? (object)DBNull.Value);

                        insertCmd.ExecuteNonQuery();
                    }
                }
            }
        }
        public BindingList<WeeklyPlanData_Model> GetWeeklyPlanData()
        {
            var weeklyList = new BindingList<WeeklyPlanData_Model>();

            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    string query = @"
                SELECT ArticleName
                      ,ModelName
                      ,Week
                      ,Month
                      ,Year
                      ,Stitching
                      ,Assembling
                      ,StockFitting
                      ,BPFC
                FROM WeeklyPlan
            ";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.CommandTimeout = 300;

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var data = new WeeklyPlanData_Model
                                {
                                    ArticleName = reader["ArticleName"] as string,
                                    ModelName = reader["ModelName"] as string,
                                    Week = reader["Week"] as int?,
                                    Month = reader["Month"] as int?,
                                    Year = reader["Year"] as int?,
                                    Stitching = reader["Stitching"] as string,
                                    Assembling = reader["Assembling"] as string,
                                    StockFitting = reader["StockFitting"] as string,
                                    BPFC = reader["BPFC"] as string,
                                };

                                weeklyList.Add(data);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"⚠️ Lỗi khi lấy dữ liệu WeeklyPlan: {ex.Message}");
            }

            return weeklyList;
        }
    }
}
