using BreadLibrary.Core.Verlet;

namespace AbyssOverhaul.Content.Items.Accessories.OmegaBlue_Wings
{
    internal class OmegaBlue_Tails : ModPlayer
    {
        public VerletChain[] Tails = [];

        public bool Active;






        public override void Initialize()
        {
            Tails = new VerletChain[2];
            InitializeTails();
        }

        public override void Load()
        {
            On_Player.UpdateTouchingTiles += UpdateTailsDetour;
        }

        public void InitializeTails()
        {
            for (int i = 0; i < Tails.Length; i++)
                Tails[i] = new VerletChain(20, 4, Player.Center);
        }

        public override void ResetEffects()
        {
            Active = false;
        }

        private void UpdateTailsDetour(On_Player.orig_UpdateTouchingTiles orig, Player self)
        {
            if (self.TryGetModPlayer(out OmegaBlue_Tails omegaBlueTails) && omegaBlueTails.Active)
                omegaBlueTails.Updatetails(self);

            orig(self);
        }

        public void Updatetails(Player player)
        {
            if (!player.TryGetModPlayer(out OmegaBlue_Tails omegaBlueTails))
                return;

            if (omegaBlueTails.Tails is null || omegaBlueTails.Tails.Length < 2 || omegaBlueTails.Tails[0] is null || omegaBlueTails.Tails[1] is null)
                omegaBlueTails.InitializeTails();

            Vector2[] offsets =
            [
                new Vector2(-10f, 0f),
                new Vector2(10f, 0f)
            ];

            for (int i = 0; i < Tails.Length; i++)
            {
                var tail = Tails[i];

                Vector2 rootedOffset = offsets[i] * new Vector2(player.direction, 1f);
                Vector2 root = player.MountedCenter + rootedOffset;

                if (tail.Positions[^1].Distance(player.Center) > 400)
                    for (int x = 0; x < tail.Positions.Length; x++)
                    {
                        tail.Positions[x] = player.Center;
                        tail.OldPositions[x] = player.Center;
                    }


                tail.Simulate(
                    -player.velocity * 0.2f,
                    root,
                    0.8f,
                    0.8f,
                    collideWithTiles: true,
                    collisionRadius: 2);

                Tails[i] = tail;
            }
        }
    }
}