using UnityEngine;

namespace PulsarManager.Content.Components.PolytechModule
{
    public class PolytechModuleMod : ComponentModBase
    {
        public virtual float MaxPowerUsage_Watts
        {
            get { return 0f; }
        }

        public override Texture2D IconTexture
        {
            get { return (Texture2D)Resources.Load("Icons/71_Processer"); }
        }

        public override bool Experimental => true;
    }
}
