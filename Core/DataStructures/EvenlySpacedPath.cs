using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Core.DataStructures
{
    public class EvenlySpacedTrail
    {
        public Vector2[] Points { get; }

        // Desired distance between each output point.
        public float Spacing { get; set; }

        // How densely we record the head path internally.
        // Usually keep this smaller than Spacing.
        public float RecordStep { get; set; }

        // Exact current head position.
        public Vector2 HeadPosition { get; private set; }

        private readonly List<Vector2> _path = new();
        private bool _initialized;

        public EvenlySpacedTrail(int pointCount, float spacing, float? recordStep = null)
        {
            if (pointCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(pointCount));
            if (spacing <= 0f)
                throw new ArgumentOutOfRangeException(nameof(spacing));

            Points = new Vector2[pointCount];
            Spacing = spacing;
            RecordStep = recordStep ?? MathF.Max(1f, spacing * 0.5f);
        }

        public void Reset(Vector2 position)
        {
            _initialized = true;
            HeadPosition = position;

            _path.Clear();
            _path.Add(position);

            for (int i = 0; i < Points.Length; i++)
                Points[i] = position;
        }

        public void Update(Vector2 newHeadPosition)
        {
            if (!_initialized)
            {
                Reset(newHeadPosition);
                return;
            }

            Vector2 lastRecorded = _path[^1];
            Vector2 delta = newHeadPosition - lastRecorded;
            float dist = delta.Length();

            HeadPosition = newHeadPosition;

            // Record enough intermediate points so the internal path
            // remains stable and accurate through fast movement.
            if (dist > 0.0001f)
            {
                Vector2 dir = delta / dist;
                float step = RecordStep;

                while (step < dist)
                {
                    _path.Add(lastRecorded + dir * step);
                    step += RecordStep;
                }

                _path.Add(newHeadPosition);
            }
            else
            {
                // Keep the most recent point synced exactly to the head.
                _path[^1] = newHeadPosition;
            }

            TrimPath();
            RebuildPoints();
        }

        private void TrimPath()
        {
            float neededLength = Spacing * (Points.Length - 1) + RecordStep * 2f;

            float accumulated = 0f;
            Vector2 current = HeadPosition;

            int keepFromIndex = 0;
            bool foundEnoughLength = false;

            for (int i = _path.Count - 1; i >= 0; i--)
            {
                float segLen = Vector2.Distance(current, _path[i]);
                accumulated += segLen;
                current = _path[i];

                if (accumulated >= neededLength)
                {
                    keepFromIndex = i;
                    foundEnoughLength = true;
                    break;
                }
            }

            // Only trim if we actually have MORE than enough stored path.
            if (foundEnoughLength && keepFromIndex > 0)
                _path.RemoveRange(0, keepFromIndex);
        }

        private void RebuildPoints()
        {
            Points[0] = HeadPosition;

            if (Points.Length == 1)
                return;

            int outIndex = 1;
            float nextSampleDistance = Spacing;
            float walked = 0f;

            Vector2 current = HeadPosition;

            for (int i = _path.Count - 1; i >= 0 && outIndex < Points.Length; i--)
            {
                Vector2 previous = _path[i];
                float segLen = Vector2.Distance(current, previous);

                if (segLen <= 0.0001f)
                {
                    current = previous;
                    continue;
                }

                while (walked + segLen >= nextSampleDistance && outIndex < Points.Length)
                {
                    float t = (nextSampleDistance - walked) / segLen;
                    Points[outIndex] = Vector2.Lerp(current, previous, t);
                    outIndex++;
                    nextSampleDistance += Spacing;
                }

                walked += segLen;
                current = previous;
            }

            // If path is too short, pin the rest to the tail end.
            for (; outIndex < Points.Length; outIndex++)
                Points[outIndex] = current;
        }
    }
}
