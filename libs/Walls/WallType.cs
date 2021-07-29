using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using JPMorrow.Revit.Documents;
using JPMorrow.Tools.Diagnostics;

namespace JPMorrow.Revit.Custom.WallInspection
{
	/// <summary>
	/// Get all information about a wall
	/// </summary>
	public class WallInfo
	{
		public Wall WallInstance { get; }
		public Document RefDocument { get; }
		public string TypeName { get => Type.Name; }
		public string Kind { get => Enum.GetName(typeof(WallKind), Type.Kind); }
		public WallType Type { get => WallInstance.WallType; }
		public List<CompoundStructureLayer> StructuralLayers { get => Type.GetCompoundStructure().GetLayers().ToList(); }
		public List<Material> Materials  { get => WallInstance.GetMaterialIds(false).ToList().Select(x => RefDocument.GetElement(x) as Material).ToList(); }

		public class WallNameWallTypePair {
			public string WallName { get; set; }
			public CustomWallType WallType { get; set; }

			public WallNameWallTypePair(string wall_name, CustomWallType custom_type) {
				WallName = wall_name;
				WallType = custom_type;
			}
		}

		public static List<WallNameWallTypePair> WallTypes { get; }  = new List<WallNameWallTypePair>() {
				new WallNameWallTypePair( "Block Wall", CustomWallType.BlockWall ),
				new WallNameWallTypePair( "Dry Wall With Metal Stud", CustomWallType.DryWallMetalStud ),
				new WallNameWallTypePair( "Solid Concrete", CustomWallType.SolidConcrete ),
			};

		public CustomWallType DerivedWallType { get {

			var name_spl = TypeName.Split(' ').SelectMany(x => x.Split('-')).ToList();

			var layers_spl = this.Materials.SelectMany(x => x.Name.Split(' ')).SelectMany(y => y.Split('-')).ToList();

			var score_strs = new List<string>();

			foreach(var s in name_spl.Concat(layers_spl))
				if(WallTypeResolution.PossibleWallTypeWordMatches.Any(x => x.ToLower().Equals(s.ToLower())))
					score_strs.Add(s.ToLower());

			CustomWallType type = CustomWallType.Default;
			int word_cnt = 0;
			foreach(var t in WallTypeResolution.WallTypeResolveDict)
			{
				int temp_word_cnt = 0;
				foreach(var s in score_strs)
				{
					if(t.MaterialWords.Contains(s))
						temp_word_cnt++;
				}

				if(temp_word_cnt > word_cnt)
				{
					type = t.WallType;
					word_cnt = temp_word_cnt;
				}
			}

			return type;
		} }

		public static CustomWallType GetCustomWallTypeFromName(string name)
		{
			if(!WallTypes.Any( x => x.WallName.Equals(name))) return CustomWallType.Default;
			return WallTypes.Find(x => x.WallName.Equals(name)).WallType;
		}

		public WallInfo(Wall wall, Document ref_doc)
		{
			WallInstance = wall;
			RefDocument = ref_doc;
		}

		public override string ToString()
		{
			return 	"--------------------\n" +
					"TypeName: " + TypeName + "\n" +
					"Kind: " + Kind + "\n" +
					"Materials:\n" + String.Join("\n", Materials.Select(x => x.Name));
		}

		private static class WallTypeResolution
		{

			// relevant words coming off of the wall in revit
			public static List<string> PossibleWallTypeWordMatches { get; } = new List<string>() {
				"brk",
				"brick",
				"sheathing",
				"concrete",
				"metal",
				"stud",
				"stone",
				"limestone",
				"insulation",
				"gwb",
				"gypsum",
			};

			private static List<string> p { get => PossibleWallTypeWordMatches; }

			public static List<MaterialWordsWallTypePair> WallTypeResolveDict { get; } = new List<MaterialWordsWallTypePair>() {
				new MaterialWordsWallTypePair( new string[] { "brk", "brick" } , 					CustomWallType.BlockWall ),
				new MaterialWordsWallTypePair( new string[] { "gwb", "gypsum", "metal", "stud" }, 	CustomWallType.DryWallMetalStud ),
				new MaterialWordsWallTypePair( new string[] { "concrete", "brick" }, 				CustomWallType.SolidConcrete )
			};

			public static CustomWallType DefaultWallType { get => CustomWallType.DryWallMetalStud; }
		}
	}

	public class MaterialWordsWallTypePair {
		public string[] MaterialWords { get; set; }
		public CustomWallType WallType { get; set; }

		public MaterialWordsWallTypePair(string[] mat_words, CustomWallType custom_type) {
			MaterialWords = mat_words;
			WallType = custom_type;
		}
	}

	public enum CustomWallType
	{
		Default,
		BlockWall,
		DryWallMetalStud,
		SolidConcrete,
	};

	public static class WallInspector
	{
		public static WallType GetHostedWallType(Element el)
		{
			FamilyInstance fam = el as FamilyInstance;
			if(fam == null) return null;

			var host = fam.Host as Wall;
			if(host == null) return null;

			return host.WallType;
		}

		public static Wall GetHostedWall(Element el)
		{
			FamilyInstance fam = el as FamilyInstance;
			if(fam == null) return null;

			var host = fam.Host as Wall;
			if(host == null) return null;

			return host as Wall;
		}

		public static Wall GetRevitLinkHostedWall(ModelInfo info, Element el, out Document link_doc)
		{
			link_doc = null;

			FamilyInstance fam = el as FamilyInstance;
			if(fam == null) return null;

			List<Document> links = new List<Document>();

			FilteredElementCollector link_coll = new FilteredElementCollector(info.DOC);
			var link_ids = link_coll.OfCategory(BuiltInCategory.OST_RvtLinks).ToElementIds();

			link_ids.ToList().ForEach(x => {
					RevitLinkInstance rli = info.DOC.GetElement(x) as RevitLinkInstance;
					if(rli != null) links.Add(rli.GetLinkDocument());
			});

			Element wall_el = null;
			foreach(var d in links)
			{
				if(fam == null || fam.HostFace == null) continue;
				if(fam.HostFace.LinkedElementId.IntegerValue < 0) continue;
				wall_el = d.GetElement(fam.HostFace.LinkedElementId);
				if(wall_el == null) continue;
				if(wall_el != null)
				{
					link_doc = d;
					break;
				}
			}

			if(wall_el == null) return null;
			return wall_el as Wall;
		}
	}
}