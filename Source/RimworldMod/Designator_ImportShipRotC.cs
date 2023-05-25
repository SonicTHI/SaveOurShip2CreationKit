using RimWorld.Planet;
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

    class Designator_ImportShipRotC : Designator
    {
        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            if (Find.CurrentMap.IsSpace())
                return true;
            Messages.Message("Ship editor works only on space maps!", MessageTypeDefOf.RejectInput);
            return false;
        }
        public Designator_ImportShipRotC()
        {
            defaultLabel = "Import Ship Rotated 90° CCW";
            defaultDesc = "Click anywhere on the map to activate.\nWARNING: Non rotatable, non even sided buildings will be discarded!";
            icon = ContentFinder<Texture2D>.Get("UI/Load_XML");
            soundDragSustain = SoundDefOf.Designate_DragStandard;
            soundDragChanged = SoundDefOf.Designate_DragStandard_Changed;
            useMouseIcon = true;
            soundSucceeded = SoundDefOf.Designate_Deconstruct;
        }
        public override void DesignateSingleCell(IntVec3 loc)
        {
            Find.WindowStack.Add(new Dialog_LoadShipRotC("shipdeftoloadrotl"));
        }
    }
    public class Dialog_LoadShipRotC : Dialog_Rename
    {
        private string ship= "shipdeftoloadrotl";
        //public static Map ImportedShip;
        public Dialog_LoadShipRotC(string ship)
        {
            curName = ship;
        }

        protected override void SetName(string name)
        {
            if (name == ship || string.IsNullOrEmpty(name))
                return;
            EnemyShipDef shipDef = DefDatabase<EnemyShipDef>.GetNamed(name);
            if (shipDef == null)
                return;
            GenerateShip(shipDef);
        }

        public static void GenerateShip(EnemyShipDef shipDef)
        {
            Map map = GetOrGenerateMapUtility.GetOrGenerateMap(ShipInteriorMod2.FindWorldTile(), new IntVec3(250, 1, 250), DefDatabase<WorldObjectDef>.GetNamed("ShipEnemy"));
            map.GetComponent<ShipHeatMapComp>().IsGraveyard = true;
            map.GetComponent<ShipHeatMapComp>().ShipCombatOriginMap = ((MapParent)Find.WorldObjects.AllWorldObjects.Where(ob => ob.def.defName.Equals("ShipOrbiting")).FirstOrDefault()).Map;
            ((WorldObjectOrbitingShip)map.Parent).radius = 150;
            ((WorldObjectOrbitingShip)map.Parent).theta = ((WorldObjectOrbitingShip)Find.CurrentMap.Parent).theta - Rand.RangeInclusive(1,10)* 0.01f;
            IntVec3 c = map.Center;
            if (shipDef.saveSysVer == 2)
                c = new IntVec3(map.Size.x - shipDef.offsetZ, 0, shipDef.offsetX);
            SoSBuilder.shipDictionary.Add(map, shipDef.defName);

            Dictionary<IntVec3, Tuple<int, ColorInt, bool>> spawnLights = new Dictionary<IntVec3, Tuple<int, ColorInt, bool>>();

            foreach (ShipShape shape in shipDef.parts)
            {
                if (shape.shapeOrDef.Equals("PawnSpawnerGeneric"))
                {
                    ThingDef def = ThingDef.Named("PawnSpawnerGeneric");
                    Thing thing = ThingMaker.MakeThing(def);
                    GenSpawn.Spawn(thing, new IntVec3(c.x - shape.z, 0, c.z + shape.z), map);
                    thing.TryGetComp<CompNameMe>().pawnKindDef = shape.stuff;
                }
                else if (shape.shapeOrDef.Equals("Cargo"))
                {
                    SoSBuilder.lastRegionPlaced = null;
                    ThingDef def = ThingDef.Named("ShipPartRegion");
                    Thing thing = ThingMaker.MakeThing(def);
                    GenSpawn.Spawn(thing, new IntVec3(c.x - shape.z - shape.height + 1, 0, c.z + shape.x), map);
                    ((Building_ShipRegion)thing).width = shape.height;
                    ((Building_ShipRegion)thing).height = shape.width;
                }
                else if (shape.shapeOrDef == "SoSLightEnabler")
                {
                    spawnLights.Add(new IntVec3(c.x - shape.z, 0, c.z + shape.z), new Tuple<int, ColorInt, bool>(shape.rot.AsInt, ColorIntUtility.AsColorInt(shape.color != Color.clear ? shape.color : Color.white), shape.alt));
                }
                else if (DefDatabase<ThingDef>.GetNamedSilentFail(shape.shapeOrDef) != null)
                {
                    Thing thing;
                    ThingDef def = ThingDef.Named(shape.shapeOrDef);
                    if (map.listerThings.AllThings.Where(t => t.Position.x == shape.x && t.Position.z == shape.z) != def)
                    {
                        if (SoSBuilder.ImportToIgnore(def))
                            continue;
                        Rot4 rota = shape.rot;
                        int adjz = shape.x;
                        int adjx = shape.z;
                        if (def.rotatable == true)
                            rota.Rotate(RotationDirection.Counterclockwise);
                        else if (def.rotatable == false && def.size.z != def.size.x) //skip non rot, non even
                            continue;
                        //pos
                        if (def.size.z % 2 == 0 && def.size.x % 2 == 0 && rota.AsByte == 0)
                            adjx += 1;

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
                        GenSpawn.Spawn(thing, new IntVec3(c.x - adjx, 0, c.z + adjz), map, rota);
                    }
                }
                else if (DefDatabase<TerrainDef>.GetNamedSilentFail(shape.shapeOrDef) != null)
                {
                    IntVec3 pos = new IntVec3(map.Size.x - shape.z, 0, shape.x);
                    if (shipDef.saveSysVer == 2)
                        pos = new IntVec3(c.x - shape.z, 0, c.z + shape.x);
                    map.terrainGrid.SetTerrain(pos, DefDatabase<TerrainDef>.GetNamed(shape.shapeOrDef));
                }
            }
            if (!shipDef.core.shapeOrDef.NullOrEmpty())
            {
                Building core = (Building)ThingMaker.MakeThing(ThingDef.Named(shipDef.core.shapeOrDef));
                core.SetFaction(Faction.OfPlayer);
                Rot4 corerot = shipDef.core.rot.Rotated(RotationDirection.Counterclockwise);
                GenSpawn.Spawn(core, new IntVec3(c.x - shipDef.core.z, 0, c.z + shipDef.core.x), map, corerot);
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
            if (map.Biome == ResourceBank.BiomeDefOf.OuterSpaceBiome)
            {
                foreach (Room room in map.regionGrid.allRooms)
                    room.Temperature = 21f;
            }
            CameraJumper.TryJump(c, map);
        }
    }
}
