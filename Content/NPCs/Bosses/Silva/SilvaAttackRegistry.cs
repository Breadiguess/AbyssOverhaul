using AbyssOverhaul.Content.NPCs.Bosses.Silva.Attacks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Content.NPCs.Bosses.Silva
{
    internal static class SilvaAttackRegistry
    {
        private static Dictionary<SilvaBoss.State, SilvaAttack> _attacks;

        public static IReadOnlyDictionary<SilvaBoss.State, SilvaAttack> Attacks
        {
            get
            {
                _attacks ??= LoadAttacks();
                return _attacks;
            }
        }

        private static Dictionary<SilvaBoss.State, SilvaAttack> LoadAttacks()
        {
            Dictionary<SilvaBoss.State, SilvaAttack> attacks = new();

            Type baseType = typeof(SilvaAttack);
            Assembly assembly = baseType.Assembly;

            foreach (Type type in assembly.GetTypes())
            {
                if (type.IsAbstract || type.IsInterface)
                    continue;

                if (!baseType.IsAssignableFrom(type))
                    continue;

                if (Activator.CreateInstance(type) is not SilvaAttack attack)
                    continue;

                if (attacks.ContainsKey(attack.ID))
                    throw new Exception($"Duplicate Silva attack registered for state {attack.ID}: {type.FullName}");

                attacks.Add(attack.ID, attack);
            }

            return attacks;
        }

        public static SilvaAttack Get(SilvaBoss.State state)
        {
            if (!Attacks.TryGetValue(state, out SilvaAttack attack))
                throw new KeyNotFoundException($"No Silva attack is registered for state {state}");

            return attack;
        }
    }
}
