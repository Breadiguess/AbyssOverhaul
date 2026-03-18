using AbyssOverhaul.Core.ModPlayers;
using AbyssOverhaul.Core.Systems;
using CalamityMod.Items.Accessories;
using CalamityMod.Items.Armor.FathomSwarmer;
using CalamityMod.Items.Armor.Mollusk;
using CalamityMod.Items.Armor.OmegaBlue;
using Terraria;
using Terraria.ID;

namespace AbyssOverhaul.Core
{
    internal class PressureResistanceItems : GlobalItem
    {
        private class _PressureResistanceLoad : ModSystem
        {
            //TODO: adjust pressure resistance based on stuff like "extra oxygen" and "defense" and "water mobility"
            public override void PostSetupContent()
            {
                PressureResistanceItems.AddPrItem(ItemID.DivingHelmet, 6f);
                PressureResistanceItems.AddPrItem(ItemID.DivingGear, 8f);
                PressureResistanceItems.AddPrItem(ItemID.Flipper, 4f);
                PressureResistanceItems.AddPrItem(ItemID.JellyfishNecklace, 6f);
                PressureResistanceItems.AddPrItem(ItemID.FloatingTube, 3f);

                PressureResistanceItems.AddPrItem(ModContent.ItemType<DepthCharm>(), 4f);
                PressureResistanceItems.AddPrItem(ModContent.ItemType<AnechoicPlating>(), 7f);
                PressureResistanceItems.AddPrItem(ItemID.JellyfishDivingGear, 14f);
                PressureResistanceItems.AddPrItem(ItemID.ArcticDivingGear, 20f);
                PressureResistanceItems.AddPrItem(ItemID.CelestialShell, 23f);
                PressureResistanceItems.AddPrItem(ItemID.MoonShell, 18f);
                PressureResistanceItems.AddPrItem(ItemID.NeptunesShell, 14f);

                PressureResistanceItems.AddPrItem(ModContent.ItemType<Baroclaw>(), 8f);
                PressureResistanceItems.AddPrItem(ModContent.ItemType<DiamondOfTheDeep>(), 8f);

                PressureResistanceItems.AddPrItem(ModContent.ItemType<AbyssalDivingGear>(), 30f);
                PressureResistanceItems.AddPrItem(ModContent.ItemType<AbyssalDivingSuit>(), 90f);

                PressureResistanceItems.AddPrItem(ModContent.ItemType<OmegaBlueHelmet>(), 30f);
                PressureResistanceItems.AddPrItem(ModContent.ItemType<OmegaBlueChestplate>(), 30f);
                PressureResistanceItems.AddPrItem(ModContent.ItemType<OmegaBlueTentacles>(), 30f);

                PressureResistanceItems.AddPrItem(ModContent.ItemType<FathomSwarmerVisage>(), 12f);
                PressureResistanceItems.AddPrItem(ModContent.ItemType<FathomSwarmerBreastplate>(), 12f);
                PressureResistanceItems.AddPrItem(ModContent.ItemType<FathomSwarmerBoots>(), 12f);



                PressureResistanceItems.AddPrItem(ModContent.ItemType<MolluskShellplate>(), 8f);
                PressureResistanceItems.AddPrItem(ModContent.ItemType<MolluskShellmet>(), 8f);

                PressureResistanceItems.AddPrItem(ModContent.ItemType<MolluskShelleggings>(), 8f);

            }
        }
        public static Dictionary<int, float> ItemsWithPressureResistance = [];
        public static void AddPrItem(int Item, float Strength)
        {
            ItemsWithPressureResistance.TryAdd(Item, Strength);
        }
        public override bool InstancePerEntity =>  true;
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
        {
            return ItemsWithPressureResistance.ContainsKey(entity.type);
        }
        public override void UpdateEquip(Item item, Player player)
        {
            base.UpdateEquip(item, player);

            if (!ItemsWithPressureResistance.ContainsKey(item.type))
                return;

            if(player.TryGetModPlayer<PressurePlayer>(out var pressure))
            {
                ItemsWithPressureResistance.TryGetValue(item.type, out var pressureR);
                pressure.PressureResistance += pressureR;
            }
        }
    }
}
