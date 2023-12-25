using RimworldMod;
using System.Linq;
using UnityEngine;
using Verse;

namespace RimWorld
{
    class Designator_ExportShipRe : Designator
    {
        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            return true;
        }

        public Designator_ExportShipRe()
        {
            defaultLabel = "ReSave Ship";
            defaultDesc = "Resave this ship to an XML file with the same name and tags it was imported with. Click anywhere on the map to activate.";
            icon = ContentFinder<Texture2D>.Get("UI/Save_XML");
            soundDragSustain = SoundDefOf.Designate_DragStandard;
            soundDragChanged = SoundDefOf.Designate_DragStandard_Changed;
            useMouseIcon = true;
            soundSucceeded = SoundDefOf.Designate_Deconstruct;
        }

        //new resave system from min x/z
        public override void DesignateSingleCell(IntVec3 loc)
        {
            if (!SoSBuilder.shipDictionary.Keys.Contains(Map))
            {
                Messages.Message("Could not resave the ship, info either missing or corrupt. Use normal save!", null, MessageTypeDefOf.NegativeEvent);
                return;
            }
            SoSBuilder.ExportShip(Map, true);
        }
    }
}
