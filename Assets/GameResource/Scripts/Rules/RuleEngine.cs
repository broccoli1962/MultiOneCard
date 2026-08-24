using System;
using System.Collections.Generic;

namespace Game.Rules
{
    /// <summary>
    /// Official 수 엔진. PlayCard / DrawCard / MirrorDiscard /
    /// ChooseSuit / ChooseQueenMode / AcceptQueen / GiveCards / ChooseKingMode / HideUnder /
    /// ApplyTimeout / Surrender / ComputeStandings.
    /// 랭크 특수: 7 문양, J 스킵, Q Reverse·Give(즉시 1장 지급), K Extra·Hide(숨김은 공개 제외).
    /// 공격: 2·A는 같은 랭크만 이어가기(색 무관), 조커 위 2·A는 색 일치. 조커 방어는 JokerDefendable 시 같은 색 3·4.
    /// 초과: 일반 드로우1, 공격 스택, Q Reverse/점수순 1장 지급, K만/낮은 점수 숨김, 7 원래 무늬, 미러 높은 점수.
    /// 연속 타임아웃 3회=기권. 손패 20장 이상=파산(기권과 동일). 점프·리버스는 활성 좌석만 센다.
    /// </summary>
    public static class RuleEngine
    {
        /// <summary>
        /// 손패의 instanceId 장을 낸다. 마지막 장이면 효과 없이 1위.
        /// 알약은 드로우가 있어 단독 피니시가 없다.
        /// requiredColor 락 중 다른 색 조커·무색(알약 제외)은 ColorLocked.
        /// </summary>
        public static RuleResult PlayCard(MatchState state, int seat, int instanceId)
        {
            var gate = GateTurn(state, seat);
            if (!gate.IsAccepted)
            {
                return gate;
            }

            if (!TryFindInHand(state, seat, instanceId, out var card))
            {
                return RuleResult.Rejected(RejectCode.NotInHand);
            }

            if (!state.KingExtraPending
                && state.DrewThisTurn
                && state.Rules.DrawAndPlay
                && state.DrawnInstanceId != instanceId)
            {
                return RuleResult.Rejected(RejectCode.IllegalCard);
            }

            if (IsColorLockedOut(state, card.Def))
            {
                return RuleResult.Rejected(RejectCode.ColorLocked);
            }

            if (!LegalMove.CanPlay(state, card))
            {
                return RuleResult.Rejected(ClassifyIllegal(state, card.Def));
            }

            if (state.KingExtraPending)
            {
                state.KingExtraPending = false;
            }

            TakeFromHand(state, seat, instanceId);
            state.Discard.Add(card);

            if (card.Def.Spec == SpecKind.Pill)
            {
                ApplyPill(state, seat, card.Def);
                if (!TryBankrupt(state, seat))
                {
                    AdvanceTurn(state);
                }

                state.EnsureInvariant();
                return AcceptAction(state, seat);
            }

            if (state.Hands[seat].Count == 0)
            {
                FinishSeat(state, seat);
                if (state.Rules.ContinueAfterFirstWin)
                {
                    AdvanceTurn(state);
                }

                state.EnsureInvariant();
                return AcceptAction(state, seat);
            }

            ApplyPlayedCard(state, card.Def);

            if (card.Def.Spec == SpecKind.Mirror)
            {
                ApplyMirror(state, seat);
                state.EnsureInvariant();
                return AcceptAction(state, seat);
            }

            if (card.Def.Spec == SpecKind.Counter)
            {
                state.CurrentSeat = PreviousSeat(state, seat);
                state.EnsureInvariant();
                return AcceptAction(state, seat);
            }

            if (BeginRankChoiceOrSkip(state, seat, card.Def))
            {
                state.EnsureInvariant();
                return AcceptAction(state, seat);
            }

            AdvanceTurn(state);
            state.EnsureInvariant();
            return AcceptAction(state, seat);
        }

        /// <summary>
        /// 드로우. 공격 중이면 스택 전부 감수. 일반은 1장. 덱 고갈이면 턴만 넘김.
        /// </summary>
        public static RuleResult DrawCard(MatchState state, int seat)
        {
            var gate = GateTurn(state, seat);
            if (!gate.IsAccepted)
            {
                return gate;
            }

            if (state.DrewThisTurn)
            {
                return RuleResult.Rejected(RejectCode.IllegalCard);
            }

            if (state.AttackStack > 0)
            {
                AcceptAttack(state, seat);
                state.EnsureInvariant();
                return AcceptAction(state, seat);
            }

            if (state.KingExtraPending)
            {
                return RuleResult.Rejected(RejectCode.IllegalCard);
            }

            if (!state.TryDrawFromDeck(out var drawn))
            {
                ClearDrawFlags(state);
                AdvanceTurn(state);
                state.EnsureInvariant();
                return AcceptAction(state, seat);
            }

            state.Hands[seat].Add(drawn);
            state.DrewThisTurn = true;
            state.DrawnInstanceId = drawn.InstanceId;
            if (TryBankrupt(state, seat))
            {
                state.EnsureInvariant();
                return AcceptAction(state, seat);
            }

            if (!(state.Rules.DrawAndPlay && LegalMove.CanPlay(state, drawn)))
            {
                ClearDrawFlags(state);
                AdvanceTurn(state);
            }

            state.EnsureInvariant();
            return AcceptAction(state, seat);
        }

        /// <summary>
        /// 미러 초과 좌석이 고른 장을 효과 없이 버린다. 장수 = 손패 - N.
        /// 처리 중 0장이면 그 좌석 1위.
        /// </summary>
        public static RuleResult MirrorDiscard(MatchState state, int seat, IReadOnlyList<int> instanceIds)
        {
            if (state.IsMatchOver || !state.PendingMirrorSeat.HasValue)
            {
                return RuleResult.Rejected(
                    state.IsMatchOver ? RejectCode.NotYourTurn : RejectCode.NeedMirrorDiscard);
            }

            if (seat != state.PendingMirrorSeat.Value)
            {
                return RuleResult.Rejected(RejectCode.NotYourTurn);
            }

            var hand = state.Hands[seat];
            var need = hand.Count - state.MirrorTargetCount;
            if (instanceIds == null || instanceIds.Count != need || need <= 0)
            {
                return RuleResult.Rejected(RejectCode.NeedMirrorDiscard);
            }

            var unique = new HashSet<int>();
            for (var i = 0; i < instanceIds.Count; i++)
            {
                if (!unique.Add(instanceIds[i]) || !TryFindInHand(state, seat, instanceIds[i], out _))
                {
                    return RuleResult.Rejected(RejectCode.NotInHand);
                }
            }

            for (var i = 0; i < instanceIds.Count; i++)
            {
                TryFindInHand(state, seat, instanceIds[i], out var discarded);
                TakeFromHand(state, seat, instanceIds[i]);
                state.Discard.Add(discarded);
            }

            if (hand.Count == 0)
            {
                FinishSeat(state, seat);
                if (state.IsMatchOver)
                {
                    ClearMirror(state);
                    state.EnsureInvariant();
                    return AcceptAction(state, seat);
                }
            }

            ResolveMirrorFrom(state, seat);
            state.EnsureInvariant();
            return AcceptAction(state, seat);
        }

        /// <summary>
        /// 7 이후 6문양 지정. 초과 시 호스트가 낸 7의 원래 무늬를 넘긴다.
        /// </summary>
        public static RuleResult ChooseSuit(MatchState state, int seat, Suit suit)
        {
            if (state.IsMatchOver)
            {
                return RuleResult.Rejected(RejectCode.NotYourTurn);
            }

            if (!state.PendingSuitSeat.HasValue)
            {
                return RuleResult.Rejected(RejectCode.NeedSuitPick);
            }

            if (seat != state.PendingSuitSeat.Value)
            {
                return RuleResult.Rejected(RejectCode.NotYourTurn);
            }

            if (!IsTrumpSuit(suit))
            {
                return RuleResult.Rejected(RejectCode.IllegalCard);
            }

            state.RequiredSuit = suit;
            state.PendingSuitSeat = null;
            AdvanceTurn(state);
            state.EnsureInvariant();
            return AcceptAction(state, seat);
        }

        /// <summary>
        /// Q 모드. Reverse=방향 반전 후 다음 활성. Give=다음 활성에게 손패 1장 즉시 지급.
        /// 지급은 방어·중첩이 없다.
        /// </summary>
        public static RuleResult ChooseQueenMode(MatchState state, int seat, QueenMode mode)
        {
            if (state.IsMatchOver)
            {
                return RuleResult.Rejected(RejectCode.NotYourTurn);
            }

            if (!state.PendingQueenModeSeat.HasValue)
            {
                return RuleResult.Rejected(RejectCode.NeedQueenMode);
            }

            if (seat != state.PendingQueenModeSeat.Value)
            {
                return RuleResult.Rejected(RejectCode.NotYourTurn);
            }

            state.PendingQueenModeSeat = null;
            if (mode == QueenMode.Give)
            {
                state.QueenStack = 1;
                state.LastQueenSeat = seat;
                state.PendingGiveSeat = seat;
                state.QueenGiveTargetSeat = NextSeat(state, seat);
                state.CurrentSeat = seat;
            }
            else
            {
                ApplyQueenReverse(state);
            }

            state.EnsureInvariant();
            return AcceptAction(state, seat);
        }

        /// <summary>
        /// 구 감수 API. Give는 즉시 지급이라 항상 거절한다.
        /// </summary>
        public static RuleResult AcceptQueen(MatchState state, int seat)
        {
            _ = state;
            _ = seat;
            return RuleResult.Rejected(RejectCode.NotQueenResponse);
        }

        /// <summary>
        /// Q를 낸 좌석이 손패 1장을 다음 활성에게 준다. 부족분은 덱에서 채운다.
        /// 지급 후 손패 0이면 1위. 아니면 받은 좌석 턴.
        /// </summary>
        public static RuleResult GiveCards(MatchState state, int seat, IReadOnlyList<int> instanceIds)
        {
            if (state.IsMatchOver || !state.PendingGiveSeat.HasValue)
            {
                return RuleResult.Rejected(
                    state.IsMatchOver ? RejectCode.NotYourTurn : RejectCode.NeedGiveCards);
            }

            if (seat != state.PendingGiveSeat.Value)
            {
                return RuleResult.Rejected(RejectCode.NotYourTurn);
            }

            if (!state.QueenGiveTargetSeat.HasValue || state.QueenStack <= 0)
            {
                return RuleResult.Rejected(RejectCode.NeedGiveCards);
            }

            var hand = state.Hands[seat];
            var fromHand = hand.Count < state.QueenStack ? hand.Count : state.QueenStack;
            if (instanceIds == null || instanceIds.Count != fromHand)
            {
                return RuleResult.Rejected(RejectCode.GiveCountMismatch);
            }

            var unique = new HashSet<int>();
            for (var i = 0; i < instanceIds.Count; i++)
            {
                if (!unique.Add(instanceIds[i]) || !TryFindInHand(state, seat, instanceIds[i], out _))
                {
                    return RuleResult.Rejected(RejectCode.NotInHand);
                }
            }

            var target = state.QueenGiveTargetSeat.Value;
            var targetHand = state.Hands[target];
            for (var i = 0; i < instanceIds.Count; i++)
            {
                TryFindInHand(state, seat, instanceIds[i], out var given);
                TakeFromHand(state, seat, instanceIds[i]);
                targetHand.Add(given);
            }

            var shortfall = state.QueenStack - fromHand;
            for (var i = 0; i < shortfall; i++)
            {
                if (!state.TryDrawFromDeck(out var drawn))
                {
                    break;
                }

                targetHand.Add(drawn);
            }

            ClearQueenChain(state);
            if (hand.Count == 0)
            {
                FinishSeat(state, seat);
                if (state.IsMatchOver)
                {
                    state.EnsureInvariant();
                    return AcceptAction(state, seat);
                }
            }

            state.CurrentSeat = target;
            ClearDrawFlags(state);
            TryBankrupt(state, target);
            state.EnsureInvariant();
            return AcceptAction(state, seat);
        }

        /// <summary>
        /// K 모드. Extra=K 기준 합법 1장 더(또 K면 재선택). Hide=밑장 대기. 숨길 장이 없으면 Hide 불가.
        /// </summary>
        public static RuleResult ChooseKingMode(MatchState state, int seat, KingMode mode)
        {
            if (state.IsMatchOver)
            {
                return RuleResult.Rejected(RejectCode.NotYourTurn);
            }

            if (!state.PendingKingModeSeat.HasValue)
            {
                return RuleResult.Rejected(RejectCode.NeedKingMode);
            }

            if (seat != state.PendingKingModeSeat.Value)
            {
                return RuleResult.Rejected(RejectCode.NotYourTurn);
            }

            if (mode == KingMode.Hide)
            {
                if (state.Hands[seat].Count == 0)
                {
                    return RuleResult.Rejected(RejectCode.NoCardToHide);
                }

                state.PendingKingModeSeat = null;
                state.PendingHideUnderSeat = seat;
                state.EnsureInvariant();
                return AcceptAction(state, seat);
            }

            state.PendingKingModeSeat = null;
            state.KingExtraPending = true;
            state.EnsureInvariant();
            return AcceptAction(state, seat);
        }

        /// <summary>
        /// 손패 1장을 K 밑에 넣는다. 효과 없음. 공개 top·최근 버림 히스토리에 나오지 않는다.
        /// </summary>
        public static RuleResult HideUnder(MatchState state, int seat, int instanceId)
        {
            if (state.IsMatchOver || !state.PendingHideUnderSeat.HasValue)
            {
                return RuleResult.Rejected(
                    state.IsMatchOver ? RejectCode.NotYourTurn : RejectCode.NeedHideUnder);
            }

            if (seat != state.PendingHideUnderSeat.Value)
            {
                return RuleResult.Rejected(RejectCode.NotYourTurn);
            }

            if (state.Hands[seat].Count == 0)
            {
                return RuleResult.Rejected(RejectCode.NoCardToHide);
            }

            if (!TryFindInHand(state, seat, instanceId, out var card))
            {
                return RuleResult.Rejected(RejectCode.NotInHand);
            }

            TakeFromHand(state, seat, instanceId);
            var insertAt = IndexOfPublicDiscardTop(state);
            if (insertAt < 0)
            {
                state.Discard.Add(card);
            }
            else
            {
                state.Discard.Insert(insertAt, card);
            }

            state.HiddenDiscardIds.Add(card.InstanceId);
            state.PendingHideUnderSeat = null;

            if (state.Hands[seat].Count == 0)
            {
                FinishSeat(state, seat);
                if (state.IsMatchOver)
                {
                    state.EnsureInvariant();
                    return AcceptAction(state, seat);
                }
            }

            AdvanceTurn(state);
            state.EnsureInvariant();
            return AcceptAction(state, seat);
        }

        /// <summary>
        /// 턴 초과. 기획서 §5 상황별 기본 수를 적용한다.
        /// 연속 3회면 기권한다. 손패는 버림에 넣는다.
        /// 일반 드로우 1장은 DrawAndPlay 여부와 관계없이 내고 턴을 끝낸다.
        /// </summary>
        public static RuleResult ApplyTimeout(MatchState state, int seat)
        {
            if (state.IsMatchOver || seat < 0 || seat >= state.SeatCount || !state.IsSeatActive(seat))
            {
                return RuleResult.Rejected(RejectCode.NotYourTurn);
            }

            if (seat != state.ActingSeat)
            {
                return RuleResult.Rejected(RejectCode.NotYourTurn);
            }

            var next = state.GetConsecutiveTimeouts(seat) + 1;
            var applied = ApplyTimeoutDefault(state, seat);
            if (!applied.IsAccepted)
            {
                return applied;
            }

            state.SetConsecutiveTimeouts(seat, next);
            if (next >= MatchState.ConsecutiveTimeoutLimit && state.IsSeatActive(seat))
            {
                ApplySurrender(state, seat);
            }

            state.EnsureInvariant();
            return RuleResult.Accepted();
        }

        /// <summary>
        /// 기권. 활성에서 빠지고 순위는 최하위. 손패는 버림에 넣는다.
        /// 선택 대기 중이면 초과 기본 수로 풀고, 자기 턴이면 드로우 없이 넘긴다.
        /// </summary>
        public static RuleResult Surrender(MatchState state, int seat)
        {
            if (state.IsMatchOver || seat < 0 || seat >= state.SeatCount || !state.IsSeatActive(seat))
            {
                return RuleResult.Rejected(RejectCode.NotYourTurn);
            }

            UnstickIfActing(state, seat);
            ApplySurrender(state, seat);
            state.EnsureInvariant();
            return RuleResult.Accepted();
        }

        /// <summary>
        /// 순위. 손패 0 완료가 먼저(완료 순), 나머지는 장수·점수 오름차순, 기권은 최하위.
        /// 같은 장수·점수면 좌석 번호가 낮은 쪽이 앞.
        /// </summary>
        public static SeatStanding[] ComputeStandings(MatchState state)
        {
            var rows = new SeatStanding[state.SeatCount];
            var order = new int[state.SeatCount];
            for (var seat = 0; seat < state.SeatCount; seat++)
            {
                order[seat] = seat;
                rows[seat] = new SeatStanding(
                    seat,
                    0,
                    state.Hands[seat].Count,
                    state.GetHandScore(seat),
                    state.IsSeatFinished(seat),
                    state.IsSeatSurrendered(seat));
            }

            Array.Sort(order, (a, b) => CompareStandings(state, a, b));

            var result = new SeatStanding[state.SeatCount];
            for (var i = 0; i < order.Length; i++)
            {
                var seat = order[i];
                var row = rows[seat];
                result[i] = new SeatStanding(
                    row.Seat,
                    i + 1,
                    row.CardCount,
                    row.Score,
                    row.IsFinished,
                    row.IsSurrendered);
            }

            return result;
        }

        /// <summary>
        /// requiredColor 락 중 다른 무색(죽창·패스·리버스조커·역날검·미러)은 낼 수 없다.
        /// 같은 색 조커는 LegalMove 가 허용한다. 알약은 락 색만 바꾼다.
        /// </summary>
        private static bool IsColorLockedOut(MatchState state, CardDef card)
        {
            if (!state.RequiredColor.HasValue || state.AttackStack > 0 || state.QueenStack > 0)
            {
                return false;
            }

            return card.IsColorless && card.Spec != SpecKind.Pill;
        }

        /// <summary>
        /// 죽창 +5·spearInStack / 패스 스택 유지 / 역날검 2×·체인 1회 / 리버스조커 순환·스택 불변.
        /// 2·A는 AttackDefendSuit, 조커는 AttackDefendColor. 패스·역날검은 방어 기준을 유지한다.
        /// </summary>
        private static void ApplyPlayedCard(MatchState state, CardDef card)
        {
            if (state.AttackStack > 0)
            {
                if (card.Rank == Rank.Three || card.Rank == Rank.Four)
                {
                    ClearAttackChain(state);
                    UpdateRequiredAfterPlay(state, card);
                    ClearDrawFlags(state);
                    return;
                }

                if (card.Spec == SpecKind.Pass)
                {
                    UpdateRequiredAfterPlay(state, card);
                    ClearDrawFlags(state);
                    return;
                }

                if (card.Spec == SpecKind.Counter)
                {
                    ApplyCounter(state);
                    UpdateRequiredAfterPlay(state, card);
                    ClearDrawFlags(state);
                    return;
                }
            }

            if (card.Spec == SpecKind.ReverseJoker)
            {
                ApplyReverseJoker(state);
                UpdateRequiredAfterPlay(state, card);
                ClearDrawFlags(state);
                return;
            }

            if (card.Spec == SpecKind.Spear)
            {
                ApplySpear(state);
                UpdateRequiredAfterPlay(state, card);
                ClearDrawFlags(state);
                return;
            }

            var increment = AttackIncrement(state, card);
            if (increment > 0)
            {
                state.AttackStack += increment;
                SetAttackDefend(state, card);
            }

            UpdateRequiredAfterPlay(state, card);
            ClearDrawFlags(state);
        }

        /// <summary>죽창: +5. 3·4 불가. 스택에 한 장이라도 있으면 spearInStack.</summary>
        private static void ApplySpear(MatchState state)
        {
            state.AttackStack += CardCatalog.AttackSpear;
            state.SpearInStack = true;
            state.AttackDefendSuit = null;
            state.AttackDefendColor = null;
            state.AttackDefendRank = null;
        }

        /// <summary>2·A → 문양 방어·같은 랭크 스택, 조커 → 색 방어. 패스·역날검 시 유지된다.</summary>
        private static void SetAttackDefend(MatchState state, CardDef card)
        {
            if (card.IsJoker)
            {
                state.AttackDefendSuit = null;
                state.AttackDefendColor = card.Color;
                state.AttackDefendRank = null;
                return;
            }

            if (card.Rank == Rank.Two || card.Rank == Rank.Ace)
            {
                state.AttackDefendSuit = card.Suit;
                state.AttackDefendColor = null;
                state.AttackDefendRank = card.Rank;
            }
        }

        /// <summary>역날검: 직전 활성에게 2×스택 새 응답. 체인당 1회. 죽창 속성은 유지.</summary>
        private static void ApplyCounter(MatchState state)
        {
            state.AttackStack *= 2;
            state.CounterUsedInChain = true;
        }

        /// <summary>리버스 조커: BW←COLOR←MOON←BW. 이미 쌓인 스택은 불변. 공개 top은 이 장.</summary>
        private static void ApplyReverseJoker(MatchState state)
        {
            state.JokerAttack.Cycle();
        }

        /// <summary>알약: 1장 드로우 후 requiredColor. 7 무늬보다 우선. 알약 반복은 락 색만 변경. 단독 피니시 없음.</summary>
        private static void ApplyPill(MatchState state, int seat, CardDef pill)
        {
            if (state.TryDrawFromDeck(out var drawn))
            {
                state.Hands[seat].Add(drawn);
            }

            state.RequiredColor = pill.Color;
            state.RequiredSuit = null;
            ClearDrawFlags(state);
        }

        /// <summary>
        /// 미러 룸: 낸 뒤 내 손패 N. 마지막 장이면 호출되지 않는다(효과 없음).
        /// 다른 좌석은 N에 맞춤. 방향의 다음부터. 초과는 MirrorDiscard, 부족은 드로우.
        /// </summary>
        private static void ApplyMirror(MatchState state, int originSeat)
        {
            state.MirrorOriginSeat = originSeat;
            state.MirrorTargetCount = state.Hands[originSeat].Count;
            state.PendingMirrorSeat = null;
            ResolveMirrorFrom(state, originSeat);
        }

        private static void ResolveMirrorFrom(MatchState state, int fromSeat)
        {
            var origin = state.MirrorOriginSeat.Value;
            var target = state.MirrorTargetCount;
            var seat = NextSeat(state, fromSeat);
            while (seat != origin)
            {
                if (state.IsMatchOver)
                {
                    ClearMirror(state);
                    return;
                }

                var hand = state.Hands[seat];
                if (hand.Count < target)
                {
                    while (hand.Count < target && state.TryDrawFromDeck(out var drawn))
                    {
                        hand.Add(drawn);
                    }

                    if (hand.Count >= MatchState.BankruptHandCount && state.IsSeatActive(seat))
                    {
                        DiscardHandIntoDiscard(state, seat);
                        state.MarkSurrendered(seat);
                        if (state.ActiveSeatCount <= 1)
                        {
                            EnsureLastActiveWins(state);
                            ClearMirror(state);
                            return;
                        }

                        seat = NextSeat(state, seat);
                        continue;
                    }
                }
                else if (hand.Count > target)
                {
                    state.PendingMirrorSeat = seat;
                    state.CurrentSeat = seat;
                    return;
                }

                if (hand.Count == 0)
                {
                    FinishSeat(state, seat);
                    if (state.IsMatchOver)
                    {
                        ClearMirror(state);
                        return;
                    }
                }

                seat = NextSeat(state, seat);
            }

            var next = state.MirrorOriginSeat ?? state.CurrentSeat;
            ClearMirror(state);
            state.CurrentSeat = next;
            AdvanceTurn(state);
        }

        private static void ClearMirror(MatchState state)
        {
            state.PendingMirrorSeat = null;
            state.MirrorOriginSeat = null;
            state.MirrorTargetCount = 0;
        }

        /// <summary>
        /// 7·Q·K 선택 대기를 열거나 J를 스킵한다. true면 턴 진행을 호출 쪽에서 하지 않는다.
        /// </summary>
        private static bool BeginRankChoiceOrSkip(MatchState state, int seat, CardDef card)
        {
            if (card.Rank == Rank.Seven)
            {
                state.PendingSuitSeat = seat;
                return true;
            }

            if (card.Rank == Rank.Queen)
            {
                state.LastQueenSeat = seat;
                state.PendingQueenModeSeat = seat;
                return true;
            }

            if (card.Rank == Rank.King)
            {
                state.PendingKingModeSeat = seat;
                return true;
            }

            if (card.Rank == Rank.Jack)
            {
                AdvanceTurn(state);
                AdvanceTurn(state);
                return true;
            }

            return false;
        }

        /// <summary>Q Reverse. 방향 반전 후 다음 활성에게 턴을 넘긴다.</summary>
        private static void ApplyQueenReverse(MatchState state)
        {
            state.Direction = -state.Direction;
            AdvanceTurn(state);
        }

        private static bool IsTrumpSuit(Suit suit)
        {
            return suit == Suit.Spade
                || suit == Suit.Heart
                || suit == Suit.Diamond
                || suit == Suit.Club
                || suit == Suit.Star
                || suit == Suit.Moon;
        }

        private static int IndexOfPublicDiscardTop(MatchState state)
        {
            for (var i = state.Discard.Count - 1; i >= 0; i--)
            {
                if (!state.HiddenDiscardIds.Contains(state.Discard[i].InstanceId))
                {
                    return i;
                }
            }

            return -1;
        }

        private static RuleResult AcceptAction(MatchState state, int seat)
        {
            state.ClearConsecutiveTimeouts(seat);
            return RuleResult.Accepted();
        }

        private static RuleResult ApplyTimeoutDefault(MatchState state, int seat)
        {
            if (state.PendingMirrorSeat == seat)
            {
                var need = state.Hands[seat].Count - state.MirrorTargetCount;
                if (need <= 0)
                {
                    return RuleResult.Rejected(RejectCode.NeedMirrorDiscard);
                }

                return MirrorDiscard(state, seat, PickInstanceIds(state.Hands[seat], need, highestFirst: true));
            }

            if (state.PendingSuitSeat == seat)
            {
                var suit = state.DiscardTop.Def.Suit;
                if (!IsTrumpSuit(suit))
                {
                    return RuleResult.Rejected(RejectCode.NeedSuitPick);
                }

                return ChooseSuit(state, seat, suit);
            }

            if (state.PendingQueenModeSeat == seat)
            {
                return ChooseQueenMode(state, seat, QueenMode.Reverse);
            }

            if (state.PendingGiveSeat == seat)
            {
                var fromHand = state.Hands[seat].Count < state.QueenStack
                    ? state.Hands[seat].Count
                    : state.QueenStack;
                return GiveCards(state, seat, PickInstanceIds(state.Hands[seat], fromHand, highestFirst: true));
            }

            if (state.PendingKingModeSeat == seat)
            {
                return EndKingOnly(state, seat);
            }

            if (state.PendingHideUnderSeat == seat)
            {
                if (state.Hands[seat].Count == 0)
                {
                    return EndKingOnly(state, seat);
                }

                return HideUnder(state, seat, PickInstanceIds(state.Hands[seat], 1, highestFirst: false)[0]);
            }

            if (state.AttackStack > 0)
            {
                return DrawCard(state, seat);
            }

            if (state.KingExtraPending)
            {
                return EndKingOnly(state, seat);
            }

            if (state.DrewThisTurn)
            {
                ClearDrawFlags(state);
                AdvanceTurn(state);
                state.EnsureInvariant();
                return AcceptAction(state, seat);
            }

            var drawn = DrawCard(state, seat);
            if (drawn.IsAccepted && state.DrewThisTurn)
            {
                ClearDrawFlags(state);
                AdvanceTurn(state);
                state.EnsureInvariant();
            }

            return drawn;
        }

        private static RuleResult EndKingOnly(MatchState state, int seat)
        {
            state.PendingKingModeSeat = null;
            state.PendingHideUnderSeat = null;
            state.KingExtraPending = false;
            AdvanceTurn(state);
            state.EnsureInvariant();
            return AcceptAction(state, seat);
        }

        private static void UnstickIfActing(MatchState state, int seat)
        {
            if (seat != state.ActingSeat)
            {
                return;
            }

            if (state.PendingMirrorSeat == seat
                || state.PendingSuitSeat == seat
                || state.PendingQueenModeSeat == seat
                || state.PendingGiveSeat == seat
                || state.PendingKingModeSeat == seat
                || state.PendingHideUnderSeat == seat)
            {
                ApplyTimeoutDefault(state, seat);
                return;
            }

            if (state.CurrentSeat == seat)
            {
                AdvanceTurn(state);
            }
        }

        private static void ApplySurrender(MatchState state, int seat)
        {
            DiscardHandIntoDiscard(state, seat);
            state.MarkSurrendered(seat);
            ClearDrawFlags(state);
            if (state.PendingMirrorSeat == seat)
            {
                ResolveMirrorFrom(state, seat);
            }

            AfterSeatInactive(state);
        }

        /// <summary>손패 전부를 버림 스택에 넣고 손을 비운다.</summary>
        private static void DiscardHandIntoDiscard(MatchState state, int seat)
        {
            var hand = state.Hands[seat];
            for (var i = 0; i < hand.Count; i++)
            {
                state.Discard.Add(hand[i]);
            }

            hand.Clear();
        }

        /// <summary>손패 20장 이상이면 파산(기권과 동일). true면 탈락 적용.</summary>
        private static bool TryBankrupt(MatchState state, int seat)
        {
            if (!state.IsSeatActive(seat) || state.Hands[seat].Count < MatchState.BankruptHandCount)
            {
                return false;
            }

            ApplySurrender(state, seat);
            return true;
        }

        private static void AfterSeatInactive(MatchState state)
        {
            if (state.ActiveSeatCount <= 1)
            {
                EnsureLastActiveWins(state);
                return;
            }

            if (state.IsMatchOver)
            {
                return;
            }

            if (!state.IsSeatActive(state.CurrentSeat))
            {
                AdvanceTurn(state);
            }
        }

        private static void EnsureLastActiveWins(MatchState state)
        {
            for (var i = 0; i < state.SeatCount; i++)
            {
                if (state.IsSeatActive(i) && !state.WinnerSeat.HasValue)
                {
                    state.WinnerSeat = i;
                    break;
                }
            }
        }

        private static int CompareStandings(MatchState state, int a, int b)
        {
            var groupA = StandingGroup(state, a);
            var groupB = StandingGroup(state, b);
            var groupCmp = groupA.CompareTo(groupB);
            if (groupCmp != 0)
            {
                return groupCmp;
            }

            if (groupA == 0)
            {
                return state.GetFinishOrder(a).CompareTo(state.GetFinishOrder(b));
            }

            var countCmp = state.Hands[a].Count.CompareTo(state.Hands[b].Count);
            if (countCmp != 0)
            {
                return countCmp;
            }

            var scoreCmp = state.GetHandScore(a).CompareTo(state.GetHandScore(b));
            if (scoreCmp != 0)
            {
                return scoreCmp;
            }

            return a.CompareTo(b);
        }

        private static int StandingGroup(MatchState state, int seat)
        {
            if (state.IsSeatFinished(seat))
            {
                return 0;
            }

            return state.IsSeatSurrendered(seat) ? 2 : 1;
        }

        private static int[] PickInstanceIds(IReadOnlyList<CardInstance> hand, int count, bool highestFirst)
        {
            var order = new int[hand.Count];
            for (var i = 0; i < hand.Count; i++)
            {
                order[i] = i;
            }

            Array.Sort(order, (left, right) =>
            {
                var scoreCmp = hand[left].Def.Score.CompareTo(hand[right].Def.Score);
                if (scoreCmp == 0)
                {
                    scoreCmp = hand[left].InstanceId.CompareTo(hand[right].InstanceId);
                }

                return highestFirst ? -scoreCmp : scoreCmp;
            });

            var take = count < hand.Count ? count : hand.Count;
            if (take < 0)
            {
                take = 0;
            }

            var ids = new int[take];
            for (var i = 0; i < take; i++)
            {
                ids[i] = hand[order[i]].InstanceId;
            }

            return ids;
        }

        private static RuleResult GateTurn(MatchState state, int seat)
        {
            if (state.IsMatchOver)
            {
                return RuleResult.Rejected(RejectCode.NotYourTurn);
            }

            var pending = GatePendingChoice(state);
            if (!pending.IsAccepted)
            {
                return pending;
            }

            if (seat < 0 || seat >= state.SeatCount || seat != state.CurrentSeat || !state.IsSeatActive(seat))
            {
                return RuleResult.Rejected(RejectCode.NotYourTurn);
            }

            return RuleResult.Accepted();
        }

        private static RuleResult GatePendingChoice(MatchState state)
        {
            if (state.PendingMirrorSeat.HasValue)
            {
                return RuleResult.Rejected(RejectCode.NeedMirrorDiscard);
            }

            if (state.PendingSuitSeat.HasValue)
            {
                return RuleResult.Rejected(RejectCode.NeedSuitPick);
            }

            if (state.PendingQueenModeSeat.HasValue)
            {
                return RuleResult.Rejected(RejectCode.NeedQueenMode);
            }

            if (state.PendingGiveSeat.HasValue)
            {
                return RuleResult.Rejected(RejectCode.NeedGiveCards);
            }

            if (state.PendingKingModeSeat.HasValue)
            {
                return RuleResult.Rejected(RejectCode.NeedKingMode);
            }

            if (state.PendingHideUnderSeat.HasValue)
            {
                return RuleResult.Rejected(RejectCode.NeedHideUnder);
            }

            return RuleResult.Accepted();
        }

        private static string ClassifyIllegal(MatchState state, CardDef card)
        {
            if (state.AttackStack > 0)
            {
                if ((card.Rank == Rank.Three || card.Rank == Rank.Four) && state.SpearInStack)
                {
                    return RejectCode.SpearNotDefendable;
                }

                if (card.Spec == SpecKind.Counter && state.CounterUsedInChain)
                {
                    return RejectCode.CounterAlreadyUsed;
                }

                return RejectCode.NotAttackResponse;
            }

            if (state.QueenStack > 0)
            {
                return RejectCode.NotQueenResponse;
            }

            return state.RequiredColor.HasValue ? RejectCode.ColorLocked : RejectCode.IllegalCard;
        }

        private static void UpdateRequiredAfterPlay(MatchState state, CardDef card)
        {
            if (state.RequiredColor.HasValue
                && card.Color == state.RequiredColor.Value
                && (card.IsTrump || card.IsJoker))
            {
                state.RequiredColor = null;
            }

            state.RequiredSuit = card.IsTrump ? card.Suit : (Suit?)null;
        }

        private static void AcceptAttack(MatchState state, int seat)
        {
            var remaining = state.AttackStack;
            var hand = state.Hands[seat];
            for (var i = 0; i < remaining; i++)
            {
                if (!state.TryDrawFromDeck(out var drawn))
                {
                    break;
                }

                hand.Add(drawn);
            }

            ClearAttackChain(state);
            ClearDrawFlags(state);
            if (!TryBankrupt(state, seat))
            {
                AdvanceTurn(state);
            }
        }

        private static void FinishSeat(MatchState state, int seat)
        {
            state.MarkFinished(seat);
            if (!state.WinnerSeat.HasValue)
            {
                state.WinnerSeat = seat;
            }

            ClearAttackChain(state);
            ClearQueenChain(state);
            ClearPendingChoices(state);
            state.RequiredColor = null;
            ClearDrawFlags(state);
        }

        private static void ClearQueenChain(MatchState state)
        {
            state.QueenStack = 0;
            state.LastQueenSeat = null;
            state.PendingGiveSeat = null;
            state.QueenGiveTargetSeat = null;
        }

        private static void ClearPendingChoices(MatchState state)
        {
            state.PendingSuitSeat = null;
            state.PendingQueenModeSeat = null;
            state.PendingGiveSeat = null;
            state.QueenGiveTargetSeat = null;
            state.PendingKingModeSeat = null;
            state.PendingHideUnderSeat = null;
            state.KingExtraPending = false;
        }

        private static void ClearAttackChain(MatchState state)
        {
            state.AttackStack = 0;
            state.SpearInStack = false;
            state.CounterUsedInChain = false;
            state.AttackDefendSuit = null;
            state.AttackDefendColor = null;
            state.AttackDefendRank = null;
        }

        private static int AttackIncrement(MatchState state, CardDef card)
        {
            if (card.IsJoker)
            {
                if (card.Spec == SpecKind.JokerColor)
                {
                    return state.JokerAttack.Color;
                }

                if (card.Spec == SpecKind.JokerBw)
                {
                    return state.JokerAttack.Bw;
                }

                return card.Spec == SpecKind.JokerMoon ? state.JokerAttack.Moon : 0;
            }

            return card.Rank == Rank.Two || card.Rank == Rank.Ace ? card.AttackValue : 0;
        }

        private static bool TryFindInHand(MatchState state, int seat, int instanceId, out CardInstance card)
        {
            var hand = state.Hands[seat];
            for (var i = 0; i < hand.Count; i++)
            {
                if (hand[i].InstanceId == instanceId)
                {
                    card = hand[i];
                    return true;
                }
            }

            card = default;
            return false;
        }

        private static void TakeFromHand(MatchState state, int seat, int instanceId)
        {
            var hand = state.Hands[seat];
            for (var i = 0; i < hand.Count; i++)
            {
                if (hand[i].InstanceId == instanceId)
                {
                    hand.RemoveAt(i);
                    return;
                }
            }
        }

        private static void ClearDrawFlags(MatchState state)
        {
            state.DrewThisTurn = false;
            state.DrawnInstanceId = null;
        }

        private static void AdvanceTurn(MatchState state)
        {
            state.CurrentSeat = NextSeat(state, state.CurrentSeat);
            ClearDrawFlags(state);
        }

        private static int NextSeat(MatchState state, int from)
        {
            return StepActive(state, from, state.Direction);
        }

        private static int PreviousSeat(MatchState state, int from)
        {
            return StepActive(state, from, -state.Direction);
        }

        private static int StepActive(MatchState state, int from, int step)
        {
            var seatCount = state.SeatCount;
            var seat = from;
            for (var i = 0; i < seatCount; i++)
            {
                seat += step;
                if (seat < 0)
                {
                    seat += seatCount;
                }
                else if (seat >= seatCount)
                {
                    seat -= seatCount;
                }

                if (state.IsSeatActive(seat))
                {
                    return seat;
                }
            }

            return from;
        }
    }
}
