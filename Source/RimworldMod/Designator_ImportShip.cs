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
using Verse.Noise;

namespace RimWorld
{

    class Designator_ImportShip : Designator
    {
        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            if (Find.CurrentMap.IsSpace())
                return true;
            Messages.Message("Ship editor works only on space maps!", MessageTypeDefOf.RejectInput);
            return false;
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
            ((WorldObjectOrbitingShip)map.Parent).theta = ((WorldObjectOrbitingShip)Find.CurrentMap.Parent).theta - Rand.RangeInclusive(1, 10) * 0.01f;
            IntVec3 c = map.Center;
            if (shipDef.saveSysVer == 2)
                c = new IntVec3(shipDef.offsetX, 0, shipDef.offsetZ);
            SoSBuilder.shipDictionary.Add(map, shipDef.defName);

            Dictionary<IntVec3, Color> spawnLights = new Dictionary<IntVec3, Color>();
            Dictionary<IntVec3, Color> spawnSunLights = new Dictionary<IntVec3, Color>();

            foreach (ShipShape shape in shipDef.parts)
            {
                IntVec3 adjPos = new IntVec3(c.x + shape.x, 0, c.z + shape.z);
                if (shape.shapeOrDef.Equals("PawnSpawnerGeneric"))
                {
                    ThingDef def = ThingDef.Named("PawnSpawnerGeneric");
                    Thing thing = ThingMaker.MakeThing(def);
                    GenSpawn.Spawn(thing, adjPos, map);
                    thing.TryGetComp<CompNameMe>().pawnKindDef = shape.stuff;
                }
                else if (shape.shapeOrDef.Equals("Cargo"))
                {
                    SoSBuilder.lastRegionPlaced = null;
                    ThingDef def = ThingDef.Named("ShipPartRegion");
                    Thing thing = ThingMaker.MakeThing(def);
                    GenSpawn.Spawn(thing, new IntVec3(c.x + shape.x, 0, c.z + shape.z), map);
                    ((Building_ShipRegion)thing).width = shape.width;
                    ((Building_ShipRegion)thing).height = shape.height;
                }
                else if (shape.shapeOrDef == "SoSLightEnabler")
                {
                    spawnLights.Add(adjPos, shape.color != Color.clear ? shape.color : Color.white);
                }
                else if (shape.shapeOrDef == "SoSSunLightEnabler")
                {
                    spawnSunLights.Add(adjPos, shape.color != Color.clear ? shape.color : Color.white);
                }
                else if (DefDatabase<ThingDef>.GetNamedSilentFail(shape.shapeOrDef) != null)
                {
                    Thing thing;
                    ThingDef def = ThingDef.Named(shape.shapeOrDef);
                    if (map.listerThings.AllThings.Where(t => t.Position.x == shape.x && t.Position.z == shape.z) != def)
                    {
                        if (SoSBuilder.ImportToIgnore(def))
                            continue;
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
                        if ((thing.TryGetComp<CompSoShipPart>()?.Props.isPlating ?? false) && adjPos.GetThingList(map).Any(t => t.TryGetComp<CompSoShipPart>()?.Props.isPlating ?? false)) { } //clean multiple hull spawns
                        else
                            GenSpawn.Spawn(thing, adjPos, map, shape.rot);
                    }
                }
                else if (DefDatabase<TerrainDef>.GetNamedSilentFail(shape.shapeOrDef) != null)
                {
                    IntVec3 pos = new IntVec3(shape.x, 0, shape.z);
                    if (shipDef.saveSysVer == 2)
                        pos = adjPos;
                    map.terrainGrid.SetTerrain(pos, DefDatabase<TerrainDef>.GetNamed(shape.shapeOrDef));
                }
            }
            if (!shipDef.core.shapeOrDef.NullOrEmpty())
            {
                Building core = (Building)ThingMaker.MakeThing(ThingDef.Named(shipDef.core.shapeOrDef));
                core.SetFaction(Faction.OfPlayer);
                Rot4 corerot = shipDef.core.rot;
                GenSpawn.Spawn(core, new IntVec3(c.x + shipDef.core.x, 0, c.z + shipDef.core.z), map, corerot);
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
            ShipInteriorMod2.SpawnLights(map, spawnLights, false);
            ShipInteriorMod2.SpawnLights(map, spawnSunLights, true);
            map.mapDrawer.RegenerateEverythingNow();
            map.regionAndRoomUpdater.RebuildAllRegionsAndRooms();
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
