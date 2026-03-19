using AbyssOverhaul.Common.Brain.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Common.Brain.SharedModules
{
    internal class EatFoodModule : INpcModule<CreatureNpcContext>
    {
        public NpcDirective Evaluate(CreatureNpcContext context)
        {
            return new NpcDirective()
            {

            };
        }
    }
}
