# LiveChat

企業內部即時通訊（簡化版 Slack）。ASP.NET Core MVC，資料存 JSON 檔，不需資料庫。

## 如何執行

需要 .NET 10 SDK。

```powershell
dotnet run --project LiveChat
```

開啟 <http://localhost:5003>。首次啟動會把 `LiveChat/SeedData/` 複製到 `LiveChat/App_Data/`。

測試資料的時間是相對「產生當天」，clone 後或隔一段時間再用，請先重置一次。

## Mock 帳號

密碼一律為 `Passw0rd!`。

| 帳號 | 名稱 | 角色 |
| --- | --- | --- |
| `admin1` | 陳怡君 | Admin |
| `admin2` | 林志明 | Admin |
| `admin3` | 黃淑芬 | Admin |
| `member1` | 王小明 | Member |
| `member2` | 李美玲 | Member |
| `member3` | 張家豪 | Member |

## 重置測試資料

僅限 Development 環境（`dotnet run` 預設即是）。兩種方式都會重新產生 `SeedData/`、驗證通過後覆蓋 `App_Data/`：

- 畫面上方 Development 工具列的「重置測試資料」按鈕（工具列也可快速切換使用者）。
- 指令列（會印出驗證結果後結束）：

```powershell
dotnet run --project LiveChat -- --seed
```

重置會改到版控中的 `SeedData/*.json`，不需要提交時可用 `git restore LiveChat/SeedData` 還原。
