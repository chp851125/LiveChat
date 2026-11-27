namespace LiveChat.Services
{
    /// <summary>頻道不存在或無權進入；兩者訊息相同，避免透露私人頻道是否存在。</summary>
    public class ChannelAccessDeniedException : BusinessRuleException
    {
        public ChannelAccessDeniedException() : base("找不到頻道，或你沒有權限進入。")
        {
        }
    }
}
