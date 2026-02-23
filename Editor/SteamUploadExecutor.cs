using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace SteamPipeForUnity.Editor
{
    /// <summary>
    /// Steam 上傳執行器
    /// </summary>
    public class SteamUploadExecutor
    {
        private Process currentProcess;
        private bool isUploading = false;
        private float uploadProgress = 0f;
        private string uploadStatus = "";

        public bool IsUploading => isUploading;
        public float UploadProgress => uploadProgress;
        public string UploadStatus => uploadStatus;

        public void StartUpload(SteamCMDManager steamCMDManager, SteamCredentialManager credentialManager, 
            SteamBuildConfig config, Action<bool, string> onComplete)
        {
            if (isUploading)
            {
                Debug.LogWarning("已有上傳任務正在執行中");
                return;
            }

            if (!steamCMDManager.IsValid(out string steamError))
            {
                onComplete?.Invoke(false, steamError);
                return;
            }

            if (!credentialManager.IsValid(out string credError))
            {
                onComplete?.Invoke(false, credError);
                return;
            }

            if (!config.Validate(out var configErrors))
            {
                string errorMsg = "配置驗證失敗:\n" + string.Join("\n", configErrors);
                onComplete?.Invoke(false, errorMsg);
                return;
            }

            isUploading = true;
            uploadProgress = 0f;
            uploadStatus = "準備上傳...";

            // 取得登入憑證
            string username = credentialManager.Username;
            string password = credentialManager.Password;
            string steamGuardCode = credentialManager.SteamGuardCode;

            // 生成 VDF 檔案
            uploadStatus = "生成 VDF 配置檔案...";
            uploadProgress = 0.1f;

            string vdfPath;
            try
            {
                SteamVDFGenerator.GenerateAppBuildVDF(config, out vdfPath);
            }
            catch (Exception e)
            {
                isUploading = false;
                string errorMsg = $"生成 VDF 檔案失敗: {e.Message}";
                Debug.LogError(errorMsg);
                onComplete?.Invoke(false, errorMsg);
                return;
            }

            uploadProgress = 0.2f;
            uploadStatus = "開始上傳至 Steam...";

            string loginCommand = string.IsNullOrWhiteSpace(steamGuardCode)
                ? $"+login {username} {password}"
                : $"+login {username} {password} {steamGuardCode}";

            string arguments = $"{loginCommand} +run_app_build \"{vdfPath}\" +quit";

            Debug.Log($"開始上傳: App ID {config.appID}");
            Debug.Log($"VDF 路徑: {vdfPath}");

            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = steamCMDManager.SteamCMDPath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                currentProcess = new Process { StartInfo = startInfo };

                currentProcess.OutputDataReceived += (_, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        ProcessOutputLine(e.Data);
                    }
                };

                currentProcess.ErrorDataReceived += (_, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Debug.LogWarning($"[SteamCMD Error] {e.Data}");
                    }
                };

                currentProcess.Start();
                currentProcess.BeginOutputReadLine();
                currentProcess.BeginErrorReadLine();

                System.Threading.Tasks.Task.Run(() =>
                {
                    currentProcess.WaitForExit();
                    int exitCode = currentProcess.ExitCode;

                    EditorApplication.delayCall += () =>
                    {
                        isUploading = false;
                        
                        if (exitCode == 0 || exitCode == 7)
                        {
                            uploadProgress = 1f;
                            uploadStatus = "上傳完成";
                            Debug.Log("上傳成功！");
                            onComplete?.Invoke(true, "上傳成功");
                        }
                        else
                        {
                            uploadStatus = $"上傳失敗 (退出碼: {exitCode})";
                            Debug.LogError(uploadStatus);
                            onComplete?.Invoke(false, uploadStatus);
                        }

                        currentProcess = null;
                    };
                });
            }
            catch (Exception e)
            {
                isUploading = false;
                string errorMsg = $"啟動上傳失敗: {e.Message}";
                Debug.LogError(errorMsg);
                onComplete?.Invoke(false, errorMsg);
            }
        }

        public void CancelUpload()
        {
            if (currentProcess != null && !currentProcess.HasExited)
            {
                Debug.Log("正在取消上傳...");
                currentProcess.Kill();
                currentProcess = null;
                isUploading = false;
                uploadStatus = "已取消";
                uploadProgress = 0f;
            }
        }

        private void ProcessOutputLine(string line)
        {
            Debug.Log($"[SteamCMD] {line}");
            uploadStatus = line;

            var progressMatch = Regex.Match(line, @"Progress:\s*(\d+\.?\d*)%");
            if (progressMatch.Success)
            {
                if (float.TryParse(progressMatch.Groups[1].Value, out float progress))
                {
                    uploadProgress = progress / 100f;
                }
            }

            if (line.Contains("FAILED") || line.Contains("ERROR") || line.Contains("Error"))
            {
                Debug.LogError($"[SteamCMD 錯誤] {line}");
            }
            else if (line.Contains("Two-factor code:") || line.Contains("Steam Guard"))
            {
                Debug.LogWarning("需要 Steam Guard 驗證碼");
            }
            else if (line.Contains("Success"))
            {
                Debug.Log($"[SteamCMD 成功] {line}");
            }
        }

        public void OnGUI()
        {
            EditorGUILayout.LabelField("上傳控制", EditorStyles.boldLabel);

            if (isUploading)
            {
                EditorGUI.ProgressBar(
                    EditorGUILayout.GetControlRect(false, 20),
                    uploadProgress,
                    $"上傳中... {(uploadProgress * 100f):F1}%"
                );

                EditorGUILayout.HelpBox(uploadStatus, MessageType.Info);

                if (GUILayout.Button("取消上傳", GUILayout.Height(30)))
                {
                    if (EditorUtility.DisplayDialog("確認", "確定要取消上傳嗎？", "確定", "取消"))
                    {
                        CancelUpload();
                    }
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(uploadStatus))
                {
                    MessageType messageType = uploadStatus.Contains("成功") 
                        ? MessageType.Info 
                        : uploadStatus.Contains("失敗") || uploadStatus.Contains("錯誤")
                            ? MessageType.Error
                            : MessageType.None;

                    if (messageType != MessageType.None)
                    {
                        EditorGUILayout.HelpBox(uploadStatus, messageType);
                    }
                }
            }
        }
    }
}
