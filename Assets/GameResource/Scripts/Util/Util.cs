using System;

namespace Util
{
    public static class Util
    {
        public static double GetUnitValue(this string number)
        {
            if (string.IsNullOrEmpty(number))
                return 0d;

            // 숫자 부분과 단위 부분 분리
            string numericPart = "";
            string unitPart = "";

            for (int i = 0; i < number.Length; i++)
            {
                char c = number[i];
                if (char.IsDigit(c) || c == '.' || c == '-')
                {
                    numericPart += c;
                }
                else
                {
                    unitPart = number.Substring(i);
                    break;
                }
            }

            // 숫자 부분 파싱
            if (!double.TryParse(numericPart, out double value))
                return 0d;

            // 단위가 없으면 그대로 반환
            if (string.IsNullOrEmpty(unitPart))
                return value;

            // 단위 인덱스 계산
            int unitIndex = GetUnitIndex(unitPart);

            // 1000^unitIndex 곱하기
            return value * Math.Pow(1000, unitIndex);
        }

        public static string FormatWithUnits(this float number)
        {
            double dNumber = number;
            return FormatWithUnits(dNumber);
        }

        public static string FormatWithUnits(this int number)
        {
            double dNumber = number;
            return FormatWithUnits(dNumber);
        }

        public static string FormatWithUnits(this long number)
        {
            double dNumber = number;
            return FormatWithUnits(dNumber);
        }

        public static string FormatWithUnits(this double number)
        {
            if (number < 1000)
                return number.ToString("0");

            int unitIndex = 0;
            double value = number;

            while (value >= 1000)
            {
                value /= 1000;
                unitIndex++;
            }

            // 앞 세 자리를 만드는 형식 지정
            string formatted;
            if (value >= 100)
            {
                formatted = value.ToString("0");
            }
            else if (value >= 10)
            {
                formatted = value.ToString("0.#");
            }
            else
            {
                formatted = value.ToString("0.##");
            }

            return $"{formatted}{GetUnitLabel(unitIndex - 1)}";
        }

        // 0 -> a, 1 -> b, ..., 25 -> z, 26 -> aa, 27 -> ab, ...
        private static string GetUnitLabel(int index)
        {
            if (index < 0) return "";

            string label = "";
            index++; // 1-based index for calculation

            while (index > 0)
            {
                index--;
                label = (char)('a' + (index % 26)) + label;
                index /= 26;
            }

            return label;
        }

        private static int GetUnitIndex(string unitLabel)
        {
            if (string.IsNullOrEmpty(unitLabel))
                return 0;

            int index = 0;
            for (int i = 0; i < unitLabel.Length; i++)
            {
                char c = unitLabel[i];
                if (c >= 'a' && c <= 'z')
                {
                    index = index * 26 + (c - 'a' + 1);
                }
            }
            return index;
        }

        public static bool ProbabilitySimulate_Percent(this float percent)
        {
            return UnityEngine.Random.value * 100f < percent;
        }

        public static string FormatTime(this TimeSpan time)
        {
            if (time.TotalSeconds > long.MaxValue)
            {
                throw new Exception("Time is too long");
            }
            else
            {
                long.TryParse(time.TotalSeconds.ToString(), out long seconds);
                return seconds.FormatTime();
            }
        }

        public static string FormatTime(this int time)
        {
            return FormatTime((long)time);
        }

        public static string FormatTime(this long time)
        {
            if (time <= 0)
                return "0s";

            var day = time / 86400;
            var hour = time % 86400 / 3600;
            var minute = time % 3600 / 60;
            var second = time % 60;

            if (day > 0)
                return $"{day}d {(hour == 0 ? "" : $"{hour}h")}";
            if (hour > 0)
                return $"{hour}h {(minute == 0 ? "" : $"{minute}m")}";
            if (minute > 0)
                return $"{minute}m {(second == 0 ? "" : $"{second}s")}";

            return $"{second}s";
        }
    }
}
