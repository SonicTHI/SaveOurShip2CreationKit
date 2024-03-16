/*using RimworldMod;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using SaveOurShip2;

namespace RimWorld
{
    class Designator_ExportShipLegacy : Designator
    {
        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            return true;
        }

        public Designator_ExportShipLegacy()
        {
            defaultLabel = "Export Ship (legacy version)";
            defaultDesc = "Save this ship to an XML file only if you plan to edit it manually. Click anywhere on the map to activate.";
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
            Building_ShipBridge shipCore = (Building_ShipBridge)Find.CurrentMap.spawnedThings.Where(t => t is Building_ShipBridge).FirstOrDefault();
            if(shipCore == null)
            {
                Messages.Message("No ship core found. Build a bridge or AI core.", MessageTypeDefOf.RejectInput);
                return;
            }
            if(shipCore.ShipName==null)
            {
                Messages.Message("Name the ship before saving it", MessageTypeDefOf.RejectInput);
                return;
            }
            int combatPoints = 0;
            int randomTurretPoints = 0;
            int ShipMass = 0;
            int minX = this.Map.Size.x;
            int minZ = this.Map.Size.z;
            int maxX = 0;
            int maxZ = 0;
            int saveSysVer = 1;
            foreach (Thing b in Find.CurrentMap.spawnedThings.Where(b => b is Building))
            {
                if (b.Position.x < minX)
                    minX = b.Position.x;
                if (b.Position.z < minZ)
                    minZ = b.Position.z;
                if (b.Position.x > maxX)
                    maxX = b.Position.x;
                if (b.Position.z > maxZ)
                    maxZ = b.Position.z;
                if (b.TryGetComp<CompSoShipPart>()?.Props.isPlating ?? false)
                    ShipMass += 1;
                else
                {
                    ShipMass += (b.def.size.x * b.def.size.z) * 3;
                    if (b.TryGetComp<CompShipHeat>() != null)
                        combatPoints += b.TryGetComp<CompShipHeat>().Props.threat;
                    else if (b.def == ThingDef.Named("ShipSpinalAmplifier"))
                        combatPoints += 5;
                    else if (b.def == ThingDef.Named("ShipPartTurretSmall")) 
                    {
                        combatPoints += 10;
                        randomTurretPoints += 10;
                    }
                    else if (b.def == ThingDef.Named("ShipPartTurretLarge"))
                    {
                        combatPoints += 30;
                        randomTurretPoints += 30;
                    }
                    else if (b.def == ThingDef.Named("ShipPartTurretSpinal"))
                        combatPoints += 100;
                }
                if (b is Building_ShipBridge bridge)
                    shipCore = bridge;
            }
            if (shipCore == null)
                Messages.Message("Warning: no ship core found! Only use this file if you know what you are doing.", MessageTypeDefOf.RejectInput);
            else if (ShipUtility.ShipBuildingsAttachedTo(shipCore).Count < Find.CurrentMap.spawnedThings.Where(b => b is Building).Count())
            {
                Messages.Message("Warning: found unattached buildings or multiple ships! Only use this file if you know what you are doing.", MessageTypeDefOf.RejectInput);
            }
            else
                saveSysVer = 2;
            if (shipCore.ShipName == null)
            {
                Messages.Message("Warning: no ship name set! You can set it manually in the exported XML", MessageTypeDefOf.RejectInput);
            }

            maxX -= minX;
            maxZ -= minZ;
            combatPoints += ShipMass / 100;

            string path = Path.Combine(GenFilePaths.SaveDataFolderPath, "ExportedShips");
            DirectoryInfo dir = new DirectoryInfo(path);
            if (!dir.Exists)
                dir.Create();
            string filename = Path.Combine(path, shipCore.ShipName + ".xml");
            SafeSaver.Save(filename, "Defs", () =>
            {
                Scribe.EnterNode("EnemyShipDef");
                Scribe_Values.Look<string>(ref shipCore.ShipName,"defName");
                Scribe_Values.Look<int>(ref saveSysVer, "saveSysVer", 1);
                Scribe_Values.Look<int>(ref minX, "offsetX", 0);
                Scribe_Values.Look<int>(ref minZ, "offsetZ", 0);
                Scribe_Values.Look<int>(ref maxX, "sizeX", 0);
                Scribe_Values.Look<int>(ref maxZ, "sizeZ", 0);
                string placeholder = "[INSERT IN-GAME NAME HERE]";
                Scribe_Values.Look<string>(ref placeholder, "label");
                int cargoPlaceholder = 0;
                Scribe_Values.Look<int>(ref combatPoints, "combatPoints",0);
                Scribe_Values.Look<int>(ref randomTurretPoints, "randomTurretPoints", 0);
                Scribe_Values.Look<int>(ref cargoPlaceholder, "cargoValue",0);
                Scribe.EnterNode("parts");
                HashSet<IntVec3> mechBugfix = new HashSet<IntVec3>();
                foreach(Thing t in Find.CurrentMap.spawnedThings)
                {
                    if (SoSBuilder.ExportToIgnore(t, shipCore))
                    {
                        continue;
                    }
                    if(t.TryGetComp<CompSoShipPart>()!=null && t.TryGetComp<CompSoShipPart>().Props.mechanoid)
                    {
                        if (!t.def.building.isEdifice&&mechBugfix.Contains(t.Position))
                            continue;
                        mechBugfix.Add(t.Position);
                    }
                    Scribe.EnterNode("li");
                    if (t is Building_ShipRegion)
                    {
                        Scribe_Values.Look<int>(ref ((Building_ShipRegion)t).width, "width");
                        Scribe_Values.Look<int>(ref ((Building_ShipRegion)t).height, "height");
                        string name = "Cargo";
                        Scribe_Values.Look<string>(ref name, "shapeOrDef");
                    }
                    else
                    {
                        Scribe_Values.Look<string>(ref t.def.defName, "shapeOrDef");
                        if (t.def.MadeFromStuff)
                        {
                            string stuff = t.Stuff.defName;
                            Scribe_Values.Look<string>(ref stuff, "stuff");
                        }
                        else if(t.TryGetComp<CompNameMe>()!=null)
                        {
                            string notStuff = t.TryGetComp<CompNameMe>().pawnKindDef;
                            Scribe_Values.Look<string>(ref notStuff, "stuff");
                        }
                        else if (t.TryGetComp<CompShipCombatShield>() != null)
                        {
                            float radius = t.TryGetComp<CompShipCombatShield>().radiusSet;
                            Scribe_Values.Look<float>(ref radius, "radius");
                        }
                        if (t.TryGetComp<CompColorable>() != null && t.TryGetComp<CompColorable>().Color != null && t.TryGetComp<CompColorable>().Color != Color.white)
                            {
                            Color color = t.TryGetComp<CompColorable>().Color;
                            Scribe_Values.Look<Color>(ref color, "color");
                        }
                        if (t.def.defName.Contains("_SPAWNER"))
                            Log.Message("Spawner " + t.def.defName);
                    }
                    int x = t.Position.x - minX;
                    Scribe_Values.Look<int>(ref x, "x");
                    int z = t.Position.z - minZ;
                    Scribe_Values.Look<int>(ref z, "z");
                    Rot4 rot = t.Rotation;
                    Scribe_Values.Look<Rot4>(ref rot, "rot");
                    Scribe.ExitNode();
                }
                foreach(IntVec3 cell in Find.CurrentMap.AllCells)
                {
                    TerrainDef def = Find.CurrentMap.terrainGrid.TerrainAt(cell);
                    if(def.defName!="EmptySpace"&&def.defName!="FakeFloorInsideShip" && def.defName != "FakeFloorInsideShipMech" && def.defName!= "FakeFloorInsideShipArchotech")
                    {
                        Scribe.EnterNode("li");
                        string name = def.defName;
                        Scribe_Values.Look<string>(ref name, "shapeOrDef");
                        int x = cell.x - minX;
                        Scribe_Values.Look<int>(ref x, "x");
                        int z = cell.z - minZ;
                        Scribe_Values.Look<int>(ref z, "z");
                        Scribe.ExitNode();
                    }
                }
                Scribe.ExitNode();
                Scribe.EnterNode("core");
                Scribe_Values.Look<string>(ref shipCore.def.defName, "shapeOrDef");
                int cx = shipCore.Position.x - minX;
                Scribe_Values.Look<int>(ref cx, "x");
                int cz = shipCore.Position.z - minZ;
                Scribe_Values.Look<int>(ref cz, "z");
                Rot4 crot = shipCore.Rotation;
                Scribe_Values.Look<Rot4>(ref crot, "rot");
                Scribe.ExitNode();
                Scribe.ExitNode();
            });
            Messages.Message("Saved ship as: " + shipCore.ShipName + ".xml", shipCore, MessageTypeDefOf.PositiveEvent);
        }
    }
}
*/