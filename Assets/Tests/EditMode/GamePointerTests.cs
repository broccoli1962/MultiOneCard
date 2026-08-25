using Backend.App;
using NUnit.Framework;

namespace Game.Rules.Tests
{
    /// <summary>
    /// 기획서 §8 GamePointer. 내기·지급·숨김은 드래그, Enter 는 내기만.
    /// </summary>
    [TestFixture]
    [Category("EditMode")]
    public sealed class GamePointerTests
    {
        [Test]
        public void TapCard_None_SelectsWithoutPlaying()
        {
            var pointer = new GamePointer();
            var played = 0;
            pointer.PlayCardRequested += _ => played++;

            pointer.TapCard(7);
            pointer.TapCard(7);

            Assert.That(played, Is.EqualTo(0));
            Assert.That(pointer.SelectedInstanceId, Is.EqualTo(-1));
        }

        [Test]
        public void Confirm_AfterSelect_IssuesPlayCard()
        {
            var pointer = new GamePointer();
            var played = 0;
            pointer.PlayCardRequested += id => played = id;

            pointer.TapCard(4);
            pointer.Confirm();

            Assert.That(played, Is.EqualTo(4));
            Assert.That(pointer.HasSelection, Is.False);
        }

        [Test]
        public void RequestPlay_IssuesPlayCard()
        {
            var pointer = new GamePointer();
            var played = 0;
            pointer.PlayCardRequested += id => played = id;

            pointer.RequestPlay(9);

            Assert.That(played, Is.EqualTo(9));
        }

        [Test]
        public void SelectCard_DoesNotToggleOrPlay()
        {
            var pointer = new GamePointer();
            var played = 0;
            pointer.PlayCardRequested += _ => played++;

            pointer.SelectCard(3);
            pointer.SelectCard(3);

            Assert.That(played, Is.EqualTo(0));
            Assert.That(pointer.SelectedInstanceId, Is.EqualTo(3));
        }

        [Test]
        public void TapCard_Give_PreviewsWithoutGiving()
        {
            var pointer = new GamePointer();
            pointer.SetSheet(GamePointerSheet.GiveCards);
            var given = 0;
            pointer.GiveCardsRequested += _ => given++;

            pointer.TapCard(1);
            pointer.TapCard(2);

            Assert.That(given, Is.EqualTo(0));
            Assert.That(pointer.SelectedInstanceId, Is.EqualTo(2));
            Assert.That(pointer.MultiSelectedIds, Has.Count.EqualTo(0));
        }

        [Test]
        public void RequestGive_IssuesGiveCards()
        {
            var pointer = new GamePointer();
            pointer.SetSheet(GamePointerSheet.GiveCards);
            var given = 0;
            pointer.GiveCardsRequested += ids => given = ids[0];

            pointer.RequestGive(12);

            Assert.That(given, Is.EqualTo(12));
            Assert.That(pointer.MultiSelectedIds, Has.Count.EqualTo(0));
        }

        [Test]
        public void RequestGive_LimitTwo_IssuesWhenSecondDropped()
        {
            var pointer = new GamePointer();
            pointer.SetSheet(GamePointerSheet.GiveCards);
            pointer.SetMultiLimit(2);
            int[] given = null;
            pointer.GiveCardsRequested += ids => given = ids;

            pointer.RequestGive(1);

            Assert.That(given, Is.Null);
            Assert.That(pointer.MultiSelectedIds, Has.Count.EqualTo(1));

            pointer.RequestGive(2);

            Assert.That(given, Is.EquivalentTo(new[] { 1, 2 }));
            Assert.That(pointer.MultiSelectedIds, Has.Count.EqualTo(0));
        }

        [Test]
        public void Confirm_Give_DoesNotIssue()
        {
            var pointer = new GamePointer();
            pointer.SetSheet(GamePointerSheet.GiveCards);
            pointer.SetMultiLimit(1);
            var issued = 0;
            pointer.GiveCardsRequested += _ => issued++;

            pointer.TapCard(4);
            pointer.Confirm();

            Assert.That(issued, Is.EqualTo(0));
        }

        [Test]
        public void RequestHide_IssuesHideUnder()
        {
            var pointer = new GamePointer();
            pointer.SetSheet(GamePointerSheet.HideUnder);
            var hidden = 0;
            pointer.HideUnderRequested += id => hidden = id;

            pointer.RequestHide(8);

            Assert.That(hidden, Is.EqualTo(8));
        }

        [Test]
        public void Confirm_Hide_DoesNotIssue()
        {
            var pointer = new GamePointer();
            pointer.SetSheet(GamePointerSheet.HideUnder);
            var hidden = 0;
            pointer.HideUnderRequested += _ => hidden++;

            pointer.TapCard(8);
            pointer.Confirm();

            Assert.That(hidden, Is.EqualTo(0));
        }

        [Test]
        public void RequestMirror_LimitTwo_IssuesWhenSecondDropped()
        {
            var pointer = new GamePointer();
            pointer.SetSheet(GamePointerSheet.MirrorDiscard);
            pointer.SetMultiLimit(2);
            int[] discarded = null;
            pointer.MirrorDiscardRequested += ids => discarded = ids;

            pointer.RequestMirror(3);
            Assert.That(discarded, Is.Null);

            pointer.RequestMirror(5);
            Assert.That(discarded, Is.EquivalentTo(new[] { 3, 5 }));
        }
    }
}
