using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Backend.App;
using Backend.Net;
using Backend.Object.Management;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace Backend.Object.Net
{
    /// <summary>
    /// 릴레이·LAN 호스트 세션. 웹소켓 경로는 쓰지 않는다.
    /// </summary>
    public static class PlaySession
    {
        private static GameObject _root;
        private static PlayHost _host;
        private static PlayClientTransport _guest;
        private static NetworkManager _nm;

        /// <summary>방장 루프백 클라. 게스트면 null.</summary>
        public static NetClient HostClient => _host != null ? _host.HostClient : null;

        /// <summary>현재 PlaySession NetworkManager. Singleton 과 다를 수 있어 전송은 이쪽을 쓴다.</summary>
        public static NetworkManager Network => _nm;

        /// <summary>게스트 전송.</summary>
        public static PlayClientTransport GuestTransport => _guest;

        /// <summary>호스트 루프백. 매치 Tick 에서 Pump 할 때 쓴다.</summary>
        public static LocalLoopback Loopback => _host != null ? _host.Loopback : null;

        /// <summary>릴레이 또는 LAN 호스트를 연다. 릴레이면 표시용 코드는 Unity 조인 코드다.</summary>
        public static async UniTask<NetClient> StartHostAsync(
            ConnectionMode mode,
            string nick,
            string roomCode,
            int seatCount)
        {
            Stop();
            EnsureNetwork(nick);
            try
            {
                var effectiveCode = roomCode;
                if (mode == ConnectionMode.Relay)
                {
                    var hosted = await UgsLobbyRelay.CreateAsync(
                        seatCount,
                        _root.GetComponent<UnityTransport>());
                    effectiveCode = hosted.Code;
                    _host.MarkRelayHost();
                }
                else
                {
                    ApplyLanHost();
                }

                var client = _host.PrepareHost(seatCount, effectiveCode);
                _host.StartListening(isHost: true);
                return client;
            }
            catch
            {
                Stop();
                throw;
            }
        }

        /// <summary>릴레이 또는 LAN 게스트로 붙는다.</summary>
        public static async UniTask<PlayClientTransport> StartGuestAsync(
            ConnectionMode mode,
            string nick,
            string roomCode)
        {
            Stop();
            EnsureNetwork(nick);
            try
            {
                if (mode == ConnectionMode.Relay)
                {
                    await UgsLobbyRelay.JoinAsync(roomCode, _root.GetComponent<UnityTransport>());
                    _host.MarkRelayGuest();
                }
                else
                {
                    ApplyLanClient(GatewaySettings.LanHost);
                }

                _guest = new PlayClientTransport();
                _host.StartListening(isHost: false);
                _nm.CustomMessagingManager.RegisterNamedMessageHandler(
                    PlayClientTransport.EventChannel,
                    OnGuestEvent);
                return _guest;
            }
            catch
            {
                Stop();
                throw;
            }
        }

        /// <summary>수신·턴 펌프.</summary>
        public static void Pump()
        {
            _guest?.PumpReceived();
            _host?.Pump();
        }

        /// <summary>세션을 닫는다. 매치로 넘길 때는 호출하지 않는다.</summary>
        public static void Stop()
        {
            if (_guest != null && _nm != null && _nm.CustomMessagingManager != null)
            {
                _nm.CustomMessagingManager.UnregisterNamedMessageHandler(PlayClientTransport.EventChannel);
            }

            _host?.Shutdown();
            _guest = null;
            _host = null;
            var root = _root;
            _root = null;
            _nm = null;
            if (root != null)
            {
                // Destroy 지연이면 다음 EnsureNetwork 의 Singleton 이 옛 인스턴스를 가리킨다.
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        /// <summary>이 기기의 LAN IPv4. 없으면 빈 문자열.</summary>
        public static string LocalIpv4()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                for (var i = 0; i < host.AddressList.Length; i++)
                {
                    var ip = host.AddressList[i];
                    if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                    {
                        return ip.ToString();
                    }
                }
            }
            catch (Exception)
            {
            }

            return string.Empty;
        }

        private static void EnsureNetwork(string nick)
        {
            if (NetworkManager.Singleton != null)
            {
                var stale = NetworkManager.Singleton;
                if (stale.IsListening)
                {
                    stale.Shutdown();
                }

                if (stale.gameObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(stale.gameObject);
                }
            }

            _root = new GameObject("PlaySession");
            UnityEngine.Object.DontDestroyOnLoad(_root);
            var transport = _root.AddComponent<UnityTransport>();
            _nm = _root.AddComponent<NetworkManager>();
            _nm.SetSingleton();
            _nm.RunInBackground = true;
            if (_nm.NetworkConfig == null)
            {
                _nm.NetworkConfig = new NetworkConfig();
            }

            var config = _nm.NetworkConfig;
            config.NetworkTransport = transport;
            config.EnableSceneManagement = false;
            config.ConnectionApproval = true;
            config.ForceSamePrefabs = false;
            config.AutoSpawnPlayerPrefabClientSide = false;
            config.PlayerPrefab = null;
            if (config.Prefabs == null)
            {
                config.Prefabs = new NetworkPrefabs();
            }

            config.ConnectionData = Encoding.UTF8.GetBytes(nick ?? string.Empty);
            _host = _root.AddComponent<PlayHost>();
            _host.Bind(_nm, nick);
        }

        private static void ApplyLanHost()
        {
            var utp = _root.GetComponent<UnityTransport>();
            utp.SetConnectionData("0.0.0.0", SessionLimits.LanPort, "0.0.0.0");
        }

        private static void ApplyLanClient(string hostIp)
        {
            var ip = hostIp != null ? hostIp.Trim() : string.Empty;
            if (ip.StartsWith("ws://", StringComparison.OrdinalIgnoreCase)
                || ip.StartsWith("wss://", StringComparison.OrdinalIgnoreCase)
                || ip.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || ip.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("랜은 서버 주소에 호스트 IP만 넣으세요");
            }

            if (string.IsNullOrEmpty(ip))
            {
                throw new InvalidOperationException("서버 주소에 호스트 LAN IP를 넣으세요");
            }

            var utp = _root.GetComponent<UnityTransport>();
            utp.SetConnectionData(ip, SessionLimits.LanPort);
        }

        private static void OnGuestEvent(ulong sender, FastBufferReader reader)
        {
            _ = sender;
            var json = PlayClientTransport.ReadJson(reader);
            _guest?.EnqueueEventJson(json);
        }
    }
}
