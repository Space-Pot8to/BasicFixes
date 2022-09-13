using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace BasicFixes
{
    /// <summary>
    /// CaravansCampaignBehavior._previouslyChangedCaravanTargetsDueToEnemyOnWay is a dictionary 
    /// that keeps a list of settlements that can't be traveled to for each caravan. The entry 
    /// for a given caravan is only cleared when the caraven enters a settlement. What can 
    /// sometimes happen, however, is that this list can grow to include all the settlements a 
    /// carvan can travel to, meaning the caravan becomes stuck and won't move. This behavior 
    /// fixes this problem by clearing the list when it reaches this state.
    /// </summary>
    public class CaravansCampaignBehaviorFix : CampaignBehaviorBase
    {
        private CaravansCampaignBehavior _original;

        public override void RegisterEvents()
        {
            CampaignEvents.AiHourlyTickEvent.AddNonSerializedListener(this, new Action<MobileParty, PartyThinkParams>(this.AIHourlyTick));
        }

        public override void SyncData(IDataStore dataStore)
        {
            
        }

        private void AIHourlyTick(MobileParty mobileParty, PartyThinkParams partyThinkParams)
        {
            if (mobileParty.IsCaravan)
            {
                if (_original == null)
                    _original = Campaign.Current.GetCampaignBehavior<CaravansCampaignBehavior>();

                // get field through access tools since it's private
                FieldInfo field = _original.GetType().GetField("_previouslyChangedCaravanTargetsDueToEnemyOnWay", BindingFlags.NonPublic | BindingFlags.Instance);
                Dictionary<MobileParty, List<Settlement>> badDict = field.GetValue(_original) as Dictionary<MobileParty, List<Settlement>>;

                // condition is from CaravansCampaignBehavior.FindNextDestinationForCaravan
                List<Town> possibleTowns = Town.AllTowns
                    .Where(x => x.Owner.Settlement != mobileParty.CurrentSettlement &&
                                !x.IsUnderSiege && !x.MapFaction.IsAtWarWith(mobileParty.MapFaction) &&
                                (!x.Settlement.Parties.Contains(MobileParty.MainParty) || !MobileParty.MainParty.MapFaction.IsAtWarWith(mobileParty.MapFaction))).ToList();

                // clear the list when there are no more possible settlement for the caravan to travel to
                if (badDict != null && badDict.ContainsKey(mobileParty) && badDict[mobileParty].Count == possibleTowns.Count()) 
                {
                    badDict[mobileParty].Clear();
                    field.SetValue(_original, badDict);
                }
            }
        }
    }
}
