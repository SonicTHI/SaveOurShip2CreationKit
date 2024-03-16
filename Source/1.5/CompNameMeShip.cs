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
    public class Dialog_NameShip : Dialog_RenameShip
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
            //spawn ghost
            bool failed = false;
            EnemyShipDef shipDef = DefDatabase<EnemyShipDef>.AllDefs.FirstOrDefault(s => s.defName.Equals(name));
            foreach (ShipShape shape in shipDef.parts)
            {
                if (DefDatabase<ThingDef>.GetNamedSilentFail(shape.shapeOrDef) != null)
                {
                    if (comp.parent.Position.x + shape.x > 239 || comp.parent.Position.z + shape.z > 239)
                    {
                        failed = true;
                        break;
                    }
                    Thing thing;
                    ThingDef def = ThingDef.Named(shape.shapeOrDef);
                    if (def.building != null && def.building.shipPart && comp.parent.Map.listerThings.AllThings.Where(t => t.Position.x == shape.x && t.Position.z == shape.z) != ThingDef.Named("ShipPartFake"))
                    {
                        if (def.size.x == 1 && def.size.z == 1)
                        {
                            thing = ThingMaker.MakeThing(ThingDef.Named("ShipPartFake"));
                            GenSpawn.Spawn(thing, new IntVec3(comp.parent.Position.x + shape.x, 0, comp.parent.Position.z + shape.z), comp.parent.Map, shape.rot);
                            comp.parts.Add(thing);
                            continue;
                        }
                        for (int i = 0; i < def.size.x; i++)
                        {
                            for (int j = 0; j < def.size.z; j++)
                            {
                                int adjx = 0;
                                int adjz = 0;
                                if (shape.rot == Rot4.North || shape.rot == Rot4.South)
                                {
                                    adjx = i - (def.size.x / 2);
                                    adjz = j - (def.size.z / 2);
                                    if (shape.rot == Rot4.North && def.size.z % 2 == 0)
                                        adjz += 1;
                                }
                                else
                                {
                                    adjx = j - (def.size.x / 2);
                                    adjz = i - (def.size.z / 2);
                                    if (def.size.x != def.size.z && def.size.z != 4)
                                    {
                                        adjx -= 1;
                                        adjz += 1;
                                    }
                                    if (shape.rot == Rot4.East && def.size.z % 2 == 0)
                                        adjx += 1;
                                }
                                int x = comp.parent.Position.x + shape.x + adjx;
                                int z = comp.parent.Position.z + shape.z + adjz;
                                if (comp.parent.Map.listerThings.AllThings.Where(t => t.Position.x == x && t.Position.z == z) != ThingDef.Named("ShipPartFake"))
                                {
                                    thing = ThingMaker.MakeThing(ThingDef.Named("ShipPartFake"));
                                    GenSpawn.Spawn(thing, new IntVec3(x, 0, z), comp.parent.Map, shape.rot);
                                    comp.parts.Add(thing);
                                }
                            }
                        }
                    }
                }
            }
            if (failed)
            {
                foreach (Thing t in comp.parts)
                {
                    if (!t.Destroyed)
                        t.Destroy();
                }
                Messages.Message("ERROR: ship out of bounds!", MessageTypeDefOf.RejectInput);
            }
            else
                comp.enemyShipDef = name;
        }
    }
    public class CompNameMeShip : ThingComp
    {
        public string enemyShipDef;
        public List<Thing> parts = new List<Thing>();
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }
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
        public override void PostDeSpawn(Map map)
        {
            foreach (Thing t in parts)
            {
                if (!t.Destroyed)
                    t.Destroy();
            }
            base.PostDeSpawn(map);
        }
        public override string CompInspectStringExtra()
        {
            return base.CompInspectStringExtra()+"Shipdef: "+ enemyShipDef + "\nPos: " + this.parent.Position;
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<string>(ref enemyShipDef, "pawnKindDef");
        }
    }
}
