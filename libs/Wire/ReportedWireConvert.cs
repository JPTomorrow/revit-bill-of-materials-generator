using System.Collections.Generic;
using System.Linq;

namespace JPMorrow.Revit.Wires
{
    public class ReportedWireCollection
    {
        private List<Wire> WireCollection { get; set; }
        public Wire[] Wires { get => WireCollection.ToArray(); }

        public ReportedWireCollection(IEnumerable<Wire> wires)
        {
            WireCollection = wires.ToList();
        }

        public ReportedWireCollection()
        {
            WireCollection = new List<Wire>();
        }

        public void AddWire(Wire wire)
        {
            WireCollection.Add(wire);
        }

        public void AddWires(IEnumerable<Wire> wires)
        {
            WireCollection.AddRange(wires);
        }
    }

    public class ReportableWireSizes
    {
        private List<string> RepWireSizes { get; set; }
        public IEnumerable<string> WireSizes { get => RepWireSizes; }

        public ReportableWireSizes(IEnumerable<string> dirty_wire_sizes)
        {
            // trim bad charaters out of the wire sizes for checking
            var wire_sizes = dirty_wire_sizes.ToArray();
            for (var i = 0; i < wire_sizes.Count(); ++i)
            {
                if(wire_sizes[i].Contains("MCM"))
                    wire_sizes[i] = wire_sizes[i].Remove(wire_sizes[i].IndexOf('M'), 3);

                if(wire_sizes[i].Contains("#"))
                    wire_sizes[i] = wire_sizes[i].Remove(wire_sizes[i].IndexOf('#'), 1);

                RepWireSizes.Add(wire_sizes[i]);
            }
        }
    }

    public class RawReportedWire
    {
        public int NumberOfHots { get; private set; } = 0;
        public string HotSize { get; private set; } = string.Empty;
        public string HotMaterialType { get; private set; } = string.Empty;

        public string GroundSize { get; private set; } = string.Empty;
        public string GroundMaterialType { get; private set; } = string.Empty; 

        public string PanelVoltage { get; private set; }

        public RawReportedWire(string reported_wire, string panel_voltage)
        {
            PanelVoltage = panel_voltage;
            var wire_props = reported_wire.ToUpper().Split(' ').Select(x => x.Trim());
            
            
        }

        public bool TryParseWires(out List<Wire> wires)
        {
            wires = new List<Wire>();

            if(NumberOfHots < 1)
                return false;

            List<string> wire_colors = new List<string>();
            if(PanelVoltage.Equals(Wire.PanelVoltages.First()))
            {
                if(NumberOfHots > 0) wire_colors.Add(WireColor.Red);
                if(NumberOfHots > 1) wire_colors.Add(WireColor.Blue);
                if(NumberOfHots > 2) wire_colors.Add(WireColor.Black);

                wire_colors.Add(WireColor.White);
            }
            else
            {

            }
            

            return false;
        }
    }

    public static class ReportedWireConverter
    {
        public static ReportedWireCollection GetWireFromReportedWires(
            ReportableWireSizes wire_sizes, string reported_wire, string panel_voltage)
        {
            var chk_sizes = wire_sizes.WireSizes;

            // split up reported wire size and parse
            List<RawReportedWire> wire_grps = new List<RawReportedWire>();

            foreach(var s in reported_wire.Split('&'))
            {
                RawReportedWire rep_wire = new RawReportedWire(s, panel_voltage);
                wire_grps.Add(rep_wire);
            }

            ReportedWireCollection collection = new ReportedWireCollection();
            foreach(var grp in wire_grps)
            {
                bool s = grp.TryParseWires(out var wires);
                if(s) collection.AddWires(wires);
            }

            return collection;
        }
    }
}