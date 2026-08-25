using Backend.App;
using NUnit.Framework;

namespace Game.Rules.Tests
{
    /// <summary>
    /// 방 인원 하드 상한이 릴레이 할당 한도와 맞는지만 본다.
    /// </summary>
    [TestFixture]
    [Category("EditMode")]
    public sealed class SessionLimitsTests
    {
        [Test]
        public void ClampPlayers_NeverExceedsMax()
        {
            Assert.That(SessionLimits.MaxPlayers, Is.EqualTo(HouseRules.MaxSeats));
            Assert.That(SessionLimits.MaxPlayers, Is.EqualTo(6));
            Assert.That(SessionLimits.MaxRelayJoins, Is.EqualTo(5));
            Assert.That(SessionLimits.MaxHostedSessions, Is.EqualTo(1));
            Assert.That(SessionLimits.ClampPlayers(99), Is.EqualTo(6));
            Assert.That(SessionLimits.ClampPlayers(1), Is.EqualTo(2));
            Assert.That(SessionLimits.ClampPlayers(4), Is.EqualTo(4));
        }
    }
}
