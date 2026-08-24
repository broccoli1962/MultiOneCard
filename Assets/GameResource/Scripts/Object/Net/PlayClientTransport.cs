using System;
using System.Collections.Concurrent;
using System.Text;
using Backend.Net;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Backend.Object.Net
{
    /// <summary>
    /// NGO 커스텀 메시지로 JSON 커맨드/이벤트를 주고받는 게스트 전송.
    /// </summary>
    public sealed class PlayClientTransport : INetTransport
    {
        public const string CommandChannel = "ot.c";
        public const string EventChannel = "ot.e";

        private readonly ConcurrentQueue<EventMessage> _inbound = new ConcurrentQueue<EventMessage>();

        /// <summary>서버에 붙어 있는지.</summary>
        public bool IsConnected
        {
            get
            {
                var nm = ResolveNetwork();
                return nm != null && nm.IsClient && nm.IsConnectedClient;
            }
        }

        /// <summary>호스트가 보낸 이벤트가 도착하면 발행한다.</summary>
        public event Action<EventMessage> EventReceived;

        /// <summary>이미 StartClient 된 소켓을 쓴다.</summary>
        public void Connect()
        {
        }

        /// <summary>클라를 끊는다.</summary>
        public void Disconnect()
        {
            var nm = ResolveNetwork();
            if (nm != null && nm.IsListening)
            {
                nm.Shutdown();
            }
        }

        /// <summary>커맨드 JSON 을 호스트로 보낸다.</summary>
        public void Send(CommandMessage command)
        {
            var nm = ResolveNetwork();
            if (command == null || nm == null || !nm.IsConnectedClient)
            {
                return;
            }

            SendNamed(nm, CommandChannel, NetworkManager.ServerClientId, WireJson.SerializeCommand(command));
        }

        /// <summary>수신 큐를 메인 스레드에서 비운다.</summary>
        public void PumpReceived()
        {
            while (_inbound.TryDequeue(out var ev))
            {
                EventReceived?.Invoke(ev);
            }
        }

        /// <summary>호스트가 보낸 이벤트 JSON 을 큐에 넣는다.</summary>
        public void EnqueueEventJson(string json)
        {
            var ev = WireJson.DeserializeEvent(json);
            if (ev != null)
            {
                _inbound.Enqueue(ev);
            }
        }

        /// <summary>UTF8 JSON 을 네임드 메시지로 보낸다.</summary>
        public static void SendNamed(NetworkManager nm, string channel, ulong target, string json)
        {
            if (nm == null || nm.CustomMessagingManager == null || string.IsNullOrEmpty(json))
            {
                return;
            }

            var bytes = Encoding.UTF8.GetBytes(json);
            var writer = new FastBufferWriter(bytes.Length + 8, Allocator.Temp);
            try
            {
                writer.WriteValueSafe(bytes.Length);
                writer.WriteBytesSafe(bytes);
                nm.CustomMessagingManager.SendNamedMessage(channel, target, writer);
            }
            finally
            {
                writer.Dispose();
            }
        }

        /// <summary>네임드 메시지에서 JSON 문자열을 읽는다.</summary>
        public static string ReadJson(FastBufferReader reader)
        {
            reader.ReadValueSafe(out int length);
            if (length <= 0 || length > 65536)
            {
                return null;
            }

            var bytes = new byte[length];
            reader.ReadBytesSafe(ref bytes, length);
            return Encoding.UTF8.GetString(bytes);
        }

        private static NetworkManager ResolveNetwork()
        {
            return PlaySession.Network != null ? PlaySession.Network : NetworkManager.Singleton;
        }
    }
}
