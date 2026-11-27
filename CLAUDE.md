# CLAUDE.md — LiveChat

## 開工前
- 先讀 `docs/REQUIREMENTS.md` 與 `docs/CONVENTIONS.md`（若尚未建立，依該階段 prompt 指示建立）。
- 命名與結構一律遵守 CONVENTIONS.md，不得自行改名；要改規則先更新 CONVENTIONS.md 並在摘要中說明。

## 專案原則
- ASP.NET Core MVC + SignalR。沿用現有空白專案，不更動 .NET 版本。
- 免費、不接外部服務、不用資料庫與 EF Core。資料用 JSON：`SeedData/*.json`，首次啟動複製到 `App_Data/`（App_Data 要在 .gitignore）。
- 保留 `IRepository<T>` + `JsonFileRepository<T>`。
- 不建測試專案、不建 CI yml、不做小遊戲。
- clone 後 `dotnet run` 就能跑，不需額外安裝步驟。

## 工作方式
- 小修改用局部編輯，不整檔重寫。
- 對話只回報摘要（做了什麼、改了哪些檔、有何疑問），不貼完整程式碼。
- 一個階段只做該階段 prompt 指定的範圍，不提前做後面階段的功能。
- 不確定時停下來問，不要自行假設。

## 階段結尾固定動作
1. `dotnet build` 無錯誤、無新增警告。
2. 依該階段 prompt 的檢查項目實際執行驗證，回報通過/未通過。
3. 更新 `docs/CONVENTIONS.md`（新增的命名、結構、共用元件）。
4. commit → push → 開 PR。

## Git 規則
- 每個功能或階段開一個分支，分支名稱依 prompt 指定。
- 完成後由你自己 push 並開 PR。
- 若環境沒有 `gh` CLI：停下來問我，並附上「PR 標題」與「PR 內文」，讓我貼到 GitHub 網頁。
- merge 與刪除分支一律由我處理，你不要做。
- commit 訊息簡短明確，一個 commit 只做一件事。
