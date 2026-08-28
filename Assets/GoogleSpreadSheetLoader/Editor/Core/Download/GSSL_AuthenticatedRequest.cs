using GoogleSpreadSheetLoader.Auth;
using UnityEngine.Networking;

namespace GoogleSpreadSheetLoader.Download
{
    internal static class GSSL_AuthenticatedRequest
    {
        public static UnityWebRequest CreateGet(string url, string accessToken)
        {
            var request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Authorization", $"Bearer {accessToken}");
            return request;
        }
    }
}
