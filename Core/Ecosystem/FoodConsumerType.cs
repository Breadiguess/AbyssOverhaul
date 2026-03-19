using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Core.Ecosystem
{
    [Flags]
    public enum FoodConsumerType
    {
        None = 0,
        Herbivore = 1 << 0,
        Carnivore = 1 << 1,
        Scavenger = 1 << 2,
        Omnivore = 1 << 3,
    }
}
