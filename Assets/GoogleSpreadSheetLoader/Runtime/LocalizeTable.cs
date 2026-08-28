using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;
// using Util;

[System.Serializable]
public class LocalizeKeyValue
{
    public string Key;
    public string Value;
}

public static class LocalizeTable
{
    private static Dictionary<string, string> dicLocalize = new();
    public static UnityAction OnChangedLanguage;

    public static void Initialize(SystemLanguage language)
    {
        var assetName = $"Localize_";
        switch (language)
        {
            case SystemLanguage.Korean:
            case SystemLanguage.English:
                assetName += $"{language}";
                break;
            default:
                // Util.Debug.LogError($"해당 국가코드 ({language}) 는 정의 되지 않아 en으로 대체합니다.");
                assetName += $"{SystemLanguage.English}";
                break;
        }

        var obj = Resources.Load<TextAsset>(assetName);

        // 배열 형태의 JSON을 먼저 역직렬화
        var localizeArray = JsonConvert.DeserializeObject<LocalizeKeyValue[]>(obj.text);

        // 딕셔너리로 변환
        dicLocalize.Clear();
        foreach (var item in localizeArray)
        {
            if (!string.IsNullOrEmpty(item.Key))
            {
                dicLocalize[item.Key] = item.Value ?? string.Empty;
            }
        }
    }

    public static void ChangeLanguage(SystemLanguage language)
    {
        Initialize(language);

        // LanguageUtil.SetLanguageCode(language);

        OnChangedLanguage?.Invoke();
    }

    public static string GetLocalizeText(this string key, params object[] param)
    {
        if (dicLocalize.TryGetValue(key, out var result))
        {
            return string.Format(result, param);
        }

        return "!" + key;
    }

#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
    private static void InitializeOnLoadMethod()
    {
        OnChangedLanguage = null;
    }
#endif
}