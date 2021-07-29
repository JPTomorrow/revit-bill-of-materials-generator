using System.Runtime.Serialization;

namespace JPMorrow.Revit.Hardware
{
	[DataContract]
	public struct HardwareEntry
	{
		[DataMember]
		public string name;
		[DataMember]
		public int qty;
		[DataMember]
		public double per_unit_labor;
		[DataMember]
		public char unit_labor_code;
	}
}