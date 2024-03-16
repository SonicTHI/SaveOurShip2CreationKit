using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using SaveOurShip2;

namespace RimWorld
{
    public class Building_ShipRegion : Building
    {
        public int width = 0;
        public int height = 0;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if(!respawningAfterLoad)
            {
                if (this.def.defName.Equals("ShipPartRegion"))
                {
                    if (SoSBuilder.lastRegionPlaced != null)
                    {
                        SoSBuilder.lastRegionPlaced.Destroy();
                    }
                    SoSBuilder.lastRegionPlaced = this;
                }
                else
                {
                    if(SoSBuilder.lastRegionPlaced == null)
                    {
                        this.Destroy();
                        return;
                    }
                    IntVec3 lowestCorner = new IntVec3(Math.Min(Position.x,SoSBuilder.lastRegionPlaced.Position.x), 0, Math.Min(Position.z, SoSBuilder.lastRegionPlaced.Position.z));
                    SoSBuilder.lastRegionPlaced.width = Math.Max(Position.x, SoSBuilder.lastRegionPlaced.Position.x) - lowestCorner.x + 1;
                    SoSBuilder.lastRegionPlaced.height = Math.Max(Position.z, SoSBuilder.lastRegionPlaced.Position.z) - lowestCorner.z + 1;
                    SoSBuilder.lastRegionPlaced.Position = lowestCorner;
                    List<IntVec3> border = new List<IntVec3>();
                    List<IntVec3> interior = new List<IntVec3>();this.Destroy();
                    SoSBuilder.lastRegionPlaced = null;
                }
            }
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            base.Destroy(mode);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref width, "width");
            Scribe_Values.Look<int>(ref height, "height");
        }

        public override string GetInspectString()
        {
            return "Width: "+width+"\nHeight: "+height;
        }
    }
}
