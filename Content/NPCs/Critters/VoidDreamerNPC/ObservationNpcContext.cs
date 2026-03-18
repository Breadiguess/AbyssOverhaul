using AbyssOverhaul.Common.Brain.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Content.NPCs.Critters.VoidDreamerNPC
{
    public class ObservationNpcContext : NpcContext
    {
        public bool SawPlayer;
        public bool SawNearbyNpc;
        public bool SawHostileProjectile;
        public bool SawWeaponSwing;

        public Vector2 ObservedPosition;
        public float ObservedDistance;
        public bool HasObservedThing;

        public int TimesinceLastObserved;
        public int ObservationSwapCooldown;


        public Player ObservedPlayer;
        public NPC ObservedNpc;
        public Projectile ObservedProjectile;
        public Entity ObservedObject;


        public float ObservationWindowRadians;
        public float DesiredObservationDistance;

        public override void Update(NPC npc)
        {
            base.Update(npc);

            SawPlayer = false;
            SawNearbyNpc = false;
            SawHostileProjectile = false;
            SawWeaponSwing = false;
            TimesinceLastObserved++;
           
            ObservedDistance = float.MaxValue;
            HasObservedThing = false;

            ObservedPlayer = null;
            ObservedNpc = null;
            ObservedProjectile = null;
        }
    }
}
