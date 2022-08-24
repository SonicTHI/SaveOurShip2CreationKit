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
    public class Dialog_NameShip : Dialog_Rename
    {
        private CompNameMeShip comp;

        public Dialog_NameShip(CompNameMeShip comp)
        {
            this.comp = comp;
            curName = comp.enemyShipDef;
        }

        protected override void SetName(string name)
        {
            if (name == comp.enemyShipDef || string.IsNullOrEmpty(name))
                return;
            if (!DefDatabase<EnemyShipDef>.AllDefs.Where(s => s.defName.Equals(name)).Any())
            {
                Messages.Message("ERROR: invalid EnemyShipDef!", MessageTypeDefOf.RejectInput);
                return;
            }
            comp.enemyShipDef = name;
            //spawn an end marker
            EnemyShipDef shipDef = DefDatabase<EnemyShipDef>.AllDefs.FirstOrDefault(s => s.defName.Equals(name));
            IntVec3 offset = comp.parent.Position;
            offset.x += shipDef.sizeX;
            offset.z += shipDef.sizeZ;
            Thing thing = ThingMaker.MakeThing(ThingDef.Named("ShipPartShipEnd"));
            thing.SetFaction(Faction.OfPlayer);
            comp.ShipPartShipEnd = GenSpawn.Spawn(thing, offset, comp.parent.Map);
            comp.ShipPartShipEnd.TryGetComp<CompNameMeShip>().enemyShipDef = name;
        }
    }
    public class CompNameMeShip : ThingComp
    {
        public string enemyShipDef;
        public Thing ShipPartShipEnd;
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }
            if (this.parent.def.defName.Equals("ShipPartShip"))
            {
                Command_Action rename = new Command_Action
                {
                    action = delegate
                    {
                        Find.WindowStack.Add(new Dialog_NameShip(this));
                    },
                    defaultLabel = "Set enemyShipDef",
                    defaultDesc = "Select which ship to spawn",
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/RenameZone")
                };
                yield return rename;
            }
        }
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
        }
        public override void PostDeSpawn(Map map)
        {
            if (ShipPartShipEnd != null)
                ShipPartShipEnd.Destroy(DestroyMode.Vanish);
            base.PostDeSpawn(map);
        }
        public override string CompInspectStringExtra()
        {
            return base.CompInspectStringExtra()+"Shipdef: "+ enemyShipDef+".";
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<string>(ref enemyShipDef, "pawnKindDef");
        }
    }
}
