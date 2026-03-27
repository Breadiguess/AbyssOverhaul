using JetBrains.Annotations;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using rail;
using ReLogic.Content;
using System;
using System.IO;
using System.Security.AccessControl;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;
using static System.Net.WebRequestMethods;

namespace AbyssOverhaul.Core;

public static class TileUtils
{
    public static int ToPixel(int frame)
    {
        return frame * 18;
    }
    public static void SetTileSprite(Tile tile, int spriteFrameX, int spriteFrameY)
    {
        tile.TileFrameX = (short)ToPixel(spriteFrameX);
        tile.TileFrameY = (short)ToPixel(spriteFrameY);
    }
    /// <summary>
    /// if this returns 1 then the tile is top left. 
    /// if this returns 2 then the tile is top right.
    /// if this returns 3 then the tile is bottom left.
    /// if this returns 4 then the tile is bottom right.
    /// </summary>
    /// <param name="tile"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns>return the type of corner the tile is at returns null if it is not a corner
    /// </returns>
    public static int? CornerType(Tile tile, int x, int y)
    {
        Tile left = Framing.GetTileSafely(x - 1, y);
        Tile Right = Framing.GetTileSafely(x + 1, y);
        Tile above = Framing.GetTileSafely(x, y - 1);
        Tile below = Framing.GetTileSafely(x, y + 1);

        if (TileUtils.HasSolidBlock(above) && !TileUtils.HasSolidBlock(below))
        {
            if (TileUtils.HasSolidBlock(Right) && !TileUtils.HasSolidBlock(left)) // top left
            {
                return 3;
            }
            if (!TileUtils.HasSolidBlock(Right) && TileUtils.HasSolidBlock(left)) // top right
            {
                return 4;
            }
        }
        if (!TileUtils.HasSolidBlock(above) && TileUtils.HasSolidBlock(below))
        {
            if (TileUtils.HasSolidBlock(Right) && !TileUtils.HasSolidBlock(left)) // bottom left
            {
                return 1;
            }
            if (!TileUtils.HasSolidBlock(Right) && TileUtils.HasSolidBlock(left)) // bottom right
            {
                return 2;
            }
        }

        return null;
    }
    public static int? CornerTypeThin(Tile tile, int x, int y)
    {
        Tile rt_tile = Framing.GetTileSafely(x + 1, y - 1);
        Tile lt_tile = Framing.GetTileSafely(x - 1, y - 1);
        Tile rb_tile = Framing.GetTileSafely(x + 1, y + 1);
        Tile lb_tile = Framing.GetTileSafely(x - 1, y + 1);
        Tile left = Framing.GetTileSafely(x - 1, y);
        Tile Right = Framing.GetTileSafely(x + 1, y);
        Tile above = Framing.GetTileSafely(x, y - 1);
        Tile below = Framing.GetTileSafely(x, y + 1);

        if (TileUtils.HasSolidBlock(above) && !TileUtils.HasSolidBlock(below))
        {
            if (TileUtils.HasSolidBlock(Right) && !TileUtils.HasSolidBlock(left) && !TileUtils.HasSolidBlock(rt_tile)) // top left
            {
                return 3;
            }
            if (!TileUtils.HasSolidBlock(Right) && TileUtils.HasSolidBlock(left) && !TileUtils.HasSolidBlock(lt_tile)) // top right
            {
                return 4;
            }
        }
        if (!TileUtils.HasSolidBlock(above) && TileUtils.HasSolidBlock(below))
        {
            if (TileUtils.HasSolidBlock(Right) && !TileUtils.HasSolidBlock(left) && !TileUtils.HasSolidBlock(rb_tile)) // bottom left
            {
                return 1;
            }
            if (!TileUtils.HasSolidBlock(Right) && TileUtils.HasSolidBlock(left) && !TileUtils.HasSolidBlock(lb_tile)) // bottom right
            {
                return 2;
            }
        }

        return null;
    }  
    public static int? InternalCornerSraight(Tile tile, int x, int y)
    {
        Tile rt_tile = Framing.GetTileSafely(x + 1, y - 1);
        Tile lt_tile = Framing.GetTileSafely(x - 1, y - 1);
        Tile rb_tile = Framing.GetTileSafely(x + 1, y + 1);
        Tile lb_tile = Framing.GetTileSafely(x - 1, y + 1);
        Tile left = Framing.GetTileSafely(x - 1, y);
        Tile Right = Framing.GetTileSafely(x + 1, y);
        Tile above = Framing.GetTileSafely(x, y - 1);
        Tile below = Framing.GetTileSafely(x, y + 1);
        if (BottomStraightTile(tile, x, y))
        {
            if (!HasSolidBlock(lt_tile))// bottom left pointing up
            {
                return 1;
            }
            if (!HasSolidBlock(rt_tile))// bottom right pointing up
            {
                return 2;
            }
        }
        if (TopStraightTile(tile, x, y))
        {
            if (!HasSolidBlock(lb_tile))// top left pointing down
            {
                return 3;
            }
            if (!HasSolidBlock(rb_tile))// top right pointing down
            {
                return 4;
            }
        }
        if (LeftTileStraight(tile, x, y))
        {
            if (!HasSolidBlock(rt_tile))// left tile pointing up
            {
                return 5;
            }
            if (!HasSolidBlock(rb_tile))// left right pointing down
            {
                return 6;
            }
        }
        if (RightTileStraight(tile, x, y))
        {
            if (!HasSolidBlock(lt_tile))// left right pointing up and left
            {
                return 7;
            }
            if (!HasSolidBlock(lb_tile))// top right pointing down
            {
                return 8;
            }
        }
        return null; 
    }
    public static bool Tagged(Tile checked_tile, Tile tile1, Tile tile2, Tile tile3, bool include_checked_tile = true)
    {
        if ((TileUtils.HasSolidBlock(checked_tile)|| !include_checked_tile) && !TileUtils.HasSolidBlock(tile1) && !TileUtils.HasSolidBlock(tile2) && !TileUtils.HasSolidBlock(tile3))
        {
            return true;
        }
        return false;
    }
    public static bool Intersection4(int x, int y)
    {
        Tile tile = Framing.GetTileSafely(x,y);
        Tile rt_tile = Framing.GetTileSafely(x + 1, y - 1);
        Tile lt_tile = Framing.GetTileSafely(x - 1, y - 1);
        Tile rb_tile = Framing.GetTileSafely(x + 1, y + 1);
        Tile lb_tile = Framing.GetTileSafely(x - 1, y + 1);
        Tile left = Framing.GetTileSafely(x - 1, y);
        Tile Right = Framing.GetTileSafely(x + 1, y);
        Tile above = Framing.GetTileSafely(x, y - 1);
        Tile below = Framing.GetTileSafely(x, y + 1);

        if (HasSolidBlock(left) &&
            HasSolidBlock(Right) &&
            HasSolidBlock(above) &&
            HasSolidBlock(below) &&
            !HasSolidBlock(rt_tile) &&
            !HasSolidBlock(lt_tile) &&
            !HasSolidBlock(rb_tile) &&
            !HasSolidBlock(lb_tile)
        )
        {
            return true;
        }
        return false;
    }
    public static int? InternalCornerThin(int x, int y)
    {
        Tile tile = Framing.GetTileSafely(x, y);
        Tile rt_tile = Framing.GetTileSafely(x + 1, y - 1);
        Tile lt_tile = Framing.GetTileSafely(x - 1, y - 1);
        Tile rb_tile = Framing.GetTileSafely(x + 1, y + 1);
        Tile lb_tile = Framing.GetTileSafely(x - 1, y + 1);
        Tile left = Framing.GetTileSafely(x - 1, y);
        Tile Right = Framing.GetTileSafely(x + 1, y);
        Tile above = Framing.GetTileSafely(x, y - 1);
        Tile below = Framing.GetTileSafely(x, y + 1);

        if (TileUtils.HasSolidBlock(above) &&
            !TileUtils.HasSolidBlock(below)
        )
        {
            if (TileUtils.HasSolidBlock(Right) &&
            !TileUtils.HasSolidBlock(left) &&
            !TileUtils.HasSolidBlock(lt_tile) &&
            !TileUtils.HasSolidBlock(rt_tile) &&
            !TileUtils.HasSolidBlock(rb_tile))
            {
                return 1;
            }
            if (TileUtils.HasSolidBlock(left) &&
            !TileUtils.HasSolidBlock(Right) &&
            !TileUtils.HasSolidBlock(rt_tile) &&
            !TileUtils.HasSolidBlock(lt_tile) &&
            !TileUtils.HasSolidBlock(lb_tile))
            {
                return 2;
            }
            return null;
        }
        if (TileUtils.HasSolidBlock(below) &&
           !TileUtils.HasSolidBlock(above)
        )
        {
            if (TileUtils.HasSolidBlock(Right) &&
            !TileUtils.HasSolidBlock(left) &&
            !TileUtils.HasSolidBlock(lb_tile) &&
            !TileUtils.HasSolidBlock(rt_tile) &&
            !TileUtils.HasSolidBlock(rb_tile))
            {
                return 3;
            }
            if (TileUtils.HasSolidBlock(left) &&
           !TileUtils.HasSolidBlock(Right) &&
           !TileUtils.HasSolidBlock(rb_tile) &&
           !TileUtils.HasSolidBlock(lt_tile) &&
           !TileUtils.HasSolidBlock(lb_tile))
            {
                return 4;
            }
            return null;
        }
        return null;
    }
    public static int? InternalCornerThick(int x, int y)
    {
        Tile tile = Framing.GetTileSafely(x, y);
        Tile rt_tile = Framing.GetTileSafely(x + 1, y - 1);
        Tile lt_tile = Framing.GetTileSafely(x - 1, y - 1);
        Tile rb_tile = Framing.GetTileSafely(x + 1, y + 1);
        Tile lb_tile = Framing.GetTileSafely(x - 1, y + 1);
        Tile left = Framing.GetTileSafely(x - 1, y);
        Tile Right = Framing.GetTileSafely(x + 1, y);
        Tile above = Framing.GetTileSafely(x, y - 1);
        Tile below = Framing.GetTileSafely(x, y + 1);

        if (TileUtils.HasSolidBlock(above) &&
            TileUtils.HasSolidBlock(below)
        )
        {
            if (TileUtils.HasSolidBlock(Right) && // bottom left
            TileUtils.HasSolidBlock(left) &&
            TileUtils.HasSolidBlock(lt_tile) &&
            !TileUtils.HasSolidBlock(rt_tile) &&
            TileUtils.HasSolidBlock(rb_tile))
            {
                return 1;
            }
            if (TileUtils.HasSolidBlock(left) && // bottom right
            TileUtils.HasSolidBlock(Right) &&
            TileUtils.HasSolidBlock(rt_tile) &&
            !TileUtils.HasSolidBlock(lt_tile) &&
            TileUtils.HasSolidBlock(lb_tile))
            {
                return 2;
            }
            if (TileUtils.HasSolidBlock(Right) && // top right
            TileUtils.HasSolidBlock(left) &&
            !TileUtils.HasSolidBlock(lb_tile) &&
            TileUtils.HasSolidBlock(rt_tile) &&
            TileUtils.HasSolidBlock(rb_tile))
            {
                return 3;
            }
            if (TileUtils.HasSolidBlock(left) && // top left 
           TileUtils.HasSolidBlock(Right) &&
           !TileUtils.HasSolidBlock(rb_tile) &&
           TileUtils.HasSolidBlock(lt_tile) &&
           TileUtils.HasSolidBlock(lb_tile))
            {
                return 4;
            }
        }
        return null;
    }
    public static int? InternalCornerThickDouble(int x, int y)
    {
        Tile tile = Framing.GetTileSafely(x, y);
        Tile rt_tile = Framing.GetTileSafely(x + 1, y - 1);
        Tile lt_tile = Framing.GetTileSafely(x - 1, y - 1);
        Tile rb_tile = Framing.GetTileSafely(x + 1, y + 1);
        Tile lb_tile = Framing.GetTileSafely(x - 1, y + 1);
        Tile left = Framing.GetTileSafely(x - 1, y);
        Tile Right = Framing.GetTileSafely(x + 1, y);
        Tile above = Framing.GetTileSafely(x, y - 1);
        Tile below = Framing.GetTileSafely(x, y + 1);

        if (TileUtils.HasSolidBlock(above) &&
            TileUtils.HasSolidBlock(below)
        )
        {
            if (TileUtils.HasSolidBlock(Right) && // bottom left
            TileUtils.HasSolidBlock(left) &&
            TileUtils.HasSolidBlock(lt_tile) &&
            !TileUtils.HasSolidBlock(rt_tile) &&
            !TileUtils.HasSolidBlock(lb_tile) &&
            TileUtils.HasSolidBlock(rb_tile))
            {
                return 1;
            }
            if (TileUtils.HasSolidBlock(left) && // bottom right
            TileUtils.HasSolidBlock(Right) &&
            TileUtils.HasSolidBlock(rt_tile) &&
            !TileUtils.HasSolidBlock(lt_tile) &&
            !TileUtils.HasSolidBlock(rb_tile) &&
            TileUtils.HasSolidBlock(lb_tile))
            {
                return 2;
            }
            if (TileUtils.HasSolidBlock(Right) && // top right
            TileUtils.HasSolidBlock(left) &&
            !TileUtils.HasSolidBlock(lb_tile) &&
            TileUtils.HasSolidBlock(rt_tile) &&
            !TileUtils.HasSolidBlock(lt_tile) &&
            TileUtils.HasSolidBlock(rb_tile))
            {
                return 3;
            }
            if (TileUtils.HasSolidBlock(left) && // top left 
           TileUtils.HasSolidBlock(Right) &&
           !TileUtils.HasSolidBlock(rb_tile) &&
           TileUtils.HasSolidBlock(lt_tile) &&
           !TileUtils.HasSolidBlock(rt_tile) &&
           TileUtils.HasSolidBlock(lb_tile))
            {
                return 4;
            }
        }
        return null;
    }
    public static int? InternalCornerThickTriple(int x, int y)
    {
        Tile tile = Framing.GetTileSafely(x, y);
        Tile rt_tile = Framing.GetTileSafely(x + 1, y - 1);
        Tile lt_tile = Framing.GetTileSafely(x - 1, y - 1);
        Tile rb_tile = Framing.GetTileSafely(x + 1, y + 1);
        Tile lb_tile = Framing.GetTileSafely(x - 1, y + 1);
        Tile left = Framing.GetTileSafely(x - 1, y);
        Tile Right = Framing.GetTileSafely(x + 1, y);
        Tile above = Framing.GetTileSafely(x, y - 1);
        Tile below = Framing.GetTileSafely(x, y + 1);

        if (TileUtils.HasSolidBlock(above) &&
            TileUtils.HasSolidBlock(below) &&
            TileUtils.HasSolidBlock(left) &&
            TileUtils.HasSolidBlock(Right)
        )
        {
            if (
            !TileUtils.HasSolidBlock(lt_tile) &&
            !TileUtils.HasSolidBlock(rt_tile) &&
            !TileUtils.HasSolidBlock(lb_tile) &&
            TileUtils.HasSolidBlock(rb_tile))
            {
                return 1;
            }
            if (
            !TileUtils.HasSolidBlock(rt_tile) &&
            !TileUtils.HasSolidBlock(lb_tile) &&
            !TileUtils.HasSolidBlock(rb_tile) &&
            TileUtils.HasSolidBlock(lt_tile))
            {
                return 2;
            }
            if (
            !TileUtils.HasSolidBlock(lb_tile) &&
            !TileUtils.HasSolidBlock(rb_tile) &&
            !TileUtils.HasSolidBlock(lt_tile) &&
            TileUtils.HasSolidBlock(rt_tile))
            {
                return 3;
            }
            if (
           !TileUtils.HasSolidBlock(rb_tile) &&
           !TileUtils.HasSolidBlock(lt_tile) &&
           !TileUtils.HasSolidBlock(rt_tile) &&
           TileUtils.HasSolidBlock(lb_tile))
            {
                return 4;
            }
        }
        return null;
    }
    public static int? Intersection2thin(int x, int y)
    {
        Tile tile = Framing.GetTileSafely(x, y);
        Tile rt_tile = Framing.GetTileSafely(x + 1, y - 1);
        Tile lt_tile = Framing.GetTileSafely(x - 1, y - 1);
        Tile rb_tile = Framing.GetTileSafely(x + 1, y + 1);
        Tile lb_tile = Framing.GetTileSafely(x - 1, y + 1);
        Tile left = Framing.GetTileSafely(x - 1, y);
        Tile Right = Framing.GetTileSafely(x + 1, y);
        Tile above = Framing.GetTileSafely(x, y - 1);
        Tile below = Framing.GetTileSafely(x, y + 1);

        if (TileUtils.HasSolidBlock(left) &&
            TileUtils.HasSolidBlock(Right)
        )
        {
            if (TileUtils.HasSolidBlock(above) &&
            !TileUtils.HasSolidBlock(rt_tile) &&
            !TileUtils.HasSolidBlock(lt_tile))
            {
                return 1;
            }
            if (TileUtils.HasSolidBlock(below) &&
            !TileUtils.HasSolidBlock(lb_tile) &&
            !TileUtils.HasSolidBlock(rb_tile))
            {
                return 3;
            }
            return null;
        }
        if (TileUtils.HasSolidBlock(above) &&
           TileUtils.HasSolidBlock(below)
        )
        {
            if (TileUtils.HasSolidBlock(Right) &&
            !TileUtils.HasSolidBlock(rt_tile) &&
            !TileUtils.HasSolidBlock(rb_tile))
            {
                return 4;
            }
            if (TileUtils.HasSolidBlock(left) &&
            !TileUtils.HasSolidBlock(lb_tile) &&
            !TileUtils.HasSolidBlock(lt_tile))
            {
                return 2;
            }
            return null;
        }
        return null;
    }
    public static int? Intersection2thick(int x, int y)
    {
        Tile tile = Framing.GetTileSafely(x, y);
        Tile rt_tile = Framing.GetTileSafely(x + 1, y - 1);
        Tile lt_tile = Framing.GetTileSafely(x - 1, y - 1);
        Tile rb_tile = Framing.GetTileSafely(x + 1, y + 1);
        Tile lb_tile = Framing.GetTileSafely(x - 1, y + 1);
        Tile left = Framing.GetTileSafely(x - 1, y);
        Tile Right = Framing.GetTileSafely(x + 1, y);
        Tile above = Framing.GetTileSafely(x, y - 1);
        Tile below = Framing.GetTileSafely(x, y + 1);

        if (TileUtils.HasSolidBlock(left) &&
            TileUtils.HasSolidBlock(Right)
        )
        {
            if (TileUtils.HasSolidBlock(above) &&
                TileUtils.HasSolidBlock(below) &&
            !TileUtils.HasSolidBlock(rt_tile) &&
            !TileUtils.HasSolidBlock(lt_tile))
            {
                return 1;
            }
            if (TileUtils.HasSolidBlock(above) &&
                TileUtils.HasSolidBlock(below) &&
            !TileUtils.HasSolidBlock(lb_tile) &&
            !TileUtils.HasSolidBlock(rb_tile))
            {
                return 3;
            }
        }
        if (TileUtils.HasSolidBlock(above) &&
           TileUtils.HasSolidBlock(below)
        )
        {
            if (TileUtils.HasSolidBlock(Right) &&
                TileUtils.HasSolidBlock(left) &&
            !TileUtils.HasSolidBlock(rt_tile) &&
            !TileUtils.HasSolidBlock(rb_tile))
            {
                return 4;
            }
            if (TileUtils.HasSolidBlock(left) &&
                TileUtils.HasSolidBlock(Right) &&
            !TileUtils.HasSolidBlock(lb_tile) &&
            !TileUtils.HasSolidBlock(lt_tile))
            {
                return 2;
            }
        }
        return null;
    }
    public static bool LeftTileStraight(Tile tile, int x, int y)
    {
        Tile left = Framing.GetTileSafely(x - 1, y);
        Tile Right = Framing.GetTileSafely(x + 1, y);
        Tile above = Framing.GetTileSafely(x, y - 1);
        Tile below = Framing.GetTileSafely(x, y + 1);

        if (!TileUtils.HasSolidBlock(left) &&
            TileUtils.HasSolidBlock(Right) &&
            TileUtils.HasSolidBlock(above) &&
            TileUtils.HasSolidBlock(below))
        {
            return true;
        }
        return false;
    }
    public static bool RightTileStraight(Tile tile, int x, int y)
    {
        Tile left = Framing.GetTileSafely(x - 1, y);
        Tile Right = Framing.GetTileSafely(x + 1, y);
        Tile above = Framing.GetTileSafely(x, y - 1);
        Tile below = Framing.GetTileSafely(x, y + 1);

        if (TileUtils.HasSolidBlock(left) &&
            !TileUtils.HasSolidBlock(Right) &&
            TileUtils.HasSolidBlock(above) &&
            TileUtils.HasSolidBlock(below))
        {
            return true;
        }
        return false;
    }
    public static bool TopStraightTile(Tile tile, int x, int y)
    {
        Tile left = Framing.GetTileSafely(x - 1, y);
        Tile Right = Framing.GetTileSafely(x + 1, y);
        Tile above = Framing.GetTileSafely(x, y - 1);
        Tile below = Framing.GetTileSafely(x, y + 1);
        if (TileUtils.HasSolidBlock(left) &&
           TileUtils.HasSolidBlock(Right) &&
           !TileUtils.HasSolidBlock(above) &&
           TileUtils.HasSolidBlock(below))
        {
            return true;
        }
        return false;
    }
    public static bool BottomStraightTile(Tile tile, int x, int y)
    {
        Tile left = Framing.GetTileSafely(x - 1, y);
        Tile Right = Framing.GetTileSafely(x + 1, y);
        Tile above = Framing.GetTileSafely(x, y - 1);
        Tile below = Framing.GetTileSafely(x, y + 1);
        if (TileUtils.HasSolidBlock(left) &&
           TileUtils.HasSolidBlock(Right) &&
           TileUtils.HasSolidBlock(above) &&
           !TileUtils.HasSolidBlock(below))
        {
            return true;
        }
        return false;
    }
    public static bool ThinHorizontialTile(Tile tile, int x, int y)
    {
        Tile left = Framing.GetTileSafely(x - 1, y);
        Tile Right = Framing.GetTileSafely(x + 1, y);
        Tile above = Framing.GetTileSafely(x, y - 1);
        Tile below = Framing.GetTileSafely(x, y + 1);
        if (TileUtils.HasSolidBlock(left) &&
           TileUtils.HasSolidBlock(Right) &&
           !TileUtils.HasSolidBlock(above) &&
           !TileUtils.HasSolidBlock(below))
        {
            return true;
        }
        return false;
    }
    public static bool ThinVerticalTile(Tile tile, int x, int y)
    {
        Tile left = Framing.GetTileSafely(x - 1, y);
        Tile Right = Framing.GetTileSafely(x + 1, y);
        Tile above = Framing.GetTileSafely(x, y - 1);
        Tile below = Framing.GetTileSafely(x, y + 1);
        if (!TileUtils.HasSolidBlock(left) &&
           !TileUtils.HasSolidBlock(Right) &&
           TileUtils.HasSolidBlock(above) &&
           TileUtils.HasSolidBlock(below))
        {
            return true;
        }
        return false;
    }
    public static int? TagType(Tile tile, int x, int y)
    {
        Tile left = Framing.GetTileSafely(x - 1, y);
        Tile Right = Framing.GetTileSafely(x + 1, y);
        Tile above = Framing.GetTileSafely(x, y - 1);
        Tile below = Framing.GetTileSafely(x, y + 1);

        if (Tagged(left, Right, above, below) == true) // pointing right
        {
            return 2;
        }
        if (Tagged(above, left, Right, below) == true) // pointing down
        {
            return 3;
        }
        if (Tagged(Right, left, above, below) == true) // pointing left
        {
            return 0;
        }
        if (Tagged(below, left, above, Right) == true) // pointing up 
        {
            return 1;
        }

        return null;
    }
    public static bool Incased(Tile tile, int x, int y)
    {
        Tile left = Framing.GetTileSafely(x - 1, y);
        Tile Right = Framing.GetTileSafely(x + 1, y);
        Tile above = Framing.GetTileSafely(x, y - 1);
        Tile below = Framing.GetTileSafely(x, y + 1);

        if (TileUtils.HasSolidBlock(left) &&
            TileUtils.HasSolidBlock(Right) &&
            TileUtils.HasSolidBlock(above) &&
            TileUtils.HasSolidBlock(below))
        {
            return true;
        }
        return false;
    }
    public static bool Free(Tile tile, int x, int y)
    {
        Tile left = Framing.GetTileSafely(x - 1, y);
        Tile Right = Framing.GetTileSafely(x + 1, y);
        Tile above = Framing.GetTileSafely(x, y - 1);
        Tile below = Framing.GetTileSafely(x, y + 1);

        if (!TileUtils.HasSolidBlock(left) &&
            !TileUtils.HasSolidBlock(Right) &&
            !TileUtils.HasSolidBlock(above) &&
            !TileUtils.HasSolidBlock(below))
        {
            return true;
        }
        return false;
    }
    public static bool HasSolidBlock(Tile tile)
    {
        if (tile.HasTile && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType])
        {
            return true;
        }
        return false;
    }
}
public abstract partial class FBGlobalTile : ModTile
{
    /// <summary>
    /// Automtically set the tile to a rare fill varients
    /// </summary>
    /// <param name="x">the x coord of the tile.</param>
    /// <param name="y">the y coord of the tile.</param>
    /// <returns></returns>
    public void SetRareTileFills(int x, int y)
    {
        Tile tile = Framing.GetTileSafely(x, y);
        int varition = TileRandom(y, x).Next(1, 4) - 1;
        tile.TileFrameY = (short)(TileUtils.ToPixel(4));
        tile.TileFrameX = (short)(TileUtils.ToPixel(9 + varition));
    }
    public bool CanUseRareTileFills(int x, int y, int sparseness)
    {
        Tile tile = Framing.GetTileSafely(x, y);
        if (TileRandom(x, y).NextBool(1, sparseness) && TileUtils.Incased(tile, x, y))
        {
            Main.NewText("true");
            return true;
        }
        Main.NewText("false");
        return false;
    }
    public UnifiedRandom TileRandom(int x, int y)
    {
        int seed = Main.ActiveWorldFileData.Seed;
        int tileSeed = x * 31 + y * 7 + seed;
        return new UnifiedRandom(tileSeed);
    }
}
public abstract partial class SlateTile : FBGlobalTile
{

    // the cords of which the custom frames of the shale tile start
    private int SlateStartX = 0;
    private int SlateStartY = 0;
    public void SetShaleStartCoords(int SpriteframeX = 0, int SpriteframeY = 0)
    {
        SlateStartX = TileUtils.ToPixel(SpriteframeX); SlateStartY = TileUtils.ToPixel(SpriteframeY);
    }
    public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak)
    {

        Tile tile = Framing.GetTileSafely(i, j);
        int? frameX = CustomSlateFrameX(tile, i, j);
        bool? IsBottom = bottomTile(tile, i, j);
        int? corner_type = TileUtils.CornerType(tile, i, j);
        int? tag_type = TileUtils.TagType(tile, i, j);

        int variation = (i + j) % 3; // Main.rand.Next(0,3); //

        if (frameX.HasValue && IsBottom.HasValue)
        {          
            if (TileUtils.Incased(tile, i, j))
            {
                tile.TileFrameX = (short)(SlateStartX + TileUtils.ToPixel((int)frameX));

                if (IsBottom == true)
                {
                    tile.TileFrameY = (short)(SlateStartY + TileUtils.ToPixel(3 + variation));
                }
                else
                {
                    tile.TileFrameY = (short)(SlateStartY + TileUtils.ToPixel(variation));
                }
                return false;
            }
            if (TileUtils.TopStraightTile(tile, i, j))
            {
                if (IsBottom == true)
                {
                    tile.TileFrameX = (short)(SlateStartX + TileUtils.ToPixel((int)frameX + 6));
                    tile.TileFrameY = (short)(SlateStartY + TileUtils.ToPixel(9 + variation));                 
                }
                else
                {
                    tile.TileFrameX = (short)(SlateStartX + TileUtils.ToPixel((int)frameX + 6));
                    tile.TileFrameY = (short)(SlateStartY + TileUtils.ToPixel(6 + variation));
                }
                return false;
            }
            if (TileUtils.BottomStraightTile(tile, i, j))
            {
                if (IsBottom == true)
                {
                    tile.TileFrameX = (short)(SlateStartX + TileUtils.ToPixel((int)frameX + 12));
                    tile.TileFrameY = (short)(SlateStartY + TileUtils.ToPixel(9 + variation));
                }
                else
                {
                    tile.TileFrameX = (short)(SlateStartX + TileUtils.ToPixel((int)frameX + 12));
                    tile.TileFrameY = (short)(SlateStartY + TileUtils.ToPixel(6 + variation));
                }
                return false;
            }
            if(TileUtils.LeftTileStraight(tile, i, j))
            {
                tile.TileFrameX = (short)(SlateStartX + TileUtils.ToPixel((int)frameX));

                if (IsBottom == true)
                {
                    tile.TileFrameY = (short)(SlateStartY + TileUtils.ToPixel(9 + variation));
                }
                else
                {
                    tile.TileFrameY = (short)(SlateStartY + TileUtils.ToPixel(6 + variation));
                }
                return false;
            }
            if (TileUtils.RightTileStraight(tile, i, j))
            {
                tile.TileFrameX = (short)(SlateStartX + TileUtils.ToPixel((int)frameX));

                if (IsBottom == true)
                {
                    tile.TileFrameY = (short)(SlateStartY + TileUtils.ToPixel(15 + variation));
                }
                else
                {
                    tile.TileFrameY = (short)(SlateStartY + TileUtils.ToPixel(12 + variation));
                }
                return false;
            }
            if (corner_type.HasValue)
            {
                switch (corner_type)
                {
                    case 1: // top left
                        tile.TileFrameX = (short)(SlateStartX + TileUtils.ToPixel((int)frameX + 6));

                        if (IsBottom == true)
                        {
                            tile.TileFrameY = (short)(SlateStartY + TileUtils.ToPixel(3 + variation));
                        }
                        else
                        {
                            tile.TileFrameY = (short)(SlateStartY + TileUtils.ToPixel(variation));
                        }
                        break;

                    case 2: // top right

                        tile.TileFrameX = (short)(SlateStartX + TileUtils.ToPixel((int)frameX + 12));

                        if (IsBottom == true)
                        {
                            tile.TileFrameY = (short)(SlateStartY + TileUtils.ToPixel(3 + variation));
                        }
                        else
                        {
                            tile.TileFrameY = (short)(SlateStartY + TileUtils.ToPixel(variation));
                        }
                        break;

                    case 3: // bottom left

                        tile.TileFrameX = (short)(SlateStartX + TileUtils.ToPixel((int)frameX + 18));

                        if (IsBottom == true)
                        {
                            tile.TileFrameY = (short)(SlateStartY + TileUtils.ToPixel(3 + variation));
                        }
                        else
                        {
                            tile.TileFrameY = (short)(SlateStartY + TileUtils.ToPixel(variation));
                        }
                        break;
                    case 4: // bottom left

                        tile.TileFrameX = (short)(SlateStartX + TileUtils.ToPixel((int)frameX + 24));

                        if (IsBottom == true)
                        {
                            tile.TileFrameY = (short)(SlateStartY + TileUtils.ToPixel(3 + variation));
                        }
                        else
                        {
                            tile.TileFrameY = (short)(SlateStartY + TileUtils.ToPixel(variation));
                        }
                        break;
                }
                return false;
            }
            if(TileUtils.ThinHorizontialTile(tile, i, j))
            {
                tile.TileFrameX = (short)(SlateStartX + TileUtils.ToPixel((int)frameX + 18));
                if (IsBottom == true)
                {
                    tile.TileFrameY = (short)(SlateStartY + TileUtils.ToPixel(9 + variation));
                }
                else
                {
                    tile.TileFrameY = (short)(SlateStartY + TileUtils.ToPixel(6 + variation));
                }
                return false;
            }
            if (TileUtils.ThinVerticalTile(tile, i, j))
            {
                tile.TileFrameX = (short)(SlateStartX + TileUtils.ToPixel((int)frameX + 24));
                if (IsBottom == true)
                {
                    tile.TileFrameY = (short)(SlateStartY + TileUtils.ToPixel(9 + variation));
                }
                else
                {
                    tile.TileFrameY = (short)(SlateStartY + TileUtils.ToPixel(6 + variation));
                }
                return false;
            }
            if (tag_type.HasValue)
            {
                tile.TileFrameX = (short)(SlateStartX + TileUtils.ToPixel((int)frameX + 6 + (int)tag_type * 6));
                if (IsBottom == true)
                {
                    tile.TileFrameY = (short)(SlateStartY + TileUtils.ToPixel(15 + variation));
                }
                else
                {
                    tile.TileFrameY = (short)(SlateStartY + TileUtils.ToPixel(12 + variation));
                }
                return false;
            }
            return false;
        }
        else
        {
            return base.TileFrame(i, j, ref resetFrame, ref noBreak);
        }
    }
    public override void AnimateIndividualTile(int type, int i, int j, ref int frameXOffset, ref int frameYOffset)
    {
        Tile tile = Framing.GetTileSafely(i, j);

        bool? IsBottom = bottomTile(tile, i, j);
        if (IsBottom.HasValue) {
            if (!CustomSlateFrameX(tile, i, j).HasValue && (bool)IsBottom == true) {
                frameYOffset = j % 2 * 90;
            } 
        
        }
    }
    private int? CustomSlateFrameX(Tile tile, int x, int y)
    {
        int? Xframe = x % 7 - ((y / 2) % 2) * 3 - (int)MathF.Abs(MathF.Cos(y / 2) * 2);

        if (TileUtils.Incased(tile, x, y) ||
            TileUtils.CornerType(tile, x, y).HasValue ||
            TileUtils.TagType(tile, x, y).HasValue ||
            TileUtils.TopStraightTile(tile, x, y) || TileUtils.BottomStraightTile(tile, x,y) ||
            TileUtils.LeftTileStraight(tile, x,y) || TileUtils.RightTileStraight(tile, x, y) ||
            TileUtils.ThinHorizontialTile(tile, x,y) || TileUtils.ThinVerticalTile(tile, x,y)
            )
        {          
            if (Xframe < 0) return Xframe + 7;
            if (Xframe == 0) { Xframe = null; }
            
            return Xframe;
        }
        return null;
    }
    private bool? bottomTile(Tile tile, int x, int y)
    {
        bool? isBottom = y % 2 == 1;
        return isBottom;
    } 
}
