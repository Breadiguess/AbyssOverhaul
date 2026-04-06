using AbyssOverhaul.Core.DataStructures;
using AbyssOverhaul.Core.Utilities;
using CalamityMod.NPCs.Abyss;
using CalamityMod.NPCs.NormalNPCs;
using CalamityMod.NPCs.PrimordialWyrm;
using CalamityMod.Waters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Content.Layers.DeepAbyss
{
    internal class TheDeepAbyss : AbyssLayer
    {
        public override int StartHeight => AbyssGenUtils.YAt(1);

        public override int EndHeight => AbyssGenUtils.YAt(1.15f);

        public override ModWaterStyle ModWaterStyle => VoidWater.Instance;
        public override Dictionary<int, float> NPCSpawnPool => new()
        {

            //[ModContent.NPCType<PrimordialWyrmHead>()] = 1f,

            
        };

        public override void ModifyGenTasks()
        {
            
        }

    }
}
