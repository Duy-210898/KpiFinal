using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using KpiApplication.Models;

namespace KpiApplication.DataAccess
{
    public class Article_DAL
    {
        private static readonly string connectionString =
            ConfigurationManager.ConnectionStrings["strCon"].ConnectionString;

        // ✅ Lấy tất cả ModelName không trùng
        public List<string> GetDistinctModelNames()
        {
            var list = new List<string>();

            const string query = "SELECT DISTINCT ModelName FROM Articles WHERE ModelName IS NOT NULL ORDER BY ModelName";

            using (var conn = new SqlConnection(connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var modelName = reader["ModelName"]?.ToString();
                        if (!string.IsNullOrWhiteSpace(modelName))
                            list.Add(modelName);
                    }
                }
            }

            return list;
        }

        public List<string> GetModelNameExistFile()
        {
            var list = new List<string>();

            const string query = "SELECT DISTINCT ModelName FROM BonusDocuments WHERE ModelName IS NOT NULL ORDER BY ModelName";

            using (var conn = new SqlConnection(connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var modelName = reader["ModelName"]?.ToString();
                        if (!string.IsNullOrWhiteSpace(modelName))
                            list.Add(modelName);
                    }
                }
            } 

            return list;
        }

        public List<Article_Model> GetByModelName(string modelName)
        {
            if (string.IsNullOrWhiteSpace(modelName))
                return new List<Article_Model>();

            const string query = @"
        SELECT ArticleID, ArticleName, ModelName, CreatedAt
        FROM Articles
        WHERE LTRIM(RTRIM(ModelName)) = @ModelName
        ORDER BY CreatedAt DESC";

            var list = new List<Article_Model>();

            using (var conn = new SqlConnection(connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@ModelName", modelName.Trim());

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Article_Model
                        {
                            ArticleID = reader.GetInt32(0),
                            ArticleName = reader["ArticleName"]?.ToString()?.Trim(),
                            ModelName = reader["ModelName"]?.ToString()?.Trim(),
                            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
                        });
                    }
                }
            }

            return list;
        }
    }
}
