using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PulsarModLoader
{
    /// <summary>
    /// Used by PML to signify a BepInEx plugin. Must have a unique harmonyID
    /// </summary>
    public class PulsarPlugin : PulsarMod
    {
        public PulsarPlugin()
        {
            // Override default behavior to skip Harmony setup
            harmony = null;
        }

        public override string HarmonyIdentifier()
        {
            // Still required, but unused since we don’t patch anything
            return "YourName.YourModName";
        }

        // Optional: override Unload to skip unpatching
        public override void Unload()
        {
            // Do not call base.Unload, so no unpatching is done
        }
    }
}
