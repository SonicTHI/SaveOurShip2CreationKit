using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using SaveOurShip2;

namespace RimWorld
{
    public class CompNameMe : ThingComp
    {
        public string pawnKindDef = "pawnKindDef";
        public string factionDef = "factionDef";

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            List<Gizmo> giz = new List<Gizmo>();
            giz.AddRange(base.CompGetGizmosExtra());
            Command_Action rename = new Command_Action
            {
                action = delegate
                            {
                                Find.WindowStack.Add(new Dialog_NamePawnDef(this));
                            },
                defaultLabel = "Set PawnKindDef",
                defaultDesc = "Select which pawn to spawn",
                icon = ContentFinder<Texture2D>.Get("UI/Commands/RenameZone")
            };
            giz.Add(rename);
            Command_Action faction = new Command_Action
            {
                action = delegate
                {
                    Find.WindowStack.Add(new Dialog_NameFactionDef(this));
                },
                defaultLabel = "Set factionDef",
                defaultDesc = "Select factionDef to spawn as",
                icon = ContentFinder<Texture2D>.Get("UI/Commands/RenameZone")
            };
            giz.Add(rename);
            giz.Add(faction);
            return giz;
        }

        public override string CompInspectStringExtra()
        {
            return base.CompInspectStringExtra() + "Pawn kind: " + pawnKindDef + "\nFaction: " + factionDef;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<string>(ref pawnKindDef, "pawnKindDef");
            Scribe_Values.Look<string>(ref factionDef, "factionDef");
        }
    }
}
