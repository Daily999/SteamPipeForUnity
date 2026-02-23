using UnityEditor;
using UnityEngine;

namespace SteamPipeForUnity.Editor
{
    /// <summary>
    /// Steam 登入憑證管理器
    /// </summary>
    public class SteamCredentialManager
    {
        private const string PREF_USERNAME = "SteamPipe.Username";
        private const string PREF_PASSWORD = "SteamPipe.Password";
        private const string PREF_REMEMBER = "SteamPipe.RememberLogin";

        private string username = "";
        private string password = "";
        private string steamGuardCode = "";
        private bool rememberLogin = false;

        public string Username => username;
        public string Password => password;
        public string SteamGuardCode => steamGuardCode;
        public bool RememberLogin => rememberLogin;

        /// <summary>
        /// 載入儲存的憑證
        /// </summary>
        public void Load()
        {
            rememberLogin = EditorPrefs.GetBool(PREF_REMEMBER, false);
            
            if (rememberLogin)
            {
                string encryptedUsername = EditorPrefs.GetString(PREF_USERNAME, "");
                string encryptedPassword = EditorPrefs.GetString(PREF_PASSWORD, "");

                username = SteamPipeUtility.Decrypt(encryptedUsername);
                password = SteamPipeUtility.Decrypt(encryptedPassword);
            }
            else
            {
                username = "";
                password = "";
            }

            steamGuardCode = ""; // Steam Guard 每次都要重新輸入
        }

        /// <summary>
        /// 儲存憑證
        /// </summary>
        public void Save()
        {
            EditorPrefs.SetBool(PREF_REMEMBER, rememberLogin);

            if (rememberLogin)
            {
                string encryptedUsername = SteamPipeUtility.Encrypt(username);
                string encryptedPassword = SteamPipeUtility.Encrypt(password);

                EditorPrefs.SetString(PREF_USERNAME, encryptedUsername);
                EditorPrefs.SetString(PREF_PASSWORD, encryptedPassword);
            }
            else
            {
                // 不記住則清除
                EditorPrefs.DeleteKey(PREF_USERNAME);
                EditorPrefs.DeleteKey(PREF_PASSWORD);
            }
        }

        /// <summary>
        /// 清除所有儲存的憑證
        /// </summary>
        public void Clear()
        {
            username = "";
            password = "";
            steamGuardCode = "";
            rememberLogin = false;

            EditorPrefs.DeleteKey(PREF_USERNAME);
            EditorPrefs.DeleteKey(PREF_PASSWORD);
            EditorPrefs.DeleteKey(PREF_REMEMBER);

            Debug.Log("已清除所有儲存的 Steam 憑證");
        }

        /// <summary>
        /// 驗證憑證是否有效
        /// </summary>
        public bool IsValid(out string error)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                error = "Steam 使用者名稱不能為空";
                return false;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                error = "Steam 密碼不能為空";
                return false;
            }

            error = null;
            return true;
        }

        /// <summary>
        /// 繪製 GUI
        /// </summary>
        public void OnGUI()
        {
            EditorGUILayout.LabelField("Steam 登入憑證", EditorStyles.boldLabel);
            
            username = EditorGUILayout.TextField("使用者名稱", username);
            password = EditorGUILayout.PasswordField("密碼", password);
            steamGuardCode = EditorGUILayout.TextField("Steam Guard 代碼", steamGuardCode);
            
            EditorGUILayout.BeginHorizontal();
            rememberLogin = EditorGUILayout.Toggle("記住登入", rememberLogin);
            
            if (GUILayout.Button("清除憑證", GUILayout.Width(100)))
            {
                if (EditorUtility.DisplayDialog("確認", "確定要清除所有儲存的 Steam 憑證嗎？", "確定", "取消"))
                {
                    Clear();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox("Steam Guard 代碼每次上傳時需要重新輸入", MessageType.Info);
        }

        /// <summary>
        /// 設定使用者名稱
        /// </summary>
        public void SetUsername(string value)
        {
            username = value;
        }

        /// <summary>
        /// 設定密碼
        /// </summary>
        public void SetPassword(string value)
        {
            password = value;
        }

        /// <summary>
        /// 設定 Steam Guard 代碼
        /// </summary>
        public void SetSteamGuardCode(string value)
        {
            steamGuardCode = value;
        }

        /// <summary>
        /// 設定記住登入
        /// </summary>
        public void SetRememberLogin(bool value)
        {
            rememberLogin = value;
        }
    }
}
