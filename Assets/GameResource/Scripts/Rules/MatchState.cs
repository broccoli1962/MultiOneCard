using System;
using System.Collections.Generic;

namespace Game.Rules
{
    /// <summary>
    /// 조커 공격값. 카탈로그 기본을 덮어쓰며, 리버스 조커가 순환한다.
    /// </summary>
    public sealed class JokerAttackValues
    {
        public JokerAttackValues(int color, int bw, int moon)
        {
            Color = color;
            Bw = bw;
            Moon = moon;
        }

        public int Color { get; set; }

        public int Bw { get; set; }

        public int Moon { get; set; }

        /// <summary>
        /// 리버스 조커 순환 BW ← COLOR ← MOON ← BW. 이미 쌓인 공격 스택은 바꾸지 않는다.
        /// </summary>
        public void Cycle()
        {
            var nextBw = Color;
            var nextColor = Moon;
            var nextMoon = Bw;
            Color = nextColor;
            Bw = nextBw;
            Moon = nextMoon;
        }

        /// <summary>
        /// Official 조커 공격값 COLOR=10, BW=5, MOON=15.
        /// </summary>
        public static JokerAttackValues Official()
        {
            return new JokerAttackValues(
                CardCatalog.AttackJokerColor,
                CardCatalog.AttackJokerBw,
                CardCatalog.AttackJokerMoon);
        }
    }

    /// <summary>
    /// 한 매치의 서버 상태. 시드는 외부 주입이며 클라로 보내지 않는다.
    /// 컨테이너: Deck 큐, Discard 스택(top만 공개), Hands[seat].
    /// 불변식: 손패합+덱+버림 = 91.
    /// </summary>
    public sealed class MatchState
    {
        public const int DirectionCounterclockwise = 1;
        public const int DirectionClockwise = -1;

        /// <summary>연속 타임아웃 이 횟수면 기권.</summary>
        public const int ConsecutiveTimeoutLimit = 3;

        /// <summary>손패가 이 장수 이상이면 파산(기권과 동일).</summary>
        public const int BankruptHandCount = 20;

        private readonly Random _rng;
        private readonly List<CardInstance>[] _hands;
        private readonly bool[] _surrendered;
        private readonly int[] _consecutiveTimeouts;
        private readonly int[] _finishOrder;
        private readonly int[] _surrenderOrder;
        private int _nextFinishOrder;
        private int _nextSurrenderOrder;

        private MatchState(
            CardCatalog catalog,
            HouseRules rules,
            int seed,
            Random rng,
            Queue<CardInstance> deck,
            List<CardInstance> discard,
            List<CardInstance>[] hands,
            int currentSeat)
        {
            Catalog = catalog;
            Rules = rules;
            Seed = seed;
            _rng = rng;
            Deck = deck;
            Discard = discard;
            _hands = hands;
            CurrentSeat = currentSeat;
            Direction = DirectionCounterclockwise;
            JokerAttack = JokerAttackValues.Official();
            _surrendered = new bool[hands.Length];
            _consecutiveTimeouts = new int[hands.Length];
            _finishOrder = new int[hands.Length];
            _surrenderOrder = new int[hands.Length];
        }

        /// <summary>고정 Official 카탈로그.</summary>
        public CardCatalog Catalog { get; }

        /// <summary>이 매치의 하우스룰. 퀵매치면 Official.</summary>
        public HouseRules Rules { get; }

        /// <summary>서버 전용 시드. 공개 스냅샷·클라 이벤트에 넣지 않는다.</summary>
        public int Seed { get; }

        /// <summary>남은 덱. 앞이 다음 드로우.</summary>
        public Queue<CardInstance> Deck { get; }

        /// <summary>버림 스택. 마지막이 top. top 만 공개.</summary>
        public List<CardInstance> Discard { get; }

        /// <summary>좌석별 손패.</summary>
        public IReadOnlyList<List<CardInstance>> Hands => _hands;

        public int SeatCount => _hands.Length;

        /// <summary>현재 턴 좌석. 배분 후 난수 좌석.</summary>
        public int CurrentSeat { get; set; }

        /// <summary>진행 방향. 기본 반시계 +1.</summary>
        public int Direction { get; set; }

        /// <summary>조커 공격값. 카탈로그 기본을 덮어쓴다.</summary>
        public JokerAttackValues JokerAttack { get; }

        public Suit? RequiredSuit { get; set; }

        public ColorGroup? RequiredColor { get; set; }

        public int AttackStack { get; set; }

        public bool SpearInStack { get; set; }

        /// <summary>
        /// 공격 체인 중 3·4 방어 문양. 2·A가 마지막 공격일 때 설정.
        /// 패스·역날검 후에도 유지되며, 체인 종료 시 비운다.
        /// </summary>
        public Suit? AttackDefendSuit { get; set; }

        /// <summary>
        /// 공격 체인 중 3·4 방어 색. 조커가 마지막 공격일 때 설정(JokerDefendable).
        /// 패스·역날검 후에도 유지되며, 체인 종료 시 비운다.
        /// </summary>
        public ColorGroup? AttackDefendColor { get; set; }

        /// <summary>
        /// 공격 체인 중 2·A 이어가기 랭크. 같은 랭크만 스택(색·문양 무관).
        /// 조커·죽창 공격이면 null. 패스·역날검 후에도 유지.
        /// </summary>
        public Rank? AttackDefendRank { get; set; }

        /// <summary>이번 공격 체인에서 역날검을 이미 썼는지. 체인당 1회.</summary>
        public bool CounterUsedInChain { get; set; }

        public int QueenStack { get; set; }

        /// <summary>마지막으로 Q를 낸 좌석. Give면 이 좌석이 1장을 지급한다.</summary>
        public int? LastQueenSeat { get; set; }

        /// <summary>7 문양 지정을 기다리는 좌석.</summary>
        public int? PendingSuitSeat { get; set; }

        /// <summary>Q Reverse/Give 선택을 기다리는 좌석.</summary>
        public int? PendingQueenModeSeat { get; set; }

        /// <summary>Q Give 후 1장을 고를 좌석(낸 사람).</summary>
        public int? PendingGiveSeat { get; set; }

        /// <summary>Q 지급을 받을 다음 활성 좌석.</summary>
        public int? QueenGiveTargetSeat { get; set; }

        /// <summary>K Extra/Hide 선택을 기다리는 좌석.</summary>
        public int? PendingKingModeSeat { get; set; }

        /// <summary>K Hide 밑장을 고를 좌석.</summary>
        public int? PendingHideUnderSeat { get; set; }

        /// <summary>K Extra로 같은 턴에 합법 1장을 더 내야 하면 true.</summary>
        public bool KingExtraPending { get; set; }

        /// <summary>버림 중 K 밑장. 공개 top·최근 버림 8장에서 제외한다.</summary>
        public HashSet<int> HiddenDiscardIds { get; } = new HashSet<int>();

        /// <summary>미러 룸을 낸 좌석. 처리 중이 아니면 null.</summary>
        public int? MirrorOriginSeat { get; set; }

        /// <summary>미러 맞춤 목표 장수 N. 처리 중이 아니면 0.</summary>
        public int MirrorTargetCount { get; set; }

        /// <summary>미러 초과 버림을 고를 좌석. 없으면 null.</summary>
        public int? PendingMirrorSeat { get; set; }

        /// <summary>즉시 1위 좌석. 없으면 null. Official 은 잔여 순위전 없음.</summary>
        public int? WinnerSeat { get; set; }

        /// <summary>이번 턴에 드로우를 했는지. DrawAndPlay 허용 판정용.</summary>
        public bool DrewThisTurn { get; set; }

        /// <summary>이번 턴에 드로우한 장. DrawAndPlay 면 이 장만 같은 턴에 낼 수 있다.</summary>
        public int? DrawnInstanceId { get; set; }

        /// <summary>
        /// 첫 1위가 났고 Official처럼 잔여 순위전을 하지 않거나, 활성 좌석이 1명 이하면 true.
        /// </summary>
        public bool IsMatchOver =>
            ActiveSeatCount <= 1
            || (WinnerSeat.HasValue && !Rules.ContinueAfterFirstWin);

        /// <summary>완료·기권이 아닌 좌석 수. 점프·리버스는 이 좌석만 센다.</summary>
        public int ActiveSeatCount
        {
            get
            {
                var count = 0;
                for (var i = 0; i < SeatCount; i++)
                {
                    if (IsSeatActive(i))
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        /// <summary>
        /// 지금 수를 두거나 선택해야 하는 좌석. 타이머·ApplyTimeout 대상.
        /// </summary>
        public int ActingSeat
        {
            get
            {
                if (PendingMirrorSeat.HasValue)
                {
                    return PendingMirrorSeat.Value;
                }

                if (PendingSuitSeat.HasValue)
                {
                    return PendingSuitSeat.Value;
                }

                if (PendingQueenModeSeat.HasValue)
                {
                    return PendingQueenModeSeat.Value;
                }

                if (PendingGiveSeat.HasValue)
                {
                    return PendingGiveSeat.Value;
                }

                if (PendingKingModeSeat.HasValue)
                {
                    return PendingKingModeSeat.Value;
                }

                if (PendingHideUnderSeat.HasValue)
                {
                    return PendingHideUnderSeat.Value;
                }

                return CurrentSeat;
            }
        }

        /// <summary>공개 최근 버림 장수. K 숨김 제외.</summary>
        public const int PublicRecentDiscardMax = 8;

        /// <summary>버림 top. K 숨김 장은 건너뛴다. 배분 직후 항상 1장.</summary>
        public CardInstance DiscardTop
        {
            get
            {
                for (var i = Discard.Count - 1; i >= 0; i--)
                {
                    if (!HiddenDiscardIds.Contains(Discard[i].InstanceId))
                    {
                        return Discard[i];
                    }
                }

                return Discard[Discard.Count - 1];
            }
        }

        /// <summary>
        /// 최근 공개 버림(최대 <paramref name="max"/>장). K 숨김은 포함하지 않는다.
        /// 오래된 장부터 최근 순.
        /// </summary>
        public CardInstance[] GetPublicRecentDiscard(int max = PublicRecentDiscardMax)
        {
            if (max <= 0)
            {
                return Array.Empty<CardInstance>();
            }

            var buffer = new CardInstance[max];
            var count = 0;
            for (var i = Discard.Count - 1; i >= 0 && count < max; i--)
            {
                if (HiddenDiscardIds.Contains(Discard[i].InstanceId))
                {
                    continue;
                }

                buffer[count] = Discard[i];
                count++;
            }

            var result = new CardInstance[count];
            for (var i = 0; i < count; i++)
            {
                result[i] = buffer[count - 1 - i];
            }

            return result;
        }

        /// <summary>이 인스턴스가 K 밑장(공개 제외)인지.</summary>
        public bool IsHiddenDiscard(int instanceId)
        {
            return HiddenDiscardIds.Contains(instanceId);
        }

        /// <summary>손패 0으로 끝난 좌석인지.</summary>
        public bool IsSeatFinished(int seat)
        {
            EnsureSeat(seat);
            return _finishOrder[seat] > 0;
        }

        /// <summary>기권 좌석인지.</summary>
        public bool IsSeatSurrendered(int seat)
        {
            EnsureSeat(seat);
            return _surrendered[seat];
        }

        /// <summary>점프·리버스·턴에 포함되는 활성 좌석인지.</summary>
        public bool IsSeatActive(int seat)
        {
            EnsureSeat(seat);
            return _finishOrder[seat] == 0 && !_surrendered[seat];
        }

        /// <summary>연속 타임아웃 횟수. 자발적 수 이후 0.</summary>
        public int GetConsecutiveTimeouts(int seat)
        {
            EnsureSeat(seat);
            return _consecutiveTimeouts[seat];
        }

        /// <summary>손패 0 완료 순서. 미완료면 0.</summary>
        public int GetFinishOrder(int seat)
        {
            EnsureSeat(seat);
            return _finishOrder[seat];
        }

        /// <summary>기권 순서. 미기권이면 0.</summary>
        public int GetSurrenderOrder(int seat)
        {
            EnsureSeat(seat);
            return _surrenderOrder[seat];
        }

        /// <summary>손패 점수 합. 동률·자동 선택·순위에 쓴다.</summary>
        public int GetHandScore(int seat)
        {
            EnsureSeat(seat);
            var hand = _hands[seat];
            var score = 0;
            for (var i = 0; i < hand.Count; i++)
            {
                score += hand[i].Def.Score;
            }

            return score;
        }

        /// <summary>자발적 수가 받아들여지면 연속 타임아웃을 0으로 돌린다.</summary>
        public void ClearConsecutiveTimeouts(int seat)
        {
            EnsureSeat(seat);
            _consecutiveTimeouts[seat] = 0;
        }

        /// <summary>연속 타임아웃 횟수를 설정한다. ApplyTimeout 전용.</summary>
        public void SetConsecutiveTimeouts(int seat, int count)
        {
            EnsureSeat(seat);
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            _consecutiveTimeouts[seat] = count;
        }

        /// <summary>손패 0 완료로 표시한다. 이미 완료·기권이면 무시.</summary>
        public void MarkFinished(int seat)
        {
            EnsureSeat(seat);
            if (_finishOrder[seat] > 0 || _surrendered[seat])
            {
                return;
            }

            _nextFinishOrder += 1;
            _finishOrder[seat] = _nextFinishOrder;
        }

        /// <summary>기권으로 표시한다. 이미 완료·기권이면 무시.</summary>
        public void MarkSurrendered(int seat)
        {
            EnsureSeat(seat);
            if (_surrendered[seat] || _finishOrder[seat] > 0)
            {
                return;
            }

            _surrendered[seat] = true;
            _nextSurrenderOrder += 1;
            _surrenderOrder[seat] = _nextSurrenderOrder;
        }

        /// <summary>
        /// Official 91장을 시드로 셔플한 뒤 배분한다.
        /// 2~4인 7장, 5~6인 5장. 이후 덱 1장을 버림(특수여도 효과 없음).
        /// rules 생략 시 Official(퀵매치 기본).
        /// </summary>
        public static MatchState Deal(int seatCount, int seed, HouseRules rules = null)
        {
            if (seatCount < HouseRules.MinSeats || seatCount > HouseRules.MaxSeats)
            {
                throw new ArgumentOutOfRangeException(nameof(seatCount), seatCount, "Seat count must be 2..6.");
            }

            if (rules == null)
            {
                rules = HouseRules.Official;
            }

            var catalog = CardCatalog.BuildOfficial();
            var rng = new Random(seed);
            var pile = new CardInstance[catalog.Count];
            for (var i = 0; i < catalog.Count; i++)
            {
                pile[i] = catalog[i];
            }

            Shuffle(pile, rng);

            var deck = new Queue<CardInstance>(pile.Length);
            for (var i = 0; i < pile.Length; i++)
            {
                deck.Enqueue(pile[i]);
            }

            var handSize = HouseRules.HandSizeFor(seatCount);
            var hands = new List<CardInstance>[seatCount];
            for (var seat = 0; seat < seatCount; seat++)
            {
                hands[seat] = new List<CardInstance>(handSize);
            }

            for (var round = 0; round < handSize; round++)
            {
                for (var seat = 0; seat < seatCount; seat++)
                {
                    hands[seat].Add(deck.Dequeue());
                }
            }

            var discard = new List<CardInstance>(8);
            discard.Add(deck.Dequeue());

            var currentSeat = rng.Next(seatCount);
            var state = new MatchState(catalog, rules, seed, rng, deck, discard, hands, currentSeat);
            state.EnsureInvariant();
            return state;
        }

        /// <summary>퀵매치 배분. 하우스룰은 항상 Official.</summary>
        public static MatchState DealQuickMatch(int seatCount, int seed)
        {
            return Deal(seatCount, seed, HouseRules.QuickMatch);
        }

        /// <summary>
        /// 덱 고갈 시 공개 버림 top 1장을 남기고 나머지를 셔플해 덱에 넣는다.
        /// K 숨김은 top으로 쓰지 않고 재순환되며 숨김 표시를 해제한다.
        /// 공개 top 만 있으면 재순환할 장이 없어 false (DeckExhausted).
        /// </summary>
        public bool RecycleDiscard()
        {
            var topIndex = -1;
            for (var i = Discard.Count - 1; i >= 0; i--)
            {
                if (!HiddenDiscardIds.Contains(Discard[i].InstanceId))
                {
                    topIndex = i;
                    break;
                }
            }

            if (topIndex < 0 || Discard.Count <= 1)
            {
                return false;
            }

            var top = Discard[topIndex];
            var rest = new CardInstance[Discard.Count - 1];
            var write = 0;
            for (var i = 0; i < Discard.Count; i++)
            {
                if (i == topIndex)
                {
                    continue;
                }

                rest[write] = Discard[i];
                HiddenDiscardIds.Remove(Discard[i].InstanceId);
                write++;
            }

            Shuffle(rest, _rng);
            Discard.Clear();
            Discard.Add(top);
            for (var i = 0; i < rest.Length; i++)
            {
                Deck.Enqueue(rest[i]);
            }

            EnsureInvariant();
            return true;
        }

        /// <summary>
        /// 덱에서 1장을 뽑는다. 비었으면 RecycleDiscard 후 재시도.
        /// 그래도 없으면 false (DeckExhausted).
        /// </summary>
        public bool TryDrawFromDeck(out CardInstance card)
        {
            if (Deck.Count == 0 && !RecycleDiscard())
            {
                card = default;
                return false;
            }

            card = Deck.Dequeue();
            return true;
        }

        /// <summary>손패합 + 덱 + 버림.</summary>
        public int CountAllCards()
        {
            var total = Deck.Count + Discard.Count;
            for (var i = 0; i < _hands.Length; i++)
            {
                total += _hands[i].Count;
            }

            return total;
        }

        /// <summary>손패합+덱+버림=91 이고 인스턴스가 겹치지 않는지 검사한다.</summary>
        public void EnsureInvariant()
        {
            var total = CountAllCards();
            if (total != CardCatalog.OfficialInstanceCount)
            {
                throw new InvalidOperationException(
                    $"Match card count invariant failed: {total} != {CardCatalog.OfficialInstanceCount}.");
            }

            var seen = new HashSet<int>();
            foreach (var card in Deck)
            {
                AddUnique(seen, card);
            }

            for (var i = 0; i < Discard.Count; i++)
            {
                AddUnique(seen, Discard[i]);
            }

            for (var seat = 0; seat < _hands.Length; seat++)
            {
                var hand = _hands[seat];
                for (var i = 0; i < hand.Count; i++)
                {
                    AddUnique(seen, hand[i]);
                }
            }

            if (seen.Count != CardCatalog.OfficialInstanceCount)
            {
                throw new InvalidOperationException(
                    $"Match unique instance invariant failed: {seen.Count} != {CardCatalog.OfficialInstanceCount}.");
            }

            foreach (var hiddenId in HiddenDiscardIds)
            {
                var inDiscard = false;
                for (var i = 0; i < Discard.Count; i++)
                {
                    if (Discard[i].InstanceId == hiddenId)
                    {
                        inDiscard = true;
                        break;
                    }
                }

                if (!inDiscard)
                {
                    throw new InvalidOperationException(
                        $"Hidden discard {hiddenId} is not in the discard pile.");
                }
            }
        }

        private void EnsureSeat(int seat)
        {
            if (seat < 0 || seat >= SeatCount)
            {
                throw new ArgumentOutOfRangeException(nameof(seat));
            }
        }

        private static void AddUnique(HashSet<int> seen, CardInstance card)
        {
            if (!seen.Add(card.InstanceId))
            {
                throw new InvalidOperationException($"Duplicate instance id {card.InstanceId}.");
            }
        }

        private static void Shuffle(CardInstance[] cards, Random rng)
        {
            for (var i = cards.Length - 1; i > 0; i--)
            {
                var j = rng.Next(i + 1);
                var tmp = cards[i];
                cards[i] = cards[j];
                cards[j] = tmp;
            }
        }
    }
}
