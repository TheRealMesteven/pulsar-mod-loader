
namespace PulsarModLoader.Content.Talents
{
    public abstract class TalentTreeMod
    {
        public TalentTreeMod() { }
        public virtual ETalents Talent { get { return ETalents.MAX; } }
        public virtual int ClassID { get { return -1; } }
        public virtual int[] Race { get { return null; } } // 0 = Human, 1 = Sylvassi, 2 = Robot
        public virtual int[] Faction { get { return null; } } // -1 = Unaligned, 0 = CU, 1 = AOG, 2 = WD, 3 = FB, 5 = PT
        public virtual bool Disable { get { return false; } }
        public virtual string[] ConflictingModdedTalents { get { return null; } } // Use talent name so can be ID'd later
        public virtual ETalents[] ConflictingDefaultTalents { get { return null; } }
    }
}
