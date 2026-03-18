using AbyssOverhaul.Content.Layers.FossilShale.Biome;
using AbyssOverhaul.Content.Layers.FossilShale.Tiles;
using AbyssOverhaul.Content.Layers.FossilShale.WorldGen;
using AbyssOverhaul.Content.NPCs;
using AbyssOverhaul.Core.DataStructures;
using AbyssOverhaul.Core.Utilities;
using CalamityMod;
using CalamityMod.NPCs.Abyss;
using CalamityMod.NPCs.AcidRain;
using CalamityMod.NPCs.SulphurousSea;
using CalamityMod.NPCs.Yharon;
using CalamityMod.Waters;
using Microsoft.Build.Tasks;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.GameContent;
using Terraria.ID;
using static tModPorter.ProgressUpdate;

namespace AbyssOverhaul.Content.Layers.FossilShale
{
    public class FossilShaleLayer : AbyssLayer
    {
        public override string MusicPath => "Assets/Sounds/Music/FossilShaleOst";
        public override int StartHeight => AbyssGenUtils.YAt(0.15f);
        public override int EndHeight => AbyssGenUtils.YAt(0.3f);
        public static FossilShaleLayer Instance => ModContent.GetInstance<FossilShaleLayer>();
        public override ModWaterStyle ModWaterStyle => VoidWater.Instance;
        public override void ModifyGenTasks()
        {
         
           

            AddGenTask("Generating Shale Caves", (layer, progress, config) =>
            {

                int topY = layer.StartHeight;
                int bottomY = layer.EndHeight;
                FossilShaleCaveGen.GenerateFossilShaleCaves(progress, config,
                    AbyssGenUtils.MinX,
                    AbyssGenUtils.MaxX,
                    topY = Instance.StartY,
                    bottomY = Instance.EndY,
                    (ushort)ModContent.TileType<CarbonShale_Tile>(),
                    (ushort)ModContent.WallType<CarbonShale_Wall>(),
                    mainChamberCount: 4,
                    extraChamberCount: 13
                );

            });
            AddGenTask("Fossil Shale: Cyanobacteria Vines", (layer, progress, config) =>
            {
                progress.Message = "Fossil Shale: growing cyanobacteria";

                int minX = AbyssGenUtils.MinX;
                int maxX = AbyssGenUtils.MaxX;
                int topY = layer.StartHeight;
                int bottomY = layer.EndHeight;
                FossilShaleGen.SeedCyanobacteriaVines(minX, maxX, topY, bottomY);
                FossilShaleGen.GrowCyanobacteriaVines(minX, maxX, topY, bottomY);
            });
        }


        public override Dictionary<int, float> NPCSpawnPool => new()
        {
            [ModContent.NPCType<AquaticUrchin>()] = 1.2f,
            [ModContent.NPCType<BabyCannonballJellyfish>()] = 0.8f,
            [ModContent.NPCType<NPCs.BoxJellyfish>()] = 0.8f,
            [ModContent.NPCType<MorayEel>()] = 0.8f,
            [ModContent.NPCType<Sulflounder>()] = 1.2f,
            [ModContent.NPCType<SlabCrab>()] = 1.2f,
            [ModContent.NPCType<ToxicMinnow>()] = 1.2f,
            [ModContent.NPCType<Cuttlefish>()] = 1.2f,
            [ModContent.NPCType<FlakCrab>()] = 0.2f,
        };

    }
}
