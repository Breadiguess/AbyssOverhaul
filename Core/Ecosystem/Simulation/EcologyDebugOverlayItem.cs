using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Core.Ecosystem.Simulation
{
    internal class EcologyDebugOverlayItem : ModItem
    {
        public override void SetDefaults()
        {
            Item.shoot = ProjectileID.BeeArrow;

            Item.useStyle = ItemUseStyleID.HoldUp;

            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.width = 16;
            Item.height = 16;
            Item.rare = ItemRarityID.Red;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            EcologyDebugOverlaySystem.Visible = !EcologyDebugOverlaySystem.Visible;
            return false;
        }
    }
}
