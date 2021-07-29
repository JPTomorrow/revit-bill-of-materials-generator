///
/// Handles automatically adding wire to a set of conduit runs
/// Author: Justin Morrow
///

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using JPMorrow.BetterFasterStrongerLinq;
using JPMorrow.Revit.Documents;
using JPMorrow.Revit.Wires;
using JPMorrow.Tools.Diagnostics;
using OfficeOpenXml;

namespace JPMorrow.Revit.ConduitRuns {

    [DataContract]
    public class LowVoltageWirePairing {

        [DataMember]
        public string WireNumber { get; private set; }
        [DataMember]
        public string[] WireNames { get; private set; }

        public LowVoltageWirePairing(string wire_number, string[] wire_names) {
            WireNumber = wire_number;
            WireNames = wire_names;
        }

        public static LowVoltageWirePairing GetWirePairing(List<LowVoltageWirePairing> wpairs, string wire_number) {
            var pairs = wpairs.Where(x => x.WireNumber.Equals(wire_number));
            if(!pairs.Any()) return null;
            return pairs.First();
        }   

        public override string ToString() {
            var names = string.Join(", ", WireNames);
            return "[ " + WireNumber  + names + " ]";
        }
    }

    [DataContract]
    public class LowVoltageDevicePairing {

        [DataMember]
        public string PanelName { get; private set; }
        [DataMember]
        public string DeviceNumber { get; private set; }
        [DataMember]
        public string WireNumber { get; private set; }

        public LowVoltageDevicePairing(string panel_name, string device_number, string wire_number) {
            PanelName = panel_name;
            DeviceNumber = device_number;
            WireNumber = wire_number;
        }

        public static LowVoltageDevicePairing GetDevicePairing(
            List<LowVoltageDevicePairing> dpairs, string panel_name, string device_number) {
                var pairs = dpairs.Where(x => x.PanelName.Equals(panel_name) && x.DeviceNumber.Equals(device_number));
                if(!pairs.Any()) return null;
                return pairs.First();
        }

        public override string ToString() {
            return string.Format("[ {0}, {1}, {2} ]", PanelName, DeviceNumber, WireNumber);
        }
    }

    public static class LowVoltageDeviceAutomation {

        private enum ExcelDevicePairingHeader {
            PanelName = 1,
            DeviceNumber = 2,
            WireNumber = 4,
        }

        private enum ExcelWirePairingHeader {
            WireNumber = 1,
            WireNames = 2,
        }

        public static IEnumerable<LowVoltageDevicePairing> MakeDevicePairings(string filename) {

            var pairings = new List<LowVoltageDevicePairing>();
            var failed_pairings = new List<LowVoltageDevicePairing>();

            // init excel file
            using ExcelPackage p = new ExcelPackage(new FileInfo(filename));
            var sheets = p.Workbook.Worksheets.ToList();
            sheets.Remove(sheets.First());
            
            foreach(var sheet in sheets) {
                int rowCount = sheet.Dimension.End.Row;

                // read from excel file
                for (int row = 4; row <= rowCount; row++)  {

                    var vals = new List<string>();

                    for (int col = 1; col <= (int)ExcelDevicePairingHeader.WireNumber; col++) {
                        if(!Enum.IsDefined(typeof(ExcelDevicePairingHeader), col)) continue; 

                        var val = sheet.Cells[row, col]?.Value?.ToString();

                        if(val == null) {
                            vals.Add(string.Empty);
                            continue;
                        }


                        vals.Add(val);
                    }
                    
                    if(vals.All(x => x.Equals(string.Empty))) continue; // handle blank lines

                    if(vals.Any(x => x.Equals(string.Empty))) {

                        foreach(var s in vals.ToArray()) {
                            if(s.Equals(string.Empty))
                                vals[vals.IndexOf(s)] = "--- ";
                        }
                        
                        failed_pairings.Add(new LowVoltageDevicePairing(vals[0], vals[1], vals[2]));
                    }
                    else {
                        pairings.Add(new LowVoltageDevicePairing(vals[0], vals[1], vals[2]));
                    }
                }
            }
            
            // print failed pairings
            if(failed_pairings.Any())
            debugger.show(
                header:"Low Voltage Device Pairings", 
                err:"The following device pairings failed to process because the excel file was not formated correctly:\n\n" +
                "**Note the '---' fields in this output, they will inform you which excel column in the sheet did not have a value\n\n" +
                string.Join("\n", failed_pairings.Select(x => x.ToString())));

            return pairings;
        }

        public static IEnumerable<LowVoltageWirePairing> MakeWirePairings(string filename) {

            var pairings = new List<LowVoltageWirePairing>();
            var failed_pairings = new List<LowVoltageWirePairing>();

            // init excel file
            using ExcelPackage p = new ExcelPackage(new FileInfo(filename));
            var sheet = p.Workbook.Worksheets[1];

            if(sheet.Name != "Wire Legend") {
                debugger.show(
                    header:"Import Wire Pairings", 
                    err:"The Wire legend is not present, aborting.\n" + 
                    "The first sheet in the excel document must be named 'Wire Legend'.");
                
                return new List<LowVoltageWirePairing>();
            }

            int rowCount = sheet.Dimension.End.Row;

            // read from excel file
            for (int row = 3; row <= rowCount; row++) {

                var vals = new List<string>();

                for (int col = 1; col <= (int)ExcelWirePairingHeader.WireNames; col++) {

                    // parse wire names col
                    if(col == (int)ExcelWirePairingHeader.WireNames) {
                        var split = sheet.Cells[row, col].Value.ToString().Split(' ').ToList();
                        split = split.Where(x => !x.Equals("&")).ToList();

                        if(split.Count % 2 != 0)  { // if odd amount in list
                            vals.Add(string.Empty);
                            continue; 
                        }
                        
                        var pp = split.SplitList(2).ToList(); // potential pairings

                        foreach(var pair in pp) {
                            var num_of_wires = pair[0].Trim();
                            var wire_type = pair[1].Trim();

                            if(num_of_wires == null || !Regex.Match(num_of_wires, @"[(]\d[)]").Success) {
                                vals.Add(string.Empty);
                                continue; 
                            }

                            if(wire_type == null || !Wire.LowVoltageWireNames.Any(x => x.ToLower().Equals(wire_type.ToLower()))) {
                                vals.Add(string.Empty);
                                continue;
                            }
                            
                            var qty = int.Parse(num_of_wires.Trim('(', ')'));
                            
                            for(var i = 0; i < qty; i++ ) 
                                vals.Add(wire_type);
                        }
                    }
                    else {
                        vals.Add(sheet.Cells[row, col].Value.ToString());
                    }
                }

                if(vals.All(x => x.Equals(string.Empty))) continue; // handle blank lines

                if(vals.Any(x => x.Equals(string.Empty))) {
                    foreach(var s in vals.ToArray()) {
                        if(s.Equals(string.Empty))
                            vals[vals.IndexOf(s)] = "--- ";
                    }
                    
                    failed_pairings.Add(new LowVoltageWirePairing(vals[0], vals.Skip(1).ToArray()));
                }
                else {
                    pairings.Add(new LowVoltageWirePairing(vals[0], vals.Skip(1).ToArray()));
                }
            }

            // print failed pairings
            if(failed_pairings.Any()) {
                debugger.show(
                    header:"Low Voltage Wire Pairings", 
                    err:"The following wire pairings failed to process because the excel file was not formated correctly:\n\n" +
                    "**Note the '---' fields in this output, they will inform you which excel column in the sheet did not have a value\n\n" +
                    string.Join("\n", failed_pairings.Select(x => x.ToString())));
            }
            
            return pairings;
        }
        
        public static void AddLowVoltageDeviceWire(
            IEnumerable<ConduitRunInfo> source, WireManager wm, 
            IEnumerable<LowVoltageDevicePairing> device_pairs, 
            IEnumerable<LowVoltageWirePairing> wire_pairs) {
            
            // check for conduit runs that have telecom device names in the 'to' parameter
            foreach(var cri in source) {
                
                var dpairs = device_pairs.ToList();
                var wpairs = wire_pairs.ToList();
                
                var to_split = cri.To.Split(',').Select(x => x.Trim()).ToList();
                var wires = wm.GetWires(cri.WireIds);
                
                wires.Where(x => Wire.LowVoltageWireNames
                    .Any(y => x.Size.Equals(y)))
                    .ToList()
                    .ForEach(x => wm.RemoveWire(cri.WireIds, x, out var rem));

                foreach(var s in to_split) {

                    bool convert = int.TryParse(s, out var i);
                    if(!convert) continue;
                    var ss = i.ToString();
                    var dpair = LowVoltageDevicePairing.GetDevicePairing(dpairs, cri.From, ss);
                    if(dpair == null) continue;
                    var wpair = LowVoltageWirePairing.GetWirePairing(wpairs, dpair.WireNumber);
                    if(wpair == null) continue;

                    foreach(var wire_name in wpair.WireNames) {
                        var cnum = cri.From + "-" + dpair.DeviceNumber + "-" + dpair.WireNumber;
                        
                        var wire = new Wire(
                            cnum, wire_name, WireColor.GetLowVoltageWireColor(wire_name), 
                            WireType.LowVoltage, WireMaterialType.Special);

                        // check to see that wire has not already been added
                        if(wires.Contains(wire)) continue;
                        wm.AddWire(cri.WireIds, wire);
                    }
                }
            }
        }
        
        ///
        /// External Transaction
        ///

        private static LowVoltageDeviceTag handler_low_voltage_device_tag = null;
		private static ExternalEvent exEvent_low_voltage_device_tag = null;

        // This is a NHA project function
        // tag all the low voltage devices with a standard device ID based on parameter data that they already have
        public static async void NHA_TagLowVoltageDevices(ModelInfo info, IEnumerable<ElementId> device_ids) {

            var ids = device_ids.ToList();
            var failed_devices = new List<ElementId>();

            Parameter p(Element el, string x) => el.LookupParameter(x);

            foreach(var id in ids) {

                var el = info.DOC.GetElement(id);

                var pff = p(el, "Panel Fed From");
                var dnp = p(el, "Device #");
                var wnp = p(el, "Wire #");

                if( pff == null || dnp == null || wnp == null || 
                    !pff.HasValue || !dnp.HasValue || !wnp.HasValue) {
                    
                    failed_devices.Add(id);
                    continue;
                }

                await NHA_PushLowVoltageDeviceTag(info, el, pff.AsString() + "-" + dnp.AsString() + "-" + wnp.AsString());
            }

            if(failed_devices.Any()) {
                
                debugger.show(
                    header:"Device Tagging", 

                    err: failed_devices.Count() + 
                    " devices failed to be tagged. " + 
                    "They will be selected in the model for review");
                
                info.SEL.SetElementIds(failed_devices);
            }
        }

        // Sign up the event handlers
        public static void NHA_LowVoltageDeviceTagSignUp() {
			handler_low_voltage_device_tag = new LowVoltageDeviceTag();
			exEvent_low_voltage_device_tag = ExternalEvent.Create(handler_low_voltage_device_tag.Clone() as IExternalEventHandler);
		}

        // Create a strut hanger in the revit model and bind it to this program
		private static async Task NHA_PushLowVoltageDeviceTag(ModelInfo info, Element device, string device_tag) {
            
			handler_low_voltage_device_tag.Info = info;
			handler_low_voltage_device_tag.Device = device;
            handler_low_voltage_device_tag.DeviceTag = device_tag;
			exEvent_low_voltage_device_tag.Raise();

			while(exEvent_low_voltage_device_tag.IsPending) {
				await Task.Delay(100);
			}
		}

        private class LowVoltageDeviceTag : IExternalEventHandler, ICloneable
		{
			public ModelInfo Info { get; set; }
            public Element Device { get; set; }
            public string DeviceTag { get; set; }


			public object Clone() => this;

			public void Execute(UIApplication app)
			{
                try {
                    using (Transaction tx = new Transaction(Info.DOC, "Tag Low Voltage Device")) {
                        tx.Start();
                        var p = Device.LookupParameter("Device ID");
                        p.Set(DeviceTag);
                        tx.Commit();
                    }
                }
                catch(Exception ex) {
                    debugger.show(header:"Low Voltage Device Tags", err:ex.ToString());
                }
			}

			public string GetName()
			{
				return "Tag Low Voltage Device ID";
			}
		}
    }
}