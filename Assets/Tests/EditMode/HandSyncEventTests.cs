using System;
using System.Collections.Generic;
using Backend.App;
using Backend.Net;
using Game.Rules;
using NUnit.Framework;

namespace Game.Rules.Tests
{
    /// <summary>
    /// Q 지급·K 숨김은 손패를 HandGranted 로 갈아끼우지 않고 빠진 장만 뺀다.
    /// 알약은 같은 행동에서 낸 뒤 뽑으므로 CardPlayed 가 CardDrawn 보다 앞이어야 한다.
    /// </summary>
    [TestFixture]
    [Category("EditMode")]
    public sealed class HandSyncEventTests
    {
        private const int Seed = 20260823;

        [Test]
        public void GiveCards_EmitsQueenGivenIds_DoesNotGrantFullHand()
        {
            var runtime = StartMatch();
            var state = runtime.Match;
            LeaveOnly(state, 0, "S6", "S8");
            LeaveOnly(state, 1, "H3");
            SetDiscardTop(state, "SQ");
            state.PendingGiveSeat = 0;
            state.QueenGiveTargetSeat = 1;
            state.QueenStack = 1;
            state.LastQueenSeat = 0;
            state.CurrentSeat = 0;
            var givenId = IdInHand(state, 0, "S6");

            var events = runtime.Submit(CommandMessage.GiveCards(0, 0, new[] { givenId }), 1);

            Assert.That(CountEv(events, EvCode.HandGranted), Is.EqualTo(0));
            var given = FindEv(events, EvCode.QueenGiven);
            Assert.That(given, Is.Not.Null);
            Assert.That(given.fromSeat, Is.EqualTo(0));
            Assert.That(given.toSeat, Is.EqualTo(1));
            Assert.That(given.instanceIds, Is.EqualTo(new[] { givenId }));
            Assert.That(CountEv(MatchRuntime.EventsForSeat(events, 1), EvCode.CardsReceived), Is.EqualTo(1));
        }

        [Test]
        public void HideUnder_EmitsKingHiddenIds_DoesNotGrantFullHand()
        {
            var runtime = StartMatch();
            var state = runtime.Match;
            LeaveOnly(state, 0, "SK", "H5");
            LeaveOnly(state, 1, "C6");
            SetDiscardTop(state, "S8");
            state.PendingHideUnderSeat = 0;
            state.CurrentSeat = 0;
            var hideId = IdInHand(state, 0, "H5");

            var events = runtime.Submit(CommandMessage.HideUnder(0, 0, hideId), 1);

            Assert.That(CountEv(events, EvCode.HandGranted), Is.EqualTo(0));
            var hidden = FindEv(events, EvCode.KingHidden);
            Assert.That(hidden, Is.Not.Null);
            Assert.That(hidden.seat, Is.EqualTo(0));
            Assert.That(hidden.instanceIds, Is.EqualTo(new[] { hideId }));
        }

        [Test]
        public void QueenGiven_RemovesOnlyGivenCardsFromGiver()
        {
            var transport = new FakeTransport();
            var client = new NetClient(transport, 0);
            client.Connect();
            var granted = EventMessage.Create(EvCode.HandGranted, 1, 0);
            granted.instanceIds = new[] { 1, 2, 3 };
            granted.defIds = new[] { "S6", "S8", "H3" };
            transport.Push(granted);

            var given = EventMessage.Create(EvCode.QueenGiven, 2, 1);
            given.fromSeat = 0;
            given.toSeat = 1;
            given.instanceIds = new[] { 2 };
            transport.Push(given);

            Assert.That(client.HandInstanceIds, Is.EqualTo(new[] { 1, 3 }));
            Assert.That(client.HandDefIds, Is.EqualTo(new[] { "S6", "H3" }));
        }

        [Test]
        public void QueenGiven_DoesNotRemoveFromReceiverHand()
        {
            var transport = new FakeTransport();
            var client = new NetClient(transport, 1);
            client.Connect();
            var granted = EventMessage.Create(EvCode.HandGranted, 1, 1);
            granted.instanceIds = new[] { 10, 11 };
            granted.defIds = new[] { "H3", "HQ" };
            transport.Push(granted);
            var received = EventMessage.Create(EvCode.CardsReceived, 2, 1);
            received.instanceIds = new[] { 2 };
            received.defIds = new[] { "S8" };
            transport.Push(received);

            var given = EventMessage.Create(EvCode.QueenGiven, 3, 1);
            given.fromSeat = 0;
            given.toSeat = 1;
            given.instanceIds = new[] { 2 };
            transport.Push(given);

            Assert.That(client.HandInstanceIds, Is.EqualTo(new[] { 10, 11, 2 }));
        }

        [Test]
        public void KingHidden_RemovesHiddenCardFromHiderHand()
        {
            var transport = new FakeTransport();
            var client = new NetClient(transport, 0);
            client.Connect();
            var granted = EventMessage.Create(EvCode.HandGranted, 1, 0);
            granted.instanceIds = new[] { 1, 2, 3 };
            granted.defIds = new[] { "SK", "H5", "C6" };
            transport.Push(granted);

            var hidden = EventMessage.Create(EvCode.KingHidden, 2, 0);
            hidden.instanceIds = new[] { 2 };
            transport.Push(hidden);

            Assert.That(client.HandInstanceIds, Is.EqualTo(new[] { 1, 3 }));
            Assert.That(client.HandDefIds, Is.EqualTo(new[] { "SK", "C6" }));
        }

        [Test]
        public void Pill_EmitsCardPlayedBeforeCardDrawn()
        {
            var runtime = StartMatch();
            var state = runtime.Match;
            LeaveOnly(state, 0, CardCatalog.IdPillRed, "S8");
            LeaveOnly(state, 1, "H3");
            SetDiscardTop(state, "C5");
            state.CurrentSeat = 0;
            var pillId = IdInHand(state, 0, CardCatalog.IdPillRed);
            var keptId = IdInHand(state, 0, "S8");

            var events = runtime.Submit(CommandMessage.PlayCard(0, 0, pillId), 1);
            var forSeat = MatchRuntime.EventsForSeat(events, 0);

            var playedAt = IndexOfEv(forSeat, EvCode.CardPlayed);
            var drawnAt = IndexOfEv(forSeat, EvCode.CardDrawn);
            Assert.That(playedAt, Is.GreaterThanOrEqualTo(0));
            Assert.That(drawnAt, Is.GreaterThanOrEqualTo(0));
            Assert.That(playedAt, Is.LessThan(drawnAt));
            Assert.That(forSeat[playedAt].instanceId, Is.EqualTo(pillId));
            Assert.That(forSeat[drawnAt].instanceIds, Is.Not.Null);
            Assert.That(forSeat[drawnAt].instanceIds.Length, Is.EqualTo(1));
            Assert.That(forSeat[drawnAt].instanceIds[0], Is.Not.EqualTo(pillId));
            Assert.That(forSeat[drawnAt].instanceIds[0], Is.Not.EqualTo(keptId));
        }

        [Test]
        public void CardPlayedThenCardDrawn_ReplacesPlayedWithDrawn()
        {
            var transport = new FakeTransport();
            var client = new NetClient(transport, 0);
            client.Connect();
            var granted = EventMessage.Create(EvCode.HandGranted, 1, 0);
            granted.instanceIds = new[] { 1, 2 };
            granted.defIds = new[] { CardCatalog.IdPillRed, "S8" };
            transport.Push(granted);

            var played = EventMessage.Create(EvCode.CardPlayed, 2, 0);
            played.instanceId = 1;
            transport.Push(played);
            var drawn = EventMessage.Create(EvCode.CardDrawn, 3, 0);
            drawn.instanceIds = new[] { 99 };
            drawn.defIds = new[] { "H4" };
            transport.Push(drawn);

            Assert.That(client.HandInstanceIds, Is.EqualTo(new[] { 2, 99 }));
            Assert.That(client.HandDefIds, Is.EqualTo(new[] { "S8", "H4" }));
        }

        private static MatchRuntime StartMatch()
        {
            var runtime = new MatchRuntime(2, Seed);
            runtime.Submit(CommandMessage.Ready(0, 0), 0);
            runtime.Submit(CommandMessage.Ready(0, 1), 0);
            runtime.Submit(CommandMessage.StartMatch(0, 0), 0);
            return runtime;
        }

        private static int CountEv(IReadOnlyList<EventMessage> events, string ev)
        {
            var count = 0;
            for (var i = 0; i < events.Count; i++)
            {
                if (events[i] != null && events[i].ev == ev)
                {
                    count++;
                }
            }

            return count;
        }

        private static EventMessage FindEv(IReadOnlyList<EventMessage> events, string ev)
        {
            var index = IndexOfEv(events, ev);
            return index < 0 ? null : events[index];
        }

        private static int IndexOfEv(IReadOnlyList<EventMessage> events, string ev)
        {
            for (var i = 0; i < events.Count; i++)
            {
                if (events[i] != null && events[i].ev == ev)
                {
                    return i;
                }
            }

            return -1;
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

            throw new InvalidOperationException("Seat " + seat + " missing " + defId + ".");
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

        private sealed class FakeTransport : INetTransport
        {
            public bool IsConnected { get; private set; }

            public event Action<EventMessage> EventReceived;

            public void Connect()
            {
                IsConnected = true;
            }

            public void Disconnect()
            {
                IsConnected = false;
            }

            public void Send(CommandMessage command)
            {
            }

            public void Push(EventMessage ev)
            {
                EventReceived?.Invoke(ev);
            }
        }
    }
}
