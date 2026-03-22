using AbyssOverhaul.Common.Brain.AbyssOverhaul.Common.Brain;
using AbyssOverhaul.Common.Brain.Contexts;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;

namespace AbyssOverhaul.Common.Brain
{
    public class ModularNpcBrain<TContext> where TContext : NpcContext
    {
        public List<INpcSensor<TContext>> Sensors = new();
        public List<INpcModule<TContext>> Modules = new();
        public TContext Context;

        public string CurrentModuleName = "None";
        public string CurrentDebugInfo = "";
        public Vector2 LastDesiredVelocity;

        public ModularNpcBrain(TContext context)
        {
            Context = context;
        }

        public void Update(NPC npc)
        {
            Context.Update(npc);

            foreach (var sensor in Sensors)
                sensor.Update(Context);

            NpcDirective best = NpcDirective.None;

            foreach (var module in Modules)
            {
                NpcDirective result = module.Evaluate(Context);

                if (!result.WantsControl)
                    continue;

                if (result.Score > best.Score)
                    best = result;
            }

            if (best.WantsControl)
            {
                LastDesiredVelocity = best.DesiredVelocity;

                ApplySteering(npc, best.DesiredVelocity);

                CurrentModuleName = best.DebugName;
                CurrentDebugInfo = best.DebugInfo ?? "";
            }
            else
            {
                CurrentModuleName = "None";
                CurrentDebugInfo = "";
                LastDesiredVelocity = Vector2.Zero;
            }
        }
        private static void ApplySteering(NPC npc, Vector2 desiredVelocity)
        {
            // Difference between where we are moving and where the brain wants to move.
            Vector2 delta = desiredVelocity - npc.velocity;

            // Cap how much the brain can change velocity per tick.
            // This is the key thing that lets knockback survive.
            float maxAcceleration = 0.18f;

            if (delta.LengthSquared() > maxAcceleration * maxAcceleration)
                delta = delta.SafeNormalize(Vector2.Zero) * maxAcceleration;

            npc.velocity += delta;

            // Optional light drag when there is no meaningful desired movement.
            // Keeps idle creatures from drifting forever after knockback.
            if (desiredVelocity.LengthSquared() <= 0.001f)
                npc.velocity *= 0.985f;
        }

        public void DrawContextDebug(SpriteBatch spriteBatch, Vector2 DrawPos)
        {
            if (this.Context is null)
                return;

            string debugText = BuildContextDebugText(this.Context);



            Utils.DrawBorderString(spriteBatch, debugText, DrawPos, Color.White, 1);
        }
        private static string BuildContextDebugText(object context)
        {
            StringBuilder sb = new();
            Type type = context.GetType();

            sb.AppendLine(type.Name);

            // Public instance fields
            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (FieldInfo field in fields)
            {
                object value = field.GetValue(context);
                sb.AppendLine($"{field.Name}: {FormatDebugValue(value)}");
            }

            // Public instance properties with getters and no index parameters
            PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo prop in properties)
            {
                if (!prop.CanRead || prop.GetIndexParameters().Length > 0)
                    continue;

                object value;
                try
                {
                    value = prop.GetValue(context);
                }
                catch
                {
                    continue;
                }

                sb.AppendLine($"{prop.Name}: {FormatDebugValue(value)}");
            }

            return sb.ToString();
        }

        private static string FormatDebugValue(object value)
        {
            if (value is null)
                return "null";

            switch (value)
            {
                case float f:
                    return f.ToString("0.00");

                case double d:
                    return d.ToString("0.00");

                case Vector2 v:
                    return $"({v.X:0.0}, {v.Y:0.0})";

                case Entity e:
                    return $"{e.GetType().Name}#{e.whoAmI}";

                case Enum:
                    return value.ToString();

                default:
                    return value.ToString();
            }
        }
    }
}