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

    class Designator_ImportShip : Designator
    {
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
            if (shipDef == null)
                return;
            GenerateShip(shipDef);
        }

        public static void GenerateShip(EnemyShipDef shipDef)
        {
            Map ImportedShip = GetOrGenerateMapUtility.GetOrGenerateMap(ShipInteriorMod2.FindWorldTile(), new IntVec3(250, 1, 250), DefDatabase<WorldObjectDef>.GetNamed("ShipEnemy"));
            ImportedShip.GetComponent<ShipHeatMapComp>().IsGraveyard = true;
            ImportedShip.GetComponent<ShipHeatMapComp>().ShipCombatOriginMap = ((MapParent)Find.WorldObjects.AllWorldObjects.Where(ob => ob.def.defName.Equals("ShipOrbiting")).FirstOrDefault()).Map;
            ((WorldObjectOrbitingShip)ImportedShip.Parent).radius = 150;
            ((WorldObjectOrbitingShip)ImportedShip.Parent).theta = ((WorldObjectOrbitingShip)Find.CurrentMap.Parent).theta - Rand.RangeInclusive(1, 10) * 0.01f;
            IntVec3 c = ImportedShip.Center;
            if (shipDef.saveSysVer == 2)
                c = new IntVec3(shipDef.offsetX, 0, shipDef.offsetZ);
            SoSBuilder.shipDictionary.Add(ImportedShip, shipDef.defName);

            foreach (ShipShape shape in shipDef.parts)
            {
                if (shape.shapeOrDef.Equals("PawnSpawnerGeneric"))
                {
                    ThingDef def = ThingDef.Named("PawnSpawnerGeneric");
                    Thing thing = ThingMaker.MakeThing(def);
                    GenSpawn.Spawn(thing, new IntVec3(c.x + shape.x, 0, c.z + shape.z), ImportedShip);
                    thing.TryGetComp<CompNameMe>().pawnKindDef = shape.stuff;
                }
                else if (shape.shapeOrDef.Equals("Cargo"))
                {
                    SoSBuilder.lastRegionPlaced = null;
                    ThingDef def = ThingDef.Named("ShipPartRegion");
                    Thing thing = ThingMaker.MakeThing(def);
                    GenSpawn.Spawn(thing, new IntVec3(c.x + shape.x, 0, c.z + shape.z), ImportedShip);
                    ((Building_ShipRegion)thing).width = shape.width;
                    ((Building_ShipRegion)thing).height = shape.height;
                }
                else if (DefDatabase<ThingDef>.GetNamedSilentFail(shape.shapeOrDef) != null)
                {
                    Thing thing;
                    ThingDef def = ThingDef.Named(shape.shapeOrDef);
                    if (ImportedShip.listerThings.AllThings.Where(t => t.Position.x == shape.x && t.Position.z == shape.z) != def)
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
                        if (thing.def.CanHaveFaction && thing.def != ShipInteriorMod2.hullPlateDef)
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
                        if ((thing.TryGetComp<CompSoShipPart>()?.Props.isPlating ?? false) && new IntVec3(c.x + shape.x, 0, c.z + shape.z).GetThingList(ImportedShip).Any(t => t.TryGetComp<CompSoShipPart>()?.Props.isPlating ?? false)) { } //clean multiple hull spawns
                        else
                            GenSpawn.Spawn(thing, new IntVec3(c.x + shape.x, 0, c.z + shape.z), ImportedShip, shape.rot);
                    }
                }
                else if (DefDatabase<TerrainDef>.GetNamedSilentFail(shape.shapeOrDef) != null)
                {
                    IntVec3 pos = new IntVec3(shape.x, 0, shape.z);
                    if (shipDef.saveSysVer == 2)
                        pos = new IntVec3(c.x + shape.x, 0, c.z + shape.z);
                    ImportedShip.terrainGrid.SetTerrain(pos, DefDatabase<TerrainDef>.GetNamed(shape.shapeOrDef));
                }
            }
            if (!shipDef.core.shapeOrDef.NullOrEmpty())
            {
                Building core = (Building)ThingMaker.MakeThing(ThingDef.Named(shipDef.core.shapeOrDef));
                core.SetFaction(Faction.OfPlayer);
                Rot4 corerot = shipDef.core.rot;
                GenSpawn.Spawn(core, new IntVec3(c.x + shipDef.core.x, 0, c.z + shipDef.core.z), ImportedShip, corerot);
            }
            foreach (Building b in ImportedShip.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial))
            {
                CompPowerTrader trader = b.TryGetComp<CompPowerTrader>();
                if (trader != null)
                {
                    trader.PowerOn = true;
                }
                if (b is Building_ShipBridge bridge)
                    bridge.ShipName = shipDef.defName;
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
