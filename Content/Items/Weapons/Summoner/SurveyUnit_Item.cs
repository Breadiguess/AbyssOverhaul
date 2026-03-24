using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Content.Items.Weapons.Summoner
{
    internal class SurveyUnit_Item:ModItem
    {
        public override void SetDefaults()
        {
            Item.shoot = ModContent.ProjectileType<SurveyUnitProjectile>();
            Item.useStyle = 1;
            Item.useTime = 40;
            Item.useAnimation = 40;
        }
    }
}
