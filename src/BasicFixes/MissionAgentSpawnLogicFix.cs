using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace BasicFixes
{
    /// <summary>
    /// In MissionAgentSpawnLogic.MissionSide.SpawnTroops two numbers (num3 and num4 when looking 
    /// at it in dnspy) count the number of cav troops and foot troops respectively. However these 
    /// variables are declared in a scope one level above where they should be. The result is that 
    /// they end up counting the total nubmer of cav and foot troops in the entire party. So if 
    /// you make an infantry formation above a cavalry formation (or any formation with too many 
    /// cav), then on deployment, that formation will have the extra spacing between its troops 
    /// that is meant for a cavalry formation.
    /// </summary>
    public class MissionAgentSpawnLogicFix : MissionLogic
    {
        private MissionAgentSpawnLogic _spawnLogic;

        public override void AfterStart()
        {
            _spawnLogic = Mission.Current.GetMissionBehavior<MissionAgentSpawnLogic>();
        }

        public override void OnMissionTick(float dt)
        {
            if (_spawnLogic != null && !_spawnLogic.IsInitialSpawnOver)
            {
                Team playerTeam = Mission.Current.PlayerTeam;
                List<Formation> formations = playerTeam.Formations.ToList();
                for (int i = 0; i < formations.Count; i++)
                {
                    Formation formation = formations[i];
                    int cavCount = formation.GetCountOfUnitsWithCondition(x => x.HasMount);
                    int footCount = formation.CountOfUnits - cavCount;
                    bool isMounted = MissionDeploymentPlan.HasSignificantMountedTroops(footCount, cavCount);
                    if (formation.CalculateHasSignificantNumberOfMounted && !isMounted)
                    {
                        formation.BeginSpawn(formation.CountOfUnits, isMounted);
                        WorldPosition worldPosition = new WorldPosition(Mission.Current.Scene, formation.OrderGroundPosition + new Vec3(0.1f, 0.1f, 0f));
                        formation.SetPositioning(worldPosition, null, 0);
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// When the player leads an infantry formation and is mounted, the projected unit formation 
    /// projects unit flags assuming they are all cavalry. This fixes that problem by setting the 
    /// projected formation to be on foot.
    /// </summary>
    [HarmonyPatch]
    public class BadFormationProjectionFix
    {
        public static MethodBase TargetMethod()
        {
            return AccessTools.FirstMethod(typeof(Formation), method => method.Name.Contains("GetUnitPositionWithIndexAccordingToNewOrder") && method.IsStatic);
        }

        public static void Prefix(Formation simulationFormation, int unitIndex, in WorldPosition formationPosition, in Vec2 formationDirection, IFormationArrangement arrangement, float width, int unitSpacing, int unitCount, ref bool isMounted, int index, ref WorldPosition? unitPosition, ref Vec2? unitDirection, ref float actualWidth)
        {
            FieldInfo fieldInfo = arrangement.GetType().GetField("owner", BindingFlags.Instance | BindingFlags.NonPublic);
            Formation owner = fieldInfo.GetValue(arrangement) as Formation;
            if (owner.GetCountOfUnitsWithCondition(x => x.IsMainAgent) > 0)
            {
                bool isPlayerMounted = Agent.Main.HasMount;
                int cavCount = owner.GetCountOfUnitsWithCondition(x => x.HasMount) - (isPlayerMounted ? 1 : 0);
                int footCount = owner.CountOfUnits - cavCount;
                isMounted = MissionDeploymentPlan.HasSignificantMountedTroops(footCount, cavCount);
            }
        }
    }

}
