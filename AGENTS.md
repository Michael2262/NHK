# NHKProject — Codex 專案協作指令

## 專案身分

- 本專案是位於 `D:\Dev\NHKProject` 的 NHK Unity 遊戲專案。
- 所有回覆、說明、程式註解與除錯訊息，優先使用繁體中文；除非使用者明確要求其他語言。
- 開始任何分析、修改或審查前，必須先完整閱讀專案根目錄的 `CLAUDE.md`，並將其視為本專案的主要架構與協作規範。

## 工作原則

- 先檢查目前工作樹與相關檔案，保留使用者既有及尚未提交的修改，不得擅自覆蓋或回復。
- 僅在使用者要求實作、修改或修復時變更檔案；若使用者只要求分析或診斷，則保持唯讀。
- 大型或跨多檔修改必須先提出計畫，等待使用者確認後再實作。
- 修改完成後，清楚列出變更檔案、行為差異與需要由使用者在 Unity Editor 內驗證的項目。

## Unity 資產安全

- 除非使用者明確要求，禁止修改或刪除任何 `.meta` 檔。
- 除非使用者明確要求，禁止手動修改 `.prefab`、`.unity`、`.asset`、`.controller` 等 Unity 序列化檔。
- 禁止修改 Spine 匯出檔及其 `.meta`，包括 `.skel`、`.atlas` 與對應 `.json`。
- 禁止修改 Unity 或工具產生的資料夾，包括 `Library/`、`Temp/`、`obj/`、`Logs/`、`.vs/` 與 `Builds/`。
- PlayMaker FSM 的場景或 Prefab 設定由使用者在 Unity Editor 內操作；Codex 可以分析並修改相關純文字 `.cs` 工具，但不得自行改動 FSM 所在的場景或 Prefab。
- Unity 編譯與 Play Mode 測試由使用者在 Unity Editor 執行；若收到 Console 錯誤，再依完整錯誤訊息修正。

## 架構慣例

- 遊戲核心狀態與跨模組存取以 `GameStatusService.Instance` 為入口。
- 邏輯與狀態放在純 C# Model；MonoBehaviour Bridge、UI 與 Trigger 僅負責轉接、顯示和觸發。
- 存檔相容性、初始化順序、讀檔後統一刷新、SceneController 正規轉場，以及其他詳細規則，一律以 `CLAUDE.md` 為準。
