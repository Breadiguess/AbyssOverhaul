using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Core.NPCOverrides
{
    public class GlobalFishNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;
        public bool IsAbyssPredator;

        public bool IsAbyssPrey;
    }
}
