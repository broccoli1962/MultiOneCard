using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GoogleSpreadSheetLoader
{
    public static class GSSL_CacheManager
    {
        private static readonly string CacheDirectory = Path.Combine(Application.dataPath, "GoogleSpreadSheetLoader", "Generated", "Cache");
        private static readonly string CacheIndexFile = Path.Combine(CacheDirectory, "cache_index.json");
        
        [Serializable]
        public class CacheInfo
        {
            private const string LastUpdatedFormat = "o";

            public string spreadSheetId;
            public string spreadSheetName;
            public string sheetName;
            public string fileName;
            // JsonUtility는 DateTime을 직렬화하지 않으므로 문자열로 저장한다.
            public string lastUpdated;
            public SheetData.eTableStyle tableStyle;

            public DateTime LastUpdatedDateTime
            {
                get
                {
                    if (string.IsNullOrEmpty(lastUpdated))
                        return DateTime.MinValue;

                    return DateTime.TryParse(
                        lastUpdated,
                        null,
                        System.Globalization.DateTimeStyles.RoundtripKind,
                        out var parsed)
                        ? parsed
                        : DateTime.MinValue;
                }
                set => lastUpdated = value.ToString(LastUpdatedFormat);
            }
        }
        
        [Serializable]
        public class CacheIndex
        {
            public List<CacheInfo> cacheInfos = new List<CacheInfo>();
        }
        
        static GSSL_CacheManager()
        {
            InitializeCacheDirectory();
        }
        
        private static void InitializeCacheDirectory()
        {
            if (!Directory.Exists(CacheDirectory))
            {
                Directory.CreateDirectory(CacheDirectory);
                AssetDatabase.Refresh();
            }
        }
        
        public static void SaveSheetToCache(string spreadSheetId, string spreadSheetName, string sheetName, string data, SheetData.eTableStyle tableStyle)
        {
            try
            {
                InitializeCacheDirectory();
                
                var fileName = GetSafeFileName(sheetName) + ".txt";
                var filePath = Path.Combine(CacheDirectory, fileName);
                
                File.WriteAllText(filePath, data);
                
                UpdateCacheIndex(spreadSheetId, spreadSheetName, sheetName, fileName, tableStyle);
                
                AssetDatabase.Refresh();
            }
            catch (Exception e)
            {
                Debug.LogError($"캐시 저장 실패 - {sheetName}: {e.Message}");
            }
        }
        
        public static string LoadSheetFromCache(string sheetName)
        {
            try
            {
                var cacheInfo = GetCacheInfo(sheetName);
                if (cacheInfo == null)
                    return null;
                    
                var filePath = Path.Combine(CacheDirectory, cacheInfo.fileName);
                if (!File.Exists(filePath))
                    return null;
                    
                return File.ReadAllText(filePath);
            }
            catch (Exception e)
            {
                Debug.LogError($"캐시 로드 실패 - {sheetName}: {e.Message}");
                return null;
            }
        }
        
        public static List<CacheInfo> GetAllCachedSheets()
        {
            try
            {
                if (!File.Exists(CacheIndexFile))
                    return new List<CacheInfo>();
                    
                var json = File.ReadAllText(CacheIndexFile);
                var cacheIndex = JsonUtility.FromJson<CacheIndex>(json);
                
                // 실제 파일이 존재하는지 확인하고 정리
                var validCacheInfos = cacheIndex.cacheInfos.Where(info =>
                {
                    var filePath = Path.Combine(CacheDirectory, info.fileName);
                    return File.Exists(filePath);
                }).ToList();

                var needsSave = validCacheInfos.Count != cacheIndex.cacheInfos.Count;

                // JsonUtility DateTime 미지원으로 과거 인덱스에 lastUpdated가 비어 있을 수 있다.
                // 캐시 파일의 수정 시각으로 보완한다.
                foreach (var info in validCacheInfos)
                {
                    if (!string.IsNullOrEmpty(info.lastUpdated))
                        continue;

                    var filePath = Path.Combine(CacheDirectory, info.fileName);
                    info.LastUpdatedDateTime = File.GetLastWriteTime(filePath);
                    needsSave = true;
                }
                
                if (needsSave)
                {
                    cacheIndex.cacheInfos = validCacheInfos;
                    SaveCacheIndex(cacheIndex);
                }
                
                return validCacheInfos;
            }
            catch (Exception e)
            {
                Debug.LogError($"캐시 목록 로드 실패: {e.Message}");
                return new List<CacheInfo>();
            }
        }
        
        public static CacheInfo GetCacheInfo(string sheetName)
        {
            var allCached = GetAllCachedSheets();
            return allCached.FirstOrDefault(info => info.sheetName == sheetName);
        }
        
        public static void ClearCache()
        {
            try
            {
                if (Directory.Exists(CacheDirectory))
                {
                    Directory.Delete(CacheDirectory, true);
                }
                InitializeCacheDirectory();
                AssetDatabase.Refresh();
            }
            catch (Exception e)
            {
                Debug.LogError($"캐시 클리어 실패: {e.Message}");
            }
        }
        
        public static void RemoveFromCache(string sheetName)
        {
            try
            {
                var cacheInfo = GetCacheInfo(sheetName);
                if (cacheInfo == null)
                    return;
                    
                var filePath = Path.Combine(CacheDirectory, cacheInfo.fileName);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                
                var allCached = GetAllCachedSheets();
                allCached.RemoveAll(info => info.sheetName == sheetName);
                
                var cacheIndex = new CacheIndex { cacheInfos = allCached };
                SaveCacheIndex(cacheIndex);
                
                AssetDatabase.Refresh();
            }
            catch (Exception e)
            {
                Debug.LogError($"캐시 삭제 실패 - {sheetName}: {e.Message}");
            }
        }
        
        private static void UpdateCacheIndex(string spreadSheetId, string spreadSheetName, string sheetName, string fileName, SheetData.eTableStyle tableStyle)
        {
            var cacheIndex = LoadCacheIndex();
            
            // 기존 항목 제거
            cacheIndex.cacheInfos.RemoveAll(info => info.sheetName == sheetName);
            
            // 새 항목 추가
            var cacheInfo = new CacheInfo
            {
                spreadSheetId = spreadSheetId,
                spreadSheetName = spreadSheetName,
                sheetName = sheetName,
                fileName = fileName,
                tableStyle = tableStyle
            };
            cacheInfo.LastUpdatedDateTime = DateTime.Now;
            cacheIndex.cacheInfos.Add(cacheInfo);
            
            SaveCacheIndex(cacheIndex);
        }
        
        private static CacheIndex LoadCacheIndex()
        {
            try
            {
                if (!File.Exists(CacheIndexFile))
                    return new CacheIndex();
                    
                var json = File.ReadAllText(CacheIndexFile);
                return JsonUtility.FromJson<CacheIndex>(json);
            }
            catch (Exception)
            {
                return new CacheIndex();
            }
        }
        
        private static void SaveCacheIndex(CacheIndex cacheIndex)
        {
            try
            {
                var json = JsonUtility.ToJson(cacheIndex, true);
                File.WriteAllText(CacheIndexFile, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"캐시 인덱스 저장 실패: {e.Message}");
            }
        }
        
        private static string GetSafeFileName(string sheetName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var safeName = new string(sheetName.Where(c => !invalidChars.Contains(c)).ToArray());
            
            if (string.IsNullOrEmpty(safeName))
            {
                safeName = "sheet_" + Guid.NewGuid().ToString("N")[..8];
            }
            
            return safeName;
        }
    }
}
