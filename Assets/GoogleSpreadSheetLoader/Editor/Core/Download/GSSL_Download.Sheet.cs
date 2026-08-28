using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GoogleSpreadSheetLoader.Auth;
using GoogleSpreadSheetLoader.Setting;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using static GoogleSpreadSheetLoader.GSSL_State;

namespace GoogleSpreadSheetLoader.Download
{
    internal static partial class GSSL_Download
    {
        private const int MaxRetryAttempts = 2; // 전체 재시도 횟수
        
        public static async Awaitable DownloadSheet(List<RequestInfo> listDownloadInfo, CancellationToken cancellationToken = default)
        {
            try
            {
                for (int attempt = 0; attempt <= MaxRetryAttempts; attempt++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    if (attempt > 0)
                    {
                        Debug.LogWarning($"전체 다운로드 재시도 중... ({attempt}/{MaxRetryAttempts})");
                        await Task.Delay(2000, cancellationToken); // 재시도 간격
                    }
                    
                    if (await TryDownloadSheet(listDownloadInfo, cancellationToken))
                    {
                        // 성공하면 루프 종료
                        break;
                    }
                    
                    if (attempt == MaxRetryAttempts)
                    {
                        // 마지막 시도까지 실패한 경우
                        var errorInfos = listDownloadInfo.Where(x => x.HasError && !x.IsDisposed).ToList();
                        var errorMessage = $"시트 다운로드 실패 (최대 재시도 횟수 초과):\n";
                        foreach (var errorInfo in errorInfos)
                        {
                            errorMessage += $"• {errorInfo.SheetName}: {errorInfo.ErrorMessage} (재시도: {errorInfo.RetryCount}회)\n";
                        }
                        
                        Debug.LogError(errorMessage);
                        throw new System.Exception($"시트 다운로드 실패: {errorInfos.Count}개 시트에서 에러 발생 (최대 재시도 횟수 초과)");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Debug.Log("시트 다운로드가 취소되었습니다.");
                throw; // 취소 예외는 다시 던져서 상위에서 처리하도록 함
            }
            finally
            {
                // 리소스 정리
                foreach (var info in listDownloadInfo)
                {
                    info.Dispose();
                }
            }
        }
        
        private static async Awaitable<bool> TryDownloadSheet(List<RequestInfo> listDownloadInfo, CancellationToken cancellationToken = default)
        {
            var accessToken = await GSSL_ServiceAccountAuth.GetAccessTokenAsync(cancellationToken);

            GSSL_DownloadedSheet.ClearAllSheetData();

            using var httpClient = new System.Net.Http.HttpClient();
            httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            for (var i = 0; i < listDownloadInfo.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var info = listDownloadInfo[i];
                if (info.IsDisposed)
                    continue;

                string progressString = $"({i + 1}/{listDownloadInfo.Count})";
                SetProgressState(eGSSL_State.DownloadingSheet, progressString);
                EditorWindow.focusedWindow?.Repaint();

                try
                {
                    var encodedSheetName = Uri.EscapeDataString(info.SheetName);
                    var url = string.Format(GSSL_URL.DownloadSheetUrl, info.SpreadSheetId, encodedSheetName);
                    using var response = await httpClient.GetAsync(url, cancellationToken);
                    var downloadText = await response.Content.ReadAsStringAsync();
                    if (!response.IsSuccessStatusCode)
                    {
                        Debug.LogError($"시트 다운로드 실패 - {info.SheetName}: {(int)response.StatusCode} {response.ReasonPhrase}\n{downloadText}");
                        return false;
                    }

                    var sheetData = new SheetData
                    {
                        spreadSheetId = info.SpreadSheetId,
                        title = info.SheetName
                    };

                    if (sheetData.title.Contains(GSSL_Setting.SettingData.sheet_enumTypeStr))
                        sheetData.tableStyle = SheetData.eTableStyle.EnumType;
                    else if (sheetData.title.Contains(GSSL_Setting.SettingData.sheet_localizationTypeStr))
                        sheetData.tableStyle = SheetData.eTableStyle.Localization;
                    else
                        sheetData.tableStyle = SheetData.eTableStyle.Common;

                    JObject jObj = JObject.Parse(downloadText);

                    if (!jObj.TryGetValue("values", out var values))
                    {
                        Debug.LogError($"변환 실패 - {info.SheetName}\n{url}\n{downloadText}");
                        return false;
                    }

                    sheetData.data = values.ToString();
                    GSSL_DownloadedSheet.AddSheetData(sheetData);

                    var spreadSheetInfo = GSSL_Setting.SettingData.listSpreadSheetInfo
                        ?.FirstOrDefault(x => x.spreadSheetId == info.SpreadSheetId);
                    var spreadSheetName = spreadSheetInfo?.spreadSheetName ?? "Unknown";

                    GSSL_CacheManager.SaveSheetToCache(
                        info.SpreadSheetId,
                        spreadSheetName,
                        info.SheetName,
                        sheetData.data,
                        sheetData.tableStyle);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"시트 데이터 처리 중 에러 발생 - {info.SheetName}: {ex.Message}");
                    return false;
                }
            }

            {
                SetProgressState(eGSSL_State.DownloadingSheet, "(Done)");
                EditorWindow.focusedWindow?.Repaint();
            }

            return true;
        }
        
        public static async Awaitable DownloadIndividualSheets(List<string> sheetNames, CancellationToken cancellationToken = default)
        {
            var cachedSheets = GSSL_CacheManager.GetAllCachedSheets();
            var requestInfoList = new List<RequestInfo>();
            
            // 선택된 시트들의 RequestInfo 생성
            foreach (var sheetName in sheetNames)
            {
                var cacheInfo = cachedSheets.FirstOrDefault(c => c.sheetName == sheetName);
                if (cacheInfo != null)
                {
                    requestInfoList.Add(new RequestInfo(cacheInfo.spreadSheetId, cacheInfo.sheetName));
                }
            }
            
            if (requestInfoList.Count == 0)
            {
                Debug.LogWarning("다운로드할 시트가 없습니다.");
                return;
            }
            
            await DownloadSheet(requestInfoList, cancellationToken);
        }
        
        public static async Awaitable RegenerateFromCache(List<string> sheetNames, CancellationToken cancellationToken = default)
        {
            try
            {
                SetProgressState(eGSSL_State.Prepare);
                EditorWindow.focusedWindow?.Repaint();
                
                GSSL_DownloadedSheet.ClearAllSheetData();
                
                var cachedSheets = GSSL_CacheManager.GetAllCachedSheets();
                var processedCount = 0;
                
                foreach (var sheetName in sheetNames)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    var cacheInfo = cachedSheets.FirstOrDefault(c => c.sheetName == sheetName);
                    if (cacheInfo == null)
                    {
                        Debug.LogWarning($"캐시에서 시트를 찾을 수 없습니다: {sheetName}");
                        continue;
                    }
                    
                    var cachedData = GSSL_CacheManager.LoadSheetFromCache(sheetName);
                    if (string.IsNullOrEmpty(cachedData))
                    {
                        Debug.LogWarning($"캐시 데이터를 로드할 수 없습니다: {sheetName}");
                        continue;
                    }
                    
                    var sheetData = new SheetData
                    {
                        spreadSheetId = cacheInfo.spreadSheetId,
                        title = cacheInfo.sheetName,
                        tableStyle = cacheInfo.tableStyle,
                        data = cachedData
                    };
                    
                    GSSL_DownloadedSheet.AddSheetData(sheetData);
                    processedCount++;
                    
                    string progressString = $"({processedCount}/{sheetNames.Count})";
                    SetProgressState(eGSSL_State.GenerateTableData, progressString);
                    EditorWindow.focusedWindow?.Repaint();
                    await Task.Delay(50, cancellationToken);
                }
                
                SetProgressState(eGSSL_State.Done);
                EditorWindow.focusedWindow?.Repaint();
                await Task.Delay(1000, cancellationToken);
                SetProgressState(eGSSL_State.None);
                EditorWindow.focusedWindow?.Repaint();
                
                Debug.Log($"캐시에서 {processedCount}개 시트를 성공적으로 로드했습니다.");
            }
            catch (OperationCanceledException)
            {
                Debug.Log("캐시 재생성이 취소되었습니다.");
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"캐시 재생성 중 에러 발생: {ex.Message}");
                throw;
            }
        }
    }
}
