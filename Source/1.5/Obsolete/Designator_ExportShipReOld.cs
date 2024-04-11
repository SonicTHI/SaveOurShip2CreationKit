/*using RimworldMod;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace RimWorld
{
	class Designator_ExportShipReOld : Designator
	{
		public override AcceptanceReport CanDesignateCell(IntVec3 loc)
		{
			return true;
		}

		public Designator_ExportShipReOld()
		{
			defaultLabel = "ReSave Ship";
			defaultDesc = "Resave this ship to an XML file with the same name and tags it was imported with. Click anywhere on the map to activate.";
			icon = ContentFinder<Texture2D>.Get("UI/Save_XML");
			soundDragSustain = SoundDefOf.Designate_DragStandard;
			soundDragChanged = SoundDefOf.Designate_DragStandard_Changed;
			useMouseIcon = true;
			soundSucceeded = SoundDefOf.Designate_Deconstruct;
			//disabled = !SoSBuilder.shipDictionary.Keys.Contains(Find.CurrentMap);
		}

		public override void DesignateSingleCell(IntVec3 loc)
		{
			Map m = Find.CurrentMap;
			if (!SoSBuilder.shipDictionary.Keys.Contains(m))
			{
				Messages.Message("Could not resave the ship, info either missing or corrupt.", null, MessageTypeDefOf.NegativeEvent);
				return;
			}
			if (!Find.CurrentMap.Biome.defName.Equals("OuterSpaceBiome"))
			{
				Messages.Message("Not on space map", MessageTypeDefOf.RejectInput);
				return;
			}
			Building_ShipBridge shipCore = (Building_ShipBridge)Find.CurrentMap.spawnedThings.Where(t => t is Building_ShipBridge).FirstOrDefault();
			if(shipCore == null)
			{
				Messages.Message("No ship core found. Build a bridge or AI core.", MessageTypeDefOf.RejectInput);
				return;
			}
			if(shipCore.ShipName==null)
			{
				Messages.Message("Name the ship before saving it", MessageTypeDefOf.RejectInput);
				return;
			}
			int combatPoints = 0;
			int randomTurretPoints = 0;
			int massPoints = 0;
			foreach (Thing b in Find.CurrentMap.spawnedThings.Where(b => b is Building))
			{
				if (b.def != ThingDef.Named("ShipHullTile"))
				{
					massPoints += (b.def.size.x * b.def.size.z) * 3;
					if (b.TryGetComp<CompShipHeat>() != null)
						combatPoints += b.TryGetComp<CompShipHeat>().Props.threat;
					else if (b.def == ThingDef.Named("ShipSpinalAmplifier"))
						combatPoints += 5;
					else if (b.def == ThingDef.Named("ShipPartTurretSmall")) 
					{
						combatPoints += 10;
						randomTurretPoints += 10;
					}
					else if (b.def == ThingDef.Named("ShipPartTurretLarge"))
					{
						combatPoints += 30;
						randomTurretPoints += 30;
					}
					else if (b.def == ThingDef.Named("ShipPartTurretSpinal"))
						combatPoints += 100;
				}
				else if (b.def == ThingDef.Named("ShipHullTile"))
					massPoints += 1;
			}
			combatPoints += massPoints / 100;

			int xCenter = Find.CurrentMap.Size.x / 2;
			int zCenter = Find.CurrentMap.Size.z / 2;
			string path = Path.Combine(GenFilePaths.SaveDataFolderPath, "ExportedShips");
			DirectoryInfo dir = new DirectoryInfo(path);
			if (!dir.Exists)
				dir.Create();
			string filename = Path.Combine(path, shipCore.ShipName + ".xml");

			char charPointer = '?';
			Dictionary<char, ShipShape> symbolTable = new Dictionary<char, ShipShape>();
			Dictionary<ShipShape, char> symbolTableBackwards = new Dictionary<ShipShape, char>();
			List<ShipPosRotShape> shipStructure = new List<ShipPosRotShape>();
			HashSet<IntVec3> mechBugfix = new HashSet<IntVec3>();

			foreach (Thing t in Find.CurrentMap.spawnedThings)
			{
				if (SoSBuilder.ExportToIgnore(t, shipCore))
				{
					continue;
				}
				if (t.TryGetComp<CompSoShipPart>() != null && t.TryGetComp<CompSoShipPart>().Props.mechanoid)
				{
					if (!t.def.building.isEdifice && mechBugfix.Contains(t.Position))
						continue;
					mechBugfix.Add(t.Position);
				}

				ShipShape shape = new ShipShape();
				if (t is Building_ShipRegion r)
				{
					shape.width = r.width;
					shape.height = r.height;
					shape.shapeOrDef = "Cargo";
				}
				else
				{
					shape.shapeOrDef=t.def.defName;
					if (t.def.MadeFromStuff)
					{
						shape.stuff = t.Stuff.defName;
					}
					else if (t.TryGetComp<CompNameMe>() != null)
					{
						shape.stuff = t.TryGetComp<CompNameMe>().pawnKindDef;
					}
					else if (t.TryGetComp<CompShipCombatShield>() != null)
					{
						shape.radius = t.TryGetComp<CompShipCombatShield>().radiusSet;
					}
					if (t.TryGetComp<CompColorable>() != null && t.TryGetComp<CompColorable>().Color != null && t.TryGetComp<CompColorable>().Color != Color.white)
					{
						shape.color = t.TryGetComp<CompColorable>().Color;
					}
				}
				shape.x = t.Position.x - xCenter;
				shape.z = t.Position.z - zCenter;
				shape.rot = t.Rotation;

				if(!symbolTableBackwards.ContainsKey(shape))
				{
					symbolTable.Add(charPointer, shape);
					symbolTableBackwards.Add(shape, charPointer);
					charPointer = (char)(((int)charPointer) + 1);
					if (charPointer == '|')
						charPointer = (char)(((int)charPointer) + 1);
				}
				ShipPosRotShape posrot = new ShipPosRotShape();
				posrot.x = shape.x;
				posrot.z = shape.z;
				posrot.rot = shape.rot;
				posrot.shape = symbolTableBackwards[shape]+"";
				shipStructure.Add(posrot);
			}

			foreach (IntVec3 cell in Find.CurrentMap.AllCells)
			{
				TerrainDef def = Find.CurrentMap.terrainGrid.TerrainAt(cell);
				if (def.defName != "EmptySpace" && def.defName != "FakeFloorInsideShip" && def.defName != "FakeFloorInsideShipMech" && def.defName != "FakeFloorInsideShipArchotech")
				{
					ShipShape shape = new ShipShape();
					shape.shapeOrDef = def.defName;
					shape.x = cell.x;
					shape.z = cell.z;

					if (!symbolTableBackwards.ContainsKey(shape))
					{
						symbolTable.Add(charPointer, shape);
						symbolTableBackwards.Add(shape, charPointer);
						charPointer = (char)(((int)charPointer) + 1);
						if (charPointer == '|')
							charPointer = (char)(((int)charPointer) + 1);
					}
					ShipPosRotShape posrot = new ShipPosRotShape();
					posrot.x = shape.x;
					posrot.z = shape.z;
					posrot.rot = shape.rot;
					posrot.shape = symbolTableBackwards[shape] + "";
					shipStructure.Add(posrot);
				}
			}

			string bigString = "";
			bool isFirst = true;
			foreach (ShipPosRotShape shape in shipStructure)
			{
				if (isFirst)
					isFirst = false;
				else
					bigString += "|";
				bigString += shape.x + "," + shape.z + "," + shape.rot.AsInt + "," + shape.shape;
			}

			SafeSaver.Save(filename, "Defs", () =>
			{
				Scribe.EnterNode("EnemyShipDef");
				EnemyShipDef shipDef = DefDatabase<EnemyShipDef>.GetNamed(SoSBuilder.shipDictionary[m]);
				Scribe_Values.Look<string>(ref shipDef.defName, "defName");
				Scribe_Values.Look<string>(ref shipDef.label, "label");
				Scribe_Values.Look<int>(ref combatPoints, "combatPoints", 0);
				Scribe_Values.Look<int>(ref randomTurretPoints, "randomTurretPoints", 0);
				Scribe_Values.Look<int>(ref shipDef.cargoValue, "cargoValue", 0);
				Scribe_Values.Look<bool>(ref shipDef.neverRandom, "neverRandom");
				Scribe_Values.Look<bool>(ref shipDef.neverAttacks, "neverAttacks");
				Scribe_Values.Look<bool>(ref shipDef.startingShip, "startingShip");
				Scribe_Values.Look<bool>(ref shipDef.startingDungeon, "startingDungeon");
				Scribe_Values.Look<bool>(ref shipDef.spaceSite, "spaceSite");
				Scribe_Values.Look<bool>(ref shipDef.tradeShip, "tradeShip");
				Scribe_Values.Look<bool>(ref shipDef.navyExclusive, "navyExclusive");
				Scribe_Values.Look<bool>(ref shipDef.customPaintjob, "customPaintjob");
				//Scribe_Values.Look<bool>(ref shipDef.fighterShip, "fighterShip");
				//Scribe_Values.Look<bool>(ref shipDef.carrierShip, "carrierShip");
				Scribe.EnterNode("core");
				Scribe_Values.Look<string>(ref shipCore.def.defName, "shapeOrDef");
				int cx = shipCore.Position.x - xCenter;
				Scribe_Values.Look<int>(ref cx, "x");
				int cz = shipCore.Position.z - zCenter;
				Scribe_Values.Look<int>(ref cz, "z");
				Rot4 crot = shipCore.Rotation;
				Scribe_Values.Look<Rot4>(ref crot, "rot");
				Scribe.ExitNode();
				Scribe.EnterNode("symbolTable");
				foreach (char key in symbolTable.Keys)
				{
					Scribe.EnterNode("li");
					char realKey = key;
					Scribe_Values.Look<char>(ref realKey, "key"); ;
					ShipShape realShape = symbolTable[key];
					Scribe_Deep.Look<ShipShape>(ref realShape, "value");
					Scribe.ExitNode();
				}
				Scribe.ExitNode();
				Scribe_Values.Look<string>(ref bigString, "bigString");
				Scribe.ExitNode();
			});
			Messages.Message("Resaved ship as: " + shipCore.ShipName + ".xml", shipCore, MessageTypeDefOf.PositiveEvent);
		}
	}
}
*/
