using System;

namespace Backend.App
{
    /// <summary>
    /// 기획서 §8 레이아웃 프리셋. OS가 아니라 해상도로 고른다.
    /// </summary>
    public enum LayoutPreset
    {
        /// <summary>세로. 레퍼런스 1080×1920.</summary>
        MobilePortrait = 0,

        /// <summary>가로이지만 PC 레퍼런스(1920×1080)보다 작거나 더 넓은 화면.</summary>
        MobileLandscape = 1,

        /// <summary>가로. 레퍼런스 1920×1080 이상·16:9대.</summary>
        PcLandscape = 2,
    }

    /// <summary>
    /// 상대 좌석 정규화 좌표. 로컬(하단 손패)은 포함하지 않는다.
    /// </summary>
    public readonly struct SeatAnchor
    {
        /// <summary>절대 좌석 번호.</summary>
        public readonly int Seat;

        /// <summary>Safe Area 안 X (0..1).</summary>
        public readonly float Nx;

        /// <summary>Safe Area 안 Y (0..1).</summary>
        public readonly float Ny;

        /// <summary>
        /// 좌석과 정규화 좌표를 묶는다.
        /// </summary>
        public SeatAnchor(int seat, float nx, float ny)
        {
            Seat = seat;
            Nx = nx;
            Ny = ny;
        }
    }

    /// <summary>
    /// 해상도 → <see cref="LayoutPreset"/>, 손패 높이, 2~4인 십자 / 5~6인 상단 아크.
    /// </summary>
    public static class LayoutPresetUtil
    {
        /// <summary>MobilePortrait 레퍼런스 가로.</summary>
        public const int MobilePortraitWidth = 1080;

        /// <summary>MobilePortrait 레퍼런스 세로.</summary>
        public const int MobilePortraitHeight = 1920;

        /// <summary>PcLandscape 레퍼런스 가로.</summary>
        public const int PcLandscapeWidth = 1920;

        /// <summary>PcLandscape 레퍼런스 세로.</summary>
        public const int PcLandscapeHeight = 1080;

        /// <summary>이 이상 가로비는 폰 와이드 → MobileLandscape.</summary>
        public const float WideLandscapeAspect = 1.9f;

        /// <summary>2~4인이면 십자 배치.</summary>
        public static bool UsesCross(int seatCount)
        {
            return seatCount <= 4;
        }

        /// <summary>5~6인이면 상단 아크 배치.</summary>
        public static bool UsesTopArc(int seatCount)
        {
            return seatCount >= 5;
        }

        /// <summary>
        /// 픽셀 해상도만으로 프리셋을 고른다. 플랫폼/OS 인자는 없다.
        /// </summary>
        public static LayoutPreset Resolve(int width, int height)
        {
            if (width < 1)
            {
                width = MobilePortraitWidth;
            }

            if (height < 1)
            {
                height = MobilePortraitHeight;
            }

            if (height > width)
            {
                return LayoutPreset.MobilePortrait;
            }

            var aspect = width / (float)height;
            if (aspect >= WideLandscapeAspect)
            {
                return LayoutPreset.MobileLandscape;
            }

            if (width >= PcLandscapeWidth && height >= PcLandscapeHeight)
            {
                return LayoutPreset.PcLandscape;
            }

            return LayoutPreset.MobileLandscape;
        }

        /// <summary>
        /// 프리셋 레퍼런스 해상도. 랜드는 둘 다 1920×1080.
        /// </summary>
        public static void GetReference(LayoutPreset preset, out int width, out int height)
        {
            if (preset == LayoutPreset.MobilePortrait)
            {
                width = MobilePortraitWidth;
                height = MobilePortraitHeight;
                return;
            }

            width = PcLandscapeWidth;
            height = PcLandscapeHeight;
        }

        /// <summary>
        /// 하단 손패 스트립 높이(캔버스 유닛).
        /// </summary>
        public static float HandHeight(LayoutPreset preset)
        {
            return preset == LayoutPreset.MobilePortrait ? 360f : 324f;
        }

        /// <summary>
        /// 상대 카드 크기(캔버스 유닛).
        /// </summary>
        public static void OpponentCardSize(LayoutPreset preset, out float width, out float height)
        {
            if (preset == LayoutPreset.MobilePortrait)
            {
                width = 108f;
                height = 151f;
                return;
            }

            width = 96f;
            height = 134f;
        }

        /// <summary>
        /// 테이블 중앙(버림) Safe Area 정규화 Y.
        /// </summary>
        public static float DiscardNormalizedY(LayoutPreset preset)
        {
            return preset == LayoutPreset.MobilePortrait ? 0.50f : 0.48f;
        }

        /// <summary>
        /// 상대 좌석을 dest 에 쓴다. 로컬은 제외. 쓴 개수를 반환한다.
        /// 2~4인 십자, 5~6인 상단 아크. 다음 좌석(+1)부터 반시계.
        /// </summary>
        public static int PlaceOpponents(int seatCount, int localSeat, SeatAnchor[] dest)
        {
            if (dest == null || dest.Length == 0)
            {
                return 0;
            }

            if (seatCount < 2)
            {
                seatCount = 2;
            }

            if (seatCount > 6)
            {
                seatCount = 6;
            }

            if (localSeat < 0 || localSeat >= seatCount)
            {
                localSeat = 0;
            }

            var count = seatCount - 1;
            if (count > dest.Length)
            {
                count = dest.Length;
            }

            if (UsesCross(seatCount))
            {
                PlaceCross(seatCount, localSeat, dest, count);
            }
            else
            {
                PlaceTopArc(seatCount, localSeat, dest, count);
            }

            return count;
        }

        private static void PlaceCross(int seatCount, int localSeat, SeatAnchor[] dest, int count)
        {
            for (var i = 0; i < count; i++)
            {
                CrossPoint(seatCount, i, out var nx, out var ny);
                dest[i] = new SeatAnchor((localSeat + 1 + i) % seatCount, nx, ny);
            }
        }

        private static void CrossPoint(int seatCount, int opponentIndex, out float nx, out float ny)
        {
            if (seatCount <= 2)
            {
                nx = 0.5f;
                ny = 0.78f;
                return;
            }

            if (seatCount == 3)
            {
                nx = opponentIndex == 0 ? 0.88f : 0.12f;
                ny = 0.52f;
                return;
            }

            if (opponentIndex == 0)
            {
                nx = 0.88f;
                ny = 0.52f;
                return;
            }

            if (opponentIndex == 1)
            {
                nx = 0.5f;
                ny = 0.78f;
                return;
            }

            nx = 0.12f;
            ny = 0.52f;
        }

        private static void PlaceTopArc(int seatCount, int localSeat, SeatAnchor[] dest, int count)
        {
            for (var i = 0; i < count; i++)
            {
                var t = count == 1 ? 0.5f : 1f - i / (float)(count - 1);
                var nx = 0.12f + 0.76f * t;
                var ny = 0.78f + 0.12f * (float)Math.Sin(Math.PI * t);
                dest[i] = new SeatAnchor((localSeat + 1 + i) % seatCount, nx, ny);
            }
        }
    }
}
