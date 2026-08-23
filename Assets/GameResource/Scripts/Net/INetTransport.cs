using System;

namespace Backend.Net
{
    /// <summary>
        /// 커맨드를 보내고 이벤트를 받는 전송. LocalLoopback 또는 <see cref="WsNetClient"/>.
    /// </summary>
    public interface INetTransport
    {
        /// <summary>호스트에 붙어 있는지.</summary>
        bool IsConnected { get; }

        /// <summary>호스트에 붙는다. 끊긴 좌석이면 재접속한다.</summary>
        void Connect();

        /// <summary>호스트에서 떨어진다. 매치 유예가 시작된다.</summary>
        void Disconnect();

        /// <summary>커맨드를 호스트로 보낸다. 접속 중이 아니면 무시한다.</summary>
        void Send(CommandMessage command);

        /// <summary>이 좌석에 보이는 이벤트가 도착하면 발행한다.</summary>
        event Action<EventMessage> EventReceived;
    }
}
