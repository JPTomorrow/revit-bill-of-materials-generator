using JPMorrow.Tools.Data;
using System.IO;
using System.Runtime.Serialization;
using JPMorrow.Revit.Documents;
using MainApp;
using JPMorrow.Revit.Wires;
using JPMorrow.Tools.Diagnostics;

namespace JPMorrow.Revit.WirePackage
{
	[DataContract]
	public class  WirePackageSettings
	{
		[DataMember]
		public string CouplingType { get; set; }

		[DataMember]
		public bool StaggeredCurcuits { get; set; }

		[DataMember]
		public bool Fire_Alarm_Mode { get; set; }

		[DataMember]
		public bool Include_JBox_Assemblies { get; set; }

		[DataMember]
		public string Panel_Voltage { get; set; }

		private static readonly string settings_path = ModelInfo.GetDataDirectory("wire_package", true);

		private static readonly string file_name = "WirePackageSettings.json";

		public static string[] Coupling_Mat_Types { get; } = new string[] {
			"Set Screw Steel",
			"Set Screw Diecast",
			"Compression Diecast",
			"Compression Stainless Steel",
			"Compression Steel",
		};

		public WirePackageSettings(
			string coupling_mat_type, bool staggered, bool fire_alarm_mode,
			bool jbox_assemblies, string panel_voltage)
		{
			CouplingType = coupling_mat_type;
			StaggeredCurcuits = staggered;
			Fire_Alarm_Mode = fire_alarm_mode;
			Include_JBox_Assemblies = jbox_assemblies;
			Panel_Voltage = panel_voltage;
		}

		public static WirePackageSettings Load()
		{
			WirePackageSettings pack;
			if(File.Exists(settings_path + file_name))
			{
				pack = JSON_Serialization.DeserializeFromFile<WirePackageSettings>(settings_path + file_name);
			}
			else
			{
				if (!Directory.Exists(settings_path))
					Directory.CreateDirectory(settings_path);

				var new_pack = new WirePackageSettings(
					Coupling_Mat_Types[0], true, false, true, Wire.PanelVoltages[0]);
				Save(new_pack);
				pack = new_pack;
			}

			return pack;
		}


		public static void Save(WirePackageSettings settings)
		{
			JSON_Serialization.SerializeToFile<WirePackageSettings>(settings, settings_path + file_name);
		}
	}
}
