using CalamityMod;
using CalamityMod.Cooldowns;

namespace AbyssOverhaul.Content.Items.Accessories.SeaShell
{
    internal class SeashellPlayer : ModPlayer
    {
        private ref readonly System.Collections.Generic.Dictionary<string, CooldownInstance> cooldowns
           => ref Player.Calamity().cooldowns;
        public bool Active = false;
        public bool Visible = true;
        public bool HasShell { get; set; }
        public bool HasBoostedDashFirstFrame { get; private set; }

        public const int MaxCooldown = 60 * 8;
        public override void PostUpdateMiscEffects()
        {
            if (!Active)
                return;

            if (HasShell)
            {
                Player.statDefense += 4;
                if (!Player.wet)
                    Player.maxRunSpeed *= 0.05f;
                else
                    Player.maxRunSpeed *= 1.05f;

                //reduce dash velocity while equipped and has shell.
                if (Player.dashDelay == -1)
                {
                    if (!HasBoostedDashFirstFrame)
                    {
                        if (!Player.wet)
                            Player.velocity.X *= 0.9f;

                        HasBoostedDashFirstFrame = true;
                    }
                }
                else
                    HasBoostedDashFirstFrame = false;
            }
            else
            {

                if (!Player.Calamity().cooldowns.ContainsKey(SeaShellCooldown.ID))
                    HasShell = true;


                bool dashStart = (Player.dashDelay == -1 && ((!Player.Calamity().HasCustomDash && Player.Calamity().IsFirstDashFrame) || (Player.Calamity().HasCustomDash && Player.Calamity().UsedDash.DashTimeAdjustedForStartup == 1)));
                if (dashStart)
                {

                }




                Player.maxRunSpeed *= 1.1f;
                if (Player.dashDelay == -1)
                {
                    if (!HasBoostedDashFirstFrame)
                    {
                        Player.velocity.X *= 1.20f;
                        HasBoostedDashFirstFrame = true;
                    }
                }
                else
                    HasBoostedDashFirstFrame = false;

            }
        }


        public override void OnHurt(Player.HurtInfo info)
        {
            if (HasShell)
            {
                //remove shell and start cooldown
                if (!Player.Calamity().cooldowns.ContainsKey(SeaShellCooldown.ID))
                    Player.AddCooldown(SeaShellCooldown.ID, MaxCooldown);

                Player.Heal(10);
                HasShell = false;
            }
        }

        public override void ResetEffects()
        {
            Active = false;
        }
    }
}
