專案：LiveChat，企業內部即時通訊（簡化版 Slack）。ASP.NET Core MVC + SignalR。
角色：Admin、Member。手機：聊天室頁面要 RWD（手機寬度可用，左側頻道清單收合成抽屜）。

核心實體：User、Channel、ChannelMember、Message、ReadMarker、Mute、AuditLog。
- Channel.Type：Public / Private / Direct；Channel.IsArchived；Channel.CreatedByUserId。
- Message：ChannelId、SenderId、Content、CreatedAt、EditedAt?、IsDeleted、MentionUserIds。
- ReadMarker：UserId、ChannelId、LastReadAt。
- Mute：UserId、Until（到期自動失效，判斷時比對現在時間，不需排程）。
- AuditLog：ActorUserId、Action、TargetDescription、CreatedAt。

核心商業規則：
R1 公開頻道：任何登入者可加入/離開；非成員可看頻道清單但看不到訊息內容。
R2 私人頻道：只有成員看得到；只有頻道建立者可邀請/移除成員。
R3 私訊：任兩人之間只有一個對話；重複建立時回傳既有的。
R4 訊息內容：去除前後空白後長度 1~1000 字，否則拒絕。
R5 編輯：僅發送者、發送後 5 分鐘內、且未刪除；編輯後顯示「已編輯」。
R6 刪除：發送者可隨時刪自己的；Admin 可刪任何人的；軟刪除，顯示「此訊息已刪除」。
R7 禁言：Mute.Until 大於現在時，不可發送訊息，可閱讀。
R8 封存頻道：唯讀，任何人不能發送或編輯；Admin 可解除封存。
R9 限流：同一使用者 10 秒內最多 5 則，超過拒絕並回傳明確錯誤。
R10 未讀數：該頻道內「晚於 LastReadAt 且非自己發送」的訊息數。
