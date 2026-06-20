using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Content.BehaviorOverrides.ToxicMinnow
{
    internal class ToxicMinnowOverride : NPCBehaviorOverride//, IEcologyParticipant
    {
        public override int NPCType => ModContent.NPCType<CalamityMod.NPCs.Abyss.ToxicMinnow>();
    }
}
