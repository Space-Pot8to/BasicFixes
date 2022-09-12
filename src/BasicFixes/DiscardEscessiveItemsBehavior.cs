using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;

namespace BasicFixes
{
    /// <summary>
    /// This campaign behavior makes sure that ai parties don't kill their speed by becoming overburdened
    /// </summary>
    public class DiscardEscessiveItemsBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.HourlyTickPartyEvent.AddNonSerializedListener(this, new Action<MobileParty>(this.HourlyTickPartyEvent));
        }

        public override void SyncData(IDataStore dataStore)
        {
            
        }

        private void HourlyTickPartyEvent(MobileParty mobileParty)
        {
            float inventoryCapacity = mobileParty.InventoryCapacity;
            float carriedWeight = mobileParty.ItemRoster.TotalWeight;
            float weightDiff = inventoryCapacity - carriedWeight;
            if (!mobileParty.IsMainParty && weightDiff < 0)
            {
                // first order by value least to greatest
                // then order by is food
                List<ItemRosterElement> items = mobileParty.ItemRoster
                    .Where(x => !x.EquipmentElement.Item.HasHorseComponent)
                    .OrderBy(x => x.EquipmentElement.ItemValue)
                    .OrderBy(x => x.EquipmentElement.Item.IsFood).ToList();

                for (int i = 0; i < items.Count && weightDiff < 0; i++)
                {
                    // discard low value items until the party is no longer unburdened
                    ItemRosterElement element = items[i];
                    float elementWeight = element.GetRosterElementWeight();
                    if (weightDiff + elementWeight < 0)
                    {
                        // then remove the entire element
                        mobileParty.ItemRoster.AddToCounts(element.EquipmentElement, -element.Amount);
                    }
                    else
                    {
                        // only remove enough of the element to balance the weight
                        int count = (int)Math.Ceiling(-weightDiff / element.EquipmentElement.Weight);
                        mobileParty.ItemRoster.AddToCounts(element.EquipmentElement, -count);
                    }
                    carriedWeight = mobileParty.ItemRoster.TotalWeight;
                    weightDiff = inventoryCapacity - carriedWeight;
                }
            }
        }
    }
}
