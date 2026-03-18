using CalamityMod;
using CalamityMod.Tiles.Abyss;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.DataStructures;
using Terraria.GameContent.Metadata;
using Terraria.ID;

namespace AbyssOverhaul.Content.Layers.FossilShale.Tiles
{
    // This is an example of a vine tile. Vine tiles are fairly straightforward, but vines randomly growing and properly converting to other tiles are a bit more complicated. The ExampleVineGlobalTile class below contains the vine growing and converting code and is necessary for a fully working vine.
    public class Cyanobacteria_Vines : ModTile
    {
        public override void SetStaticDefaults()
        {
            Main.tileLighted[Type] = true;
            Main.tileCut[Type] = true;
            Main.tileBlockLight[Type] = true;
            Main.tileLavaDeath[Type] = true;
            Main.tileNoFail[Type] = true;
            Main.tileNoSunLight[Type] = false;
            AddMapEntry(new Color(0, 50, 0));
            HitSound = SoundID.Grass;
            DustType = DustID.Grass;
            TileID.Sets.IsVine[Type] = true;
            TileID.Sets.ReplaceTileBreakDown[Type] = true;
            TileID.Sets.VineThreads[Type] = true;
            TileID.Sets.DrawFlipMode[Type] = 1;
            TileMaterials.SetForTileId(Type, TileMaterials._materialsByName["Plant"]);
        }

        public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
        {
            Main.instance.TilesRenderer.CrawlToTopOfVineAndAddSpecialPoint(j, i);
            return false;
        }

        public override void NumDust(int i, int j, bool fail, ref int num)
        {
            num = fail ? 1 : 3;
        }

        public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak)
        {
            Tile tileAbove = Framing.GetTileSafely(i, j - 1);

            if (!tileAbove.HasTile)
            {
                Terraria.WorldGen.KillTile(i, j);
                return true;
            }

            return true;
        }

        public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
        {
            // GIVE VINE ROPE IF SPECIAL VINE BOOK
            if (Terraria.WorldGen.genRand.NextBool(2) && Main.player[(int)Player.FindClosest(new Vector2((float)(i * 16), (float)(j * 16)), 16, 16)].cordage)
                Item.NewItem(new EntitySource_TileBreak(i, j), new Vector2(i * 16 + 8f, j * 16 + 8f), ItemID.VineRope);

            if (Main.tile[i, j + 1] != null)
            {
                if (Main.tile[i, j + 1].HasTile)
                {
                    if (Main.tile[i, j + 1].TileType == ModContent.TileType<Cyanobacteria_Vines>())
                    {
                        Terraria.WorldGen.KillTile(i, j + 1, false, false, false);
                        if (!Main.tile[i, j + 1].HasTile && Main.netMode != NetmodeID.SinglePlayer)
                            NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 0, (float)i, (float)j + 1, 0f, 0, 0, 0);
                    }
                }
            }
        }

        public override void NearbyEffects(int i, int j, bool closer)
        {
            
        }

        private const int MaxVineHeight = 10;

        public override void RandomUpdate(int i, int j)
        {
            Tile below = Main.tile[i, j + 1];
            if (!below.HasTile && below.LiquidType == LiquidID.Water && below.LiquidAmount >= 128)
            {
                bool growVine = false;
                for (int vineOriginYPos = j; vineOriginYPos > j - MaxVineHeight; vineOriginYPos--)
                {
                    Tile consideredVineOrigin = Main.tile[i, vineOriginYPos];
                    // Vines won't grow if they are coming out of a bottom-sloped block (which they shouldn't be able to anyway)
                    if (consideredVineOrigin.BottomSlope)
                    {
                        growVine = false;
                        break;
                    }
                    // Vines can continue to grow unimpeded out of any solid block that isn't bottom-sloped.
                    if (Main.tile[i, vineOriginYPos].HasTile && !Main.tile[i, vineOriginYPos].BottomSlope && Main.tileSolid[Main.tile[i, vineOriginYPos].TileType])
                    {
                        growVine = true;
                        break;
                    }
                }

                if (growVine)
                {
                    int x = i;
                    int y = j + 1;

                    // Zero faith that code using local variables works with struct tiles, from experience.

                    // Spawn the new vine.
                    Main.tile[x, y].TileType = (ushort)ModContent.TileType<Cyanobacteria_Vines>();
                    Main.tile[x, y].TileFrameX = (short)(Terraria.WorldGen.genRand.Next(8) * 18);
                    Main.tile[x, y].TileFrameY = 4 * 18;
                    Main.tile[x, y].Get<TileWallWireStateData>().HasTile = true; // .HasTile = true; refuses to work.

                    // Pick a new sprite for the current vine.
                    Main.tile[i, j].TileFrameX = (short)(Terraria.WorldGen.genRand.Next(12) * 18);
                    Main.tile[i, j].TileFrameY = (short)(Terraria.WorldGen.genRand.Next(4) * 18);

                    // Reframe both vines the correct vanilla way.
                    Terraria.WorldGen.SquareTileFrame(x, y, true);
                    Terraria.WorldGen.SquareTileFrame(i, j, true);

                    // Send update packets as needed.
                    if (Main.dedServ)
                        NetMessage.SendTileSquare(-1, x, y, 3, TileChangeType.None);
                }
            }
        }
        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            float brightness = 0.7f;
            brightness *= (float)MathF.Sin(-j / 40f + Main.GameUpdateCount * 0.01f + i);
            brightness += 0.5f;
            r = 1f-0.208f;
            g = 1f- 0.134f;
            b = 1f - 0.138f;
            r *= brightness;
            g *= brightness;
            b *= brightness;
        }
    }

    // This class handles spawning and growing Cyanobacteria_Vines (RandomUpdate). Vines can either grow from the tip of an existing vine or spawn from the tile it grows from.
    // Because this behavior needs to act on both Cyanobacteria_Vines and ExampleBlock tiles, we put this logic in a GlobalTile rather than in both ModTile classes.
    // This class also handle transforming vines to Cyanobacteria_Vines if the anchor tile changes (TileFrame).
    public class ExampleVineGlobalTile : GlobalTile
    {
        private int ExampleVine;
        private int ExampleBlock; // TODO: Replace with ExampleGrass eventually.

        public override void SetStaticDefaults()
        {
            // Caching these tile type values to make the code more readable
            ExampleVine = ModContent.TileType<Cyanobacteria_Vines>();
            ExampleBlock = ModContent.TileType<CarbonShale_Tile>();
        }

        // Random growth behavior:
        public override void RandomUpdate(int i, int j, int type)
        {
            if (j >= Main.worldSurface - 1)
            {
                return; // Cyanobacteria_Vines only grows above ground
            }

            Tile tile = Main.tile[i, j];
            if (!tile.HasUnactuatedTile)
            {
                return; // Don't grow on actuated tiles.
            }

            // Vine tiles usually grow on themselves (from the tip) or on any tile they spawn from (grass tiles usually). GrowMoreVines checks that the nearby area isn't already full of vines.
            if ((tile.TileType == ExampleVine || tile.TileType == ExampleBlock) && Terraria.WorldGen.GrowMoreVines(i, j))
            {
                int growChance = 70;
                if (tile.TileType == ExampleVine)
                {
                    growChance = 7; // 10 times more likely to extend an existing vine than start a new vine
                }

                int below = j + 1;
                Tile tileBelow = Main.tile[i, below];
                if (Terraria.WorldGen.genRand.NextBool(growChance) && !tileBelow.HasTile && tileBelow.LiquidType != LiquidID.Lava)
                {
                    // We check that the vine can grow longer and is not already broken.
                    bool vineIsHangingOffValidTile = false;
                    for (int above = j; above > j - 10; above--)
                    {
                        Tile tileAbove = Main.tile[i, above];
                        if (tileAbove.BottomSlope)
                        {
                            return;
                        }

                        if (tileAbove.HasTile && tileAbove.TileType == ExampleBlock && !tileAbove.BottomSlope)
                        {
                            vineIsHangingOffValidTile = true;
                            break;
                        }
                    }

                    if (vineIsHangingOffValidTile)
                    {
                        // If all the checks succeed, place the tile, copy paint from the tile we grew from, and sync the tile change.
                        tileBelow.TileType = (ushort)ExampleVine;
                        tileBelow.HasTile = true;
                        tileBelow.CopyPaintAndCoating(tile);
                        Terraria.WorldGen.SquareTileFrame(i, below);
                        if (Main.netMode == NetmodeID.Server)
                        {
                            NetMessage.SendTileSquare(-1, i, below);
                        }
                    }
                }
            }
        }

        // Transforming vines to Cyanobacteria_Vines if necessary behavior
        public override bool TileFrame(int i, int j, int type, ref bool resetFrame, ref bool noBreak)
        {
            // This code handles transforming any vine to Cyanobacteria_Vines if the anchored tile happens to change to ExampleBlock. This can happen with spreading grass tiles or Clentaminator solutions. Without this code the vine would just break in those situations.
            if (!TileID.Sets.IsVine[type])
            {
                return true;
            }

            Tile tile = Main.tile[i, j];
            Tile tileAbove = Main.tile[i, j - 1];

            // We determine the tile type of the tile above this tile. If the tile doesn't exist, is actuated, or has a slopped bottom, the vine will be destroyed (-1).
            int aboveTileType = tileAbove.HasUnactuatedTile && !tileAbove.BottomSlope ? tileAbove.TileType : -1;

            // If this tile isn't the same as the one above, we need to verify that the above tile is valid.
            if (type != aboveTileType)
            {
                // If the above tile is a valid Cyanobacteria_Vines anchor, but this tile isn't Cyanobacteria_Vines, we change this tile into Cyanobacteria_Vines.
                if ((aboveTileType == ExampleBlock || aboveTileType == ExampleVine) && type != ExampleVine)
                {
                    tile.TileType = (ushort)ExampleVine;
                    Terraria.WorldGen.SquareTileFrame(i, j);
                    return true;
                }

                // Finally, we need to handle the case where there is not longer a valid placement for Cyanobacteria_Vines.
                // Due to the ordering of hooks with respect to vanilla code, it is not easy to do this in a mod-compatible manner directly. Vanilla vine code or vine code from other mods might convert the vine to a new tile type, but we can't know that here.
                // If the anchor tile is invalid, we kill the tile, otherwise we change the vine tile to TileID.Vines and let the vanilla code that will run after this handle the remaining logic.
                if (type == ExampleVine && aboveTileType != ExampleBlock)
                {
                    if (aboveTileType == -1)
                    {
                        Terraria.WorldGen.KillTile(i, j);
                    }
                    else
                    {
                        tile.TileType = TileID.Vines;
                    }
                }
            }

            return true;
        }
    }

    // With growing or spreading tiles, it can be time consuming to wait for tiles to grow naturally to test their behavior. Debug code like this can help with testing, just be sure to remove it when publishing your mod.
	public class TestVinesSystem : ModSystem
	{
		public override void PostUpdateWorld() {
            if(false)
			if (Main.keyState.IsKeyDown(Keys.D3) && !Main.oldKeyState.IsKeyDown(Keys.D3)) {
				// Spawn vines at the cursor location.
				new ActionVines(3, 8, ModContent.TileType<Cyanobacteria_Vines>()).Apply(new Point(Player.tileTargetX, Player.tileTargetY), Player.tileTargetX, Player.tileTargetY);

				Terraria.WorldGen.RangeFrame(Player.tileTargetX - 1, Player.tileTargetY - 1, Player.tileTargetX + 1, Player.tileTargetY + 10);
			}
		}
	}
	
}
