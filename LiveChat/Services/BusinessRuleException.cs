namespace LiveChat.Services
{
    /// <summary>商業規則不成立；Message 為可直接顯示給使用者的繁體中文訊息。</summary>
    public class BusinessRuleException : Exception
    {
        public BusinessRuleException(string message) : base(message)
        {
        }
    }
}
