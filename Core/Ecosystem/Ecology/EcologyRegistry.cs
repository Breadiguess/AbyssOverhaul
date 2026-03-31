namespace AbyssOverhaul.Core.Ecosystem.Ecology
{

    public static class EcologyRegistry
    {
        private static readonly HashSet<int> Participants = new();
        private static readonly Dictionary<int, IEcologyParticipant> Providers = new();
        private static readonly Dictionary<int, SpeciesEcologyDefinition> SpeciesDefinitions = new();

        public static bool HasParticipant(int npcType) => Participants.Contains(npcType);

        public static SpeciesEcologyDefinition GetSpecies(int npcType)
        {
            SpeciesDefinitions.TryGetValue(npcType, out var def);
            return def;
        }

        public static SpeciesEcologyDefinition GetOrCreateSpecies(int npcType)
        {
            if (!SpeciesDefinitions.TryGetValue(npcType, out var def))
            {
                def = new SpeciesEcologyDefinition(npcType);
                SpeciesDefinitions[npcType] = def;
            }

            return def;
        }

        public static void Register(int npcType, IEcologyParticipant provider)
        {
            if (npcType <= 0 || provider is null)
                return;

            Participants.Add(npcType);
            Providers[npcType] = provider;

            SpeciesEcologyDefinition def = GetOrCreateSpecies(npcType);
            provider.SetSpeciesEcology(def);
        }

        public static void ApplyIndividualSetup(NPC npc, EcologyGlobalNPC ecology)
        {
            if (npc is null || ecology is null)
                return;

            if (Providers.TryGetValue(npc.type, out IEcologyParticipant provider))
                provider.SetupIndividualEcology(npc, ecology);
        }

        public static void Clear()
        {
            Participants.Clear();
            Providers.Clear();
            SpeciesDefinitions.Clear();
        }
    }
}


