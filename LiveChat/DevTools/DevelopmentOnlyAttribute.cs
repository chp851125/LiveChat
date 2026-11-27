namespace LiveChat.DevTools
{
    /// <summary>標記只在 Development 環境存在的 Controller。</summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class DevelopmentOnlyAttribute : Attribute
    {
    }
}
