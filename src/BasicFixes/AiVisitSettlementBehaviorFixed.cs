using System;
using System.Collections.Generic;
using System.Linq;

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using Helpers;

namespace BasicFixes
{
	/// <summary>
	/// Feels silly to recreate an entire class to fix one method, but here we are. The bad method 
	/// is ApproximateNumberOfVolunteersCanBeRecruitedFromSettlement. The original method doesn't 
	/// take into account whether or not the relation between the notable and the mobile party 
	/// leader will allow the mobile party to recruit at all.
	/// </summary>
	public class AiVisitSettlementBehaviorFixed : CampaignBehaviorBase
	{
		// Token: 0x06003B85 RID: 15237 RVA: 0x0011A08C File Offset: 0x0011828C
		public override void RegisterEvents()
		{
			CampaignEvents.AiHourlyTickEvent.AddNonSerializedListener(this, new Action<MobileParty, PartyThinkParams>(this.AiHourlyTick));
		}

		// Token: 0x06003B86 RID: 15238 RVA: 0x0011A0A5 File Offset: 0x001182A5
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x06003B87 RID: 15239 RVA: 0x0011A0A8 File Offset: 0x001182A8
		public void AiHourlyTick(MobileParty mobileParty, PartyThinkParams p)
		{
			Settlement currentSettlement = mobileParty.CurrentSettlement;
			if (((currentSettlement != null) ? currentSettlement.SiegeEvent : null) != null)
			{
				return;
			}
			Settlement settlement = mobileParty.CurrentSettlement ?? ((mobileParty.LastVisitedSettlement != null && mobileParty.LastVisitedSettlement.Position2D.DistanceSquared(mobileParty.Position2D) < 1f) ? mobileParty.LastVisitedSettlement : null);
			if (mobileParty.IsBandit)
			{
				this.CalculateVisitHideoutScoresForBanditParty(mobileParty, settlement, p);
				return;
			}
			IFaction mapFaction = mobileParty.MapFaction;
			if (mobileParty.IsMilitia || mobileParty.IsCaravan || mobileParty.IsVillager || (!mapFaction.IsMinorFaction && !mapFaction.IsKingdomFaction && (mobileParty.LeaderHero == null || !mobileParty.LeaderHero.IsLord)))
			{
				return;
			}
			if (mobileParty.Army == null || mobileParty.Army.LeaderParty == mobileParty || mobileParty.Army.Cohesion < (float)mobileParty.Army.CohesionThresholdForDispersion)
			{
				Hero leaderHero = mobileParty.LeaderHero;
				ValueTuple<float, float, int, int> valueTuple = this.CalculatePartyParameters(mobileParty);
				float item = valueTuple.Item1;
				float item2 = valueTuple.Item2;
				int item3 = valueTuple.Item3;
				int item4 = valueTuple.Item4;
				float num = item2 / Math.Min(1f, Math.Max(0.1f, item));
				float num2 = (num >= 1f) ? 0.33f : ((MathF.Max(1f, MathF.Min(2f, num)) - 0.5f) / 1.5f);
				float num3 = mobileParty.Food;
				float num4 = -mobileParty.FoodChange;
				int num5 = (leaderHero != null) ? leaderHero.Gold : 0;
				int num6 = 1;
				if (mobileParty.Army != null && mobileParty == mobileParty.Army.LeaderParty)
				{
					foreach (MobileParty mobileParty2 in mobileParty.Army.LeaderParty.AttachedParties)
					{
						num3 += mobileParty2.Food;
						num4 += -mobileParty2.FoodChange;
						int num7 = num5;
						Hero leaderHero2 = mobileParty2.LeaderHero;
						num5 = num7 + ((leaderHero2 != null) ? leaderHero2.Gold : 0);
						num6++;
					}
				}
				float num8 = 1f;
				if (leaderHero != null && mobileParty.IsLordParty)
				{
					num8 = this.CalculateSellItemScore(mobileParty);
				}
				int num9 = mobileParty.Party.PrisonerSizeLimit;
				if (mobileParty.Army != null)
				{
					foreach (MobileParty mobileParty3 in mobileParty.Army.LeaderParty.AttachedParties)
					{
						num9 += mobileParty3.Party.PrisonerSizeLimit;
					}
				}

				SortedList<ValueTuple<float, int>, Settlement> sortedList = this.FindSettlementsToVisitWithDistances(mobileParty);

				float num10 = PartyBaseHelper.FindPartySizeNormalLimit(mobileParty);
				float num11 = Campaign.MapDiagonalSquared;
				if (num3 - num4 < 0f)
				{
					foreach (KeyValuePair<ValueTuple<float, int>, Settlement> keyValuePair in sortedList)
					{
						float item5 = keyValuePair.Key.Item1;
						Settlement value = keyValuePair.Value;
						if (item5 < 250f && (float)value.ItemRoster.TotalFood > num4 * 2f && item5 < num11)
						{
							num11 = item5;
						}
					}
				}
				float num12 = 2000f;
				float num13 = 2000f;
				if (leaderHero != null)
				{
					num12 = HeroHelper.StartRecruitingMoneyLimitForClanLeader(leaderHero);
					num13 = HeroHelper.StartRecruitingMoneyLimit(leaderHero);
				}
				float idealGarrisonStrengthPerWalledCenter = -1f;
				float num14 = Campaign.AverageDistanceBetweenTwoFortifications * 0.4f;
				float num15 = (84f + Campaign.AverageDistanceBetweenTwoFortifications * 1.5f) * 0.5f;
				float num16 = (424f + 7.57f * Campaign.AverageDistanceBetweenTwoFortifications) * 0.5f;
				foreach (KeyValuePair<ValueTuple<float, int>, Settlement> keyValuePair2 in sortedList)
				{
					Settlement value2 = keyValuePair2.Value;
					float item6 = keyValuePair2.Key.Item1;
					float num17 = 1.6f;
					if (mobileParty.IsDisbanding)
					{
						this.AddBehaviorTupleWithScore(p, value2, this.CalculateMergeScoreForDisbandingParty(mobileParty, value2, item6));
					}
					else if (leaderHero == null)
					{
						this.AddBehaviorTupleWithScore(p, value2, this.CalculateMergeScoreForLeaderlessParty(mobileParty, value2, item6));
					}
					else
					{
						float num18 = item6;
						if (num18 >= 250f)
						{
							this.AddBehaviorTupleWithScore(p, value2, 0.025f);
							continue;
						}
						float num19 = num18;
						num18 = MathF.Max(num14, num18);
						float num20 = MathF.Max(0.1f, MathF.Min(1f, num15 / (num15 - num14 + num18)));
						float num21 = num20;
						if (item < 0.6f)
						{
							num21 = MathF.Pow(num20, MathF.Pow(0.6f / MathF.Max(0.15f, item), 0.3f));
						}
						int? num22 = (settlement != null) ? new int?(settlement.ItemRoster.TotalFood) : null;
						int num23 = item4 / Campaign.Current.Models.MobilePartyFoodConsumptionModel.NumberOfMenOnMapToEatOneFood * 3;
						bool flag = (num22.GetValueOrDefault() > num23 & num22 != null) || num3 > (float)(item4 / Campaign.Current.Models.MobilePartyFoodConsumptionModel.NumberOfMenOnMapToEatOneFood);
						float num24 = (float)item3 / (float)item4;
						float num25 = 1f + ((item4 > 0) ? (num24 * MathF.Max(0.25f, num20 * num20) * MathF.Pow((float)item3, 0.25f) * ((mobileParty.Army != null) ? 4f : 3f) * ((value2.IsFortification && flag) ? 18f : 0f)) : 0f);
						if (mobileParty.MapEvent != null || mobileParty.SiegeEvent != null)
						{
							num25 = MathF.Sqrt(num25);
						}
						float num26 = 1f;
						if ((value2 == settlement && settlement.IsFortification) || (settlement == null && value2 == mobileParty.TargetSettlement))
						{
							num26 = 1.2f;
						}
						else if (settlement == null && value2 == mobileParty.LastVisitedSettlement)
						{
							num26 = 0.8f;
						}
						float num27 = 0.16f;
						float num28 = Math.Max(0f, num3) / num4;
						if (num4 > 0f && (mobileParty.BesiegedSettlement == null || num28 <= 1f) && num5 > 100 && (value2.IsTown || value2.IsVillage) && num28 < 4f)
						{
							float num29 = (float)((int)(num4 * ((num28 < 1f && value2.IsVillage) ? Campaign.Current.Models.PartyFoodBuyingModel.MinimumDaysFoodToLastWhileBuyingFoodFromVillage : Campaign.Current.Models.PartyFoodBuyingModel.MinimumDaysFoodToLastWhileBuyingFoodFromTown)) + 1);
							float num30 = 3f - Math.Min(3f, Math.Max(0f, num28 - 1f));
							float num31 = num29 + 20f * (float)(value2.IsTown ? 2 : 1) * ((num19 > 100f) ? 1f : (num19 / 100f));
							int val = (int)((float)(num5 - 100) / Campaign.Current.Models.PartyFoodBuyingModel.LowCostFoodPriceAverage);
							num27 += num30 * num30 * 0.093f * ((num28 < 2f) ? (1f + 0.5f * (2f - num28)) : 1f) * (float)Math.Pow((double)(Math.Min(num31, (float)Math.Min(val, value2.ItemRoster.TotalFood)) / num31), 0.5);
						}
						float num32 = 0f;
						int num33 = 0;
						int num34 = 0;
						if (item < 1f && (mobileParty.UnlimitedWage || mobileParty.TotalWage < mobileParty.PaymentLimit))
						{
							num33 = value2.NumberOfLordPartiesAt;
							num34 = value2.NumberOfLordPartiesTargeting;
							if (settlement == value2)
							{
								int num35 = num33;
								Army army = mobileParty.Army;
								num33 = num35 - ((army != null) ? army.LeaderPartyAndAttachedParties.Count<MobileParty>() : 1);
								if (num33 < 0)
								{
									num33 = 0;
								}
							}
							if (mobileParty.TargetSettlement == value2 || (mobileParty.Army != null && mobileParty.Army.LeaderParty.TargetSettlement == value2))
							{
								int num36 = num34;
								Army army2 = mobileParty.Army;
								num34 = num36 - ((army2 != null) ? army2.LeaderPartyAndAttachedParties.Count<MobileParty>() : 1);
								if (num34 < 0)
								{
									num34 = 0;
								}
							}
							if (!value2.IsCastle && !mobileParty.Party.IsStarving && (float)leaderHero.Gold > num13 && (leaderHero.Clan.Leader == leaderHero || (float)leaderHero.Clan.Gold > num12) && num10 > mobileParty.PartySizeRatio)
							{
								num32 = (float)AiVisitSettlementBehaviorFixed.ApproximateNumberOfVolunteersCanBeRecruitedFromSettlement(leaderHero, value2);
								if(num32 > 0)
                                {

                                }
								num32 = ((num32 > (float)((int)((num10 - mobileParty.PartySizeRatio) * 100f))) ? ((float)((int)((num10 - mobileParty.PartySizeRatio) * 100f))) : num32);
							}
						}
						float num37 = num32 * num20 / MathF.Sqrt((float)(1 + num33 + num34));
						float num38 = (num37 < 1f) ? num37 : ((float)Math.Pow((double)num37, (double)num2));
						float num39 = Math.Max(Math.Min(1f, num27), Math.Max((mapFaction == value2.MapFaction) ? 0.25f : 0.16f, num * Math.Max(1f, Math.Min(2f, num)) * num38 * (1f - 0.9f * num24) * (1f - 0.9f * num24)));
						if (mobileParty.Army != null)
						{
							num39 /= (float)mobileParty.Army.LeaderPartyAndAttachedParties.Count<MobileParty>();
						}
						num17 *= num39 * num25 * num27 * num21;
						if (num17 >= 2.5f)
						{
							this.AddBehaviorTupleWithScore(p, value2, num17);
							break;
						}
						float num40 = 1f;
						if (num32 > 0f)
						{
							num40 = 1f + ((mobileParty.DefaultBehavior == AiBehavior.GoToSettlement && value2 != settlement && num18 < num14) ? (0.1f * MathF.Min(5f, num32) - 0.1f * MathF.Min(5f, num32) * (num18 / num14) * (num18 / num14)) : 0f);
						}
						float num41 = value2.IsCastle ? 1.4f : 1f;
						num17 *= (value2.IsTown ? num8 : 1f) * num40 * num41;
						if (num17 >= 2.5f)
						{
							this.AddBehaviorTupleWithScore(p, value2, num17);
							break;
						}
						int num42 = mobileParty.PrisonRoster.TotalManCount + mobileParty.PrisonRoster.TotalHeroes * 5;
						float num43 = 1f;
						float num44 = 1f;
						if (mobileParty.Army != null)
						{
							if (mobileParty.Army.LeaderParty != mobileParty)
							{
								num43 = ((float)mobileParty.Army.CohesionThresholdForDispersion - mobileParty.Army.Cohesion) / (float)mobileParty.Army.CohesionThresholdForDispersion;
							}
							num44 = ((MobileParty.MainParty != null && mobileParty.Army == MobileParty.MainParty.Army) ? 0.6f : 0.8f);
							foreach (MobileParty mobileParty4 in mobileParty.Army.LeaderParty.AttachedParties)
							{
								num42 += mobileParty4.PrisonRoster.TotalManCount + mobileParty4.PrisonRoster.TotalHeroes * 5;
							}
						}
						float num45 = value2.IsFortification ? (1f + 2f * (float)(num42 / num9)) : 1f;
						float num46 = 1f;
						if (mobileParty.NumberOfRecentFleeingFromAParty > 0)
						{
							Vec2 v = value2.Position2D - mobileParty.Position2D;
							v.Normalize();
							float num47 = mobileParty.AverageFleeTargetDirection.Distance(v);
							num46 = 1f - Math.Max(2f - num47, 0f) * 0.25f * (Math.Min((float)mobileParty.NumberOfRecentFleeingFromAParty, 10f) * 0.2f);
						}
						float num48 = 1f;
						float num49 = 1f;
						float num50 = 1f;
						float num51 = 1f;
						float num52 = 1f;
						if (num27 <= 0.5f)
						{
							ValueTuple<float, float, float, float> valueTuple2 = this.CalculateBeingSettlementOwnerScores(mobileParty, value2, settlement, idealGarrisonStrengthPerWalledCenter, num20, item);
							num48 = valueTuple2.Item1;
							num49 = valueTuple2.Item2;
							num50 = valueTuple2.Item3;
							num51 = valueTuple2.Item4;
						}
						else
						{
							float num53 = MathF.Sqrt(num11);
							num52 = ((num53 > num16) ? (1f + 7f * MathF.Min(1f, num27 - 0.5f)) : (1f + 7f * (num53 / num16) * MathF.Min(1f, num27 - 0.5f)));
						}
						num17 *= num46 * num52 * num26 * num43 * num45 * num44 * num48 * num50 * num49 * num51;
						if (value2 != null && value2.Name.ToString().Contains("Goleryn"))
						{

						}
					}
					if (num17 > 0.025f)
					{
						this.AddBehaviorTupleWithScore(p, value2, num17);
					}
				}
			}
		}

		/// <summary>
		/// This is the problem method. AI thinks there's enough troops to bother going to the village 
		/// to recruit, but doesn't account for the fact that 
		/// 1.) they can't necessarily recruit at all indices and
		/// 2.) those indices might be empty
		/// </summary>
		/// <param name="hero"></param>
		/// <param name="settlement"></param>
		/// <returns></returns>
		public static int ApproximateNumberOfVolunteersCanBeRecruitedFromSettlement(Hero hero, Settlement settlement)
		{
			int num = hero.MapFaction == settlement.MapFaction ? 4 : 2;

			int maxRecruitable = 0;
			foreach (Hero hero2 in settlement.Notables)
			{
				int num2 = Campaign.Current.Models.VolunteerModel.MaximumIndexHeroCanRecruitFromHero(hero, hero2, -101);
				for (int i = 0; i < num2 && i < num; i++)
				{
					if (hero2.VolunteerTypes[i] != null)
					{
						maxRecruitable++;
					}
				}
			}
			return maxRecruitable;
		}

		// Token: 0x06003B89 RID: 15241 RVA: 0x0011AE18 File Offset: 0x00119018
		private float CalculateSellItemScore(MobileParty mobileParty)
		{
			float num = 0f;
			float num2 = 0f;
			foreach (ItemRosterElement itemRosterElement in mobileParty.ItemRoster)
			{
				if (itemRosterElement.EquipmentElement.Item.IsMountable)
				{
					num2 += (float)(itemRosterElement.Amount * itemRosterElement.EquipmentElement.Item.Value);
				}
				else if (!itemRosterElement.EquipmentElement.Item.IsFood)
				{
					num += (float)(itemRosterElement.Amount * itemRosterElement.EquipmentElement.Item.Value);
				}
			}
			float num3 = (num2 > (float)mobileParty.LeaderHero.Gold * 0.1f) ? MathF.Min(3f, MathF.Pow((num2 + 1000f) / ((float)mobileParty.LeaderHero.Gold * 0.1f + 1000f), 0.33f)) : 1f;
			float num4 = 1f + MathF.Min(3f, MathF.Pow(num / (((float)mobileParty.MemberRoster.TotalManCount + 5f) * 100f), 0.33f));
			float num5 = num3 * num4;
			if (mobileParty.Army != null)
			{
				num5 = MathF.Sqrt(num5);
			}
			return num5;
		}

		// Token: 0x06003B8A RID: 15242 RVA: 0x0011AF80 File Offset: 0x00119180
		private ValueTuple<float, float, int, int> CalculatePartyParameters(MobileParty mobileParty)
		{
			float num = 0f;
			int num2 = 0;
			int num3 = 0;
			float item;
			if (mobileParty.Army != null)
			{
				float num4 = 0f;
				foreach (MobileParty mobileParty2 in mobileParty.Army.Parties)
				{
					float partySizeRatio = mobileParty2.PartySizeRatio;
					num4 += partySizeRatio;
					num2 += mobileParty2.MemberRoster.TotalWounded;
					num3 += mobileParty2.MemberRoster.TotalManCount;
					float num5 = PartyBaseHelper.FindPartySizeNormalLimit(mobileParty2);
					num += num5;
				}
				item = num4 / (float)mobileParty.Army.Parties.Count;
				num /= (float)mobileParty.Army.Parties.Count;
			}
			else
			{
				item = mobileParty.PartySizeRatio;
				num2 += mobileParty.MemberRoster.TotalWounded;
				num3 += mobileParty.MemberRoster.TotalManCount;
				num += PartyBaseHelper.FindPartySizeNormalLimit(mobileParty);
			}
			return new ValueTuple<float, float, int, int>(item, num, num2, num3);
		}

		// Token: 0x06003B8B RID: 15243 RVA: 0x0011B094 File Offset: 0x00119294
		private void CalculateVisitHideoutScoresForBanditParty(MobileParty mobileParty, Settlement currentSettlement, PartyThinkParams p)
		{
			if (!mobileParty.MapFaction.Culture.CanHaveSettlement)
			{
				return;
			}
			if (currentSettlement != null && currentSettlement.IsHideout)
			{
				return;
			}
			int num = 0;
			foreach (ItemRosterElement itemRosterElement in mobileParty.ItemRoster)
			{
				num += itemRosterElement.Amount * itemRosterElement.EquipmentElement.Item.Value;
			}
			float num2 = 1f + 4f * Math.Min((float)num, 1000f) / 1000f;
			int num3 = 0;
			List<Hideout> allHideouts = Settlement.All.Where(x => x.IsHideout).Select(x => x.Hideout).ToList();
			foreach (Hideout hideout in allHideouts)
			{
				if (hideout.Settlement.Culture == mobileParty.Party.Culture && hideout.IsInfested)
				{
					num3++;
				}
			}
			float num4 = 1f + 4f * (float)Math.Sqrt((double)(mobileParty.PrisonRoster.TotalManCount / mobileParty.Party.PrisonerSizeLimit));
			int numberOfMinimumBanditPartiesInAHideoutToInfestIt = Campaign.Current.Models.BanditDensityModel.NumberOfMinimumBanditPartiesInAHideoutToInfestIt;
			int numberOfMaximumBanditPartiesInEachHideout = Campaign.Current.Models.BanditDensityModel.NumberOfMaximumBanditPartiesInEachHideout;
			int numberOfMaximumHideoutsAtEachBanditFaction = Campaign.Current.Models.BanditDensityModel.NumberOfMaximumHideoutsAtEachBanditFaction;
			float num5 = (424f + 7.57f * Campaign.AverageDistanceBetweenTwoFortifications) / 2f;
			foreach (Hideout hideout2 in allHideouts)
			{
				Settlement settlement = hideout2.Settlement;
				if (settlement.Party.MapEvent == null && settlement.Culture == mobileParty.Party.Culture)
				{
					float num6 = Campaign.Current.Models.MapDistanceModel.GetDistance(mobileParty, settlement);
					num6 = Math.Max(10f, num6);
					float num7 = num5 / (num5 + num6);
					int num8 = 0;
					foreach (MobileParty mobileParty2 in settlement.Parties)
					{
						if (mobileParty2.IsBandit && !mobileParty2.IsBanditBossParty)
						{
							num8++;
						}
					}
					float num10;
					if (num8 < numberOfMinimumBanditPartiesInAHideoutToInfestIt)
					{
						float num9 = (float)(numberOfMaximumHideoutsAtEachBanditFaction - num3) / (float)numberOfMaximumHideoutsAtEachBanditFaction;
						num10 = ((num3 < numberOfMaximumHideoutsAtEachBanditFaction) ? (0.25f + 0.75f * num9) : 0f);
					}
					else
					{
						num10 = Math.Max(0f, 1f * (1f - (float)(Math.Min(numberOfMaximumBanditPartiesInEachHideout, num8) - numberOfMinimumBanditPartiesInAHideoutToInfestIt) / (float)(numberOfMaximumBanditPartiesInEachHideout - numberOfMinimumBanditPartiesInAHideoutToInfestIt)));
					}
					float num11 = (mobileParty.DefaultBehavior == AiBehavior.GoToSettlement && mobileParty.TargetSettlement == settlement) ? 1f : (MBRandom.RandomFloat * MBRandom.RandomFloat * MBRandom.RandomFloat * MBRandom.RandomFloat * MBRandom.RandomFloat * MBRandom.RandomFloat * MBRandom.RandomFloat * MBRandom.RandomFloat);
					float visitingNearbySettlementScore = num7 * num10 * num2 * num11 * num4;
					this.AddBehaviorTupleWithScore(p, hideout2.Settlement, visitingNearbySettlementScore);
				}
			}
		}

		// Token: 0x06003B8C RID: 15244 RVA: 0x0011B41C File Offset: 0x0011961C
		private ValueTuple<float, float, float, float> CalculateBeingSettlementOwnerScores(MobileParty mobileParty, Settlement settlement, Settlement currentSettlement, float idealGarrisonStrengthPerWalledCenter, float distanceScorePure, float averagePartySizeRatioToMaximumSize)
		{
			float num = 1f;
			float num2 = 1f;
			float num3 = 1f;
			float item = 1f;
			Hero leaderHero = mobileParty.LeaderHero;
			IFaction mapFaction = mobileParty.MapFaction;
			if (currentSettlement != settlement && (mobileParty.Army == null || mobileParty.Army.LeaderParty != mobileParty))
			{
				if (settlement.OwnerClan.Leader == leaderHero)
				{
					float currentTime = Campaign.CurrentTime;
					float lastVisitTimeOfOwner = settlement.LastVisitTimeOfOwner;
					float num4 = ((currentTime - lastVisitTimeOfOwner > 24f) ? (currentTime - lastVisitTimeOfOwner) : ((24f - (currentTime - lastVisitTimeOfOwner)) * 15f)) / 360f;
					num += num4;
				}
				if (MBRandom.RandomFloat < 0.1f && settlement.IsFortification && leaderHero.Clan != Clan.PlayerClan && (settlement.OwnerClan.Leader == leaderHero || settlement.OwnerClan == leaderHero.Clan))
				{
					if (idealGarrisonStrengthPerWalledCenter == -1f)
					{
						idealGarrisonStrengthPerWalledCenter = FactionHelper.FindIdealGarrisonStrengthPerWalledCenter(mapFaction as Kingdom, null);
					}
					int num5 = Campaign.Current.Models.SettlementGarrisonModel.FindNumberOfTroopsToTakeFromGarrison(mobileParty, settlement, idealGarrisonStrengthPerWalledCenter);
					if (num5 > 0)
					{
						num2 = 1f + MathF.Pow((float)num5, 0.67f);
						if (mobileParty.Army != null && mobileParty.Army.LeaderParty == mobileParty)
						{
							num2 = 1f + (num2 - 1f) / MathF.Sqrt((float)mobileParty.Army.Parties.Count);
						}
					}
				}
			}
			if (settlement == leaderHero.HomeSettlement && mobileParty.Army == null)
			{
				float num6 = leaderHero.HomeSettlement.IsCastle ? 1.5f : 1f;
				if (currentSettlement == settlement)
				{
					num3 += 3000f * num6 / (250f + leaderHero.PassedTimeAtHomeSettlement * leaderHero.PassedTimeAtHomeSettlement);
				}
				else
				{
					num3 += 1000f * num6 / (250f + leaderHero.PassedTimeAtHomeSettlement * leaderHero.PassedTimeAtHomeSettlement);
				}
			}
			if (settlement != currentSettlement)
			{
				float num7 = 1f;
				if (mobileParty.LastVisitedSettlement == settlement)
				{
					num7 = 0.25f;
				}
				if (settlement.IsFortification && settlement.MapFaction == mapFaction && settlement.OwnerClan != Clan.PlayerClan)
				{
					float num8 = (settlement.Town.GarrisonParty != null) ? settlement.Town.GarrisonParty.Party.TotalStrength : 0f;
					float num9 = FactionHelper.OwnerClanEconomyEffectOnGarrisonSizeConstant(settlement.OwnerClan);
					float num10 = FactionHelper.SettlementProsperityEffectOnGarrisonSizeConstant(settlement);
					float num11 = FactionHelper.SettlementFoodPotentialEffectOnGarrisonSizeConstant(settlement);
					if (idealGarrisonStrengthPerWalledCenter == -1f)
					{
						idealGarrisonStrengthPerWalledCenter = FactionHelper.FindIdealGarrisonStrengthPerWalledCenter(mapFaction as Kingdom, null);
					}
					float num12 = idealGarrisonStrengthPerWalledCenter;
					if (settlement.Town.GarrisonParty != null && settlement.Town.GarrisonParty.PaymentLimit > 0 && !settlement.Town.GarrisonParty.UnlimitedWage)
					{
						num12 = (float)(settlement.Town.GarrisonParty.PaymentLimit / Campaign.Current.Models.PartyWageModel.GetCharacterWage(3));
					}
					else
					{
						if (mobileParty.Army != null)
						{
							num12 *= 0.75f;
						}
						num12 *= num9 * num10 * num11;
					}
					float num13 = num12;
					if (num8 < num13)
					{
						float num14 = (settlement.OwnerClan == leaderHero.Clan) ? 149f : 99f;
						if (settlement.OwnerClan == Clan.PlayerClan)
						{
							num14 *= 0.5f;
						}
						float num15 = 1f - num8 / num13;
						item = 1f + num14 * distanceScorePure * distanceScorePure * averagePartySizeRatioToMaximumSize * num15 * num15 * num15 * num7;
					}
				}
			}
			return new ValueTuple<float, float, float, float>(num, num2, num3, item);
		}

		// Token: 0x06003B8D RID: 15245 RVA: 0x0011B7A0 File Offset: 0x001199A0
		private float CalculateMergeScoreForDisbandingParty(MobileParty disbandParty, Settlement settlement, float distance)
		{
			float num = MathF.Pow(3.5f - 0.95f * (Math.Min(Campaign.MapDiagonal, distance) / Campaign.MapDiagonal), 3f);
			Hero owner = disbandParty.Party.Owner;
			float num2;
			if (((owner != null) ? owner.Clan : null) != settlement.OwnerClan)
			{
				Hero owner2 = disbandParty.Party.Owner;
				num2 = ((((owner2 != null) ? owner2.MapFaction : null) == settlement.MapFaction) ? 0.35f : 0.025f);
			}
			else
			{
				num2 = 1f;
			}
			float num3 = num2;
			float num4 = (disbandParty.DefaultBehavior == AiBehavior.GoToSettlement && disbandParty.TargetSettlement == settlement) ? 1f : 0.3f;
			float num5 = settlement.IsFortification ? 3f : 1f;
			float num6 = num * num3 * num4 * num5;
			if (num6 < 0.025f)
			{
				num6 = 0.035f;
			}
			return num6;
		}

		// Token: 0x06003B8E RID: 15246 RVA: 0x0011B870 File Offset: 0x00119A70
		private float CalculateMergeScoreForLeaderlessParty(MobileParty leaderlessParty, Settlement settlement, float distance)
		{
			if (settlement.IsVillage)
			{
				return -1.6f;
			}
			float num = MathF.Pow(3.5f - 0.95f * (Math.Min(Campaign.MapDiagonal, distance) / Campaign.MapDiagonal), 3f);
			float num2;
			if (leaderlessParty.ActualClan != settlement.OwnerClan)
			{
				Clan actualClan = leaderlessParty.ActualClan;
				num2 = ((((actualClan != null) ? actualClan.MapFaction : null) == settlement.MapFaction) ? 0.35f : 0f);
			}
			else
			{
				num2 = 2f;
			}
			float num3 = num2;
			float num4 = (leaderlessParty.DefaultBehavior == AiBehavior.GoToSettlement && leaderlessParty.TargetSettlement == settlement) ? 1f : 0.3f;
			float num5 = settlement.IsFortification ? 3f : 0.5f;
			return num * num3 * num4 * num5;
		}

		// Token: 0x06003B8F RID: 15247 RVA: 0x0011B928 File Offset: 0x00119B28
		private SortedList<ValueTuple<float, int>, Settlement> FindSettlementsToVisitWithDistances(MobileParty mobileParty)
		{
			SortedList<ValueTuple<float, int>, Settlement> sortedList = new SortedList<ValueTuple<float, int>, Settlement>();
			MapDistanceModel mapDistanceModel = Campaign.Current.Models.MapDistanceModel;
			if (mobileParty.LeaderHero != null && mobileParty.LeaderHero.MapFaction.IsKingdomFaction)
			{
				if (mobileParty.Army == null || mobileParty.Army.LeaderParty == mobileParty)
				{
					foreach (Settlement settlement in Settlement.FindSettlementsAroundPosition(mobileParty.Position2D, 30f, (Settlement s) => !s.IsCastle && this.IsSettlementSuitableForVisitingCondition(mobileParty, s)))
					{
						float distance = mapDistanceModel.GetDistance(mobileParty, settlement);
						if (distance < 350f)
						{
							sortedList.Add(new ValueTuple<float, int>(distance, settlement.GetHashCode()), settlement);
						}
					}
				}
				using (List<Settlement>.Enumerator enumerator2 = mobileParty.MapFaction.Settlements.GetEnumerator())
				{
					while (enumerator2.MoveNext())
					{
						Settlement settlement2 = enumerator2.Current;
						if (this.IsSettlementSuitableForVisitingCondition(mobileParty, settlement2))
						{
							float distance2 = mapDistanceModel.GetDistance(mobileParty, settlement2);
							if (distance2 < 350f && distance2 > 900f)
							{
								sortedList.Add(new ValueTuple<float, int>(distance2, settlement2.GetHashCode()), settlement2);
							}
						}
					}
					return sortedList;
				}
			}
			foreach (Settlement settlement3 in Settlement.FindSettlementsAroundPosition(mobileParty.Position2D, 50f, (Settlement s) => this.IsSettlementSuitableForVisitingCondition(mobileParty, s)))
			{
				float distance3 = mapDistanceModel.GetDistance(mobileParty, settlement3);
				if (distance3 < 350f)
				{
					sortedList.Add(new ValueTuple<float, int>(distance3, settlement3.GetHashCode()), settlement3);
				}
			}
			return sortedList;
		}

		// Token: 0x06003B90 RID: 15248 RVA: 0x0011BB48 File Offset: 0x00119D48
		private void AddBehaviorTupleWithScore(PartyThinkParams p, Settlement settlement, float visitingNearbySettlementScore)
		{
			AIBehaviorTuple aibehaviorTuple = new AIBehaviorTuple(settlement, AiBehavior.GoToSettlement, false);
			if (p.AIBehaviorScores.ContainsKey(aibehaviorTuple))
			{
				Dictionary<AIBehaviorTuple, float> aibehaviorScores = p.AIBehaviorScores;
				AIBehaviorTuple key = aibehaviorTuple;
				aibehaviorScores[key] += visitingNearbySettlementScore;
				return;
			}
			p.AIBehaviorScores.Add(aibehaviorTuple, visitingNearbySettlementScore);
		}

		// Token: 0x06003B91 RID: 15249 RVA: 0x0011BB94 File Offset: 0x00119D94
		private bool IsSettlementSuitableForVisitingCondition(MobileParty mobileParty, Settlement settlement)
		{
			return settlement.Party.MapEvent == null && settlement.Party.SiegeEvent == null && (!mobileParty.Party.Owner.MapFaction.IsAtWarWith(settlement.MapFaction) || (mobileParty.Party.Owner.MapFaction.IsMinorFaction && settlement.IsVillage)) && (settlement.IsVillage || settlement.IsFortification) && (!settlement.IsVillage || settlement.Village.VillageState == Village.VillageStates.Normal);
		}

		// Token: 0x040011FD RID: 4605
		private const float NumberOfHoursAtDay = 24f;

		// Token: 0x040011FE RID: 4606
		private const float IdealTimePeriodForVisitingOwnedSettlement = 360f;

		// Token: 0x040011FF RID: 4607
		private const float DefaultMoneyLimitForRecruiting = 2000f;

		// Token: 0x04001200 RID: 4608
		private const float MaximumMeaningfulDistance = 250f;

		// Token: 0x04001201 RID: 4609
		private const float MaximumFilteredDistance = 350f;

		// Token: 0x04001202 RID: 4610
		private const float MeaningfulScoreThreshold = 0.025f;

		// Token: 0x04001203 RID: 4611
		private const float GoodEnoughScore = 2.5f;

		// Token: 0x04001204 RID: 4612
		private const float BaseVisitScore = 1.6f;
	}

}
