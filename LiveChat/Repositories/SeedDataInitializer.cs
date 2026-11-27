namespace LiveChat.Repositories
{
    public static class SeedDataInitializer
    {
        public const string SeedFolder = "SeedData";
        public const string DataFolder = "App_Data";

        /// <summary>把 App_Data 缺少的 JSON 檔從 SeedData 複製過去；既有檔案不覆蓋。</summary>
        public static void Initialize(string contentRootPath)
        {
            var seedPath = Path.Combine(contentRootPath, SeedFolder);
            var dataPath = Path.Combine(contentRootPath, DataFolder);
            Directory.CreateDirectory(dataPath);

            if (!Directory.Exists(seedPath))
            {
                return;
            }

            foreach (var source in Directory.GetFiles(seedPath, "*.json"))
            {
                var target = Path.Combine(dataPath, Path.GetFileName(source));
                if (File.Exists(target))
                {
                    continue;
                }

                var tempPath = target + ".tmp";
                File.Copy(source, tempPath, overwrite: true);
                File.Move(tempPath, target);
            }
        }
    }
}
