// debug flag for the system 
#define P3_DEBUG

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autodesk.Revit.DB;
using JPMorrow.Revit.Documents;
using JPMorrow.Revit.Measurements;
using JPMorrow.Tools.Diagnostics;
using MoreLinq;

namespace JPMorrow.P3
{
    public enum P3PartCategory
    {
        Box, Plaster_Ring, Bracket,
        Hardware, Connector, Stinger,
        Clip
    }

    /// <summary>
    /// Main Library Class
    /// </summary>
    public static class P3InWall
    {
        private static Dictionary<string, string> DeviceParameters = new Dictionary<string, string>() {

            {"Fire Rated", "Fire Rated Wall"},
            {"Ring Type", "P-Ring Type"},
            {"Connector Type", "Connector Type"},
            {"Device Type", "Device Type Letter"},

            {"Width", "Width"},
            {"Height", "Height"},
            {"Depth", "Depth"},

            {"Top Left Connector Size", "Top Left Connector Size"},
            {"Top Middle Connector Size", "Top Middle Connector Size"},
            {"Top Right Connector Size", "Top Right Connector Size"},

            {"Bottom c Left Connector Size", "Bottom Left Connector Size"},
            {"Bottom Middle Connector Size", "Bottom Middle Connector Size"},
            {"Bottom Right Connector Size", "Bottom Right Connector Size"},
        };

        private static List<char> DeviceCheckChars = new List<char>() {
            'S', '3', 'X', 'F', 'P', 'N', 'M', 'C', '\u00B2', 'R', 'E', 'B'
        };

        private static Dictionary<string, string> DeviceCodeToPartName = new Dictionary<string, string>() {

            { "SP|1/2", "4\" Square Box - 1-1/2\" Deep - 3/4\" & 1/2\" KO" },
            { "SP|3/4", "4\" Square Box - 1-1/2\" Deep - 3/4\" & 1/2\" KO" },
            { "SP|1", "4\" Square Box - 1-1/2\" Deep - 1\" KO" },
            { "SP|M", "4\" Square Box - 1-1/2\" Deep - 3/4\" & 1/2\" KO" },

            { "SN|1/2", "4\" Square Box - 1-1/2\" Deep - 3/4\" & 1/2\" KO" },
            { "SN|3/4", "4\" Square Box - 1-1/2\" Deep - 3/4\" & 1/2\" KO" },
            { "SN|1", "4\" Square Box - 1-1/2\" Deep - 1\" KO" },
            { "SN|M", "4\" Square Box - 1-1/2\" Deep - 3/4\" & 1/2\" KO" },

            { "XN|1/2", "4-11/16\" Square Box - 2-1/8\" Deep - 3/4\" & 1/2\" KO" },
            { "XN|3/4", "4-11/16\" Square Box - 2-1/8\" Deep - 3/4\" & 1/2\" KO" },
            { "XN|1", "4-11/16\" Square Box - 2-1/8\" Deep - 1\" KO" },
            { "XN|M", "4-11/16\" Square Box - 2-1/8\" Deep - 3/4\" & 1/2\" KO" },

            { "XP|1/2", "4-11/16\" Square Box - 2-1/8\" Deep - 3/4\" & 1/2\" KO" },
            { "XP|3/4", "4-11/16\" Square Box - 2-1/8\" Deep - 3/4\" & 1/2\" KO" },
            { "XP|1", "4-11/16\" Square Box - 2-1/8\" Deep - 1\" KO" },
            { "XP|M", "4-11/16\" Square Box - 2-1/8\" Deep - 3/4\" & 1/2\" KO" },

            { "XF|1/2", "4-11/16\" Red Life Saftey Square Box - 2-1/8\" Deep - 3/4\" & 1/2\" KO" },
            { "XF|3/4", "4-11/16\" Red Life Saftey Square Box - 2-1/8\" Deep - 3/4\" & 1/2\" KO" },
            { "XF|1", "4-11/16\" Red Life Saftey Square Box - 2-1/8\" Deep - 1\" KO" },
            { "XF|M", "4-11/16\" Red Life Saftey Square Box - 2-1/8\" Deep - 3/4\" & 1/2\" KO" },

            { "3N|1/2", "4-11/16\" Square Box - 3-1/4\" Deep - 3/4\" & 1/2\" KO" },
            { "3N|3/4", "4-11/16\" Square Box - 3-1/4\" Deep - 3/4\" & 1/2\" KO" },
            { "3N|1", "4-11/16\" Square Box - 3-1/4\" Deep - 1\" KO" },
            { "3N|M", "4-11/16\" Square Box - 3-1/4\" Deep - 3/4\" & 1/2\" KO" },

            { "3P|1/2", "4-11/16\" Square Box - 3-1/4\" Deep - 3/4\" & 1/2\" KO" },
            { "3P|3/4", "4-11/16\" Square Box - 3-1/4\" Deep - 3/4\" & 1/2\" KO" },
            { "3P|1", "4-11/16\" Square Box - 3-1/4\" Deep - 1\" KO" },
            { "3P|M", "4-11/16\" Square Box - 3-1/4\" Deep - 3/4\" & 1/2\" KO" },

            { "3F|1/2", "4-11/16\" Red Life Saftey Square Box - 3-1/4\" Deep - 3/4\" & 1/2\" KO" },
            { "3F|3/4", "4-11/16\" Red Life Saftey Square Box - 3-1/4\" Deep - 3/4\" & 1/2\" KO" },
            { "3F|1", "4-11/16\" Red Life Saftey Square Box - 3-1/4\" Deep - 1\" KO" },
            { "3F|M", "4-11/16\" Red Life Saftey Square Box - 3-1/4\" Deep - 3/4\" & 1/2\" KO" },

            { "F|1/2", "4\" Red Life Saftey Square Box - 3-1/2\" Deep - 3/4\" & 1/2\" KO" },
            { "F|3/4", "4\" Red Life Saftey Square Box - 3-1/2\" Deep - 3/4\" & 1/2\" KO" },
            { "F|1", "4\" Red Life Saftey Square Box - 3-1/2\" Deep - 1\" KO" },
            { "F|M", "4\" Red Life Saftey Square Box - 3-1/2\" Deep - 3/4\" & 1/2\" KO" },

            { "P|1/2", "4\" Square Box - 2-1/8\" Deep - 3/4\" & 1/2\" KO" },
            { "P|3/4", "4\" Square Box - 2-1/8\" Deep - 3/4\" & 1/2\" KO" },
            { "P|1", "4\" Square Box - 2-1/8\" Deep - 1\" KO" },
            { "P|M", "4\" Square Box - 2-1/8\" Deep - 3/4\" & 1/2\" KO" },

            { "N|1/2", "4\" Square Box - 2-1/8\" Deep - 3/4\" & 1/2\" KO" },
            { "N|3/4", "4\" Square Box - 2-1/8\" Deep - 3/4\" & 1/2\" KO" },
            { "N|1", "4\" Square Box - 2-1/8\" Deep - 1\" KO" },
            { "N|M", "4\" Square Box - 2-1/8\" Deep - 3/4\" & 1/2\" KO" }, // @NOTE: should never happen
		};

        // "ConnectorType|ConnectorSize" : "Part Name"
        private static Dictionary<string, string> DeviceCodeToConduitConnectorSize = new Dictionary<string, string>() {
            { "2|C", "1/2" },
            { "3|C", "3/4" },
            { "4|C", "1" },
            { "5|C", "1 1/4" },
            { "6|C", "1 1/2" },
            { "8|C", "2" },
            { "M", "M" },
        };

        // format: [Connector Size]|[Connector Material Type]
        private static Dictionary<string, string> ExtraConnectorToConnectorPartName = new Dictionary<string, string>() {
            { "CT|1/2", "Connector - Set Screw Steel - EMT - 1/2\"" },
            { "CT|3/4", "Connector - Set Screw Steel - EMT - 3/4\"" },
            { "CT|1", "Connector - Set Screw Steel - EMT - 1\"" },
            { "CT|1 1/4", "Connector - Set Screw Steel - EMT - 1 1/4\"" },
            { "CT|1 1/2", "Connector - Set Screw Steel - EMT - 1 1/2\"" },
            { "CT|2", "Connector - Set Screw Steel - EMT - 2\"" },

            { "CB|1/2", "Connector - Male Adapter - PVC - 1/2\"" },
            { "CB|3/4", "Connector - Male Adapter - PVC - 3/4\"" },
            { "CB|1", "Connector - Male Adapter - PVC - 1\"" },
            { "CB|1 1/4", "Connector - Male Adapter - PVC - 1 1/4\"" },
            { "CB|1 1/2", "Connector - Male Adapter - PVC - 1 1/2\"" },
            { "CB|2", "Connector - Male Adapter - PVC - 2\"" },
        };

        private static Dictionary<string, string> ConnectorSizeToPartName = new Dictionary<string, string>() {
            { "1/2", "Connector - Set Screw Steel - EMT - 1/2\"" },
            { "3/4", "Connector - Set Screw Steel - EMT - 3/4\"" },
            { "1", "Connector - Set Screw Steel - EMT - 1\"" },
            { "1 1/4", "Connector - Set Screw Steel - EMT - 1 1/4\"" },
            { "1 1/2", "Connector - Set Screw Steel - EMT - 1 1/2\"" },
            { "2", "Connector - Set Screw Steel - EMT - 2\"" },
            { "M", "Connector - Metal Clad Cable - 3/8\"" },
        };

        private static Dictionary<string, string> ConnectorSizeToConduit = new Dictionary<string, string>() {
            { "1/2", "Conduit - EMT - 1/2\"" },
            { "3/4", "Conduit - EMT - 3/4\"" },
            { "1", "Conduit - EMT - 1\"" },
            { "1 1/4", "Conduit - EMT - 1 1/4\"" },
            { "1 1/2", "Conduit - EMT - 1 1/2\"" },
            { "2", "Conduit - EMT - 2\"" },
            { "M", "Conduit - MC Cable - #12/2C" },
        };

        // format: [Gang]|[Ring Type]|[Ring Depth]
        private static Dictionary<string, string> DeviceCodeToPlasterRingName = new Dictionary<string, string>() {
            { "4|1|1/2", "4\" Square Plaster Ring - Steel - 1-Gang - 1/2\" Deep" },
            { "4|2|1/2", "4\" Square Plaster Ring - Steel - 2-Gang - 1/2\" Deep" },
            { "4|3|1/2", "4\" Square Plaster Ring - Steel - 3-Gang - 1/2\" Deep" },
            { "4|4|1/2", "4\" Square Plaster Ring - Steel - 4-Gang - 1/2\" Deep" },
            { "4|R|1/2", "4\" Round Plaster Ring - Steel - 1/2\" Deep" },

            { "4 11/16|1|1/2", "4 11/16\" Square Plaster Ring - Steel - 1-Gang - 1/2\" Deep" },
            { "4 11/16|2|1/2", "4 11/16\" Square Plaster Ring - Steel - 2-Gang - 1/2\" Deep" },
            { "4 11/16|3|1/2", "4 11/16\" Square Plaster Ring - Steel - 3-Gang - 1/2\" Deep" },
            { "4 11/16|4|1/2", "4 11/16\" Square Plaster Ring - Steel - 4-Gang - 1/2\" Deep" },
            { "4 11/16|R|1/2", "4 11/16\" Round Plaster Ring - Steel - 1/2\" Deep" },

			// 1/4"------------------

			{ "4|1|1/4", "4\" Square Plaster Ring - Steel - 1-Gang - 1/4\" Deep" },
            { "4|2|1/4", "4\" Square Plaster Ring - Steel - 2-Gang - 1/4\" Deep" },
            { "4|3|1/4", "4\" Square Plaster Ring - Steel - 3-Gang - 1/4\" Deep" },
            { "4|4|1/4", "4\" Square Plaster Ring - Steel - 4-Gang - 1/4\" Deep" },
            { "4|R|1/4", "4\" Round Plaster Ring - Steel - 1/4\" Deep" },

            { "4 11/16|1|1/4", "4 11/16\" Square Plaster Ring - Steel - 1-Gang - 1/4\" Deep" },
            { "4 11/16|2|1/4", "4 11/16\" Square Plaster Ring - Steel - 2-Gang - 1/4\" Deep" },
            { "4 11/16|3|1/4", "4 11/16\" Square Plaster Ring - Steel - 3-Gang - 1/4\" Deep" },
            { "4 11/16|4|1/4", "4 11/16\" Square Plaster Ring - Steel - 4-Gang - 1/4\" Deep" },
            { "4 11/16|R|1/4", "4 11/16\" Round Plaster Ring - Steel - 1/4\" Deep" },

			// 3/4"------------------

			{ "4|1|3/4", "4\" Square Plaster Ring - Steel - 1-Gang - 3/4\" Deep" },
            { "4|2|3/4", "4\" Square Plaster Ring - Steel - 2-Gang - 3/4\" Deep" },
            { "4|3|3/4", "4\" Square Plaster Ring - Steel - 3-Gang - 3/4\" Deep" },
            { "4|4|3/4", "4\" Square Plaster Ring - Steel - 4-Gang - 3/4\" Deep" },
            { "4|R|3/4", "4\" Round Plaster Ring - Steel - 3/4\" Deep" },

            { "4 11/16|1|3/4", "4 11/16\" Square Plaster Ring - Steel - 1-Gang - 3/4\" Deep" },
            { "4 11/16|2|3/4", "4 11/16\" Square Plaster Ring - Steel - 2-Gang - 3/4\" Deep" },
            { "4 11/16|3|3/4", "4 11/16\" Square Plaster Ring - Steel - 3-Gang - 3/4\" Deep" },
            { "4 11/16|4|3/4", "4 11/16\" Square Plaster Ring - Steel - 4-Gang - 3/4\" Deep" },
            { "4 11/16|R|3/4", "4 11/16\" Round Plaster Ring - Steel - 3/4\" Deep" },

			// 5/8"------------------

			{ "4|1|5/8", "4\" Square Plaster Ring - Steel - 1-Gang - 5/8\" Deep" },
            { "4|2|5/8", "4\" Square Plaster Ring - Steel - 2-Gang - 5/8\" Deep" },
            { "4|3|5/8", "4\" Square Plaster Ring - Steel - 3-Gang - 5/8\" Deep" },
            { "4|4|5/8", "4\" Square Plaster Ring - Steel - 4-Gang - 5/8\" Deep" },
            { "4|R|5/8", "4\" Round Plaster Ring - Steel - 5/8\" Deep" },
            { "4|E|5/8", "4\" Red Life Safety Square Plaster Ring - Steel - 5/8\" Deep" },

            { "4 11/16|1|5/8", "4 11/16\" Square Plaster Ring - Steel - 1-Gang - 5/8\" Deep" },
            { "4 11/16|2|5/8", "4 11/16\" Square Plaster Ring - Steel - 2-Gang - 5/8\" Deep" },
            { "4 11/16|3|5/8", "4 11/16\" Square Plaster Ring - Steel - 3-Gang - 5/8\" Deep" },
            { "4 11/16|4|5/8", "4 11/16\" Square Plaster Ring - Steel - 4-Gang - 5/8\" Deep" },
            { "4 11/16|R|5/8", "4 11/16\" Round Plaster Ring - Steel - 5/8\" Deep" },
            { "4 11/16|E|5/8", "4 11/16\" Red Life Safety Square Plaster Ring - Steel - 5/8\" Deep" },

			// 1"------------------

			{ "4|1|1", "4\" Square Plaster Ring - Steel - 1-Gang - 1\" Deep" },
            { "4|2|1", "4\" Square Plaster Ring - Steel - 2-Gang - 1\" Deep" },
            { "4|3|1", "4\" Square Plaster Ring - Steel - 3-Gang - 1\" Deep" },
            { "4|4|1", "4\" Square Plaster Ring - Steel - 4-Gang - 1\" Deep" },
            { "4|R|1", "4\" Round Plaster Ring - Steel - 1\" Deep" },

            { "4 11/16|1|1", "4 11/16\" Square Plaster Ring - Steel - 1-Gang - 1\" Deep" },
            { "4 11/16|2|1", "4 11/16\" Square Plaster Ring - Steel - 2-Gang - 1\" Deep" },
            { "4 11/16|3|1", "4 11/16\" Square Plaster Ring - Steel - 3-Gang - 1\" Deep" },
            { "4 11/16|4|1", "4 11/16\" Square Plaster Ring - Steel - 4-Gang - 1\" Deep" },
            { "4 11/16|R|1", "4 11/16\" Round Plaster Ring - Steel - 1\" Deep" },

			// 1 1/4"------------------

			{ "4|1|1 1/4", "4\" Square Plaster Ring - Steel - 1-Gang - 1 1/4\" Deep" },
            { "4|2|1 1/4", "4\" Square Plaster Ring - Steel - 2-Gang - 1 1/4\" Deep" },
            { "4|3|1 1/4", "4\" Square Plaster Ring - Steel - 3-Gang - 1 1/4\" Deep" },
            { "4|4|1 1/4", "4\" Square Plaster Ring - Steel - 4-Gang - 1 1/4\" Deep" },
            { "4|R|1 1/4", "4\" Round Plaster Ring - Steel - 1 1/4\" Deep" },
            { "4|E|1 1/4", "4\" Red Life Safety Square Plaster Ring - Steel - 1 1/4\" Deep" },

            { "4 11/16|1|1 1/4", "4 11/16\" Square Plaster Ring - Steel - 1-Gang - 1 1/4\" Deep" },
            { "4 11/16|2|1 1/4", "4 11/16\" Square Plaster Ring - Steel - 2-Gang - 1 1/4\" Deep" },
            { "4 11/16|3|1 1/4", "4 11/16\" Square Plaster Ring - Steel - 3-Gang - 1 1/4\" Deep" },
            { "4 11/16|4|1 1/4", "4 11/16\" Square Plaster Ring - Steel - 4-Gang - 1 1/4\" Deep" },
            { "4 11/16|R|1 1/4", "4 11/16\" Round Plaster Ring - Steel - 1 1/4\" Deep" },
            { "4 11/16|E|1 1/4", "4 11/16\" Red Life Safety Square Plaster Ring - Steel - 1 1/4\" Deep" },

			// 1 1/2"------------------

			{ "4|1|1 1/2", "4\" Square Plaster Ring - Steel - 1-Gang - 1 1/2\" Deep" },
            { "4|2|1 1/2", "4\" Square Plaster Ring - Steel - 2-Gang - 1 1/2\" Deep" },
            { "4|3|1 1/2", "4\" Square Plaster Ring - Steel - 3-Gang - 1 1/2\" Deep" },
            { "4|4|1 1/2", "4\" Square Plaster Ring - Steel - 4-Gang - 1 1/2\" Deep" },
            { "4|R|1 1/2", "4\" Round Plaster Ring - Steel - 1 1/2\" Deep" },

            { "4 11/16|1|1 1/2", "4 11/16\" Square Plaster Ring - Steel - 1-Gang - 1 1/2\" Deep" },
            { "4 11/16|2|1 1/2", "4 11/16\" Square Plaster Ring - Steel - 2-Gang - 1 1/2\" Deep" },
            { "4 11/16|3|1 1/2", "4 11/16\" Square Plaster Ring - Steel - 3-Gang - 1 1/2\" Deep" },
            { "4 11/16|4|1 1/2", "4 11/16\" Square Plaster Ring - Steel - 4-Gang - 1 1/2\" Deep" },
            { "4 11/16|R|1 1/2", "4 11/16\" Round Plaster Ring - Steel - 1 1/2\" Deep" },

			// 2"------------------

			{ "4|1|2", "4\" Square Plaster Ring - Steel - 1-Gang - 2\" Deep" },
            { "4|2|2", "4\" Square Plaster Ring - Steel - 2-Gang - 2\" Deep" },
            { "4|3|2", "4\" Square Plaster Ring - Steel - 3-Gang - 2\" Deep" },
            { "4|4|2", "4\" Square Plaster Ring - Steel - 4-Gang - 2\" Deep" },
            { "4|R|2", "4\" Round Plaster Ring - Steel - 2\" Deep" },

            { "4 11/16|1|2", "4 11/16\" Square Plaster Ring - Steel - 1-Gang - 2\" Deep" },
            { "4 11/16|2|2", "4 11/16\" Square Plaster Ring - Steel - 2-Gang - 2\" Deep" },
            { "4 11/16|3|2", "4 11/16\" Square Plaster Ring - Steel - 3-Gang - 2\" Deep" },
            { "4 11/16|4|2", "4 11/16\" Square Plaster Ring - Steel - 4-Gang - 2\" Deep" },
            { "4 11/16|R|2", "4 11/16\" Round Plaster Ring - Steel - 2\" Deep" },

			// A------------------

			{ "4|1|A", "4\" Square Plaster Ring - Steel - 1-Gang - Adjustable" },
            { "4|2|A", "4\" Square Plaster Ring - Steel - 2-Gang - Adjustable" },
            { "4|3|A", "4\" Square Plaster Ring - Steel - 3-Gang - Adjustable" },
            { "4|4|A", "4\" Square Plaster Ring - Steel - 4-Gang - Adjustable" },
            { "4|R|A", "4\" Round Plaster Ring - Steel - Adjustable" },

            { "4 11/16|1|A", "4 11/16\" Square Plaster Ring - Steel - 1-Gang - Adjustable" },
            { "4 11/16|2|A", "4 11/16\" Square Plaster Ring - Steel - 2-Gang - Adjustable" },
            { "4 11/16|3|A", "4 11/16\" Square Plaster Ring - Steel - 3-Gang - Adjustable" },
            { "4 11/16|4|A", "4 11/16\" Square Plaster Ring - Steel - 4-Gang - Adjustable" },
            { "4 11/16|R|A", "4 11/16\" Round Plaster Ring - Steel - Adjustable" },
        };

        /// <summary>
        /// Represents a device code and a quantity of how many 
        /// are present in model
        /// </summary>
        public class DeviceCodeQtyPair
        {
            public string DeviceCode { get; set; }
            public int Qty { get; set; }

            public DeviceCodeQtyPair(string code, int qty)
            {
                DeviceCode = code;
                Qty = qty;
            }
        }

        /// <summary>
        /// Extract device codes and quantities from a text file 
        /// describing those boxes
        /// </summary>
        public static IEnumerable<DeviceCodeQtyPair> GetDeviceCodeAndQtyFromFile(string file_name)
        {
            var all_txt = File.ReadAllText(file_name);
            var lines = all_txt.Split('\n');

            List<DeviceCodeQtyPair> pairs = new List<DeviceCodeQtyPair>();
            foreach (var l in lines)
            {
                var entry = l.Split('|');
                if (entry.Length != 2) continue;
                pairs.Add(new DeviceCodeQtyPair(entry[0].Trim(), int.Parse(entry[1].Trim())));
            }

            return pairs;
        }

        /// <summary>
        /// Get Legacy device codes from file and return a collection 
        /// of parts to make up the box
        /// </summary>
        public static IEnumerable<P3PartCollection> GetLegacyDevices(ModelInfo info, IEnumerable<DeviceCodeQtyPair> code_pairs)
        {
            var codes = new List<P3Code>();
            foreach (var p in code_pairs)
            {
                codes.AddRange(P3Code.GetDeviceCodesFromPair(info.DOC, p));
            }

            var devices = ParseLegacyDeviceCodes(info, codes);
            return devices;
        }

        /// <summary>
        /// get legacy device codes from fixtures in the revit model 
        /// and return a collection of parts to make up the box
        /// </summary>
        public static IEnumerable<P3PartCollection> GetLegacyDevices(ModelInfo info, IEnumerable<ElementId> fixture_ids)
        {
            if (!fixture_ids.Any())
            {
                debugger.show(header: "P3 In Wall", err: "No boxes to process");
                return new List<P3PartCollection>();
            }

            // collect the device codes
            var device_codes = new List<P3Code>();

            string o = "";
            foreach (var id in fixture_ids)
            {
                try
                {
                    var code = P3Code.GetDeviceCodeFromFixture(info.DOC, id);
                    if (code.IsValidCode) device_codes.Add(code);
                }
                catch
                {
                    o += id.ToString() + "\n";
                }
            }

            // USE THIS IF YOU ARE TRYING TO DEBUG FAILED BOXES
            // debugger.show(header:"failed boxes", err: o);

            return ParseLegacyDeviceCodes(info, device_codes);
        }

        private static IEnumerable<P3PartCollection> ParseLegacyDeviceCodes(ModelInfo info, IEnumerable<P3Code> codes)
        {

            List<ElementId> failed_devices = new List<ElementId>();

            static IEnumerable<P3PartCollection> set_return_state(string debug_message)
            {
                debugger.show(header: "P3 In Wall", err: debug_message);
                return new List<P3PartCollection>();
            }

            if (!codes.Any()) return set_return_state("No device codes provided");
            List<P3PartCollection> part_colls = new List<P3PartCollection>();

            // string stored_code = null;
            foreach (var code in codes)
            {
                var hardware_parts = new List<P3Part>();

                bool has_box_part = DeviceCodeToPartName.TryGetValue(code.BoxSizeCode, out var box_part);
                bool has_conduit_size = DeviceCodeToConduitConnectorSize.TryGetValue(code.ConnectorSizeCode, out var connector_size);
                bool has_connector_part = ConnectorSizeToPartName.TryGetValue(connector_size, out var connector_part);
                bool has_plaster_ring = DeviceCodeToPlasterRingName.TryGetValue(code.GangCode, out var plaster_ring);

                if (!has_box_part || !has_conduit_size || !has_connector_part || !has_plaster_ring)
                {
                    failed_devices.Add(code.FixtureId);
                    continue;
                }

                P3Part conduit_clip = null;
                if (connector_size != "M")
                {
                    conduit_clip = GetConduitClip(info, code.BoxSizeCode, connector_size);
                    hardware_parts.Add(conduit_clip);
                }

                if (code.BoxSizeCode.Contains("P"))
                {
                    hardware_parts.Add(new P3Part("Ground Stinger #12 Copper", 1, P3PartCategory.Stinger));
                    // hardware_parts.Add(new P3Part("Mounting Bracket", 1, P3PartCategory.Bracket));
                    hardware_parts.Add(new P3Part("Wafer Head Tek Screw", 4, P3PartCategory.Hardware));

                    bool s = ConnectorSizeToConduit.TryGetValue(connector_size, out var conduit);
                    if (!s) throw new Exception("conduit material not found");

                    if (connector_size.Contains("M"))
                    {
                        hardware_parts.Add(new P3Part(conduit, 15, P3PartCategory.Hardware));
                        hardware_parts.Add(new P3Part("Conduit Strap - 1/2\"", (int)Math.Ceiling(15.0 / 4.0), P3PartCategory.Hardware));
                    }
                    else
                    {
                        hardware_parts.Add(new P3Part(conduit, 15, P3PartCategory.Hardware));
                        hardware_parts.Add(new P3Part("Conduit Strap - " + connector_size + "\"", 3, P3PartCategory.Hardware));
                    }
                }

                if (code.BoxSizeCode.Contains("N") || code.BoxSizeCode.Contains("F"))
                {
                    // hardware_parts.Add(new P3Part("Mounting Bracket", 1, P3PartCategory.Bracket));
                    hardware_parts.Add(new P3Part("Wafer Head Tek Screw", 4, P3PartCategory.Hardware));

                    bool s = ConnectorSizeToConduit.TryGetValue(connector_size, out var conduit);
                    if (!s) throw new Exception("conduit material not found");

                    if (connector_size.Contains("M"))
                    {
                        hardware_parts.Add(new P3Part(conduit, 10, P3PartCategory.Hardware));
                        hardware_parts.Add(new P3Part("Conduit Strap - 1/2\"", (int)Math.Ceiling(10.0 / 4.0), P3PartCategory.Hardware));
                    }
                    else
                    {
                        hardware_parts.Add(new P3Part(conduit, 10, P3PartCategory.Hardware));
                        hardware_parts.Add(new P3Part("Conduit Strap - " + connector_size + "\"", 3, P3PartCategory.Hardware));
                    }
                }

                foreach (var connector in code.ExtraConnectors)
                {
                    if (connector_size.Contains("M")) continue;
                    var part_name = connector + "|" + connector_size;
                    bool s = ExtraConnectorToConnectorPartName.TryGetValue(part_name, out string con_part);
                    if (!s) continue;
                    hardware_parts.Add(new P3Part(con_part, 1, P3PartCategory.Connector));
                }

                P3Part final_box_part = new P3Part(box_part, 1, P3PartCategory.Box);
                P3Part plaster_ring_part = new P3Part(plaster_ring, 1, P3PartCategory.Plaster_Ring);
                P3Part final_connector_part = new P3Part(connector_part, 1, P3PartCategory.Connector);

                if (code.RawDeviceCode.Contains("\u00B2"))
                    hardware_parts.Add(new P3Part("1/4\"x20 - 1/2\" Long Screw", 1, P3PartCategory.Hardware));

                var p_idx = part_colls.FindIndex(x => x.DeviceCode.Equals(code.RawDeviceCode));
                if (p_idx == -1)
                {
                    hardware_parts.Add(final_box_part);
                    hardware_parts.Add(final_connector_part);
                    hardware_parts.Add(plaster_ring_part);
                    var coll = new P3PartCollection(
                        code.RawDeviceCode, hardware_parts);
                    part_colls.Add(coll);
                }
                else
                {
                    part_colls[p_idx].AddPart(final_box_part);
                    hardware_parts.Add(final_connector_part);
                    part_colls[p_idx].AddPart(plaster_ring_part);

                    foreach (var part in hardware_parts)
                        part_colls[p_idx].AddPart(part);
                }
            }

            debugger.show(header: "Devices", err: string.Join("\n", P3Code.PrintDevices(codes)));

            // print failed devices
            if (failed_devices.Any())
            {

                debugger.show(
                    err: failed_devices.Count().ToString() +
                    " devices failed to be parsed. These will be selected" +
                    " in the model so you can review their device code.");

                info.SEL.SetElementIds(failed_devices);
            }

            return part_colls;
        }

        /// <summary>
        /// get conduit clip info and add part for it
        /// </summary>
        private static P3Part GetConduitClip(ModelInfo info, string box_code, string connector)
        {
            // conduit clip
            var three_quarters = RMeasure.LengthDbl(info.DOC, "3/4\"");
            var one_half = RMeasure.LengthDbl(info.DOC, "1/2\"");
            var one_inch = RMeasure.LengthDbl(info.DOC, "1\"");

            var c_size = connector + "\"";
            var cc_size = RMeasure.LengthDbl(info.DOC, c_size);

            if (box_code.Contains("M"))
            {
                if (cc_size == one_half)
                {
                    return new P3Part("Metal Clad Conduit Clip Snap Close Flange Hanger Side Mount - 1/2\"", 1, P3PartCategory.Clip);
                }
                else if (cc_size == three_quarters)
                {
                    return new P3Part("Metal Clad Conduit Clip Snap Close Flange Hanger Side Mount - 3/4\"", 1, P3PartCategory.Clip);
                }
                else if (cc_size >= one_inch)
                {
                    return new P3Part("Metal Clad Conduit Clip Snap Close Flange Hanger Side Mount - 3/4\"", 1, P3PartCategory.Clip);
                }
            }
            else
            {
                if (cc_size == one_half)
                {
                    return new P3Part("Conduit Clip Snap Close Flange Hanger Side Mount - 1/2\"", 1, P3PartCategory.Clip);
                }
                else if (cc_size == three_quarters)
                {
                    return new P3Part("Conduit Clip Snap Close Flange Hanger Side Mount - 3/4\"", 1, P3PartCategory.Clip);
                }
                else if (cc_size >= one_inch)
                {
                    return new P3Part("Conduit Clip Snap Close Flange Hanger Side Mount - 1\"", 1, P3PartCategory.Clip);
                }
            }

            return null;
        }

        public class P3Code
        {
            public ElementId FixtureId { get; private set; }
            public string RawDeviceCode { get; private set; }
            public string ConnectorSizeCode { get; private set; } = string.Empty;
            public string BoxSizeCode { get; private set; } = string.Empty;
            public string GangCode { get; private set; } = string.Empty;

            private List<string> _extra_connectors = new List<string>();
            public IList<string> ExtraConnectors { get => _extra_connectors; }

            public bool IsValidCode { get; private set; } = false;
            private static string CodeParameterName = "Box Assembly Code";

            private P3Code(Document doc, Element fixture)
            {
                FixtureId = fixture.Id;
                IsValidCode = false;
                RawDeviceCode = GetDeviceCodeFromComments(doc, fixture);
                ProcessCode();
            }

            private P3Code(Document doc, string code)
            {
                FixtureId = ElementId.InvalidElementId;
                RawDeviceCode = code;
                ProcessCode();
            }

            public static P3Code GetDeviceCodeFromFixture(Document doc, ElementId fixture_id)
            {
                var fixture = doc.GetElement(fixture_id);
                return new P3Code(doc, fixture);
            }

            public static IEnumerable<P3Code> GetDeviceCodesFromPair(Document doc, DeviceCodeQtyPair pair)
            {
                List<P3Code> codes = new List<P3Code>();
                for (int i = 0; i < pair.Qty; ++i)
                {
                    codes.Add(new P3Code(doc, pair.DeviceCode));
                }
                return codes;
            }

            public override string ToString()
            {
                string o = RawDeviceCode + "\n{\n";
                o += string.Format(
                    "\tBox Size Code: {0},\n\tGang Size Code: {1},\n\tConnector Size Code: {2}\n",
                    BoxSizeCode ?? "null", GangCode ?? "null", ConnectorSizeCode ?? "null");
                o += "}\n";
                return o;
            }

            internal class InternalPrintFormat
            {
                public string Code { get; set; }
                public int Qty { get; set; }

                public InternalPrintFormat(string code, int qty)
                {
                    Code = code;
                    Qty = qty;
                }
            }

            public static string PrintDevices(IEnumerable<P3Code> devices)
            {
                string ret = "";
                List<InternalPrintFormat> flatten = new List<InternalPrintFormat>();

                foreach (var device in devices)
                {
                    var idx = flatten.FindIndex(x => x.Code.Equals(device.RawDeviceCode));

                    if (idx == -1)
                        flatten.Add(new InternalPrintFormat(device.RawDeviceCode, 1));
                    else
                    {
                        var cnt = flatten[idx].Qty;
                        flatten.RemoveAt(idx);
                        flatten.Add(new InternalPrintFormat(device.RawDeviceCode, cnt + 1));
                    }
                }

                foreach (var f in flatten)
                {
                    ret += string.Format("{0}{1}\n",
                    f.Code.Trim().PadRight(30 - f.Code.Length - f.Qty.ToString().Length, ' '),
                    f.Qty.ToString());
                }

                return ret;
            }

            /// <summary>
            /// get the device code from the comments 
            /// section and create an P3BoxInfo
            /// </summary>
            private string GetDeviceCodeFromComments(Document doc, Element fixture)
            {
                var p = fixture.LookupParameter(CodeParameterName);
                if (p == null)
                {
                    IsValidCode = false;
                    return string.Empty;
                }

                var raw_code_str = p.AsString();

                bool unfit_code_format(string s) =>
                    s.Any(c => !char.IsDigit(c) && !DeviceCheckChars.Any(y => y.Equals(c)));

                if (raw_code_str == null || String.IsNullOrWhiteSpace(raw_code_str)) return string.Empty;

                var split_comments = raw_code_str.Split('-');
                if (!split_comments.Any()) return string.Empty;
                var first_part = split_comments.First();
                if (unfit_code_format(first_part)) return string.Empty;
                return raw_code_str;
            }

            /// <summary>
            /// Process device code into its parts
            /// </summary>
            private void ProcessCode()
            {

                if (RawDeviceCode == string.Empty) return;

                var code_split = RawDeviceCode.Split('-');

                if (code_split.Count() < 2) return;

                var first_part = code_split.First();

                var last_parts = code_split
                    .Where(x => !x.Equals(first_part))
                    .Select(y => y.Trim())
                    .ToList();

                if (!last_parts.Any()) return;

                // character -> string conversion for code janitoring
                string s(params char[] x) => string.Join("", x.Select(x => x.ToString()));

                if (first_part.Length == 3)
                { // no conduit connector size or upsized box

                    BoxSizeCode = s(first_part[0]);
                    GangCode = s(first_part[1]);
                    ConnectorSizeCode = s(first_part[2]);
                }
                else if (first_part.Length == 4)
                {
                    bool chk = char.IsDigit(first_part[1]) || first_part[1].Equals('R') || first_part[1].Equals('E');
                    BoxSizeCode = chk ? s(first_part[0]) : s(first_part[0], first_part[1]);
                    GangCode = chk ? s(first_part[1]) : s(first_part[2]);
                    ConnectorSizeCode = chk ? s(first_part[2], '|', first_part[3]) : s(first_part[3]);
                }
                else if (first_part.Length == 5)
                { // has 2 box code characters, as well as connector size

                    BoxSizeCode = s(first_part[0], first_part[1]);
                    GangCode = s(first_part[2]);
                    ConnectorSizeCode = s(first_part[3], '|', first_part[4]);
                }
                else if (first_part.Length == 6)
                {

                    BoxSizeCode = s(first_part[0], first_part[1]);
                    GangCode = s(first_part[2]);
                    ConnectorSizeCode = s(first_part[4], '|', first_part[5]);
                }

                bool has_connector_size = DeviceCodeToConduitConnectorSize
                    .TryGetValue(ConnectorSizeCode, out var connector_size);

                if (!has_connector_size) return;

                BoxSizeCode += "|" + connector_size;
                bool has_box_part = DeviceCodeToPartName.TryGetValue(BoxSizeCode, out var box_part);

                if (!has_box_part) return;

                /* if (RawDeviceCode.First().Equals('3'))
                    debugger.debug_show(err: "box part: " + box_part + "\n" +
                    "box size code: " + BoxSizeCode + "\n" +
                    "raw code: " + RawDeviceCode); */

                if (RawDeviceCode.Contains("\u00B2"))
                {
                    GangCode = "4|" + GangCode;
                }
                else
                {
                    GangCode = box_part.Contains("4-11/16") ? "4 11/16|" + GangCode : "4|" + GangCode;
                }

                GangCode = CorrectGang(GangCode, last_parts);
                _extra_connectors = GetExtraConnectors(last_parts).ToList();

                IsValidCode = true;
            }

            /// <summary>
            /// correct the gang code from the 
            /// provided last parts of the device code
            /// </summary>
            private static string CorrectGang(string gang, IEnumerable<string> last_parts)
            {

                if (last_parts == null || !last_parts.Any())
                    throw new Exception("last_parts is empty");

                return (gang + "|" + last_parts.First()).Trim();
            }

            /// <summary>
            /// Extract information about extra connectors 
            /// from the last parts of the device code
            /// </summary>
            private static IEnumerable<string> GetExtraConnectors(IEnumerable<string> last_parts)
            {
                var extra_c = last_parts
                    .Where(x => x.ToLower().Equals("cb") || x.ToLower().Equals("ct"))
                    .Select(x => x.ToUpper().Trim()).ToList();

                return extra_c;
            }
        }
    }

    /// <summary>
    /// A collection of P3 In Wall Parts that 
    /// belong to a specific device code
    /// </summary>
	public class P3PartCollection
    {
        public string DeviceCode { get; private set; }
        public List<P3Part> Parts { get; set; } = new List<P3Part>();

        public P3PartCollection(string device_code, IEnumerable<P3Part> parts)
        {
            DeviceCode = device_code;
            foreach (var p in parts) AddPart(p);
        }

        public P3PartCollection(string device_code)
        {
            DeviceCode = device_code;
            Parts = new List<P3Part>();
        }

        public void AddPart(P3Part part)
        {
            var idx = Parts.FindIndex(x => x.Name.Equals(part.Name));
            if (idx > -1) Parts[idx].AddQty(part.Qty);
            else Parts.Add(part.Clone() as P3Part);
            Parts = Parts.OrderBy(x => x.Name).ToList();
        }

        public override string ToString()
        {
            string o = "\n" + DeviceCode + " {\n";
            o += string.Join("\n", Parts.Select(x => x.ToString()));
            o += "}\n";
            return o;
        }

        public string ToString(P3PartCategory category)
        {
            return string.Join("\n", Parts
                .Where(x => x.Category == category)
                .Select(x => x.ToString()));
        }

        /// <summary>
        /// Get Device Code Separated Part Totals
        /// </summary>
        public static IEnumerable<P3PartCollection> GetPartTotalsByCategory(IEnumerable<P3PartCollection> pcolls, params P3PartCategory[] cats)
        {
            var copy_colls = pcolls.ToList().ConvertAll(x => new P3PartCollection(x.DeviceCode, x.Parts));
            foreach (var coll in copy_colls)
            {
                List<P3Part> remove_parts = new List<P3Part>();
                foreach (var part in coll.Parts)
                {
                    if (!cats.Any(x => part.Category == x))
                        remove_parts.Add(part);
                }
                remove_parts.ForEach(x => coll.Parts.Remove(x));
            }

            copy_colls = copy_colls.OrderBy(x => x.DeviceCode).ToList();
            return copy_colls;
        }
    }

    /// <summary>
    /// A collection of P3 In Wall Parts
    /// </summary>
    public class P3PartTotal
    {
        public List<P3Part> Parts { get; set; } = new List<P3Part>();

        public P3PartTotal() { }

        public P3PartTotal(IEnumerable<P3Part> parts)
        {
            foreach (var p in parts) AddPart(p);
        }

        public void AddPart(P3Part part)
        {
            var idx = Parts.FindIndex(x => x.Name.Equals(part.Name));
            if (idx > -1) Parts[idx].AddQty(part.Qty);
            else Parts.Add(part.Clone() as P3Part);
            Parts = Parts.OrderBy(x => x.Name).ToList();
        }

        public override string ToString()
        {
            return string.Join("\n", Parts.Select(x => x.ToString()));
        }

        public string ToString(P3PartCategory category)
        {
            return string.Join("\n", Parts
                .Where(x => x.Category == category)
                .Select(x => x.ToString()));
        }

        /// <summary>
        /// Get Part Totals not separated by device code
        /// </summary>
        public static P3PartTotal GetPartTotals(IEnumerable<P3PartCollection> pcolls, params P3PartCategory[] cats)
        {
            var total = new P3PartTotal();

            foreach (var p in pcolls.SelectMany(x => x.Parts))
            {
                if (!cats.Any(x => x == p.Category)) continue;
                total.AddPart(p);
            }

            return total;
        }
    }

    /// <summary>
    /// A Set P3 In Wall hardware part
    /// </summary>
    public class P3Part : ICloneable
    {
        public string Name { get; private set; }
        public int Qty { get; private set; }
        public P3PartCategory Category { get; private set; }

        public int IncrementQty()
        {
            Qty += 1;
            return Qty;
        }

        public void AddQty(int qty) => Qty += qty;

        public P3Part(string name, int qty, P3PartCategory category)
        {
            Name = name;
            Qty = qty;
            Category = category;
        }

        public override string ToString()
        {
            return string.Format("{0} - {1}", Name, Qty);
        }

        public object Clone()
        {
            return new P3Part(Name, Qty, Category);
        }
    }
}
