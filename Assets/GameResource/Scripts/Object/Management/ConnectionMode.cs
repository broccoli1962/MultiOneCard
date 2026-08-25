namespace Backend.Object.Management
{
    /// <summary>
    /// 대기실 접속 경로. 릴레이는 Unity Lobby + Relay, LAN은 같은 와이파이 NGO.
    /// </summary>
    public enum ConnectionMode
    {
        /// <summary>Unity Lobby + Relay. 방당 최대 <see cref="Backend.App.SessionLimits.MaxPlayers"/>.</summary>
        Relay = 1,

        /// <summary>같은 와이파이. 릴레이 없음. 서버 주소에 호스트 LAN IP.</summary>
        Lan = 2,
    }
}
