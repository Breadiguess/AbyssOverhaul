using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Core.Ecosystem.Ecology
{
    [Flags]
    public enum NpcTraitFlags
    {
        None = 0,
        Predator = 1 << 0,
        Prey = 1 << 1,
        Schooling = 1 << 2,
        AmbushPredator = 1 << 3,
        Territorial = 1 << 4,
        Scavager = 1 << 5,
    }
}
