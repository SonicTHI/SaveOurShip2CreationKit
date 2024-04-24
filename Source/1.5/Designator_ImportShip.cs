using UnityEngine;
using Verse;
using RimWorld;

namespace SaveOurShip2
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
	public class Dialog_LoadShip : Dialog_RenameShip
	{
		private string ship = "shipdeftoload";
		//public static Map ImportedShip;
		public Dialog_LoadShip(string ship)
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
			SoSBuilder.GenerateShip(shipDef);
		}
	}
}
