using RimworldMod;
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
    class Designator_ExportShipNew : Designator
    {
        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            return true;
        }

        public Designator_ExportShipNew()
        {
            defaultLabel = "Export Ship V2";
            defaultDesc = "Save this ship to an XML file. You will need to set the name and tags manually. Click anywhere on the map to activate.";
            icon = ContentFinder<Texture2D>.Get("UI/Save_XML");
            soundDragSustain = SoundDefOf.Designate_DragStandard;
            soundDragChanged = SoundDefOf.Designate_DragStandard_Changed;
            useMouseIcon = true;
            soundSucceeded = SoundDefOf.Designate_Deconstruct;
        }

        //new save system from min x/z
        public override void DesignateSingleCell(IntVec3 loc)
        {
            if(!this.Map.IsSpace())
            {
                Messages.Message("Not on space map", MessageTypeDefOf.RejectInput);
                return;
            }
             //(Building_ShipBridge)Find.CurrentMap.spawnedThings.Where(t => t is Building_ShipBridge).FirstOrDefault();
            Building_ShipBridge shipCore = null;
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
                if (b.def == ShipInteriorMod2.hullPlateDef || b.def == ShipInteriorMod2.mechHullPlateDef || b.def == ShipInteriorMod2.archoHullPlateDef)
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

            char charPointer = '?';
            Dictionary<char, ShipShape> symbolTable = new Dictionary<char, ShipShape>();
            Dictionary<ShipShape, char> symbolTableBackwards = new Dictionary<ShipShape, char>();
            List<ShipPosRotShape> shipStructure = new List<ShipPosRotShape>();
            HashSet<IntVec3> mechBugfix = new HashSet<IntVec3>();

            foreach (Thing t in Find.CurrentMap.spawnedThings)
            {
                if (t is Pawn || t == shipCore)// || SoSBuilder.thingsNotToSave.Contains(t))
                {
                    continue;
                }
                if (t.TryGetComp<CompRoofMe>() != null && t.TryGetComp<CompRoofMe>().Props.mechanoid)
                {
                    if (!t.def.building.isEdifice && mechBugfix.Contains(t.Position))
                        continue;
                    mechBugfix.Add(t.Position);
                }

                ShipShape shape = new ShipShape();
                if (t is Building_ShipRegion)
                {
                    shape.width = ((Building_ShipRegion)t).width;
                    shape.height = ((Building_ShipRegion)t).height;
                    shape.shapeOrDef = "Cargo";
                }
                else
                {
                    shape.shapeOrDef=t.def.defName;
                    if (t.def.MadeFromStuff)
                    {
                        shape.stuff = t.Stuff.defName;
                    }
                    else if (t.TryGetComp<CompNameMe>() != null)
                    {
                        shape.stuff = t.TryGetComp<CompNameMe>().pawnKindDef;
                    }
                    else if (t.TryGetComp<CompShipCombatShield>() != null)
                    {
                        shape.radius = t.TryGetComp<CompShipCombatShield>().radiusSet;
                    }
                    if (t.TryGetComp<CompColorable>() != null && t.TryGetComp<CompColorable>().Color != null && t.TryGetComp<CompColorable>().Color != Color.white)
                    {
                        shape.color = t.TryGetComp<CompColorable>().Color;
                    }
                }
                shape.x = t.Position.x - minX;
                shape.z = t.Position.z - minZ;
                shape.rot = t.Rotation;

                if(!symbolTableBackwards.ContainsKey(shape))
                {
                    symbolTable.Add(charPointer, shape);
                    symbolTableBackwards.Add(shape, charPointer);
                    charPointer = (char)(((int)charPointer) + 1);
                    if (charPointer == '|')
                        charPointer = (char)(((int)charPointer) + 1);
                }
                ShipPosRotShape posrot = new ShipPosRotShape();
                posrot.x = shape.x;
                posrot.z = shape.z;
                posrot.rot = shape.rot;
                posrot.shape = symbolTableBackwards[shape]+"";
                shipStructure.Add(posrot);
            }

            foreach (IntVec3 cell in Find.CurrentMap.AllCells)
            {
                TerrainDef def = Find.CurrentMap.terrainGrid.TerrainAt(cell);
                if (def.defName != "EmptySpace" && def != ShipInteriorMod2.hullFloorDef && def != ShipInteriorMod2.mechHullFloorDef && def != ShipInteriorMod2.archoHullFloorDef)
                {
                    ShipShape shape = new ShipShape();
                    shape.shapeOrDef = def.defName;
                    shape.x = cell.x - minX;
                    shape.z = cell.z - minZ;

                    if (!symbolTableBackwards.ContainsKey(shape))
                    {
                        symbolTable.Add(charPointer, shape);
                        symbolTableBackwards.Add(shape, charPointer);
                        charPointer = (char)(((int)charPointer) + 1);
                        if (charPointer == '|')
                            charPointer = (char)(((int)charPointer) + 1);
                    }
                    ShipPosRotShape posrot = new ShipPosRotShape();
                    posrot.x = shape.x;
                    posrot.z = shape.z;
                    posrot.rot = shape.rot;
                    posrot.shape = symbolTableBackwards[shape] + "";
                    shipStructure.Add(posrot);
                }
            }

            string bigString = "";
            bool isFirst = true;
            foreach (ShipPosRotShape shape in shipStructure)
            {
                if (isFirst)
                    isFirst = false;
                else
                    bigString += "|";
                bigString += shape.x + "," + shape.z + "," + shape.rot.AsInt + "," + shape.shape;
            }
            SafeSaver.Save(filename, "Defs", () =>
            {
                Scribe.EnterNode("EnemyShipDef");
                Map m = Find.CurrentMap;
                Scribe_Values.Look<string>(ref shipCore.ShipName, "defName");
                Scribe_Values.Look<int>(ref saveSysVer, "saveSysVer", 1);
                Scribe_Values.Look<int>(ref minX, "offsetX", 0);
                Scribe_Values.Look<int>(ref minZ, "offsetZ", 0);
                Scribe_Values.Look<int>(ref maxX, "sizeX", 0);
                Scribe_Values.Look<int>(ref maxZ, "sizeZ", 0);
                string placeholder = "[INSERT IN-GAME NAME HERE]";
                Scribe_Values.Look<string>(ref placeholder, "label");
                int cargoPlaceholder = 0;
                Scribe_Values.Look<int>(ref combatPoints, "combatPoints", 0);
                Scribe_Values.Look<int>(ref randomTurretPoints, "randomTurretPoints", 0);
                Scribe_Values.Look<int>(ref cargoPlaceholder, "cargoValue", 0);
                bool temp = false;
                Scribe_Values.Look<bool>(ref temp, "neverRandom", forceSave: true);
                Scribe_Values.Look<bool>(ref temp, "neverAttacks", forceSave: true);
                Scribe_Values.Look<bool>(ref temp, "spaceSite", forceSave: true);
                Scribe_Values.Look<bool>(ref temp, "imperialShip", forceSave: true);
                Scribe_Values.Look<bool>(ref temp, "pirateShip", forceSave: true);
                Scribe_Values.Look<bool>(ref temp, "bountyShip", forceSave: true);
                Scribe_Values.Look<bool>(ref temp, "mechanoidShip", forceSave: true);
                Scribe_Values.Look<bool>(ref temp, "fighterShip", forceSave: true);
                Scribe_Values.Look<bool>(ref temp, "carrierShip", forceSave: true);
                Scribe_Values.Look<bool>(ref temp, "tradeShip", forceSave: true);
                Scribe_Values.Look<bool>(ref temp, "startingShip", forceSave: true);
                Scribe_Values.Look<bool>(ref temp, "startingDungeon", forceSave: true);
                Scribe.EnterNode("core");
                Scribe_Values.Look<string>(ref shipCore.def.defName, "shapeOrDef");
                int cx = shipCore.Position.x - minX;
                Scribe_Values.Look<int>(ref cx, "x");
                int cz = shipCore.Position.z - minZ;
                Scribe_Values.Look<int>(ref cz, "z");
                Rot4 crot = shipCore.Rotation;
                Scribe_Values.Look<Rot4>(ref crot, "rot");
                Scribe.ExitNode();
                Scribe.EnterNode("symbolTable");
                foreach (char key in symbolTable.Keys)
                {
                    Scribe.EnterNode("li");
                    char realKey = key;
                    Scribe_Values.Look<char>(ref realKey, "key"); ;
                    ShipShape realShape = symbolTable[key];
                    Scribe_Deep.Look<ShipShape>(ref realShape, "value");
                    Scribe.ExitNode();
                }
                Scribe.ExitNode();
                Scribe_Values.Look<string>(ref bigString, "bigString");
                Scribe.ExitNode();
            });
            Messages.Message("Saved ship as: " + shipCore.ShipName + ".xml", shipCore, MessageTypeDefOf.PositiveEvent);
        }
    }
}
