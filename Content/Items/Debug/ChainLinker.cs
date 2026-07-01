using AbyssOverhaul.Content.Layers.FossilShale.Systems;
using AbyssOverhaul.Content.Rarities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Content.Items.Debug
{
    public sealed class ChainLinker : ModItem
    {
        public const float DefaultPixelsPerSegment = 10f;
        public const float DefaultGravity = 0.3f;
        public const float DefaultDamping = 0.99f;
        public const int DefaultSimulateIterations = 4;
        public const int DefaultAnchorIterations = 4;
        public const bool DefaultCollideWithTiles = true;
        public const float DefaultCollisionRadius = 3f;
        public const float DefaultThickness = 2f;

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.useTime = 12;
            Item.useAnimation = 12;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.UseSound = SoundID.Grab;
            Item.rare = ModContent.RarityType<PrimordialYellow>(); //test the primordial yellow on a no tooltip item
            Item.value = Item.buyPrice(silver: 50);
            Item.noMelee = true;
            Item.useTurn = true;
            Item.autoReuse = false;
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI != Main.myPlayer)
                return true;

            ChainLinkerPlayer modPlayer = player.GetModPlayer<ChainLinkerPlayer>();

            if (player.altFunctionUse == 2)
            {
                modPlayer.ClearPendingAnchor();
                Main.NewText("Cleared pending chain anchor.");
                return true;
            }

            Point mouseTilePoint = Main.MouseWorld.ToTileCoordinates();
            Point16 clickedTile = new Point16(mouseTilePoint.X, mouseTilePoint.Y);

            if (!TileToTileChainSystem.IsValidAnchorTile(clickedTile))
            {
                Main.NewText("That tile is not a valid anchor.");
                return true;
            }

            if (!modPlayer.HasPendingAnchor)
            {
                modPlayer.HasPendingAnchor = true;
                modPlayer.PendingAnchor = clickedTile;
                Main.NewText($"Stored first anchor: {clickedTile.X}, {clickedTile.Y}");
                return true;
            }

            Point16 firstAnchor = modPlayer.PendingAnchor;

            if (firstAnchor == clickedTile)
            {
                Main.NewText("Pick a different second tile.");
                return true;
            }

            modPlayer.ClearPendingAnchor();

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                ModPacket packet = Mod.GetPacket();
                packet.Write((byte)AbyssOverhaulMessageType.CreateChain);
                packet.Write((short)firstAnchor.X);
                packet.Write((short)firstAnchor.Y);
                packet.Write((short)clickedTile.X);
                packet.Write((short)clickedTile.Y);
                packet.Send();
            }
            else
            {
                TileToTileChainSystem.AddChain(
                    firstAnchor,
                    clickedTile,
                    DefaultPixelsPerSegment,
                    DefaultGravity,
                    DefaultDamping,
                    DefaultSimulateIterations,
                    DefaultAnchorIterations,
                    DefaultCollideWithTiles,
                    DefaultCollisionRadius,
                    Vector2.Zero,
                    Vector2.Zero,
                    DefaultThickness);
            }

            Main.NewText($"Created chain from {firstAnchor.X}, {firstAnchor.Y} to {clickedTile.X}, {clickedTile.Y}");
            return true;
        }
    }
}
