using System.Linq;
using System.Xml;

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors.AiBehaviors;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

using Helpers;
using TaleWorlds.ModuleManager;

namespace BasicFixes
{
    public class SubModule : MBSubModuleBase
    {
        private bool enableDiscardFood = true;
        private bool enableRecruitFix = true;
        private bool enableCaravanFix = true;

        protected override void InitializeGameStarter(Game game, IGameStarter starter)
        {
            CampaignGameStarter campaignGameStarter = starter as CampaignGameStarter;
            if(campaignGameStarter != null)
            {
                XmlDocument settingsDoc = MiscHelper.LoadXmlFile(ModuleHelper.GetModuleFullPath("BasicFixes") + "ModuleData/settings.xml");
                if(settingsDoc != null)
                {
                    XmlElement documentElement = settingsDoc.DocumentElement;
                    XmlNodeList xmlNodeList = documentElement.SelectNodes("/Settings/*");
                    foreach(XmlNode node in xmlNodeList)
                    {
                        if(node.Name == "DiscardExcessFood")
                        {
                            enableDiscardFood = bool.Parse(node.InnerText);
                        }
                        else if (node.Name == "AIRecruitFix")
                        {
                            enableRecruitFix = bool.Parse(node.InnerText);
                        }
                        else if (node.Name == "CaravanFix")
                        {
                            enableCaravanFix = bool.Parse(node.InnerText);
                        }
                    }
                }

                if(enableDiscardFood)
                    campaignGameStarter.AddBehavior(new DiscardExcessiveItemsBehavior());

                if (enableRecruitFix)
                {
                    // remove bad AIVisitSettlementBehavior
                    AiVisitSettlementBehavior badBehavior = campaignGameStarter.CampaignBehaviors.FirstOrDefault(x => x is AiVisitSettlementBehavior) as AiVisitSettlementBehavior;
                    if (badBehavior != null)
                        campaignGameStarter.RemoveBehavior(badBehavior);
                    campaignGameStarter.AddBehavior(new AiVisitSettlementBehaviorFixed());
                }

                if (enableCaravanFix)
                {
                    campaignGameStarter.AddBehavior(new CaravansCampaignBehaviorFix());
                }
            }
        }
    }
}
