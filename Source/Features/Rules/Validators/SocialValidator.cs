using Better_Work_Tab.ModSupport;
using RimWorld;
using Better_Work_Tab.Features.Rules;
using System.Linq;
using Verse;

namespace Better_Work_Tab.Features.Rules.Validators
{
    /// <summary>
    /// Ensures work is assigned based on the pawn's social and relational status.
    /// Checks:
    /// - If the pawn has a child on the map.
    /// - If the pawn holds a required ideology role.
    /// - If the pawn belongs to a specific LTO Colony Group.
    /// </summary>
    public static class SocialValidator
    {
        public static bool Validate(Pawn pawn, WorkAssignmentParameters p)
        {
            if (p.HasChildOnMap)
            {
                var map = Find.CurrentMap;
                if (map == null)
                    return false;

                bool foundChild = false;
                foreach (var child in map.mapPawns.FreeColonists
                    .Where(ch => (int)ch.DevelopmentalStage < (int)DevelopmentalStage.Adult))
                {
                    if (child == pawn)
                        continue;
                    if (child.GetFather() == pawn || child.GetMother() == pawn)
                    {
                        foundChild = true;
                        break;
                    }
                }

                if (!foundChild)
                    return false;
            }

            // Ideology role requirement — pawn must hold the specified role in their ideology
            if (p.RequiredIdeoRole != null)
            {
                if (!ModsConfig.IdeologyActive || pawn.Ideo == null)
                    return false;

                bool hasRole = pawn.Ideo.PreceptsListForReading
                    .OfType<Precept_Role>()
                    .Any(r => r.def == p.RequiredIdeoRole && r.ChosenPawnSingle() == pawn);

                if (!hasRole)
                    return false;
            }

            // LTO Colony Groups membership
            if (!string.IsNullOrEmpty(p.ColonyGroupName) &&
                !ModSupportManager.IsInColonyGroup(pawn, p.ColonyGroupName))
                return false;

            return true;
        }
    }
}