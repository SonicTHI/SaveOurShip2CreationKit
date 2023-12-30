using System;
using System.Collections.Generic;
using System.IO;
using RimWorld;
using Verse;
using UnityEngine;
using HarmonyLib;
using SaveOurShip2;
using System.Linq;
using RimWorld.Planet;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using Verse.Noise;
using System.Security.Policy;
using static UnityEngine.Random;

namespace RimworldMod
{
    public class SoSBuilder : Mod
    {
        public static Building_ShipCircle lastCirclePlaced;
        public static Building_ShipRect lastRectPlaced;
        public static Building_ShipRegion lastRegionPlaced;

        public static Dictionary<Map, string> shipDictionary = new Dictionary<Map, string>();

        public SoSBuilder(ModContentPack content) : base(content)
        {
            var harmony = new Harmony("SoSBuilder");
            harmony.PatchAll();
        }

        public static void GenerateHull(List<IntVec3> border, List<IntVec3> interior, Map map, ThingDef hull, ThingDef floor)
        {
            foreach (IntVec3 vec in interior)
            {
                if (vec.GetFirstThing(map, floor) == null)
                {
                    Thing f = ThingMaker.MakeThing(floor);
                    Thing t = GenSpawn.Spawn(f, vec, map);
                }
            }
            foreach (IntVec3 vec in border)
            {
                if (!GenSpawn.WouldWipeAnythingWith(vec, Rot4.South, hull, map, (Thing x) => x.def.category == ThingCategory.Building))
                {
                    Thing wall = ThingMaker.MakeThing(hull);
                    wall.SetFaction(Faction.OfPlayer);
                    Thing t = GenSpawn.Spawn(wall, vec, map);
                }
            }
        }
        //cleanup for bad exports + temp for rework
        public static bool ImportToIgnore(ThingDef def)
        {
            if (def.defName.StartsWith("Lighting_MURWallLight_Glower") || def.defName.Equals("Lighting_MURWallSunLight_Glower") || def.defName.StartsWith("Ship_Beam_Light"))
            {
                return true;
            }
            return false;
        }
        public static bool ExportToIgnore(Thing t, Building_ShipBridge core)
        {
            if (t is Pawn || t == core || t.def.defName.StartsWith("Lighting_MURWallLight_Glower") || t.def.defName.Equals("Lighting_MURWallSunLight_Glower") || t.def.defName.StartsWith("Ship_Beam_Light"))
            {
                return true;
            }
            return false;
        }
        public static void ReSaveAll()
        {
            foreach (EnemyShipDef shipDef in DefDatabase<EnemyShipDef>.AllDefs.ToList())
            {
                ReSave(shipDef);
            }
        }
        public static void ReSave(EnemyShipDef shipDef)
        {
            //recalc threat //td
            int combatPoints = 0;
            /*foreach (ShipShape shape in shipDef.parts)
            {
                IntVec3 adjPos;
                adjPos = new IntVec3(c.x + shape.x, 0, c.z + shape.z);
                if (shape.shapeOrDef.Equals("PawnSpawnerGeneric"))
                {
                    ThingDef def = ThingDef.Named("PawnSpawnerGeneric");
                    Thing thing = ThingMaker.MakeThing(def);
                    GenSpawn.Spawn(thing, adjPos, map);
                    thing.TryGetComp<CompNameMe>().pawnKindDef = shape.stuff;
                }
                else if (shape.shapeOrDef.Equals("Cargo"))
                {
                    lastRegionPlaced = null;
                    ThingDef def = ThingDef.Named("ShipPartRegion");
                    Thing thing = ThingMaker.MakeThing(def);
                    {
                        ((Building_ShipRegion)thing).width = shape.width;
                        ((Building_ShipRegion)thing).height = shape.height;
                    }
                    GenSpawn.Spawn(thing, adjPos, map);
                }
                else if (shape.shapeOrDef == "SoSLightEnabler")
                {
                    spawnLights.Add(adjPos, new Tuple<int, ColorInt, bool>(shape.rot.AsInt, ColorIntUtility.AsColorInt(shape.color != Color.clear ? shape.color : Color.white), shape.alt));
                }
                else if (DefDatabase<ThingDef>.GetNamedSilentFail(shape.shapeOrDef) != null)
                {
                    Thing thing;
                    ThingDef def = ThingDef.Named(shape.shapeOrDef);
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
                        else if (b.TryGetComp<CompEngineTrail>() != null && b.Rotation != Rot4.West)
                            neverFleet = true;
                    }
                    if (b is Building_ShipBridge bridge)
                        shipCore = bridge;



                    if (map.listerThings.AllThings.Where(t => t.Position.x == shape.x && t.Position.z == shape.z) != def)
                    {
                        if (ImportToIgnore(def))
                            continue;
                        Rot4 rota = shape.rot;
                        if (def.MadeFromStuff)
                        {
                            if (shape.stuff != null && !def.defName.StartsWith("Apparel_SpaceSuit"))
                                thing = ThingMaker.MakeThing(def, ThingDef.Named(shape.stuff));
                            else
                                thing = ThingMaker.MakeThing(def, GenStuff.DefaultStuffFor(def));
                        }
                        else
                            thing = ThingMaker.MakeThing(def);

                        if (thing.TryGetComp<CompColorable>() != null && shape.color != Color.clear)
                            thing.SetColor(shape.color);
                        if (thing.def.CanHaveFaction && thing.def != ResourceBank.ThingDefOf.ShipHullTile)
                            thing.SetFaction(Faction.OfPlayer);
                        if (thing.TryGetComp<CompPowerBattery>() != null)
                            thing.TryGetComp<CompPowerBattery>().AddEnergy(thing.TryGetComp<CompPowerBattery>().AmountCanAccept);
                        if (thing.TryGetComp<CompRefuelable>() != null)
                            thing.TryGetComp<CompRefuelable>().Refuel(thing.TryGetComp<CompRefuelable>().Props.fuelCapacity);
                        var shieldComp = thing.TryGetComp<CompShipCombatShield>();
                        if (shieldComp != null)
                        {
                            shieldComp.radiusSet = 40;
                            shieldComp.radius = 40;
                            if (shape.radius != 0)
                            {
                                shieldComp.radiusSet = shape.radius;
                                shieldComp.radius = shape.radius;
                            }
                        }
                        if (thing.def.stackLimit > 1)
                            thing.stackCount = (int)Math.Min(25, thing.def.stackLimit);
                        //GenSpawn.Spawn(thing, adjPos, map, rota);
                    }
                }
                else if (DefDatabase<TerrainDef>.GetNamedSilentFail(shape.shapeOrDef) != null)
                {
                    IntVec3 pos;
                    pos = new IntVec3(shape.x, 0, shape.z);

                    if (shipDef.saveSysVer == 2)
                        pos = adjPos;
                    map.terrainGrid.SetTerrain(pos, DefDatabase<TerrainDef>.GetNamed(shape.shapeOrDef));
                }

            }*/
            //resave //td key = null
            Building core = (Building)ThingMaker.MakeThing(ThingDef.Named(shipDef.core.shapeOrDef));
            SafeSaver.Save("test\\" + shipDef.fileName, "Defs", () =>
            {
                Scribe.EnterNode("EnemyShipDef");
                {
                    Scribe_Values.Look<string>(ref shipDef.defName, "defName");
                    int saveSysVer = 2;
                    Scribe_Values.Look<int>(ref saveSysVer, "saveSysVer", 1);
                    Scribe_Values.Look<int>(ref shipDef.offsetX, "offsetX", 0);
                    Scribe_Values.Look<int>(ref shipDef.offsetZ, "offsetZ", 0);
                    Scribe_Values.Look<int>(ref shipDef.sizeX, "sizeX", 0);
                    Scribe_Values.Look<int>(ref shipDef.sizeZ, "sizeZ", 0);
                    Scribe_Values.Look<string>(ref shipDef.label, "label");

                    Scribe_Values.Look<int>(ref combatPoints, "combatPoints", 0);
                    Scribe_Values.Look<int>(ref shipDef.randomTurretPoints, "randomTurretPoints", 0);
                    Scribe_Values.Look<int>(ref shipDef.cargoValue, "cargoValue", 0);
                    if (shipDef.rarityLevel > 1)
                        Scribe_Values.Look<int>(ref shipDef.rarityLevel, "rarityLevel", 1);

                    if (core != null)
                    {
                        Scribe_Values.Look<bool>(ref shipDef.neverRandom, "neverRandom");
                        Scribe_Values.Look<bool>(ref shipDef.neverAttacks, "neverAttacks");
                        Scribe_Values.Look<bool>(ref shipDef.neverWreck, "neverWreck");
                        Scribe_Values.Look<bool>(ref shipDef.startingShip, "startingShip");
                        Scribe_Values.Look<bool>(ref shipDef.startingDungeon, "startingDungeon");
                        Scribe_Values.Look<bool>(ref shipDef.spaceSite, "spaceSite");
                        Scribe_Values.Look<bool>(ref shipDef.tradeShip, "tradeShip");
                        Scribe_Values.Look<bool>(ref shipDef.navyExclusive, "navyExclusive");
                        Scribe_Values.Look<bool>(ref shipDef.customPaintjob, "customPaintjob");
                        Scribe_Values.Look<bool>(ref shipDef.neverFleet, "neverFleet");
                        Scribe.EnterNode("core");
                        {
                            Scribe_Values.Look<string>(ref core.def.defName, "shapeOrDef");
                            Scribe_Values.Look<int>(ref shipDef.core.x, "x");
                            Scribe_Values.Look<int>(ref shipDef.core.z, "z");
                            Scribe_Values.Look<Rot4>(ref shipDef.core.rot, "rot");
                        }
                        Scribe.ExitNode();
                    }
                    else
                    {
                        bool tempTrue = true;
                        Scribe_Values.Look<bool>(ref tempTrue, "neverAttacks", forceSave: true);
                        Scribe_Values.Look<bool>(ref tempTrue, "spaceSite", forceSave: true);
                    }
                    Scribe.EnterNode("symbolTable");
                    {
                        foreach (string key in shipDef.symbolTable.Keys)
                        {
                            Scribe.EnterNode("li");
                            {
                                char realKey = char.Parse(key);
                                Scribe_Values.Look<char>(ref realKey, "key"); ;
                                ShipShape realShape = shipDef.symbolTable[key];
                                Scribe_Deep.Look<ShipShape>(ref realShape, "value");
                                Scribe.ExitNode();
                            }
                        }
                        Scribe.ExitNode();
                    }
                    Scribe_Values.Look<string>(ref shipDef.bigString, "bigString");
                    Scribe.ExitNode();
                }
            });
            Log.Message("Resaved ship as: " + shipDef.fileName + ".xml");
        }

        public static void GenerateShip(EnemyShipDef shipDef, bool rotate = false)
        {
            Map map = GetOrGenerateMapUtility.GetOrGenerateMap(ShipInteriorMod2.FindWorldTile(), new IntVec3(250, 1, 250), DefDatabase<WorldObjectDef>.GetNamed("ShipEnemy"));
            map.GetComponent<ShipHeatMapComp>().CacheOff = true;
            map.GetComponent<ShipHeatMapComp>().IsGraveyard = true;
            ((WorldObjectOrbitingShip)map.Parent).radius = 150;
            ((WorldObjectOrbitingShip)map.Parent).theta = ((WorldObjectOrbitingShip)Find.CurrentMap.Parent).theta - Rand.RangeInclusive(1, 10) * 0.01f;

            IntVec3 c = map.Center;
            if (shipDef.saveSysVer == 2)
            {
                if (rotate)
                    c = new IntVec3(map.Size.x - shipDef.offsetZ, 0, shipDef.offsetX);
                else
                    c = new IntVec3(shipDef.offsetX, 0, shipDef.offsetZ);
            }
            shipDictionary.Add(map, shipDef.defName);

            Dictionary<IntVec3, Tuple<int, ColorInt, bool>> spawnLights = new Dictionary<IntVec3, Tuple<int, ColorInt, bool>>();

            foreach (ShipShape shape in shipDef.parts)
            {
                IntVec3 adjPos;
                if (rotate)
                    adjPos = new IntVec3(c.x - shape.z, 0, c.z + shape.z);
                else
                    adjPos = new IntVec3(c.x + shape.x, 0, c.z + shape.z);
                if (shape.shapeOrDef.Equals("PawnSpawnerGeneric"))
                {
                    ThingDef def = ThingDef.Named("PawnSpawnerGeneric");
                    Thing thing = ThingMaker.MakeThing(def);
                    GenSpawn.Spawn(thing, adjPos, map);
                    thing.TryGetComp<CompNameMe>().pawnKindDef = shape.stuff;
                }
                else if (shape.shapeOrDef.Equals("Cargo"))
                {
                    lastRegionPlaced = null;
                    ThingDef def = ThingDef.Named("ShipPartRegion");
                    Thing thing = ThingMaker.MakeThing(def);
                    if (rotate)
                    {
                        adjPos = new IntVec3(c.x - shape.z - shape.height + 1, 0, c.z + shape.x);
                        ((Building_ShipRegion)thing).width = shape.height;
                        ((Building_ShipRegion)thing).height = shape.width;
                    }
                    else
                    {
                        ((Building_ShipRegion)thing).width = shape.width;
                        ((Building_ShipRegion)thing).height = shape.height;
                    }
                    GenSpawn.Spawn(thing, adjPos, map);
                }
                else if (shape.shapeOrDef == "SoSLightEnabler")
                {
                    spawnLights.Add(adjPos, new Tuple<int, ColorInt, bool>(shape.rot.AsInt, ColorIntUtility.AsColorInt(shape.color != Color.clear ? shape.color : Color.white), shape.alt));
                }
                else if (DefDatabase<ThingDef>.GetNamedSilentFail(shape.shapeOrDef) != null)
                {
                    Thing thing;
                    ThingDef def = ThingDef.Named(shape.shapeOrDef);
                    if (map.listerThings.AllThings.Where(t => t.Position.x == shape.x && t.Position.z == shape.z) != def)
                    {
                        if (ImportToIgnore(def))
                            continue;
                        Rot4 rota = shape.rot;
                        if (rotate)
                        {
                            int adjz = shape.x;
                            int adjx = shape.z;
                            if (def.rotatable == true)
                                rota.Rotate(RotationDirection.Counterclockwise);
                            else if (def.rotatable == false && def.size.z != def.size.x) //skip non rot, non even
                                continue;
                            //pos
                            if (def.size.z % 2 == 0 && def.size.x % 2 == 0 && rota.AsByte == 0)
                                adjx += 1;
                            adjPos = new IntVec3(c.x - adjx, 0, c.z + adjz);
                        }
                        if (def.MadeFromStuff)
                        {
                            if (shape.stuff != null && !def.defName.StartsWith("Apparel_SpaceSuit"))
                                thing = ThingMaker.MakeThing(def, ThingDef.Named(shape.stuff));
                            else
                                thing = ThingMaker.MakeThing(def, GenStuff.DefaultStuffFor(def));
                        }
                        else
                            thing = ThingMaker.MakeThing(def);

                        if (thing.TryGetComp<CompColorable>() != null && shape.color != Color.clear)
                            thing.SetColor(shape.color);
                        if (thing.def.CanHaveFaction && thing.def != ResourceBank.ThingDefOf.ShipHullTile)
                            thing.SetFaction(Faction.OfPlayer);
                        if (thing.TryGetComp<CompPowerBattery>() != null)
                            thing.TryGetComp<CompPowerBattery>().AddEnergy(thing.TryGetComp<CompPowerBattery>().AmountCanAccept);
                        if (thing.TryGetComp<CompRefuelable>() != null)
                            thing.TryGetComp<CompRefuelable>().Refuel(thing.TryGetComp<CompRefuelable>().Props.fuelCapacity);
                        var shieldComp = thing.TryGetComp<CompShipCombatShield>();
                        if (shieldComp != null)
                        {
                            shieldComp.radiusSet = 40;
                            shieldComp.radius = 40;
                            if (shape.radius != 0)
                            {
                                shieldComp.radiusSet = shape.radius;
                                shieldComp.radius = shape.radius;
                            }
                        }
                        if (!rotate && !thing.def.rotatable && rota != Rot4.North)
                            rota = Rot4.North;
                        if (thing.def.stackLimit > 1)
                            thing.stackCount = (int)Math.Min(25, thing.def.stackLimit);
                        GenSpawn.Spawn(thing, adjPos, map, rota);
                    }
                }
                else if (DefDatabase<TerrainDef>.GetNamedSilentFail(shape.shapeOrDef) != null)
                {
                    IntVec3 pos;
                    if (rotate)
                        pos = new IntVec3(map.Size.x - shape.z, 0, shape.x);
                    else
                        pos = new IntVec3(shape.x, 0, shape.z);

                    if (shipDef.saveSysVer == 2)
                        pos = adjPos;
                    map.terrainGrid.SetTerrain(pos, DefDatabase<TerrainDef>.GetNamed(shape.shapeOrDef));
                }
            }
            if (!shipDef.core.shapeOrDef.NullOrEmpty())
            {
                Building core = (Building)ThingMaker.MakeThing(ThingDef.Named(shipDef.core.shapeOrDef));
                core.SetFaction(Faction.OfPlayer);
                Rot4 corerot;
                IntVec3 corepos;
                if (rotate)
                {
                    corepos = new IntVec3(c.x - shipDef.core.z, 0, c.z + shipDef.core.x);
                    corerot = shipDef.core.rot.Rotated(RotationDirection.Counterclockwise);
                }
                else
                {
                    corepos = new IntVec3(c.x + shipDef.core.x, 0, c.z + shipDef.core.z);
                    corerot = shipDef.core.rot;
                }
                GenSpawn.Spawn(core, corepos, map, corerot);
            }
            foreach (Building b in map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial))
            {
                CompPowerTrader trader = b.TryGetComp<CompPowerTrader>();
                if (trader != null)
                {
                    trader.PowerOn = true;
                }
                if (b is Building_ShipBridge bridge)
                    bridge.ShipName = shipDef.defName;
            }
            ShipInteriorMod2.SpawnLights(map, spawnLights);
            map.regionAndRoomUpdater.RebuildAllRegionsAndRooms();
            map.mapDrawer.RegenerateEverythingNow();
            map.temperatureCache.ResetTemperatureCache();
            map.GetComponent<ShipHeatMapComp>().RecacheMap();
            if (map.Biome == ResourceBank.BiomeDefOf.OuterSpaceBiome)
            {
                foreach (Room room in map.regionGrid.allRooms)
                    room.Temperature = 21f;
            }
            CameraJumper.TryJump(c, map);
        }
        public static void ExportShip(Map map, bool resave = false)
        {
            if (!map.IsSpace())
            {
                Messages.Message("Not on space map", MessageTypeDefOf.RejectInput);
                return;
            }
            Building_ShipBridge core = null;
            int combatPoints = 0;
            int randomTurretPoints = 0;
            int ShipMass = 0;
            int rarityLevel = 0;
            int minX = map.Size.x;
            int minZ = map.Size.z;
            int maxX = 0;
            int maxZ = 0;
            bool neverFleet = false;
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
                    else if (b.TryGetComp<CompEngineTrail>() != null && b.Rotation != Rot4.West)
                        neverFleet = true;
                }
                if (b is Building_ShipBridge bridge)
                    core = bridge;
            }
            if (neverFleet)
            {
                Messages.Message("Warning: ship not facing west! Can not be used in random fleets!", MessageTypeDefOf.RejectInput);
            }
            if (core == null)
            {
                Messages.Message("Warning: no ship core found! Tags set to neverAttacks, spaceSite!", MessageTypeDefOf.RejectInput);
            }
            else if (ShipUtility.ShipBuildingsAttachedTo(core).Count < Find.CurrentMap.spawnedThings.Where(b => b is Building).Count())
            {
                Messages.Message("Warning: found unattached buildings or multiple ships! Only use this file as spaceSite, startingShip or startingDungeon!", MessageTypeDefOf.RejectInput);
            }
            else if (core.ShipName == null)
            {
                Messages.Message("Warning: no ship name set! You can set it manually in the exported XML", MessageTypeDefOf.RejectInput);
            }

            string path = Path.Combine(GenFilePaths.SaveDataFolderPath, "ExportedShips");
            DirectoryInfo dir = new DirectoryInfo(path);
            if (!dir.Exists)
                dir.Create();
            string shipName = "siteTemp";
            if (core != null)
                shipName = core.ShipName;
            string filename = Path.Combine(path, shipName + ".xml");

            maxX -= minX;
            maxZ -= minZ;
            combatPoints += ShipMass / 100;

            char charPointer = '?';
            Dictionary<char, ShipShape> symbolTable = new Dictionary<char, ShipShape>();
            Dictionary<ShipShape, char> symbolTableBackwards = new Dictionary<ShipShape, char>();
            List<ShipPosRotShape> shipStructure = new List<ShipPosRotShape>();

            foreach (Thing t in Find.CurrentMap.spawnedThings) //save things
            {
                if (ExportToIgnore(t, core))
                {
                    continue;
                }
                ShipShape shape = new ShipShape();
                if (t is Building_ShipRegion r)
                {
                    shape.width = r.width;
                    shape.height = r.height;
                    shape.shapeOrDef = "Cargo";
                }
                else
                {
                    shape.shapeOrDef = t.def.defName;
                    if (t.def.MadeFromStuff && !t.def.defName.StartsWith("Apparel_SpaceSuit"))
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
                    var compCol = t.TryGetComp<CompColorable>();
                    if (compCol != null && compCol.Color != null && compCol.Color != Color.white && !t.def.defName.StartsWith("ShipSpinal") && !t.def.defName.StartsWith("Lighting_MURWall"))
                    {
                        shape.color = t.TryGetComp<CompColorable>().Color;
                    }
                }
                shape.x = t.Position.x - minX;
                shape.z = t.Position.z - minZ;
                shape.rot = t.Rotation;

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

            foreach (Thing t in Find.CurrentMap.spawnedThings.Where(b => b is Building)) //save lights
            {
                var partComp = t.TryGetComp<CompSoShipLight>();
                if (partComp != null && partComp.hasLight)
                {
                    ShipShape shape = new ShipShape();
                    shape.shapeOrDef = "SoSLightEnabler";
                    shape.x = t.Position.x - minX;
                    shape.z = t.Position.z - minZ;
                    shape.rot = new Rot4(partComp.lightRot);
                    shape.alt = partComp.sunLight;
                    if (partComp.lightColor != new ColorInt(Color.white))
                        shape.color = partComp.lightColor.ToColor;

                    if (partComp != null && partComp.hasLight)
                    {
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
            }

            foreach (IntVec3 cell in Find.CurrentMap.AllCells) //save terrain
            {
                TerrainDef def = Find.CurrentMap.terrainGrid.TerrainAt(cell);
                if (def.defName != "EmptySpace" && def != ResourceBank.TerrainDefOf.FakeFloorInsideShip && def != ResourceBank.TerrainDefOf.FakeFloorInsideShipMech && def != ResourceBank.TerrainDefOf.FakeFloorInsideShipArchotech)
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
            if (resave)
            {
                SafeSaver.Save(filename, "Defs", () =>
                {
                    Scribe.EnterNode("EnemyShipDef");
                    {
                        EnemyShipDef shipDef = DefDatabase<EnemyShipDef>.GetNamed(shipDictionary[map]);
                        Scribe_Values.Look<string>(ref shipDef.defName, "defName");
                        int saveSysVer = 2;
                        Scribe_Values.Look<int>(ref saveSysVer, "saveSysVer", 1);
                        Scribe_Values.Look<int>(ref minX, "offsetX", 0);
                        Scribe_Values.Look<int>(ref minZ, "offsetZ", 0);
                        Scribe_Values.Look<int>(ref maxX, "sizeX", 0);
                        Scribe_Values.Look<int>(ref maxZ, "sizeZ", 0);
                        Scribe_Values.Look<string>(ref shipDef.label, "label");

                        Scribe_Values.Look<int>(ref combatPoints, "combatPoints", 0);
                        Scribe_Values.Look<int>(ref randomTurretPoints, "randomTurretPoints", 0);
                        Scribe_Values.Look<int>(ref shipDef.cargoValue, "cargoValue", 0);
                        if (shipDef.rarityLevel > 1)
                            Scribe_Values.Look<int>(ref shipDef.rarityLevel, "rarityLevel", 1);

                        if (core != null)
                        {
                            Scribe_Values.Look<bool>(ref shipDef.neverRandom, "neverRandom");
                            Scribe_Values.Look<bool>(ref shipDef.neverAttacks, "neverAttacks");
                            Scribe_Values.Look<bool>(ref shipDef.neverWreck, "neverWreck");
                            Scribe_Values.Look<bool>(ref shipDef.startingShip, "startingShip");
                            Scribe_Values.Look<bool>(ref shipDef.startingDungeon, "startingDungeon");
                            Scribe_Values.Look<bool>(ref shipDef.spaceSite, "spaceSite");
                            Scribe_Values.Look<bool>(ref shipDef.tradeShip, "tradeShip");
                            Scribe_Values.Look<bool>(ref shipDef.navyExclusive, "navyExclusive");
                            Scribe_Values.Look<bool>(ref shipDef.customPaintjob, "customPaintjob");
                            Scribe_Values.Look<bool>(ref shipDef.neverFleet, "neverFleet");
                            Scribe.EnterNode("core");
                            {
                                Scribe_Values.Look<string>(ref core.def.defName, "shapeOrDef");
                                int cx = core.Position.x - minX;
                                Scribe_Values.Look<int>(ref cx, "x");
                                int cz = core.Position.z - minZ;
                                Scribe_Values.Look<int>(ref cz, "z");
                                Rot4 crot = core.Rotation;
                                Scribe_Values.Look<Rot4>(ref crot, "rot");
                            }
                            Scribe.ExitNode();
                        }
                        else
                        {
                            bool tempTrue = true;
                            Scribe_Values.Look<bool>(ref tempTrue, "neverAttacks", forceSave: true);
                            Scribe_Values.Look<bool>(ref tempTrue, "spaceSite", forceSave: true);
                        }
                        Scribe.EnterNode("symbolTable");
                        {
                            foreach (char key in symbolTable.Keys)
                            {
                                Scribe.EnterNode("li");
                                {
                                    char realKey = key;
                                    Scribe_Values.Look<char>(ref realKey, "key"); ;
                                    ShipShape realShape = symbolTable[key];
                                    Scribe_Deep.Look<ShipShape>(ref realShape, "value");
                                    Scribe.ExitNode();
                                }
                            }
                            Scribe.ExitNode();
                        }
                        Scribe_Values.Look<string>(ref bigString, "bigString");
                        Scribe.ExitNode();
                    }
                });
                Messages.Message("Resaved ship as: " + shipName + ".xml", core, MessageTypeDefOf.PositiveEvent);
            }
            else
            {
                SafeSaver.Save(filename, "Defs", () =>
                {
                    Scribe.EnterNode("EnemyShipDef");
                    {
                        Scribe_Values.Look<string>(ref shipName, "defName");
                        int saveSysVer = 2;
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
                        Scribe_Values.Look<int>(ref rarityLevel, "rarityLevel", 1);
                        bool temp = false;
                        if (core != null)
                        {
                            Scribe_Values.Look<bool>(ref temp, "neverRandom", forceSave: true);
                            Scribe_Values.Look<bool>(ref temp, "neverAttacks", forceSave: true);
                            Scribe_Values.Look<bool>(ref temp, "neverWreck", forceSave: true);
                            Scribe_Values.Look<bool>(ref neverFleet, "neverFleet", forceSave: true);
                            Scribe_Values.Look<bool>(ref temp, "startingShip", forceSave: true);
                            Scribe_Values.Look<bool>(ref temp, "startingDungeon", forceSave: true);
                            Scribe_Values.Look<bool>(ref temp, "spaceSite", forceSave: true);
                            Scribe_Values.Look<bool>(ref temp, "tradeShip", forceSave: true);
                            Scribe_Values.Look<bool>(ref temp, "navyExclusive", forceSave: true);
                            Scribe_Values.Look<bool>(ref temp, "customPaintjob", forceSave: true);
                            Scribe_Values.Look<bool>(ref temp, "neverFleet", forceSave: true);
                            Scribe.EnterNode("core");
                            {
                                Scribe_Values.Look<string>(ref core.def.defName, "shapeOrDef");
                                int cx = core.Position.x - minX;
                                Scribe_Values.Look<int>(ref cx, "x");
                                int cz = core.Position.z - minZ;
                                Scribe_Values.Look<int>(ref cz, "z");
                                Rot4 crot = core.Rotation;
                                Scribe_Values.Look<Rot4>(ref crot, "rot");
                            }
                            Scribe.ExitNode();
                        }
                        else
                        {
                            bool tempTrue = true;
                            Scribe_Values.Look<bool>(ref tempTrue, "neverAttacks", forceSave: true);
                            Scribe_Values.Look<bool>(ref tempTrue, "spaceSite", forceSave: true);
                        }
                        Scribe.EnterNode("symbolTable");
                        {
                            foreach (char key in symbolTable.Keys)
                            {
                                Scribe.EnterNode("li");
                                {
                                    char realKey = key;
                                    Scribe_Values.Look<char>(ref realKey, "key"); ;
                                    ShipShape realShape = symbolTable[key];
                                    Scribe_Deep.Look<ShipShape>(ref realShape, "value");
                                    Scribe.ExitNode();
                                }
                            }
                            Scribe.ExitNode();
                        }
                        Scribe_Values.Look<string>(ref bigString, "bigString");
                        Scribe.ExitNode();
                    }
                });
                Messages.Message("Saved ship as: " + shipName + ".xml", core, MessageTypeDefOf.PositiveEvent);
            }
        }
    }
    public struct ShipPosRotShape
    {
        public int x;
        public int z;
        public Rot4 rot;
        public string shape;

        public override int GetHashCode()
        {
            return (x + "," + z + "," + rot).GetHashCode();
        }
    }

    [HarmonyPatch(typeof(LetterStack), "LettersOnGUI")]
    public static class ShipCKWarning
    {
        [HarmonyPrefix]
        public static bool DrawWarning(ref float baseY)
        {
            float num = (float)UI.screenWidth - 200f;
            Rect rect = new Rect(num, baseY - 16f, 193f, 26f);
            Text.Anchor = TextAnchor.MiddleRight;
            string text = "WARNING: SOS2CK IS ACTIVE";
            float x = Text.CalcSize(text).x;
            Rect rect2 = new Rect(rect.xMax - x, rect.y, x, rect.height);
            if (Mouse.IsOver(rect2))
            {
                Widgets.DrawHighlight(rect2);
            }
            TooltipHandler.TipRegionByKey(rect2, "SOS2 Creation Kit IS ACTIVE!\nDO NOT USE IT IN NORMAL PLAY");
            Widgets.Label(rect2, text);
            Text.Anchor = TextAnchor.UpperLeft;
            baseY -= 26f;
            return true;
        }
    }
}

