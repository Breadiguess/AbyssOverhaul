using AbyssOverhaul.Core.DataStructures;
using AbyssOverhaul.Core.Utilities;
using CalamityMod.NPCs.Abyss;
using CalamityMod.NPCs.SulphurousSea;
using CalamityMod.Waters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Content.Layers.SulphuricDepths
{
    internal class SulphuricDepthsLayer : AbyssLayer
    {
        public override int StartHeight => AbyssGenUtils.YAt(-0.2f);

        public override int EndHeight => AbyssGenUtils.TopY;



        public override string Name => "Sulphuric Depths";
        public override ModWaterStyle ModWaterStyle => SulphuricWater.Instance;
        public override Dictionary<int, float> NPCSpawnPool => new()
        {
            [ModContent.NPCType<AquaticUrchin>()] = 1.2f,
            [ModContent.NPCType<BabyCannonballJellyfish>()] = 0.8f,
            [ModContent.NPCType<BoxJellyfish>()] = 1.2f,
            [ModContent.NPCType<MorayEel>()] = 1.2f,
            [ModContent.NPCType<Sulflounder>()] = 1.2f,
            [ModContent.NPCType<SlabCrab>()] = 1.2f,
            [ModContent.NPCType<ToxicMinnow>()] = 1.2f,
            [ModContent.NPCType<Toxicatfish>()] = 1.2f,


        };
    }
}
