namespace Backend.App
{
    /// <summary>
    /// 스크린 Safe Area 인셋. Unity <c>Screen.safeArea</c> 픽셀을 받아 정규화 앵커를 낸다.
    /// 손패는 하단 Safe Area 안에 둔다.
    /// </summary>
    public sealed class SafeAreaFitter
    {
        /// <summary>스크린 가로(px).</summary>
        public float ScreenWidth { get; }

        /// <summary>스크린 세로(px).</summary>
        public float ScreenHeight { get; }

        /// <summary>왼쪽 인셋(px).</summary>
        public float Left { get; }

        /// <summary>오른쪽 인셋(px).</summary>
        public float Right { get; }

        /// <summary>아래 인셋(px).</summary>
        public float Bottom { get; }

        /// <summary>위 인셋(px).</summary>
        public float Top { get; }

        /// <summary>Safe Area 왼쪽 정규화 앵커.</summary>
        public float AnchorMinX => Left / ScreenWidth;

        /// <summary>Safe Area 아래 정규화 앵커.</summary>
        public float AnchorMinY => Bottom / ScreenHeight;

        /// <summary>Safe Area 오른쪽 정규화 앵커.</summary>
        public float AnchorMaxX => 1f - Right / ScreenWidth;

        /// <summary>Safe Area 위 정규화 앵커.</summary>
        public float AnchorMaxY => 1f - Top / ScreenHeight;

        /// <summary>
        /// 스크린과 Safe Area 사각형(원점 좌하)으로 인셋을 계산한다.
        /// </summary>
        public SafeAreaFitter(
            float screenWidth,
            float screenHeight,
            float safeX,
            float safeY,
            float safeWidth,
            float safeHeight)
        {
            if (screenWidth < 1f)
            {
                screenWidth = 1f;
            }

            if (screenHeight < 1f)
            {
                screenHeight = 1f;
            }

            if (safeWidth < 0f)
            {
                safeWidth = 0f;
            }

            if (safeHeight < 0f)
            {
                safeHeight = 0f;
            }

            if (safeX < 0f)
            {
                safeX = 0f;
            }

            if (safeY < 0f)
            {
                safeY = 0f;
            }

            if (safeX + safeWidth > screenWidth)
            {
                safeWidth = screenWidth - safeX;
            }

            if (safeY + safeHeight > screenHeight)
            {
                safeHeight = screenHeight - safeY;
            }

            if (safeWidth < 1f)
            {
                safeX = 0f;
                safeWidth = screenWidth;
            }

            if (safeHeight < 1f)
            {
                safeY = 0f;
                safeHeight = screenHeight;
            }

            ScreenWidth = screenWidth;
            ScreenHeight = screenHeight;
            Left = safeX;
            Bottom = safeY;
            Right = screenWidth - (safeX + safeWidth);
            Top = screenHeight - (safeY + safeHeight);
            if (Right < 0f)
            {
                Right = 0f;
            }

            if (Top < 0f)
            {
                Top = 0f;
            }
        }

        /// <summary>
        /// 인셋 없는 전체 화면.
        /// </summary>
        public static SafeAreaFitter FullScreen(float width, float height)
        {
            return new SafeAreaFitter(width, height, 0f, 0f, width, height);
        }

        /// <summary>
        /// Safe Area 안 정규화 좌표를 스크린 앵커로 바꾼다.
        /// </summary>
        public void MapPoint(float nx, float ny, out float anchorX, out float anchorY)
        {
            if (nx < 0f)
            {
                nx = 0f;
            }
            else if (nx > 1f)
            {
                nx = 1f;
            }

            if (ny < 0f)
            {
                ny = 0f;
            }
            else if (ny > 1f)
            {
                ny = 1f;
            }

            anchorX = AnchorMinX + (AnchorMaxX - AnchorMinX) * nx;
            anchorY = AnchorMinY + (AnchorMaxY - AnchorMinY) * ny;
        }

        /// <summary>
        /// 하단 손패 스트립. min/max Y 는 Safe Area 하단(높이 0). 높이는 sizeDelta.y 로 준다.
        /// </summary>
        public void GetHandAnchors(out float minX, out float minY, out float maxX, out float maxY)
        {
            minX = AnchorMinX;
            maxX = AnchorMaxX;
            minY = AnchorMinY;
            maxY = AnchorMinY;
        }

        /// <summary>
        /// 손패 스트립이 Safe Area 안에 있는지. bottom 인셋 위, 좌우 안쪽.
        /// </summary>
        public bool ContainsHandStrip()
        {
            return AnchorMinY >= 0f && AnchorMinX >= 0f && AnchorMaxX <= 1f && AnchorMinX < AnchorMaxX;
        }
    }
}
