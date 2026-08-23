using System;

namespace Backend.Net
{
    /// <summary>
    /// 클라→서버 커맨드 봉투. 필드명은 JSON 와이어와 같다. op 에 해당하는 페이로드만 채운다.
    /// </summary>
    [Serializable]
    public sealed class CommandMessage
    {
        public string op;
        public int seq;
        public int seat;
        public int protocolMajor;
        public int protocolMinor;
        public int instanceId;
        public int[] instanceIds;
        public string suit;
        public string queenMode;
        public string kingMode;
        public string text;
        public string quickId;
        public string channel;
        public bool rematchYes;

        /// <summary>버전을 담은 Ready 커맨드를 만든다.</summary>
        public static CommandMessage Ready(int seq, int seat)
        {
            var message = Create(OpCode.Ready, seq, seat);
            message.protocolMajor = ProtocolVersion.Major;
            message.protocolMinor = ProtocolVersion.Minor;
            return message;
        }

        /// <summary>방장 StartMatch 커맨드를 만든다.</summary>
        public static CommandMessage StartMatch(int seq, int seat)
        {
            return Create(OpCode.StartMatch, seq, seat);
        }

        /// <summary>PlayCard(instanceId) 커맨드를 만든다.</summary>
        public static CommandMessage PlayCard(int seq, int seat, int instanceId)
        {
            var message = Create(OpCode.PlayCard, seq, seat);
            message.instanceId = instanceId;
            return message;
        }

        /// <summary>Draw 커맨드를 만든다.</summary>
        public static CommandMessage Draw(int seq, int seat)
        {
            return Create(OpCode.Draw, seq, seat);
        }

        /// <summary>ChooseSuit 커맨드를 만든다. suit 는 SuitCode.</summary>
        public static CommandMessage ChooseSuit(int seq, int seat, string suit)
        {
            var message = Create(OpCode.ChooseSuit, seq, seat);
            message.suit = suit;
            return message;
        }

        /// <summary>ChooseQueenMode(Reverse|Give) 커맨드를 만든다.</summary>
        public static CommandMessage ChooseQueenMode(int seq, int seat, string queenMode)
        {
            var message = Create(OpCode.ChooseQueenMode, seq, seat);
            message.queenMode = queenMode;
            return message;
        }

        /// <summary>AcceptQueen 커맨드를 만든다.</summary>
        public static CommandMessage AcceptQueen(int seq, int seat)
        {
            return Create(OpCode.AcceptQueen, seq, seat);
        }

        /// <summary>GiveCards 커맨드를 만든다.</summary>
        public static CommandMessage GiveCards(int seq, int seat, int[] instanceIds)
        {
            var message = Create(OpCode.GiveCards, seq, seat);
            message.instanceIds = instanceIds;
            return message;
        }

        /// <summary>ChooseKingMode(Extra|Hide) 커맨드를 만든다.</summary>
        public static CommandMessage ChooseKingMode(int seq, int seat, string kingMode)
        {
            var message = Create(OpCode.ChooseKingMode, seq, seat);
            message.kingMode = kingMode;
            return message;
        }

        /// <summary>HideUnder 커맨드를 만든다.</summary>
        public static CommandMessage HideUnder(int seq, int seat, int instanceId)
        {
            var message = Create(OpCode.HideUnder, seq, seat);
            message.instanceId = instanceId;
            return message;
        }

        /// <summary>MirrorDiscard 커맨드를 만든다.</summary>
        public static CommandMessage MirrorDiscard(int seq, int seat, int[] instanceIds)
        {
            var message = Create(OpCode.MirrorDiscard, seq, seat);
            message.instanceIds = instanceIds;
            return message;
        }

        /// <summary>Surrender 커맨드를 만든다.</summary>
        public static CommandMessage Surrender(int seq, int seat)
        {
            return Create(OpCode.Surrender, seq, seat);
        }

        /// <summary>Chat 커맨드를 만든다. 본문 또는 quickId.</summary>
        public static CommandMessage Chat(int seq, int seat, string text, string channel, string quickId = null)
        {
            var message = Create(OpCode.Chat, seq, seat);
            message.text = text;
            message.channel = channel;
            message.quickId = quickId;
            return message;
        }

        /// <summary>RematchVote 커맨드를 만든다.</summary>
        public static CommandMessage RematchVote(int seq, int seat, bool rematchYes)
        {
            var message = Create(OpCode.RematchVote, seq, seat);
            message.rematchYes = rematchYes;
            return message;
        }

        /// <summary>Heartbeat 커맨드를 만든다.</summary>
        public static CommandMessage Heartbeat(int seq, int seat)
        {
            return Create(OpCode.Heartbeat, seq, seat);
        }

        /// <summary>SnapshotRequest 커맨드를 만든다.</summary>
        public static CommandMessage SnapshotRequest(int seq, int seat)
        {
            return Create(OpCode.SnapshotRequest, seq, seat);
        }

        private static CommandMessage Create(string op, int seq, int seat)
        {
            return new CommandMessage
            {
                op = op,
                seq = seq,
                seat = seat,
            };
        }
    }
}
