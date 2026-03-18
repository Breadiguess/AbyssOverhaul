namespace AbyssOverhaul.Common.Brain
{
    public struct NpcDirective
    {
        public float Score;
        public Vector2 DesiredVelocity;
        public bool WantsControl;
        public string DebugName;
        public string DebugInfo;

        public static NpcDirective None => new()
        {
            Score = float.MinValue,
            DesiredVelocity = Vector2.Zero,
            WantsControl = false,
            DebugName = "None",
            DebugInfo = ""
        };
    }
}