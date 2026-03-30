namespace AbyssOverhaul.Core.Ecosystem.TerritorySystem
{
    public static class TerritoryRegistry
    {
        public static readonly HashSet<Territory> _territories = new();

        // No per-call allocations.
        public static IEnumerable<Territory> Territories => _territories;
        public static int Count => _territories.Count;

        public static void Register(Territory territory)
        {
            if (territory is null)
                return;

            _territories.Add(territory);
        }

        public static Territory FindOwnedBy(Entity owner)
        {
            if (owner is null)
                return null;

            foreach (Territory territory in _territories)
            {
                if (ReferenceEquals(territory.Owner, owner))
                    return territory;
            }

            return null;
        }

        public static Territory FindContaining(Vector2 worldPosition)
        {
            Point point = worldPosition.ToPoint();

            foreach (Territory territory in _territories)
            {
                if (territory.Bounds.Contains(point))
                    return territory;
            }

            return null;
        }

        public static Vector2 ClampInside(Territory territory, Vector2 position, float padding = 16f)
        {
            if (territory is null)
                return position;

            Rectangle bounds = territory.Bounds;

            float minX = bounds.Left + padding;
            float maxX = bounds.Right - padding;
            float minY = bounds.Top + padding;
            float maxY = bounds.Bottom - padding;

            if (maxX < minX)
                maxX = minX;
            if (maxY < minY)
                maxY = minY;

            return new Vector2(
                MathHelper.Clamp(position.X, minX, maxX),
                MathHelper.Clamp(position.Y, minY, maxY)
            );
        }

        public static void RemoveDeadTerritories()
        {
            List<Territory> toRemove = new();

            foreach (Territory territory in _territories)
            {
                if (territory.Owner is null || !territory.Owner.active)
                    toRemove.Add(territory);
            }

            foreach (Territory territory in toRemove)
                _territories.Remove(territory);
        }
    }
}
