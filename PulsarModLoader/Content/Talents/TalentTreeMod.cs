
namespace PulsarModLoader.Content.Talents
{
    public abstract class TalentTreeMod
    {
        public TalentTreeMod() { }
        public virtual ETalents Talent { get { return ETalents.MAX; } }
        public virtual int ClassID { get { return -1; } }
        public virtual int[] Race { get { return null; } } // 0 = Human, 1 = Sylvassi, 2 = Robot
        public virtual bool Disable { get { return false; } }
    }
}
