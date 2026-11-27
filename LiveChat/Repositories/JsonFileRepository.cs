using System.Collections.Concurrent;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using LiveChat.Models;

namespace LiveChat.Repositories
{
    public class JsonFileRepository<T> : IRepository<T> where T : class, IEntity
    {
        // 以完整路徑為鍵，確保同一個檔案永遠只有一把鎖
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> FileLocks =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
            Converters = { new JsonStringEnumConverter() },
            WriteIndented = true
        };

        private readonly string _filePath;
        private readonly SemaphoreSlim _fileLock;

        public JsonFileRepository(IWebHostEnvironment environment)
        {
            var fileName = $"{JsonNamingPolicy.CamelCase.ConvertName(typeof(T).Name)}s.json";

            _filePath = Path.GetFullPath(
                Path.Combine(environment.ContentRootPath, SeedDataInitializer.DataFolder, fileName));
            _fileLock = FileLocks.GetOrAdd(_filePath, _ => new SemaphoreSlim(1, 1));
        }

        public IReadOnlyList<T> GetAll() => WithLock(Load);

        public T? GetById(Guid id) => WithLock(() => Load().FirstOrDefault(x => x.Id == id));

        public IReadOnlyList<T> Find(Func<T, bool> predicate) =>
            WithLock(() => Load().Where(predicate).ToList());

        public void Add(T entity) => WithLock(() =>
        {
            var items = Load();

            if (entity.Id == Guid.Empty)
            {
                entity.Id = Guid.NewGuid();
            }
            else if (items.Any(x => x.Id == entity.Id))
            {
                throw new InvalidOperationException($"{typeof(T).Name} 的 Id 重複：{entity.Id}");
            }

            items.Add(entity);
            Save(items);
            return true;
        });

        public bool Update(T entity) => WithLock(() =>
        {
            var items = Load();
            var index = items.FindIndex(x => x.Id == entity.Id);
            if (index < 0)
            {
                return false;
            }

            items[index] = entity;
            Save(items);
            return true;
        });

        public bool Delete(Guid id) => WithLock(() =>
        {
            var items = Load();
            if (items.RemoveAll(x => x.Id == id) == 0)
            {
                return false;
            }

            Save(items);
            return true;
        });

        private TResult WithLock<TResult>(Func<TResult> action)
        {
            _fileLock.Wait();
            try
            {
                return action();
            }
            finally
            {
                _fileLock.Release();
            }
        }

        // 僅能在持有鎖時呼叫
        private List<T> Load()
        {
            if (!File.Exists(_filePath))
            {
                return new List<T>();
            }

            using var stream = File.OpenRead(_filePath);
            return JsonSerializer.Deserialize<List<T>>(stream, JsonOptions) ?? new List<T>();
        }

        // 僅能在持有鎖時呼叫；先寫暫存檔再覆蓋，避免寫到一半損毀原檔
        private void Save(List<T> items)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);

            var tempPath = _filePath + ".tmp";
            using (var stream = File.Create(tempPath))
            {
                JsonSerializer.Serialize(stream, items, JsonOptions);
                stream.Flush(flushToDisk: true);
            }

            File.Move(tempPath, _filePath, overwrite: true);
        }
    }
}
