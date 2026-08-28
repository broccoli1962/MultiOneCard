using Backend.App;
using NUnit.Framework;

namespace Game.Rules.Tests
{
    /// <summary>
    /// 대기실 규칙 문구가 Official 값을 쓰는지 본다.
    /// </summary>
    [TestFixture]
    [Category("EditMode")]
    public sealed class HouseRulesTests
    {
        [Test]
        public void Format_Official_ListsCoreLines()
        {
            var text = HouseRulesText.Format(HouseRules.Official);
            Assert.That(text, Does.Contain("공식 규칙"));
            Assert.That(text, Does.Contain("2~6명"));
            Assert.That(text, Does.Contain("15초"));
            Assert.That(text, Does.Contain("뽑은 장은 같은 턴에 내지 않음"));
            Assert.That(text, Does.Contain("조커 공격은 같은 색 3·4로 막을 수 있음"));
            Assert.That(text, Does.Contain("첫 1위에서 판이 끝남"));
        }
    }
}
