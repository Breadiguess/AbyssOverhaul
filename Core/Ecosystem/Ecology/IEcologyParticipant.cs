using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Core.Ecosystem.Ecology
{
    public interface IEcologyParticipant
    {
        public void SetupEcology(NPC npc, EcologyGlobalNPC ecology);
    }
}
