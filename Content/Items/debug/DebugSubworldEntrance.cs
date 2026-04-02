using AbyssOverhaul.Core.Subworlds;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Content.Items.Debug
{
    internal class DebugSubworldEntrance : ModItem
    {
        public override void SetDefaults()
        {

            Item.useStyle = ItemUseStyleID.HoldUp;
        }

        public override bool? UseItem(Player player)
        {
           
            return base.UseItem(player);
        }
    }
}
