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

    class Designator_ImportShipRot : Designator
    {
        public static List<EnemyShipDef> shipDefsAll = new List<EnemyShipDef>();

        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            return true;
        }
        public Designator_ImportShipRot()
        {
            defaultLabel = "Import Ship Rotated 90° Right";
            defaultDesc = "Click anywhere on the map to activate.\nWARNING: Non rotatable buildings will not be placed correctly!";
            icon = ContentFinder<Texture2D>.Get("UI/Load_XML");
            soundDragSustain = SoundDefOf.Designate_DragStandard;
            soundDragChanged = SoundDefOf.Designate_DragStandard_Changed;
            useMouseIcon = true;
            soundSucceeded = SoundDefOf.Designate_Deconstruct;
        }
        public override void DesignateSingleCell(IntVec3 loc)
        {
            Find.WindowStack.Add(new Dialog_LoadShipRot("shipdeftoloadrot"));
        }
    }
    public class Dialog_LoadShipRot : Dialog_Rename
    {
        private string ship= "shipdeftoloadrot";
        //public static Map ImportedShip;
        public Dialog_LoadShipRot(string ship)
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
            SoSBuilder.shipDictionary.Add(ImportedShip, shipDef.defName);

            IntVec3 c = ImportedShip.Center;
            ThingDef hullPlateDef = ThingDef.Named("ShipHullTile");
            foreach (ShipShape shape in shipDef.parts)
            {
                if (shape.shapeOrDef.Equals("Cargo"))
                {
                    SoSBuilder.lastRegionPlaced = null;
                    ThingDef def = ThingDef.Named("ShipPartRegion");
                    Thing thing = ThingMaker.MakeThing(def);
                    GenSpawn.Spawn(thing, new IntVec3(c.x + shape.z, 0, c.z + shape.x), ImportedShip);
                    ((Building_ShipRegion)thing).width = shape.height;
                    ((Building_ShipRegion)thing).height = shape.width;
                }
                else if (shape.shapeOrDef.Equals("PawnSpawnerGeneric"))
                {
                    ThingDef def = ThingDef.Named("PawnSpawnerGeneric");
                    Thing thing = ThingMaker.MakeThing(def);
                    GenSpawn.Spawn(thing, new IntVec3(c.x + shape.z, 0, c.z + shape.x), ImportedShip);
                    thing.TryGetComp<CompNameMe>().pawnKindDef = shape.stuff;
                }
                if (DefDatabase<ThingDef>.GetNamedSilentFail(shape.shapeOrDef) != null)
                {
                    Thing thing;
                    ThingDef def = ThingDef.Named(shape.shapeOrDef);
                    if (ImportedShip.listerThings.AllThings.Where(t => t.Position.x == shape.x && t.Position.z == shape.z) != def)
                    {
                        Rot4 rota = shape.rot;
                        int adjz = shape.x;
                        int adjx = shape.z;
                        if (def.rotatable==true)
                        {
                            //rot
                            if (!rota.IsHorizontal && shape.shapeOrDef.Contains("Ship_Corner_OneT"))
                            {
                                rota.Rotate(RotationDirection.Counterclockwise);
                            }
                            else if (shape.shapeOrDef.Equals("Ship_Corner_OneOne"))
                            {
                                if (!rota.IsHorizontal)
                                {
                                    rota.Rotate(RotationDirection.Clockwise);
                                    rota.Rotate(RotationDirection.Clockwise);
                                }
                            }
                            else
                                rota.Rotate(RotationDirection.Clockwise);

                            //pos
                            if (def.size.z % 2 == 0 && def.size.x % 2 == 0)
                                adjz += 1;
                            else if (def.size.z == 2)
                            {
                                if (rota.AsByte == 0)
                                    adjz -= 1;
                                else if (rota.AsByte == 1) 
                                {
                                    if (shape.shapeOrDef.Contains("Ship_Corner_OneTwo"))
                                        adjx -= 1;
                                }
                                else if (rota.AsByte == 3)
                                {
                                    if (shape.shapeOrDef.Contains("Ship_Corner_OneTwo"))
                                        adjx += 1;
                                }
                                else if (rota.AsByte == 2)
                                    adjz += 1;
                            }
                        }

                        if (def.MadeFromStuff)
                        {
                            if (shape.stuff != null)
                                thing = ThingMaker.MakeThing(def, ThingDef.Named(shape.stuff));
                            else
                                thing = ThingMaker.MakeThing(def, GenStuff.DefaultStuffFor(def));
                        }
                        else if (shape.shapeOrDef.Equals("Ship_Corner_OneTwo"))
                            thing = ThingMaker.MakeThing(ThingDef.Named("Ship_Corner_OneTwoFlip"));
                        else if (shape.shapeOrDef.Equals("Ship_Corner_OneTwoFlip"))
                            thing = ThingMaker.MakeThing(ThingDef.Named("Ship_Corner_OneTwo"));
                        else if (shape.shapeOrDef.Equals("Ship_Corner_OneThree"))
                            thing = ThingMaker.MakeThing(ThingDef.Named("Ship_Corner_OneThreeFlip"));
                        else if (shape.shapeOrDef.Equals("Ship_Corner_OneThreeFlip"))
                            thing = ThingMaker.MakeThing(ThingDef.Named("Ship_Corner_OneThree"));
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
                        GenSpawn.Spawn(thing, new IntVec3(c.x + adjx, 0, c.z + adjz), ImportedShip, rota);
                        //if (shape.shapeOrDef.Equals("ShipAirlock") || shape.shapeOrDef.Equals("ShipHullTile") || shape.shapeOrDef.Equals("ShipHullTileMech"))
                        //cellsToFog.Add(thing.Position);
                    }
                }
                else if (DefDatabase<TerrainDef>.GetNamedSilentFail(shape.shapeOrDef) != null)
                {
                    ImportedShip.terrainGrid.SetTerrain(new IntVec3(shape.z, 0, shape.x), DefDatabase<TerrainDef>.GetNamed(shape.shapeOrDef));
                }
            }
            Building core = (Building)ThingMaker.MakeThing(ThingDef.Named(shipDef.core.shapeOrDef)); 
            core.SetFaction(Faction.OfPlayer);
            Rot4 corerot= shipDef.core.rot.Rotated(RotationDirection.Clockwise);
            GenSpawn.Spawn(core, new IntVec3(c.x + shipDef.core.z, 0, c.z + shipDef.core.x), ImportedShip, corerot);
            ((Building_ShipBridge)core).ShipName = shipDef.defName;
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
    }
}
