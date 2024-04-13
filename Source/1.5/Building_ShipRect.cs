using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace SaveOurShip2
{
	public class Building_ShipRect : Building
	{
		public int width = 0;
		public int height = 0;
		ThingDef hull;
		ThingDef floor;

		public override void SpawnSetup(Map map, bool respawningAfterLoad)
		{
			if (this.def.defName.Equals("ShipPartRectUnpEnd"))
			{
				hull = ThingDef.Named("Ship_Beam_Unpowered");
				floor = ThingDef.Named("ShipHullTile");
			}
			else if (this.def.defName.Equals("ShipPartRectEnd"))
			{
				hull = ThingDef.Named("Ship_Beam");
				floor = ThingDef.Named("ShipHullTile");
			}
			else if (this.def.defName.Equals("ShipPartRectWreckEnd"))
			{
				hull = ThingDef.Named("Ship_Beam_Wrecked");
				floor = ThingDef.Named("ShipHullTileWrecked");
			}
			else if (this.def.defName.Equals("MechPartRectUnpEnd"))
			{
				hull = ThingDef.Named("Ship_BeamMech_Unpowered");
				floor = ThingDef.Named("ShipHullTileMech");
			}
			else if (this.def.defName.Equals("MechPartRectEnd"))
			{
				hull = ThingDef.Named("Ship_BeamMech");
				floor = ThingDef.Named("ShipHullTileMech");
			}
			base.SpawnSetup(map, respawningAfterLoad);
			if(!respawningAfterLoad)
			{
				if (this.def.defName.Equals("ShipPartRect"))
				{
					if (SoSBuilder.lastRectPlaced != null)
					{
						SoSBuilder.lastRectPlaced.Destroy();
					}
					SoSBuilder.lastRectPlaced = this;
				}
				else
				{
					if(SoSBuilder.lastRectPlaced == null)
					{
						this.Destroy();
						return;
					}
					IntVec3 lowestCorner = new IntVec3(Math.Min(Position.x,SoSBuilder.lastRectPlaced.Position.x), 0, Math.Min(Position.z, SoSBuilder.lastRectPlaced.Position.z));
					SoSBuilder.lastRectPlaced.width = Math.Max(Position.x, SoSBuilder.lastRectPlaced.Position.x) - lowestCorner.x + 1;
					SoSBuilder.lastRectPlaced.height = Math.Max(Position.z, SoSBuilder.lastRectPlaced.Position.z) - lowestCorner.z + 1;
					SoSBuilder.lastRectPlaced.Position = lowestCorner;
					List<IntVec3> border = new List<IntVec3>();
					List<IntVec3> interior = new List<IntVec3>();
					ShipInteriorMod2.RectangleUtility(SoSBuilder.lastRectPlaced.Position.x, SoSBuilder.lastRectPlaced.Position.z, SoSBuilder.lastRectPlaced.width, SoSBuilder.lastRectPlaced.height, ref border, ref interior);
					this.Destroy();
					SoSBuilder.GenerateHull(border, interior, Find.CurrentMap, hull, floor);
					SoSBuilder.lastRectPlaced.Destroy();
					SoSBuilder.lastRectPlaced = null;
				}
			}
		}

		public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
		{
			base.Destroy(mode);
		}
	}
}
