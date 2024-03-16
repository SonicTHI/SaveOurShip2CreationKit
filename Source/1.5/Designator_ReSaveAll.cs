using UnityEngine;
using Verse;
using SaveOurShip2;

namespace RimWorld
{
    class Designator_ReSaveAll : Designator
    {
        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            return true;
        }

        public Designator_ReSaveAll()
        {
            defaultLabel = "ReSave ALL!";
            defaultDesc = "ReSave ALL ships into a subdir. CR will be recalced. Click anywhere on the map to activate.";
            icon = ContentFinder<Texture2D>.Get("UI/Save_XML");
            soundDragSustain = SoundDefOf.Designate_DragStandard;
            soundDragChanged = SoundDefOf.Designate_DragStandard_Changed;
            useMouseIcon = true;
            soundSucceeded = SoundDefOf.Designate_Deconstruct;
        }

        //new save system from min x/z
        public override void DesignateSingleCell(IntVec3 loc)
        {
            SoSBuilder.ReSaveAll();
        }
    }
}
