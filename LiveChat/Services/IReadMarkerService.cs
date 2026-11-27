namespace LiveChat.Services
{
    /// <summary>已讀標記與未讀數（R10）。</summary>
    public interface IReadMarkerService
    {
        /// <summary>我加入的每個頻道（含私訊與已封存）的未讀數。</summary>
        IReadOnlyDictionary<Guid, int> GetUnreadCounts(Guid userId);

        /// <summary>把該頻道的已讀時間更新為現在。非成員不做任何事。</summary>
        void MarkAsRead(Guid channelId, Guid userId);
    }
}
