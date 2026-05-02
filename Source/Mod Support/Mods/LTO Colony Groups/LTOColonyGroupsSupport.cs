using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace Better_Work_Tab.ModSupport
{
    /// <summary>
    /// [LTO] Colony Groups compatibility module.
    /// Resolves group membership for pawns via reflection so the assembly stays optional.
    /// Package ID: DerekBickley.LTOColonyGroupsFinal
    /// </summary>
    public sealed class LTOColonyGroupsSupport : ModSupportModuleBase
    {
        public override string PackageId => "DerekBickley.LTOColonyGroupsFinal";
        public override string DisplayName => "[LTO] Colony Groups";

        // TacticalGroups.TacticalGroups (WorldComponent) — holds pawnGroups list
        private Type _worldCompType;
        private FieldInfo _pawnGroupsField;

        // TacticalGroups.ColonistGroup — holds pawns list and curGroupName
        private FieldInfo _colonistGroupPawnsField;
        private FieldInfo _colonistGroupNameField;

        public override void OnModsDetected()
        {
            Debug("[LTO] Detected. Caching reflection handles...");

            _worldCompType = AccessTools.TypeByName("TacticalGroups.TacticalGroups");
            if (_worldCompType == null)
            {
                Debug("[LTO] WorldComponent type not found; skipping integration.");
                return;
            }

            _pawnGroupsField = AccessTools.Field(_worldCompType, "pawnGroups");

            var colonistGroupType = AccessTools.TypeByName("TacticalGroups.ColonistGroup");
            if (colonistGroupType == null)
            {
                Debug("[LTO] ColonistGroup type not found; skipping integration.");
                _worldCompType = null;
                return;
            }

            _colonistGroupPawnsField = AccessTools.Field(colonistGroupType, "pawns");
            _colonistGroupNameField = AccessTools.Field(colonistGroupType, "curGroupName");

            if (_pawnGroupsField == null || _colonistGroupPawnsField == null || _colonistGroupNameField == null)
            {
                Debug("[LTO] Missing expected fields; integration disabled.");
                _worldCompType = null;
                return;
            }

            Debug("[LTO] Reflection cached successfully.");
        }

        /// <summary>
        /// Returns true if the pawn belongs to a group with the given name.
        /// </summary>
        public bool IsInGroup(Pawn pawn, string groupName)
        {
            if (_worldCompType == null || pawn == null || string.IsNullOrEmpty(groupName))
                return false;

            try
            {
                var worldComp = Find.World?.GetComponent(_worldCompType);
                if (worldComp == null)
                    return false;

                var groups = _pawnGroupsField.GetValue(worldComp) as IList;
                if (groups == null)
                    return false;

                foreach (var group in groups)
                {
                    var name = _colonistGroupNameField.GetValue(group) as string;
                    if (!string.Equals(name, groupName, StringComparison.OrdinalIgnoreCase))
                        continue;

                    var pawns = _colonistGroupPawnsField.GetValue(group) as List<Pawn>;
                    if (pawns != null && pawns.Contains(pawn))
                        return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Debug($"[LTO] Error checking group membership for '{pawn?.LabelShort}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Returns all current pawn group names for the UI picker.
        /// </summary>
        public List<string> GetGroupNames()
        {
            var result = new List<string>();

            if (_worldCompType == null)
                return result;

            try
            {
                var worldComp = Find.World?.GetComponent(_worldCompType);
                if (worldComp == null)
                    return result;

                var groups = _pawnGroupsField.GetValue(worldComp) as IList;
                if (groups == null)
                    return result;

                foreach (var group in groups)
                {
                    var name = _colonistGroupNameField.GetValue(group) as string;
                    if (!string.IsNullOrEmpty(name) && !result.Contains(name))
                        result.Add(name);
                }
            }
            catch (Exception ex)
            {
                Debug($"[LTO] Error fetching group names: {ex.Message}");
            }

            return result;
        }
    }
}
