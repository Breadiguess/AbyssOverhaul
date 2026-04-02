using Daybreak.Common.Features.Hooks;
using SubworldLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Core.Subworlds
{
    internal class AbyssSubworldGlobalNPC : GlobalNPC
    {
        public override void EditSpawnPool(IDictionary<int, float> pool, NPCSpawnInfo spawnInfo)
        {
        
            if (!SubworldSystem.IsActive<AbyssSubworld>())
                return;

            pool.Clear();
        }

    }
}
