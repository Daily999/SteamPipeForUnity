using System;
using System.Text;
using UnityEngine;

namespace SteamPipeForUnity.Editor
{
    /// <summary>
    /// 工具類：提供加密、路徑驗證等輔助方法
    /// </summary>
    public static class SteamPipeUtility
    {
        private const string XOR_KEY = "SteamPipeForUnity2026";

        /// <summary>
        /// 加密字串 (XOR + Base64)
        /// </summary>
        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;

            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] keyBytes = Encoding.UTF8.GetBytes(XOR_KEY);
            byte[] encryptedBytes = new byte[plainBytes.Length];

            for (int i = 0; i < plainBytes.Length; i++)
            {
                encryptedBytes[i] = (byte)(plainBytes[i] ^ keyBytes[i % keyBytes.Length]);
            }

            return Convert.ToBase64String(encryptedBytes);
        }

        /// <summary>
        /// 解密字串 (Base64 + XOR)
        /// </summary>
        public static string Decrypt(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText))
                return string.Empty;

            try
            {
                byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
                byte[] keyBytes = Encoding.UTF8.GetBytes(XOR_KEY);
                byte[] decryptedBytes = new byte[encryptedBytes.Length];

                for (int i = 0; i < encryptedBytes.Length; i++)
                {
                    decryptedBytes[i] = (byte)(encryptedBytes[i] ^ keyBytes[i % keyBytes.Length]);
                }

                return Encoding.UTF8.GetString(decryptedBytes);
            }
            catch (Exception e)
            {
                Debug.LogError($"解密失敗: {e.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// 驗證路徑是否存在
        /// </summary>
        public static bool ValidatePath(string path, out string error)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                error = "路徑不能為空";
                return false;
            }

            if (!System.IO.Directory.Exists(path) && !System.IO.File.Exists(path))
            {
                error = "路徑不存在";
                return false;
            }

            error = null;
            return true;
        }

        /// <summary>
        /// 格式化 VDF 檔案內容
        /// </summary>
        public static string FormatVDFValue(string value)
        {
            // VDF 格式需要雙引號包圍，並轉義特殊字元
            return $"\"{value.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";
        }

        /// <summary>
        /// 取得當前平台的 SteamCMD 執行檔名稱
        /// </summary>
        public static string GetSteamCMDExecutableName()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsEditor:
                    return "steamcmd.exe";
                case RuntimePlatform.OSXEditor:
                case RuntimePlatform.LinuxEditor:
                    return "steamcmd.sh";
                default:
                    return "steamcmd";
            }
        }

        /// <summary>
        /// 取得 SteamCMD 下載 URL
        /// </summary>
        public static string GetSteamCMDDownloadURL()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsEditor:
                    return "https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip";
                case RuntimePlatform.OSXEditor:
                    return "https://steamcdn-a.akamaihd.net/client/installer/steamcmd_osx.tar.gz";
                case RuntimePlatform.LinuxEditor:
                    return "https://steamcdn-a.akamaihd.net/client/installer/steamcmd_linux.tar.gz";
                default:
                    return null;
            }
        }
    }
}
