using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors.AiBehaviors;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace BasicFixes
{
    public class SubModule : MBSubModuleBase
    {
        protected override void InitializeGameStarter(Game game, IGameStarter starter)
        {
            CampaignGameStarter campaignGameStarter = starter as CampaignGameStarter;
            if(campaignGameStarter != null)
            {
                campaignGameStarter.AddBehavior(new DiscardExcessiveItemsBehavior());

                // remove bad AIVisitSettlementBehavior
                AiVisitSettlementBehavior badBehavior = campaignGameStarter.CampaignBehaviors.FirstOrDefault(x => x is AiVisitSettlementBehavior) as AiVisitSettlementBehavior;
                if(badBehavior != null)
                    campaignGameStarter.RemoveBehavior(badBehavior);
                campaignGameStarter.AddBehavior(new AiVisitSettlementBehaviorFixed());
            }
        }
    }
}
