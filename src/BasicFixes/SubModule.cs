using System.Linq;
using System.Xml;

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors.AiBehaviors;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

using Helpers;
using TaleWorlds.ModuleManager;

using HarmonyLib;

namespace BasicFixes
{
    public class SubModule : MBSubModuleBase
    {
        // fixes are enalbed by default
        private bool discardExcessFood = true;
        private bool recruitFix = true;
        private bool caravanFix = true;
        private bool badFormationProjectionFix = true;
        private bool resizeLooseFormationFix = true;

        public Harmony HarmonyInstance;
        private bool failFamilyFeudQuestFix;
        private bool lordsNotSoldFix;
        private bool unitsRunWhenFormingUp;
        private bool unitsDontUseShieldsWhenFormingUp;
        private bool formationSizeFix;

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            HarmonyInstance = new Harmony("mod.harmony.BasicFixes");
            XmlDocument settingsDoc = MiscHelper.LoadXmlFile(ModuleHelper.GetModuleFullPath("BasicFixes") + "ModuleData/settings.xml");
            if (settingsDoc != null)
            {
                XmlElement documentElement = settingsDoc.DocumentElement;
                XmlNodeList xmlNodeList = documentElement.SelectNodes("/Settings/*");
                foreach (XmlNode node in xmlNodeList)
                {
                    string name = node.Name;
                    switch (name)
                    {
                        case "DiscardExcessFood":
                            bool.TryParse(node.InnerText, out discardExcessFood);
                            break;

                        case "AIRecruitFix":
                            bool.TryParse(node.InnerText, out recruitFix);
                            break;

                        case "CaravanFix":
                            bool.TryParse(node.InnerText, out caravanFix);
                            break;

                        case "BadFormationProjectionFix":
                            bool.TryParse(node.InnerText, out badFormationProjectionFix);
                            break;

                        case "ResizeLooseFormationFix":
                            bool.TryParse(node.InnerText, out resizeLooseFormationFix);
                            break;

                        case "FailFamilyFeudQuestFix":
                            bool.TryParse(node.InnerText, out failFamilyFeudQuestFix);
                            break;

                        case "LordsNotSoldFix":
                            bool.TryParse(node.InnerText, out lordsNotSoldFix);
                            break;

                        case "FormationSizeFix":
                            bool.TryParse(node.InnerText, out formationSizeFix);
                            break;

                        case "UnitsRunWhenFormingUp":
                            bool.TryParse(node.InnerText, out unitsRunWhenFormingUp);
                            break;

                        case "UnitsDontUseShieldsWhenFormingUp":
                            bool.TryParse(node.InnerText, out unitsDontUseShieldsWhenFormingUp);
                            break;

                        default:
                            break;
                    }
                }
            }

            if (badFormationProjectionFix)
            {
                var original = BadFormationProjectionFix.TargetMethod();
                var prefix = typeof(BadFormationProjectionFix).GetMethod("Prefix");
                HarmonyInstance.Patch(original, new HarmonyMethod(prefix));
            }

            if (failFamilyFeudQuestFix)
            {
                var original = FailFamilyFeudQuestFix.TargetMethod();
                var prefix = typeof(FailFamilyFeudQuestFix).GetMethod("Prefix");
                HarmonyInstance.Patch(original, new HarmonyMethod(prefix));
            }

            if (lordsNotSoldFix)
            {
                var original = LordsNotSoldOffFix.TargetMethod();
                var prefix = typeof(LordsNotSoldOffFix).GetMethod("Prefix");
                HarmonyInstance.Patch(original, new HarmonyMethod(prefix));
            }

            if (formationSizeFix)
            {
                var original = Formation_ArrangementOrder_Patch.TargetMethod();
                var prefix = typeof(Formation_ArrangementOrder_Patch).GetMethod("Prefix");
                HarmonyInstance.Patch(original, new HarmonyMethod(prefix));

                if (unitsRunWhenFormingUp)
                {
                    var original2 = HumanAIComponent_AdjustSpeedLimit_Patch.TargetMethod();
                    var prefix2 = typeof(HumanAIComponent_AdjustSpeedLimit_Patch).GetMethod("Prefix");
                    HarmonyInstance.Patch(original2, new HarmonyMethod(prefix2));
                }

                if (unitsDontUseShieldsWhenFormingUp)
                {
                    var original3 = ArrangementOrder_GetShieldDirectionOfUnit_Patch.TargetMethod();
                    var prefix3 = typeof(ArrangementOrder_GetShieldDirectionOfUnit_Patch).GetMethod("Prefix");
                    HarmonyInstance.Patch(original3, new HarmonyMethod(prefix3));
                }
            }
        }

        protected override void InitializeGameStarter(Game game, IGameStarter starter)
        {
            CampaignGameStarter campaignGameStarter = starter as CampaignGameStarter;
            if(campaignGameStarter != null)
            {
                if(discardExcessFood)
                    campaignGameStarter.AddBehavior(new DiscardExcessiveItemsBehavior());

                if (recruitFix)
                {
                    // remove bad AIVisitSettlementBehavior
                    AiVisitSettlementBehavior badBehavior = campaignGameStarter.CampaignBehaviors.FirstOrDefault(x => x is AiVisitSettlementBehavior) as AiVisitSettlementBehavior;
                    if (badBehavior != null)
                        campaignGameStarter.RemoveBehavior(badBehavior);
                    campaignGameStarter.AddBehavior(new AiVisitSettlementBehaviorFixed());
                }

                if (caravanFix)
                    campaignGameStarter.AddBehavior(new CaravansCampaignBehaviorFix());
            }
        }

        public override void OnBeforeMissionBehaviorInitialize(Mission mission)
        {
           MissionAgentSpawnLogic spawnLogic = Mission.Current.GetMissionBehavior<MissionAgentSpawnLogic>();
            if (spawnLogic != null)
            {
                if (resizeLooseFormationFix)
                    mission.AddMissionBehavior(new ResizeLooseFormationFix());

                if (badFormationProjectionFix)
                    mission.AddMissionBehavior(new MissionAgentSpawnLogicFix());
            }

            if (formationSizeFix)
                mission.AddMissionBehavior(new FormationTracker());
        }
    }
}
