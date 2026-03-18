namespace AbyssOverhaul.Common.OldBrain
{
    public sealed class NpcMemory
    {
        public Vector2 LastSeenFoodPosition;
        public bool HasSeenFoodRecently;
        public int FoodMemoryTime;

        public Vector2 LastSeenThreatPosition;
        public bool HasSeenThreatRecently;
        public int ThreatMemoryTime;

        public Vector2 WanderAnchor;
        public Vector2 HomePosition;

        public int FailedPathingTime;
        public int FeedCooldown;
        public int PanicTime;

        public PersonalityProfile Personality = new();

        public void Tick()
        {
            if (FoodMemoryTime > 0)
                FoodMemoryTime--;
            else
                HasSeenFoodRecently = false;

            if (ThreatMemoryTime > 0)
                ThreatMemoryTime--;
            else
                HasSeenThreatRecently = false;

            if (FeedCooldown > 0)
                FeedCooldown--;

            if (PanicTime > 0)
                PanicTime--;
        }
    }

    public sealed class PersonalityProfile
    {
        public float Boldness = 0.5f;
        public float Sociability = 0.7f;
        public float Curiosity = 0.4f;
        public float Aggression = 0.3f;
    }
}