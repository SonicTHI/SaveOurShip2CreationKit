using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;

namespace SaveOurShip2
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
			defaultDesc = "Export target ship as a blueprint. Ship must already be present in an active mod!";
			icon = ContentFinder<Texture2D>.Get("UI/Save_XML");
			soundDragSustain = SoundDefOf.Designate_DragStandard;
			soundDragChanged = SoundDefOf.Designate_DragStandard_Changed;
			useMouseIcon = true;
			soundSucceeded = SoundDefOf.Designate_Deconstruct;
		}

		public override void DesignateSingleCell(IntVec3 loc)
		{
			if (!Map.IsSpace())
			{
				Messages.Message("Not on space map", MessageTypeDefOf.RejectInput);
				return;
			}
			ShipDef shipDef = null;
			Building_ShipBridge bridge = null;
			string defName = "error";
			string label = "error";
			string description = "error";
			string shipDefName = "error";

			foreach (Building_ShipBridge b in loc.GetThingList(Map).Where(t => t is Building_ShipBridge))
			{
				if (DefDatabase<ShipDef>.AllDefs.Any(s => s.defName.Equals(b.ShipName)))
				{
					shipDef = DefDatabase<ShipDef>.AllDefs.Where(s => s.defName.Equals(b.ShipName)).FirstOrDefault();
					bridge = b;
					defName = "Ship" + shipDef.defName;
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
				Messages.Message("Click on ship bridge", MessageTypeDefOf.RejectInput);
				return;
			}
			int threat = 0;
			int mass = 0;
			float thrust = 0;
			List<Building> cachedShipParts = ShipUtility.ShipBuildingsAttachedTo(bridge);
			List<ResearchProjectDef> researchListFirst = new List<ResearchProjectDef>();
			List<ResearchProjectDef> researchListSecond = new List<ResearchProjectDef>();
			List<ResearchProjectDef> researchList = new List<ResearchProjectDef>();
			Dictionary<ThingDef, int> costList = new Dictionary<ThingDef, int>();
			Dictionary<string, int> weaponList = new Dictionary<string, int>();
			foreach (Building b in cachedShipParts)
			{
				if (b.TryGetComp<CompShipCachePart>()?.Props.isPlating ?? false)
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
					else if (b.def == ResourceBank.ThingDefOf.ShipSpinalAmplifier)
						threat += 10;
					else if (b.def == ThingDef.Named("ShipPartTurretSmall")) //default to plasma for randoms
					{
						threat += 10;
						if (!weaponList.ContainsKey("ShipTurret_Plasma"))
						{
							weaponList.Add("ShipTurret_Plasma", 1);
						}
						else
							weaponList["ShipTurret_Plasma"] += 1;
					}
					else if (b.def == ThingDef.Named("ShipPartTurretLarge"))
					{
						threat += 30;
						if (!weaponList.ContainsKey("ShipTurret_Plasma_Large"))
						{
							weaponList.Add("ShipTurret_Plasma_Large", 1);
						}
						else
							weaponList["ShipTurret_Plasma_Large"] += 1;
					}
					else if (b.def == ThingDef.Named("ShipPartTurretSpinal"))
					{
						threat += 100;
						if (!weaponList.ContainsKey("ShipSpinalBarrelPlasma"))
						{
							weaponList.Add("ShipSpinalBarrelPlasma", 1);
						}
						else
							weaponList["ShipSpinalBarrelPlasma"] += 1;
					}

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
				if (b.def.researchPrerequisites.NullOrEmpty())
					continue;
				foreach (ResearchProjectDef res in b.def.researchPrerequisites)
				{
					if (!researchList.Contains(res))
						researchList.Add(res);
				}
			}

			researchList.Remove(ResearchProjectDef.Named("ShipBasics"));
			researchListFirst.Add(ResearchProjectDef.Named("ShipBasics"));
			if (researchList.Contains(ResearchProjectDef.Named("Electricity")))
			{
				researchListFirst.Add(ResearchProjectDef.Named("Electricity"));
				researchList.Remove(ResearchProjectDef.Named("Electricity"));
			}
			if (researchList.Contains(ResearchProjectDef.Named("SolarPanels")))
			{
				researchListFirst.Add(ResearchProjectDef.Named("SolarPanels"));
				researchList.Remove(ResearchProjectDef.Named("SolarPanels"));
			}
			if (researchList.Contains(ResearchProjectDef.Named("AirConditioning")))
			{
				researchListFirst.Add(ResearchProjectDef.Named("AirConditioning"));
				researchList.Remove(ResearchProjectDef.Named("AirConditioning"));
			}
			if (researchList.Contains(ResearchProjectDef.Named("ShipEngine")))
			{
				researchListSecond.Add(ResearchProjectDef.Named("ShipEngine"));
				researchList.Remove(ResearchProjectDef.Named("ShipEngine"));
			}
			if (cachedShipParts.Any(b => b.def.defName.Equals("Ship_Engine_Large")) && researchList.Contains(ResearchProjectDef.Named("ShipReactor")))
			{
				researchListSecond.Add(ResearchProjectDef.Named("ShipReactor"));
				researchList.Remove(ResearchProjectDef.Named("ShipReactor"));
			}
			if (researchList.Contains(ResearchProjectDef.Named("SoSJTDrive")))
			{
				researchListSecond.Add(ResearchProjectDef.Named("SoSJTDrive"));
				researchList.Remove(ResearchProjectDef.Named("SoSJTDrive"));
			}

			thrust *= 500f / Mathf.Pow(cachedShipParts.Count, 1.1f);
			threat += mass / 100;
			description = "A three stage reusable blueprint that can be used to build a complete space ship as long as one is capable of constructing the individual parts. Can be customized at any point but the next stage will only place prints when the required parts under them are already built.\\n\\n";
			description += "Class: " + shipDef.label + "\\n[DESCRIPTION HERE]\\n\\n";
			description += "Mass: " + mass + "\\n";
			description += "Size: " + shipDef.sizeX + " x " + shipDef.sizeZ + "\\n";
			description += "T/W ratio: " + thrust.ToString("F3") + "\\n";
			description += "Combat rating: " + threat + "\\n";
			description += "\\nWeapons: " + "\\n";
			if (weaponList.NullOrEmpty())
			{
				description += "none";
			}
			foreach (string s in weaponList.Keys)
			{
				description += weaponList[s] + "x " + s + "\\n";
			}
			description += "\\nRequired resources: " + "\\n";
			foreach (ThingDef def in costList.Keys)
			{
				description += def.label + ": " + costList[def] + "\\n";
			}
			description += "\\nRequired research for full construction:\\nFirst stage:\\n";
			for (int i = 0; i < researchListFirst.Count; i++)
			{
				description += researchListFirst[i].label;
				if (i < researchListFirst.Count - 1)
					description += ", ";
			}
			description += "\\nSecond stage:\\n";
			for (int i = 0; i < researchListSecond.Count; i++)
			{
				description += researchListSecond[i].label;
				if (i < researchListSecond.Count - 1)
					description += ", ";
			}
			description += "\\nThird stage:\\n";
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
			string newDefName = "ShipBP" + defName;
			string filename = Path.Combine(path, "blueprintTemp.xml");
			SafeSaver.Save(filename, "Defs", () =>
			{
				Scribe.EnterNode("ThingDef");
				{
					Scribe_Values.Look<string>(ref newDefName, "defName");
					label = "ship blueprint (" + shipDef.label + ")";
					Scribe_Values.Look<string>(ref label, "label");
					Scribe_Values.Look<string>(ref description, "description");
					Scribe.EnterNode("statBases");
					{
						/*int maxHitPoints = 20;
						Scribe_Values.Look<int>(ref maxHitPoints, "MaxHitPoints", 0);
						float massTemp = 0.05f;
						Scribe_Values.Look<float>(ref massTemp, "Mass", 0);
						float flammability = 1;
						Scribe_Values.Look<float>(ref flammability, "Flammability", 0);*/
						int marketValue = 2000;
						Scribe_Values.Look<int>(ref marketValue, "MarketValue", 0);
						Scribe.ExitNode();
					}
					Scribe.EnterNode("comps");
					{
						Scribe.EnterNode("li");
						{
							Scribe_Values.Look<string>(ref shipDefName, "shipDef");
							Scribe.ExitNode();
						}
						Scribe.ExitNode();
					}
					Scribe.ExitNode();
				}
			});
			Messages.Message("Saved bluprint in temp file: blueprintTemp.xml", MessageTypeDefOf.PositiveEvent);
		}
	}
}
