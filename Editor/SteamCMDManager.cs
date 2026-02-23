using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace SteamPipeForUnity.Editor
{
    /// <summary>
    /// SteamCMD 管理器：負責下載、安裝、驗證 SteamCMD
    /// </summary>
    public class SteamCMDManager
    {
        private const string PREF_STEAMCMD_PATH = "SteamPipe.SteamCMDPath";
        private const string DEFAULT_STEAMCMD_DIR = "Library/SteamCMD";

        private string steamCMDPath = "";
        private bool isDownloading = false;
        private bool isInitializing = false;

        public string SteamCMDPath => steamCMDPath;
        public bool IsDownloading => isDownloading;
        public bool IsInitializing => isInitializing;

        public void Initialize()
        {
            steamCMDPath = EditorPrefs.GetString(PREF_STEAMCMD_PATH, "");

            if (string.IsNullOrEmpty(steamCMDPath) || !File.Exists(steamCMDPath))
            {
                string defaultPath = GetDefaultSteamCMDPath();
                if (File.Exists(defaultPath))
                {
                    steamCMDPath = defaultPath;
                    EditorPrefs.SetString(PREF_STEAMCMD_PATH, steamCMDPath);
                }
                else
                {
                    steamCMDPath = "";
                }
            }
        }

        private string GetDefaultSteamCMDPath()
        {
            string projectPath = Directory.GetParent(Application.dataPath)?.FullName ?? "";
            string executableName = SteamPipeUtility.GetSteamCMDExecutableName();
            return Path.Combine(projectPath, DEFAULT_STEAMCMD_DIR, executableName);
        }

        public bool IsValid(out string error)
        {
            if (string.IsNullOrEmpty(steamCMDPath))
            {
                error = "SteamCMD 路徑未設定";
                return false;
            }

            if (!File.Exists(steamCMDPath))
            {
                error = "SteamCMD 執行檔不存在";
                return false;
            }

            error = null;
            return true;
        }

        public void DownloadAndInstall(Action<bool, string> onComplete)
        {
            if (isDownloading)
            {
                Debug.LogWarning("SteamCMD 正在下載中...");
                return;
            }

            string downloadURL = SteamPipeUtility.GetSteamCMDDownloadURL();
            if (string.IsNullOrEmpty(downloadURL))
            {
                onComplete?.Invoke(false, "不支援的平台");
                return;
            }

            string projectPath = Directory.GetParent(Application.dataPath)?.FullName ?? "";
            string installDir = Path.Combine(projectPath, DEFAULT_STEAMCMD_DIR);

            if (!Directory.Exists(installDir))
            {
                Directory.CreateDirectory(installDir);
            }

            isDownloading = true;

            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                DownloadAndExtractWindows(downloadURL, installDir, onComplete);
            }
            else
            {
                DownloadAndExtractUnix(downloadURL, installDir, onComplete);
            }
        }

        private void DownloadAndExtractWindows(string url, string installDir, Action<bool, string> onComplete)
        {
            string zipPath = Path.Combine(Application.temporaryCachePath, "steamcmd.zip");

            try
            {
                EditorUtility.DisplayProgressBar("下載 SteamCMD", "正在下載...", 0.3f);

                using (WebClient client = new WebClient())
                {
                    client.DownloadFile(url, zipPath);
                }

                EditorUtility.DisplayProgressBar("下載 SteamCMD", "正在解壓縮...", 0.7f);
                ZipFile.ExtractToDirectory(zipPath, installDir);
                File.Delete(zipPath);
                EditorUtility.ClearProgressBar();

                steamCMDPath = GetDefaultSteamCMDPath();
                EditorPrefs.SetString(PREF_STEAMCMD_PATH, steamCMDPath);
                isDownloading = false;
                Debug.Log($"SteamCMD 下載完成: {steamCMDPath}");
                
                InitializeSteamCMD(onComplete);
            }
            catch (Exception e)
            {
                EditorUtility.ClearProgressBar();
                isDownloading = false;
                string error = $"下載失敗: {e.Message}";
                Debug.LogError(error);
                onComplete?.Invoke(false, error);
            }
        }

        private void DownloadAndExtractUnix(string url, string installDir, Action<bool, string> onComplete)
        {
            try
            {
                EditorUtility.DisplayProgressBar("下載 SteamCMD", "正在下載並解壓縮...", 0.5f);

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"cd '{installDir}' && curl -sqL '{url}' | tar zxvf -\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                Process process = Process.Start(startInfo);
                process?.WaitForExit();
                EditorUtility.ClearProgressBar();

                if (process != null && process.ExitCode == 0)
                {
                    steamCMDPath = GetDefaultSteamCMDPath();
                    
                    if (File.Exists(steamCMDPath))
                    {
                        Process.Start("chmod", $"+x \"{steamCMDPath}\"")?.WaitForExit();
                    }

                    EditorPrefs.SetString(PREF_STEAMCMD_PATH, steamCMDPath);
                    isDownloading = false;
                    Debug.Log($"SteamCMD 下載完成: {steamCMDPath}");
                    
                    InitializeSteamCMD(onComplete);
                }
                else
                {
                    string error = process?.StandardError.ReadToEnd() ?? "Unknown error";
                    isDownloading = false;
                    Debug.LogError($"下載失敗: {error}");
                    onComplete?.Invoke(false, error);
                }
            }
            catch (Exception e)
            {
                EditorUtility.ClearProgressBar();
                isDownloading = false;
                string error = $"下載失敗: {e.Message}";
                Debug.LogError(error);
                onComplete?.Invoke(false, error);
            }
        }

        private void InitializeSteamCMD(Action<bool, string> onComplete)
        {
            if (isInitializing)
            {
                Debug.LogWarning("SteamCMD 正在初始化中...");
                return;
            }

            if (!IsValid(out string error))
            {
                onComplete?.Invoke(false, error);
                return;
            }

            isInitializing = true;

            try
            {
                EditorUtility.DisplayProgressBar("初始化 SteamCMD", "正在進行首次初始化，這可能需要一些時間...", 0.5f);

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = steamCMDPath,
                    Arguments = "+quit",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                Process process = Process.Start(startInfo);
                
                if (process != null)
                {
                    process.OutputDataReceived += (_, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            Debug.Log($"[SteamCMD] {e.Data}");
                        }
                    };
                    process.BeginOutputReadLine();
                    process.WaitForExit();

                    EditorUtility.ClearProgressBar();
                    isInitializing = false;

                    if (process.ExitCode == 0 || process.ExitCode == 7)
                    {
                        Debug.Log("SteamCMD 初始化完成");
                        onComplete?.Invoke(true, "安裝成功");
                    }
                    else
                    {
                        string errorMsg = $"初始化失敗，退出碼: {process.ExitCode}";
                        Debug.LogError(errorMsg);
                        onComplete?.Invoke(false, errorMsg);
                    }
                }
            }
            catch (Exception e)
            {
                EditorUtility.ClearProgressBar();
                isInitializing = false;
                string errorMsg = $"初始化失敗: {e.Message}";
                Debug.LogError(errorMsg);
                onComplete?.Invoke(false, errorMsg);
            }
        }

        public void SetCustomPath(string path)
        {
            if (File.Exists(path))
            {
                steamCMDPath = path;
                EditorPrefs.SetString(PREF_STEAMCMD_PATH, path);
                Debug.Log($"已設定 SteamCMD 路徑: {path}");
            }
            else
            {
                Debug.LogError($"檔案不存在: {path}");
            }
        }

        public void OnGUI()
        {
            EditorGUILayout.LabelField("SteamCMD 設定", EditorStyles.boldLabel);

            bool isValidSteamCMD = IsValid(out string validationError);

            if (isValidSteamCMD)
            {
                EditorGUILayout.HelpBox($"SteamCMD 已就緒\n路徑: {steamCMDPath}", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox($"SteamCMD 未安裝或路徑無效\n{validationError}", MessageType.Warning);
            }

            EditorGUILayout.BeginHorizontal();

            GUI.enabled = !isDownloading && !isInitializing;
            if (GUILayout.Button("自動下載並安裝", GUILayout.Height(25)))
            {
                DownloadAndInstall((success, message) =>
                {
                    if (success)
                    {
                        EditorUtility.DisplayDialog("成功", message, "確定");
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("失敗", message, "確定");
                    }
                });
            }

            if (GUILayout.Button("手動指定路徑", GUILayout.Height(25)))
            {
                string path = EditorUtility.OpenFilePanel("選擇 SteamCMD 執行檔", "", "");
                if (!string.IsNullOrEmpty(path))
                {
                    SetCustomPath(path);
                }
            }
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();

            if (isDownloading)
            {
                EditorGUILayout.HelpBox("正在下載 SteamCMD，請稍候...", MessageType.Info);
            }
            else if (isInitializing)
            {
                EditorGUILayout.HelpBox("正在初始化 SteamCMD，請稍候...", MessageType.Info);
            }
        }
    }
}
