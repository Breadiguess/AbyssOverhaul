using AbyssOverhaul.Content.Layers.FossilShale.Biome;
using CalamityMod;
using CalamityMod.BiomeManagers;
using CalamityMod.CalPlayer;
using CalamityMod.Items.LoreItems;
using CalamityMod.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Core.ModPlayers
{
    internal class PreventDeathFromJustBeingOutOfWater : ModPlayer
    {
        
        public bool IsInModdedAbyssBiome;

        public override void Load()
        {
            On_Player.InModBiome += On_Player_InModBiome;
        }

        private bool On_Player_InModBiome(On_Player.orig_InModBiome orig, Player self, ModBiome baseInstance)
        {
            if (baseInstance is AbyssLayer1Biome||
                baseInstance is AbyssLayer2Biome||
                baseInstance is AbyssLayer3Biome||
                baseInstance is AbyssLayer4Biome)
                return false;




            return orig(self, baseInstance);
            
        }

        public override void UpdateBadLifeRegen()
        {
            float deathNegativeRegenBonus = 0.25f;
            float calamityDebuffMultiplier = 1f + (CalamityWorld.death ? deathNegativeRegenBonus : 0f);
            if (Player.Calamity().ZoneAbyss)
            {
                if (!Player.IsUnderwater())
                {
                    if (Player.statLife > 100)
                    {
                       

                        Player.lifeRegen += (int)(160D * calamityDebuffMultiplier);
                    }
                }
            }
        }
        
        public override void ResetEffects()
        {
           
           
            if (!Player.InModBiome<FossilShaleBiome>())
            IsInModdedAbyssBiome = false;
        }

       
    }
}
