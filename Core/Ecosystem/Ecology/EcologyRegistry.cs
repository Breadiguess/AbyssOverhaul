namespace AbyssOverhaul.Core.Ecosystem.Ecology
{

    public static class EcologyRegistry
    {
        private static readonly HashSet<int> Participants = new();
        private static readonly Dictionary<int, IEcologyParticipant> Providers = new();

        public static bool HasParticipant(int npcType) => Participants.Contains(npcType);

        public static void Register(int npcType, IEcologyParticipant provider)
        {
            if (npcType <= 0 || provider is null)
                return;

            Participants.Add(npcType);
            Providers[npcType] = provider;
        }

        public static void ApplySetup(NPC npc, EcologyGlobalNPC ecology)
        {
            if (Providers.TryGetValue(npc.type, out IEcologyParticipant provider))
                provider.SetupEcology(npc, ecology);
        }

        public static void Clear()
        {
            Participants.Clear();
            Providers.Clear();
        }
    }
}


