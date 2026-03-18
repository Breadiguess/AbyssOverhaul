using AbyssOverhaul.Common.Brain.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.Social.WeGame;

namespace AbyssOverhaul.Common.Brain
{
    namespace AbyssOverhaul.Common.Brain
    {
        public interface INpcSensor<in TContext> where TContext : NpcContext
        {
            void Update(TContext context);
        }

        public interface INpcModule<in TContext> where TContext : NpcContext
        {
            NpcDirective Evaluate(TContext context);
        }
    }
}
