using BreadLibrary.Core.Verlet;
using Terraria.DataStructures;

namespace AbyssOverhaul.Content.Items.Armor.BlackMoon.Players
{
    public partial class BlackMoonPlayer
    {

        public List<VerletChain> HeadPieces;

        public List<Vector2> HeadPieceOffsets;
        public void UpdateHeadgear(Player Player)
        {
            BlackMoonPlayer black = Player.GetModPlayer<BlackMoonPlayer>();

            if (black.HeadPieces is null || HeadPieceOffsets is null)
            {
                black.HeadPieces = new List<VerletChain>();

                black.HeadPieces.Add(new(11, 3, Player.Top));
                black.HeadPieces.Add(new(12, 3, Player.Top));
                black.HeadPieces.Add(new(8, 3, Player.Top));
                black.HeadPieces.Add(new(9, 3, Player.Top));

                HeadPieceOffsets = new List<Vector2>();
                HeadPieceOffsets.Add(new Vector2(-1, 0));
                HeadPieceOffsets.Add(new Vector2(1, 0));
                HeadPieceOffsets.Add(new Vector2(2, 0));
                HeadPieceOffsets.Add(new Vector2(-2, 0));

            }
           

            for (int i = 0; i < black.HeadPieces.Count; i++)
            {
                Vector2 Velocity = -Player.velocity + Vector2.UnitX * MathF.Sin(Main.GameUpdateCount * 0.01f + i * 10) * 0.2f;
                black.HeadPieces[i].Simulate(Velocity, Player.Top + HeadPieceOffsets[i]*10*Player.direction, -1, 0.7f, constraintIterations: 12, false);
            }

        }






        public VerletChain Tail;

        public void UpdateTail(Player Player)
        {

            if (Tail is null)
            {
                Tail = new VerletChain(10, 4, Player.Center);
            }

            //todo: if player is completely surroundedby tiles or cannot collide with them, remove tile colllision from the tail.
            
            Vector2 TailPos = Player.MountedCenter + Vector2.UnitY * 4;
            Tail.Simulate(Vector2.Zero, TailPos, 1, 0.4f);
            
        }
    }



    public class BlackMoonHeadgearDrawLayer : PlayerDrawLayer
    {


        public override Position GetDefaultPosition()
        {
            return new AfterParent(PlayerDrawLayers.Head);
        }

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            return drawInfo.drawPlayer.head == EquipLoader.GetEquipSlot(Mod, nameof(BlackMoonHelmet), EquipType.Head);
        }
        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Player Owner = drawInfo.drawPlayer;


            if (Owner.TryGetModPlayer<BlackMoonPlayer>(out var black))
            {
                

                if(black.HeadPieces is not null)
                {
                    for (int x = 0; x < black.HeadPieces.Count; x++)
                    {
                        for (int i = 0; i < black.HeadPieces[x].Positions.Length-1; i++)
                        {
                            Vector2 start = black.HeadPieces[x].Positions[i];
                            Vector2 end = black.HeadPieces[x].Positions[i + 1];

                            Utils.DrawLine(Main.spriteBatch, start, end, Color.Green, Color.Green, 3);
                        }
                    }
                }

                if(black.Tail is not null)
                {

                    for(int i = 0;i< black.Tail.Positions.Length - 1; i++)
                    {

                        Vector2 start = black.Tail.Positions[i];
                        Vector2 end = black.Tail.Positions[i + 1];

                        Utils.DrawLine(Main.spriteBatch, start, end, Color.Purple);
                    }
                }
            }




        }
    }
}
