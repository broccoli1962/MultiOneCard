namespace Backend.Net
{
    /// <summary>
    /// 기획서 §6 Reject 문자열 코드. JSON <c>reject</c> 값.
    /// Game.Rules.RejectCode 와 같은 리터럴을 쓴다. 규칙 판정은 하지 않는다.
    /// </summary>
    public static class RejectCode
    {
        public const string NotYourTurn = "NotYourTurn";
        public const string IllegalCard = "IllegalCard";
        public const string NotInHand = "NotInHand";
        public const string NotAttackResponse = "NotAttackResponse";
        public const string NotQueenResponse = "NotQueenResponse";
        public const string NeedSuitPick = "NeedSuitPick";
        public const string NeedQueenMode = "NeedQueenMode";
        public const string NeedGiveCards = "NeedGiveCards";
        public const string GiveCountMismatch = "GiveCountMismatch";
        public const string NeedKingMode = "NeedKingMode";
        public const string NeedHideUnder = "NeedHideUnder";
        public const string NoCardToHide = "NoCardToHide";
        public const string SpearNotDefendable = "SpearNotDefendable";
        public const string CounterAlreadyUsed = "CounterAlreadyUsed";
        public const string NeedMirrorDiscard = "NeedMirrorDiscard";
        public const string ColorLocked = "ColorLocked";
        public const string ChatRate = "ChatRate";
        public const string ChatEmpty = "ChatEmpty";
        public const string VersionMismatch = "VersionMismatch";
        public const string SeatTaken = "SeatTaken";
        public const string RoomFull = "RoomFull";
        public const string MatchAlreadyStarted = "MatchAlreadyStarted";
        public const string GraceExpired = "GraceExpired";
    }
}
