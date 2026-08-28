using System;
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
    /// 릴레이 호스트 세션.
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

        /// <summary>릴레이 호스트를 연다. 표시용 코드는 Unity 조인 코드다.</summary>
        public static async UniTask<NetClient> StartHostAsync(
            string nick,
            int seatCount,
            bool isPrivate = false)
        {
            await StopAsync();
            await EnsureNetworkAsync(nick);
            try
            {
                var hosted = await UgsLobbyRelay.CreateAsync(
                    seatCount,
                    _root.GetComponent<UnityTransport>(),
                    isPrivate,
                    nick);
                _host.MarkRelayHost();
                var client = _host.PrepareHost(seatCount, hosted.Code);
                _host.StartListening(isHost: true);
                return client;
            }
            catch
            {
                await StopAsync();
                throw;
            }
        }

        /// <summary>릴레이 게스트로 붙는다.</summary>
        public static async UniTask<PlayClientTransport> StartGuestAsync(
            string nick,
            string roomCode,
            string sessionId = null)
        {
            await StopAsync();
            await EnsureNetworkAsync(nick);
            try
            {
                var transport = _root.GetComponent<UnityTransport>();
                if (!string.IsNullOrEmpty(sessionId))
                {
                    await UgsLobbyRelay.JoinByIdAsync(sessionId, transport);
                }
                else
                {
                    await UgsLobbyRelay.JoinAsync(roomCode, transport);
                }

                _host.MarkRelayGuest();
                _guest = new PlayClientTransport();
                _host.StartListening(isHost: false);
                _nm.CustomMessagingManager.RegisterNamedMessageHandler(
                    PlayClientTransport.EventChannel,
                    OnGuestEvent);
                return _guest;
            }
            catch
            {
                await StopAsync();
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
            StopAsync().Forget();
        }

        /// <summary>
        /// NGO·릴레이를 안전하게 내린다.
        /// DestroyImmediate 는 WSS/Relay 종료 중 메인 스레드를 멈출 수 있어 쓰지 않는다.
        /// </summary>
        public static async UniTask StopAsync()
        {
            if (_guest != null && _nm != null && _nm.CustomMessagingManager != null)
            {
                _nm.CustomMessagingManager.UnregisterNamedMessageHandler(PlayClientTransport.EventChannel);
            }

            _host?.Shutdown();
            _guest = null;
            _host = null;
            var root = _root;
            var nm = _nm;
            _root = null;
            _nm = null;

            if (nm != null && nm && nm.IsListening)
            {
                nm.Shutdown();
            }

            if (root != null)
            {
                UnityEngine.Object.Destroy(root);
                await WaitSingletonClearedAsync();
            }
        }

        private static async UniTask EnsureNetworkAsync(string nick)
        {
            await ClearStaleSingletonAsync();

            _root = new GameObject("PlaySession");
            UnityEngine.Object.DontDestroyOnLoad(_root);
            var transport = _root.AddComponent<UnityTransport>();
            if (WebBuild.IsPlayer)
            {
                transport.UseWebSockets = true;
            }

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

        private static async UniTask ClearStaleSingletonAsync()
        {
            var stale = NetworkManager.Singleton;
            if (stale == null || !stale)
            {
                return;
            }

            if (stale.IsListening)
            {
                stale.Shutdown();
            }

            if (stale.gameObject != null)
            {
                UnityEngine.Object.Destroy(stale.gameObject);
            }

            await WaitSingletonClearedAsync();
        }

        private static async UniTask WaitSingletonClearedAsync()
        {
            await UniTask.Yield();
            for (var i = 0; i < 30 && NetworkManager.Singleton != null; i++)
            {
                await UniTask.Yield();
            }
        }

        private static void OnGuestEvent(ulong sender, FastBufferReader reader)
        {
            _ = sender;
            var json = PlayClientTransport.ReadJson(reader);
            _guest?.EnqueueEventJson(json);
        }
    }
}
