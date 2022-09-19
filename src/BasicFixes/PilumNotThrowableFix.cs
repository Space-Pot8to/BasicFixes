using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using TaleWorlds.ObjectSystem;

using HarmonyLib;

namespace BasicFixes
{
    /// <summary>
    /// Makes the Pilum and Triangular Throwing Spear throwable. Pilum because Pilum literally 
    /// means javelin in Latin. Triangular Throwing Spear because it says it's throwable.
    /// </summary>
    public class PilumNotThrowableFix : BasicFix
    {
        public PilumNotThrowableFix(bool isEnabled) : base(isEnabled)
        {
            // remove xml and xslt that fixes the non-throwable pilum and triangular throwing spear
            if (!isEnabled)
            {
                List<MbObjectXmlInformation> infos = XmlResource.XmlInformationList.Where(x => x.ModuleName == "BasicFixes").ToList();
                XmlResource.XmlInformationList.RemoveAll(x => infos.Contains(x));
            }

            // base.SimpleHarmonyPatches.Add(new XmlResource_GetXmlListAndApply_Patch(isEnabled));
        }

        [HarmonyPatch]
        public class XmlResource_GetXmlListAndApply_Patch : SimpleHarmonyPatch
        {
            private bool _isEnabled;
            public XmlResource_GetXmlListAndApply_Patch(bool isEnabled)
            {
                _isEnabled = isEnabled;
            }

            public override MethodBase TargetMethod 
            {
                get
                {
                    return AccessTools.FirstMethod(typeof(XmlResource), x => x.Name == "GetXmlListAndApply" && x.IsStatic);
                }
            }

            public override string PatchType { get { return "Prefix"; } }

            public static bool Prefix(string moduleName)
            {
                if(moduleName == "BasicFixes")
                    return false;
                return true;
            }
        }
    }
}
