using System.Collections.Concurrent;
using LiveChat.Models;

namespace LiveChat.Repositories
{
    public class JsonFileRepository<T> : IRepository<T> where T : class, IEntity
    {
        // 以完整路徑為鍵，確保同一個檔案永遠只有一把鎖
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> FileLocks =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly string _filePath;
        private readonly SemaphoreSlim _fileLock;

        public JsonFileRepository(IWebHostEnvironment environment)
        {
            _filePath = Path.GetFullPath(Path.Combine(
                environment.ContentRootPath, SeedDataInitializer.DataFolder, JsonDataFile.FileName<T>()));
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

        public void ReplaceAll(IEnumerable<T> entities) => WithLock(() =>
        {
            Save(entities.ToList());
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
        private List<T> Load() => JsonDataFile.Read<T>(_filePath);

        // 僅能在持有鎖時呼叫
        private void Save(List<T> items) => JsonDataFile.Write(_filePath, items);
    }
}
