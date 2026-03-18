using Terraria.ID;

namespace AbyssOverhaul.Core.Utilities
{

    public sealed class AbyssLayoutSettings
    {
        public int SideInset = 170;
        public int TrenchHalfWidth = 170;
        public int SideWallThickness = 28;

        public int TopYPadding = 0;
        public int BottomYPadding = 0;

        public int EntryRadius = 18;
        public int MidRadius = 28;
        public int BottomRadius = 42;

        public int Meander = 18;
        public int BaseFillTileType = TileID.Stone;
        public ushort BaseWallType = WallID.None;
    }

    public static class AbyssGenUtils
    {

        public static int MinX { get; private set; }
        public static int MaxX { get; private set; }
        public static int TopY { get; private set; }
        public static int BottomY { get; private set; }
        public static int ChasmX { get; private set; }
        public static bool OnLeft { get; private set; }
        public static bool Initialized { get; private set; }

        public static int AbyssWorldMinX { get; private set; }
        public static int AbyssWorldMaxX { get; private set; }

        public static int Width => MaxX - MinX;
        public static int Height => BottomY - TopY;

        public static void Initialize(Mod loggerMod, int extraWidth = 0)
        {
            int x = Main.maxTilesX;
            int y = Main.maxTilesY;

            OnLeft = Main.dungeonX < x / 2;

            ChasmX = OnLeft ? 170 : x - 170;

            int baseMinX = OnLeft ? 0 : ChasmX - 160;
            int baseMaxX = OnLeft ? ChasmX + 160 : x - 1;

            if (extraWidth > 0)
            {
                if (OnLeft)
                    baseMaxX += extraWidth;
                else
                    baseMinX -= extraWidth;
            }

            int rockLayer = (int)Main.rockLayer;
            int underworldTop = y - 200;

            SetBounds(
                Utils.Clamp(baseMinX, 0, x - 1),
                Utils.Clamp(baseMaxX, 0, x - 1),
                rockLayer,
                underworldTop,
                ChasmX,
                OnLeft,
                loggerMod
            );
        }

        public static void SetBounds(int minX, int maxX, int topY, int bottomY, int chasmX, bool onLeft, Mod loggerMod = null)
        {
            int x = Main.maxTilesX;
            int y = Main.maxTilesY;

            MinX = Utils.Clamp(minX, 0, x - 1);
            MaxX = Utils.Clamp(maxX, 0, x - 1);
            TopY = Utils.Clamp(topY, 0, y - 1);
            BottomY = Utils.Clamp(bottomY, 0, y - 1);
            ChasmX = Utils.Clamp(chasmX, 0, x - 1);
            OnLeft = onLeft;

            if (MaxX < MinX)
                (MinX, MaxX) = (MaxX, MinX);

            if (BottomY < TopY)
                (TopY, BottomY) = (BottomY, TopY);

            AbyssWorldMinX = MinX;
            AbyssWorldMaxX = MaxX;
            Initialized = true;

            loggerMod?.Logger.Info(
                $"[AbyssGenUtils.SetBounds] OnLeft={OnLeft} ChasmX={ChasmX} MinX={MinX} MaxX={MaxX} TopY={TopY} BottomY={BottomY}"
            );
        }

        public static int YAt(float t)
        {
            
            return (int)MathHelper.Lerp(TopY, BottomY, t);
        }

        public static float TAt(int y)
        {
            if (BottomY <= TopY)
                return 0f;

            return MathHelper.Clamp((y - TopY) / (float)(BottomY - TopY), 0f, 1f);
        }

        public static (int x, int y) RandomPoint(int padding = 0)
        {
            int rx = Terraria.WorldGen.genRand.Next(MinX + padding, MaxX - padding);
            int ry = Terraria.WorldGen.genRand.Next(TopY + padding, BottomY - padding);
            return (rx, ry);
        }

        public static bool InAbyss(int x, int y, int pad = 0)
        {
            return x >= MinX + pad && x <= MaxX - pad &&
                   y >= TopY + pad && y <= BottomY - pad;
        }

        public static bool InAbyssX(int x, int padding = 0)
        {
            return x >= MinX + padding && x <= MaxX - padding;
        }

        public static bool InAbyss(int x, int y, int paddingX = 0, int paddingY = 0)
        {
            return x >= MinX + paddingX &&
                   x <= MaxX - paddingX &&
                   y >= TopY + paddingY &&
                   y <= BottomY - paddingY;
        }
        public static void Reset()
        {
            MinX = 0;
            MaxX = 0;
            TopY = 0;
            BottomY = 0;
            ChasmX = 0;
            OnLeft = false;
            Initialized = false;
            AbyssWorldMinX = 0;
            AbyssWorldMaxX = 0;
        }

        public static bool IsWithinAbyss(int tileX, int tileY)
        {
            if (!Initialized)
                return false;

            return tileX >= MinX &&
                   tileX <= MaxX &&
                   tileY >= TopY &&
                   tileY <= BottomY;
        }
    }
}




