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
using UnityEngine.Networking;
using static GoogleSpreadSheetLoader.GSSL_State;

// ReSharper disable PossibleNullReferenceException

namespace GoogleSpreadSheetLoader.Download
{
    internal static partial class GSSL_Download
    {
        internal static async Awaitable DownloadSpreadSheetOnly(CancellationToken cancellationToken = default)
        {
            var dicSheetName = new Dictionary<string, List<string>>();

            var listDownloadTarget = GSSL_Setting.SettingData.listSpreadSheetInfo;

            cancellationToken.ThrowIfCancellationRequested();

            SetProgressState(eGSSL_State.Prepare);
            EditorWindow.focusedWindow?.Repaint();

            var accessToken = await GSSL_ServiceAccountAuth.GetAccessTokenAsync(cancellationToken);

            var listInfoOperPair = new List<(SpreadSheetInfo info, UnityWebRequestAsyncOperation oper)>();

            listInfoOperPair.AddRange(from info in listDownloadTarget
                                      let url = string.Format(GSSL_URL.DownloadSpreadSheetUrl, info.spreadSheetId)
                                      let webRequest = GSSL_AuthenticatedRequest.CreateGet(url, accessToken)
                                      let asyncOperator = webRequest.SendWebRequest()
                                      select (info, asyncOperator));

            do
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                string progressString = $"({listInfoOperPair.Count(x => x.oper.isDone)}/{listInfoOperPair.Count})";
                SetProgressState(eGSSL_State.DownloadingSpreadSheet, progressString);
                EditorWindow.focusedWindow?.Repaint();
                await Task.Delay(100, cancellationToken);
            } while (listInfoOperPair.Any(x => !x.oper.isDone));

            SetProgressState(eGSSL_State.Done);
            EditorWindow.focusedWindow?.Repaint();
            await Task.Delay(1000, cancellationToken);
            SetProgressState(eGSSL_State.None);
            EditorWindow.focusedWindow?.Repaint();

            foreach ((SpreadSheetInfo info, UnityWebRequestAsyncOperation oper) pair in listInfoOperPair)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                JObject jObj = JObject.Parse(pair.oper.webRequest.downloadHandler.text);
                if (jObj.TryGetValue("sheets", out var sheetsJToken))
                {
                    dicSheetName.TryAdd(pair.info.spreadSheetId, new List<string>());
                    dicSheetName[pair.info.spreadSheetId].Clear();

                    IEnumerable<JToken> enumTitle = sheetsJToken.Select(x => x["properties"]["title"]);
                    foreach (JToken title in enumTitle)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        
                        var titleString = title.ToString();

                        var isContains = titleString.Contains(GSSL_Setting.SettingData.sheetTargetStr);

                        switch (isContains)
                        {
                            case true when GSSL_Setting.SettingData.sheetTarget == SettingData.eSheetTargetStandard.제외:
                            case false when GSSL_Setting.SettingData.sheetTarget == SettingData.eSheetTargetStandard.포함:
                                continue;
                        }

                        if (dicSheetName[pair.info.spreadSheetId].Contains(titleString))
                        {
                            Debug.LogError(
                                $"중복 시트 이름 : {pair.info.spreadSheetName}에서 {titleString}의 중복 이름이 존재!");

                            continue;
                        }

                        dicSheetName[pair.info.spreadSheetId].Add(titleString);
                    }
                }
            }

            await DownloadSheet(GetRequestInfoList(dicSheetName), cancellationToken);
        }

        public static async Awaitable<List<RequestInfo>> DownloadSpreadSheetAll(CancellationToken cancellationToken = default)
        {
            var listDownloadTarget = GSSL_Setting.SettingData.listSpreadSheetInfo;
            List<RequestInfo> listResult = new();
            const int MaxRetryAttempts = 2; // 최대 재시도 횟수

            cancellationToken.ThrowIfCancellationRequested();

            SetProgressState(eGSSL_State.Prepare);
            EditorWindow.focusedWindow?.Repaint();

            var allWebRequests = new List<UnityWebRequest>(); // 리소스 정리를 위한 리스트
            
            try
            {
                for (int attempt = 0; attempt <= MaxRetryAttempts; attempt++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    if (attempt > 0)
                    {
                        Debug.LogWarning($"스프레드시트 다운로드 재시도 중... ({attempt}/{MaxRetryAttempts})");
                        await Task.Delay(2000, cancellationToken); // 재시도 간격
                    }

                    var accessToken = await GSSL_ServiceAccountAuth.GetAccessTokenAsync(cancellationToken);
                    var listInfoOperPair = new List<(SpreadSheetInfo info, UnityWebRequestAsyncOperation oper)>();

                    listInfoOperPair.AddRange(from info in listDownloadTarget
                                              let url = string.Format(GSSL_URL.DownloadSpreadSheetUrl, info.spreadSheetId)
                                              let webRequest = GSSL_AuthenticatedRequest.CreateGet(url, accessToken)
                                              let asyncOperator = webRequest.SendWebRequest()
                                              select (info, asyncOperator));
                                              
                    // 웹 요청들을 리스트에 추가 (리소스 정리용)
                    allWebRequests.AddRange(listInfoOperPair.Select(x => x.oper.webRequest));

                do
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    string progressString = $"({listInfoOperPair.Count(x => x.oper.isDone)}/{listInfoOperPair.Count})";
                    SetProgressState(eGSSL_State.DownloadingSpreadSheet, progressString);
                    EditorWindow.focusedWindow?.Repaint();
                    await Task.Delay(100, cancellationToken);
                } while (listInfoOperPair.Any(x => !x.oper.isDone));
                
                {
                    string progressString = $"(Done)";
                    SetProgressState(eGSSL_State.DownloadingSpreadSheet, progressString);
                    EditorWindow.focusedWindow?.Repaint();
                    await Task.Delay(500, cancellationToken);
                }

                // 다운로드 완료 후 에러 체크
                var errorPairs = listInfoOperPair.Where(x => x.oper.webRequest.result != UnityWebRequest.Result.Success).ToList();
                if (errorPairs.Any())
                {
                    var errorMessage = $"스프레드시트 다운로드 중 에러가 발생했습니다 (시도 {attempt + 1}/{MaxRetryAttempts + 1}):\n";
                    foreach (var (info, oper) in errorPairs)
                    {
                        errorMessage += $"• {info.spreadSheetName} ({info.spreadSheetId}): {oper.webRequest.error}\n";
                    }
                    
                    Debug.LogError(errorMessage);
                    
                    if (attempt == MaxRetryAttempts)
                    {
                        // 마지막 시도까지 실패한 경우
                        throw new System.Exception($"스프레드시트 다운로드 실패: {errorPairs.Count}개 스프레드시트에서 에러 발생 (최대 재시도 횟수 초과)");
                    }
                    
                    // 재시도하기 위해 루프 계속
                    continue;
                }

                // 성공한 경우 결과 처리
                listResult.Clear();
                foreach ((SpreadSheetInfo info, UnityWebRequestAsyncOperation oper) pair in listInfoOperPair)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    try
                    {
                        JObject jObj = JObject.Parse(pair.oper.webRequest.downloadHandler.text);
                        if (jObj.TryGetValue("sheets", out var sheetsJToken))
                        {
                            IEnumerable<JToken> enumTitle = sheetsJToken.Select(x => x["properties"]["title"]);
                            foreach (JToken title in enumTitle)
                            {
                                cancellationToken.ThrowIfCancellationRequested();
                                
                                var titleString = title.ToString();

                                var isContains = titleString.Contains(GSSL_Setting.SettingData.sheetTargetStr);

                                switch (isContains)
                                {
                                    case true when GSSL_Setting.SettingData.sheetTarget == SettingData.eSheetTargetStandard.제외:
                                    case false when GSSL_Setting.SettingData.sheetTarget == SettingData.eSheetTargetStandard.포함:
                                        continue;
                                }

                                if (listResult.Any(x => x.SheetName == titleString))
                                {
                                    Debug.LogError(
                                        $"중복 시트 이름 : {pair.info.spreadSheetName}에서 {titleString}의 중복 이름이 존재!");

                                    continue;
                                }

                                listResult.Add(new RequestInfo(pair.info.spreadSheetId, titleString));
                            }
                        }
                        else
                        {
                            Debug.LogError($"스프레드시트 {pair.info.spreadSheetName}에서 'sheets' 필드를 찾을 수 없습니다.");
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"스프레드시트 {pair.info.spreadSheetName} 처리 중 에러 발생: {ex.Message}");
                        
                        if (attempt == MaxRetryAttempts)
                        {
                            throw;
                        }
                        
                        // 재시도하기 위해 루프 계속
                        goto retry;
                    }
                }
                
                    // 성공적으로 완료된 경우 루프 종료
                    break;
                    
                    retry:
                    continue;
                }
            }
            catch (OperationCanceledException)
            {
                Debug.Log("스프레드시트 다운로드가 취소되었습니다.");
                throw;
            }
            finally
            {
                // 모든 웹 요청 리소스 정리
                foreach (var webRequest in allWebRequests)
                {
                    try
                    {
                        if (!webRequest.isDone)
                        {
                            webRequest.Abort();
                        }
                        webRequest.Dispose();
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"웹 요청 정리 중 에러: {ex.Message}");
                    }
                }
            }

            return listResult;
        }

        /// <summary>
        /// 특정 스프레드시트 하나만 다운로드합니다.
        /// </summary>
        public static async Awaitable<List<RequestInfo>> DownloadSingleSpreadSheet(string spreadSheetId, CancellationToken cancellationToken = default)
        {
            var listDownloadTarget = GSSL_Setting.SettingData.listSpreadSheetInfo;
            var targetInfo = listDownloadTarget.FirstOrDefault(x => x.spreadSheetId == spreadSheetId);
            
            if (targetInfo == null)
            {
                Debug.LogError($"스프레드시트를 찾을 수 없습니다: {spreadSheetId}");
                return new List<RequestInfo>();
            }
            
            List<RequestInfo> listResult = new();
            const int MaxRetryAttempts = 2;

            cancellationToken.ThrowIfCancellationRequested();

            SetProgressState(eGSSL_State.Prepare);
            EditorWindow.focusedWindow?.Repaint();

            var allWebRequests = new List<UnityWebRequest>();
            
            try
            {
                for (int attempt = 0; attempt <= MaxRetryAttempts; attempt++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    if (attempt > 0)
                    {
                        Debug.LogWarning($"스프레드시트 다운로드 재시도 중... ({attempt}/{MaxRetryAttempts})");
                        await Task.Delay(2000, cancellationToken);
                    }

                    var accessToken = await GSSL_ServiceAccountAuth.GetAccessTokenAsync(cancellationToken);
                    var url = string.Format(GSSL_URL.DownloadSpreadSheetUrl, spreadSheetId);
                    var webRequest = GSSL_AuthenticatedRequest.CreateGet(url, accessToken);
                    var asyncOperator = webRequest.SendWebRequest();
                    allWebRequests.Add(webRequest);

                    do
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        
                        string progressString = $"({(asyncOperator.isDone ? 1 : 0)}/1)";
                        SetProgressState(eGSSL_State.DownloadingSpreadSheet, progressString);
                        EditorWindow.focusedWindow?.Repaint();
                        await Task.Delay(100, cancellationToken);
                    } while (!asyncOperator.isDone);
                    
                    {
                        string progressString = $"(Done)";
                        SetProgressState(eGSSL_State.DownloadingSpreadSheet, progressString);
                        EditorWindow.focusedWindow?.Repaint();
                        await Task.Delay(500, cancellationToken);
                    }

                    // 다운로드 완료 후 에러 체크
                    if (asyncOperator.webRequest.result != UnityWebRequest.Result.Success)
                    {
                        var errorMessage = $"스프레드시트 다운로드 중 에러가 발생했습니다 (시도 {attempt + 1}/{MaxRetryAttempts + 1}):\n";
                        errorMessage += $"• {targetInfo.spreadSheetName} ({spreadSheetId}): {asyncOperator.webRequest.error}\n";
                        
                        Debug.LogError(errorMessage);
                        
                        if (attempt == MaxRetryAttempts)
                        {
                            throw new System.Exception($"스프레드시트 다운로드 실패: {targetInfo.spreadSheetName} (최대 재시도 횟수 초과)");
                        }
                        
                        continue;
                    }

                    // 성공한 경우 결과 처리
                    listResult.Clear();
                    try
                    {
                        JObject jObj = JObject.Parse(asyncOperator.webRequest.downloadHandler.text);
                        if (jObj.TryGetValue("sheets", out var sheetsJToken))
                        {
                            IEnumerable<JToken> enumTitle = sheetsJToken.Select(x => x["properties"]["title"]);
                            foreach (JToken title in enumTitle)
                            {
                                cancellationToken.ThrowIfCancellationRequested();
                                
                                var titleString = title.ToString();

                                var isContains = titleString.Contains(GSSL_Setting.SettingData.sheetTargetStr);

                                switch (isContains)
                                {
                                    case true when GSSL_Setting.SettingData.sheetTarget == SettingData.eSheetTargetStandard.제외:
                                    case false when GSSL_Setting.SettingData.sheetTarget == SettingData.eSheetTargetStandard.포함:
                                        continue;
                                }

                                if (listResult.Any(x => x.SheetName == titleString))
                                {
                                    Debug.LogError(
                                        $"중복 시트 이름 : {targetInfo.spreadSheetName}에서 {titleString}의 중복 이름이 존재!");

                                    continue;
                                }

                                listResult.Add(new RequestInfo(spreadSheetId, titleString));
                            }
                        }
                        else
                        {
                            Debug.LogError($"스프레드시트 {targetInfo.spreadSheetName}에서 'sheets' 필드를 찾을 수 없습니다.");
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"스프레드시트 {targetInfo.spreadSheetName} 처리 중 에러 발생: {ex.Message}");
                        
                        if (attempt == MaxRetryAttempts)
                        {
                            throw;
                        }
                        
                        continue;
                    }
                    
                    // 성공적으로 완료된 경우 루프 종료
                    break;
                }
            }
            catch (OperationCanceledException)
            {
                Debug.Log("스프레드시트 다운로드가 취소되었습니다.");
                throw;
            }
            finally
            {
                // 모든 웹 요청 리소스 정리
                foreach (var webRequest in allWebRequests)
                {
                    try
                    {
                        if (!webRequest.isDone)
                        {
                            webRequest.Abort();
                        }
                        webRequest.Dispose();
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"웹 요청 정리 중 에러: {ex.Message}");
                    }
                }
            }

            return listResult;
        }
    }
}