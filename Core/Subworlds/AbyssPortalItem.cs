using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Core.Subworlds
{
    internal sealed class AbyssPortalItem : ModItem
    {
        public override string Texture => Assets.Textures.Extra.Star.KEY;
        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(ModContent.TileType<AbyssPortalTile>());
            Item.width = 32;
            Item.height = 32;
            Item.maxStack = 9999;
            Item.useTurn = true;
            Item.autoReuse = true;
            Item.consumable = true;
            Item.noUseGraphic = true;
            Item.rare = ItemRarityID.LightRed;
        }
    }
}
