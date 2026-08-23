namespace Backend.Net
{
    /// <summary>
    /// 기획서 §6 클라→서버 커맨드 이름. JSON <c>op</c> 값과 동일하다.
    /// </summary>
    public static class OpCode
    {
        public const string Ready = "Ready";
        public const string StartMatch = "StartMatch";
        public const string PlayCard = "PlayCard";
        public const string Draw = "Draw";
        public const string ChooseSuit = "ChooseSuit";
        public const string ChooseQueenMode = "ChooseQueenMode";
        public const string AcceptQueen = "AcceptQueen";
        public const string GiveCards = "GiveCards";
        public const string ChooseKingMode = "ChooseKingMode";
        public const string HideUnder = "HideUnder";
        public const string MirrorDiscard = "MirrorDiscard";
        public const string Surrender = "Surrender";
        public const string Chat = "Chat";
        public const string RematchVote = "RematchVote";
        public const string Heartbeat = "Heartbeat";
        public const string SnapshotRequest = "SnapshotRequest";
    }

    /// <summary>
    /// 기획서 §7 프로토콜 버전. major 불일치는 <see cref="RejectCode.VersionMismatch"/>.
    /// </summary>
    public static class ProtocolVersion
    {
        public const int Major = 1;
        public const int Minor = 0;
        public const string Region = "ap-northeast";
    }

    /// <summary>
    /// ChooseSuit / requiredSuit JSON 값. CardDefId 접두(S H D C R M)와 같다.
    /// </summary>
    public static class SuitCode
    {
        public const string Spade = "S";
        public const string Heart = "H";
        public const string Diamond = "D";
        public const string Club = "C";
        public const string Star = "R";
        public const string Moon = "M";
    }

    /// <summary>ChooseQueenMode JSON 값. Reverse | Give.</summary>
    public static class QueenModeName
    {
        public const string Reverse = "Reverse";
        public const string Give = "Give";
    }

    /// <summary>ChooseKingMode JSON 값. Extra | Hide.</summary>
    public static class KingModeName
    {
        public const string Extra = "Extra";
        public const string Hide = "Hide";
    }

    /// <summary>requiredColor / ColorLock JSON 값.</summary>
    public static class ColorCode
    {
        public const string Black = "Black";
        public const string Red = "Red";
        public const string Blue = "Blue";
    }

    /// <summary>기획서 §9 채팅 채널.</summary>
    public static class ChatChannel
    {
        public const string Room = "room";
        public const string Match = "match";
    }

    /// <summary>기획서 §9 채팅 type.</summary>
    public static class ChatType
    {
        public const string User = "user";
        public const string Quick = "quick";
        public const string System = "system";
        public const string Emote = "emote";
    }
}
