using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Common.Brain.Interfaces
{
    public interface IThreatAware
    {
        bool HasThreat { get; set; }
        Vector2 ThreatPosition { get; set; }
        float ThreatLevel { get; set; }
        int TimeSinceThreatSeen { get; set; }
    }

    public interface IDisturbanceAware
    {
        bool HasDisturbance { get; set; }
        Vector2 DisturbancePosition { get; set; }
    }

    public interface ICreatureVitals
    {
        float Energy { get; set; }
        float Fatigue { get; set; }
        float Fear { get; set; }
        float Curiosity { get; set; }
        int WanderTimer { get; set; }
        int StuckTime { get; set; }
    }

    public interface IHasPreferredSpacing
    {
        float PreferredSpacing { get; set; }
    }

    public interface IHungry
    {
        float Hunger { get; set; }
    }

    public interface IPreyTargeter
    {
        bool HasPreyTarget { get; set; }
        NPC PreyTargetNpc { get; set; }
        Player PreyTargetPlayer { get; set; }
        Vector2 PreyTargetPosition { get; set; }
        Vector2 PreyTargetVelocity { get; set; }
        int TimeSinceLastSeenPrey { get; set; }
    }

    public interface ISchoolingAware
    {
        bool WantsSchool { get; set; }
        Vector2 SchoolCenter { get; set; }
        int SchoolmateCount { get; set; }
        Vector2 SeparationForce { get; set; }
        Vector2 AlignmentForce { get; set; }
        Vector2 CohesionForce { get; set; }
    }
}
