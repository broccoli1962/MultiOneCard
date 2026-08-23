namespace Backend.Net
{
    /// <summary>
    /// 기획서 §6 서버→클라 이벤트 이름. JSON <c>ev</c> 값과 동일하다.
    /// </summary>
    public static class EvCode
    {
        public const string RoomUpdated = "RoomUpdated";
        public const string MatchStarted = "MatchStarted";
        public const string TurnChanged = "TurnChanged";
        public const string CardPlayed = "CardPlayed";
        public const string DrewCount = "DrewCount";
        public const string QueenModeChosen = "QueenModeChosen";
        public const string QueenGiven = "QueenGiven";
        public const string KingModeChosen = "KingModeChosen";
        public const string KingHidden = "KingHidden";
        public const string JokerValues = "JokerValues";
        public const string ColorLock = "ColorLock";
        public const string MirrorAdjusted = "MirrorAdjusted";
        public const string SuitChanged = "SuitChanged";
        public const string PlayerDisconnected = "PlayerDisconnected";
        public const string PlayerRejoined = "PlayerRejoined";
        public const string PlayerOut = "PlayerOut";
        public const string Chat = "Chat";
        public const string MatchEnded = "MatchEnded";

        public const string HandGranted = "HandGranted";
        public const string CardDrawn = "CardDrawn";
        public const string CardsReceived = "CardsReceived";
        public const string Reject = "Reject";

        /// <summary>
        /// 개인 이벤트 여부. HandGranted / CardDrawn / CardsReceived / Reject 만 true.
        /// </summary>
        public static bool IsPrivate(string ev)
        {
            return ev == HandGranted
                || ev == CardDrawn
                || ev == CardsReceived
                || ev == Reject;
        }
    }

    /// <summary>기획서 §3 매치 phase. JSON 문자열.</summary>
    public static class MatchPhase
    {
        public const string Waiting = "Waiting";
        public const string Starting = "Starting";
        public const string InMatch = "InMatch";
        public const string Result = "Result";
    }
}
