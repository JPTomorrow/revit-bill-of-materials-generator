/*

    // steps to calculate 
    Step #1:  Determine Maximum Allowed Uniform Load from Load Table
    Step #2:  Multiple by the Applicable Pierced Hole Factor (if using a Beam Load Table for the solid channel)
        0.95 for “KO”
        0.90 for “HS” & “H3”
        0.85 for "T", “SL” & “WT”
        0.70 for “DS”
    Step #3:  Multiply by the Unbraced Length Factor
    Step #4:  Subtract Channel Weight
    Step #5:  Multiply by 50% for Midpsan Loading  (if Applicable)

    NOTE:   Loads in the Beam Load Tables for UNISTRUT® metal framing channel are given as a 
            total uniform load (W) in pounds. For the more familiar uniform load (w) in pounds per foot 
            or pounds per inch, divide the table load by the span. 

    NOTE:   Loads under the column headings of “Span / 180”, “Span / 240” and “Span / 360” are provided for 
            installations in which deflection (sag) of the loaded UNISTRUT® channel must be limited. These ratios 
            are standard engineering practice and, when applicable, are usually given by the Professional of Record 
            or the Project Specifications. Actual deflection from these preset ratios equals the span (inches or feet) 
            divided by the number 180, 240 or 360. When designing to one of these deflection limits, the allowed 
            uniform load is generally less than the values under the column heading “Maximum Allowed Uniform Load”. 
            For further information or assistance on this issue, contact us.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using JPMorrow.Revit.Measurements;
using JPMorrow.Revit.Wires;
using Autodesk.Revit.DB;
using JPMorrow.Tools.Diagnostics;

namespace JPMorrow.Revit.ConduitRuns
{
    public static class LoadCalcData
    {
        public static Dictionary<string[], double> ConduitToWeight = new Dictionary<string[], double>(new StringArrayComparer())
        {
            { new[] {"Rigid Nonmetallic Conduit (RNC Sch 40)", "1/2\""} ,   .164  },
            { new[] {"Rigid Nonmetallic Conduit (RNC Sch 40)", "3/4\""} ,   .218  },
            { new[] {"Rigid Nonmetallic Conduit (RNC Sch 40)", "1\""} ,     .321  },
            { new[] {"Rigid Nonmetallic Conduit (RNC Sch 40)", "1 1/4\""} , .434 },
            { new[] {"Rigid Nonmetallic Conduit (RNC Sch 40)", "1 1/2\""} , .518 },
            { new[] {"Rigid Nonmetallic Conduit (RNC Sch 40)", "2\""} ,     .695 },
            { new[] {"Rigid Nonmetallic Conduit (RNC Sch 40)", "2 1/2\""} , 1.096 },
            { new[] {"Rigid Nonmetallic Conduit (RNC Sch 40)", "3\""} ,     1.435 },
            { new[] {"Rigid Nonmetallic Conduit (RNC Sch 40)", "3 1/2\""} , 1.729 },
            { new[] {"Rigid Nonmetallic Conduit (RNC Sch 40)", "4\""} ,     2.043 },

            { new[] {"Electrical Metallic Tubing (EMT)", "1/2\""} ,         30.0 / 100.0  },
            { new[] {"Electrical Metallic Tubing (EMT)", "3/4\""} ,         46.0 / 100.0  },
            { new[] {"Electrical Metallic Tubing (EMT)", "1\""} ,           67.0 / 100.0  },
            { new[] {"Electrical Metallic Tubing (EMT)", "1 1/4\""} ,       101.0 / 100.0 },
            { new[] {"Electrical Metallic Tubing (EMT)", "1 1/2\""} ,       116.0 / 100.0 },
            { new[] {"Electrical Metallic Tubing (EMT)", "2\""} ,           148.0 / 100.0 },
            { new[] {"Electrical Metallic Tubing (EMT)", "2 1/2\""} ,       216.0 / 100.0 },
            { new[] {"Electrical Metallic Tubing (EMT)", "3\""} ,           263.0 / 100.0 },
            { new[] {"Electrical Metallic Tubing (EMT)", "3 1/2\""} ,       349.0 / 100.0 },
            { new[] {"Electrical Metallic Tubing (EMT)", "4\""} ,           393.0 / 100.0 },
        };

        public static Dictionary<string, double> WireToWeight = new Dictionary<string, double>()
        {
            {"#14",     16 / 1000   },
            {"#12",     24 / 1000   },
            {"#10",     37 / 1000   },
            {"#8",      64 / 1000	},
            {"#6",      96 / 1000	},
            {"#4",      155 / 1000	},
            {"#3",      190 / 1000	},
            {"#2",      235 / 1000	},
            {"#1",      300 / 1000	},
            {"#1/0",    370 / 1000   },
            {"#2/0",    465 / 1000   },
            {"#3/0",    570 / 1000   },
            {"#4/0",    720 / 1000   },
            {"250MCM",  850 / 1000   },
            {"300MCM",  1011 / 1000   },
            {"350MCM",  1172 / 1000   },
            {"400MCM",  1333 / 1000   },
            {"500MCM",  1653 / 1000   },
            {"600MCM",  1985 / 1000   },
            {"700MCM",  2100 / 1000   },
            {"750MCM",  2462 / 1000   },
            {"800MCM",  2700 / 1000   },
            {"900MCM",  3000 / 1000   },
            {"1000MCM", 3300 / 1000   },
            {"1250MCM", 3600 / 1000   },
            {"1500MCM", 3900 / 1000   },
            {"1750MCM", 4200 / 1000   },
            {"2000MCM", 4500 / 1000   },
        };

        public static List<double> StandardizedUnistrutSizes = new List<double>()
        {
            1.0, 1.5, 2.0, 2.5, 3.0, 3.5, 4.0, 4.5, 5.0
        };

        // Uniform Load and Deflection { Max Load, Deflection Length, Load at deflection 1/240, load at deflection 1/360 }
        public static bool GetB22SlottedUnistrut_ULD(Document doc, double key, out double[] d)
        {
            d = null;
            var dict = new Dictionary<string, double[]>()
            {
                { RMeasure.LengthDbl(doc, "12\"").ToString(),  new double[] { 2610, RMeasure.LengthDbl(doc, ".014\""), 2610, 2610 }},
                { RMeasure.LengthDbl(doc, "18\"").ToString(),  new double[] { 2269, RMeasure.LengthDbl(doc, ".031\""), 2269, 2269 }},
                { RMeasure.LengthDbl(doc, "24\"").ToString(),  new double[] { 1702, RMeasure.LengthDbl(doc, ".056\""), 1702, 1702 }},
                { RMeasure.LengthDbl(doc, "30\"").ToString(),  new double[] { 1361, RMeasure.LengthDbl(doc, ".087\""), 1361, 1294 }},
                { RMeasure.LengthDbl(doc, "36\"").ToString(),  new double[] { 1135, RMeasure.LengthDbl(doc, ".126\""), 1135, 889 }},
                { RMeasure.LengthDbl(doc, "42\"").ToString(),  new double[] { 972, RMeasure.LengthDbl(doc, ".172\""), 972, 660 }},
                { RMeasure.LengthDbl(doc, "48\"").ToString(),  new double[] { 851, RMeasure.LengthDbl(doc, ".224\""), 758, 505 }},
                { RMeasure.LengthDbl(doc, "54\"").ToString(),  new double[] { 756, RMeasure.LengthDbl(doc, ".284\""), 599, 399 }},
                { RMeasure.LengthDbl(doc, "60\"").ToString(),  new double[] { 681, RMeasure.LengthDbl(doc, ".351\""), 485, 323 }},
                { RMeasure.LengthDbl(doc, "66\"").ToString(),  new double[] { 619, RMeasure.LengthDbl(doc, ".424\""), 401, 267 }},
                { RMeasure.LengthDbl(doc, "72\"").ToString(),  new double[] { 567, RMeasure.LengthDbl(doc, ".505\""), 337, 225 }},
                { RMeasure.LengthDbl(doc, "78\"").ToString(),  new double[] { 524, RMeasure.LengthDbl(doc, ".593\""), 287, 191 }},
                { RMeasure.LengthDbl(doc, "84\"").ToString(),  new double[] { 486, RMeasure.LengthDbl(doc, ".687\""), 248, 165 }},
                { RMeasure.LengthDbl(doc, "90\"").ToString(),  new double[] { 454, RMeasure.LengthDbl(doc, ".789\""), 216, 144 }},
                { RMeasure.LengthDbl(doc, "96\"").ToString(),  new double[] { 425, RMeasure.LengthDbl(doc, ".898\""), 190, 126 }},
                { RMeasure.LengthDbl(doc, "102\"").ToString(), new double[] { 400, RMeasure.LengthDbl(doc, "1.013\""), 168, 112 }},
                { RMeasure.LengthDbl(doc, "108\"").ToString(), new double[] { 378, RMeasure.LengthDbl(doc, "1.136\""), 150, 100 }},
                { RMeasure.LengthDbl(doc, "114\"").ToString(), new double[] { 358, RMeasure.LengthDbl(doc, "1.266\""), 134, 90 }},
                { RMeasure.LengthDbl(doc, "120\"").ToString(), new double[] { 340, RMeasure.LengthDbl(doc, "1.403\""), 121, 81 }},
            };

            bool s = dict.TryGetValue(key.ToString(), out d);
            if(!s) return false;
            return true;
        }

        // { mid point load, mid point deflection }
        public static double[] GetConcetratedMidPointLoadCapacity(double uniform_load, double uniform_deflection)
        {
            return new double[] { uniform_load * 0.5, uniform_deflection * 0.8 };
        }
    }

    public class ConduitLoadCalcResult
    {
        private List<(double Length, double Diameter, double ConduitWeight, double WireWeight)> _conduit_info_pairs = new List<(double Length, double Diameter, double ConduitWeight, double WireWeight)>();
        public IList<(double Length, double Diameter, double ConduitWeight, double WireWeight)> ConduitInfoPairs { get => _conduit_info_pairs; }

        public List<(string MaterialType, string Diameter)> FailedConduitRuns { get; set; } = new List<(string MaterialType, string Diameter)>();
        public List<string> FailedWires { get; set; } = new List<string>();
        public bool HasFailedEntries { get => FailedConduitRuns.Any() || FailedWires.Any(); }

        public double StandardStrutLength { get; private set; } = 1.0;

        public double TotalConduitLength { get => _conduit_info_pairs.Select(x => x.Length).Sum(); }
        public double TotalConduitWeight { get => _conduit_info_pairs.Select(x => x.ConduitWeight).Sum(); }
        public double TotalWireWeight { get => _conduit_info_pairs.Select(x => x.WireWeight).Sum(); }
        public double TotalWeight { get => TotalConduitWeight + TotalWireWeight; }

        public double WeightPerFoot { get {
            double final_w = 0.0;
            foreach(var c in _conduit_info_pairs)
            {
                final_w += (c.ConduitWeight + c.WireWeight) / c.Length;        
            }
            return final_w; 
        } }

        public int NumberOfConduit { get => _conduit_info_pairs.Count; }
        public double PiercedHoleFactor { get; private set; } = 0.95; // for KO's
        public string DisplayLength(Document doc) => RMeasure.LengthFromDbl(doc, TotalConduitLength);

        public ConduitLoadCalcResult(double standard_strut_length, double pierced_hole_factor)
        {
            PiercedHoleFactor = pierced_hole_factor;
            StandardStrutLength = standard_strut_length;
        }

        public void AddConduitInfo(double length, double diameter, double conduit_weight, double wire_weight)
        {
            _conduit_info_pairs.Add((length, diameter, conduit_weight, wire_weight));
        }

        public string PrintCalcs(Document doc)
        {
            var dias = _conduit_info_pairs.Select(x => RMeasure.LengthFromDbl(doc, x.Diameter));
            var o = 
                string.Format("\nDiameters Present: [ {0} ]", string.Join(", ", dias)) + 
                string.Format(
                    "\nTotal Conduit Length: {0}\nConduit Weight: {1} lbs" + 
                    "\nWire Weight: {2} lbs\nTotal Weight: {3} lbs\nWeight Per Foot: {4} lb/ft.", 
                DisplayLength(doc), TotalConduitWeight, TotalWireWeight, TotalWeight, WeightPerFoot);

            var strut_len = RMeasure.LengthFromDbl(doc, StandardStrutLength);
            o += "\nMax Standard Strut Length For Rack: " + strut_len + "\n";
            o += "Pierced Hole Factor: " + PiercedHoleFactor.ToString() + "\n\n";
            o += PrintLoadInfo(doc);

            return o;
        }

        public string PrintFailedEntries(Document doc)
        {
            string o = "Conduit:\n";

            FailedConduitRuns.ForEach(x => o += "() " + x.MaterialType + ", " + x.Diameter + " ),\n");
            o += "\nWires:\n";
            FailedWires.ForEach(x => o += x + "\n");
            return o;
        }

        private string PrintLoadInfo(Document doc)
        {
            double support_spacing = 0.0;
            double add_spacing = 1.0;
            string o = "";
            var strut_len = RMeasure.LengthFromDbl(doc, StandardStrutLength);

            while(support_spacing <= 12.0)
            {
                support_spacing += add_spacing;
                var weight_to_be_supported = WeightPerFoot * support_spacing;
                bool s = LoadCalcData.GetB22SlottedUnistrut_ULD(doc, StandardStrutLength, out var uld);
                if (!s)
                {
                    debugger.debug_show(err:"did not find us: " + StandardStrutLength);
                    continue;
                }
                var max_center = LoadCalcData.GetConcetratedMidPointLoadCapacity(uld[0], uld[1]);

                o += string.Format("{0} in. Unistrut at {1} ft. spacing -> ({2} / {3}) -> {4}\n",
                    strut_len, support_spacing, weight_to_be_supported, uld[0].ToString(),
                    weight_to_be_supported < uld[0] ? "Passed" : "Failed");
            }

            return o;
        }
    }

    public static class ConduitLoadCalc
    {
        public static ConduitLoadCalcResult CalcSupportLoad(
            Document doc, IEnumerable<ConduitRunInfo> cris, 
            WireManager wm, double conduit_length, double standard_strut_length = 9.0, 
            double pierced_hole_factor = 0.95)
        {
            ConduitLoadCalcResult r = new ConduitLoadCalcResult(standard_strut_length, pierced_hole_factor);

            foreach(var cri in cris)
            {
                var mat_type = cri.ConduitMaterialType.Trim();
                var dia = cri.DiameterStr(doc).Trim();
                var arr = new[] { mat_type, dia };

                if(LoadCalcData.ConduitToWeight.TryGetValue(arr, out double w))
                {
                    var wires = wm.GetWires(cri.WireIds).ToList();
                    double wire_total_weight = 0.0;

                    wires.ForEach(w => {
                        var size = w.Size.Trim();
                        if(LoadCalcData.WireToWeight.TryGetValue(size, out double ww))
                            wire_total_weight += Math.Ceiling(ww * conduit_length);
                        else r.FailedWires.Add(size);
                    });

                    r.AddConduitInfo(conduit_length, cri.Diameter, Math.Ceiling(w * conduit_length), wire_total_weight);
                }
                else
                {
                    r.FailedConduitRuns.Add((mat_type, dia));
                    continue;
                }
            }

            return r;
        }
    }

    public class StringArrayComparer : IEqualityComparer<string[]>
    {
        public bool Equals(string[] x, string[] y)
        {
            if (x.Length != y.Length)
            {
                return false;
            }
            for (int i = 0; i < x.Length; i++)
            {
                if (x[i] != y[i])
                {
                    return false;
                }
            }
            return true;
        }

        public int GetHashCode(string[] obj)
        {
            int result = 17;
            for (int i = 0; i < obj.Length; i++)
            {
                unchecked
                {
                    result = result * 23 + obj[i].ToCharArray().Select(x => (int)x).Sum();
                }
            }
            return result;
        }
    }
}