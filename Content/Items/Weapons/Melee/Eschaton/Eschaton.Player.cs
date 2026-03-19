namespace AbyssOverhaul.Content.Items.Weapons.Melee.Eschaton
{
    internal class EschatonPlayer : ModPlayer
    {
        public int FinalityStacks;
        public const int MaxFinalityStacks = 9;

        private int finalityStackCooldown;

        private int FinalityGainCooldown;
        public override float UseSpeedMultiplier(Item item)
        {
            if (item.type == ModContent.ItemType<EschatonItem>())
                return 1f + FinalityStacks / (float)MaxFinalityStacks*1.2f;

            return 1f;
        }

        public override void ModifyItemScale(Item item, ref float scale)
        {
            if (item.type == ModContent.ItemType<EschatonItem>())
            {
                if (Player.itemAnimation == Player.itemAnimationMax)
                    scale *= 1f + FinalityStacks / (float)MaxFinalityStacks;

            }
        }

        public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (proj.type != ModContent.ProjectileType<Eschaton_Holdout>())
                return;

            if (FinalityStacks < MaxFinalityStacks && FinalityGainCooldown<=0)
                FinalityStacks++;
            FinalityGainCooldown = 30;
            finalityStackCooldown = 120;
        }

        public override void PostUpdate()
        {
            if (FinalityGainCooldown > 0)
                FinalityGainCooldown--;

            if (finalityStackCooldown > 0)
            {
                finalityStackCooldown--;
            }
            else if (FinalityStacks > 0)
            {
                FinalityStacks--;
                finalityStackCooldown = 60;
            }
        }

        public override void UpdateDead()
        {
            FinalityStacks = 0;
            finalityStackCooldown = 0;
        }
    }
}