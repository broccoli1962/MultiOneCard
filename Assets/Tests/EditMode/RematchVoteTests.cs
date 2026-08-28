using System.Collections.Generic;
using Backend.App;
using Backend.Net;
using NUnit.Framework;

namespace Game.Rules.Tests
{
    /// <summary>
    /// 재대결 투표는 찬성 현황을 RoomUpdated 로 알리고,
    /// 한 명이라도 반대하면 Waiting 으로 되돌린다.
    /// </summary>
    [TestFixture]
    [Category("EditMode")]
    public sealed class RematchVoteTests
    {
        private const int Seed = 20260828;

        [Test]
        public void RematchVote_Yes_KeepsResultAndReportsCount()
        {
            var runtime = StartEndedMatch(2);
            var events = runtime.Submit(CommandMessage.RematchVote(0, 0, true), 1);

            Assert.That(CountEv(events, EvCode.MatchStarted), Is.EqualTo(0));
            var room = LastRoom(events);
            Assert.That(room, Is.Not.Null);
            Assert.That(room.phase, Is.EqualTo(MatchPhase.Result));
            Assert.That(room.rematchVoted, Is.EqualTo(new[] { true, false }));
            Assert.That(room.rematchYes, Is.EqualTo(new[] { true, false }));
            Assert.That(runtime.Phase, Is.EqualTo(MatchPhase.Result));
        }

        [Test]
        public void RematchVote_AnyNo_ReturnsToWaiting()
        {
            var runtime = StartEndedMatch(2);
            runtime.Submit(CommandMessage.RematchVote(0, 0, true), 1);
            var events = runtime.Submit(CommandMessage.RematchVote(0, 1, false), 2);

            Assert.That(runtime.Phase, Is.EqualTo(MatchPhase.Waiting));
            var room = LastRoom(events);
            Assert.That(room, Is.Not.Null);
            Assert.That(room.phase, Is.EqualTo(MatchPhase.Waiting));
            Assert.That(CountEv(events, EvCode.MatchStarted), Is.EqualTo(0));
        }

        [Test]
        public void RematchVote_NoWithoutOthers_ReturnsToWaitingImmediately()
        {
            var runtime = StartEndedMatch(3);
            var events = runtime.Submit(CommandMessage.RematchVote(0, 1, false), 1);

            Assert.That(runtime.Phase, Is.EqualTo(MatchPhase.Waiting));
            var room = LastRoom(events);
            Assert.That(room, Is.Not.Null);
            Assert.That(room.phase, Is.EqualTo(MatchPhase.Waiting));
        }

        [Test]
        public void RematchVote_AllYes_StartsMatch()
        {
            var runtime = StartEndedMatch(2);
            runtime.Submit(CommandMessage.RematchVote(0, 0, true), 1);
            var events = runtime.Submit(CommandMessage.RematchVote(0, 1, true), 2);

            Assert.That(CountEv(events, EvCode.MatchStarted), Is.EqualTo(1));
            Assert.That(runtime.Phase, Is.EqualTo(MatchPhase.InMatch));
        }

        [Test]
        public void RematchVote_DisconnectedSeatDoesNotBlockYes()
        {
            var runtime = StartEndedMatch(3);
            runtime.Disconnect(2, 2);
            runtime.Submit(CommandMessage.RematchVote(0, 0, true), 3);
            var events = runtime.Submit(CommandMessage.RematchVote(0, 1, true), 4);

            Assert.That(CountEv(events, EvCode.MatchStarted), Is.EqualTo(1));
            Assert.That(runtime.Phase, Is.EqualTo(MatchPhase.InMatch));
        }

        [Test]
        public void RematchVote_DeadlineWithoutVotes_ReturnsToWaiting()
        {
            var runtime = StartEndedMatch(2);
            var deadline = 1 + MatchRuntime.RematchSeconds * 1000L;
            var events = runtime.Pump(deadline);

            Assert.That(runtime.Phase, Is.EqualTo(MatchPhase.Waiting));
            var room = LastRoom(events);
            Assert.That(room, Is.Not.Null);
            Assert.That(room.phase, Is.EqualTo(MatchPhase.Waiting));
        }

        private static MatchRuntime StartEndedMatch(int seatCount)
        {
            var nicks = new string[seatCount];
            for (var i = 0; i < seatCount; i++)
            {
                nicks[i] = "P" + i;
            }

            var runtime = new MatchRuntime(seatCount, Seed, nicks: nicks);
            for (var seat = 0; seat < seatCount; seat++)
            {
                runtime.Submit(CommandMessage.Ready(0, seat), 0);
            }

            runtime.Submit(CommandMessage.StartMatch(0, 0), 0);
            for (var seat = 1; seat < seatCount; seat++)
            {
                runtime.Submit(CommandMessage.Surrender(0, seat), 1);
            }

            Assert.That(runtime.Phase, Is.EqualTo(MatchPhase.Result));
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

        private static RoomView LastRoom(IReadOnlyList<EventMessage> events)
        {
            RoomView last = null;
            for (var i = 0; i < events.Count; i++)
            {
                if (events[i] != null && events[i].room != null)
                {
                    last = events[i].room;
                }
            }

            return last;
        }
    }
}
