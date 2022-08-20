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
        public static List<EnemyShipDef> shipDefsAll = new List<EnemyShipDef>();

        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            return true;
        }
        public Designator_ImportShipRotC()
        {
            defaultLabel = "Import Ship Rotated 90° CCW";
            defaultDesc = "Click anywhere on the map to activate.\nWARNING: Non rotatable, non even sided buildings  will be discarded!";
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
            GenerateShip(shipDef);
        }

        public static void GenerateShip(EnemyShipDef shipDef)
        {
            Map ImportedShip = GetOrGenerateMapUtility.GetOrGenerateMap(ShipInteriorMod2.FindWorldTile(), new IntVec3(250, 1, 250), DefDatabase<WorldObjectDef>.GetNamed("ShipEnemy"));
            ImportedShip.GetComponent<ShipHeatMapComp>().IsGraveyard = true;
            ImportedShip.GetComponent<ShipHeatMapComp>().ShipCombatOriginMap = ((MapParent)Find.WorldObjects.AllWorldObjects.Where(ob => ob.def.defName.Equals("ShipOrbiting")).FirstOrDefault()).Map;
            ((WorldObjectOrbitingShip)ImportedShip.Parent).radius = 150;
            ((WorldObjectOrbitingShip)ImportedShip.Parent).theta = ((WorldObjectOrbitingShip)Find.CurrentMap.Parent).theta - Rand.RangeInclusive(1,10)* 0.01f;
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
                    GenSpawn.Spawn(thing, new IntVec3(c.x - shape.z, 0, c.z + shape.z), ImportedShip);
                    thing.TryGetComp<CompNameMe>().pawnKindDef = shape.stuff;
                }
                else if (shape.shapeOrDef.Equals("Cargo"))
                {
                    SoSBuilder.lastRegionPlaced = null;
                    ThingDef def = ThingDef.Named("ShipPartRegion");
                    Thing thing = ThingMaker.MakeThing(def);
                    GenSpawn.Spawn(thing, new IntVec3(c.x - shape.z - shape.height + 1, 0, c.z + shape.x), ImportedShip);
                    ((Building_ShipRegion)thing).width = shape.height;
                    ((Building_ShipRegion)thing).height = shape.width;
                }
                if (DefDatabase<ThingDef>.GetNamedSilentFail(shape.shapeOrDef) != null)
                {
                    Thing thing;
                    ThingDef def = ThingDef.Named(shape.shapeOrDef);
                    if (ImportedShip.listerThings.AllThings.Where(t => t.Position.x == shape.x && t.Position.z == shape.z) != def)
                    {
                        if (SoSBuilder.ImportToIgnore(def))
                            continue;
                        Rot4 rota = shape.rot;
                        int adjz = shape.x;
                        int adjx = shape.z;
                        if (def.rotatable == true)
                            rota.Rotate(RotationDirection.Counterclockwise);
                        else if (def.rotatable == false && def.size.z != def.size.x)//skip non rot, non even
                            continue;
                        //pos
                        if (def.size.z == 2 && def.size.x == 2 && rota.AsByte == 0)
                            adjx += 1;

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
                        //if (thing.TryGetComp<CompRefuelable>() != null)
                        //    thing.TryGetComp<CompRefuelable>().Refuel(thing.TryGetComp<CompRefuelable>().Props.fuelCapacity);
                        var compShield = thing.TryGetComp<CompShipCombatShield>();
                        if (compShield != null)
                        {
                            compShield.radiusSet = 40;
                            compShield.radius = 40;
                            compShield.radiusSet = shape.radius;
                            compShield.radius = shape.radius;
                        }
                        if (thing.def.stackLimit > 1)
                            thing.stackCount = (int)Math.Min(25, thing.def.stackLimit);
                        if (thing.def == ShipInteriorMod2.hullPlateDef && new IntVec3(c.x - adjx, 0, c.z + adjz).GetThingList(ImportedShip).Any(t => t.def == ShipInteriorMod2.hullPlateDef)) { } //clean multiple hull spawns
                        else
                            GenSpawn.Spawn(thing, new IntVec3(c.x - adjx, 0, c.z + adjz), ImportedShip, rota);
                    }
                }
                else if (DefDatabase<TerrainDef>.GetNamedSilentFail(shape.shapeOrDef) != null)
                {
                    IntVec3 pos = new IntVec3(shape.x, 0, shape.z);
                    if (shipDef.saveSysVer == 2)
                        pos = new IntVec3(c.x + shape.x, 0, c.z + shape.z);
                    ImportedShip.terrainGrid.SetTerrain(new IntVec3(ImportedShip.Size.x - pos.z, 0, pos.x), DefDatabase<TerrainDef>.GetNamed(shape.shapeOrDef));
                }
            }
            Building core = (Building)ThingMaker.MakeThing(ThingDef.Named(shipDef.core.shapeOrDef)); 
            core.SetFaction(Faction.OfPlayer);
            Rot4 corerot= shipDef.core.rot.Rotated(RotationDirection.Counterclockwise);
            GenSpawn.Spawn(core, new IntVec3(c.x - shipDef.core.z, 0, c.z + shipDef.core.x), ImportedShip, corerot);
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
