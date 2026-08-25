namespace Backend.App
{
    /// <summary>
    /// 가로·세로 픽셀. 주사율은 포함하지 않는다.
    /// </summary>
    public readonly struct DisplaySize
    {
        /// <summary>가로(px).</summary>
        public readonly int Width;

        /// <summary>세로(px).</summary>
        public readonly int Height;

        /// <summary>
        /// 가로·세로를 묶는다.
        /// </summary>
        public DisplaySize(int width, int height)
        {
            Width = width;
            Height = height;
        }

        /// <summary>설정 UI 표시용. 예: 1920 x 1080.</summary>
        public string Label => Width + " x " + Height;
    }

    /// <summary>
    /// 모니터 해상도 목록에서 중복을 빼고 정렬한다.
    /// </summary>
    public static class DisplaySizeUtil
    {
        /// <summary>
        /// widths[i]·heights[i] 짝을 dest에 유일·오름차순으로 쓴다. 쓴 개수를 반환한다.
        /// </summary>
        public static int CollectUnique(int[] widths, int[] heights, DisplaySize[] dest)
        {
            if (widths == null || heights == null || dest == null || dest.Length == 0)
            {
                return 0;
            }

            var pairCount = widths.Length < heights.Length ? widths.Length : heights.Length;
            var unique = new DisplaySize[pairCount];
            var uniqueCount = 0;

            for (var i = 0; i < pairCount; i++)
            {
                var width = widths[i];
                var height = heights[i];
                if (width < 1 || height < 1)
                {
                    continue;
                }

                if (Contains(unique, uniqueCount, width, height))
                {
                    continue;
                }

                unique[uniqueCount] = new DisplaySize(width, height);
                uniqueCount++;
            }

            SortBySize(unique, uniqueCount);

            var write = uniqueCount < dest.Length ? uniqueCount : dest.Length;
            for (var i = 0; i < write; i++)
            {
                dest[i] = unique[i];
            }

            return write;
        }

        /// <summary>
        /// sizes[0..count) 에서 같은 크기의 인덱스를 찾는다. 없으면 -1.
        /// </summary>
        public static int IndexOf(DisplaySize[] sizes, int count, int width, int height)
        {
            if (sizes == null || count < 1)
            {
                return -1;
            }

            if (count > sizes.Length)
            {
                count = sizes.Length;
            }

            for (var i = 0; i < count; i++)
            {
                if (sizes[i].Width == width && sizes[i].Height == height)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// 목록을 순환하며 index + delta. count가 0이면 0.
        /// </summary>
        public static int WrapStep(int index, int count, int delta)
        {
            if (count <= 0)
            {
                return 0;
            }

            var next = index % count;
            if (next < 0)
            {
                next += count;
            }

            next += delta % count;
            next %= count;
            if (next < 0)
            {
                next += count;
            }

            return next;
        }

        private static bool Contains(DisplaySize[] sizes, int count, int width, int height)
        {
            for (var i = 0; i < count; i++)
            {
                if (sizes[i].Width == width && sizes[i].Height == height)
                {
                    return true;
                }
            }

            return false;
        }

        private static void SortBySize(DisplaySize[] sizes, int count)
        {
            for (var i = 0; i < count - 1; i++)
            {
                var min = i;
                for (var j = i + 1; j < count; j++)
                {
                    if (Compare(sizes[j], sizes[min]) < 0)
                    {
                        min = j;
                    }
                }

                if (min == i)
                {
                    continue;
                }

                var swap = sizes[i];
                sizes[i] = sizes[min];
                sizes[min] = swap;
            }
        }

        private static int Compare(DisplaySize a, DisplaySize b)
        {
            if (a.Width != b.Width)
            {
                return a.Width < b.Width ? -1 : 1;
            }

            if (a.Height == b.Height)
            {
                return 0;
            }

            return a.Height < b.Height ? -1 : 1;
        }
    }
}
