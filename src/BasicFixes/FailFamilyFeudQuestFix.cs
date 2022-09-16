using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;
using SandBox.Issues;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace BasicFixes
{
    public class FamilyFeudQuestFix : BasicFix
    {
        public FamilyFeudQuestFix() : base()
        {
            base.SimpleHarmonyPatches.Add(new FamilyFeudIssueBehavior_FamilyFeudIssueQuest_OnMissionEnd_Patch());
        }
    }

    /// <summary>
    /// Problem as described here https://forums.taleworlds.com/index.php?threads/family-feud-quest-fails-if-you-go-to-tournament-before-its-completion.453479/
    /// The vanilla logic for this quest ends with failure when the player falls unconcious for 
    /// any reason, even if it happens outside the quest's target settlement.
    /// </summary>
    [HarmonyPatch]
    public class FamilyFeudIssueBehavior_FamilyFeudIssueQuest_OnMissionEnd_Patch : SimpleHarmonyPatch
    {
        public override string PatchType { get { return "Prefix"; } }

        public override MethodBase TargetMethod
        {
            get
            {
                return typeof(FamilyFeudIssueBehavior.FamilyFeudIssueQuest).GetMethod("OnMissionEnd", BindingFlags.Instance | BindingFlags.NonPublic);
            }
        }

        public static bool Prefix(FamilyFeudIssueBehavior.FamilyFeudIssueQuest __instance, IMission mission)
        {
            FieldInfo fieldInfo = __instance.GetType().GetField("_targetSettlement", BindingFlags.Instance | BindingFlags.NonPublic);
            Settlement targetSettlement = (Settlement)fieldInfo.GetValue(__instance);
            if (Settlement.CurrentSettlement != targetSettlement)
                return false;
            return true;
        }
    }
}
