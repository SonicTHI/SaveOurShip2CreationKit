using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;

namespace SaveOurShip2
{
	class Designator_ImportShipRotCclean : Designator
	{
		public override AcceptanceReport CanDesignateCell(IntVec3 loc)
		{
			if (Find.CurrentMap.IsSpace())
				return true;
			Messages.Message("Ship editor works only on space maps!", MessageTypeDefOf.RejectInput);
			return false;
		}
		public Designator_ImportShipRotCclean()
		{
			defaultLabel = "Import Ship Rotated 90° CCW, cleaned";
			defaultDesc = "Click anywhere on the map to activate.\nWill discard anything but walls and engines.";
			icon = ContentFinder<Texture2D>.Get("UI/Load_XML");
			soundDragSustain = SoundDefOf.Designate_DragStandard;
			soundDragChanged = SoundDefOf.Designate_DragStandard_Changed;
			useMouseIcon = true;
			soundSucceeded = SoundDefOf.Designate_Deconstruct;
		}
		public override void DesignateSingleCell(IntVec3 loc)
		{
			Find.WindowStack.Add(new Dialog_LoadShipRotCclean("shipdeftoloadrotl"));
		}
	}
	public class Dialog_LoadShipRotCclean : Dialog_RenameShip
	{
		private string ship= "shipdeftoloadrotl";
		//public static Map ImportedShip;
		public Dialog_LoadShipRotCclean(string ship)
		{
			curName = ship;
		}

		protected override void SetName(string name)
		{
			if (name == ship || string.IsNullOrEmpty(name))
				return;
			ShipDef shipDef = DefDatabase<ShipDef>.GetNamed(name);
			if (shipDef == null)
				return;
			GenerateShip(shipDef);
		}

		public static void GenerateShip(ShipDef shipDef)
		{
			Map map = GetOrGenerateMapUtility.GetOrGenerateMap(ShipInteriorMod2.FindWorldTile(), new IntVec3(250, 1, 250), DefDatabase<WorldObjectDef>.GetNamed("ShipEnemy"));
			map.GetComponent<ShipMapComp>().CacheOff = true;
			map.GetComponent<ShipMapComp>().ShipMapState = ShipMapState.isGraveyard;
			((WorldObjectOrbitingShip)map.Parent).Radius = 150;
			((WorldObjectOrbitingShip)map.Parent).Theta = ((WorldObjectOrbitingShip)Find.CurrentMap.Parent).Theta - Rand.RangeInclusive(1,10)* 0.01f;
			GetOrGenerateMapUtility.UnfogMapFromEdge(map);

			IntVec3 c = map.Center;
			if (shipDef.saveSysVer == 2)
				c = new IntVec3(map.Size.x - shipDef.offsetZ, 0, shipDef.offsetX);
			SoSBuilder.shipDictionary.Add(map, shipDef.defName);

			foreach (ShipShape shape in shipDef.parts)
			{
				if (DefDatabase<ThingDef>.GetNamedSilentFail(shape.shapeOrDef) != null)
				{
					Thing thing;
					ThingDef def = ThingDef.Named(shape.shapeOrDef);
					if (map.listerThings.AllThings.Where(t => t.Position.x == shape.x && t.Position.z == shape.z) != def)
					{
						if (!(def.defName.StartsWith("Ship_Corner") || def.defName.StartsWith("Ship_Beam") || def.defName.StartsWith("Ship_Engine")))
						{
							continue;
						}
						Rot4 rota = shape.rot;
						int adjz = shape.x;
						int adjx = shape.z;
						if (def.rotatable == true)
							rota.Rotate(RotationDirection.Counterclockwise);
						else if (def.rotatable == false && def.size.z != def.size.x)//skip non rot, non even
							continue;
						//pos
						if (def.size.z % 2 == 0 && def.size.x % 2 == 0 && rota.AsByte == 0)
							adjx += 1;

						thing = ThingMaker.MakeThing(def);

						if (thing.def.CanHaveFaction && thing.def != ResourceBank.ThingDefOf.ShipHullTile)
							thing.SetFaction(Faction.OfPlayer);
						GenSpawn.Spawn(thing, new IntVec3(c.x - adjx, 0, c.z + adjz), map, rota);
					}
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
			map.mapDrawer.RegenerateEverythingNow();
			map.regionAndRoomUpdater.RebuildAllRegionsAndRooms();
			map.temperatureCache.ResetTemperatureCache();
			map.GetComponent<ShipMapComp>().RecacheMap();
			CameraJumper.TryJump(c, map);
		}
	}
}
