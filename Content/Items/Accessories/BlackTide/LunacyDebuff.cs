namespace AbyssOverhaul.Content.Items.Accessories.BlackTide
{
    internal class LunacyDebuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
        }
    }
}