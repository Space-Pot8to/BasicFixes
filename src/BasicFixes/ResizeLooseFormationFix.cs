using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using TaleWorlds.MountAndBlade;

using HarmonyLib;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace BasicFixes
{
    public class AllowSkinnyFormationsFix : BasicFix
    {
        public AllowSkinnyFormationsFix(bool isEnabled) : base(isEnabled)
        {
            if(isEnabled)
                base.SimpleHarmonyPatches.Add(new LineFormation_MinimumFileCount_Patch());
        }

        /// <summary>
        /// Decreases the minimum file count (depth) to one. Lets the formation get really thin.
        /// </summary>
        [HarmonyPatch]
        public class LineFormation_MinimumFileCount_Patch : SimpleHarmonyPatch
        {
            public override MethodBase TargetMethod
            {
                get
                {
                    return AccessTools.PropertyGetter(typeof(LineFormation), "MinimumFileCount");
                }
            }

            public override string PatchType { get { return "Prefix"; } }

            public static bool Prefix(Formation __instance, ref int __result)
            {
                __result = 1;
                return false;
            }
        }
    }

    /// <summary>
    /// Problem as describe here https://forums.taleworlds.com/index.php?threads/loose-formation-bug.449051/
    /// When archers are in loose formation and start getting killed, the formation will be 
    /// resized to Formation.FormOrder._customWidth, which for an archer formation is set at 
    /// around 47 at the beginning of the mission.
    /// </summary>
    public class ResizeLooseFormationFix : BasicFix
    {
        public ResizeLooseFormationFix(bool isEnabled) : base(isEnabled)
        {
            if (isEnabled)
            {
                base.MissionLogics.Add(
                    new Tuple<MissionCondition, MissionConsequence>
                    (
                        delegate (Mission mission)
                        {
                            return Mission.Current.GetMissionBehavior<MissionAgentSpawnLogic>() != null;
                        },
                        delegate (Mission mission)
                        {
                            mission.AddMissionBehavior(new KeepFormationWidth());
                        }
                    ));

                // base.SimpleHarmonyPatches.Add(new OrderController_DecreaseUnitSpacingAndWidthIfNotAllUnitsFit_Patch());
            }
        }

        /// <summary>
        /// Sets the new form order of all formations at the start of battle to -1 so that when they 
        /// start dying no resize happens
        /// </summary>
        public class KeepFormationWidth : MissionLogic
        {
            private bool _isInitialSpawnOver;
            private MissionAgentSpawnLogic _spawnLogic;

            public override void AfterStart()
            {
                _isInitialSpawnOver = false;
                _spawnLogic = Mission.Current.GetMissionBehavior<MissionAgentSpawnLogic>();
            }

            public override void OnMissionTick(float dt)
            {
                if (_spawnLogic != null && _spawnLogic.IsInitialSpawnOver && !_isInitialSpawnOver)
                {
                    Team playerTeam = Mission.Current.PlayerTeam;
                    List<Formation> formations = playerTeam.Formations.ToList();
                    for (int i = 0; i < formations.Count; i++)
                    {
                        Formation formation = formations[i];
                        FieldInfo fieldInfo = formation.GetType().GetField("_formOrder", BindingFlags.Instance | BindingFlags.NonPublic);
                        FormOrder order = (FormOrder)fieldInfo.GetValue(formation);
                        object boxed = order;
                        FieldInfo fieldInfo1 = order.GetType().GetField("_customWidth", BindingFlags.Instance | BindingFlags.NonPublic);
                        fieldInfo1.SetValue(boxed, formation.Width);
                        order = (FormOrder)boxed;
                        fieldInfo.SetValue(formation, order);
                    }
                    _isInitialSpawnOver = true;
                }
            }

            public override void OnBattleEnded()
            {
                _spawnLogic = null;
            }
        }

        /// <summary>
        /// I forget what this is supposed to do...
        /// </summary>
        [HarmonyPatch]
        public class OrderController_DecreaseUnitSpacingAndWidthIfNotAllUnitsFit_Patch : SimpleHarmonyPatch
        {
            public override MethodBase TargetMethod
            {
                get
                {
                    return AccessTools.FirstMethod(typeof(OrderController), x => x.Name == "DecreaseUnitSpacingAndWidthIfNotAllUnitsFit" && x.IsStatic);
                }
            }

            public override string PatchType { get { return "Prefix"; } }

            public static void Prefix(Formation formation, ref Formation simulationFormation, in WorldPosition formationPosition, in Vec2 formationDirection, ref float formationWidth, ref int unitSpacingReduction)
            {
                if (formation.UnitSpacing != simulationFormation.UnitSpacing)
                {
                    Formation formation1 = new Formation(null, -1);
                    formation1.SetPositioning(null, null, formation.UnitSpacing);
                    formation1.Arrangement.DeepCopyFrom(formation.Arrangement);

                    simulationFormation = formation1;
                }
            }
        }
    }
}
