using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using TaleWorlds.MountAndBlade;

namespace BasicFixes
{
    public class ResizeLooseFormationFix : BasicFix
    {
        public ResizeLooseFormationFix() : base()
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
        }
    }

    /// <summary>
    /// Problem as describe here https://forums.taleworlds.com/index.php?threads/loose-formation-bug.449051/
    /// When archers are in loose formation and start getting killed, the formation will be 
    /// resized to Formation.FormOrder._customWidth, which for an archer formation is set at 
    /// around 47 at the beginning of the mission.
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
                    fieldInfo1.SetValue(boxed, -1f);
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
}
