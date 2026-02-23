using System;
using UnityEngine;

namespace SteamPipeForUnity.Editor
{
    /// <summary>
    /// Depot 配置資料結構
    /// </summary>
    [Serializable]
    public class DepotConfig
    {
        [Tooltip("Depot ID")]
        public string depotID = "";
        
        [Tooltip("註解標籤（僅供識別用途，例如：Windows x64、macOS、Linux 等）")]
        public string platformLabel = "";
        
        [Tooltip("內容根目錄路徑")]
        public string contentRoot = "";
        
        [Tooltip("檔案排除模式 (例如: *.pdb, *.log)")]
        public string[] excludePatterns = new string[0];
        
        [Header("Steam DRM 設定")]
        [Tooltip("是否啟用 Steam DRM 包裝")]
        public bool enableDRM = false;
        
        [Tooltip("要加密的執行檔名稱（相對於內容根目錄的路徑，例如：MyGame.exe 或 bin/MyGame.exe）")]
        public string executableFileName = "";

        /// <summary>
        /// 驗證 Depot 配置是否有效
        /// </summary>
        public bool IsValid(out string error)
        {
            if (string.IsNullOrWhiteSpace(depotID))
            {
                error = "Depot ID 不能為空";
                return false;
            }

            if (string.IsNullOrWhiteSpace(contentRoot))
            {
                error = "內容根目錄不能為空";
                return false;
            }

            if (!System.IO.Directory.Exists(contentRoot))
            {
                error = $"內容根目錄不存在: {contentRoot}";
                return false;
            }

            // 驗證 DRM 設定
            if (enableDRM)
            {
                if (string.IsNullOrWhiteSpace(executableFileName))
                {
                    error = "啟用 DRM 時，執行檔名稱不能為空";
                    return false;
                }

                string fullExePath = System.IO.Path.Combine(contentRoot, executableFileName);
                string backupPath = System.IO.Path.Combine(contentRoot, "SKIP_UPLOAD", executableFileName);
                
                // 檢查執行檔是否存在（可能在原位置或 SKIP_UPLOAD 中）
                if (!System.IO.File.Exists(fullExePath) && !System.IO.File.Exists(backupPath))
                {
                    error = $"執行檔不存在: {fullExePath}\n（也未在 SKIP_UPLOAD/ 中找到）";
                    return false;
                }
            }

            error = null;
            return true;
        }
    }
}
