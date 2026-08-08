using System;
using UnityEngine;

namespace Util
{
    public class LanguageUtil
    {
        private static string prefsKey = "language_code";

        private static bool HasSaveLanguageCode => PlayerPrefs.HasKey(prefsKey);

        private static string languageCode
        {
            get => PlayerPrefs.GetString(prefsKey);
            set => PlayerPrefs.SetString(prefsKey, value);
        }

        public static SystemLanguage GetLanguageCode()
        {
            if (!HasSaveLanguageCode)
            {
                var systemLang = Application.systemLanguage;

                if (systemLang == SystemLanguage.Unknown)
                {
                    systemLang = SystemLanguage.English;
                }

                languageCode = systemLang.ToString();

                return systemLang;
            }

            return Enum.Parse<SystemLanguage>(languageCode);
        }

        public static void SetLanguageCode(SystemLanguage language)
        {
            languageCode = language.ToString();
        }
    }
}