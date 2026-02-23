using System.Collections.Generic;
using UnityEngine;

namespace SteamPipeForUnity.Editor
{
    /// <summary>
    /// Steam 建置配置 ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "SteamBuildConfig", menuName = "Steam/Build Config", order = 1)]
    public class SteamBuildConfig : ScriptableObject
    {
        [Header("基本設定")]
        [Tooltip("Steam App ID")]
        public string appID = "";
        
        [Tooltip("建置描述")]
        [TextArea(3, 5)]
        public string buildDescription = "";
        
        [Space(5)]
        public string setLiveBranch = "";

        [Header("Depot 配置")]
        [Tooltip("多個 Depot 設定，支援多平台")]
        public List<DepotConfig> depots = new List<DepotConfig>();

        /// <summary>
        /// 驗證配置是否有效
        /// </summary>
        public bool Validate(out List<string> errors)
        {
            errors = new List<string>();

            if (string.IsNullOrWhiteSpace(appID))
            {
                errors.Add("App ID 不能為空");
            }

            if (depots == null || depots.Count == 0)
            {
                errors.Add("至少需要一個 Depot 配置");
            }
            else
            {
                for (int i = 0; i < depots.Count; i++)
                {
                    if (depots[i] == null)
                    {
                        errors.Add($"Depot[{i}] 為 null");
                        continue;
                    }

                    if (!depots[i].IsValid(out string error))
                    {
                        errors.Add($"Depot[{i}] ({depots[i].platformLabel}): {error}");
                    }
                }
            }

            return errors.Count == 0;
        }
    }
}
