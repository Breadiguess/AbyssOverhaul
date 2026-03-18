using Microsoft.Xna.Framework;
using System.IO;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace AbyssOverhaul.Content.Items.Accessories.BlackTide
{
    internal class BlackTideEarringNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        public int JudgmentStage;
        public int JudgmentTimer;

        public int AnchorTimer;
        public Vector2 AnchorCenter;

        public int BlacktideBlastHits;

        public bool IsFocused => JudgmentStage >= 1 && JudgmentTimer > 0;
        public bool IsAnchored => AnchorTimer > 0;

        public void ResetJudgment()
        {
            JudgmentStage = 0;
            JudgmentTimer = 0;
        }

        public void ApplyJudgmentHit(NPC npc)
        {
            JudgmentTimer = 60 * 10; // 10 second tracking window
            JudgmentStage = Utils.Clamp(JudgmentStage + 1, 0, 3);

            if (JudgmentStage >= 3)
            {
                AnchorTimer = 60 * 5; // 5 seconds
                AnchorCenter = npc.Center;
            }

            npc.netUpdate = true;
        }

        public override void AI(NPC npc)
        {
            if (JudgmentTimer > 0)
            {
                JudgmentTimer--;
                if (JudgmentTimer <= 0)
                    ResetJudgment();
            }

            if (AnchorTimer > 0)
            {
                AnchorTimer--;

                npc.velocity = Vector2.Zero;
                npc.Center = AnchorCenter;

                if (AnchorTimer <= 0)
                    npc.netUpdate = true;
            }
        }

        public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter writer)
        {
            writer.Write(JudgmentStage);
            writer.Write(JudgmentTimer);
            writer.Write(AnchorTimer);
            writer.WriteVector2(AnchorCenter);
            writer.Write7BitEncodedInt(BlacktideBlastHits);
        }

        public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader reader)
        {
            JudgmentStage = reader.ReadInt32();
            JudgmentTimer = reader.ReadInt32();
            AnchorTimer = reader.ReadInt32();
            AnchorCenter = reader.ReadVector2();
            BlacktideBlastHits = reader.Read7BitEncodedInt();
        }
    }
}