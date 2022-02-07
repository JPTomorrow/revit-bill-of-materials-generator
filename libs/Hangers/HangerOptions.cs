namespace JPMorrow.Revit.Hangers
{
    public class HangerOptions
	{
		public double NominalSpacing { get; set; } = 8;
		public double BendSpacing { get; set; } = 3;
		public double RodDiameter { get; set; } = 0;
		public double MaxStrutGapSpan { get; set; } = 3;
		public double InsideRodGap { get; set; } = 0;
		public double OutsideRodExtraLength { get; set; } = 0;
		public double MinRodLength { get; set; } = 0;
		public string SingleAttType { get; set; } = HangerConstants.SingleHangerAttachmentTypes[0];
		public bool CeilingHangers { get; set; } = false;
		public bool DrawSingleHangerModelGeometry { get; set; } = false;
		public bool DrawStrutHangerModelGeometry { get; set; } = false;

		public HangerOptions(HangerOptions other)
		{
            NominalSpacing = other.NominalSpacing;
            BendSpacing = other.BendSpacing;
            RodDiameter = other.RodDiameter;
            MaxStrutGapSpan = other.MaxStrutGapSpan;
            InsideRodGap = other.InsideRodGap;
            OutsideRodExtraLength = other.OutsideRodExtraLength;
            MinRodLength = other.MinRodLength;
            SingleAttType = other.SingleAttType;
            CeilingHangers = other.CeilingHangers;
            DrawSingleHangerModelGeometry = other.DrawSingleHangerModelGeometry;
            DrawStrutHangerModelGeometry = other.DrawStrutHangerModelGeometry;
        }

        public HangerOptions() { }
    }
}
