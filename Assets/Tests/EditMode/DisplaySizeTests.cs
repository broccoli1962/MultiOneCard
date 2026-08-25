using Backend.App;
using NUnit.Framework;

namespace Game.Rules.Tests
{
    /// <summary>
    /// 해상도 목록 중복 제거·순환 인덱스.
    /// </summary>
    [TestFixture]
    [Category("EditMode")]
    public sealed class DisplaySizeTests
    {
        [Test]
        public void CollectUnique_DropsDuplicatesAndSorts()
        {
            var dest = new DisplaySize[8];
            var count = DisplaySizeUtil.CollectUnique(
                new[] { 1920, 1280, 1920, 800 },
                new[] { 1080, 720, 1080, 600 },
                dest);

            Assert.That(count, Is.EqualTo(3));
            Assert.That(dest[0].Width, Is.EqualTo(800));
            Assert.That(dest[0].Height, Is.EqualTo(600));
            Assert.That(dest[1].Label, Is.EqualTo("1280 x 720"));
            Assert.That(dest[2].Label, Is.EqualTo("1920 x 1080"));
        }

        [Test]
        public void CollectUnique_SkipsInvalidAndNull()
        {
            Assert.That(DisplaySizeUtil.CollectUnique(null, new[] { 1 }, new DisplaySize[1]), Is.EqualTo(0));

            var dest = new DisplaySize[2];
            var count = DisplaySizeUtil.CollectUnique(new[] { 0, 640 }, new[] { 480, 480 }, dest);
            Assert.That(count, Is.EqualTo(1));
            Assert.That(dest[0].Width, Is.EqualTo(640));
        }

        [Test]
        public void IndexOf_FindsOrMinusOne()
        {
            var sizes = new[] { new DisplaySize(800, 600), new DisplaySize(1920, 1080) };
            Assert.That(DisplaySizeUtil.IndexOf(sizes, 2, 1920, 1080), Is.EqualTo(1));
            Assert.That(DisplaySizeUtil.IndexOf(sizes, 2, 1024, 768), Is.EqualTo(-1));
        }

        [Test]
        public void WrapStep_Cycles()
        {
            Assert.That(DisplaySizeUtil.WrapStep(0, 3, -1), Is.EqualTo(2));
            Assert.That(DisplaySizeUtil.WrapStep(2, 3, 1), Is.EqualTo(0));
            Assert.That(DisplaySizeUtil.WrapStep(1, 0, 1), Is.EqualTo(0));
        }
    }
}
