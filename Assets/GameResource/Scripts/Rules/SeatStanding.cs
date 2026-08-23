namespace Game.Rules
{
    /// <summary>
    /// 한 좌석의 매치 순위. 손패 0 완료가 앞, 나머지는 장수·점수 오름차순, 기권은 최하위.
    /// </summary>
    public readonly struct SeatStanding
    {
        public SeatStanding(int seat, int rank, int cardCount, int score, bool isFinished, bool isSurrendered)
        {
            Seat = seat;
            Rank = rank;
            CardCount = cardCount;
            Score = score;
            IsFinished = isFinished;
            IsSurrendered = isSurrendered;
        }

        /// <summary>좌석 번호.</summary>
        public int Seat { get; }

        /// <summary>1부터 시작하는 순위.</summary>
        public int Rank { get; }

        /// <summary>남은 손패 장수.</summary>
        public int CardCount { get; }

        /// <summary>남은 손패 점수 합.</summary>
        public int Score { get; }

        /// <summary>손패 0으로 끝난 좌석인지.</summary>
        public bool IsFinished { get; }

        /// <summary>기권 좌석인지. 기권은 최하위.</summary>
        public bool IsSurrendered { get; }
    }
}
