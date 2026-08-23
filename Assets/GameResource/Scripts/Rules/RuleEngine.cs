namespace Game.Rules
{
    /// <summary>
    /// Official 기본 수 엔진. 턴·소유·합법 검증 후 PlayCard / DrawCard 를 적용한다.
    /// 2/A/조커 공격, 3·4 방어, DrawAndPlay, 손패 0 즉시 1위(효과 미적용).
    /// </summary>
    public static class RuleEngine
    {
        /// <summary>
        /// 손패의 instanceId 장을 낸다. 합법이고 마지막 장이면 효과 없이 1위.
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

            if (state.DrewThisTurn && state.Rules.DrawAndPlay && state.DrawnInstanceId != instanceId)
            {
                return RuleResult.Rejected(RejectCode.IllegalCard);
            }

            if (!LegalMove.CanPlay(state, card))
            {
                return RuleResult.Rejected(ClassifyIllegal(state, card.Def));
            }

            TakeFromHand(state, seat, instanceId);
            state.Discard.Add(card);

            if (state.Hands[seat].Count == 0)
            {
                FinishSeat(state, seat);
                if (state.Rules.ContinueAfterFirstWin)
                {
                    AdvanceTurn(state);
                }

                state.EnsureInvariant();
                return RuleResult.Accepted();
            }

            ApplyPlayEffects(state, card.Def);
            UpdateRequiredAfterPlay(state, card.Def);
            ClearDrawFlags(state);
            AdvanceTurn(state);
            state.EnsureInvariant();
            return RuleResult.Accepted();
        }

        /// <summary>
        /// 드로우. 공격 중이면 스택 전부 감수(추가 수 없음).
        /// 일반 턴은 1장. DrawAndPlay 이고 낸 수 있으면 같은 턴에 그 장만 낼 수 있다.
        /// 덱이 재순환 후에도 비면 턴만 넘긴다.
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
                return RuleResult.Accepted();
            }

            if (state.QueenStack > 0)
            {
                return RuleResult.Rejected(RejectCode.IllegalCard);
            }

            if (!state.TryDrawFromDeck(out var drawn))
            {
                ClearDrawFlags(state);
                AdvanceTurn(state);
                state.EnsureInvariant();
                return RuleResult.Accepted();
            }

            state.Hands[seat].Add(drawn);
            state.DrewThisTurn = true;
            state.DrawnInstanceId = drawn.InstanceId;

            var canPlayDrawn = state.Rules.DrawAndPlay && LegalMove.CanPlay(state, drawn);
            if (!canPlayDrawn)
            {
                ClearDrawFlags(state);
                AdvanceTurn(state);
            }

            state.EnsureInvariant();
            return RuleResult.Accepted();
        }

        private static RuleResult GateTurn(MatchState state, int seat)
        {
            if (state.IsMatchOver)
            {
                return RuleResult.Rejected(RejectCode.NotYourTurn);
            }

            if (seat < 0 || seat >= state.SeatCount || seat != state.CurrentSeat)
            {
                return RuleResult.Rejected(RejectCode.NotYourTurn);
            }

            return RuleResult.Accepted();
        }

        private static string ClassifyIllegal(MatchState state, CardDef card)
        {
            if (state.AttackStack > 0)
            {
                if (IsThreeOrFour(card) && state.SpearInStack)
                {
                    return RejectCode.SpearNotDefendable;
                }

                return RejectCode.NotAttackResponse;
            }

            if (state.QueenStack > 0)
            {
                return RejectCode.NotQueenResponse;
            }

            if (state.RequiredColor.HasValue)
            {
                return RejectCode.ColorLocked;
            }

            return RejectCode.IllegalCard;
        }

        private static void ApplyPlayEffects(MatchState state, CardDef card)
        {
            if (state.AttackStack > 0 || state.QueenStack > 0)
            {
                if (IsThreeOrFour(card))
                {
                    state.AttackStack = 0;
                    state.QueenStack = 0;
                    state.SpearInStack = false;
                    return;
                }
            }

            var increment = AttackIncrement(state, card);
            if (increment > 0)
            {
                state.AttackStack += increment;
            }
        }

        private static void UpdateRequiredAfterPlay(MatchState state, CardDef card)
        {
            if (state.RequiredColor.HasValue && card.IsTrump && card.Color == state.RequiredColor.Value)
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
                if (!state.TryDrawFromDeck(out var card))
                {
                    break;
                }

                hand.Add(card);
            }

            state.AttackStack = 0;
            state.SpearInStack = false;
            ClearDrawFlags(state);
            AdvanceTurn(state);
        }

        private static void FinishSeat(MatchState state, int seat)
        {
            state.WinnerSeat = seat;
            state.AttackStack = 0;
            state.QueenStack = 0;
            state.SpearInStack = false;
            state.RequiredColor = null;
            ClearDrawFlags(state);
        }

        private static int AttackIncrement(MatchState state, CardDef card)
        {
            if (card.IsJoker)
            {
                switch (card.Spec)
                {
                    case SpecKind.JokerColor:
                        return state.JokerAttack.Color;
                    case SpecKind.JokerBw:
                        return state.JokerAttack.Bw;
                    case SpecKind.JokerMoon:
                        return state.JokerAttack.Moon;
                    default:
                        return 0;
                }
            }

            if (card.Rank == Rank.Two || card.Rank == Rank.Ace)
            {
                return card.AttackValue;
            }

            return 0;
        }

        private static bool IsThreeOrFour(CardDef card)
        {
            return card.Rank == Rank.Three || card.Rank == Rank.Four;
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
            var seatCount = state.SeatCount;
            var seat = from;
            for (var i = 0; i < seatCount; i++)
            {
                seat += state.Direction;
                if (seat < 0)
                {
                    seat += seatCount;
                }
                else if (seat >= seatCount)
                {
                    seat -= seatCount;
                }

                if (state.WinnerSeat != seat)
                {
                    return seat;
                }
            }

            return seat;
        }
    }
}
