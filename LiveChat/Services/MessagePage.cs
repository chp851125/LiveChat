namespace LiveChat.Services
{
    /// <summary>某人眼中的一頁訊息（舊到新）。</summary>
    public record MessagePage(IReadOnlyList<MessageEntry> Messages, bool HasOlder);

    /// <summary>某人眼中的一則訊息。已刪除時 Content 為空字串；CanEdit / CanDelete 為查詢當下的結果。</summary>
    public record MessageEntry(
        Guid Id,
        Guid SenderId,
        string SenderName,
        string Content,
        DateTime CreatedAt,
        bool IsEdited,
        bool IsDeleted,
        bool CanEdit,
        bool CanDelete);
}
