using Backend.App;
using NUnit.Framework;

namespace Game.Rules.Tests
{
    /// <summary>
    /// 기획서 §8 해상도 프리셋·Safe Area. OS 인자는 쓰지 않는다.
    /// </summary>
    [TestFixture]
    [Category("EditMode")]
    public sealed class LayoutPresetTests
    {
        [Test]
        public void Resolve_Portrait1080x1920_IsMobilePortrait()
        {
            Assert.That(LayoutPresetUtil.Resolve(1080, 1920), Is.EqualTo(LayoutPreset.MobilePortrait));
        }

        [Test]
        public void Resolve_Landscape1920x1080_IsPcLandscape()
        {
            Assert.That(LayoutPresetUtil.Resolve(1920, 1080), Is.EqualTo(LayoutPreset.PcLandscape));
        }

        [Test]
        public void Resolve_WidePhoneLandscape_IsMobileLandscape()
        {
            Assert.That(LayoutPresetUtil.Resolve(2400, 1080), Is.EqualTo(LayoutPreset.MobileLandscape));
        }

        [Test]
        public void Resolve_SmallLandscape_IsMobileLandscape()
        {
            Assert.That(LayoutPresetUtil.Resolve(1280, 720), Is.EqualTo(LayoutPreset.MobileLandscape));
        }

        [Test]
        public void Resolve_IgnoresOrientationName_UsesPixelsOnly()
        {
            Assert.That(LayoutPresetUtil.Resolve(1080, 1920), Is.Not.EqualTo(LayoutPreset.PcLandscape));
            Assert.That(LayoutPresetUtil.Resolve(1920, 1080), Is.Not.EqualTo(LayoutPreset.MobilePortrait));
        }

        [Test]
        public void PlaceOpponents_TwoToFour_UsesCross()
        {
            Assert.That(LayoutPresetUtil.UsesCross(2), Is.True);
            Assert.That(LayoutPresetUtil.UsesCross(4), Is.True);
            Assert.That(LayoutPresetUtil.UsesTopArc(4), Is.False);

            var dest = new SeatAnchor[5];
            var n = LayoutPresetUtil.PlaceOpponents(4, 0, dest);
            Assert.That(n, Is.EqualTo(3));
            Assert.That(dest[0].Seat, Is.EqualTo(1));
            Assert.That(dest[1].Seat, Is.EqualTo(2));
            Assert.That(dest[2].Seat, Is.EqualTo(3));
            Assert.That(dest[1].Ny, Is.GreaterThan(0.75f));
        }

        [Test]
        public void PlaceOpponents_FiveToSix_UsesTopArc()
        {
            Assert.That(LayoutPresetUtil.UsesTopArc(5), Is.True);
            Assert.That(LayoutPresetUtil.UsesTopArc(6), Is.True);
            Assert.That(LayoutPresetUtil.UsesCross(6), Is.False);

            var dest = new SeatAnchor[5];
            var n = LayoutPresetUtil.PlaceOpponents(6, 0, dest);
            Assert.That(n, Is.EqualTo(5));
            for (var i = 0; i < n; i++)
            {
                Assert.That(dest[i].Ny, Is.GreaterThanOrEqualTo(0.78f));
            }
        }

        [Test]
        public void SafeAreaFitter_HandStrip_StaysInsideBottomSafeArea()
        {
            var fitter = new SafeAreaFitter(1080f, 1920f, 0f, 68f, 1080f, 1784f);
            Assert.That(fitter.Bottom, Is.EqualTo(68f));
            Assert.That(fitter.Top, Is.EqualTo(68f));
            Assert.That(fitter.ContainsHandStrip(), Is.True);

            fitter.GetHandAnchors(out var minX, out var minY, out var maxX, out var maxY);
            Assert.That(minY, Is.EqualTo(fitter.AnchorMinY));
            Assert.That(maxY, Is.EqualTo(fitter.AnchorMinY));
            Assert.That(minX, Is.EqualTo(fitter.AnchorMinX));
            Assert.That(maxX, Is.EqualTo(fitter.AnchorMaxX));
            Assert.That(minY, Is.GreaterThan(0f));
        }

        [Test]
        public void SafeAreaFitter_FullScreen_HasZeroInsets()
        {
            var fitter = SafeAreaFitter.FullScreen(1920f, 1080f);
            Assert.That(fitter.Left, Is.EqualTo(0f));
            Assert.That(fitter.Right, Is.EqualTo(0f));
            Assert.That(fitter.Bottom, Is.EqualTo(0f));
            Assert.That(fitter.Top, Is.EqualTo(0f));
        }
    }
}
