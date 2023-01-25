using System;
using System.Collections.Generic;
using System.IO;
using RimWorld;
using Verse;
using UnityEngine;
using HarmonyLib;

namespace RimworldMod
{
    public class SoSBuilder : Mod
    {
        public static Building_ShipCircle lastCirclePlaced;
        public static Building_ShipRect lastRectPlaced;
        public static Building_ShipRegion lastRegionPlaced;

        public static Dictionary<Map, string> shipDictionary = new Dictionary<Map, string>();

        public SoSBuilder(ModContentPack content) : base(content)
        {
            var harmony = new Harmony("SoSBuilder");
            harmony.PatchAll();
        }

        public static void GenerateHull(List<IntVec3> border, List<IntVec3> interior, Map map, ThingDef hull, ThingDef floor)
        {
            foreach (IntVec3 vec in interior)
            {
                if (vec.GetFirstThing(map, floor) == null)
                {
                    Thing f = ThingMaker.MakeThing(floor);
                    Thing t = GenSpawn.Spawn(f, vec, map);
                }
            }
            foreach (IntVec3 vec in border)
            {
                if (!GenSpawn.WouldWipeAnythingWith(vec, Rot4.South, hull, map, (Thing x) => x.def.category == ThingCategory.Building))
                {
                    Thing wall = ThingMaker.MakeThing(hull);
                    wall.SetFaction(Faction.OfPlayer);
                    Thing t = GenSpawn.Spawn(wall, vec, map);
                }
            }
        }
        public static bool ExportToIgnore(Thing t, Building_ShipBridge shipCore)
        {
            if (t is Pawn || t == shipCore || t.def.defName.StartsWith("Lighting_MURWallLight_Glower") || t.def.defName.Equals("Lighting_MURWallSunLight_Glower"))
            {
                return true;
            }
            return false;
        }
        //cleanup for bad exports + temp for rework
        public static bool ImportToIgnore(ThingDef def)
        {
            if (def.defName.StartsWith("Lighting_MURWallLight_Glower") || def.defName.Equals("Lighting_MURWallSunLight_Glower"))
            {
                return true;
            }
            return false;
        }
    }
    public struct ShipPosRotShape
    {
        public int x;
        public int z;
        public Rot4 rot;
        public string shape;

        public override int GetHashCode()
        {
            return (x + "," + z + "," + rot).GetHashCode();
        }
    }

    [HarmonyPatch(typeof(LetterStack), "LettersOnGUI")]
    public static class ShipCKWarning
    {
        [HarmonyPrefix]
        public static bool DrawWarning(ref float baseY)
        {
            float num = (float)UI.screenWidth - 200f;
            Rect rect = new Rect(num, baseY - 16f, 193f, 26f);
            Text.Anchor = TextAnchor.MiddleRight;
            string text = "WARNING: SOS2CK IS ACTIVE";
            float x = Text.CalcSize(text).x;
            Rect rect2 = new Rect(rect.xMax - x, rect.y, x, rect.height);
            if (Mouse.IsOver(rect2))
            {
                Widgets.DrawHighlight(rect2);
            }
            TooltipHandler.TipRegionByKey(rect2, "SOS2 Creation Kit IS ACTIVE!\nDO NOT USE IT IN NORMAL PLAY");
            Widgets.Label(rect2, text);
            Text.Anchor = TextAnchor.UpperLeft;
            baseY -= 26f;
            return true;
        }
    }
}

