using System.IO;
using System.Runtime.Serialization;
using JPMorrow.Revit.Documents;
using JPMorrow.Tools.Data;

namespace JPMorrow.Revit.Hangers
{
    [DataContract]
	public class HangerOptions
	{
        [DataMember]
		public double NominalSpacing { get; set; } = 8;
        [DataMember]
		public double BendSpacing { get; set; } = 3;
		[DataMember]
		public double RodDiameter { get; set; } = 0;
		[DataMember]
		public double MaxStrutGapSpan { get; set; } = 3;
		[DataMember]
		public double InsideRodGap { get; set; } = 0;
		[DataMember]
		public double OutsideRodExtraLength { get; set; } = 0;
		[DataMember]
		public double MinRodLength { get; set; } = 0;
		[DataMember]
		public string SingleAttType { get; set; } = HangerConstants.SingleHangerAttachmentTypes[0];
        [DataMember]
		public bool CeilingHangers { get; set; } = false;
		[DataMember]
		public bool DrawRayLines { get; set; } = false;

		private static readonly string hanger_opt_path = ModelInfo.GetDataDirectory("hanger_opts", true);
		private static readonly string file_name = "hanger_opts.json";
       
		public static HangerOptions Load(ModelInfo info)
		{
			if (!File.Exists(hanger_opt_path + file_name))
			{
				if (!Directory.Exists(hanger_opt_path))
					Directory.CreateDirectory(hanger_opt_path);

				var new_opts = new HangerOptions();
				new_opts.Save();
			}
			HangerOptions o = JSON_Serialization.DeserializeFromFile<HangerOptions>(hanger_opt_path + file_name);
			return o;
		}

		public void Save()
		{
			JSON_Serialization.SerializeToFile<HangerOptions>(this, hanger_opt_path + file_name);
		}
	}
}
