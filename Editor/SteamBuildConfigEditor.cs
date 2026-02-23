using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace SteamPipeForUnity.Editor
{
    /// <summary>
    /// SteamBuildConfig 的自訂 Inspector
    /// </summary>
    [CustomEditor(typeof(SteamBuildConfig))]
    public class SteamBuildConfigEditor : UnityEditor.Editor
    {
        private ReorderableList depotList;
        private SerializedProperty appIDProp;
        private SerializedProperty buildDescriptionProp;
        private SerializedProperty setLiveBranchProp;
        private SerializedProperty depotsProp;
        private bool[] drmFoldouts; // 追蹤每個 Depot 的 DRM 折疊狀態

        private void OnEnable()
        {
            appIDProp = serializedObject.FindProperty("appID");
            buildDescriptionProp = serializedObject.FindProperty("buildDescription");
            setLiveBranchProp = serializedObject.FindProperty("setLiveBranch");
            depotsProp = serializedObject.FindProperty("depots");

            // 初始化 DRM 折疊狀態陣列
            drmFoldouts = new bool[depotsProp.arraySize];

            SetupDepotList();
        }

        private void SetupDepotList()
        {
            depotList = new ReorderableList(serializedObject, depotsProp, true, true, true, true)
            {
                drawHeaderCallback = rect =>
                {
                    EditorGUI.LabelField(rect, "Depot 配置列表");
                },

                drawElementCallback = (rect, index, _, _) =>
                {
                    // 確保 foldout 陣列大小正確
                    if (drmFoldouts == null || drmFoldouts.Length != depotsProp.arraySize)
                    {
                        drmFoldouts = new bool[depotsProp.arraySize];
                    }

                    SerializedProperty element = depotsProp.GetArrayElementAtIndex(index);
                    rect.y += 5; // 增加頂部間距

                    SerializedProperty depotIDProp = element.FindPropertyRelative("depotID");
                    SerializedProperty platformLabelProp = element.FindPropertyRelative("platformLabel");
                    SerializedProperty contentRootProp = element.FindPropertyRelative("contentRoot");
                    SerializedProperty enableDRMProp = element.FindPropertyRelative("enableDRM");
                    SerializedProperty executableFileNameProp = element.FindPropertyRelative("executableFileName");

                    float lineHeight = EditorGUIUtility.singleLineHeight;
                    float spacing = 5f; // 增加行間距
                    float currentY = rect.y;

                    // 第一行：Depot ID
                    Rect depotIDRect = new Rect(rect.x, currentY, rect.width, lineHeight);
                    EditorGUI.PropertyField(depotIDRect, depotIDProp, new GUIContent("Depot ID"));
                    currentY += lineHeight + spacing;

                    // 第二行：平台標籤
                    Rect platformRect = new Rect(rect.x, currentY, rect.width, lineHeight);
                    EditorGUI.PropertyField(platformRect, platformLabelProp, new GUIContent("註解"));
                    currentY += lineHeight + spacing;

                    // 第三行：內容路徑和瀏覽按鈕
                    Rect pathRect = new Rect(rect.x, currentY, rect.width - 70, lineHeight);
                    Rect browseRect = new Rect(rect.x + rect.width - 65, currentY, 65, lineHeight);

                    EditorGUI.PropertyField(pathRect, contentRootProp, new GUIContent("路徑"));
                    
                    if (GUI.Button(browseRect, "瀏覽"))
                    {
                        string selectedPath = EditorUtility.OpenFolderPanel("選擇內容根目錄", contentRootProp.stringValue, "");
                        if (!string.IsNullOrEmpty(selectedPath))
                        {
                            contentRootProp.stringValue = selectedPath;
                            serializedObject.ApplyModifiedProperties();
                        }
                    }
                    currentY += lineHeight + spacing;

                    // DRM 設定折疊區塊
                    Rect drmFoldoutRect = new Rect(rect.x, currentY, rect.width, lineHeight);
                    drmFoldouts[index] = EditorGUI.Foldout(drmFoldoutRect, drmFoldouts[index], "Steam DRM 設定", true);
                    currentY += lineHeight + spacing;

                    if (drmFoldouts[index])
                    {
                        float indentWidth = 15f;
                        
                        // DRM 啟用開關
                        Rect enableDRMRect = new Rect(rect.x + indentWidth, currentY, rect.width - indentWidth, lineHeight);
                        enableDRMProp.boolValue = EditorGUI.Toggle(enableDRMRect, "啟用 DRM", enableDRMProp.boolValue);
                        currentY += lineHeight + spacing;

                        // 如果啟用 DRM，顯示執行檔設定
                        if (enableDRMProp.boolValue)
                        {
                            // 執行檔名稱欄位和按鈕
                            Rect exeLabelRect = new Rect(rect.x + indentWidth, currentY, EditorGUIUtility.labelWidth - indentWidth, lineHeight);
                            Rect exeFieldRect = new Rect(rect.x + EditorGUIUtility.labelWidth, currentY, rect.width - EditorGUIUtility.labelWidth - 70, lineHeight);
                            Rect exeBrowseRect = new Rect(rect.x + rect.width - 65, currentY, 65, lineHeight);

                            EditorGUI.LabelField(exeLabelRect, "執行檔");
                            executableFileNameProp.stringValue = EditorGUI.TextField(exeFieldRect, executableFileNameProp.stringValue);
                            
                            if (GUI.Button(exeBrowseRect, "選擇"))
                            {
                                string contentRoot = contentRootProp.stringValue;
                                if (!string.IsNullOrEmpty(contentRoot) && Directory.Exists(contentRoot))
                                {
                                    string selectedFile = EditorUtility.OpenFilePanel("選擇執行檔", contentRoot, "exe,app,x86_64,x86");
                                    if (!string.IsNullOrEmpty(selectedFile))
                                    {
                                        // 轉換為相對路徑
                                        if (selectedFile.StartsWith(contentRoot))
                                        {
                                            string relativePath = selectedFile.Substring(contentRoot.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                                            executableFileNameProp.stringValue = relativePath;
                                        }
                                        else
                                        {
                                            executableFileNameProp.stringValue = selectedFile;
                                        }
                                        serializedObject.ApplyModifiedProperties();
                                    }
                                }
                                else
                                {
                                    EditorUtility.DisplayDialog("錯誤", "請先設定內容根目錄", "確定");
                                }
                            }
                            currentY += lineHeight + spacing;

                            // 說明文字
                            Rect helpRect = new Rect(rect.x + indentWidth, currentY, rect.width - indentWidth, lineHeight * 2);
                            EditorGUI.HelpBox(helpRect, "原始執行檔將移至 SKIP_UPLOAD/ 資料夾，包裝後的檔案將替換原檔案。", MessageType.Info);
                            currentY += lineHeight * 2 + spacing;

                            // DRM 包裝按鈕
                            Rect drmButtonRect = new Rect(rect.x + indentWidth, currentY, rect.width - indentWidth, lineHeight + 5);
                            if (GUI.Button(drmButtonRect, "執行 DRM 包裝"))
                            {
                                ExecuteDRMWrapForDepot(index);
                            }
                            currentY += lineHeight + spacing + 5;
                        }
                    }
                },

                elementHeightCallback = index =>
                {
                    // 確保 foldout 陣列大小正確
                    if (drmFoldouts == null || drmFoldouts.Length != depotsProp.arraySize)
                    {
                        drmFoldouts = new bool[depotsProp.arraySize];
                    }

                    SerializedProperty element = depotsProp.GetArrayElementAtIndex(index);
                    SerializedProperty enableDRMProp = element.FindPropertyRelative("enableDRM");

                    float lineHeight = EditorGUIUtility.singleLineHeight;
                    float spacing = 5f;
                    
                    // 基本高度：3 行基本欄位 + DRM 折疊標題行 + 間距
                    float height = lineHeight * 4 + spacing * 5 + 10; // 頂部和底部額外間距

                    // 如果 DRM 折疊展開
                    if (drmFoldouts[index])
                    {
                        height += lineHeight + spacing; // DRM 啟用開關

                        // 如果啟用了 DRM，加上執行檔欄位、說明和按鈕的高度
                        if (enableDRMProp.boolValue)
                        {
                            height += lineHeight + spacing; // 執行檔路徑欄位
                            height += lineHeight * 2 + spacing; // 說明文字（兩行高度）
                            height += lineHeight + spacing + 5; // DRM 包裝按鈕
                        }
                    }

                    return height;
                },

                onAddCallback = list =>
                {
                    int index = list.serializedProperty.arraySize;
                    list.serializedProperty.arraySize++;
                    list.index = index;

                    SerializedProperty element = list.serializedProperty.GetArrayElementAtIndex(index);
                    element.FindPropertyRelative("depotID").stringValue = "";
                    element.FindPropertyRelative("platformLabel").stringValue = "新 Depot";
                    element.FindPropertyRelative("contentRoot").stringValue = "";
                    element.FindPropertyRelative("excludePatterns").arraySize = 0;
                    element.FindPropertyRelative("enableDRM").boolValue = false;
                    element.FindPropertyRelative("executableFileName").stringValue = "";

                    // 擴展 foldout 陣列
                    System.Array.Resize(ref drmFoldouts, list.serializedProperty.arraySize);

                    serializedObject.ApplyModifiedProperties();
                }
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Steam Build Configuration", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // 基本設定
            EditorGUILayout.LabelField("基本設定", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(appIDProp, new GUIContent("App ID"));
            EditorGUILayout.PropertyField(buildDescriptionProp, new GUIContent("建置描述"));

            EditorGUILayout.Space(10);

            EditorGUILayout.PropertyField(setLiveBranchProp, new GUIContent("釋出分支"));
            
            string branchValue = setLiveBranchProp.stringValue?.Trim().ToLower();
            
            if (!string.IsNullOrWhiteSpace(setLiveBranchProp.stringValue))
            {
                // 檢查是否為 default 分支
                if (branchValue == "default")
                {
                    EditorGUILayout.HelpBox(
                        "Beta分支名稱在構建成功後自動設定上線，如果為空，則不設定。 請注意，「default」分支不能設定自動釋出。 這必須透過網頁管理面板完成。",
                        MessageType.Warning);
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        $"⚠️ 警告：上傳完成後將立即釋出到分支 '{setLiveBranchProp.stringValue}'！\n" +
                        "此操作不可逆，玩家將立即看到新版本。\n" +
                        "建議：通常留空，在 Steamworks 網站手動釋出更安全。",
                        MessageType.Error);
                }
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Beta分支名稱在構建成功後自動設定上線，如果為空，則不設定。 請注意，「default」分支不能設定自動釋出。 這必須透過網頁管理面板完成。",
                    MessageType.Info);
            }

            EditorGUILayout.Space(10);

            // Depot 列表
            depotList.DoLayoutList();

            EditorGUILayout.Space(10);

            // 驗證按鈕
            if (GUILayout.Button("驗證配置", GUILayout.Height(30)))
            {
                ValidateConfig();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void ValidateConfig()
        {
            SteamBuildConfig config = (SteamBuildConfig)target;
            
            if (config.Validate(out var errors))
            {
                EditorUtility.DisplayDialog("驗證成功", "配置有效！", "確定");
            }
            else
            {
                string errorMsg = "驗證失敗:\n\n" + string.Join("\n", errors);
                EditorUtility.DisplayDialog("驗證失敗", errorMsg, "確定");
            }
        }

        private void ExecuteDRMWrapForDepot(int index)
        {
            serializedObject.ApplyModifiedProperties();
            
            SteamBuildConfig config = (SteamBuildConfig)target;
            
            if (index < 0 || index >= config.depots.Count)
            {
                EditorUtility.DisplayDialog("錯誤", "無效的 Depot 索引", "確定");
                return;
            }

            DepotConfig depot = config.depots[index];

            // 驗證基本設定
            if (string.IsNullOrWhiteSpace(config.appID))
            {
                EditorUtility.DisplayDialog("錯誤", "請先設定 App ID", "確定");
                return;
            }

            if (!depot.enableDRM)
            {
                EditorUtility.DisplayDialog("錯誤", "此 Depot 未啟用 DRM", "確定");
                return;
            }

            if (string.IsNullOrWhiteSpace(depot.executableFileName))
            {
                EditorUtility.DisplayDialog("錯誤", "請先設定執行檔名稱", "確定");
                return;
            }

            string exePath = Path.Combine(depot.contentRoot, depot.executableFileName);
            if (!File.Exists(exePath))
            {
                EditorUtility.DisplayDialog("錯誤", $"找不到執行檔：{exePath}", "確定");
                return;
            }

            // 確認對話框
            if (!EditorUtility.DisplayDialog(
                "確認 DRM 包裝",
                $"即將對 Depot [{depot.platformLabel}] 執行 DRM 包裝：\n\n" +
                $"執行檔: {depot.executableFileName}\n" +
                $"原始檔案將移至: SKIP_UPLOAD/\n\n" +
                "此操作不可逆，確定要繼續嗎？",
                "確定", "取消"))
            {
                return;
            }

            // 檢查 Steam 登入資訊
            var credentialManager = new SteamCredentialManager();
            credentialManager.Load();

            if (!credentialManager.IsValid(out string credError))
            {
                EditorUtility.DisplayDialog("錯誤", $"Steam 登入資訊不完整：\n{credError}\n\n請先在 Steam Pipe 視窗中設定登入資訊。", "確定");
                return;
            }

            // 檢查 SteamCMD
            var steamCMDManager = new SteamCMDManager();
            steamCMDManager.Initialize();

            if (!steamCMDManager.IsValid(out string steamError))
            {
                EditorUtility.DisplayDialog("錯誤", $"SteamCMD 未就緒：\n{steamError}\n\n請先在 Steam Pipe 視窗中設定 SteamCMD。", "確定");
                return;
            }

            // 執行 DRM 包裝
            EditorUtility.DisplayProgressBar("DRM 包裝", $"正在包裝 {depot.platformLabel}...", 0.5f);

            try
            {
                bool success = SteamDRMWrapper.ExecuteDRMWrap(
                    steamCMDManager.SteamCMDPath,
                    credentialManager.Username,
                    credentialManager.Password,
                    credentialManager.SteamGuardCode,
                    config.appID,
                    depot,
                    out string error
                );

                EditorUtility.ClearProgressBar();

                if (success)
                {
                    EditorUtility.DisplayDialog(
                        "DRM 包裝完成",
                        $"Depot [{depot.platformLabel}] DRM 包裝成功！\n\n" +
                        $"原始檔案已移至: {Path.Combine(depot.contentRoot, "SKIP_UPLOAD")}\n" +
                        $"包裝後檔案: {exePath}",
                        "確定");
                }
                else
                {
                    EditorUtility.DisplayDialog("DRM 包裝失敗", error, "確定");
                }
            }
            catch (System.Exception ex)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("DRM 包裝失敗", $"發生異常：\n{ex.Message}", "確定");
            }
        }
    }
}
