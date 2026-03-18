using CalamityMod.Cooldowns;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Content.Items.Armor.BlackMoon
{
    public class BlackMoonArmorCooldown : CooldownHandler
    {

        public override bool CanTickDown => true;
        public static new string ID => "BlackMoonArmorCooldow";
        public override string Texture => "CalamityMod/Cooldowns/AquaticHeartIceShield";

        public override Color OutlineColor => Color.Lerp(new Color(163, 186, 198), new Color(146, 187, 255), (float)Math.Sin(Main.GlobalTimeWrappedHourly) * 0.5f + 0.5f);
        public override Color CooldownStartColor => new Color(124, 195, 214);
        public override Color CooldownEndColor => new Color(147, 230, 253);
    }
}
