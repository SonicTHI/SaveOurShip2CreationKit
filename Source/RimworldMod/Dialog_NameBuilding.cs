using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace SaveOurShip2
{
    public class Dialog_NameBuilding : Dialog_Rename
    {
        private CompNameMe comp;

        public Dialog_NameBuilding(CompNameMe comp)
        {
            this.comp = comp;
            curName = comp.pawnKindDef;
        }

        protected override void SetName(string name)
        {
            if (name == comp.pawnKindDef || string.IsNullOrEmpty(name))
                return;

            comp.pawnKindDef = name;
        }
    }
}