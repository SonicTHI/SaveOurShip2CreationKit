using RimworldMod;
using SaveOurShip2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace RimWorld
{

    class Designator_ImportShip : Designator
    {
        public static List<EnemyShipDef> shipDefsAll = new List<EnemyShipDef>();

        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            return true;
        }
        public Designator_ImportShip()
        {
            defaultLabel = "Import Ship";
            defaultDesc = "Click anywhere on the map to activate.";
            icon = ContentFinder<Texture2D>.Get("UI/Load_XML");
            soundDragSustain = SoundDefOf.Designate_DragStandard;
            soundDragChanged = SoundDefOf.Designate_DragStandard_Changed;
            useMouseIcon = true;
            soundSucceeded = SoundDefOf.Designate_Deconstruct;
        }
        public override void DesignateSingleCell(IntVec3 loc)
        {
            Find.WindowStack.Add(new Dialog_LoadShip("shipdeftoload"));
        }
    }
    public class Dialog_LoadShip : Dialog_Rename
    {
        private string ship= "shipdeftoload";
        //public static Map ImportedShip;
        public Dialog_LoadShip(string ship)
        {
            curName = ship;
        }

        protected override void SetName(string name)
        {
            if (name == ship || string.IsNullOrEmpty(name))
                return;
            EnemyShipDef shipDef = DefDatabase<EnemyShipDef>.GetNamed(name);
            GenerateShip(shipDef);
        }

        public static void GenerateShip(EnemyShipDef shipDef)
        {
            Map ImportedShip = GetOrGenerateMapUtility.GetOrGenerateMap(ShipInteriorMod2.FindWorldTile(), new IntVec3(250, 1, 250), DefDatabase<WorldObjectDef>.GetNamed("ShipEnemy"));
            ImportedShip.GetComponent<ShipHeatMapComp>().IsGraveyard = true;
            ((WorldObjectOrbitingShip)ImportedShip.Parent).radius = 150;
            ((WorldObjectOrbitingShip)ImportedShip.Parent).theta = ((WorldObjectOrbitingShip)Find.CurrentMap.Parent).theta - Rand.RangeInclusive(1, 10) * 0.01f;
            IntVec3 c = ImportedShip.Center;
            SoSBuilder.shipDictionary.Add(ImportedShip, shipDef.defName);

            ThingDef hullPlateDef = ThingDef.Named("ShipHullTile");
            //List<ShipShape> partsToGenerate = new List<ShipShape>();
            foreach (ShipShape shape in shipDef.parts)
            {
                if (shape.shapeOrDef.Equals("Circle"))
                {
                    List<IntVec3> border = new List<IntVec3>();
                    List<IntVec3> interior = new List<IntVec3>();
                    ShipInteriorMod2.CircleUtility(c.x + shape.x, c.z + shape.z, shape.width, ref border, ref interior);
                    GenerateHull(border, interior, Faction.OfPlayer, ImportedShip);
                    //cellsToFog.AddRange(interior);
                }
                else if (shape.shapeOrDef.Equals("Rect"))
                {
                    List<IntVec3> border = new List<IntVec3>();
                    List<IntVec3> interior = new List<IntVec3>();
                    ShipInteriorMod2.RectangleUtility(c.x + shape.x, c.z + shape.z, shape.width, shape.height, ref border, ref interior);
                    GenerateHull(border, interior, Faction.OfPlayer, ImportedShip);
                    //cellsToFog.AddRange(interior);
                }
                else if (shape.shapeOrDef.Equals("PawnSpawnerGeneric"))
                {
                    ThingDef def = ThingDef.Named("PawnSpawnerGeneric");
                    Thing thing = ThingMaker.MakeThing(def);
                    GenSpawn.Spawn(thing, new IntVec3(c.x + shape.x, 0, c.z + shape.z), ImportedShip);
                    thing.TryGetComp<CompNameMe>().pawnKindDef = shape.stuff;
                }/*
                else if (DefDatabase<ImportedShipPartDef>.GetNamedSilentFail(shape.shapeOrDef) != null)
                {
                    partsToGenerate.Add(shape);
                }
                else if (DefDatabase<PawnKindDef>.GetNamedSilentFail(shape.shapeOrDef) != null)
                {
                    PawnGenerationRequest req = new PawnGenerationRequest(DefDatabase<PawnKindDef>.GetNamed(shape.shapeOrDef), Faction.OfAncientsHostile);
                    Pawn pawn = PawnGenerator.GeneratePawn(req);
                    if(defendShip!=null)
                        defendShip.AddPawn(pawn);
                    GenSpawn.Spawn(pawn, new IntVec3(c.x + shape.x, 0, c.z + shape.z), ImportedShip);
                }*/
                else if (shape.shapeOrDef.Equals("Cargo"))
                {
                    SoSBuilder.lastRegionPlaced = null;
                    ThingDef def = ThingDef.Named("ShipPartRegion");
                    Thing thing = ThingMaker.MakeThing(def);
                    GenSpawn.Spawn(thing, new IntVec3(c.x + shape.x, 0, c.z + shape.z), ImportedShip);
                    ((Building_ShipRegion)thing).width = shape.width;
                    ((Building_ShipRegion)thing).height = shape.height;
                }
                if (DefDatabase<ThingDef>.GetNamedSilentFail(shape.shapeOrDef) != null)
                {
                    Thing thing;
                    ThingDef def = ThingDef.Named(shape.shapeOrDef);
                    if (ImportedShip.listerThings.AllThings.Where(t => t.Position.x == shape.x && t.Position.z == shape.z) != def)
                    {
                        if (def.MadeFromStuff)
                        {
                            if (shape.stuff != null)
                                thing = ThingMaker.MakeThing(def, ThingDef.Named(shape.stuff));
                            else
                                thing = ThingMaker.MakeThing(def, GenStuff.DefaultStuffFor(def));
                        }
                        else
                            thing = ThingMaker.MakeThing(def);

                        if (thing.TryGetComp<CompColorable>() != null && shape.color != Color.clear)
                            thing.SetColor(shape.color);
                        if (thing.def.CanHaveFaction && thing.def != hullPlateDef)
                            thing.SetFaction(Faction.OfPlayer);
                        if (thing.TryGetComp<CompPowerBattery>() != null)
                            thing.TryGetComp<CompPowerBattery>().AddEnergy(thing.TryGetComp<CompPowerBattery>().AmountCanAccept);
                        if (thing.TryGetComp<CompRefuelable>() != null)
                            thing.TryGetComp<CompRefuelable>().Refuel(thing.TryGetComp<CompRefuelable>().Props.fuelCapacity);
                        if (thing.TryGetComp<CompShipCombatShield>() != null)
                        {
                            thing.TryGetComp<CompShipCombatShield>().radiusSet = 40;
                            thing.TryGetComp<CompShipCombatShield>().radius = 40;
                            thing.TryGetComp<CompShipCombatShield>().radiusSet = shape.radius;
                            thing.TryGetComp<CompShipCombatShield>().radius = shape.radius;
                        }
                        if (thing.def.stackLimit > 1)
                            thing.stackCount = (int)Math.Min(25, thing.def.stackLimit);
                        GenSpawn.Spawn(thing, new IntVec3(c.x + shape.x, 0, c.z + shape.z), ImportedShip, shape.rot);
                        //if (shape.shapeOrDef.Equals("ShipAirlock") || shape.shapeOrDef.Equals("ShipHullTile") || shape.shapeOrDef.Equals("ShipHullTileMech"))
                        //cellsToFog.Add(thing.Position);
                    }
                }
                else if (DefDatabase<TerrainDef>.GetNamedSilentFail(shape.shapeOrDef) != null)
                {
                    ImportedShip.terrainGrid.SetTerrain(new IntVec3(shape.x, 0, shape.z), DefDatabase<TerrainDef>.GetNamed(shape.shapeOrDef));
                }
            }
            Building core = (Building)ThingMaker.MakeThing(ThingDef.Named(shipDef.core.shapeOrDef)); 
            core.SetFaction(Faction.OfPlayer);
            Rot4 corerot = shipDef.core.rot;
            GenSpawn.Spawn(core, new IntVec3(c.x + shipDef.core.x, 0, c.z + shipDef.core.z), ImportedShip, corerot);
            foreach (Building b in ImportedShip.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial))
            {
                //Building b = t as Building;
                //if (b == null)
                    //continue; 
                if (b.TryGetComp<CompPowerTrader>() != null)
                {
                    CompPowerTrader trader = b.TryGetComp<CompPowerTrader>();
                    trader.PowerOn = true;
                }
                if (b is Building_ShipBridge)
                    ((Building_ShipBridge)b).ShipName = shipDef.defName;
            }
            ImportedShip.mapDrawer.RegenerateEverythingNow();
            ImportedShip.regionAndRoomUpdater.RebuildAllRegionsAndRooms();
            ImportedShip.temperatureCache.ResetTemperatureCache();
            if (ImportedShip.Biome == ShipInteriorMod2.OuterSpaceBiome)
            {
                foreach (Room room in ImportedShip.regionGrid.allRooms)
                    room.Temperature = 21f;
            }
            CameraJumper.TryJump(c, ImportedShip);
        }
		
		private static void GenerateHull(List<IntVec3> border, List<IntVec3> interior, Faction fac, Map importedShip)
        {
            foreach (IntVec3 vec in border)
            {
                if (!GenSpawn.WouldWipeAnythingWith(vec, Rot4.South, ThingDef.Named("Ship_Beam"), importedShip, (Thing x) => x.def.category == ThingCategory.Building) && !vec.GetThingList(importedShip).Where(t => t.def == ThingDef.Named("ShipHullTile") || t.def == ThingDef.Named("ShipHullTileMech")).Any())
                {
                    Thing wall = ThingMaker.MakeThing(ThingDef.Named("Ship_Beam"));
                    wall.SetFaction(fac);
                    GenSpawn.Spawn(wall, vec, importedShip);
                }
            }
            foreach (IntVec3 vec in interior)
            {
                Thing floor = ThingMaker.MakeThing(ThingDef.Named("ShipHullTile"));
                GenSpawn.Spawn(floor, vec, importedShip);
            }
        }
    }
}
