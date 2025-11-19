using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public static class MailHelper
{
    private static readonly HttpClient _httpClient = new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(5)
    };

    public static async Task PushDailyAlertDataAsync(IEnumerable<object> rows)
    {
        if (rows == null)
            throw new ArgumentException("Rows không được null", nameof(rows));

        // ✅ Chuyển sang danh sách để có thể đếm & duyệt nhiều lần
        var rowList = rows.ToList();

        // ✅ Không push nếu không có dòng nào
        if (rowList.Count == 0)
        {
            Debug.WriteLine("⚠️ Không có dữ liệu để push (rows.Count == 0).");
            return;
        }

        // ✅ Kiểm tra xem tất cả object có rỗng ({} hoặc null) không
        bool allEmpty = rowList.All(r =>
        {
            if (r == null) return true;
            string json = JsonSerializer.Serialize(r);
            return string.IsNullOrWhiteSpace(json) || json == "{}";
        });

        if (allEmpty)
        {
            Debug.WriteLine("⚠️ Dữ liệu push rỗng (toàn object trống). Bỏ qua.");
            return;
        }

        var payload = new { rows = rowList };
        string jsonData = JsonSerializer.Serialize(payload);
        var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

        try
        {
            var url = "http://10.30.0.116:3000/addDailyData";
            HttpResponseMessage response = await _httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                string body = await response.Content.ReadAsStringAsync();
                LogError($"❌ Mail collector error {response.StatusCode}: {body}", null);
            }
            else
            {
                Debug.WriteLine($"✅ Push dữ liệu cảnh báo thành công ({rowList.Count} dòng).");
            }
        }
        catch (Exception ex)
        {
            LogError("Không thể push dữ liệu cảnh báo", ex);
        }
    }

    private static void LogError(string message, Exception ex)
    {
        try
        {
            string logFile = Path.Combine(AppContext.BaseDirectory, "mail-error.log");
            string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {message}";
            if (ex != null) logEntry += $" | {ex}";
            logEntry += Environment.NewLine;

            File.AppendAllText(logFile, logEntry);
        }
        catch
        {
            // Bỏ qua lỗi ghi log
        }
    }
}
