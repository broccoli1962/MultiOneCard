using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using GoogleSpreadSheetLoader.Setting;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace GoogleSpreadSheetLoader.Auth
{
    public static class GSSL_ServiceAccountAuth
    {
        private const string TokenUrl = "https://oauth2.googleapis.com/token";
        private const string SheetsReadOnlyScope = "https://www.googleapis.com/auth/spreadsheets.readonly";

        private static string _cachedAccessToken;
        private static DateTime _tokenExpiresAtUtc = DateTime.MinValue;

        public static void ClearCache()
        {
            _cachedAccessToken = null;
            _tokenExpiresAtUtc = DateTime.MinValue;
        }

        public static async Awaitable<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
        {
            if (!string.IsNullOrEmpty(_cachedAccessToken) &&
                DateTime.UtcNow < _tokenExpiresAtUtc.AddMinutes(-5))
            {
                return _cachedAccessToken;
            }

            var jsonText = ReadJsonText(GSSL_Setting.SettingData.serviceAccountJsonPath);
            var json = JObject.Parse(jsonText);
            var clientEmail = json["client_email"]?.ToString();
            var privateKeyPem = json["private_key"]?.ToString();

            if (string.IsNullOrWhiteSpace(clientEmail) || string.IsNullOrWhiteSpace(privateKeyPem))
            {
                throw new Exception("서비스 계정 JSON에 client_email 또는 private_key가 없습니다.");
            }

            var jwt = CreateSignedJwt(clientEmail, privateKeyPem);
            var tokenResponse = await RequestAccessTokenAsync(jwt, cancellationToken);

            _cachedAccessToken = tokenResponse.accessToken;
            _tokenExpiresAtUtc = DateTime.UtcNow.AddSeconds(tokenResponse.expiresInSeconds);

            return _cachedAccessToken;
        }

        public static string GetServiceAccountEmail()
        {
            return TryGetClientEmailFromPath(GSSL_Setting.SettingData.serviceAccountJsonPath);
        }

        public static string TryGetClientEmailFromPath(string jsonPath)
        {
            var jsonText = TryReadJsonText(jsonPath);
            if (string.IsNullOrWhiteSpace(jsonText))
                return null;

            return JObject.Parse(jsonText)["client_email"]?.ToString();
        }

        public static bool IsJsonPathValid(string jsonPath)
        {
            return !string.IsNullOrWhiteSpace(jsonPath) && File.Exists(jsonPath);
        }

        private static string ReadJsonText(string jsonPath)
        {
            var jsonText = TryReadJsonText(jsonPath);
            if (string.IsNullOrWhiteSpace(jsonText))
            {
                throw new Exception(
                    "서비스 계정 JSON 경로가 지정되지 않았거나 파일을 찾을 수 없습니다. GSSL 설정에서 JSON 파일의 절대 경로를 지정하세요.");
            }

            return jsonText;
        }

        private static string TryReadJsonText(string jsonPath)
        {
            if (string.IsNullOrWhiteSpace(jsonPath) || !File.Exists(jsonPath))
                return null;

            return File.ReadAllText(jsonPath);
        }

        private static string CreateSignedJwt(string clientEmail, string privateKeyPem)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var headerJson = "{\"alg\":\"RS256\",\"typ\":\"JWT\"}";
            var payloadJson =
                $"{{\"iss\":\"{clientEmail}\",\"scope\":\"{SheetsReadOnlyScope}\",\"aud\":\"{TokenUrl}\",\"exp\":{now + 3600},\"iat\":{now}}}";

            var header = Base64UrlEncode(Encoding.UTF8.GetBytes(headerJson));
            var payload = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));
            var unsignedToken = $"{header}.{payload}";

            using var rsa = CreateRsaFromPrivateKeyPem(privateKeyPem);
            var signature = rsa.SignData(
                Encoding.UTF8.GetBytes(unsignedToken),
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            return $"{unsignedToken}.{Base64UrlEncode(signature)}";
        }

        private static RSA CreateRsaFromPrivateKeyPem(string privateKeyPem)
        {
            var normalizedPem = privateKeyPem.Replace("\\n", "\n");
            var keyBytes = DecodePem(normalizedPem);
            var pkcs1Bytes = IsPkcs8PrivateKeyInfo(keyBytes)
                ? ExtractPkcs1FromPkcs8(keyBytes)
                : keyBytes;

            var rsa = RSA.Create();
            rsa.ImportParameters(DecodePkcs1PrivateKey(pkcs1Bytes));
            return rsa;
        }

        private static bool IsPkcs8PrivateKeyInfo(byte[] keyBytes)
        {
            if (keyBytes == null || keyBytes.Length < 4 || keyBytes[0] != 0x30)
                return false;

            try
            {
                var offset = 0;
                ReadAsnTag(keyBytes, ref offset, 0x30);
                var outerLength = ReadAsnLength(keyBytes, ref offset);
                var outerEnd = offset + outerLength;
                if (outerEnd > keyBytes.Length || keyBytes[offset] != 0x02)
                    return false;

                SkipAsnElement(keyBytes, ref offset);
                return offset < outerEnd && keyBytes[offset] == 0x30;
            }
            catch
            {
                return false;
            }
        }

        private static byte[] ExtractPkcs1FromPkcs8(byte[] pkcs8)
        {
            var offset = 0;
            ReadAsnTag(pkcs8, ref offset, 0x30);
            var outerLength = ReadAsnLength(pkcs8, ref offset);
            var outerEnd = offset + outerLength;

            SkipAsnElement(pkcs8, ref offset);
            SkipAsnElement(pkcs8, ref offset);

            ReadAsnTag(pkcs8, ref offset, 0x04);
            var octetLength = ReadAsnLength(pkcs8, ref offset);
            if (offset + octetLength > pkcs8.Length)
                throw new Exception("Invalid PKCS#8 private key format.");

            var pkcs1 = new byte[octetLength];
            Array.Copy(pkcs8, offset, pkcs1, 0, octetLength);
            return pkcs1;
        }

        private static void SkipAsnElement(byte[] data, ref int offset)
        {
            offset++;
            var contentLength = ReadAsnLength(data, ref offset);
            offset += contentLength;
        }

        private static byte[] DecodePem(string pem)
        {
            var base64 = pem
                .Replace("-----BEGIN PRIVATE KEY-----", "")
                .Replace("-----END PRIVATE KEY-----", "")
                .Replace("-----BEGIN RSA PRIVATE KEY-----", "")
                .Replace("-----END RSA PRIVATE KEY-----", "")
                .Replace("\n", "")
                .Replace("\r", "")
                .Trim();

            return Convert.FromBase64String(base64);
        }

        private static RSAParameters DecodePkcs1PrivateKey(byte[] pkcs1)
        {
            using var memory = new MemoryStream(pkcs1);
            using var reader = new BinaryReader(memory);

            if (reader.ReadByte() != 0x30)
                throw new Exception("Invalid PKCS#1 RSA private key format.");

            ReadAsnContentLength(reader);

            if (reader.ReadByte() != 0x02)
                throw new Exception("Invalid PKCS#1 RSA private key version tag.");

            var versionLength = ReadAsnContentLength(reader);
            reader.ReadBytes(versionLength);

            return new RSAParameters
            {
                Modulus = ReadAsnIntegerBytes(reader),
                Exponent = ReadAsnIntegerBytes(reader),
                D = ReadAsnIntegerBytes(reader),
                P = ReadAsnIntegerBytes(reader),
                Q = ReadAsnIntegerBytes(reader),
                DP = ReadAsnIntegerBytes(reader),
                DQ = ReadAsnIntegerBytes(reader),
                InverseQ = ReadAsnIntegerBytes(reader),
            };
        }

        private static byte[] ReadAsnIntegerBytes(BinaryReader reader)
        {
            if (reader.ReadByte() != 0x02)
                throw new Exception("Invalid ASN.1 integer.");

            var length = ReadAsnContentLength(reader);
            var bytes = reader.ReadBytes(length);

            if (bytes.Length > 0 && bytes[0] == 0x00)
            {
                var trimmed = new byte[bytes.Length - 1];
                Array.Copy(bytes, 1, trimmed, 0, trimmed.Length);
                return trimmed;
            }

            return bytes;
        }

        private static int ReadAsnContentLength(BinaryReader reader)
        {
            var length = reader.ReadByte();
            if ((length & 0x80) == 0)
                return length;

            var byteCount = length & 0x7F;
            if (byteCount == 1)
                return reader.ReadByte();

            if (byteCount == 2)
            {
                var high = reader.ReadByte();
                var low = reader.ReadByte();
                return (high << 8) | low;
            }

            throw new Exception("Unsupported ASN.1 length.");
        }

        private static void ReadAsnTag(byte[] data, ref int offset, byte expectedTag)
        {
            if (data[offset++] != expectedTag)
                throw new Exception($"Expected ASN.1 tag 0x{expectedTag:X2}.");
        }

        private static int ReadAsnLength(byte[] data, ref int offset)
        {
            var firstByte = data[offset++];
            if ((firstByte & 0x80) == 0)
                return firstByte;

            var byteCount = firstByte & 0x7F;
            if (byteCount == 0 || byteCount > 4)
                throw new Exception("Invalid ASN.1 length.");

            var length = 0;
            for (var i = 0; i < byteCount; i++)
                length = (length << 8) | data[offset++];

            return length;
        }

        private static async Awaitable<(string accessToken, int expiresInSeconds)> RequestAccessTokenAsync(
            string jwt,
            CancellationToken cancellationToken)
        {
            var body = $"grant_type=urn%3Aietf%3Aparams%3Aoauth%3Agrant-type%3Ajwt-bearer&assertion={Uri.EscapeDataString(jwt)}";
            using var content = new System.Net.Http.StringContent(
                body,
                Encoding.UTF8,
                "application/x-www-form-urlencoded");
            using var httpClient = new System.Net.Http.HttpClient();
            using var response = await httpClient.PostAsync(TokenUrl, content);
            cancellationToken.ThrowIfCancellationRequested();
            var responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"서비스 계정 토큰 발급 실패: {(int)response.StatusCode} {response.ReasonPhrase}\n{responseText}");
            }

            var tokenResponse = JObject.Parse(responseText);
            var accessToken = tokenResponse["access_token"]?.ToString();
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                throw new Exception($"서비스 계정 토큰 응답에 access_token이 없습니다.\n{responseText}");
            }

            var expiresIn = tokenResponse["expires_in"]?.Value<int>() ?? 3600;
            return (accessToken, expiresIn);
        }

        private static string Base64UrlEncode(byte[] input)
        {
            return Convert.ToBase64String(input)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }
    }
}
