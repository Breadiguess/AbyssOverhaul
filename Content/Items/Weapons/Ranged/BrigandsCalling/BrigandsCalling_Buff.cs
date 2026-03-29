using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Content.Items.Weapons.Ranged.BrigandsCalling
{
    internal class BrigandsCalling_Buff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
            BuffID.Sets.LongerExpertDebuff[Type] = true;

        }


        public override void Update(Player player, ref int buffIndex)
        {
            if(player.TryGetModPlayer<BrigandsCalling_Player>(out var modPlayer))
            {
                if(modPlayer.RPMBoost - 120 < 0)
                modPlayer.RPMBoost = 120;
            }

            
        }
    }
}
