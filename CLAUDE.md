# CLAUDE.md — NHK 專案開發指南

本檔提供給 Claude Code（與其他 AI 協作工具）閱讀，說明本專案的架構慣例。
**動手前請先讀完「嚴格規則」「協作流程」「核心架構」「鐵則」四節**，避免破壞 Unity 資產參照、存檔相容性與初始化順序。

---

## 嚴格規則（絕對遵守）

**不可碰的檔案 —— 這些一律只「讀」不「寫」，除非我明確要求：**

1. **絕對不要修改或刪除任何 `.meta` 檔** —— 會造成 Unity GUID 參照斷裂、資產遺失。
2. **不要手動編輯 `.prefab`、`.unity`（場景）、`.asset`、`.controller` 等序列化檔**，除非我明確要求。這些檔優先「讀」不要「寫」。
3. **不要動 Spine 匯出檔**（`.skel`、`.atlas`，以及對應的 `.json`）與其 `.meta`。
4. **不要動產生物資料夾**：`Library/`、`Temp/`、`obj/`、`Logs/`、`.vs/`、`Builds/`。
5. **PlayMaker FSM 邏輯多半存在場景或 prefab 中（非純 C#）**。要調整 FSM 行為時，**先跟我說明你打算怎麼做，由我在 Editor 內手動操作**，不要自行去改場景／prefab 檔。

---

## 協作流程

- **你（Claude Code）負責編輯 `.cs` 等純文字程式碼檔**（不含翻譯字串與 CSV）。
- **你無法編譯或執行 Unity。** Compile 與 Play Mode 測試由我在 Unity Editor 進行。
- 我用 **Visual Studio** 開啟專案；你改檔後我會在 VS 重新載入。
- **有編譯錯誤或執行期錯誤時，我會把 Unity Console 的訊息貼給你，再請你修。**
- **大型或跨多檔的修改：先提出計畫讓我確認，再動手**，方便我逐筆審 diff。

---

## 專案概觀

- **引擎**：Unity（C#）
- **類型**：以對話、養成、情緒系統為核心的單機遊戲
- **關鍵第三方套件**：
  - **Pixel Crushers Dialogue System**：對話、劇情流程、Lua 變數、存檔（Saver/SaveSystem）
  - **DOTween**：動畫補間
  - **Newtonsoft.Json**：自訂存檔的序列化（**不是** Unity 內建 JsonUtility）
- **程式位置**：`Assets/Scripts/`，以 `數字_模組名` 編號分模組，單一 assembly（無 asmdef 切分）。

---

## 核心架構

### 1. `GameStatusService` —— 全域唯一的服務核心

`Assets/Scripts/00_Save/GameStatusService.cs`

- **Singleton MonoBehaviour**，掛在場景 GameObject 上，`DontDestroyOnLoad`。
- `[DefaultExecutionOrder(-900)]`：比一般腳本更早 `Awake`，確保其他系統存取時它已就緒。
- **職責**：作為服務定位器（Service Locator）+ 生命週期管理者。
  - `Awake → InitializeGameModels()`：用 `new` 實例化**所有** Model 與 Manager，並完成依賴注入。
  - 對外以唯讀屬性公開所有 Model（資料層）與 Manager/Service（邏輯層）。
  - 持有 `SaveManager`（`SaveGameManager`），所有檔案讀寫都委派給它。
  - 協調跨系統流程：`StartNewGame()`、`HandleDayPassed()`、`HandleSceneChanged()`、時間事件清 Flag 等。
- **存取方式**：`GameStatusService.Instance.Protagonist`、`.Heroines["xxx"]`、`.ProgressFlags`、`.SaveManager` …

> ⚠️ 幾乎所有跨模組的存取都從 `GameStatusService.Instance` 出發。新增系統時，把它的 Model/Manager 也註冊進 `InitializeGameModels()`，並視需要訂閱 `OnGameStatusLoaded`。

### 2. 每個模組的標準資料夾分工

各模組（`01_PlayerStatus`、`09_Progress` … 等）大致遵循以下分工：

| 資料夾 | 職責 | 是否 MonoBehaviour |
|--------|------|--------------------|
| `Models/` `Model/` | **模組核心**：狀態 + 邏輯 + 對外事件 | ❌ 純 C# 類別 |
| `DTO/` | **存檔項目**：`[Serializable]` 資料容器，與 Model 用 `ToSaveData()`/`LoadFromSaveData()` 對接 | ❌ 純 C# 類別 |
| `Config/` `SO/` | ScriptableObject 設定 / 資料庫，在 Inspector 拖入 `GameStatusService` | ❌ ScriptableObject |
| `BridgeAPI/` `Bridge/` `LuaBridge/` | **轉接層**：把 Model 方法暴露給 `Button.onClick` / `UnityEvent` / Dialogue System Lua | ✅ MonoBehaviour |
| `Trigger/` | 場景中監聽狀態並反應的元件 | ✅ MonoBehaviour |
| `UI/` | 視圖層 | ✅ MonoBehaviour |
| `Debug/` `DeBug/` | 除錯工具 | ✅ MonoBehaviour |
| `Editor/` | 自訂編輯器視窗（EditorWindow / CustomEditor） | ❌ Editor only |

**Model 是純 C# 類別（不是 MonoBehaviour）**，由 `GameStatusService` 用 `new` 建立並持有。
邏輯與狀態都放在 Model；MonoBehaviour 只做「轉接 / 顯示 / 觸發」。

### 3. 存檔架構（最重要，改動前務必理解）

#### 資料流

```
存檔：各 Model.ToSaveData() → 塞進 GameSaveData（根容器）→ Newtonsoft JSON → persistentDataPath
讀檔：JSON → GameSaveData → ApplySaveData() → 各 Model.LoadFromSaveData() → NotifyGameStatusLoaded() → 各系統整批 refresh
```

- **根容器**：`00_Save/GameSaveData.cs`。它彙整**散在各模組 `DTO/` 的存檔型別**
  （`ProtagonistSaveData`、`HeroineSaveData`、`ProgressFlagSaveData` …）。
- **讀寫器**：`00_Save/SaveGameManager.cs`
  - `SaveGame(slot)` → `CollectSaveData()` 收集所有 Model 狀態 → 寫 `gamesave_{slot}.json`，
    同時呼叫 `SaveSystem.SaveToSlot(slot)` 存 Pixel Crushers 對話資料。
  - `LoadGame(slot)` → **走 `SceneController.ChangeScene` 正規轉場管線**，
    在 `onBeforeHandlers` 套用存檔資料（`ApplySaveData`），再 `NotifyGameStatusLoaded`，
    最後套 Pixel Crushers 資料（`SaveSystem.ApplySavedGameData`）。

#### 兩種存檔範圍

| 範圍 | 型別 / 檔案 | 用途 |
|------|-------------|------|
| 單一存檔槽（per-slot） | `GameSaveData` → `gamesave_{n}.json` | 一般遊戲進度。槽位 0 為自動存檔（`AUTOSAVE_SLOT_INDEX`） |
| 跨存檔全域（cross-save） | `GlobalProgressModel` → `global_progress.json` | 解鎖 CG、通關次數等。`UnlockGlobalFlag` / `SetGlobalValue` / `AddGlobalValue` 會**即時寫檔** |

#### 為什麼 `GameSaveData` 用平行 List 而非 Dictionary

JSON 序列化（含 Unity JsonUtility）無法直接還原 `Dictionary`。
因此多實例資料（女主角、家人 RiskAgent）用**兩個對齊的 List** 模擬：

```csharp
public List<string> HeroineIDs;            // key
public List<HeroineSaveData> HeroineSaveDataList; // value（與 IDs 同 index 對齊）
```

> ✅ **新增可存檔的多實例資料時，必須沿用這個「ID List + Data List」配對 pattern**，
> 並在 `CollectSaveData()` / `ApplySaveData()` 兩端同步維護（含長度不匹配的防呆）。

---

## 模組地圖（`Assets/Scripts/`）

| 編號 | 模組 | 內容 |
|------|------|------|
| `00_Save` | 存檔核心 | `GameStatusService`、`GameSaveData`、`SaveGameManager`、全域進度 keys |
| `00_Setting` | 設定 | 遊戲設定 |
| `01_PlayerStatus` | 角色狀態 | 主角 / 女主角 Model、情緒卡池、解鎖、Bridge、Lua 橋接 |
| `02_ShopItemInventory` | 商店 / 道具 / 背包 | |
| `03_Skill` | 技能樹 | |
| `04_Time` | 時間系統 | Phase / Slot / Day 推進 |
| `05_MainUI` | 主 UI | |
| `06_Bridge` | 通用橋接層 | |
| `07_ScenceChangeTask` | 場景切換流程 | `SceneController` 轉場管線、ReadyHandler |
| `08_StatusEffect` | 狀態效果 | |
| `09_Progress` | 進度旗標系統 | `ProgressFlagModel`（Flag + 數值 Variable）、觸發器、解鎖條件 |
| `10_GlobalProgress` | 全域進度 | 跨存檔 `GlobalProgressModel` |
| `11_MiniGame` | 小遊戲 | |
| `12_EncounterGenerator` | 遭遇 / 事件生成 | |
| `13_Risk` | 風險代理人（家人） | `RiskAgentModel` |
| `14_Hub` | Hub 場景控制 | `HubController` 等 ReadyHandler |
| `15_Tachie` | 立繪 | |
| `19_MainLoop` | 主迴圈 | |
| 工具庫 | `MyDialogueSystem` `MyPlayMaker` | 自製 Dialogue System / PlayMaker 工具 —— 見下方「兩個工具庫」專節 |
| 其他 | `Enum` `Router` `SO` `Spine` `Tool` `ImgChange` `Collider` `Cursor` `ParticleAndEffect` | 共用工具 / 第三方整合 |

---

## 進度旗標系統（`09_Progress`）

`ProgressFlagModel` 同時管理**布林 Flag** 與**數值 Variable**，且 Flag 依生命週期分桶：

| `FlagLifetime` | 何時自動清除 |
|----------------|--------------|
| `Persistent` | 永不（存入存檔） |
| `Scene` | 切換場景 |
| `UntilNextSlot` | 時間推進一個 Slot |
| `UntilNextPhase` | 進入下一個 Phase |
| `UntilNextDay` | 跨日 |

- 語法糖：`AddPersistentFlag` / `AddSceneFlag` / `AddSlotFlag` / `AddPhaseFlag` / `AddDailyFlag`。
- 數值 `> 0` 也會被 `Contains()` 視為「擁有該 Flag」（為了舊腳本相容）。
- 清桶由 `GameStatusService` 訂閱時間 / 場景事件後呼叫（`ClearSlotFlags` 等）。
- 預設 Flag / Value 由 `ApplyAllDefaults()` 從 `Resources/Progress` 載入 `ProgressBaseDefinition` 套用。

---

## 兩個工具庫（高頻使用，新增工具請照範本）

這兩個資料夾是**自製工具的集中地**，劇情與 FSM 幾乎都靠它們驅動遊戲邏輯。
兩者的共通點：**工具本身不放邏輯，而是呼叫 `GameStatusService.Instance` 上的 Model/Manager**。

### `MyDialogueSystem/` —— 自製 Dialogue System 工具

針對 Pixel Crushers Dialogue System 擴充的工具集合：

- **`Sequencer/`（工具庫核心）**：自製 Sequencer Command，讓對話腳本能用一行指令驅動遊戲。
  - 繼承 `PixelCrushers.DialogueSystem.SequencerCommand`，命名空間放在
    `PixelCrushers.DialogueSystem.SequencerCommands`，類別名 `SequencerCommandXxx`。
  - 在 `Awake()` 解析參數（`GetParameter`、`GetParameterAsInt`…）→ 取 `GameStatusService.Instance` 上的 Model → 執行 → `Stop()`。
  - **對話腳本中的呼叫語法 = 去掉 `SequencerCommand` 前綴**，例如
    `SequencerCommandProgressFlag` → `ProgressFlag(Add, FlagID)`。
  - 既有命令涵蓋：情緒卡池、女主角數值/UI、進度旗標、時間、場景切換、Spine、立繪、音樂、劇情佇列、警告提示等。
    **新增同類功能時，先找有沒有現成命令可擴充，再決定要不要開新檔。**
- `LuaBridge/`：把 C# 函式註冊成 Dialogue System 的 Lua 函式（給對話條件式 / 腳本用）。
- `Manager/`（`StoryManager`）、`Trigger/`（`StoryTrigger`、`ConditionalStoryTrigger`）、`Bridge/`、`SaveDialogue/`：劇情排程、觸發與存檔監聽。

### `MyPlayMaker/` —— 自製 PlayMaker 工具

針對 PlayMaker（FSM）擴充的工具集合：

- **`actions/`（工具庫核心）**：自製 `FsmStateAction`，供 FSM 在 Editor 內拖用。
  - 繼承 `HutongGames.PlayMaker.FsmStateAction`，命名空間 `MyGame.Actions`。
  - 用 `[ActionCategory("...")]`、`[Tooltip]`、`[RequiredField]` 標註；欄位用 `FsmEnum`/`FsmInt`/SO 參照等。
  - 在 `OnEnter()` 取 `GameStatusService.Instance` 上的 Model → 執行 → `Finish()`。
  - 既有 action 依功能分子資料夾：`ProgressFlag/`、`Heroine/`、`Protagonist/`、`Minigame/`、
    `SaveSystem/`、`Scenario/`、`Spine/`、`Collider2DManager/`、`FsmValueManager/`、`WoodenMan/` 等。
- `Manager/`（`FsmValueManager`）、`SmallTool/`：FSM 數值管理與小工具。

> ⚠️ 你可以**新增 / 修改這些工具的 `.cs` 原始碼**；但**把工具實際掛到哪個 State、連哪條 transition、設哪個參數，是存在場景/prefab 裡的 FSM 設定** —— 依「嚴格規則 5」，那部分由我在 Editor 手動處理。

---

## 鐵則（修改前必讀）

1. **存檔相容性**：更動任何 `DTO/*SaveData` 欄位時要想到舊存檔。
   - 新增多實例可存檔資料 → 沿用 `IDs List + Data List` pattern。
   - 不要把 `Dictionary` 直接塞進 `GameSaveData`。

2. **初始化順序**：新系統的 Model/Manager 要在 `InitializeGameModels()` 內建立，
   依賴別人就排在被依賴者之後（`SaveManager` 依賴 `this`，固定最後實例化）。

3. **讀檔後刷新走事件**：`LoadFromSaveData` 期間**不要**逐項廣播 UI 事件，
   等 `NotifyGameStatusLoaded()` 統一觸發 `OnGameStatusLoaded`，各系統整批 refresh，
   避免訂閱者看到「半殘的世界狀態」。

4. **讀檔走 SceneController 管線**，不要用 `SaveSystem.LoadFromSlot` 的 `LoadSceneMode.Single`
   （會卸載 `GlobalStatusUI` 並跳過 ReadyHandler）。Unity 場景上 Save System 元件需**取消勾選
   "Save Current Scene"**。

5. **邏輯放 Model，不要放 MonoBehaviour**：Bridge / UI / Trigger 只做轉接與顯示。

6. **`HeroineStatusModel` 的相容層勿用**：檔案後段（約 558 行起）的 `AddAttack`、`Discomfort`、
   `Virginity`、`Excitement` 等是**舊專案遺留 stub**，NHK 新功能請改用情緒卡池 / `Libido` / `Trust` /
   `HCount` 等現役 API，不要依賴那些相容成員。

---

## 慣例

- **語言**：程式註解與 Debug 訊息以繁體中文為主，沿用既有風格。
- **序列化**：自訂存檔一律用 **Newtonsoft.Json**（`JsonConvert`），對話相關交給 **Pixel Crushers SaveSystem**。
- **存取全域狀態**：一律經 `GameStatusService.Instance`，不要自行再開 Singleton 持有遊戲資料。
- **多實例角色**：以 `Dictionary<string, Model>`（key = ID）儲存，從對應的 `Config`/`Database` SO 建立。
