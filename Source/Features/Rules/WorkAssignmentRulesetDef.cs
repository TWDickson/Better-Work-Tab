using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using RimWorld;
using UnityEngine;
using Verse;

namespace Better_Work_Tab.Features.Rules
{
    /// <summary>
    /// This class handles the automatic work assignment process for pawns based on a set of defined rules.
    /// </summary>
    public class WorkAssignmentRulesetDef : Def
    {
        //private readonly BetterWorkTabSettings _settings;
        public bool ResetBeforeApplying = true;
        public List<WorkAssignmentRule> Rules = new List<WorkAssignmentRule>();

        public WorkAssignmentRulesetDef(string rulesetName, List<WorkAssignmentParameters> parameters, bool resetBeforeApplying = true)
        {
            label = rulesetName;
            ResetBeforeApplying = resetBeforeApplying;

            foreach (var p in parameters)
            {
                Rules.Add(new WorkAssignmentRule(p));
            }

        }

        public WorkAssignmentRulesetDef(string rulesetName, List<WorkAssignmentRule> rules, bool resetBeforeApplying = true)
        {
            label = rulesetName;
            Rules = rules;
            ResetBeforeApplying = resetBeforeApplying;
        }

        public WorkAssignmentRulesetDef()
        {
            //default constructor for xml loading
        }

        public static void SetAllToZero()
        {
            new WorkAssignmentRulesetDef("Reset", new List<WorkAssignmentRule> {
                        new WorkAssignmentRule(new WorkAssignmentParameters("Reset", 0, allowOverwritingHigherPriority: true))
                    }).ApplyAutoAssignments();
        }

        /// <summary>
        /// Applies all configured auto-assignment rules to the free colonists on the current map.
        /// </summary>
        public void ApplyAutoAssignments()
        {
            Log.Message("Here 1");
            var map = Find.CurrentMap;
            if (map == null) return;
            Log.Message("Here 2");
            Find.PlaySettings.useWorkPriorities = true;
            Log.Message("Here 3");
            var pawns = map.mapPawns.FreeColonists.ToList();
            if (pawns.Count == 0) return;
            Log.Message("Here 4");
            var allWorkTypes = DefDatabase<WorkTypeDef>.AllDefsListForReading.OrderBy(wt => wt.naturalPriority).Reverse().ToList();
            allWorkTypes.RemoveDuplicates();
            Log.Message("Here 5");

            foreach (var rule in Rules)
            {
                Log.Message($"Applying rule: {rule.Parameters.RuleName}");
                foreach (var worktype in allWorkTypes)
                {
                    Log.Message($"Considering work type: {worktype.defName}");
                    if (rule.Parameters.Worktype != null)
                    {

                        //if there's a rule that applies to only one worktype, skip all others.
                        if (rule.Parameters.Worktype != worktype)
                            continue;
                    }
                    Log.Message($"Passed worktype check for: {worktype.defName}");


                    //if (rule.Parameters.WorktypeDefNameIgnoreIfNonexistant != "")
                    //{
                    //    //if there's a rule that applies to only one ignorable worktype, skip all others.
                    //    if (!DefDatabase<WorkTypeDef>.AllDefs.Contains(DefDatabase<WorkTypeDef>.GetNamedSilentFail(rule.Parameters.WorktypeDefNameIgnoreIfNonexistant)))
                    //        continue;
                    //}
                    //Log.Message(rule.Parameters.WorktypeDefNameIgnoreIfNonexistant == "" ? "No ignore worktype defined." : $"Ignore worktype defined: {rule.Parameters.WorktypeDefNameIgnoreIfNonexistant}");
                    List<Pawn> pawnsForThisWorktype = new List<Pawn>();

                    Log.Message($"Applying rule: {rule.Parameters.RuleName} to work type: {worktype.defName}");
                    //Log.Message($"Auto-assigning work type: {worktype.defName}");
                    foreach (var pawn in pawns)
                    {
                        Log.Message($"Considering pawn: {pawn.NameShortColored} for work type: {worktype.defName}");
                        if (pawn.workSettings == null) continue;
                        bool pawnAlreadyAssigned = pawn.workSettings.GetPriority(worktype) > 0;
                        //// Apply all rules
                        if (rule.Apply(pawn, pawns, worktype))
                        {
                            //Log.Message("assigned " + worktype.defName +" to " + pawn.NameShortColored +". Skipping remaining pawns.");
                            //Apply returns true if the rest of the pawns should be skipped for this worktype
                            break;
                        }
                        if(rule.Parameters.RandomIfMultiple && pawn.workSettings.GetPriority(worktype) > 0 && !pawnAlreadyAssigned)
                        {
                            //this was assigned. add to list for potential randomization later.
                            pawnsForThisWorktype.Add(pawn);
                        }
                    }

                    if (rule.Parameters.RandomIfMultiple && pawnsForThisWorktype.Count > 0)
                    {
                        //copy the list so we can sort it
                        //filter to only those with a priority that has been set

                        //reset before reassigning for randomization
                        foreach (var p in pawnsForThisWorktype)
                        {
                            Log.Message($"Resetting {worktype.defName} for {p.NameShortColored} before random assignment.");
                            //if (p.workSettings.GetPriority(worktype) == rule.Parameters.Priority)
                            p.workSettings.SetPriority(worktype, 0);
                        }
                        //directly ripped straight out of rimworld but what can I do? it's a mod lol.
                        pawnsForThisWorktype.Where(p => !p.WorkTypeIsDisabled(worktype)).InRandomOrder().First().workSettings.SetPriority(worktype, rule.Parameters.Priority);
                    }

                    if (rule.Parameters.FailedToApplyFallback != null && !pawns.Where(p => { return p.workSettings.GetPriority(worktype) > 0; }).Any())
                    {
                        Log.Message($"No pawn could be assigned to work type: {worktype.defName}. Applying fallback.");
                        foreach (var pawn in pawns)
                            new WorkAssignmentRule(rule.Parameters.FailedToApplyFallback).Apply(pawn, pawns, worktype);
                    }

                }
            }
        }
    }
}