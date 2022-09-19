using System;
using System.Collections.Generic;
using System.Xml;
using System.Reflection;

using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

using Helpers;
using TaleWorlds.ModuleManager;

using HarmonyLib;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem;
using System.Linq;
using TaleWorlds.ObjectSystem;

namespace BasicFixes
{
    public abstract class SimpleHarmonyPatch
    {
        public abstract MethodBase TargetMethod { get; }

        public abstract string PatchType { get; }
    }

    public class BasicFix
    {
        protected bool _isEnabled;

        public delegate bool MissionCondition(Mission mission);
        public delegate void MissionConsequence(Mission mission);
        public delegate bool CampaignBehaviorCondition(Game game, IGameStarter starter);
        public delegate void CampaignBehaviorConsequence(Game game, IGameStarter starter);

        protected List<SimpleHarmonyPatch> SimpleHarmonyPatches;
        protected List<Tuple<MissionCondition, MissionConsequence>> MissionLogics;
        protected List<Tuple<CampaignBehaviorCondition, CampaignBehaviorConsequence>> CampaignBehaviors;

        public BasicFix(bool isEnabled)
        {
            _isEnabled = isEnabled;
            SimpleHarmonyPatches = new List<SimpleHarmonyPatch>();
            MissionLogics = new List<Tuple<MissionCondition, MissionConsequence>>();
            CampaignBehaviors = new List<Tuple<CampaignBehaviorCondition, CampaignBehaviorConsequence>>();
        }

        public void PatchAll(Harmony instance)
        {
            foreach(SimpleHarmonyPatch patch in SimpleHarmonyPatches)
            {
                var original = patch.TargetMethod;
                string patchType = patch.PatchType;
                Type type = patch.GetType();
                var prefix = patchType == "Prefix" ? new HarmonyMethod(type.GetMethod(patchType)) : null;
                var postfix = patchType == "Postfix" ? new HarmonyMethod(type.GetMethod(patchType)) : null;
                var transpiler = patchType == "Transpiler" ? new HarmonyMethod(type.GetMethod(patchType)) : null;
                var finalizer = patchType == "Finalizer" ? new HarmonyMethod(type.GetMethod(patchType)) : null;
                instance.Patch(original, prefix, postfix, transpiler, finalizer);
            }
        }

        public void AddCampaignBehaviors(Game game, IGameStarter starter)
        {
            foreach (Tuple<CampaignBehaviorCondition, CampaignBehaviorConsequence> pair in CampaignBehaviors)
            {
                if (pair.Item1(game, starter))
                    pair.Item2(game, starter);
            }
        }

        public void AddMissionLogics(Mission mission)
        {
            foreach(Tuple<MissionCondition, MissionConsequence> pair in MissionLogics)
            {
                if (pair.Item1(mission))
                    pair.Item2(mission);
            }
        }
    }

    public class SubModule : MBSubModuleBase
    {
        // fixes are enalbed by default
        #region Module Settings
        public static bool discardExcessFood = true;
        public static bool recruitFix = true;
        public static bool caravanFix = true;
        public static bool badFormationProjectionFix = true;
        public static bool resizeLooseFormationFix = true;
        public static bool failFamilyFeudQuestFix = true;
        public static bool lordsNotSoldFix = true;
        public static bool unitsRunWhenFormingUp = true;
        public static bool unitsDontUseShieldsWhenFormingUp = true;
        public static bool formationSizeFix = true;
        public static bool bouncingScrollablePanelFix = true;
        public static bool skinnyFormations = true;
        public static bool throwablePilum = true;
        #endregion

        public Harmony HarmonyInstance;
        private List<BasicFix> basicFixes;
        private bool _thing;

        public SubModule()
        {
            _thing = false;
        }

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();

            basicFixes = new List<BasicFix>();
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

                        case "SkinnyFormations":
                            bool.TryParse(node.InnerText, out skinnyFormations);
                            break;

                        case "BouncingScrollablePanelFix":
                            bool.TryParse(node.InnerText, out bouncingScrollablePanelFix);
                            break;

                        case "ThrowablePilum":
                            bool.TryParse(node.InnerText, out throwablePilum);
                            break;

                        default:
                            break;
                    }
                }
            }

            #region Campaign Fixes
            basicFixes.Add(new DiscardExcessiveFoodFix(discardExcessFood));
            basicFixes.Add(new FamilyFeudQuestFix(failFamilyFeudQuestFix));
            basicFixes.Add(new AiVisitSettlementBehaviorFix(recruitFix));
            basicFixes.Add(new CaravansCampaignBehaviorFix(caravanFix));
            #endregion

            #region Mission Fixes
            basicFixes.Add(new LordsNotSoldOffFix(lordsNotSoldFix));
            basicFixes.Add(new MissionAgentSpawnLogicFix(badFormationProjectionFix));
            basicFixes.Add(new BattleLineFix(formationSizeFix));
            basicFixes.Add(new ResizeLooseFormationFix(resizeLooseFormationFix));
            basicFixes.Add(new AllowSkinnyFormationsFix(skinnyFormations));
            #endregion

            basicFixes.Add(new BouncingScrollablePanelFix(bouncingScrollablePanelFix));
            basicFixes.Add(new PilumNotThrowableFix(throwablePilum));

            foreach (BasicFix fix in basicFixes)
            {
                fix.PatchAll(HarmonyInstance);
            }
        }

        protected override void InitializeGameStarter(Game game, IGameStarter starter)
        {
            foreach(BasicFix fix in basicFixes)
            {
                fix.AddCampaignBehaviors(game, starter);
            }
        }

        public override void OnBeforeMissionBehaviorInitialize(Mission mission)
        {
            /*
            if (!_thing)
            {
                new FormationsFollow().PatchAll(HarmonyInstance);
                _thing = true;
            }
            */

            foreach (BasicFix fix in basicFixes)
            {
                fix.AddMissionLogics(mission);
            }
        }

        protected override void OnApplicationTick(float dt)
        {
            
        }
    }
    
}
