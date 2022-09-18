using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;
using HarmonyLib;

namespace BasicFixes
{
    public class BattleLineFix : BasicFix
    {
        public BattleLineFix() : base()
        {
            base.MissionLogics.Add(
                new Tuple<MissionCondition, MissionConsequence>
                (
                    delegate (Mission mission)
                    {
                        return true;
                    },
                    delegate (Mission mission)
                    {
                        mission.AddMissionBehavior(new FormationTracker());
                    }
                ));

            base.SimpleHarmonyPatches.Add(new Formation_ArrangementOrder_Patch());
            if(SubModule.unitsRunWhenFormingUp)
                base.SimpleHarmonyPatches.Add(new HumanAIComponent_AdjustSpeedLimit_Patch());
            if (SubModule.unitsDontUseShieldsWhenFormingUp)
                base.SimpleHarmonyPatches.Add(new ArrangementOrder_GetShieldDirectionOfUnit_Patch());

            //base.SimpleHarmonyPatches.Add(new PatchToMakeArmyFollowInFormation2());
            //base.SimpleHarmonyPatches.Add(new PatchToMakeArmyFollowInFormation3());
        }
    }

    /// <summary>
    /// Problem is that formations don't keep the number of rows in their formation when resizing 
    /// for different formation as described here:
    /// https://forums.taleworlds.com/index.php?threads/formation-broken.448859/
    /// </summary>
    public class FormationTracker : MissionLogic
    {
        public static FormationTracker Instance;

        public List<Formation> transformingFormations;
        public FormationTracker()
        {
            transformingFormations = new List<Formation>();
            Instance = this;
        }

        public static bool IsFormedUp(Formation formation, float tolerance)
        {
            bool isFormed = true;
            IEnumerable<Agent> agents = formation.GetUnitsWithoutDetachedOnes();
            foreach (Agent agent in agents)
            {
                Vec2 agentPosition = agent.Position.AsVec2;
                Vec2 orderedPosition = formation.GetCurrentGlobalPositionOfUnit(agent, true);
                bool isInPosition = agentPosition.Distance(orderedPosition).ApproximatelyEqualsTo(0f, tolerance);
                if (!isInPosition)
                {
                    isFormed = false;
                    break;
                } 
            }
            return isFormed;
        }

        public override void OnMissionTick(float dt)
        {
            //Formation formation = transformingFormations.FirstOrDefault(x => IsFormedUp(x, 0.2f));
            //if (formation != null)
            //    transformingFormations.Remove(formation);

            transformingFormations.RemoveAll(x => IsFormedUp(x, 0.2f));
        }

        public override void OnBattleEnded()
        {
            transformingFormations.Clear();
        }
    }

    /// <summary>
    /// Problem is that formations don't keep to the width the player sets. Problem described here:
    /// https://forums.taleworlds.com/index.php?threads/unit-formations-sometimes-force-themselves-into-a-permanent-square-shape.453610/
    /// Makes Formations keep the width of the previous formation.
    /// </summary>
    [HarmonyPatch]
    public class Formation_ArrangementOrder_Patch : SimpleHarmonyPatch
    {
        
        public override MethodBase TargetMethod
        {
            get
            {
                return AccessTools.PropertySetter(typeof(Formation), "ArrangementOrder");
            }
        }

        public override string PatchType { get { return "Prefix"; } }

        public static void Prefix(Formation __instance)
        {
            Formation already = FormationTracker.Instance.transformingFormations.FirstOrDefault(x => x.FormationIndex == __instance.FormationIndex);
            if (already != null)
                FormationTracker.Instance.transformingFormations.Remove(already);

            if (__instance.CountOfUnits > 0)
                FormationTracker.Instance.transformingFormations.Add(__instance);
            __instance.FormOrder = FormOrder.FormOrderCustom(__instance.Width);
        }
    }

    /// <summary>
    /// Problem is that units will walk to their formation position whe the formation wants them 
    /// to hold up their shields. This patch makes units that haven't arrived at their position 
    /// in the formation run.
    /// </summary>
    /// <dependency cref="Formation_ArrangementOrder_Patch"/>
    [HarmonyPatch]
    public class HumanAIComponent_AdjustSpeedLimit_Patch : SimpleHarmonyPatch
    {
        public override string PatchType { get { return "Prefix"; } }
        public override MethodBase TargetMethod
        {
            get
            {
                return AccessTools.Method(typeof(HumanAIComponent), "AdjustSpeedLimit");
            }
        }

        public static void Prefix(Agent agent, ref float desiredSpeed, ref bool limitIsMultiplier)
        {
            if(agent.Formation != null && !agent.IsMainAgent)
            {
                if (FormationTracker.Instance.transformingFormations.Contains(agent.Formation))
                {
                    bool isArrived = agent.Formation.GetCurrentGlobalPositionOfUnit(agent, true).Distance(agent.Position.AsVec2).ApproximatelyEqualsTo(0f, 1f);
                    if (!limitIsMultiplier && !isArrived)
                    {
                        desiredSpeed = agent.RunSpeedCached;
                    }
                    else if (limitIsMultiplier && !isArrived)
                    {
                        desiredSpeed = 1f;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Problem is that units will walk to their formation position when that formation tells them 
    /// to hold up their shields. I consider this a bug, because in my experience the fact that 
    /// they have their shields up makes them more vulnerable thatn if they just ran with their 
    /// shields down. This patch makes units which haven't arrived at their position in formation 
    /// not block with shields.
    /// </summary>
    /// <dependency cref="Formation_ArrangementOrder_Patch"/>
    [HarmonyPatch]
    public class ArrangementOrder_GetShieldDirectionOfUnit_Patch : SimpleHarmonyPatch
    {
        public override string PatchType { get { return "Prefix"; } }
        public override MethodBase TargetMethod
        {
            get
            {
                return AccessTools.Method(typeof(ArrangementOrder), "GetShieldDirectionOfUnit");
            }
        }

        public static bool Prefix(Formation formation, Agent unit, ArrangementOrder.ArrangementOrderEnum orderEnum, ref Agent.UsageDirection __result)
        {
            // if the formation is transforming from a different formation, don't run with shields
            if (FormationTracker.Instance.transformingFormations.Contains(formation))
            {
                Vec2 formationPosition = formation.GetCurrentGlobalPositionOfUnit(unit, true);
                Vec2 unitPosition = unit.Position.AsVec2;
                bool isArrived = formationPosition.Distance(unitPosition).ApproximatelyEqualsTo(0.0f, 0.5f);
                if (!isArrived)
                {
                    __result = Agent.UsageDirection.None;
                    return false;
                }
            }

            return true;
        }
    }

    /// <summary>
    /// It's stupid that when you tell all formations to follow you that they all collapse to the 
    /// same point. They should follow with respect to other formations as well.
    /// </summary>
    [HarmonyPatch]
    public class PatchToMakeArmyFollowInFormation : SimpleHarmonyPatch
    {
        public Agent Agent { get; set; }
        public override MethodBase TargetMethod
        {
            get
            {
                return AccessTools.FirstMethod(typeof(MovementOrder), x => x.Name == "GetPositionAuxFollow");
            }
        }

        public override string PatchType { get { return "Prefix"; } }

        public static void Prefix(MovementOrder __instance, Formation f)
        {
            
        }
    }

    [HarmonyPatch]
    public class PatchToMakeArmyFollowInFormation2 : SimpleHarmonyPatch
    {
        //public MovementOrder OriginalOrder;
            
        public static PatchToMakeArmyFollowInFormation2 Instance { get; private set; }

        public PatchToMakeArmyFollowInFormation2()
        {
            Instance = this;
            //OriginalOrder = default(MovementOrder);
        }

        public override MethodBase TargetMethod
        {
            get
            {
                return AccessTools.FirstMethod(typeof(MovementOrder), x => x.Name == "OnApply");
            }
        }

        public override string PatchType { get { return "Prefix"; } }

        public static void Prefix(Formation formation)
        {
            //if (input.OrderEnum == MovementOrder.MovementOrderEnum.Follow) 
            //{
                //Instance.OriginalOrder = input;
                //Agent agent = input._targetAgent;
               // agent.TeleportToPosition(new Vec3(__instance.Direction * 2f + __instance.GetMiddleFrontUnitPositionOffset() + __instance.CurrentPosition));
                //MovementOrder order = MovementOrder.MovementOrderFollow(agent);
                //input = order;

                /*
                object boxed = input;
                PropertyInfo fieldInfo1 = input.GetType().GetProperty("_targetAgent");

                // create a ghost agent witha  different position

                fieldInfo1.SetValue(boxed, formation.Width);
                input = (FormOrder)boxed;
                */
            //}
        }
    }

    [HarmonyPatch]
    public class PatchToMakeArmyFollowInFormation3 : SimpleHarmonyPatch
    {
        public override MethodBase TargetMethod
        {
            get
            {
                return AccessTools.FirstMethod(typeof(Formation), x => x.Name == "SetMovementOrder");
            }
        }

        public override string PatchType { get { return "Postfix"; } }

        public static void Postfix(Formation __instance)
        {
            //if(PatchToMakeArmyFollowInFormation2.Instance.OriginalOrder != default(MovementOrder))
            //    input = PatchToMakeArmyFollowInFormation2.Instance.OriginalOrder;
        }
    }
}