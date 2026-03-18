namespace AbyssOverhaul.Common.OldBrain
{
    public sealed class NpcNeeds
    {
        public float Hunger;
        public float Fear;
        public float Social;
        public float Fatigue;
        public float Curiosity;

        public void Update(NpcContext ctx, NpcMemory memory)
        {
            Hunger = MathHelper.Clamp(Hunger + 0.0008f, 0f, 1f);

            float fearTarget = ctx.LocalDanger;
            Fear = MathHelper.Lerp(Fear, fearTarget, 0.08f);

            float socialTarget = ctx.NearbySchoolmates.Count <= 1 ? 1f : 0.15f;
            Social = MathHelper.Lerp(Social, socialTarget, 0.03f);

            float fatigueGain = ctx.InWater ? 0.0002f : 0.0005f;
            Fatigue = MathHelper.Clamp(Fatigue + fatigueGain, 0f, 1f);

            Curiosity = MathHelper.Clamp(Curiosity + 0.0003f, 0f, 1f);
        }
    }
}