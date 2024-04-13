using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;

namespace SaveOurShip2
{
	public class Building_ShipCircle : Building
	{
		public int radius = 0;
		ThingDef hull;
		ThingDef floor;

		public override void SpawnSetup(Map map, bool respawningAfterLoad)
		{
			if (this.def.defName.Equals("ShipPartCircleUnpEnd"))
			{
				hull = ThingDef.Named("Ship_Beam_Unpowered");
				floor = ThingDef.Named("ShipHullTile");
			}
			else if (this.def.defName.Equals("ShipPartCircleEnd"))
			{
				hull = ThingDef.Named("Ship_Beam");
				floor = ThingDef.Named("ShipHullTile");
			}
			else if (this.def.defName.Equals("ShipPartCircleWreckEnd"))
			{
				hull = ThingDef.Named("Ship_Beam_Wrecked");
				floor = ThingDef.Named("ShipHullTileWrecked");
			}
			else if (this.def.defName.Equals("MechPartCircleUnpEnd"))
			{
				hull = ThingDef.Named("Ship_BeamMech_Unpowered");
				floor = ThingDef.Named("ShipHullTileMech");
			}
			else if (this.def.defName.Equals("MechPartCircleEnd"))
			{
				hull = ThingDef.Named("Ship_BeamMech");
				floor = ThingDef.Named("ShipHullTileMech");
			}
			base.SpawnSetup(map, respawningAfterLoad);
			if(!respawningAfterLoad)
			{
				if (this.def.defName.Equals("ShipPartCircle"))
				{
					if (SoSBuilder.lastCirclePlaced != null)
					{
						SoSBuilder.lastCirclePlaced.Destroy();
					}
					SoSBuilder.lastCirclePlaced = this;
				}
				else
				{
					if(SoSBuilder.lastCirclePlaced == null)
					{
						this.Destroy();
						return;
					}
					SoSBuilder.lastCirclePlaced.radius = (int)this.Position.DistanceTo(SoSBuilder.lastCirclePlaced.Position);
					List<IntVec3> border = new List<IntVec3>();
					List<IntVec3> interior = new List<IntVec3>();
					ShipInteriorMod2.CircleUtility(SoSBuilder.lastCirclePlaced.Position.x, SoSBuilder.lastCirclePlaced.Position.z, SoSBuilder.lastCirclePlaced.radius, ref border, ref interior);
					this.Destroy();
					SoSBuilder.GenerateHull(border, interior, Find.CurrentMap, hull, floor);
					SoSBuilder.lastCirclePlaced.Destroy();
					SoSBuilder.lastCirclePlaced = null;
				}
			}
		}

		public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
		{
			base.Destroy(mode);
		}
	}
}
