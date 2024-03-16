using RimWorld;
using Verse;

namespace SaveOurShip2
{
    public class Dialog_NameFactionDef : Dialog_RenameShip
    {
        private CompNameMe comp;

        public Dialog_NameFactionDef(CompNameMe comp)
        {
            this.comp = comp;
            curName = comp.factionDef;
        }

        protected override void SetName(string name)
        {
            if (name == comp.factionDef || string.IsNullOrEmpty(name))
                return;

            comp.factionDef = name;
        }
    }
}