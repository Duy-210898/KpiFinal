using System;
using System.Configuration;
using System.Data.SqlClient;

namespace KpiApplication.Utils
{
    public static class UserLogHelper
    {
        private static readonly string connectionString = ConfigurationManager.ConnectionStrings["strCon"].ConnectionString;

        /// <summary>
        /// Ghi log thao tác của người dùng vào bảng UserActivities
        /// </summary>
        /// <param name="action">Tên hành động (Insert, Update, Delete...)</param>
        /// <param name="table">Tên bảng bị tác động</param>
        /// <param name="targetID">ID đối tượng (nullable)</param>
        /// <param name="description">Mô tả chi tiết hành động</param>
        /// <param name="tran">Transaction hiện tại (nếu có)</param>
        public static void Log(string action, string table, int? targetID, string description, SqlTransaction tran = null)
        {
            bool ownsConnection = false;
            SqlConnection conn = tran?.Connection;

            if (conn == null)
            {
                conn = new SqlConnection(connectionString);
                conn.Open();
                ownsConnection = true;
            }

            try
            {
                using (var cmd = new SqlCommand(@"
                    INSERT INTO UserActivities
                        (Username, Action, TargetTable, TargetID, Timestamp, Description)
                    VALUES 
                        (@Username, @Action, @TargetTable, @TargetID, @Timestamp, @Description)", conn, tran))
                {
                    cmd.Parameters.AddWithValue("@Username", Common.Global.CurrentEmployee?.Username ?? "Unknown");
                    cmd.Parameters.AddWithValue("@Action", action);
                    cmd.Parameters.AddWithValue("@TargetTable", table);
                    cmd.Parameters.AddWithValue("@TargetID", (object)targetID ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Timestamp", DateTime.Now);
                    cmd.Parameters.AddWithValue("@Description", description ?? string.Empty);
                    cmd.ExecuteNonQuery();
                }
            }
            finally
            {
                if (ownsConnection)
                {
                    conn.Dispose();
                }
            }
        }
    }
}
