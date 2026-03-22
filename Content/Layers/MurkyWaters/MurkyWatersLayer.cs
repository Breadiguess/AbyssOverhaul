using AbyssOverhaul.Content.NPCs.DeepSnapperNPC;
using AbyssOverhaul.Core.DataStructures;
using AbyssOverhaul.Core.Utilities;
using CalamityMod.NPCs.Abyss;
using CalamityMod.NPCs.AcidRain;
using CalamityMod.NPCs.OldDuke;
using CalamityMod.NPCs.SulphurousSea;
using CalamityMod.Waters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Content.Layers.MurkyWaters
{
    internal class MurkyWatersLayer : AbyssLayer
    {
        public override int StartHeight => AbyssGenUtils.YAt(0.0f)-50;

        public override int EndHeight => AbyssGenUtils.YAt(0.15f);

        public override ModWaterStyle ModWaterStyle => UpperAbyssWater.Instance;
        public override Dictionary<int, float> NPCSpawnPool => new()
        {

            [ModContent.NPCType<AquaticUrchin>()] = 1.2f,
            [ModContent.NPCType<BabyCannonballJellyfish>()] = 0.8f,
            [ModContent.NPCType<BoxJellyfish>()] = 1.2f,
            [ModContent.NPCType<MorayEel>()] = 1.2f,
            [ModContent.NPCType<Sulflounder>()] = 1.2f,
            [ModContent.NPCType<SlabCrab>()] = 1.2f,
            [ModContent.NPCType<ToxicMinnow>()] = 1.2f,
            [ModContent.NPCType<Orthocera>()] = 0.2f,
            [ModContent.NPCType<DeepSnapper>()] = 0.9f,
            [ModContent.NPCType<OldDuke>()] = 0.0001f,
        };
    }
}
