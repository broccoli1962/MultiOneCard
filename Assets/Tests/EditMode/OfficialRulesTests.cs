using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Game.Rules.Tests
{
    /// <summary>
    /// Official §5 헤드리스 EditMode 테스트. 고정 시드 배분 후 장을 재배치해 케이스를 고정한다.
    /// </summary>
    [TestFixture]
    [Category("EditMode")]
    public sealed class OfficialRulesTests
    {
        private const int FixedSeed = 20260823;
        private const int SimulationActions = 80;

        private static readonly Suit[] TrumpSuits =
        {
            Suit.Spade, Suit.Heart, Suit.Diamond, Suit.Club, Suit.Star, Suit.Moon,
        };

        #region Deal / 91

        [TestCase(2, 7)]
        [TestCase(3, 7)]
        [TestCase(4, 7)]
        [TestCase(5, 5)]
        [TestCase(6, 5)]
        public void Deal_FixedSeed_KeepsNinetyOneAndOfficialHandSize(int seatCount, int handSize)
        {
            var state = MatchState.Deal(seatCount, FixedSeed);
            Assert.That(state.CountAllCards(), Is.EqualTo(CardCatalog.OfficialInstanceCount));
            Assert.That(state.Discard.Count, Is.EqualTo(1));
            Assert.That(state.Rules.IsOfficial, Is.True);
            for (var seat = 0; seat < seatCount; seat++)
            {
                Assert.That(state.Hands[seat].Count, Is.EqualTo(handSize));
            }

            state.EnsureInvariant();
        }

        #endregion

        #region Legal / Illegal

        [Test]
        public void PlayCard_SameSuitOrRank_Accepts_MismatchRejects()
        {
            var state = Table(2);
            LeaveOnly(state, 0, "S8", "H5", "D9");
            SetDiscardTop(state, "S5");

            Assert.That(LegalMove.CanPlay(state, state.Catalog.GetDef("S8")), Is.True);
            Assert.That(LegalMove.CanPlay(state, state.Catalog.GetDef("H5")), Is.True);
            Assert.That(LegalMove.CanPlay(state, state.Catalog.GetDef("D9")), Is.False);
            AssertRejected(RuleEngine.PlayCard(state, 0, IdInHand(state, 0, "D9")), RejectCode.IllegalCard);
            AssertAccepted(RuleEngine.PlayCard(state, 0, IdInHand(state, 0, "S8")));
        }

        [Test]
        public void PlayCard_NotCurrentSeat_RejectsNotYourTurn()
        {
            var state = Table(2);
            LeaveOnly(state, 1, "S8");
            SetDiscardTop(state, "S5");
            AssertRejected(RuleEngine.PlayCard(state, 1, IdInHand(state, 1, "S8")), RejectCode.NotYourTurn);
        }

        [Test]
        public void LegalMove_NormalTurn_SevenAndWildOk_PassIllegal()
        {
            var state = Table(2);
            SetDiscardTop(state, "C6");
            Assert.That(LegalMove.CanPlay(state, state.Catalog.GetDef("H7")), Is.True);
            Assert.That(LegalMove.CanPlay(state, state.Catalog.GetDef(CardCatalog.IdSpear)), Is.True);
            Assert.That(LegalMove.CanPlay(state, state.Catalog.GetDef(CardCatalog.IdJokerColor)), Is.True);
            Assert.That(LegalMove.CanPlay(state, state.Catalog.GetDef(CardCatalog.IdPass)), Is.False);
        }

        [Test]
        public void PlayCard_AttackResponse_RejectsNonResponse()
        {
            var state = Table(2);
            LeaveOnly(state, 0, "S2", "S6");
            LeaveOnly(state, 1, "H8", "H3");
            SetDiscardTop(state, "S9");
            AssertAccepted(RuleEngine.PlayCard(state, 0, IdInHand(state, 0, "S2")));
            Assert.That(state.AttackStack, Is.EqualTo(CardCatalog.AttackTwo));
            AssertRejected(RuleEngine.PlayCard(state, 1, IdInHand(state, 1, "H8")), RejectCode.NotAttackResponse);
            AssertAccepted(RuleEngine.PlayCard(state, 1, IdInHand(state, 1, "H3")));
            Assert.That(state.AttackStack, Is.EqualTo(0));
        }

        #endregion

        #region Spear

        [Test]
        public void PlayCard_Spear_BlocksThreeFour_AllowsPass()
        {
            var state = Table(2);
            LeaveOnly(state, 0, CardCatalog.IdSpear, "H6");
            LeaveOnly(state, 1, "S3", "C4", CardCatalog.IdPass);
            SetDiscardTop(state, "H5");
            AssertAccepted(RuleEngine.PlayCard(state, 0, IdInHand(state, 0, CardCatalog.IdSpear)));
            Assert.That(state.AttackStack, Is.EqualTo(CardCatalog.AttackSpear));
            Assert.That(state.SpearInStack, Is.True);
            Assert.That(LegalMove.CanPlay(state, state.Catalog.GetDef("S3")), Is.False);
            AssertRejected(RuleEngine.PlayCard(state, 1, IdInHand(state, 1, "S3")), RejectCode.SpearNotDefendable);
            AssertRejected(RuleEngine.PlayCard(state, 1, IdInHand(state, 1, "C4")), RejectCode.SpearNotDefendable);
            AssertAccepted(RuleEngine.PlayCard(state, 1, IdInHand(state, 1, CardCatalog.IdPass)));
            Assert.That(state.AttackStack, Is.EqualTo(CardCatalog.AttackSpear));
            Assert.That(state.SpearInStack, Is.True);
        }

        #endregion

        #region Queen

        [Test]
        public void QueenGiveChain_TransfersThenClearsStack()
        {
            var state = Table(2);
            LeaveOnly(state, 0, "SQ", "S6", "S8");
            LeaveOnly(state, 1, "H9", "H10");
            SetDiscardTop(state, "S5");
            var giverCount = state.Hands[0].Count;
            var targetCount = state.Hands[1].Count;
            var givenId = IdInHand(state, 0, "S6");

            AssertAccepted(RuleEngine.PlayCard(state, 0, IdInHand(state, 0, "SQ")));
            Assert.That(state.PendingQueenModeSeat, Is.EqualTo(0));
            AssertAccepted(RuleEngine.ChooseQueenMode(state, 0, QueenMode.Give));
            Assert.That(state.QueenStack, Is.EqualTo(1));
            Assert.That(state.LastQueenSeat, Is.EqualTo(0));
            Assert.That(state.CurrentSeat, Is.EqualTo(1));

            AssertRejected(RuleEngine.PlayCard(state, 1, IdInHand(state, 1, "H9")), RejectCode.NotQueenResponse);
            AssertAccepted(RuleEngine.AcceptQueen(state, 1));
            Assert.That(state.PendingGiveSeat, Is.EqualTo(0));
            AssertAccepted(RuleEngine.GiveCards(state, 0, new[] { givenId }));

            Assert.That(state.QueenStack, Is.EqualTo(0));
            Assert.That(state.PendingGiveSeat, Is.Null);
            Assert.That(ContainsInstance(state.Hands[1], givenId), Is.True);
            Assert.That(state.Hands[0].Count, Is.EqualTo(giverCount - 2));
            Assert.That(state.Hands[1].Count, Is.EqualTo(targetCount + 1));
            Assert.That(state.CountAllCards(), Is.EqualTo(CardCatalog.OfficialInstanceCount));
        }

        #endregion

        #region King

        [Test]
        public void KingHide_ExcludesHiddenFromPublicTop()
        {
            var state = Table(2);
            LeaveOnly(state, 0, "SK", "H5", "C6");
            SetDiscardTop(state, "S8");
            var hideId = IdInHand(state, 0, "H5");

            AssertAccepted(RuleEngine.PlayCard(state, 0, IdInHand(state, 0, "SK")));
            Assert.That(state.PendingKingModeSeat, Is.EqualTo(0));
            AssertAccepted(RuleEngine.ChooseKingMode(state, 0, KingMode.Hide));
            AssertAccepted(RuleEngine.HideUnder(state, 0, hideId));

            Assert.That(state.IsHiddenDiscard(hideId), Is.True);
            Assert.That(state.DiscardTop.Def.Id, Is.EqualTo("SK"));
            var recent = state.GetPublicRecentDiscard();
            for (var i = 0; i < recent.Length; i++)
            {
                Assert.That(recent[i].InstanceId, Is.Not.EqualTo(hideId));
            }
        }

        [Test]
        public void KingExtra_AllowsOneMoreLegalCard()
        {
            var state = Table(2);
            LeaveOnly(state, 0, "SK", "S5", "S6");
            SetDiscardTop(state, "S8");
            AssertAccepted(RuleEngine.PlayCard(state, 0, IdInHand(state, 0, "SK")));
            AssertAccepted(RuleEngine.ChooseKingMode(state, 0, KingMode.Extra));
            Assert.That(state.KingExtraPending, Is.True);
            AssertAccepted(RuleEngine.PlayCard(state, 0, IdInHand(state, 0, "S5")));
            Assert.That(state.KingExtraPending, Is.False);
            Assert.That(state.CurrentSeat, Is.EqualTo(1));
        }

        #endregion

        #region Pill

        [Test]
        public void Pill_LocksColor_BlocksJokerAndColorless()
        {
            var state = Table(2);
            LeaveOnly(state, 0, CardCatalog.IdPillRed);
            LeaveOnly(state, 1, CardCatalog.IdJokerColor, CardCatalog.IdSpear, "H8", "S9");
            SetDiscardTop(state, "C5");
            var before = state.Hands[0].Count;

            AssertAccepted(RuleEngine.PlayCard(state, 0, IdInHand(state, 0, CardCatalog.IdPillRed)));
            Assert.That(state.RequiredColor, Is.EqualTo(ColorGroup.Red));
            Assert.That(state.RequiredSuit, Is.Null);
            Assert.That(state.Hands[0].Count, Is.EqualTo(before));
            Assert.That(state.IsSeatFinished(0), Is.False);
            Assert.That(state.WinnerSeat, Is.Null);

            AssertRejected(
                RuleEngine.PlayCard(state, 1, IdInHand(state, 1, CardCatalog.IdJokerColor)),
                RejectCode.ColorLocked);
            AssertRejected(
                RuleEngine.PlayCard(state, 1, IdInHand(state, 1, CardCatalog.IdSpear)),
                RejectCode.ColorLocked);
            AssertRejected(RuleEngine.PlayCard(state, 1, IdInHand(state, 1, "S9")), RejectCode.ColorLocked);
            AssertAccepted(RuleEngine.PlayCard(state, 1, IdInHand(state, 1, "H8")));
            Assert.That(state.RequiredColor, Is.Null);
        }

        [Test]
        public void Pill_RepeatChangesLockColor_NoSoloFinish()
        {
            var state = Table(2);
            LeaveOnly(state, 0, CardCatalog.IdPillRed);
            LeaveOnly(state, 1, CardCatalog.IdPillBlue);
            SetDiscardTop(state, "C5");
            AssertAccepted(RuleEngine.PlayCard(state, 0, IdInHand(state, 0, CardCatalog.IdPillRed)));
            Assert.That(state.RequiredColor, Is.EqualTo(ColorGroup.Red));
            AssertAccepted(RuleEngine.PlayCard(state, 1, IdInHand(state, 1, CardCatalog.IdPillBlue)));
            Assert.That(state.RequiredColor, Is.EqualTo(ColorGroup.Blue));
            Assert.That(state.IsSeatFinished(1), Is.False);
        }

        #endregion

        #region Finish

        [Test]
        public void PlayCard_LastLegalAttack_FinishesWithoutEffect()
        {
            var state = Table(2);
            LeaveOnly(state, 0, "S2");
            SetDiscardTop(state, "S5");
            AssertAccepted(RuleEngine.PlayCard(state, 0, IdInHand(state, 0, "S2")));
            Assert.That(state.WinnerSeat, Is.EqualTo(0));
            Assert.That(state.IsSeatFinished(0), Is.True);
            Assert.That(state.AttackStack, Is.EqualTo(0));
            Assert.That(state.IsMatchOver, Is.True);
        }

        [Test]
        public void PlayCard_LastPill_DoesNotFinish()
        {
            var state = Table(2);
            LeaveOnly(state, 0, CardCatalog.IdPillBlack);
            SetDiscardTop(state, "H5");
            AssertAccepted(RuleEngine.PlayCard(state, 0, IdInHand(state, 0, CardCatalog.IdPillBlack)));
            Assert.That(state.Hands[0].Count, Is.EqualTo(1));
            Assert.That(state.IsSeatFinished(0), Is.False);
            Assert.That(state.WinnerSeat, Is.Null);
            Assert.That(state.RequiredColor, Is.EqualTo(ColorGroup.Black));
        }

        #endregion

        #region ApplyTimeout

        [Test]
        public void ApplyTimeout_Normal_DrawsOneAndAdvances()
        {
            var state = Table(2);
            var before = state.Hands[0].Count;
            var deckBefore = state.Deck.Count;
            AssertAccepted(RuleEngine.ApplyTimeout(state, 0));
            Assert.That(state.Hands[0].Count, Is.EqualTo(before + 1));
            Assert.That(state.Deck.Count, Is.EqualTo(deckBefore - 1));
            Assert.That(state.CurrentSeat, Is.EqualTo(1));
            Assert.That(state.GetConsecutiveTimeouts(0), Is.EqualTo(1));
            Assert.That(state.DrewThisTurn, Is.False);
        }

        [Test]
        public void ApplyTimeout_Attack_TakesStack()
        {
            var state = Table(2);
            LeaveOnly(state, 0, "SA", "S6");
            SetDiscardTop(state, "S5");
            AssertAccepted(RuleEngine.PlayCard(state, 0, IdInHand(state, 0, "SA")));
            Assert.That(state.AttackStack, Is.EqualTo(CardCatalog.AttackAce));
            var before = state.Hands[1].Count;
            AssertAccepted(RuleEngine.ApplyTimeout(state, 1));
            Assert.That(state.Hands[1].Count, Is.EqualTo(before + CardCatalog.AttackAce));
            Assert.That(state.AttackStack, Is.EqualTo(0));
            Assert.That(state.CurrentSeat, Is.EqualTo(0));
        }

        [Test]
        public void ApplyTimeout_Seven_UsesPlayedSuit()
        {
            var state = Table(2);
            LeaveOnly(state, 0, "S7", "S6");
            SetDiscardTop(state, "S5");
            AssertAccepted(RuleEngine.PlayCard(state, 0, IdInHand(state, 0, "S7")));
            AssertAccepted(RuleEngine.ApplyTimeout(state, 0));
            Assert.That(state.RequiredSuit, Is.EqualTo(Suit.Spade));
            Assert.That(state.PendingSuitSeat, Is.Null);
            Assert.That(state.CurrentSeat, Is.EqualTo(1));
        }

        [Test]
        public void ApplyTimeout_QueenMode_Reverses()
        {
            var state = Table(3);
            LeaveOnly(state, 0, "SQ", "S6");
            SetDiscardTop(state, "S5");
            AssertAccepted(RuleEngine.PlayCard(state, 0, IdInHand(state, 0, "SQ")));
            AssertAccepted(RuleEngine.ApplyTimeout(state, 0));
            Assert.That(state.Direction, Is.EqualTo(MatchState.DirectionClockwise));
            Assert.That(state.PendingQueenModeSeat, Is.Null);
            Assert.That(state.QueenStack, Is.EqualTo(0));
            Assert.That(state.CurrentSeat, Is.EqualTo(2));
        }

        [Test]
        public void ApplyTimeout_QueenResponse_StartsGive()
        {
            var state = Table(2);
            LeaveOnly(state, 0, "SQ", "S6");
            LeaveOnly(state, 1, "H9");
            SetDiscardTop(state, "S5");
            AssertAccepted(RuleEngine.PlayCard(state, 0, IdInHand(state, 0, "SQ")));
            AssertAccepted(RuleEngine.ChooseQueenMode(state, 0, QueenMode.Give));
            AssertAccepted(RuleEngine.ApplyTimeout(state, 1));
            Assert.That(state.PendingGiveSeat, Is.EqualTo(0));
            Assert.That(state.QueenGiveTargetSeat, Is.EqualTo(1));
        }

        [Test]
        public void ApplyTimeout_KingMode_EndsWithoutExtra()
        {
            var state = Table(2);
            LeaveOnly(state, 0, "SK", "S5");
            SetDiscardTop(state, "S8");
            AssertAccepted(RuleEngine.PlayCard(state, 0, IdInHand(state, 0, "SK")));
            AssertAccepted(RuleEngine.ApplyTimeout(state, 0));
            Assert.That(state.PendingKingModeSeat, Is.Null);
            Assert.That(state.KingExtraPending, Is.False);
            Assert.That(state.CurrentSeat, Is.EqualTo(1));
            Assert.That(ContainsDef(state.Hands[0], "S5"), Is.True);
        }

        [Test]
        public void ApplyTimeout_Hide_HidesLowestScore()
        {
            var state = Table(2);
            LeaveOnly(state, 0, "SK", "H5", "HA");
            SetDiscardTop(state, "S8");
            var lowId = IdInHand(state, 0, "H5");
            AssertAccepted(RuleEngine.PlayCard(state, 0, IdInHand(state, 0, "SK")));
            AssertAccepted(RuleEngine.ChooseKingMode(state, 0, KingMode.Hide));
            AssertAccepted(RuleEngine.ApplyTimeout(state, 0));
            Assert.That(state.IsHiddenDiscard(lowId), Is.True);
            Assert.That(state.DiscardTop.Def.Id, Is.EqualTo("SK"));
            Assert.That(state.PendingHideUnderSeat, Is.Null);
        }

        [Test]
        public void ApplyTimeout_ThreeConsecutive_SurrendersWithoutDiscardingHand()
        {
            var state = Table(2);
            for (var i = 0; i < 2; i++)
            {
                AssertAccepted(RuleEngine.ApplyTimeout(state, 0));
                AssertAccepted(RuleEngine.ApplyTimeout(state, 1));
            }

            AssertAccepted(RuleEngine.ApplyTimeout(state, 0));
            Assert.That(state.IsSeatSurrendered(0), Is.True);
            Assert.That(state.GetConsecutiveTimeouts(0), Is.EqualTo(3));
            var remaining = SnapshotIds(state.Hands[0]);
            Assert.That(remaining.Count, Is.GreaterThan(0));
            for (var i = 0; i < remaining.Count; i++)
            {
                Assert.That(ContainsInstance(state.Discard, remaining[i]), Is.False);
            }

            Assert.That(state.CountAllCards(), Is.EqualTo(CardCatalog.OfficialInstanceCount));
        }

        #endregion

        #region Simulation

        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        public void RandomPolicy_ShortSimulation_KeepsNinetyOneInvariant(int seatCount)
        {
            var seed = FixedSeed + seatCount;
            var state = MatchState.Deal(seatCount, seed);
            var rng = new Random(seed);
            for (var step = 0; step < SimulationActions && !state.IsMatchOver; step++)
            {
                state.EnsureInvariant();
                Assert.That(state.CountAllCards(), Is.EqualTo(CardCatalog.OfficialInstanceCount));
                var seat = state.ActingSeat;
                var result = ApplyRandomPolicy(state, seat, rng);
                if (!result.IsAccepted && !state.IsMatchOver)
                {
                    result = RuleEngine.ApplyTimeout(state, state.ActingSeat);
                }

                Assert.That(result.IsAccepted || state.IsMatchOver, Is.True, $"step {step} seat {seat} {result.Reject}");
            }

            state.EnsureInvariant();
            Assert.That(state.CountAllCards(), Is.EqualTo(CardCatalog.OfficialInstanceCount));
        }

        #endregion

        #region Harness

        private static MatchState Table(int seatCount)
        {
            var state = MatchState.Deal(seatCount, FixedSeed);
            state.CurrentSeat = 0;
            state.Direction = MatchState.DirectionCounterclockwise;
            return state;
        }

        private static void AssertAccepted(RuleResult result)
        {
            Assert.That(result.IsAccepted, Is.True, result.Reject);
        }

        private static void AssertRejected(RuleResult result, string code)
        {
            Assert.That(result.IsAccepted, Is.False);
            Assert.That(result.Reject, Is.EqualTo(code));
        }

        private static void SetDiscardTop(MatchState state, string defId)
        {
            if (state.Discard.Count > 0 && state.DiscardTop.Def.Id == defId)
            {
                return;
            }

            state.Discard.Add(TakeCard(state, card => card.Def.Id == defId));
            EnsureDiscardNotEmpty(state);
            state.EnsureInvariant();
        }

        private static void LeaveOnly(MatchState state, int seat, params string[] defIds)
        {
            var hand = state.Hands[seat];
            while (hand.Count > 0)
            {
                state.Deck.Enqueue(hand[hand.Count - 1]);
                hand.RemoveAt(hand.Count - 1);
            }

            for (var i = 0; i < defIds.Length; i++)
            {
                hand.Add(TakeCard(state, card => card.Def.Id == defIds[i]));
            }

            EnsureDiscardNotEmpty(state);
            state.EnsureInvariant();
        }

        private static int IdInHand(MatchState state, int seat, string defId)
        {
            var hand = state.Hands[seat];
            for (var i = 0; i < hand.Count; i++)
            {
                if (hand[i].Def.Id == defId)
                {
                    return hand[i].InstanceId;
                }
            }

            throw new InvalidOperationException($"Seat {seat} missing {defId}.");
        }

        private static bool ContainsDef(IReadOnlyList<CardInstance> cards, string defId)
        {
            for (var i = 0; i < cards.Count; i++)
            {
                if (cards[i].Def.Id == defId)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsInstance(IReadOnlyList<CardInstance> cards, int instanceId)
        {
            for (var i = 0; i < cards.Count; i++)
            {
                if (cards[i].InstanceId == instanceId)
                {
                    return true;
                }
            }

            return false;
        }

        private static List<int> SnapshotIds(IReadOnlyList<CardInstance> cards)
        {
            var ids = new List<int>(cards.Count);
            for (var i = 0; i < cards.Count; i++)
            {
                ids.Add(cards[i].InstanceId);
            }

            return ids;
        }

        private static void EnsureDiscardNotEmpty(MatchState state)
        {
            if (state.Discard.Count > 0)
            {
                return;
            }

            if (!state.TryDrawFromDeck(out var card))
            {
                throw new InvalidOperationException("Discard empty and deck exhausted.");
            }

            state.Discard.Add(card);
        }

        private static CardInstance TakeCard(MatchState state, Func<CardInstance, bool> match)
        {
            for (var seat = 0; seat < state.SeatCount; seat++)
            {
                var hand = state.Hands[seat];
                for (var i = 0; i < hand.Count; i++)
                {
                    if (match(hand[i]))
                    {
                        var card = hand[i];
                        hand.RemoveAt(i);
                        return card;
                    }
                }
            }

            for (var i = 0; i < state.Discard.Count; i++)
            {
                if (match(state.Discard[i]))
                {
                    var card = state.Discard[i];
                    state.HiddenDiscardIds.Remove(card.InstanceId);
                    state.Discard.RemoveAt(i);
                    return card;
                }
            }

            var remaining = new Queue<CardInstance>();
            var found = default(CardInstance);
            var foundAny = false;
            foreach (var card in state.Deck)
            {
                if (!foundAny && match(card))
                {
                    found = card;
                    foundAny = true;
                    continue;
                }

                remaining.Enqueue(card);
            }

            if (!foundAny)
            {
                throw new InvalidOperationException("Requested card is not in the match.");
            }

            state.Deck.Clear();
            foreach (var card in remaining)
            {
                state.Deck.Enqueue(card);
            }

            return found;
        }

        private static RuleResult ApplyRandomPolicy(MatchState state, int seat, Random rng)
        {
            if (state.PendingSuitSeat == seat)
            {
                return RuleEngine.ChooseSuit(state, seat, TrumpSuits[rng.Next(TrumpSuits.Length)]);
            }

            if (state.PendingQueenModeSeat == seat)
            {
                return RuleEngine.ChooseQueenMode(
                    state,
                    seat,
                    rng.Next(2) == 0 ? QueenMode.Reverse : QueenMode.Give);
            }

            if (state.PendingGiveSeat == seat)
            {
                var fromHand = state.Hands[seat].Count < state.QueenStack
                    ? state.Hands[seat].Count
                    : state.QueenStack;
                return RuleEngine.GiveCards(state, seat, PickRandomIds(state.Hands[seat], fromHand, rng));
            }

            if (state.PendingKingModeSeat == seat)
            {
                var hide = rng.Next(2) == 0 && state.Hands[seat].Count > 0;
                return RuleEngine.ChooseKingMode(state, seat, hide ? KingMode.Hide : KingMode.Extra);
            }

            if (state.PendingHideUnderSeat == seat)
            {
                var hand = state.Hands[seat];
                return RuleEngine.HideUnder(state, seat, hand[rng.Next(hand.Count)].InstanceId);
            }

            if (state.PendingMirrorSeat == seat)
            {
                var need = state.Hands[seat].Count - state.MirrorTargetCount;
                return RuleEngine.MirrorDiscard(state, seat, PickRandomIds(state.Hands[seat], need, rng));
            }

            if (state.KingExtraPending)
            {
                var extra = CollectLegal(state, seat);
                extra.RemoveAll(id => state.Catalog.GetInstance(id).Def.Rank == Rank.King);
                if (extra.Count > 0)
                {
                    return RuleEngine.PlayCard(state, seat, extra[rng.Next(extra.Count)]);
                }

                return RuleEngine.ApplyTimeout(state, seat);
            }

            if (state.QueenStack > 0 && CollectLegal(state, seat).Count == 0)
            {
                return RuleEngine.AcceptQueen(state, seat);
            }

            var legal = CollectLegal(state, seat);
            if (legal.Count > 0 && rng.Next(3) != 0)
            {
                return RuleEngine.PlayCard(state, seat, legal[rng.Next(legal.Count)]);
            }

            var drawn = RuleEngine.DrawCard(state, seat);
            return drawn.IsAccepted ? drawn : RuleEngine.ApplyTimeout(state, seat);
        }

        private static List<int> CollectLegal(MatchState state, int seat)
        {
            var ids = new List<int>();
            var hand = state.Hands[seat];
            for (var i = 0; i < hand.Count; i++)
            {
                var card = hand[i];
                if (state.DrewThisTurn
                    && state.DrawnInstanceId.HasValue
                    && card.InstanceId != state.DrawnInstanceId.Value)
                {
                    continue;
                }

                if (LegalMove.CanPlay(state, card))
                {
                    ids.Add(card.InstanceId);
                }
            }

            return ids;
        }

        private static int[] PickRandomIds(IReadOnlyList<CardInstance> hand, int count, Random rng)
        {
            var ids = new int[hand.Count];
            for (var i = 0; i < hand.Count; i++)
            {
                ids[i] = hand[i].InstanceId;
            }

            for (var i = ids.Length - 1; i > 0; i--)
            {
                var j = rng.Next(i + 1);
                var tmp = ids[i];
                ids[i] = ids[j];
                ids[j] = tmp;
            }

            var take = count < ids.Length ? count : ids.Length;
            if (take < 0)
            {
                take = 0;
            }

            var result = new int[take];
            for (var i = 0; i < take; i++)
            {
                result[i] = ids[i];
            }

            return result;
        }

        #endregion
    }
}
