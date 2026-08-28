using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace GoogleSpreadSheetLoader.Download
{
    internal static partial class GSSL_Download
    {
        private static List<RequestInfo> GetRequestInfoList(Dictionary<string, List<string>> dicSpreadSheet)
        {
            var listDownloadInfo = new List<RequestInfo>();

            foreach (var (spreadSheetId, listTitle) in dicSpreadSheet)
            {
                listDownloadInfo.AddRange(listTitle.Select(title => new RequestInfo(spreadSheetId, title)));
            }

            return listDownloadInfo;
        }
    }

    public class RequestInfo
    {
        private const int MaxRetryCount = 3;
        private const int RetryDelayMs = 1000;

        private readonly string _spreadSheetId;
        private readonly string _sheetName;
        private string _accessToken;
        private string _url;
        private UnityWebRequest _webRequest;
        private int _retryCount;
        private bool _isDisposed;

        public RequestInfo(string spreadSheetId, string sheetName)
        {
            _spreadSheetId = spreadSheetId;
            _sheetName = sheetName;
        }

        public string SpreadSheetId => _spreadSheetId;
        public string SheetName => _sheetName;
        public string URL => _url;
        public bool IsDone => _webRequest?.isDone ?? false;
        public string DownloadText => _webRequest?.downloadHandler?.text ?? "";
        public bool HasError => _webRequest != null && _webRequest.isDone && _webRequest.result != UnityWebRequest.Result.Success;
        public string ErrorMessage => _webRequest?.error ?? "";
        public UnityWebRequest.Result Result => _webRequest?.result ?? UnityWebRequest.Result.DataProcessingError;
        public int RetryCount => _retryCount;
        public bool CanRetry => _retryCount < MaxRetryCount && !_isDisposed;
        public bool IsDisposed => _isDisposed;

        public void Prepare(string accessToken)
        {
            if (_isDisposed)
                return;

            _accessToken = accessToken;
            _webRequest?.Dispose();
            CreateWebRequest();
        }

        public void SendAndGetAsyncOperation()
        {
            if (_isDisposed || _webRequest == null)
                return;

            _webRequest.SendWebRequest();
        }

        public async System.Threading.Tasks.Task<bool> RetryAsync(System.Threading.CancellationToken cancellationToken = default)
        {
            if (!CanRetry)
                return false;

            _retryCount++;
            Debug.LogWarning($"시트 '{_sheetName}' 재시도 중... ({_retryCount}/{MaxRetryCount})");

            _webRequest?.Dispose();
            await System.Threading.Tasks.Task.Delay(RetryDelayMs, cancellationToken);
            CreateWebRequest();
            SendAndGetAsyncOperation();

            return true;
        }

        public void Cancel()
        {
            if (_webRequest != null && !_webRequest.isDone)
                _webRequest.Abort();
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            Cancel();
            _webRequest?.Dispose();
        }

        private void CreateWebRequest()
        {
            if (string.IsNullOrEmpty(_accessToken))
                throw new System.InvalidOperationException("Access token is not set.");

            var encodedSheetName = UnityWebRequest.EscapeURL(_sheetName);
            _url = string.Format(GSSL_URL.DownloadSheetUrl, _spreadSheetId, encodedSheetName);
            _webRequest = GSSL_AuthenticatedRequest.CreateGet(_url, _accessToken);
        }
    }
}
