using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace SteamPipeForUnity.Editor
{
    /// <summary>
    /// Steam DRM 包裝器：執行 drm_wrap 命令加密執行檔
    /// </summary>
    public static class SteamDRMWrapper
    {
        /// <summary>
        /// 執行 DRM 包裝
        /// </summary>
        /// <param name="steamCMDPath">SteamCMD 執行檔路徑</param>
        /// <param name="username">Steam 帳號</param>
        /// <param name="password">Steam 密碼</param>
        /// <param name="steamGuardCode">Steam Guard 代碼（可選）</param>
        /// <param name="appID">Steam App ID</param>
        /// <param name="depot">Depot 配置</param>
        /// <param name="error">錯誤訊息（如果失敗）</param>
        /// <returns>是否成功</returns>
        public static bool ExecuteDRMWrap(
            string steamCMDPath,
            string username,
            string password,
            string steamGuardCode,
            string appID,
            DepotConfig depot,
            out string error)
        {
            error = null;

            if (!depot.enableDRM)
            {
                return true; // 未啟用 DRM，視為成功
            }

            // 驗證設定
            if (string.IsNullOrWhiteSpace(depot.executableFileName))
            {
                error = $"Depot [{depot.platformLabel}] 未設定執行檔名稱";
                return false;
            }

            string inputFilePath = Path.Combine(depot.contentRoot, depot.executableFileName);
            if (!File.Exists(inputFilePath))
            {
                error = $"執行檔不存在: {inputFilePath}";
                return false;
            }

            // 建立臨時輸出路徑
            string outputDir = Path.Combine(Application.temporaryCachePath, "SteamPipe", "DRM_Output");
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            string outputFileName = Path.GetFileName(depot.executableFileName);
            string outputFilePath = Path.Combine(outputDir, outputFileName);

            // 確保 SKIP_UPLOAD 資料夾存在（用於備份原始執行檔）
            string skipUploadDir = Path.Combine(depot.contentRoot, "SKIP_UPLOAD");
            if (!Directory.Exists(skipUploadDir))
            {
                Directory.CreateDirectory(skipUploadDir);
                Debug.Log($"[DRM] 建立 SKIP_UPLOAD 備份資料夾: {skipUploadDir}");
            }

            Debug.Log($"[DRM] 開始包裝執行檔: {depot.platformLabel}");
            Debug.Log($"[DRM] 輸入檔案: {inputFilePath}");
            Debug.Log($"[DRM] 輸出檔案: {outputFilePath}");
            Debug.Log($"[DRM] 備份位置: {skipUploadDir}");

            // 組合 SteamCMD 命令
            string loginCommand = string.IsNullOrWhiteSpace(steamGuardCode)
                ? $"+login {username} {password}"
                : $"+login {username} {password} {steamGuardCode}";

            // drm_wrap 命令格式: drm_wrap <appid> <input> <output> <toolname> <flags> [destination]
            // toolname: drmtoolp (唯一支援的選項)
            // flags: 0 (一般包裝，防護最高)
            // destination: cloud (預設，無需指定)
            string drmWrapCommand = $"+drm_wrap {appID} \"{inputFilePath}\" \"{outputFilePath}\" drmtoolp 0";
            string arguments = $"{loginCommand} {drmWrapCommand} +quit";

            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = steamCMDPath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                Process process = new Process { StartInfo = startInfo };
                using (process)
                {
                    bool hasError = false;
                    string errorOutput = "";

                    process.OutputDataReceived += (_, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            Debug.Log($"[DRM SteamCMD] {e.Data}");
                            
                            if (e.Data.Contains("FAILED") || e.Data.Contains("ERROR") || e.Data.Contains("Error"))
                            {
                                hasError = true;
                                errorOutput += e.Data + "\n";
                            }
                        }
                    };

                    process.ErrorDataReceived += (_, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            Debug.LogError($"[DRM Error] {e.Data}");
                            hasError = true;
                            errorOutput += e.Data + "\n";
                        }
                    };

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    process.WaitForExit();

                    int exitCode = process.ExitCode;

                    // 檢查執行結果
                    if (exitCode != 0 && exitCode != 7)
                    {
                        error = $"DRM 包裝失敗 (退出碼: {exitCode})\n{errorOutput}";
                        Debug.LogError(error);
                        return false;
                    }

                    if (hasError)
                    {
                        error = $"DRM 包裝過程中發生錯誤:\n{errorOutput}";
                        Debug.LogError(error);
                        return false;
                    }

                    // 驗證輸出檔案是否存在
                    if (!File.Exists(outputFilePath))
                    {
                        error = "DRM 包裝完成，但找不到輸出檔案";
                        Debug.LogError(error);
                        return false;
                    }

                    Debug.Log($"[DRM] 包裝成功，開始處理檔案...");

                    // 移動原始檔案到 SKIP_UPLOAD
                    string backupFileName = Path.GetFileName(depot.executableFileName);
                    string backupFilePath = Path.Combine(skipUploadDir, backupFileName);
                    
                    // 如果目標已存在，先刪除
                    if (File.Exists(backupFilePath))
                    {
                        File.Delete(backupFilePath);
                    }

                    File.Move(inputFilePath, backupFilePath);
                    Debug.Log($"[DRM] 原始檔案已移至: {backupFilePath}");

                    // 複製包裝後的檔案到原位置
                    File.Copy(outputFilePath, inputFilePath, true);
                    Debug.Log($"[DRM] 包裝後檔案已放置: {inputFilePath}");

                    // 清理臨時輸出檔案
                    File.Delete(outputFilePath);

                    Debug.Log($"[DRM] Depot [{depot.platformLabel}] DRM 包裝完成！");
                    return true;
                }
            }
            catch (Exception ex)
            {
                error = $"執行 DRM 包裝時發生異常: {ex.Message}";
                Debug.LogError(error);
                return false;
            }
        }
    }
}
