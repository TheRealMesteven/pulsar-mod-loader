
namespace PulsarModLoader.Content.Talents
{
    public abstract class TalentMod
    {
        public TalentMod() { }
        public virtual string Name { get { return ""; } }
        public virtual string Description { get { return ""; } }
        public virtual int MaxRank { get { return 3; } }
        public virtual int ClassID { get { return -1; } }
        public virtual int[] ResearchCost { get { return new int[6]; } }
        public virtual int WarpsToResearch { get { return 3; } }
        public virtual string ExtendsModdedTalent { get { return ""; } } // Use talent name so can be ID'd later
        public virtual ETalents ExtendsDefaultTalent { get { return ETalents.MAX; } }
        public virtual int MinLevel { get { return 0; } }
        public virtual int[] Race { get { return null; } } // 0 = Human, 1 = Sylvassi, 2 = Robot
        public virtual int[] Faction { get { return null; } } // -1 = Unaligned, 0 = CU, 1 = AOG, 2 = WD, 3 = FB, 5 = PT

        public virtual TalentInfo TalentInfo
        {
            get
            {
                TalentInfo info = new TalentInfo();
                info.Name = Name;
                info.Desc = Description;
                info.MaxRank = MaxRank;
                info.ClassID = ClassID;
                info.ResearchCost = ResearchCost;
                info.WarpsToResearch = WarpsToResearch;
                info.ExtendsTalent = ExtendsDefaultTalent;
                //int extendsModded = TalentModManager.Instance.GetTalentIDFromName(ExtendsModdedTalent);
                //if (extendsModded != -1) info.ExtendsTalent = (ETalents)extendsModded;
                info.MinLevel = MinLevel;
                return info;
            }
        }
    }
}
