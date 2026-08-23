using System;

namespace Backend.Net
{
    /// <summary>
    /// 서버→클라 이벤트 봉투. 필드명은 JSON 와이어와 같다.
    /// 공개 이벤트는 isPrivate=false, 개인 이벤트는 대상 좌석만 받는다.
    /// 시드는 넣지 않는다.
    /// </summary>
    [Serializable]
    public sealed class EventMessage
    {
        public string ev;
        public int seq;
        public int ackSeq;
        public int seat;
        public bool isPrivate;
        public long deadlineMs;
        public string reject;
        public int instanceId;
        public int[] instanceIds;
        public string defId;
        public string[] defIds;
        public int count;
        public string queenMode;
        public string kingMode;
        public string suit;
        public string color;
        public int fromSeat;
        public int toSeat;
        public int jokerColor;
        public int jokerBw;
        public int jokerMoon;
        public string text;
        public string chatType;
        public string channel;
        public string quickId;
        public PublicMatchView match;
        public RoomView room;
        public MatchEndView result;

        /// <summary>공개 또는 개인 이벤트 봉투를 만든다. isPrivate 는 EvCode 로 정한다.</summary>
        public static EventMessage Create(string ev, int seq, int seat)
        {
            return new EventMessage
            {
                ev = ev,
                seq = seq,
                seat = seat,
                isPrivate = EvCode.IsPrivate(ev),
            };
        }

        /// <summary>개인 Reject 이벤트를 만든다.</summary>
        public static EventMessage Reject(int seq, int seat, int ackSeq, string reject)
        {
            var message = Create(EvCode.Reject, seq, seat);
            message.ackSeq = ackSeq;
            message.reject = reject;
            return message;
        }
    }

    /// <summary>
    /// 기획서 §5 공개 매치 뷰. 타인 손패 def·덱 순서·시드는 넣지 않는다.
    /// </summary>
    [Serializable]
    public sealed class PublicMatchView
    {
        public string discardTop;
        public string requiredSuit;
        public string requiredColor;
        public int jokerColor;
        public int jokerBw;
        public int jokerMoon;
        public int direction;
        public int currentSeat;
        public long deadlineMs;
        public int[] handCounts;
        public int attackStack;
        public bool spearInStack;
        public int queenStack;
        public int deckCount;
        public string[] recentDiscard;
    }

    /// <summary>RoomUpdated 페이로드.</summary>
    [Serializable]
    public sealed class RoomView
    {
        public string roomCode;
        public string phase;
        public string[] nicks;
        public bool[] ready;
        public int hostSeat;
        public int seatCount;
    }

    /// <summary>MatchEnded 페이로드. 순위·장수·점수.</summary>
    [Serializable]
    public sealed class MatchEndView
    {
        public int[] ranks;
        public int[] scores;
        public int[] handCounts;
    }
}
