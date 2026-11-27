using LiveChat.Models;

namespace LiveChat.Repositories
{
    public interface IRepository<T> where T : class, IEntity
    {
        IReadOnlyList<T> GetAll();

        T? GetById(Guid id);

        void Add(T entity);

        bool Update(T entity);

        bool Delete(Guid id);

        IReadOnlyList<T> Find(Func<T, bool> predicate);

        /// <summary>以指定內容整批覆蓋（重置測試資料用）。</summary>
        void ReplaceAll(IEnumerable<T> entities);
    }
}
