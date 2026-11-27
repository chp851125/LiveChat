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
| `ViewComponents/` | View Component |
| `DevTools/` | 僅 Development 使用的工具（測試資料產生、驗證、重置） |
| `Hubs/` | SignalR Hub |
| `Views/` | Razor 畫面 |
| `SeedData/` | 初始 JSON（進版控） |
| `App_Data/` | 執行期資料（首次啟動產生，不進版控） |

尚無內容的資料夾，等有檔案時再建立。
範本原有的 `Models/ErrorViewModel.cs` 維持原位；之後新增的畫面用模型一律放 `ViewModels/`。
`App_Data/` 已在 `.gitignore` 排除，並在 `LiveChat.csproj` 排除於建置與發佈輸出之外。

## 分層規則

- Service 介面 `IXxxService`，實作 `XxxService`，放 `Services/`。
- 規則一律寫在 Service，Controller 與 Hub 只負責呼叫與回應。
- 錯誤訊息用繁體中文，統一由 Service 丟出 `BusinessRuleException`（含訊息）。
- 所有時間存 UTC，顯示時才轉本地。
- `BusinessRuleException` 位於 `Services/`；Controller 接住後把 `Message` 顯示給使用者。

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
  - `ReplaceAll`：整批覆蓋，目前供重置測試資料使用。
  - `Add`：`Id` 為 `Guid.Empty` 時自動產生；`Id` 重複丟 `InvalidOperationException`。
  - `Update`、`Delete`：找不到回傳 `false`。
  - 每次讀取都回傳新物件，修改後要呼叫 `Update` 才會寫入。
- `JsonFileRepository<T>`：以 Singleton 註冊；每個 JSON 檔一把 `SemaphoreSlim`，讀寫都鎖；先寫 `.tmp` 再覆蓋。
  - 單次方法呼叫是執行緒安全的；「先查再寫」這類多步驟規則（例如 R3、R9），由 Service 自行加鎖。
  - 僅支援單一程序存取同一份 `App_Data/`。
- `JsonDataFile`：檔名規則、JSON 格式與讀寫的唯一來源；本身不上鎖。執行期資料一律經 `IRepository<T>`，不直接呼叫它寫 `App_Data/`。
- 檔名：實體名稱轉 camelCase 加 `s`（`User` → `users.json`、`ChannelMember` → `channelMembers.json`）。
- `SeedDataInitializer`：啟動時把 `App_Data/` 缺少的檔案從 `SeedData/` 複製過去，既有檔案不覆蓋。
- 新增實體時，要同時新增對應的 `SeedData/xxx.json`。

## JSON 格式

- 屬性名稱 camelCase；列舉存字串；中文不跳脫。
- 時間用 ISO 8601 並帶 `Z`（例：`2026-01-01T08:00:00Z`），手寫 SeedData 時也一樣。

## 登入與授權

- Cookie 驗證；Mock 帳號共用密碼定義在 `AuthService`，並記載於 README。沒有註冊功能。
- Fallback Policy 要求所有端點登入；免登入的端點要明確標 `[AllowAnonymous]`。
- Claims：`NameIdentifier` = 使用者 Id、`Name` = DisplayName、`Role` = `Admin` / `Member`。
- 建立 `ClaimsPrincipal` 一律用 `IAuthService.CreatePrincipal`。
- 全域啟用 `AutoValidateAntiforgeryTokenAttribute`；POST 表單用 form tag helper 產生權杖。

## 畫面

- 共用版面 `Views/Shared/_Layout.cshtml`：頂部導覽、登入者名稱與角色、登出；Development 時其下顯示工具列（View Component `DevToolbar`）。
- View Component：類別 `XxxViewComponent`，畫面 `Views/Shared/Components/Xxx/Default.cshtml`。
- 畫面用模型命名 `XxxViewModel`。
- 欄位值改變即送出表單：加 `data-auto-submit`（處理在 `wwwroot/js/site.js`）。

## Development 工具

- 只給 Development 的 Controller 標 `[DevelopmentOnly]`，其他環境由 `DevelopmentOnlyConvention` 移除路由；對應服務只在 Development 註冊。
- 畫面上的工具以 `IsDevelopment()` 判斷是否輸出。

## 測試資料

- 由 `SeedGenerator` 產生，不手改 `SeedData/*.json`。時間一律相對產生當下的 `DateTime.UtcNow`，不寫死日期。
- Id 固定：`00000000-0000-0000-<實體種類 4 碼>-<流水號 12 碼>`（1 User、2 Channel、3 ChannelMember、4 Message、5 ReadMarker、6 Mute）。
- 重置：`SeedResetService.Reset()`（工具列按鈕或 `dotnet run -- --seed`），產生 → 寫入 → 讀回驗證 → 通過才覆蓋 `App_Data/`。
- 每種情況 3 筆，改資料時要同步更新 `SeedValidator`。計數定義：
  - 頻道 Public / Private / Direct 只算未封存者；已封存另計。
  - 訊息四類互斥：已刪除 > 含 @提及 > 依發送是否滿 5 分鐘分為可編輯 / 不可編輯。
  - @提及寫法為 `@帳號`（UserName）。
