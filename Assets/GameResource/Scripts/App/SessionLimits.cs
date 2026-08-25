using Game.Rules;

namespace Backend.App
{
    /// <summary>
    /// 한 판·한 호스트의 하드 상한. Unity Relay 무료(평균 50 CCU)를
    /// 클라에서 계정 전체로 막을 수는 없고, 방당 접속만 이 값으로 자른다.
    /// </summary>
    public static class SessionLimits
    {
        /// <summary>한 방 최대 인원. <see cref="HouseRules.MaxSeats"/> 와 같다.</summary>
        public const int MaxPlayers = HouseRules.MaxSeats;

        /// <summary>한 방 최소 인원.</summary>
        public const int MinPlayers = HouseRules.MinSeats;

        /// <summary>
        /// Relay CreateAllocation 의 maxConnections. 호스트를 제외한 게스트 수.
        /// </summary>
        public const int MaxRelayJoins = MaxPlayers - 1;

        /// <summary>이 기기가 동시에 호스트할 수 있는 방 수.</summary>
        public const int MaxHostedSessions = 1;

        /// <summary>LAN Unity Transport 포트. 릴레이를 쓰지 않는다.</summary>
        public const ushort LanPort = 7778;

        /// <summary>2..6 으로 자른다.</summary>
        public static int ClampPlayers(int seatCount)
        {
            if (seatCount < MinPlayers)
            {
                return MinPlayers;
            }

            return seatCount > MaxPlayers ? MaxPlayers : seatCount;
        }
    }
}
