using UnityEngine;
using Verse;
using RimWorld;

namespace SaveOurShip2
{
	class Designator_ExportShip : Designator
	{
		public override AcceptanceReport CanDesignateCell(IntVec3 loc)
		{
			return true;
		}

		public Designator_ExportShip()
		{
			defaultLabel = "Export Ship";
			defaultDesc = "Save this ship to an XML file. You will need to set the name and tags manually. Click anywhere on the map to activate.";
			icon = ContentFinder<Texture2D>.Get("UI/Save_XML");
			soundDragSustain = SoundDefOf.Designate_DragStandard;
			soundDragChanged = SoundDefOf.Designate_DragStandard_Changed;
			useMouseIcon = true;
			soundSucceeded = SoundDefOf.Designate_Deconstruct;
		}

		//new save system from min x/z
		public override void DesignateSingleCell(IntVec3 loc)
		{
			SoSBuilder.ExportShip(Map);
		}
	}
}
