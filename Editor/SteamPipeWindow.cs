using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SteamPipeForUnity.Editor
{
    /// <summary>
    /// Steam Pipe 主編輯器視窗
    /// </summary>
    public class SteamPipeWindow : EditorWindow
    {
        private SteamBuildConfig currentConfig;
        private SteamCMDManager steamCMDManager;
        private SteamCredentialManager credentialManager;
        private SteamUploadExecutor uploadExecutor;

        private Vector2 scrollPosition;
        private int selectedTab = 0;
        private readonly string[] tabNames = new string[] { "SteamCMD", "登入", "配置", "上傳" };

        [MenuItem("Window/Steam Pipe")]
        public static void ShowWindow()
        {
            var window = GetWindow<SteamPipeWindow>("Steam Pipe");
            window.minSize = new Vector2(400, 600);
            window.Show();
        }

        private void OnEnable()
        {
            steamCMDManager = new SteamCMDManager();
            steamCMDManager.Initialize();

            credentialManager = new SteamCredentialManager();
            credentialManager.Load();

            uploadExecutor = new SteamUploadExecutor();
        }

        private void OnDisable()
        {
            // 儲存憑證
            credentialManager?.Save();
        }

        private void OnGUI()
        {
            DrawHeader();
            EditorGUILayout.Space(5);

            DrawConfigSelection();
            EditorGUILayout.Space(10);

            if (currentConfig != null)
            {
                // Tab 切換
                selectedTab = GUILayout.Toolbar(selectedTab, tabNames, GUILayout.Height(30));
                EditorGUILayout.Space(5);

                // 開始捲動區域
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

                // 根據選擇的 Tab 顯示對應內容
                switch (selectedTab)
                {
                    case 0:
                        DrawSteamCMDSettings();
                        break;
                    case 1:
                        DrawLoginSettings();
                        break;
                    case 2:
                        DrawConfigPreview();
                        break;
                    case 3:
                        DrawUploadControl();
                        break;
                }

                EditorGUILayout.EndScrollView();
            }
            else
            {
                EditorGUILayout.HelpBox("請選擇或建立一個 Steam Build Config", MessageType.Warning);
            }


            // 如果正在上傳，定期重繪
            if (uploadExecutor.IsUploading)
            {
                Repaint();
            }
        }

        private void DrawHeader()
        {
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter
            };

            EditorGUILayout.LabelField("Steam Pipe For Unity", headerStyle, GUILayout.Height(30));
            EditorGUILayout.LabelField("Steam 多平台上傳工具", EditorStyles.centeredGreyMiniLabel);
        }

        private void DrawConfigSelection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("建置配置", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            currentConfig = (SteamBuildConfig)EditorGUILayout.ObjectField(
                "Config Asset",
                currentConfig,
                typeof(SteamBuildConfig),
                false
            );

            if (GUILayout.Button("新建", GUILayout.Width(60)))
            {
                CreateNewConfig();
            }

            EditorGUILayout.EndHorizontal();

            if (currentConfig != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"App ID: {currentConfig.appID}", EditorStyles.miniLabel);

                if (GUILayout.Button("編輯", GUILayout.Width(60)))
                {
                    Selection.activeObject = currentConfig;
                    EditorGUIUtility.PingObject(currentConfig);
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawSteamCMDSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            steamCMDManager.OnGUI();
            EditorGUILayout.EndVertical();
        }

        private void DrawLoginSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            credentialManager.OnGUI();
            EditorGUILayout.EndVertical();
        }

        private void DrawConfigPreview()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("App ID", currentConfig.appID);
            EditorGUILayout.LabelField("描述", currentConfig.buildDescription);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField($"Depots ({currentConfig.depots.Count})", EditorStyles.boldLabel);

            if (currentConfig.depots.Count == 0)
            {
                EditorGUILayout.HelpBox("尚未配置任何 Depot", MessageType.Warning);
            }
            else
            {
                foreach (var depot in currentConfig.depots)
                {
                    if (depot == null) continue;

                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.LabelField($"Depot ID: {depot.depotID}", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"註解: {depot.platformLabel}");
                    EditorGUILayout.LabelField($"路徑: {depot.contentRoot}", EditorStyles.wordWrappedMiniLabel);

                    bool isValid = depot.IsValid(out string error);
                    if (!isValid)
                    {
                        EditorGUILayout.HelpBox(error, MessageType.Error);
                    }

                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(3);
                }
            }

            EditorGUILayout.Space(5);

            // 驗證按鈕
            if (GUILayout.Button("驗證配置", GUILayout.Height(25)))
            {
                ValidateConfiguration();
            }

            EditorGUILayout.EndVertical();
        }


        private void DrawUploadControl()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            uploadExecutor.OnGUI();

            EditorGUILayout.Space(5);

            GUI.enabled = !uploadExecutor.IsUploading;

            if (GUILayout.Button("開始上傳", GUILayout.Height(40)))
            {
                StartUpload();
            }

            EditorGUILayout.Space(3);

            EditorGUILayout.HelpBox("測試上傳：使用 SteamCMD Preview 模式掃描檔案清單，不會實際上傳到 Steam。可用於驗證配置與網路連線是否正常。", MessageType.Info);

            if (GUILayout.Button("測試上傳（Preview 模式）", GUILayout.Height(30)))
            {
                StartUpload(preview: true);
            }

            GUI.enabled = true;

            EditorGUILayout.EndVertical();
        }


        private void CreateNewConfig()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "建立 Steam Build Config",
                "SteamBuildConfig",
                "asset",
                "請選擇儲存位置"
            );

            if (!string.IsNullOrEmpty(path))
            {
                SteamBuildConfig config = CreateInstance<SteamBuildConfig>();
                AssetDatabase.CreateAsset(config, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                currentConfig = config;
                Selection.activeObject = config;
                EditorGUIUtility.PingObject(config);

                Debug.Log($"已建立新的 Steam Build Config: {path}");
            }
        }

        private void ValidateConfiguration()
        {
            if (currentConfig == null)
            {
                EditorUtility.DisplayDialog("驗證失敗", "未選擇配置檔案", "確定");
                return;
            }

            List<string> errors = new List<string>();

            // 驗證 SteamCMD
            if (!steamCMDManager.IsValid(out string steamError))
            {
                errors.Add($"[SteamCMD] {steamError}");
            }

            // 驗證憑證
            if (!credentialManager.IsValid(out string credError))
            {
                errors.Add($"[登入] {credError}");
            }

            // 驗證配置
            if (!currentConfig.Validate(out List<string> configErrors))
            {
                errors.AddRange(configErrors);
            }

            if (errors.Count == 0)
            {
                EditorUtility.DisplayDialog("驗證成功", "所有配置都正確！可以開始上傳。", "確定");
            }
            else
            {
                string errorMessage = "發現以下問題:\n\n" + string.Join("\n", errors);
                EditorUtility.DisplayDialog("驗證失敗", errorMessage, "確定");
            }
        }

        private void StartUpload(bool preview = false)
        {
            if (currentConfig == null)
            {
                EditorUtility.DisplayDialog("錯誤", "未選擇配置檔案", "確定");
                return;
            }

            // 先驗證
            List<string> errors = new List<string>();

            if (!steamCMDManager.IsValid(out string steamError))
            {
                errors.Add(steamError);
            }

            if (!credentialManager.IsValid(out string credError))
            {
                errors.Add(credError);
            }

            if (!currentConfig.Validate(out List<string> configErrors))
            {
                errors.AddRange(configErrors);
            }

            if (errors.Count > 0)
            {
                string errorMessage = "無法開始上傳:\n\n" + string.Join("\n", errors);
                EditorUtility.DisplayDialog("錯誤", errorMessage, "確定");
                return;
            }

            if (preview)
            {
                // 測試模式確認對話框
                string previewMessage = $"將以 Preview 模式（測試）執行 SteamCMD。\n\n" +
                                        $"App ID: {currentConfig.appID}\n" +
                                        $"Depots: {currentConfig.depots.Count}\n\n" +
                                        $"此操作只會掃描檔案清單，不會實際上傳任何內容到 Steam。";

                if (EditorUtility.DisplayDialog("確認測試上傳", previewMessage, "開始測試", "取消"))
                {
                    uploadExecutor.StartUpload(steamCMDManager, credentialManager, currentConfig, (success, message) =>
                    {
                        if (success)
                        {
                            EditorUtility.DisplayDialog("測試完成", message, "確定");
                        }
                        else
                        {
                            EditorUtility.DisplayDialog("測試失敗", $"測試上傳失敗:\n{message}", "確定");
                        }
                    }, preview: true);
                }

                return;
            }

            // 確認對話框
            string confirmMessage = $"確定要上傳到 Steam 嗎？\n\n" +
                                    $"App ID: {currentConfig.appID}\n" +
                                    $"Depots: {currentConfig.depots.Count}";

            // 如果設定了自動釋出，添加明顯警告
            if (!string.IsNullOrWhiteSpace(currentConfig.setLiveBranch) && !currentConfig.setLiveBranch.Trim().ToLower().Equals("default"))
            {
                confirmMessage += $"\n\n⚠️⚠️⚠️ 警告 ⚠️⚠️⚠️\n" +
                                  $"上傳完成後將立即釋出到分支：{currentConfig.setLiveBranch}\n" +
                                  $"玩家將立即看到新版本！";
            }

            if (EditorUtility.DisplayDialog("確認上傳", confirmMessage, "開始上傳", "取消"))
            {
                uploadExecutor.StartUpload(steamCMDManager, credentialManager, currentConfig, (success, message) =>
                {
                    if (success)
                    {
                        EditorUtility.DisplayDialog("成功", "上傳完成！", "確定");
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("失敗", $"上傳失敗:\n{message}", "確定");
                    }
                });
            }
        }
    }
}