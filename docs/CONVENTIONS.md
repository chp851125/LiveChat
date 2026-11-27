# CONVENTIONS — LiveChat

命名與結構規則。每階段結尾更新；要改規則，先改本文件再改程式。

## 資料夾（位於 `LiveChat/`）

| 資料夾 | 內容 |
| --- | --- |
| `Controllers/` | MVC Controller |
| `Services/` | 商業規則 |
| `Repositories/` | 資料存取 |
| `Models/` | 實體與列舉 |
| `ViewModels/` | 畫面用模型 |
| `Hubs/` | SignalR Hub |
| `Views/` | Razor 畫面 |
| `SeedData/` | 初始 JSON（進版控） |
| `App_Data/` | 執行期資料（首次啟動產生，不進版控） |

尚無內容的資料夾，等有檔案時再建立。

## 分層規則

- Service 介面 `IXxxService`，實作 `XxxService`，放 `Services/`。
- 規則一律寫在 Service，Controller 與 Hub 只負責呼叫與回應。
- 錯誤訊息用繁體中文，統一由 Service 丟出 `BusinessRuleException`（含訊息）。
- 所有時間存 UTC，顯示時才轉本地。

## 程式風格

- namespace 對應資料夾（`LiveChat.Models`、`LiveChat.Repositories`），沿用範本的區塊式 `namespace X { }`。
- DI 在 `Program.cs` 註冊；Service 註冊在「Service 註冊區」。

## 資料模型（`Models/`）

- 每個實體一個檔案，實作 `IEntity`（`Guid Id`）。
- 外鍵命名 `XxxId`（`Guid`）；時間欄位型別 `DateTime`，程式內一律用 `DateTime.UtcNow`。
- 實體：`User`、`Channel`、`ChannelMember`、`Message`、`ReadMarker`、`Mute`、`AuditLog`。
- 列舉：`UserRole`（`Admin` / `Member`）、`ChannelType`（`Public` / `Private` / `Direct`）。

## 資料層（`Repositories/`）

- `IRepository<T>`：`GetAll`、`GetById`、`Add`、`Update`、`Delete`、`Find(predicate)`，皆為同步方法。
  - `Add`：`Id` 為 `Guid.Empty` 時自動產生；`Id` 重複丟 `InvalidOperationException`。
  - `Update`、`Delete`：找不到回傳 `false`。
  - 每次讀取都回傳新物件，修改後要呼叫 `Update` 才會寫入。
- `JsonFileRepository<T>`：以 Singleton 註冊；每個 JSON 檔一把 `SemaphoreSlim`，讀寫都鎖；先寫 `.tmp` 再覆蓋。
  - 單次方法呼叫是執行緒安全的；「先查再寫」這類多步驟規則（例如 R3、R9），由 Service 自行加鎖。
  - 僅支援單一程序存取同一份 `App_Data/`。
- 檔名：實體名稱轉 camelCase 加 `s`（`User` → `users.json`、`ChannelMember` → `channelMembers.json`）。
- `SeedDataInitializer`：啟動時把 `App_Data/` 缺少的檔案從 `SeedData/` 複製過去，既有檔案不覆蓋。
- 新增實體時，要同時新增對應的 `SeedData/xxx.json`。

## JSON 格式

- 屬性名稱 camelCase；列舉存字串；中文不跳脫。
- 時間用 ISO 8601 並帶 `Z`（例：`2026-01-01T08:00:00Z`），手寫 SeedData 時也一樣。
