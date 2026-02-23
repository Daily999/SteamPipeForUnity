# SteamPipe For Unity

Unity 編輯器擴充工具，用於將遊戲建置上傳至 Steam。

## 專案狀態

![License](https://img.shields.io/badge/License-MIT-blue.svg?style=flat-square)
![AI Assisted](https://img.shields.io/badge/AI-Assisted-purple.svg?style=flat-square)

## 重要提醒

> ⚠️ **本專案程式碼大部分由 AI 生成**  
> 使用前請充分測試，並根據需求調整。建議先上傳到測試分支驗證。

## 快速開始

1. 在 Unity 選單中開啟 `Window > Steam Pipe`
2. 建立新的 `SteamBuildConfig` 配置檔案
3. 設定 App ID 和 Depot 資訊
4. 下載 SteamCMD（自動或手動）
5. 輸入 Steam 登入資訊
6. 開始上傳

## 主要功能

- 🚀 自動下載和管理 SteamCMD
- 🔐 加密儲存登入憑證
- 🌍 支援多平台 Depot 管理
- 🛡️ Steam DRM 包裝整合
- 📊 即時上傳進度顯示
- ⚙️ ScriptableObject 配置管理

### 詳細步驟

#### 1. 建立建置配置

1. 在 Steam Pipe 視窗頂部選擇或建立一個 `SteamBuildConfig` 資產
2. 點擊 **「新建」** 按鈕，選擇儲存位置
3. 切換到 **「配置」** 頁籤設定：
    - **App ID**：你的 Steam App ID
    - **建置描述**：本次上傳的描述（會自動加上 `<Tool Upload>` 標籤）
    - **分支名稱**：留空表示直接上傳到 default 分支

#### 2. 配置 Depot

在 **「配置」** 頁籤中：

1. 點擊 **「+」** 按鈕新增 Depot
2. 為每個 Depot 設定：
    - **Depot ID**：Steam 後台的 Depot ID
    - **平台註解**：方便識別的註解（如 `Windows 64-bit`、`macOS`）
    - **內容根目錄路徑**：指向建置檔案的根目錄

3. **（可選）設定 Steam DRM**：
    - 展開 **「Steam DRM 設定」**
    - 勾選 **「啟用 DRM」**
    - 設定 **「執行檔相對路徑」**（相對於內容根目錄，如 `MyGame.exe` 或 `MyGame.app/Contents/MacOS/MyGame`）
    - ⚠️ **重要**：DRM 包裝需在上傳前執行，使用下方的「執行 DRM 包裝」按鈕

**範例配置：**

```
Depot 1: Windows 建置
  Depot ID: xxxxxxx
  平台註解: Windows 64-bit
  路徑: C:/Builds/MyGame/Windows
  DRM: 啟用
  執行檔: MyGame.exe

Depot 2: macOS 建置
  Depot ID: xxxxxxx
  平台註解: macOS Universal
  路徑: /Users/me/Builds/MyGame/macOS
  DRM: 啟用
  執行檔: MyGame.app/Contents/MacOS/MyGame

Depot 3: Linux 建置
  Depot ID: xxxxxxx
  平台註解: Linux x64
  路徑: /home/user/Builds/MyGame/Linux
  DRM: 未啟用
```

#### 3. 設定 SteamCMD

切換到 **「SteamCMD」** 頁籤：

1. 點擊 **「自動下載並安裝」** 讓工具自動下載 SteamCMD
    - Windows：自動下載 `steamcmd.zip` 並解壓
    - macOS/Linux：使用 `curl` 下載 tar.gz 並解壓
2. SteamCMD 會安裝到專案的 `Library/SteamCMD/` 目錄
3. 或點擊 **「手動指定路徑」** 指向現有的 SteamCMD 執行檔

#### 4. 設定登入憑證

切換到 **「登入」** 頁籤：

1. 輸入你的 Steam 帳號和密碼
2. 勾選 **「記住登入資訊」** 以加密儲存憑證（可選，使用 XOR 加密）
3. 上傳時若需要 **Steam Guard 代碼**，請在該欄位輸入
4. 未輸入**Steam Guard 代碼**，Steam Guard會在登入時會跳出登入請求，同意即可。

⚠️ **注意事項**：

- Steam Guard 代碼每次上傳都需要重新輸入
- 建議使用專用的 Steam 建置帳號，而非個人帳號
- 密碼會使用基本加密儲存在 EditorPrefs，但不建議在共用電腦上勾選「記住登入」

#### 5. （可選）執行 DRM 包裝

如果你的 Depot 啟用了 Steam DRM：

1. 確保已設定正確的執行檔路徑
2. 在對應的 Depot 設定中點擊 **「執行 DRM 包裝」** 按鈕
3. 工具會：
    - 登入 Steam（可能需要 Steam Guard 代碼）
    - 將原始執行檔備份到 `SKIP_UPLOAD/` 資料夾
    - 上傳執行檔到 Steam 伺服器進行 DRM 加密
    - 下載加密後的執行檔並覆蓋原始檔案
4. 驗證 DRM 包裝成功後再進行上傳

⚠️ **DRM 注意事項**：

- 原始未加密的執行檔會被移至 `SKIP_UPLOAD/` 目錄
- `SKIP_UPLOAD/` 資料夾會自動建立，並**總是排除上傳**（無論是否啟用 DRM）
- 你也可以將其他不需上傳的檔案放入此資料夾
- 務必保留原始檔案的備份
- DRM 包裝失敗時會中斷流程，請檢查錯誤訊息

#### 6. 開始上傳

切換到 **「上傳」** 頁籤：

1. 檢視配置預覽，確認所有設定正確
2. 點擊 **「驗證配置」** 按鈕檢查是否有錯誤（會檢查路徑、檔案存在性等）
3. 確認無誤後，點擊 **「開始上傳」** 按鈕
4. 在彈出的確認對話框中確認上傳
5. 查看即時進度條和 Unity Console 的詳細日誌
6. 上傳完成後，登入 Steamworks 後台驗證

## 授權

MIT License

---

**版本**: 0.1.1  
**作者**: Daily  
**Email**: zxc99707@gmail.com
