using System.Text;
using Game.Rules;

namespace Backend.App
{
    /// <summary>
    /// 대기실 규칙 패널용 하우스룰 요약.
    /// </summary>
    public static class HouseRulesText
    {
        /// <summary>현재 룰을 구역별로 나눈 본문.</summary>
        public static string Format(HouseRules rules)
        {
            if (rules == null)
            {
                rules = HouseRules.Official;
            }

            var sb = new StringBuilder(320);
            sb.AppendLine(rules.IsOfficial ? "공식 규칙" : "현재 규칙");
            sb.AppendLine();
            sb.AppendLine("인원");
            sb.Append("2~").Append(HouseRules.MaxSeats).AppendLine("명");
            sb.Append("2~4인 ").Append(HouseRules.HandSizeTwoToFour).Append("장");
            sb.Append("  ·  5~6인 ").Append(HouseRules.HandSizeFiveToSix).AppendLine("장");
            sb.AppendLine();
            sb.AppendLine("턴");
            sb.Append(rules.TurnSeconds).AppendLine("초");
            sb.AppendLine("한 턴 1장. K만 한 장 더 낼 수 있음.");
            sb.AppendLine();
            sb.AppendLine("내기");
            sb.AppendLine("같은 무늬 또는 같은 랭크.");
            sb.AppendLine("조커·무색 특수는 알약 제한이 없으면 어느 위에나 가능.");
            sb.AppendLine();
            sb.AppendLine("공격 · 방어");
            sb.AppendLine("2는 +2, A는 +3.");
            sb.AppendLine(rules.JokerDefendable
                ? "조커 공격은 같은 색 3·4로 막을 수 있음."
                : "조커 공격은 3·4로 막을 수 없음.");
            sb.AppendLine();
            sb.AppendLine("드로우");
            sb.AppendLine(rules.DrawAndPlay
                ? "뽑은 장을 같은 턴에 낼 수 있음."
                : "뽑은 장은 같은 턴에 내지 않음.");
            sb.AppendLine();
            sb.AppendLine("승리");
            sb.AppendLine("손패 0장이면 1위.");
            sb.Append(rules.ContinueAfterFirstWin
                ? "첫 1위 이후에도 잔여 순위전을 이어 감."
                : "첫 1위에서 판이 끝남.");
            return sb.ToString();
        }
    }
}
