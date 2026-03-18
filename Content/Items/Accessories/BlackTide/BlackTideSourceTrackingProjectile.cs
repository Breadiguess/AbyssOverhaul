using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace AbyssOverhaul.Content.Items.Accessories.BlackTide
{
    internal class BlackTideSourceTrackingProjectile : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        public int SourceNpcIndex = -1;

        public override void OnSpawn(Projectile projectile, IEntitySource source)
        {
            SourceNpcIndex = -1;

            if (source is EntitySource_Parent parent && parent.Entity is NPC npc)
                SourceNpcIndex = npc.whoAmI;

            else if (source is EntitySource_Misc misc)
            {
                // Optional extra handling if you have custom spawn pipelines.
            }
        }
    }
}