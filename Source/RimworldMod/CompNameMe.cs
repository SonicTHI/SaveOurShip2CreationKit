using SaveOurShip2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace RimWorld
{
    public class CompNameMe : ThingComp
    {
        public string pawnKindDef;

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            List<Gizmo> giz = new List<Gizmo>();
            giz.AddRange(base.CompGetGizmosExtra());
            Command_Action rename = new Command_Action
            {
                action = delegate
                {
                    Find.WindowStack.Add(new Dialog_NameBuilding(this));
                },
                defaultLabel = "Set PawnKindDef",
                defaultDesc = "Select which pawn to spawn",
            };
            rename.icon = ContentFinder<Texture2D>.Get("UI/Commands/RenameZone");
            giz.Add(rename);
            return giz;
        }

        public override string CompInspectStringExtra()
        {
            return base.CompInspectStringExtra() + "Pawn kind: " + pawnKindDef;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<string>(ref pawnKindDef, "pawnKindDef");
        }
    }
}
