using AbyssOverhaul.Common.Brain;
using AbyssOverhaul.Common.Brain.Contexts;
using AbyssOverhaul.Common.Brain.SharedModules;
using BreadLibrary.Core;
using BreadLibrary.Core.Verlet;
using Microsoft.Xna.Framework.Graphics;
using Terraria.DataStructures;

namespace AbyssOverhaul.Content.NPCs
{
    internal class Wrothfish : ModNPC, IMultiSegmentNPC
    {
        private VerletChain Chain;
        private List<ExtraNPCSegment> _ExtraHitBoxes;

        private const int MaxLength = 5;

        public ModularNpcBrain<NpcContext> Brain;
        ref List<ExtraNPCSegment> IMultiSegmentNPC.ExtraHitBoxes()
        {
            return ref _ExtraHitBoxes;
        }

        public bool FirstTimeSeeingPlayer = true;
        public override void SetDefaults()
        {
            NPC.friendly = false;
            NPC.Size = new Vector2(40);
            NPC.defense = 30;
            NPC.lifeMax = 1170;
            NPC.noGravity = true;

            InitializeAnythingMissing();
        }

        public void InitializeAnythingMissing()
        {
            _ExtraHitBoxes = new List<ExtraNPCSegment>(MaxLength);
            for (int i = 0; i < MaxLength; i++)
            {
                _ExtraHitBoxes.Add(new(new Rectangle(0, 0, 25, 25)));
            }

            Chain = new(MaxLength, 25, NPC.Center);
            ModularNpcBrain<NpcContext> Brain = new(new());

            Brain.Modules.Add(new FollowPlayerModule()
            {
                MoveSpeed = 9,
                FollowRange = 190
            });
            Brain.Modules.Add(new AvoidSameTypeModule()
            {
                AvoidanceRadius = 4f,
                RequireLineOfSight = true,

            });

            this.Brain = Brain;
        }


        public override void OnSpawn(IEntitySource source)
        {

        }
        public override bool PreAI()
        {
            if (_ExtraHitBoxes is null || Chain is null || Brain is null)
            {
                InitializeAnythingMissing();
            }

            return base.PreAI();
        }
        public override void AI()
        {
            Brain.Update(NPC);

            if (Brain.Context.ClosestPlayer.Center.Distance(NPC.Center) < 190 && FirstTimeSeeingPlayer)
            {
                FirstTimeSeeingPlayer = false;
            }
        }

        public override void PostAI()
        {
            Chain.Simulate(-NPC.velocity, NPC.Center, 0, 0.4f);

            for (int i = 0; i < _ExtraHitBoxes.Count; i++)
            {
                _ExtraHitBoxes[i].Hitbox.Location = (Chain.Positions[i] - _ExtraHitBoxes[i].Hitbox.Size() / 2).ToPoint();
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {

            if (NPC.IsABestiaryIconDummy)
                return false;


            if (Chain is null)
                return false;

            for (int i = 0; i < _ExtraHitBoxes.Count; i++)
            {
                Utils.DrawRect(spriteBatch, _ExtraHitBoxes[i].Hitbox, drawColor);
            }

            return false;
        }


    }
}
