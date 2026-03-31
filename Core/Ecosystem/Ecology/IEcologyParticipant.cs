using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Core.Ecosystem.Ecology
{
    public interface IEcologyParticipant
    {
        public void SetSpeciesEcology(SpeciesEcologyDefinition definition);
        public void SetupIndividualEcology(NPC npc, EcologyGlobalNPC ecology);
    }
}
