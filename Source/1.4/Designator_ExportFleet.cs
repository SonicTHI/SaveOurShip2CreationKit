using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using SaveOurShip2;

namespace RimWorld
{
    class Designator_ExportFleet : Designator
    {
        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            return true;
        }

        public Designator_ExportFleet()
        {
            defaultLabel = "Export Fleet";
            defaultDesc = "Save this fleet to an XML file. You will need to set the name and tags manually. Click anywhere on the map to activate.";
            icon = ContentFinder<Texture2D>.Get("UI/Save_XML");
            soundDragSustain = SoundDefOf.Designate_DragStandard;
            soundDragChanged = SoundDefOf.Designate_DragStandard_Changed;
            useMouseIcon = true;
            soundSucceeded = SoundDefOf.Designate_Deconstruct;
        }

        public override void DesignateSingleCell(IntVec3 loc)
        {
            if (!Find.CurrentMap.IsSpace())
            {
                Messages.Message("Not on space map", MessageTypeDefOf.RejectInput);
                return;
            }
            int combatPoints = 0;
            List<OffsetShip> ships = new List<OffsetShip>();
            foreach (Building b in Find.CurrentMap.listerBuildings.allBuildingsColonist)
            {
                if (b.def.defName.Equals("ShipPartShip"))
                {
                    EnemyShipDef shipDef = DefDatabase<EnemyShipDef>.AllDefs.Where(s => s.defName.Equals(b.TryGetComp<CompNameMeShip>().enemyShipDef)).FirstOrDefault();
                    if (shipDef == null)
                    {
                        Messages.Message("ERROR: invalid EnemyShipDef found, aborting export!", MessageTypeDefOf.RejectInput);
                        return;
                    }
                    combatPoints += shipDef.combatPoints;
                    OffsetShip ship;
                    ship.ship = shipDef.defName;
                    ship.offsetX = b.Position.x;
                    ship.offsetZ = b.Position.z;
                    ships.Add(ship);
                }
                else if (b.def.defName.Equals("ShipPartFake")) { }
                else
                {
                    Messages.Message("ERROR: found things other than fleet spawns, aborting export!", MessageTypeDefOf.RejectInput);
                    return;
                }
            }
            if (ships.NullOrEmpty())
            {
                Messages.Message("ERROR: no valid EnemyShipDefs found, aborting export!", MessageTypeDefOf.RejectInput);
                return;
            }
            string path = Path.Combine(GenFilePaths.SaveDataFolderPath, "ExportedShips");
            DirectoryInfo dir = new DirectoryInfo(path);
            if (!dir.Exists)
                dir.Create();
            string filename = Path.Combine(path, "fleetTemp.xml");
            string newfleet = "[INSERT UNIQUE NAME HERE]";
            int saveSysVer = 2;

            SafeSaver.Save(filename, "Defs", () =>
            {
                Scribe.EnterNode("EnemyShipDef");
                Map m = Find.CurrentMap;
                Scribe_Values.Look<string>(ref newfleet, "defName");
                string placeholder = "[INSERT IN-GAME NAME HERE]";
                Scribe_Values.Look<int>(ref saveSysVer, "saveSysVer", 1);
                Scribe_Values.Look<string>(ref placeholder, "label");
                Scribe_Values.Look<int>(ref combatPoints, "combatPoints", 0);
                Scribe.EnterNode("ships");
                    foreach (OffsetShip ship in ships)
                    {
                        Scribe.EnterNode("li");
                        string name = ship.ship;
                        Scribe_Values.Look<string>(ref name, "ship");
                        int offsetX = ship.offsetX;
                        Scribe_Values.Look<int>(ref offsetX, "offsetX");
                        int offsetZ = ship.offsetZ;
                        Scribe_Values.Look<int>(ref offsetZ, "offsetZ");
                        Scribe.ExitNode();
                    }
                    Scribe.ExitNode();
                Scribe.EnterNode("parts");
                Scribe.ExitNode();
            });
            Messages.Message("Saved fleet in temp file: fleetTemp.xml", MessageTypeDefOf.PositiveEvent);
        }
    }
}
