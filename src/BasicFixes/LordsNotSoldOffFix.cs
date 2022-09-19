using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;

using HarmonyLib;

namespace BasicFixes
{
	/// <summary>
	/// Problem: https://forums.taleworlds.com/index.php?threads/ransom-for-captured-lords-is-not-paid-after-upgrading-to-1-8-0.453369/
	/// 
	/// In short, Lords aren't sold off when "Ransom your prisoners" is selected in the tavern game menu
	/// </summary>
	/// <remarks>
	/// As far as I can tell, there is no difference in roguery xp gain between selling prisoners via 
	/// the party menu or by the sell all button in the game menu.
	/// </remarks>
	public class LordsNotSoldOffFix : BasicFix
    {
		public LordsNotSoldOffFix(bool isEnabled) : base(isEnabled)
        {
			if(isEnabled)
				base.SimpleHarmonyPatches.Add(new SellPrisonersAction_ApplyForAllPrisoners_Patch());
        }

		[HarmonyPatch]
		public class SellPrisonersAction_ApplyForAllPrisoners_Patch : SimpleHarmonyPatch
		{
			public override string PatchType { get { return "Prefix"; } }

			public override MethodBase TargetMethod
			{
				get
				{
					return AccessTools.FirstMethod(typeof(SellPrisonersAction), method => method.Name.Contains("ApplyForAllPrisoners") && method.IsStatic);
				}
			}

			public static bool Prefix(MobileParty sellerParty, TroopRoster prisoners, Settlement currentSettlement, bool applyGoldChange = true)
			{
				if (!sellerParty.IsMainParty)
					return true;

				TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();
				int num = 0;
				List<string> list = Campaign.Current.GetCampaignBehavior<IViewDataTracker>().GetPartyPrisonerLocks().ToList<string>();
				for (int i = prisoners.Count - 1; i >= 0; i--)
				{
					TroopRosterElement elementCopyAtIndex = prisoners.GetElementCopyAtIndex(i);
					if (elementCopyAtIndex.Character != CharacterObject.PlayerCharacter)
					{
						int woundedNumber = elementCopyAtIndex.WoundedNumber;
						int num2 = elementCopyAtIndex.Number - woundedNumber;
						if (!list.Contains(elementCopyAtIndex.Character.StringId) && !elementCopyAtIndex.Character.IsHero)
						{
							sellerParty.PrisonRoster.AddToCounts(elementCopyAtIndex.Character, -num2 - woundedNumber, false, -woundedNumber, 0, true, -1);
							int num3 = Campaign.Current.Models.RansomValueCalculationModel.PrisonerRansomValue(elementCopyAtIndex.Character, sellerParty.LeaderHero);
							if (applyGoldChange)
							{
								num += (num2 + woundedNumber) * num3;
							}
						}
						else if (!list.Contains(elementCopyAtIndex.Character.StringId) && elementCopyAtIndex.Character.IsHero)
						{
							sellerParty.PrisonRoster.AddToCounts(elementCopyAtIndex.Character, -1, false, -woundedNumber, 0, true, -1);
							int num3 = Campaign.Current.Models.RansomValueCalculationModel.PrisonerRansomValue(elementCopyAtIndex.Character, sellerParty.LeaderHero);
							if (applyGoldChange)
							{
								num += (num2 + woundedNumber) * num3;
							}
							EndCaptivityAction.ApplyByRansom(elementCopyAtIndex.Character.HeroObject, sellerParty.LeaderHero);
						}
						troopRoster.AddToCounts(elementCopyAtIndex.Character, num2 + woundedNumber, false, 0, 0, true, -1);
					}
				}
				if (applyGoldChange)
				{
					if (sellerParty.LeaderHero != null)
					{
						GiveGoldAction.ApplyBetweenCharacters(null, sellerParty.LeaderHero, num, false);
					}
					else if (sellerParty.Party.Owner != null)
					{
						GiveGoldAction.ApplyBetweenCharacters(null, sellerParty.Party.Owner, num, false);
					}
				}
				SkillLevelingManager.OnPrisonerSell(sellerParty, (float)troopRoster.TotalManCount);
				CampaignEventDispatcher.Instance.OnPrisonerSold(sellerParty, troopRoster, currentSettlement);

				return false;
			}
		}
	}
}
