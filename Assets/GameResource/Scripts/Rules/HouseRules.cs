using System;

namespace Game.Rules
{
    /// <summary>
    /// 매치 하우스룰. 퀵매치는 항상 Official.
    /// 커스텀 룸만 Official 과 다른 값을 쓸 수 있다.
    /// </summary>
    public sealed class HouseRules
    {
        public const int MinSeats = 2;
        public const int MaxSeats = 6;
        public const int HandSizeTwoToFour = 7;
        public const int HandSizeFiveToSix = 5;
        public const int OfficialTurnSeconds = 15;

        public static readonly HouseRules Official = new HouseRules(
            drawAndPlay: true,
            jokerDefendable: true,
            continueAfterFirstWin: false,
            turnSeconds: OfficialTurnSeconds);

        /// <summary>퀵매치는 항상 Official.</summary>
        public static HouseRules QuickMatch => Official;

        public HouseRules(
            bool drawAndPlay,
            bool jokerDefendable,
            bool continueAfterFirstWin,
            int turnSeconds)
        {
            if (turnSeconds <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(turnSeconds));
            }

            DrawAndPlay = drawAndPlay;
            JokerDefendable = jokerDefendable;
            ContinueAfterFirstWin = continueAfterFirstWin;
            TurnSeconds = turnSeconds;
        }

        /// <summary>true 면 드로우 장을 같은 턴에 낼 수 있다.</summary>
        public bool DrawAndPlay { get; }

        /// <summary>true 면 조커 공격을 3·4 로 막을 수 있다.</summary>
        public bool JokerDefendable { get; }

        /// <summary>true 면 첫 1위 이후에도 잔여 순위전을 이어 간다. Official 은 false.</summary>
        public bool ContinueAfterFirstWin { get; }

        /// <summary>턴 제한(초). Official 은 15.</summary>
        public int TurnSeconds { get; }

        /// <summary>필드가 Official 프리셋과 같은지 여부.</summary>
        public bool IsOfficial =>
            DrawAndPlay == Official.DrawAndPlay
            && JokerDefendable == Official.JokerDefendable
            && ContinueAfterFirstWin == Official.ContinueAfterFirstWin
            && TurnSeconds == Official.TurnSeconds;

        /// <summary>
        /// 인원별 손패 장수. 2~4인 7장, 5~6인 5장.
        /// </summary>
        public static int HandSizeFor(int seatCount)
        {
            if (seatCount < MinSeats || seatCount > MaxSeats)
            {
                throw new ArgumentOutOfRangeException(nameof(seatCount), seatCount, "Seat count must be 2..6.");
            }

            return seatCount <= 4 ? HandSizeTwoToFour : HandSizeFiveToSix;
        }
    }
}
