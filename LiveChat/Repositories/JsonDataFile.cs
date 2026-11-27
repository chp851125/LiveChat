using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;

namespace LiveChat.Repositories
{
    /// <summary>資料 JSON 檔的命名與讀寫格式。本身不上鎖，鎖由呼叫端負責。</summary>
    public static class JsonDataFile
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
            Converters = { new JsonStringEnumConverter() },
            WriteIndented = true
        };

        public static string FileName<T>() =>
            $"{JsonNamingPolicy.CamelCase.ConvertName(typeof(T).Name)}s.json";

        public static List<T> Read<T>(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return new List<T>();
            }

            using var stream = File.OpenRead(filePath);
            return JsonSerializer.Deserialize<List<T>>(stream, JsonOptions) ?? new List<T>();
        }

        /// <summary>先寫暫存檔再覆蓋，避免寫到一半損毀原檔。</summary>
        public static void Write<T>(string filePath, IEnumerable<T> items)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

            var tempPath = filePath + ".tmp";
            using (var stream = File.Create(tempPath))
            {
                JsonSerializer.Serialize(stream, items.ToList(), JsonOptions);
                stream.Flush(flushToDisk: true);
            }

            File.Move(tempPath, filePath, overwrite: true);
        }
    }
}
