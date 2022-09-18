using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets.Party;
using TaleWorlds.TwoDimension;

namespace BasicFixes
{
	/// <summary>
	/// Problem describe here: https://forums.taleworlds.com/index.php?threads/troop-list-bouncing.453551/
	/// The list of troops in the party screen sometimes bounces around when units are clicked on. 
	/// This is because the scroll bar for the ui doesn't get set to the proper value in 
	/// ScrollablePanel.ScrollToChild. The patch for this fix just prevents the scrollbar from 
	/// being set to a new value in ScrollablePanel.ScrollToChild. It's resized elsewhere, so this 
	/// is fine.
	/// </summary>
	public class BouncingScrollablePanelFix : BasicFix
    {
        public BouncingScrollablePanelFix() : base()
        {
            // base.SimpleHarmonyPatches.Add(new NavigatableListPanel_OnWidgetGainedGamepadFocus_Patch());
			base.SimpleHarmonyPatches.Add(new ScrollablePanel_ScrollToChild_Patch());
        }

		[HarmonyPatch]
		public class NavigatableListPanel_OnWidgetGainedGamepadFocus_Patch : SimpleHarmonyPatch
		{
			public override MethodBase TargetMethod
			{
				get
				{
					return AccessTools.FirstMethod(typeof(NavigatableListPanel), x => x.Name ==  "OnWidgetGainedGamepadFocus");
				}
			}

			public override string PatchType { get { return "Prefix"; } }

			public static void Prefix(NavigatableListPanel __instance, ref Widget widget)
            {
                if (__instance.ParentPanel != null)
                {
					Type tuple2 = widget.GetType();
					List<Type> types = widget.Parents.Select(x => x.GetType()).ToList();
					Widget tuple = widget.Parents.FirstOrDefault(x => x.GetType() == typeof(NavigatableListPanel));
                }
            }
		}


		/// <summary>
		/// Disables ScrollablePanel.ScrollToChild since it's the source of the bouncing.
		/// </summary>
        [HarmonyPatch]
        public class ScrollablePanel_ScrollToChild_Patch : SimpleHarmonyPatch
        {
            public override MethodBase TargetMethod
            {
                get
                {
                    return AccessTools.FirstMethod(typeof(ScrollablePanel), x => x.Name == "ScrollToChild");
                }
            }

            public override string PatchType { get { return "Prefix"; } }

			public static float cached = 0f;

            public static bool Prefix(ScrollablePanel __instance, Widget targetWidget, float widgetTargetXValue, float widgetTargetYValue, int scrollXOffset, ref int scrollYOffset)
            {
				return false;

				/**
				if (__instance.ClipRect != null && __instance.InnerPanel != null && __instance.AllChildren.Contains(targetWidget))
				{
					List<float> heights = __instance.AllChildren.Select(x => x.Size.Y).ToList();
					if (__instance.VerticalScrollbar != null)
					{
						bool lowRequest = targetWidget.GlobalPosition.Y - (float)scrollYOffset < __instance.ClipRect.GlobalPosition.Y;

						float diff2 = cached - __instance.VerticalScrollbar.MaxValue;
						bool isGrowing = diff2 > 0f;
						bool isShrinking = diff2 < 0f;

						float requestedEnd = targetWidget.GlobalPosition.Y + targetWidget.Size.Y + (float)scrollYOffset;
						float clipRectEnd = __instance.ClipRect.Size.Y;
						
						bool highRequest = requestedEnd > clipRectEnd;
						float diff = requestedEnd - clipRectEnd;

                        if (isGrowing || isShrinking)
                        {
							float perc2 = diff2 / cached;
							float newScroll = __instance.VerticalScrollbar.ValueFloat / __instance.VerticalScrollbar.MaxValue * MathF.Abs(__instance.VerticalScrollbar.MaxValue - cached);
							newScroll = MathF.Clamp(newScroll, __instance.VerticalScrollbar.MinValue, cached);

							//__instance.VerticalScrollbar.ValueFloat = newScroll;
						}

						if (lowRequest || highRequest)
						{
							if (widgetTargetYValue == -1f)
							{
								widgetTargetYValue = (lowRequest ? 0f : 1f);
							}
							float perc = diff / __instance.VerticalScrollbar.MaxValue;
							
							
						}
						cached = __instance.VerticalScrollbar.MaxValue;
					}
					if (__instance.HorizontalScrollbar != null)
					{
						bool flag3 = targetWidget.GlobalPosition.X - (float)scrollXOffset < __instance.ClipRect.GlobalPosition.X;
						bool flag4 = targetWidget.GlobalPosition.X + targetWidget.Size.X + (float)scrollXOffset > __instance.ClipRect.GlobalPosition.X + __instance.ClipRect.Size.X;
						if (flag3 || flag4)
						{
							if (widgetTargetXValue == -1f)
							{
								widgetTargetXValue = (flag3 ? 0f : 1f);
							}
							float scrollXValueForWidget = GetScrollXValueForWidget(__instance, targetWidget, widgetTargetXValue, (float)(flag3 ? (-(float)scrollXOffset) : scrollXOffset));
							__instance.HorizontalScrollbar.ValueFloat = scrollXValueForWidget;
						}
					}
				}
				return false;
				*/
            }

			private static float GetScrollYValueForWidget(ScrollablePanel instance, Widget widget, float widgetTargetYValue, float offset)
			{

				float clipRectEnd = instance.ClipRect.GlobalPosition.Y + instance.ClipRect.Size.Y;
				float requestedEnd = widget.GlobalPosition.Y + widget.Size.Y + (float)offset;
				float widgetPos = widget.GlobalPosition.Y;
				float rectPos = instance.ClipRect.GlobalPosition.Y;
				float scrolBarPercent = (instance.VerticalScrollbar.ValueFloat - instance.VerticalScrollbar.MinValue) / (instance.VerticalScrollbar.MaxValue - instance.VerticalScrollbar.MinValue);

				float perc = widget.GlobalPosition.Y / (instance.InnerPanel.Size.Y); 

				float amount = MBMath.ClampFloat(widgetTargetYValue, 0f, 1f);
				float start = widget.GlobalPosition.Y + offset;
				float end = widget.GlobalPosition.Y - instance.ClipRect.Size.Y + widget.Size.Y + offset;
				float value = Mathf.Lerp(start, end, amount);
				float num = InverseLerp(instance.InnerPanel.GlobalPosition.Y, instance.InnerPanel.GlobalPosition.Y + instance.InnerPanel.Size.Y - instance.ClipRect.Size.Y, value);

				return MathF.Lerp(instance.VerticalScrollbar.MinValue, instance.VerticalScrollbar.MaxValue, num, 1E-05f);
			}

			// Token: 0x060005D9 RID: 1497 RVA: 0x0001A65C File Offset: 0x0001885C
			private static float GetScrollXValueForWidget(ScrollablePanel instance, Widget widget, float widgetTargetXValue, float offset)
			{
				float amount = MBMath.ClampFloat(widgetTargetXValue, 0f, 1f);
				float value = Mathf.Lerp(widget.GlobalPosition.X + offset, widget.GlobalPosition.X - instance.ClipRect.Size.X + widget.Size.X + offset, amount);
				float num = InverseLerp(instance.InnerPanel.GlobalPosition.X, instance.InnerPanel.GlobalPosition.X + instance.InnerPanel.Size.X - instance.ClipRect.Size.X, value);
				num = MathF.Clamp(num, 0f, 1f);
				return MathF.Lerp(instance.HorizontalScrollbar.MinValue, instance.HorizontalScrollbar.MaxValue, num, 1E-05f);
			}

			private static float InverseLerp(float fromValue, float toValue, float value)
			{
				if (fromValue == toValue)
				{
					return 0f;
				}
				return (value - fromValue) / (toValue - fromValue);
			}
		}
    }
}
