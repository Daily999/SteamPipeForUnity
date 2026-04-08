
using System.IO;
using System.Text;
using UnityEngine;

namespace SteamPipeForUnity.Editor
{
    /// <summary>
    /// Steam VDF 檔案生成器
    /// </summary>
    public static class SteamVDFGenerator
    {
        /// <summary>
        /// 生成 app_build VDF 檔案
        /// </summary>
        /// <param name="preview">設為 true 時只掃描檔案清單、不實際上傳（測試模式）</param>
        public static string GenerateAppBuildVDF(SteamBuildConfig config, out string vdfPath, bool preview = false)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("\"AppBuild\"");
            sb.AppendLine("{");
            sb.AppendLine($"    \"AppID\" \"{config.appID}\"");

            // 在說明欄加上上傳類型標示
            string uploadTag = preview ? "<Test Upload>" : "<Tool Upload>";
            string description = string.IsNullOrWhiteSpace(config.buildDescription)
                ? uploadTag
                : $"{uploadTag} {config.buildDescription}";
            sb.AppendLine($"    \"Desc\" \"{EscapeVDFString(description)}\"");

            // 測試模式：只掃描不上傳
            if (preview)
            {
                sb.AppendLine($"    \"Preview\" \"1\"");
            }
            if (!string.IsNullOrWhiteSpace(config.setLiveBranch))
            {
                string branchName = config.setLiveBranch.Trim().ToLower();

                if (branchName == "default" || branchName == "")
                {
                    Debug.LogWarning($"[Steam] ⚠️ 安全警告：'default' 分支不會自動釋出，需在 Steamworks 網站手動設定。");
                }
                else
                {
                    sb.AppendLine($"    \"SetLive\" \"{EscapeVDFString(config.setLiveBranch)}\"");
                    Debug.LogWarning($"[Steam] ⚠️ 警告：此建置將自動釋出到分支 '{config.setLiveBranch}'");
                }
            }

            sb.AppendLine($"    \"ContentRoot\" \"\""); // 使用空字串，由各 depot 指定
            sb.AppendLine($"    \"BuildOutput\" \"{EscapeVDFString(GetBuildOutputPath())}\"");

            sb.AppendLine("    \"Depots\"");
            sb.AppendLine("    {");

            foreach (var depot in config.depots)
            {
                if (depot == null) continue;

                string depotVDFPath = GenerateDepotBuildVDF(depot);
                sb.AppendLine($"        \"{depot.depotID}\" \"{EscapeVDFString(depotVDFPath)}\"");
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            // 儲存到臨時目錄
            string tempDir = Path.Combine(Application.temporaryCachePath, "SteamPipe");
            if (!Directory.Exists(tempDir))
            {
                Directory.CreateDirectory(tempDir);
            }

            vdfPath = Path.Combine(tempDir, $"app_build_{config.appID}.vdf");
            File.WriteAllText(vdfPath, sb.ToString(), Encoding.UTF8);

            Debug.Log($"生成 App Build VDF: {vdfPath}");
            return sb.ToString();
        }

        /// <summary>
        /// 生成 depot_build VDF 檔案
        /// </summary>
        private static string GenerateDepotBuildVDF(DepotConfig depot)
        {
            // 確保 SKIP_UPLOAD 資料夾存在（總是建立，避免 VDF 錯誤）
            string skipUploadDir = Path.Combine(depot.contentRoot, "SKIP_UPLOAD");
            if (!Directory.Exists(skipUploadDir))
            {
                Directory.CreateDirectory(skipUploadDir);
                Debug.Log($"[VDF] 建立 SKIP_UPLOAD 資料夾: {skipUploadDir}");
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("\"DepotBuild\"");
            sb.AppendLine("{");
            sb.AppendLine($"    \"DepotID\" \"{depot.depotID}\"");
            sb.AppendLine($"    \"ContentRoot\" \"{EscapeVDFString(depot.contentRoot)}\"");

            // 檔案映射（包含所有檔案）
            sb.AppendLine("    \"FileMapping\"");
            sb.AppendLine("    {");
            sb.AppendLine("        \"LocalPath\" \"*\"");
            sb.AppendLine("        \"DepotPath\" \".\"");
            sb.AppendLine("        \"Recursive\" \"1\"");
            sb.AppendLine("    }");

            // 排除規則 - 每個規則是獨立的 "FileExclusion" "pattern" key-value pair
            // SteamCMD VDF 允許相同 key 出現多次

            // 總是排除 SKIP_UPLOAD 資料夾（用於 DRM 原始檔案或其他不需上傳的檔案）
            sb.AppendLine("    \"FileExclusion\" \"SKIP_UPLOAD/*\"");

            // 加入使用者自訂的排除規則
            if (depot.excludePatterns != null && depot.excludePatterns.Length > 0)
            {
                foreach (var pattern in depot.excludePatterns)
                {
                    if (!string.IsNullOrWhiteSpace(pattern))
                    {
                        sb.AppendLine($"    \"FileExclusion\" \"{EscapeVDFString(pattern)}\"");
                    }
                }
            }

            sb.AppendLine("}");

            // 儲存 depot VDF
            string tempDir = Path.Combine(Application.temporaryCachePath, "SteamPipe");
            string depotVDFPath = Path.Combine(tempDir, $"depot_build_{depot.depotID}.vdf");
            File.WriteAllText(depotVDFPath, sb.ToString(), Encoding.UTF8);

            Debug.Log($"生成 Depot Build VDF ({depot.platformLabel}): {depotVDFPath}");
            return depotVDFPath;
        }

        /// <summary>
        /// 取得建置輸出路徑
        /// </summary>
        private static string GetBuildOutputPath()
        {
            string outputPath = Path.Combine(Application.temporaryCachePath, "SteamPipe", "Output");
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }
            return outputPath;
        }

        /// <summary>
        /// 轉義 VDF 字串中的特殊字元
        /// </summary>
        private static string EscapeVDFString(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            return input.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
