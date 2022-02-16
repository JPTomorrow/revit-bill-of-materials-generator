
using System.Collections.Generic;
using JPMorrow.Revit.Documents;
using JPMorrow.Revit.Measurements;
using MoreLinq;
using System.Linq;
using System.Runtime.Serialization;
using JPMorrow.Revit.Wires;
using JPMorrow.Revit.BOMPackage;

namespace JPMorrow.Revit.VoltageDrop
{

    [DataContract]
    public class VoltageDropRule
    {

        private VoltageDropRule(
            double longer_than_dist, string from_wire_size, string to_wire_size, string voltage)
        {
            LongerThanDistance = longer_than_dist;
            FromWireSize = from_wire_size;
            ToWireSize = to_wire_size;
            Voltage = voltage;
        }

        [DataMember]
        public double LongerThanDistance { get; private set; }
        [DataMember]
        public string FromWireSize { get; private set; }
        [DataMember]
        public string ToWireSize { get; private set; }
        [DataMember]
        public string Voltage { get; private set; }

        public bool IsInRange(double wire_length) => wire_length > LongerThanDistance;

        // get a human readable text representation of a voltage drop rule
        public string ToString(ModelInfo info)
        {
            var ltd = RMeasure.LengthFromDbl(info.DOC, LongerThanDistance);
            return string.Format("{0} to {1} when length > {2}", FromWireSize, ToWireSize, ltd);
        }

        // create a voltage drop rule given the user input
        public static VoltageDropRule DeclareRule(
            double longer_than_dist,
            string from_wire_size, string to_wire_size,
            string voltage)
        {

            return new VoltageDropRule(longer_than_dist, from_wire_size, to_wire_size, voltage);
        }
    }

    // all of the static functions for Voltage Drop
    public static class VoltageDrop
    {

        // drop the voltage of all the wire in a MasterDataPackage
        public static MasterDataPackage AllWireDropVoltage(MasterDataPackage old_package, out string changed_wires)
        {
            changed_wires = "";
            string temp_changed = "";
            void log_changed_wire(Wire wire)
            {
                temp_changed += "Changed: " + wire.ToString() + "\n";
            }

            var package = new MasterDataPackage(old_package);
            var old_man = package.GetSelectedConduitPackage().WireManager;
            WireManager new_man = new WireManager(new List<HashedWire>());

            foreach (var run in package.GetSelectedConduitPackage().Cris)
            {

                var run_length = run.Length + old_package.GetSelectedGlobalSettingsPackage().WireMakeupLength;
                var wires = old_man.GetWires(run.WireIds).ToList();
                var ground_wires = wires.Where(x => WireColor.Ground_Colors.Any(y => y.Equals(x.Color))).ToList();
                ground_wires.ForEach(x => wires.Remove(x));
                string largest_size = null;
                bool changed = false;

                foreach (var wire in wires)
                {

                    var size = wire.Size;
                    var old_size = wire.Size;

                    // dont parse if wire size is low voltage special
                    if (Wire.LowVoltageWireNames.Any(x => size.Equals(x)))
                    {
                        new_man.AddWire(run.WireIds, wire);
                        continue;
                    }

                    if (wire.GetPanelVoltage(out string voltage) &&
                        package.VoltageDropRules.GetRuleByVoltageAndLength(voltage, run_length, out var rule) &&
                        size.Equals(rule.FromWireSize))
                    {
                        // if the wire is larger than the rule, drop the 
                        // wire size and update the largest size
                        size = rule.ToWireSize;
                        changed = true;

                        if (largest_size == null || Wire.IsWireSizeGreaterThan(largest_size, size))
                        {
                            largest_size = size;
                        }
                    }

                    // add the wire to the new wire manager after parsing size
                    Wire new_wire = new Wire(wire.CircuitNumber, size, wire.Color, wire.WireType, wire.WireMaterialType);
                    if (!new_wire.Size.Equals(old_size)) log_changed_wire(new_wire);
                    new_man.AddWire(run.WireIds, new_wire);
                }

                // @TODO: cleanup and document
                if (changed)
                {
                    foreach (var wire in ground_wires)
                    {
                        var s = Wire.WireSizeToBreakerSize
                            .TryGetValue(largest_size, out int breaker_amps);

                        if (!s)
                        {
                            new_man.AddWire(run.WireIds, wire);
                            continue;
                        }

                        var idx = Wire.BreakerSizeToProportionalGroundWireSize.Keys.ToList().BinarySearch(breaker_amps);
                        string grd_wire_size = idx < 0 ?
                            Wire.BreakerSizeToProportionalGroundWireSize.Values.ToList()[~idx] :
                            Wire.BreakerSizeToProportionalGroundWireSize.Values.ToList()[idx];

                        Wire new_wire = new Wire(wire.CircuitNumber, grd_wire_size, wire.Color, wire.WireType, wire.WireMaterialType);
                        new_man.AddWire(run.WireIds, new_wire);
                    }
                }
                else
                {
                    new_man.AddWires(run.WireIds, ground_wires);
                }
            }

            package.GetSelectedConduitPackage().WireManager.Clear();
            foreach (var run in package.GetSelectedConduitPackage().Cris)
            {
                package.GetSelectedConduitPackage().WireManager.AddWires(run.WireIds, new_man.GetWires(run.WireIds));
            }

            changed_wires = temp_changed;
            return package;
        }
    }

    public static class VoltageDropEXT
    {

        public static IEnumerable<VoltageDropRule> OrderByPrecedence(this List<VoltageDropRule> source)
        {
            return source.OrderBy(x => x.LongerThanDistance).ToList();
        }

        public static bool GetRuleByVoltageAndLength(
            this List<VoltageDropRule> source,
            string panel_voltage, double length,
            out VoltageDropRule base_rule)
        {

            var idx = source.FindIndex(x => x.Voltage.Equals(panel_voltage) && x.IsInRange(length));

            if (idx == -1)
            {
                base_rule = null;
                return false;
            }
            else
            {
                base_rule = source[idx];
                return true;
            }
        }


    }
}
