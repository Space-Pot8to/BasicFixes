using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
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
                campaignGameStarter.AddBehavior(new DiscardEscessiveItemsBehavior());
            }
        }
    }
}
