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

namespace AbyssOverhaul.Content.Layers.TheVoid
{
    internal class TheVoidLayer : AbyssLayer
    {
        public override int StartHeight => AbyssGenUtils.YAt(0.8f);

        public override int EndHeight => AbyssGenUtils.YAt(1);

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
