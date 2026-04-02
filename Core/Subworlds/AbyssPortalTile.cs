using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.Enums;
using Terraria.Localization;
using Terraria.ObjectData;

namespace AbyssOverhaul.Core.Subworlds
{
    internal sealed class AbyssPortalTile : ModTile
    {
        private const int Width = 8;
        private const int Height = 8;

        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileNoAttach[Type] = true;
            Main.tileLavaDeath[Type] = false;
            Main.tileSolidTop[Type] = false;
            Main.tileTable[Type] = false;

            TileID.Sets.DisableSmartCursor[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
            TileObjectData.newTile.Width = Width;
            TileObjectData.newTile.Height = Height;
            TileObjectData.newTile.Origin = new Point16(Width / 2, Height - 1);
            TileObjectData.newTile.CoordinateWidth = 16;
            TileObjectData.newTile.CoordinatePadding = 2;
            TileObjectData.newTile.CoordinateHeights = new int[Height] { 16, 16, 16, 16, 16, 16, 16, 16 };
            TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop, Width, 0);
            TileObjectData.addTile(Type);

            LocalizedText name = CreateMapEntryName();
            AddMapEntry(new Color(20, 45, 85), name);
        }

        public override bool RightClick(int i, int j)
        {
            Player player = Main.LocalPlayer;
            if (!player.active || player.dead)
                return false;

            Tile tile = Framing.GetTileSafely(i, j);

            int left = i - (tile.TileFrameX / 18) % Width;
            int top = j - (tile.TileFrameY / 18) % Height;

            Rectangle portalWorldRect = new Rectangle(left * 16, top * 16, Width * 16, Height * 16);

            // Keep this if you want the player to actually be standing in the portal.
            if (!player.Hitbox.Intersects(portalWorldRect))
                return false;

            AbyssSubworldActions.TryEnter(player);
            return true;
        }

        public override void MouseOver(int i, int j)
        {
            Player player = Main.LocalPlayer;
            player.noThrow = 2;
            player.cursorItemIconEnabled = true;
            player.cursorItemIconID = ModContent.ItemType<AbyssPortalItem>();
        }

        public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
        {
            return base.PreDraw(i, j, spriteBatch);
        }
    }
}
