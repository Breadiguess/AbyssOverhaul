namespace AbyssOverhaul.Content.Items.Armor.BlackMoon.Players
{
    public partial class BlackMoonPlayer : ModPlayer
    {
        public bool Active;

        public override void ResetEffects()
        {
            Active = false;
        }


        public override void Load()
        {
            On_Player.UpdateTouchingTiles += UpdateHeadKelp;
        }

        private void UpdateHeadKelp(On_Player.orig_UpdateTouchingTiles orig, Player self)
        {
            orig(self);


            if (self.TryGetModPlayer<BlackMoonPlayer>(out var black))
            {
                black.UpdateHeadgear(self);
                black.UpdateTail(self);
            }
        }
        public Vector2 PreTeleportLoc;
        public override void PreUpdateMovement()
        {
            if (!Active)
                return;

            if (TeleportDelayTime > 0)
            {
                Player.velocity *= 0;
                HasTeleported = false;
                TeleportDelayTime--;
                PreTeleportLoc = Player.Center;
                //CameraPanSystem.PanTowards(TeleportLoc - Vector2.UnitY * 20, 1 - Utilities.InverseLerp(0, 10, TeleportDelayTime));
            }
            else if (!HasTeleported && !TeleportLoc.Equals(Vector2.Zero))
            {

                //CameraPanSystem.PanTowards(PreTeleportLoc, 1);
                Player.position = TeleportLoc - new Vector2(Player.Size.X / 2, Player.Size.Y + 8);

                Point? tilesToHit;

                for (int i = -3; i < 4; i++)
                {
                    Vector2 Start = Player.Center + Vector2.UnitX * i * 10;

                    tilesToHit = LineAlgorithm.RaycastTo(Start, Start + Vector2.UnitY * 600, debug: true);
                    if (tilesToHit.HasValue)
                    {
                        Collision.HitTiles(tilesToHit.Value.ToWorldCoordinates() - Vector2.UnitY * 32, new Vector2(0, 40), 10, 10);
                    }
                }

                HasTeleported = true;
            }


        }


        public int TeleportDelayTime;
        public bool HasTeleported;
        private Vector2 TeleportLoc;
        public override void ArmorSetBonusActivated()
        {






            if (TeleportDelayTime > 0)
                return;

            Vector2 StartPos = Player.Center;
            Vector2 EndPos = StartPos + Vector2.UnitX.RotatedBy(-new Vector2(Player.velocity.X * 0.2f, -4).ToRotation()) * 700;
            Point? HitTile = LineAlgorithm.RaycastTo(StartPos, EndPos, debug: true);


            if (HitTile.HasValue)
            {
                if (HitTile.Value.ToWorldCoordinates().Distance(Player.Center) > 4)

                {


                    TeleportLoc = HitTile.Value.ToWorldCoordinates();
                    TeleportDelayTime = 10;
                }



            }

        }
    }
}
