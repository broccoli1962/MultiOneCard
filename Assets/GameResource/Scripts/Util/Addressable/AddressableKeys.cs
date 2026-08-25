// Auto Generate Code.
using System.Collections.Generic;

namespace Backend.AddressableKey
{
    public static class AddressableKeys
    {
        public static class InGame
        {
            private static readonly Dictionary<string, string> Keys = new Dictionary<string, string>()
            {
            };

            public static string Get<T>() => Keys.TryGetValue(typeof(T).Name, out var key) ? key : null;
            public static string Get(string keyName) => Keys.TryGetValue(keyName, out var key) ? key : null;
        }

        public static class Cards
        {
            private static readonly Dictionary<string, string> Keys = new Dictionary<string, string>()
            {
                { "D5", "Cards/D5" },
                { "JOKER:BW", "Cards/JOKER:BW" },
                { "H7", "Cards/H7" },
                { "M4", "Cards/M4" },
                { "D2", "Cards/D2" },
                { "{CardDefId}", "Cards/{CardDefId}" },
                { "M7", "Cards/M7" },
                { "D3", "Cards/D3" },
                { "BACK", "Cards/BACK" },
                { "SPEC:PILL_BL", "Cards/SPEC:PILL_BL" },
                { "HA", "Cards/HA" },
                { "RJ", "Cards/RJ" },
                { "M6", "Cards/M6" },
                { "C6", "Cards/C6" },
                { "H6", "Cards/H6" },
                { "M9", "Cards/M9" },
                { "H10", "Cards/H10" },
                { "CK", "Cards/CK" },
                { "S2", "Cards/S2" },
                { "HQ", "Cards/HQ" },
                { "SQ", "Cards/SQ" },
                { "C5", "Cards/C5" },
                { "R7", "Cards/R7" },
                { "HK", "Cards/HK" },
                { "R10", "Cards/R10" },
                { "R2", "Cards/R2" },
                { "D6", "Cards/D6" },
                { "DA", "Cards/DA" },
                { "D7", "Cards/D7" },
                { "C3", "Cards/C3" },
                { "RA", "Cards/RA" },
                { "SPEC:REVJOKER", "Cards/SPEC:REVJOKER" },
                { "R6", "Cards/R6" },
                { "S9", "Cards/S9" },
                { "C8", "Cards/C8" },
                { "R3", "Cards/R3" },
                { "SPEC:COUNTER", "Cards/SPEC:COUNTER" },
                { "RK", "Cards/RK" },
                { "SPEC:PASS", "Cards/SPEC:PASS" },
                { "M2", "Cards/M2" },
                { "C2", "Cards/C2" },
                { "S8", "Cards/S8" },
                { "R4", "Cards/R4" },
                { "S5", "Cards/S5" },
                { "C4", "Cards/C4" },
                { "H2", "Cards/H2" },
                { "C9", "Cards/C9" },
                { "H5", "Cards/H5" },
                { "CJ", "Cards/CJ" },
                { "SA", "Cards/SA" },
                { "D8", "Cards/D8" },
                { "SPEC:PILL_RD", "Cards/SPEC:PILL_RD" },
                { "SK", "Cards/SK" },
                { "H8", "Cards/H8" },
                { "SPEC:MIRROR", "Cards/SPEC:MIRROR" },
                { "JOKER:MOON", "Cards/JOKER:MOON" },
                { "R5", "Cards/R5" },
                { "HJ", "Cards/HJ" },
                { "D9", "Cards/D9" },
                { "C7", "Cards/C7" },
                { "DJ", "Cards/DJ" },
                { "MA", "Cards/MA" },
                { "M8", "Cards/M8" },
                { "R8", "Cards/R8" },
                { "MK", "Cards/MK" },
                { "MJ", "Cards/MJ" },
                { "H3", "Cards/H3" },
                { "S4", "Cards/S4" },
                { "CQ", "Cards/CQ" },
                { "RQ", "Cards/RQ" },
                { "C10", "Cards/C10" },
                { "M5", "Cards/M5" },
                { "S3", "Cards/S3" },
                { "H4", "Cards/H4" },
                { "M3", "Cards/M3" },
                { "D10", "Cards/D10" },
                { "DQ", "Cards/DQ" },
                { "S7", "Cards/S7" },
                { "SJ", "Cards/SJ" },
                { "H9", "Cards/H9" },
                { "M10", "Cards/M10" },
                { "DK", "Cards/DK" },
                { "JOKER:COLOR", "Cards/JOKER:COLOR" },
                { "R9", "Cards/R9" },
                { "S6", "Cards/S6" },
                { "SPEC:SPEAR", "Cards/SPEC:SPEAR" },
                { "D4", "Cards/D4" },
                { "SPEC:PILL_BK", "Cards/SPEC:PILL_BK" },
                { "MQ", "Cards/MQ" },
                { "S10", "Cards/S10" },
                { "CA", "Cards/CA" },
            };

            public static string Get<T>() => Keys.TryGetValue(typeof(T).Name, out var key) ? key : null;
            public static string Get(string keyName) => Keys.TryGetValue(keyName, out var key) ? key : null;
        }

        public static class UI
        {
            private static readonly Dictionary<string, string> Keys = new Dictionary<string, string>()
            {
                { "UI_RoomPanel_prefab", "UI/RoomPanel.prefab" },
                { "UI_TitlePanel_prefab", "UI/TitlePanel.prefab" },
                { "UI_ResultPanel_prefab", "UI/ResultPanel.prefab" },
                { "LobbyPanel", "UI/LobbyPanel.prefab" },
                { "MatchPanel", "UI/MatchPanel.prefab" },
                { "ResultPanel", "UI/ResultPanel.prefab" },
                { "RoomPanel", "UI/RoomPanel.prefab" },
                { "SettingsPopup", "UI/SettingsPopup.prefab" },
                { "TitlePanel", "UI/TitlePanel.prefab" },
                { "UIRoot", "UI/UIRoot.prefab" },
                { "UI_LobbyPanel_prefab", "UI/LobbyPanel.prefab" },
                { "UI_MatchPanel_prefab", "UI/MatchPanel.prefab" },
                { "UI_SettingsPopup_prefab", "UI/SettingsPopup.prefab" },
            };

            public static string Get<T>() => Keys.TryGetValue(typeof(T).Name, out var key) ? key : null;
            public static string Get(string keyName) => Keys.TryGetValue(keyName, out var key) ? key : null;
        }

        public static class Sounds
        {
            private static readonly Dictionary<string, string> Keys = new Dictionary<string, string>()
            {
                { "AudioMixer", "Assets/GameResource/Sounds/AudioMixer.mixer" },
                { "Card_Flip", "Assets/GameResource/Sounds/Sfx/Card_Flip.wav" },
            };

            public static string Get<T>() => Keys.TryGetValue(typeof(T).Name, out var key) ? key : null;
            public static string Get(string keyName) => Keys.TryGetValue(keyName, out var key) ? key : null;
        }

    }
}
