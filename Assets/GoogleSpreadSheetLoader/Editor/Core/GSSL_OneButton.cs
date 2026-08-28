using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using GoogleSpreadSheetLoader.Download;
using GoogleSpreadSheetLoader.Generate;
using GoogleSpreadSheetLoader.Setting;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using static GoogleSpreadSheetLoader.SheetData;
using static GoogleSpreadSheetLoader.GSSL_State;

namespace GoogleSpreadSheetLoader.OneButton
{
    public static class GSSL_OneButton
    {
        private static readonly string GenerateDataPrefsKey = "GenerateData";
        private static bool GenerateDataFlag => EditorPrefs.HasKey(GenerateDataPrefsKey);
        private static string GenerateDataString
        {
            get => EditorPrefs.HasKey(GenerateDataPrefsKey) ? EditorPrefs.GetString(GenerateDataPrefsKey) : string.Empty;
            set
            {
                if (string.IsNullOrEmpty(value))
                    EditorPrefs.DeleteKey(GenerateDataPrefsKey);
                else
                    EditorPrefs.SetString(GenerateDataPrefsKey, value);
            }
        }
        
        // 취소 관련 필드들
        private static CancellationTokenSource _cancellationTokenSource;
        public static bool IsProcessRunning => _cancellationTokenSource != null && !_cancellationTokenSource.Token.IsCancellationRequested;

        public static bool TableLinkerFlag
        {
            get => EditorPrefs.HasKey("TableLinkerLink");
            set
            {
                if (value)
                {
                    EditorPrefs.SetString("TableLinkerLink", true.ToString());
                }
                else
                {
                    EditorPrefs.DeleteKey("TableLinkerLink");
                }
            }
        }

        /// <summary>
        /// 현재 진행중인 프로세스를 취소합니다.
        /// </summary>
        public static void CancelCurrentProcess()
        {
            if (_cancellationTokenSource != null && !_cancellationTokenSource.Token.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
                GSSL_Log.Log("프로세스 취소가 요청되었습니다.");
            }
            
            // 상태를 None으로 설정
            SetProgressState(eGSSL_State.None);
        }

        /// <summary>
        /// 특정 스프레드시트 하나만 최신화합니다.
        /// </summary>
        public static async Awaitable OneButtonProcessSingleSpreadSheet(string spreadSheetId, CancellationToken cancellationToken = default)
        {
            // 이전 작업이 진행중이면 취소
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
            }
            
            // 새로운 취소 토큰 생성
            _cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationToken == default ? _cancellationTokenSource.Token : cancellationToken;
            
            try
            {
                var spreadSheetInfo = GSSL_Setting.SettingData.listSpreadSheetInfo
                    .FirstOrDefault(x => x.spreadSheetId == spreadSheetId);
                
                if (spreadSheetInfo == null)
                {
                    GSSL_Log.LogError($"스프레드시트를 찾을 수 없습니다: {spreadSheetId}");
                    return;
                }
                
                GSSL_Log.Log($"스프레드시트 최신화 시작: {spreadSheetInfo.spreadSheetName}");
                
                token.ThrowIfCancellationRequested();
                
                GSSL_Log.Log("Download SpreadSheet Start");
                var listDownloadInfo = await GSSL_Download.DownloadSingleSpreadSheet(spreadSheetId, token);
                GSSL_Log.Log("Download SpreadSheet Done");

                if (listDownloadInfo.Count == 0)
                {
                    GSSL_Log.Log($"다운로드할 시트가 없습니다: {spreadSheetInfo.spreadSheetName}");
                    return;
                }

                GSSL_Log.Log("Download Sheet Start");
                await GSSL_Download.DownloadSheet(listDownloadInfo, token);
                GSSL_Log.Log("Download Sheet Done");

                token.ThrowIfCancellationRequested();

                await ProcessSingleSpreadSheetSheets(spreadSheetId, token);
            }
            catch (OperationCanceledException)
            {
                GSSL_Log.Log("프로세스가 취소되었습니다.");
                SetProgressState(eGSSL_State.None);
            }
            catch (Exception ex)
            {
                GSSL_Log.LogError($"프로세스 중 에러가 발생했습니다: {ex.Message}");
                SetProgressState(eGSSL_State.None);
            }
            finally
            {
                // 리소스 정리
                if (cancellationToken == default)
                {
                    _cancellationTokenSource?.Dispose();
                    _cancellationTokenSource = null;
                }
            }
        }

        /// <summary>
        /// 특정 스프레드시트의 시트들을 처리합니다. Enum 우선, Localization은 캐시의 모든 Localization 시트와 함께 처리.
        /// </summary>
        private static async Awaitable ProcessSingleSpreadSheetSheets(string spreadSheetId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var allSheetData = GSSL_DownloadedSheet.GetAllSheetData();
            var targetSheets = allSheetData.Where(x => x.spreadSheetId == spreadSheetId).ToList();

            // Localization 시트가 있는지 확인
            bool hasLocalizationSheet = targetSheets.Any(x => x.tableStyle == eTableStyle.Localization);

            // Localization 시트가 있는 경우, 캐시의 모든 Localization 시트를 함께 처리
            if (hasLocalizationSheet)
            {
                var cachedSheets = GSSL_CacheManager.GetAllCachedSheets();
                var allLocalizationSheets = cachedSheets
                    .Where(sheet => sheet.tableStyle == SheetData.eTableStyle.Localization)
                    .Select(sheet => sheet.sheetName)
                    .Distinct()
                    .ToList();

                // 다운로드된 시트와 캐시의 Localization 시트를 합침
                var downloadedSheetTitles = allSheetData.Select(x => x.title).ToList();
                var missingLocalizationSheets = allLocalizationSheets
                    .Where(name => !downloadedSheetTitles.Contains(name))
                    .ToList();

                if (missingLocalizationSheets.Count > 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    // 캐시에서 누락된 Localization 시트들 로드
                    foreach (var sheetName in missingLocalizationSheets)
                    {
                        var cacheInfo = cachedSheets.FirstOrDefault(c => c.sheetName == sheetName);
                        if (cacheInfo == null) continue;
                        
                        var cachedData = GSSL_CacheManager.LoadSheetFromCache(sheetName);
                        if (string.IsNullOrEmpty(cachedData)) continue;
                        
                        var sheetData = new SheetData
                        {
                            spreadSheetId = cacheInfo.spreadSheetId,
                            title = cacheInfo.sheetName,
                            tableStyle = cacheInfo.tableStyle,
                            data = cachedData
                        };
                        
                        GSSL_DownloadedSheet.AddSheetData(sheetData);
                    }
                    
                    GSSL_Log.Log($"캐시에서 {missingLocalizationSheets.Count}개의 Localization 시트를 추가로 로드했습니다.");
                    
                    // 모든 시트 데이터 다시 가져오기
                    allSheetData = GSSL_DownloadedSheet.GetAllSheetData();
                }

                // Localization 시트 목록 업데이트 (다운로드된 것 + 캐시에서 로드한 것)
                var allLocalizationSheetData = allSheetData
                    .Where(x => x.tableStyle == eTableStyle.Localization)
                    .ToList();
                
                targetSheets = targetSheets
                    .Where(x => x.tableStyle != eTableStyle.Localization)
                    .Concat(allLocalizationSheetData)
                    .ToList();
            }

            var dicSheetData = new Dictionary<eTableStyle, List<SheetData>>();

            dicSheetData.TryAdd(eTableStyle.EnumType, new());
            dicSheetData.TryAdd(eTableStyle.Common, new());
            dicSheetData.TryAdd(eTableStyle.Localization, new());

            foreach (var sheetData in targetSheets)
            {
                dicSheetData[sheetData.tableStyle].Add(sheetData);
            }

            await Task.Delay(1);
            cancellationToken.ThrowIfCancellationRequested();

            SetProgressState(eGSSL_State.GenerateTableScript);
            
            // Enum 타입을 먼저 처리
            if (dicSheetData[eTableStyle.EnumType].Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                GSSL_Generate.GenerateEnumDef(dicSheetData[eTableStyle.EnumType]);
            }
            
            // Common 타입 처리
            if (dicSheetData[eTableStyle.Common].Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                GSSL_Generate.GenerateTableScripts(dicSheetData[eTableStyle.Common]);
            }
            
            // Localization 타입 처리 (캐시의 모든 Localization 시트와 함께)
            if (dicSheetData[eTableStyle.Localization].Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                GSSL_Generate.GenerateLocalize(dicSheetData[eTableStyle.Localization]);
            }

            // TableLinker 생성 시 캐시의 모든 Common 타입 시트들도 함께 포함
            cancellationToken.ThrowIfCancellationRequested();
            
            var allCachedSheets = GSSL_CacheManager.GetAllCachedSheets();
            var allCommonSheets = allCachedSheets
                .Where(sheet => sheet.tableStyle == SheetData.eTableStyle.Common)
                .Select(sheet => sheet.sheetName)
                .Distinct()
                .ToList();
            
            // 다운로드된 시트 목록 가져오기 (이전에 업데이트된 allSheetData 사용)
            allSheetData = GSSL_DownloadedSheet.GetAllSheetData();
            var downloadedCommonSheetTitles = allSheetData.Select(x => x.title).ToList();
            
            // 캐시에는 있지만 다운로드되지 않은 Common 시트들을 캐시에서 로드
            var missingCommonSheets = allCommonSheets
                .Where(name => !downloadedCommonSheetTitles.Contains(name))
                .ToList();
            
            if (missingCommonSheets.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                // 캐시에서 누락된 Common 시트들 로드
                foreach (var sheetName in missingCommonSheets)
                {
                    var cacheInfo = allCachedSheets.FirstOrDefault(c => c.sheetName == sheetName);
                    if (cacheInfo == null) continue;
                    
                    var cachedData = GSSL_CacheManager.LoadSheetFromCache(sheetName);
                    if (string.IsNullOrEmpty(cachedData)) continue;
                    
                    var sheetData = new SheetData
                    {
                        spreadSheetId = cacheInfo.spreadSheetId,
                        title = cacheInfo.sheetName,
                        tableStyle = cacheInfo.tableStyle,
                        data = cachedData
                    };
                    
                    GSSL_DownloadedSheet.AddSheetData(sheetData);
                }
                
                GSSL_Log.Log($"TableLinker 생성을 위해 캐시에서 {missingCommonSheets.Count}개의 Common 시트를 추가로 로드했습니다.");
            }
            
            // Common 타입만 데이터 생성용으로 저장
            allSheetData = GSSL_DownloadedSheet.GetAllSheetData();
            var allCommonSheetData = allSheetData
                .Where(x => x.tableStyle == eTableStyle.Common)
                .ToList();
            
            // 새로 다운로드한 Common 시트만 데이터 생성용으로 저장
            var newCommonTables = dicSheetData[eTableStyle.Common];
            if (newCommonTables.Count > 0)
            {
                var commonDic = new Dictionary<eTableStyle, List<SheetData>>
                {
                    { eTableStyle.Common, newCommonTables }
                };
                var str = JsonConvert.SerializeObject(commonDic);
                GenerateDataString = str;
                TableLinkerFlag = true;
            }

            cancellationToken.ThrowIfCancellationRequested();

            // 모든 Common 시트를 포함하여 TableLinker 생성
            GSSL_Generate.GenerateTableLinkerScript();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            CheckPrefsAndGenerateTableData();
        }

        public static async Awaitable OneButtonProcessSpreadSheet(bool isClearGeneratedFolder = true)
        {
            // 이전 작업이 진행중이면 취소
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
            }
            
            // 새로운 취소 토큰 생성
            _cancellationTokenSource = new CancellationTokenSource();
            
            try
            {
                if (isClearGeneratedFolder)
                {
                    GSSL_Path.ClearGeneratedFolder();
                }
                
                GSSL_Log.Log("Download SpreadSheet Start");
                var listDownloadInfo = await GSSL_Download.DownloadSpreadSheetAll(_cancellationTokenSource.Token);
                GSSL_Log.Log("Download SpreadSheet Done");

                GSSL_Log.Log("Download Sheet Start");
                await OneButtonProcessSheet(listDownloadInfo, _cancellationTokenSource.Token);
                GSSL_Log.Log("Download Sheet Done");
            }
            catch (OperationCanceledException)
            {
                GSSL_Log.Log("프로세스가 취소되었습니다.");
                SetProgressState(eGSSL_State.None);
            }
            catch (Exception ex)
            {
                GSSL_Log.LogError($"프로세스 중 에러가 발생했습니다: {ex.Message}");
                SetProgressState(eGSSL_State.None);
            }
            finally
            {
                // 리소스 정리
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        internal static async Awaitable OneButtonProcessSheet(List<RequestInfo> listRequestInfo, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            await GSSL_Download.DownloadSheet(listRequestInfo, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            var listSheetData = GSSL_DownloadedSheet.GetAllSheetData()
                .Where(x => listRequestInfo.Any(downloadInfo => downloadInfo.SheetName == x.title));

            var dicSheetData = new Dictionary<eTableStyle, List<SheetData>>();

            dicSheetData.TryAdd(eTableStyle.EnumType, new());
            dicSheetData.TryAdd(eTableStyle.Common, new());
            dicSheetData.TryAdd(eTableStyle.Localization, new());

            foreach (var sheetData in listSheetData)
            {
                dicSheetData[sheetData.tableStyle].Add(sheetData);
            }

            cancellationToken.ThrowIfCancellationRequested();

            SetProgressState(eGSSL_State.GenerateTableScript);
            foreach ((eTableStyle tableStyle, var list) in dicSheetData)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                switch (tableStyle)
                {
                    case eTableStyle.Common:
                        GSSL_Generate.GenerateTableScripts(list);
                        break;
                    case eTableStyle.EnumType:
                        GSSL_Generate.GenerateEnumDef(list);
                        break;
                    case eTableStyle.Localization:
                        GSSL_Generate.GenerateLocalize(list);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            dicSheetData.Remove(eTableStyle.EnumType);
            dicSheetData.Remove(eTableStyle.Localization);
            var str = JsonConvert.SerializeObject(dicSheetData);
            GenerateDataString = str;
            TableLinkerFlag = true;

            cancellationToken.ThrowIfCancellationRequested();

            GSSL_Generate.GenerateTableLinkerScript();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            CheckPrefsAndGenerateTableData();
        }

        private static async Awaitable GenerateTableLinkerAsync()
        {
            await Task.Delay(100);

            GSSL_Log.Log("Generate Table Start");
            GSSL_Generate.GenerateTableLinkerData();
            GSSL_Log.Log("Generate Table Done");
        }

        [InitializeOnLoadMethod]
        private static void CheckPrefsAndGenerateTableData()
        {
            // GSSL_Log.Log($"Generate Data Check ({GenerateDataFlag})");

            if (GenerateDataFlag)
            {
                SetProgressState(eGSSL_State.GenerateTableData);

                var str = GenerateDataString;

                GSSL_Log.Log("Generate Data Start");
                GenerateData(str);
                GSSL_Log.Log("Generate Data Done");

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            // GSSL_Log.Log($"Generate TableLinker Check ({TableLinkerFlag})");
            if (TableLinkerFlag)
            {
                SetProgressState(eGSSL_State.GenerateTableLinker);

                GenerateTableLinkerAsync().GetAwaiter().GetResult();

                TableLinkerFlag = false;
            }

            GenerateDataString = string.Empty;

            Task.Run(async () =>
            {
                SetProgressState(eGSSL_State.Done);
                await Task.Delay(500);
                SetProgressState(eGSSL_State.None);
            });
        }

        private static void GenerateData(string str)
        {
            var dic = JsonConvert.DeserializeObject<Dictionary<eTableStyle, List<SheetData>>>(str);

            foreach ((eTableStyle tableStyle, var list) in dic)
            {
                switch (tableStyle)
                {
                    case eTableStyle.Common:
                        GSSL_Generate.GenerateTableData(list);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
        
        /// <summary>
        /// 선택된 개별 시트들을 다운로드하고 최신화합니다.
        /// </summary>
        public static async Awaitable IndividualUpdateSelectedSheets(List<string> selectedSheetNames)
        {
            // 이전 작업이 진행중이면 취소
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
            }
            
            // 새로운 취소 토큰 생성
            _cancellationTokenSource = new CancellationTokenSource();
            
            try
            {
                // 선택된 시트 중 string이 포함된 시트가 있는지 확인
                bool hasStringSheet = selectedSheetNames.Any(name => 
                    name != null && name.ToLower().Contains("string"));
                
                // string 시트가 선택된 경우, 다운로드 후 재생성 시 캐시의 모든 string 시트를 함께 처리
                // 다운로드는 선택된 시트만 수행
                List<string> sheetsToDownload = selectedSheetNames;
                
                GSSL_Log.Log($"개별 시트 최신화 시작 - {selectedSheetNames.Count}개 시트");
                
                // 개별 시트들 다운로드
                await GSSL_Download.DownloadIndividualSheets(sheetsToDownload, _cancellationTokenSource.Token);
                
                // 다운로드된 시트들로 스크립트 및 데이터 생성
                // ProcessIndividualSheets 내부에서 string 시트가 있으면 모든 string 시트를 함께 처리
                await ProcessIndividualSheets(selectedSheetNames, _cancellationTokenSource.Token);
                
                GSSL_Log.Log("개별 시트 최신화 완료");
            }
            catch (OperationCanceledException)
            {
                GSSL_Log.Log("개별 시트 최신화가 취소되었습니다.");
                SetProgressState(eGSSL_State.None);
                throw;
            }
            catch (Exception ex)
            {
                GSSL_Log.LogError($"개별 시트 최신화 중 에러가 발생했습니다: {ex.Message}\n{ex.StackTrace}");
                SetProgressState(eGSSL_State.None);
                throw;
            }
            finally
            {
                // 리소스 정리
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }
        
        /// <summary>
        /// 선택된 시트들을 캐시에서 로드하여 스크립터블 오브젝트를 재생성합니다.
        /// </summary>
        public static async Awaitable RegenerateSelectedSheets(List<string> selectedSheetNames)
        {
            // 이전 작업이 진행중이면 취소
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
            }
            
            // 새로운 취소 토큰 생성
            _cancellationTokenSource = new CancellationTokenSource();
            
            try
            {
                // 선택된 시트 중 string이 포함된 시트가 있는지 확인
                bool hasStringSheet = selectedSheetNames.Any(name => 
                    name != null && name.ToLower().Contains("string"));
                
                // string 시트가 선택된 경우, 캐시에 있는 모든 string 시트를 함께 처리
                List<string> sheetsToRegenerate = new List<string>(selectedSheetNames);
                if (hasStringSheet)
                {
                    var cachedSheets = GSSL_CacheManager.GetAllCachedSheets();
                    var allStringSheets = cachedSheets
                        .Where(sheet => sheet.sheetName != null && 
                                        sheet.sheetName.ToLower().Contains("string"))
                        .Select(sheet => sheet.sheetName)
                        .Distinct()
                        .ToList();
                    
                    // 선택된 시트와 모든 string 시트를 합침 (중복 제거)
                    sheetsToRegenerate = selectedSheetNames.Union(allStringSheets).Distinct().ToList();
                    
                    GSSL_Log.Log($"string 시트가 선택되어 캐시의 모든 string 시트({allStringSheets.Count}개)를 함께 재생성합니다.");
                }
                
                GSSL_Log.Log($"선택된 시트 재생성 시작 - {sheetsToRegenerate.Count}개 시트");
                
                // 캐시에서 시트 데이터 로드
                await GSSL_Download.RegenerateFromCache(sheetsToRegenerate, _cancellationTokenSource.Token);
                
                // 로드된 시트들로 스크립트 및 데이터 생성
                await ProcessIndividualSheets(sheetsToRegenerate, _cancellationTokenSource.Token);
                
                GSSL_Log.Log("선택된 시트 재생성 완료");
            }
            catch (OperationCanceledException)
            {
                GSSL_Log.Log("선택된 시트 재생성이 취소되었습니다.");
                SetProgressState(eGSSL_State.None);
            }
            catch (Exception ex)
            {
                GSSL_Log.LogError($"선택된 시트 재생성 중 에러가 발생했습니다: {ex.Message}");
                SetProgressState(eGSSL_State.None);
            }
            finally
            {
                // 리소스 정리
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }
        
        private static async Awaitable ProcessIndividualSheets(List<string> selectedSheetNames, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var allSheetData = GSSL_DownloadedSheet.GetAllSheetData();
            var selectedSheetData = allSheetData
                .Where(x => selectedSheetNames.Contains(x.title))
                .ToList();
            
            // 선택된 시트 중 Localization 타입이 있는지 확인
            bool hasLocalizationSheet = selectedSheetData.Any(x => x.tableStyle == eTableStyle.Localization);
            
            // Localization 시트가 선택된 경우, 캐시의 모든 Localization 시트를 함께 처리
            if (hasLocalizationSheet)
            {
                var cachedSheets = GSSL_CacheManager.GetAllCachedSheets();
                var allLocalizationSheets = cachedSheets
                    .Where(sheet => sheet.tableStyle == SheetData.eTableStyle.Localization)
                    .Select(sheet => sheet.sheetName)
                    .Distinct()
                    .ToList();
                
                GSSL_Log.Log($"Localization 시트가 선택되어 캐시의 모든 Localization 시트({allLocalizationSheets.Count}개)를 함께 처리합니다.");
                
                // 다운로드되지 않은 Localization 시트들을 캐시에서 로드
                var downloadedSheetTitles = allSheetData.Select(x => x.title).ToList();
                var missingLocalizationSheets = allLocalizationSheets
                    .Where(name => !downloadedSheetTitles.Contains(name))
                    .ToList();
                
                if (missingLocalizationSheets.Count > 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    // 캐시에서 누락된 Localization 시트들 로드
                    foreach (var sheetName in missingLocalizationSheets)
                    {
                        var cacheInfo = cachedSheets.FirstOrDefault(c => c.sheetName == sheetName);
                        if (cacheInfo == null) continue;
                        
                        var cachedData = GSSL_CacheManager.LoadSheetFromCache(sheetName);
                        if (string.IsNullOrEmpty(cachedData)) continue;
                        
                        var sheetData = new SheetData
                        {
                            spreadSheetId = cacheInfo.spreadSheetId,
                            title = cacheInfo.sheetName,
                            tableStyle = cacheInfo.tableStyle,
                            data = cachedData
                        };
                        
                        GSSL_DownloadedSheet.AddSheetData(sheetData);
                    }
                    
                    GSSL_Log.Log($"캐시에서 {missingLocalizationSheets.Count}개의 Localization 시트를 추가로 로드했습니다.");
                    
                    // 모든 시트 데이터 다시 가져오기
                    allSheetData = GSSL_DownloadedSheet.GetAllSheetData();
                }
                
                // Localization 시트 목록 업데이트 (다운로드된 것 + 캐시에서 로드한 것)
                var allLocalizationSheetData = allSheetData
                    .Where(x => x.tableStyle == eTableStyle.Localization)
                    .ToList();
                
                selectedSheetData = selectedSheetData
                    .Where(x => x.tableStyle != eTableStyle.Localization)
                    .Concat(allLocalizationSheetData)
                    .ToList();
            }
            
            var listSheetData = selectedSheetData;

            var dicSheetData = new Dictionary<eTableStyle, List<SheetData>>();

            dicSheetData.TryAdd(eTableStyle.EnumType, new());
            dicSheetData.TryAdd(eTableStyle.Common, new());
            dicSheetData.TryAdd(eTableStyle.Localization, new());

            foreach (var sheetData in listSheetData)
            {
                dicSheetData[sheetData.tableStyle].Add(sheetData);
            }

            cancellationToken.ThrowIfCancellationRequested();

            SetProgressState(eGSSL_State.GenerateTableScript);
            
            // Enum 타입을 먼저 처리
            if (dicSheetData[eTableStyle.EnumType].Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                GSSL_Generate.GenerateEnumDef(dicSheetData[eTableStyle.EnumType]);
            }
            
            // Common 타입 처리
            if (dicSheetData[eTableStyle.Common].Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                GSSL_Generate.GenerateTableScripts(dicSheetData[eTableStyle.Common]);
            }
            
            // Localization 타입 처리 (캐시의 모든 Localization 시트와 함께)
            if (dicSheetData[eTableStyle.Localization].Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                GSSL_Generate.GenerateLocalize(dicSheetData[eTableStyle.Localization]);
            }
            
            // 공통 테이블만 데이터 생성 (Enum과 Localization은 제외)
            var commonTables = dicSheetData[eTableStyle.Common];
            if (commonTables.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                SetProgressState(eGSSL_State.GenerateTableData);
                GSSL_Generate.GenerateTableData(commonTables);
            }
            
            // TableLinker 생성 시 캐시의 모든 Common 타입 시트들도 함께 포함
            cancellationToken.ThrowIfCancellationRequested();
            
            var allCachedSheetsForLinker = GSSL_CacheManager.GetAllCachedSheets();
            var allCommonSheetsForLinker = allCachedSheetsForLinker
                .Where(sheet => sheet.tableStyle == SheetData.eTableStyle.Common)
                .Select(sheet => sheet.sheetName)
                .Distinct()
                .ToList();
            
            // 다운로드된 시트 목록 가져오기
            allSheetData = GSSL_DownloadedSheet.GetAllSheetData();
            var downloadedCommonSheetTitlesForLinker = allSheetData.Select(x => x.title).ToList();
            
            // 캐시에는 있지만 다운로드되지 않은 Common 시트들을 캐시에서 로드
            var missingCommonSheetsForLinker = allCommonSheetsForLinker
                .Where(name => !downloadedCommonSheetTitlesForLinker.Contains(name))
                .ToList();
            
            if (missingCommonSheetsForLinker.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                // 캐시에서 누락된 Common 시트들 로드
                foreach (var sheetName in missingCommonSheetsForLinker)
                {
                    var cacheInfo = allCachedSheetsForLinker.FirstOrDefault(c => c.sheetName == sheetName);
                    if (cacheInfo == null) continue;
                    
                    var cachedData = GSSL_CacheManager.LoadSheetFromCache(sheetName);
                    if (string.IsNullOrEmpty(cachedData)) continue;
                    
                    var sheetData = new SheetData
                    {
                        spreadSheetId = cacheInfo.spreadSheetId,
                        title = cacheInfo.sheetName,
                        tableStyle = cacheInfo.tableStyle,
                        data = cachedData
                    };
                    
                    GSSL_DownloadedSheet.AddSheetData(sheetData);
                }
                
                GSSL_Log.Log($"TableLinker 생성을 위해 캐시에서 {missingCommonSheetsForLinker.Count}개의 Common 시트를 추가로 로드했습니다.");
            }
            
            // 개별 시트 업데이트 후 테이블링커 재생성
            cancellationToken.ThrowIfCancellationRequested();
            GSSL_Generate.GenerateTableLinkerScript();
            
            cancellationToken.ThrowIfCancellationRequested();
            GSSL_Generate.ReconnectTableLinker();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            SetProgressState(eGSSL_State.Done);
            await Task.Delay(1000, cancellationToken);
            SetProgressState(eGSSL_State.None);
        }
        
        /// <summary>
        /// 테이블링커 연결만 다시 처리합니다.
        /// </summary>
        public static async Awaitable ReconnectTableLinker()
        {
            // 이전 작업이 진행중이면 취소
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
            }
            
            // 새로운 취소 토큰 생성
            _cancellationTokenSource = new CancellationTokenSource();
            
            try
            {
                GSSL_Log.Log("테이블링커 연결 시작");
                
                var cancellationToken = _cancellationTokenSource.Token;
                cancellationToken.ThrowIfCancellationRequested();
                
                SetProgressState(eGSSL_State.GenerateTableData);
                
                // 테이블링커 연결 재처리
                GSSL_Generate.ReconnectTableLinker();
                
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                GSSL_Log.Log("테이블링커 연결 완료");
                
                SetProgressState(eGSSL_State.Done);
                await Task.Delay(1000, cancellationToken);
                SetProgressState(eGSSL_State.None);
            }
            catch (OperationCanceledException)
            {
                GSSL_Log.Log("테이블링커 연결이 취소되었습니다.");
                SetProgressState(eGSSL_State.None);
            }
            catch (Exception ex)
            {
                GSSL_Log.LogError($"테이블링커 연결 중 에러가 발생했습니다: {ex.Message}");
                SetProgressState(eGSSL_State.None);
            }
            finally
            {
                // 리소스 정리
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }
    }
}