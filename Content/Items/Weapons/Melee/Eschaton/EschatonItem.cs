using AbyssOverhaul.Content.Rarities;
using CalamityMod;
using CalamityMod.Items;
using CalamityMod.Items.BaseItems;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;

namespace AbyssOverhaul.Content.Items.Weapons.Melee.Eschaton
{
    public class EschatonItem : CustomUseProjItem, ILocalizedModType
    {
        public static string Path => ModContent.GetInstance<EschatonItem>().GetPath();
        public new string LocalizationCategory => "Items.Weapons.Melee";
        public static Asset<Texture2D> GlowTex;
        public override void Load()
        {
            GlowTex = ModContent.Request<Texture2D>(this.GetPath() + "_Glow");
        }
        public override void SetDefaults()
        {
            Item.width = 124;
            Item.height = 124;
            Item.damage = 14007; // Feel free to change these 7s as balance requires. The other 7s should stay - Update: no more 2777... :(
            Item.DamageType = DamageClass.MeleeNoSpeed;//TrueMeleeDamageClass.Instance;
            Item.useAnimation = 66;
            Item.useTime = 66;
            Item.useTurn = true;
            Item.knockBack = 12f;
            Item.autoReuse = true;
            Item.value = CalamityGlobalItem.RarityTurquoiseBuyPrice;
            Item.rare = ModContent.RarityType<AbyssalRarity>();

            Item.channel = true;
            Item.shoot = ModContent.ProjectileType<Eschaton_Holdout>();
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.useStyle = ItemUseStyleID.Shoot;
        }
        public override void PostDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, float rotation, float scale, int whoAmI)
        {
            Vector2 DrawPos = Item.Center - Main.screenPosition;
            Vector2 origin = new Vector2(GlowTex.Value.Width / 2f, GlowTex.Value.Height / 2f);
            //Main.EntitySpriteDraw(GlowTex.Value, DrawPos, null, Color.White, rotation, origin, Item.scale, Item.direction.ToSpriteDirection());
            //Item.DrawItemGlowmaskSingleFrame(spriteBatch, rotation, ModContent.Request<Texture2D>(this.GetPath()+"Holdout_Glow").Value);
        }
        public override bool MeleePrefix() => true;

        public override void ModifyTooltips(List<TooltipLine> list)
        {
            //list.FindAndReplace("[GFB]", Lang.SupportGlyphs(this.GetLocalizedValue(Main.zenithWorld ? "TooltipGFB" : "TooltipNormal")));
        }
        public override void AddRecipes()
        {

        }


    }
}
