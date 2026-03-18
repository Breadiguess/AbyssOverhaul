using AbyssOverhaul.Content.Items.Armor.BlackMoon.Players;
using AbyssOverhaul.Content.Rarities;
using CalamityMod.Items;
using CalamityMod.Items.Armor.OmegaBlue;
using CalamityMod.Rarities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Content.Items.Armor.BlackMoon
{
    [AutoloadEquip(EquipType.Head)]
    internal class BlackMoonHelmet: ModItem
    {
        public override void SetStaticDefaults()
        {
            ArmorIDs.Head.Sets.DrawHead[Item.headSlot] = false;
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.value = CalamityGlobalItem.RarityPureGreenBuyPrice;
            Item.defense = 19;
            Item.rare = ModContent.RarityType<AbyssalRarity>();
        }
        public override bool IsArmorSet(Item head, Item body, Item legs) => body.type == ModContent.ItemType<BlackMoonBreastplate>() && legs.type == ModContent.ItemType<BlackMoonGreaves>();

        public override void UpdateEquip(Player player)
        {
            player.ignoreWater = true;

            //player.GetDamage<GenericDamageClass>() += DamageBoost;
            //player.GetCritChance<GenericDamageClass>() += CritBoost;
        }

        public override void UpdateArmorSet(Player player)
        {
            player.GetModPlayer<BlackMoonPlayer>().Active = true;
        }
        public override void EquipFrameEffects(Player player, EquipType type)
        {
            
        }
    }
}
