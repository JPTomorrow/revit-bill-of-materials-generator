
using System.Collections.Generic;
using JPMorrow.Revit.Documents;
using JPMorrow.Revit.Measurements;
using MoreLinq;
using System.Linq;
using System.Runtime.Serialization;
using JPMorrow.Revit.Wires;
using JPMorrow.Revit.BOMPackage;

namespace JPMorrow.Revit.VoltageDrop {

    [DataContract]
    public class VoltageDropRule {

        private VoltageDropRule(
            double min_dist, double max_dist,
            string wire_size, string voltage) {

            MinDistance = min_dist;
            MaxDistance = max_dist;
            WireSize = wire_size;
            Voltage = voltage;
        }

        [DataMember]
        public double MinDistance { get; private set; }
        [DataMember]
        public double MaxDistance { get; private set; }
        [DataMember]
        public string WireSize { get; private set; }
        [DataMember]
        public string Voltage { get; private set; }

        public bool IsInRange(double d) => d > MinDistance && d < MaxDistance;
        
        // get a human readable text representation of a voltage drop rule
        public string ToString(ModelInfo info) {
            var mind = RMeasure.LengthFromDbl(info.DOC, MinDistance);
            var maxd = RMeasure.LengthFromDbl(info.DOC, MaxDistance);
            return string.Format("{0} < {1} < {2}", mind, WireSize, maxd);
        }

        // create a voltage drop rule given the user input
        public static VoltageDropRule DeclareRule(
            double min_distance, double max_distance,
            string wire_size, string voltage) {

            return new VoltageDropRule(min_distance, max_distance, wire_size, voltage);
        }
    }

    // all of the static functions for Voltage Drop
    public static class VoltageDrop {

        // drop the voltage of all the wire in a MasterDataPackage
        public static MasterDataPackage AllWireDropVoltage(MasterDataPackage old_package) {

            var package  = new MasterDataPackage(old_package);
            var old_man = package.WireManager;
            WireManager new_man = new WireManager(new List<HashedWire>());
            
            foreach(var run in package.Cris) {

                var wires = old_man.GetWires(run.WireIds).ToList();
                var ground_wires = wires.Where(x => WireColor.Ground_Colors.Any(y => y.Equals(x.Color))).ToList();
                ground_wires.ForEach(x => wires.Remove(x));
                string largest_size = null;
                bool changed = false;
                
                foreach(var wire in wires) {
                    
                    var size = wire.Size;
                    if(Wire.LowVoltageWireNames.Any(x => size.Equals(x))) {
                        new_man.AddWire(run.WireIds, wire);
                        continue;
                    }

                    var s = wire.GetPanelVoltage(out string voltage);
                    
                    if(s) {
                        s = package.VoltageDropRules.GetRuleByVoltageAndLength(voltage, run.Length + old_package.WireMakeupLength, out var rule);
                        
                        if(s) {
                            var pass = Wire.IsWireSizeGreaterThan(size, rule.WireSize);
                            if(pass) {
                                size = rule.WireSize;
                                changed = true;
                                
                                if(largest_size == null || Wire.IsWireSizeGreaterThan(largest_size, size)) {
                                    largest_size = size;
                                }
                            }
                        }
                    }
                    Wire new_wire = new Wire(wire.CircuitNumber, size, wire.Color, wire.WireType, wire.WireMaterialType);
                    new_man.AddWire(run.WireIds, new_wire);
                }

                if(changed) {
                    
                    foreach(var wire in ground_wires) {

                        var s = Wire.WireSizeToBreakerSize
                            .TryGetValue(largest_size, out int breaker_amps);
                        
                        if(!s) {
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
                else {
                    new_man.AddWires(run.WireIds, ground_wires);
                }
            }
            
            package.WireManager.Clear();
            foreach(var run in package.Cris) {
                package.WireManager.AddWires(run.WireIds, new_man.GetWires(run.WireIds));
            }

            return package;
        }
    }

    public static class VoltageDropEXT {

        public static IEnumerable<VoltageDropRule> OrderByPrecedence(this List<VoltageDropRule> source) {

            return source.OrderBy(x => x.MinDistance).ToList();
        }

        public static bool GetRuleByVoltageAndLength(
            this List<VoltageDropRule> source,
            string panel_voltage, double length,
            out VoltageDropRule base_rule) {

            var idx = source.FindIndex(x => x.Voltage.Equals(panel_voltage) && x.IsInRange(length));

            if(idx == -1) {
                base_rule = null;
                return false;
            }
            else {
                base_rule = source[idx];
                return true;
            }
        }


    }
}
