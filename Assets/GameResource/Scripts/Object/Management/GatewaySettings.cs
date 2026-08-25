using UnityEngine;

namespace Backend.Object.Management
{
    /// <summary>
    /// 접속 경로와 LAN 호스트 IP를 PlayerPrefs에 저장한다.
    /// </summary>
    public static class GatewaySettings
    {
        private const string PREF_LAN_HOST = "gateway_url";
        private const string PREF_CONNECTION_MODE = "connection_mode";

        /// <summary>LAN 게스트가 붙을 호스트 IP. 릴레이 모드에서는 쓰지 않는다.</summary>
        public static string LanHost => PlayerPrefs.GetString(PREF_LAN_HOST, string.Empty);

        /// <summary>로비에서 고른 접속 경로.</summary>
        public static ConnectionMode Mode
        {
            get
            {
                var value = PlayerPrefs.GetInt(PREF_CONNECTION_MODE, (int)ConnectionMode.Relay);
                if (value == (int)ConnectionMode.Relay || value == (int)ConnectionMode.Lan)
                {
                    return (ConnectionMode)value;
                }

                return ConnectionMode.Relay;
            }
        }

        /// <summary>LAN 호스트 IP를 저장한다. 공백이면 삭제한다.</summary>
        public static void SaveLanHost(string raw)
        {
            var trimmed = raw != null ? raw.Trim() : string.Empty;
            if (trimmed.Length == 0)
            {
                PlayerPrefs.DeleteKey(PREF_LAN_HOST);
                PlayerPrefs.Save();
                return;
            }

            PlayerPrefs.SetString(PREF_LAN_HOST, trimmed);
            PlayerPrefs.Save();
        }

        /// <summary>접속 경로를 저장한다.</summary>
        public static void SaveMode(ConnectionMode mode)
        {
            PlayerPrefs.SetInt(PREF_CONNECTION_MODE, (int)mode);
            PlayerPrefs.Save();
        }
    }
}
