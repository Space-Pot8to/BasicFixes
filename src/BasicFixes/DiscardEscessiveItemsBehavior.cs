using System;
using System.Collections.Generic;
using System.Linq;

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;

namespace BasicFixes
{
    /// <summary>
    /// This campaign behavior makes sure that ai parties don't kill their speed by becoming overburdened.
    /// Either 1 the ai party needs to be prevented from taking on too much weight, or 2 the excess 
    /// weight needs to be taken away soon after it is gained. I'm going for option 2 since option 
    /// 1 would involve patching BattleCampaignBehavior.CollectLoots.
    /// </summary>
    public class DiscardExcessiveItemsBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.HourlyTickPartyEvent.AddNonSerializedListener(this, new Action<MobileParty>(this.HourlyTickPartyEvent));
            CampaignEvents.DistributeLootToPartyEvent.AddNonSerializedListener(this, new Action<MapEvent, PartyBase, Dictionary<PartyBase, ItemRoster>>(this.DistributeLootToPartyEvent));
        }

        public override void SyncData(IDataStore dataStore)
        {
            
        }

        /// <summary>
        /// Removed excess food from mobile party by reducing the greatest single food by 10% until 
        /// the party is either no longer overburdened or party would soon starve if more food is 
        /// removed.
        /// </summary>
        /// <param name="mobileParty"></param>
        private void RemoveExcessFood(MobileParty mobileParty)
        {
            // calculate excess weight
            float extraWeight = mobileParty.TotalWeightCarried - mobileParty.InventoryCapacity;

            // get list of foods since that's what is being exponentially added to parties
            List<ItemRosterElement> allFoods = mobileParty.ItemRoster
                .Where(x => x.EquipmentElement.Item.IsFood)
                .OrderByDescending(x => x.Amount)
                .ToList();

            // get list of foods
            ItemRosterElement largest = allFoods.MaxBy(x => x.GetRosterElementWeight());
            // make sure removing foods won't make our party starve
            float daysWithFood = mobileParty.GetNumDaysForFoodToLast();
            // when amount is zero, then the excess weight isn't due to food, so let's stop
            int amount = (int)(largest.Amount * 0.1f);
            while (extraWeight > 0 && daysWithFood > 10 && amount > 0)
            {
                mobileParty.ItemRoster.AddToCounts(largest.EquipmentElement, -amount);

                daysWithFood = mobileParty.GetNumDaysForFoodToLast();
                extraWeight = mobileParty.TotalWeightCarried - mobileParty.InventoryCapacity;

                allFoods = mobileParty.ItemRoster
                .Where(x => x.EquipmentElement.Item.IsFood)
                .OrderByDescending(x => x.Amount)
                .ToList();
                largest = allFoods.MaxBy(x => x.GetRosterElementWeight());
                amount = (int)(largest.Amount * 0.1f);
            }
        }

        /// <summary>
        /// This takes food from a mobile party that just received loot if it is not the player 
        /// and it is overburndened.
        /// </summary>
        /// <param name="mapEvent"></param>
        /// <param name="party"></param>
        /// <param name="pairs"></param>
        private void DistributeLootToPartyEvent(MapEvent mapEvent, PartyBase party, Dictionary<PartyBase, ItemRoster> pairs)
        {
            MobileParty mobileParty = party.MobileParty;
            if (mobileParty != null 
                && !mobileParty.IsMainParty 
                && mobileParty.TotalWeightCarried > mobileParty.InventoryCapacity
                && mobileParty.Food > 0)
            {
                RemoveExcessFood(mobileParty);
            }
        }

        /// <summary>
        /// This tries to take food from a mobile party that is for whatever reason overburdened 
        /// until the party is no longer overburdened. Should fix parties that are in the save 
        /// data and are overburdened.
        /// </summary>
        /// <param name="mobileParty"></param>
        private void HourlyTickPartyEvent(MobileParty mobileParty)
        {
            if (mobileParty != null 
                && !mobileParty.IsMainParty 
                && mobileParty.TotalWeightCarried > mobileParty.InventoryCapacity
                && mobileParty.Food > 0)
            {
                RemoveExcessFood(mobileParty);
            }
        }
    }
}
