using RimworldMod;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using SaveOurShip2;

namespace RimWorld
{
    class Designator_ExportBlueprint : Designator
    {
        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            return true;
        }

        public Designator_ExportBlueprint()
        {
            defaultLabel = "Export Blueprint";
            defaultDesc = "EXport target ship as a blueprint. Ship must already be present in an active mod!";
            icon = ContentFinder<Texture2D>.Get("UI/Save_XML");
            soundDragSustain = SoundDefOf.Designate_DragStandard;
            soundDragChanged = SoundDefOf.Designate_DragStandard_Changed;
            useMouseIcon = true;
            soundSucceeded = SoundDefOf.Designate_Deconstruct;
        }

        public override void DesignateSingleCell(IntVec3 loc)
        {
            if (!Find.CurrentMap.IsSpace())
            {
                Messages.Message("Not on space map", MessageTypeDefOf.RejectInput);
                return;
            }
            EnemyShipDef shipDef = null;
            Building_ShipBridge bridge = null;
            string defName = "error";
            string label = "error";
            string description = "error";
            string shipDefName = "error";

            foreach (Building_ShipBridge b in loc.GetThingList(Find.CurrentMap).Where(t => t is Building_ShipBridge))
            {
                if (DefDatabase<EnemyShipDef>.AllDefs.Any(s => s.defName.Equals(b.ShipName)))
                {
                    shipDef = DefDatabase<EnemyShipDef>.AllDefs.Where(s => s.defName.Equals(b.ShipName)).FirstOrDefault();
                    bridge = b;
                    defName = "ShipBlueprint" + shipDef.defName;
                    shipDefName = shipDef.defName;
                }
                else
                {
                    Messages.Message("Ship not found in database! If this is a new ship, save it first and put it into an active mod!", MessageTypeDefOf.RejectInput);
                    return;
                }
            }
            if (bridge == null)
            {
                Messages.Message("No bridge found", MessageTypeDefOf.RejectInput);
                return;
            }
            int threat = 0;
            int mass = 0;
            float thrust = 0;
            List<Building> cachedShipParts = ShipUtility.ShipBuildingsAttachedTo(bridge);
            List<ResearchProjectDef> researchList = new List<ResearchProjectDef>();
            Dictionary<ThingDef, int> costList = new Dictionary<ThingDef, int>();
            Dictionary<string, int> weaponList = new Dictionary<string, int>();
            foreach (Building b in cachedShipParts)
            {
                if (b.TryGetComp<CompSoShipPart>()?.Props.isPlating ?? false)
                    mass += 1;
                else
                {
                    mass += (b.def.size.x * b.def.size.z) * 3;
                    if (b.TryGetComp<CompShipHeat>() != null)
                    {
                        threat += b.TryGetComp<CompShipHeat>().Props.threat;
                        if (b is Building_ShipTurret)
                        {
                            if (!weaponList.ContainsKey(b.Label))
                            {
                                weaponList.Add(b.Label, 1);
                            }
                            else
                                weaponList[b.Label] += 1;
                        }
                    }
                    else if (b.def == ThingDef.Named("ShipSpinalAmplifier"))
                        threat += 5;
                    var engine = b.TryGetComp<CompEngineTrail>();
                    if (engine != null)
                    {
                        thrust += engine.Props.thrust;
                    }
                }
                if (b.def.CostList.NullOrEmpty())
                    continue;
                foreach (ThingDefCountClass mat in b.def.CostList)
                {
                    if (!costList.ContainsKey(mat.thingDef))
                    {
                        costList.Add(mat.thingDef, mat.count);
                    }
                    else
                        costList[mat.thingDef] += mat.count;
                }
                foreach (ResearchProjectDef res in b.def.researchPrerequisites)
                {
                    if (!researchList.Contains(res))
                        researchList.Add(res);
                }
            }
            thrust *= 500f / Mathf.Pow(cachedShipParts.Count, 1.1f);
            threat += mass / 100;
            description = "Class: " + shipDef.label + "\\n";
            description += "Mass: " + mass + "\\n";
            description += "T/W ratio: " + thrust.ToString("F3") + "\\n";
            description += "Combat rating: " + threat + "\\n";
            description += "\\nWeapons: " + "\\n";
            foreach (string s in weaponList.Keys)
            {
                description += weaponList[s] + "x " + s + "\\n";
            }
            description += "\\nRequired resources: " + "\\n";
            foreach (ThingDef def in costList.Keys)
            {
                description += def.label + ": " + costList[def] + "\\n";
            }
            description += "\\nRequired research: " + "\\n";
            for (int i = 0; i < researchList.Count; i++)
            {
                description += researchList[i].label;
                if (i < researchList.Count - 1)
                    description += ", ";
            }

            string path = Path.Combine(GenFilePaths.SaveDataFolderPath, "ExportedShips");
            DirectoryInfo dir = new DirectoryInfo(path);
            if (!dir.Exists)
                dir.Create();
            string filename = Path.Combine(path, "blueprintTemp.xml");
            SafeSaver.Save(filename, "Defs", () =>
            {
                Scribe.EnterNode("ThingDef");
                    Scribe_Values.Look<string>(ref defName, "defName");
                    label = "[INSERT IN-GAME NAME HERE]";
                    Scribe_Values.Look<string>(ref label, "label");
                    Scribe_Values.Look<string>(ref description, "description");
                    Scribe.EnterNode("statBases");
                    /*int maxHitPoints = 20;
                    Scribe_Values.Look<int>(ref maxHitPoints, "MaxHitPoints", 0);
                    float massTemp = 0.05f;
                    Scribe_Values.Look<float>(ref massTemp, "Mass", 0);
                    float flammability = 1;
                    Scribe_Values.Look<float>(ref flammability, "Flammability", 0);*/
                    int marketValue = 4000;
                    Scribe_Values.Look<int>(ref marketValue, "MarketValue", 0);
                    Scribe.ExitNode();
                    Scribe.EnterNode("comps");
                        Scribe.EnterNode("li");
                            Scribe_Values.Look<string>(ref shipDefName, "shipDef");
                        Scribe.ExitNode();
                    Scribe.ExitNode();
                Scribe.ExitNode();
            });
            Messages.Message("Saved bluprint in temp file: blueprintTemp.xml", MessageTypeDefOf.PositiveEvent);
        }
    }
}
