using UnityEngine;
using Verse;
using SaveOurShip2;

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
        private string ship = "shipdeftoloadrotl";
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
            SoSBuilder.GenerateShip(shipDef, true);
        }
    }
}
