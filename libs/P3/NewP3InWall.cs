/* 

using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using JPMorrow.Revit.Documents;
using JPMorrow.Revit.Measurements;
using JPMorrow.Tools.Diagnostics;

namespace JPMorrow.Revit.P3 
{   
    using PartLookup = Dictionary<string, string>; // alias for shortening part swap type

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

    public class P3InWallDevice
    {
        public P3InWallDevice(string file_name)
        {
            var all_txt = File.ReadAllText(file_name);
            var lines = all_txt.Split('\n');

            List<DeviceCodeQtyPair> pairs = new List<DeviceCodeQtyPair>();
            foreach(var l in lines)
            {
                var entry = l.Split('|');
                if(entry.Length != 2) continue;
                pairs.Add(new DeviceCodeQtyPair(entry[0].Trim(), int.Parse(entry[1].Trim())));
            }

            return pairs;
        }

        public P3InWallDevice()
        {

        }
    }

    /// <summary>
    /// Class for all P3InWall Free Functions
    /// </summary>
    public static class P3InWall
    {
        /// <summary>
        /// Extract device codes and quantities from a text file 
        /// describing those boxes
        /// </summary>
        public static IEnumerable<DeviceCodeQtyPair> GetDeviceCodeAndQtyFromFile(string file_name)
        {
            var all_txt = File.ReadAllText(file_name);
            var lines = all_txt.Split('\n');

            List<DeviceCodeQtyPair> pairs = new List<DeviceCodeQtyPair>();
            foreach(var l in lines)
            {
                var entry = l.Split('|');
                if(entry.Length != 2) continue;
                pairs.Add(new DeviceCodeQtyPair(entry[0].Trim(), int.Parse(entry[1].Trim())));
            }

            return pairs;
        }

        /// <summary>
        /// Get Legacy device codes from file and return a collection 
        /// of parts to make up the box
        /// </summary>
        public static IEnumerable<P3PartCollection> GetLegacyDevices(
            ModelInfo info, IEnumerable<DeviceCodeQtyPair> code_pairs)
        {
            var codes = new List<P3Code>();
            foreach(var p in code_pairs)
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
        public static IEnumerable<P3PartCollection> GetLegacyDevices(
            ModelInfo info, IEnumerable<ElementId> fixture_ids)
        {
            if(!fixture_ids.Any())
            {
                debugger.show(header: "P3 In Wall", err:"No boxes to process");
                return new List<P3PartCollection>();
            }

            // collect the device codes
            var device_codes = new List<P3Code>();

            foreach (var id in fixture_ids)
            {
                var code = P3Code.GetDeviceCodeFromFixture(info.DOC, id);
                if (code.IsValidCode) device_codes.Add(code);
            }

            return ParseLegacyDeviceCodes(info, device_codes);
        }
    }

    public static class P3InWallData
    {
        public static PartLookup DeviceParameters = new PartLookup() {

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

        public static List<char> DeviceCheckChars = new List<char>() {
            'S', 'X', 'F', 'P', 'N', 'M', 'C', '\u00B2', 'R', 'E'
        };

        public static PartLookup DeviceCodeToPartName = new PartLookup() {

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

        // format: [Connector Size]|[Connector Material Type]
        public static PartLookup DeviceCodeToConduitConnectorSize = new PartLookup() {
            { "2|C", "1/2" },
            { "3|C", "3/4" },
            { "4|C", "1" },
            { "5|C", "1 1/4" },
            { "6|C", "1 1/2" },
            { "8|C", "2" },   
            { "M", "M" },
        };

        public static PartLookup ConnectorSizeToPartName = new PartLookup() {
            { "1/2", "Connector - EMT - 1/2\" - Set Screw Steel" },
            { "3/4", "Connector - EMT - 3/4\" - Set Screw Steel" },
            { "1", "Connector - EMT - 1\" - Set Screw Steel" },
            { "1 1/4", "Connector - EMT - 1 1/4\" - Set Screw Steel" },
            { "1 1/2", "Connector - EMT - 1 1/2\" - Set Screw Steel" },
            { "2", "Connector - EMT - 2\" - Set Screw Steel" },
            { "M", "Connector - Metal Clad Cable - 3/8\"" },
        };

        public static PartLookup ConnectorSizeToConduit = new PartLookup() {
            { "1/2", "Conduit - EMT - 1/2\"" },
            { "3/4", "Conduit - EMT - 3/4\"" },
            { "1", "Conduit - EMT - 1\"" },
            { "1 1/4", "Conduit - EMT - 1 1/4\"" },
            { "1 1/2", "Conduit - EMT - 1 1/2\"" },
            { "2", "Conduit - EMT - 2\"" },
            { "M", "MC Cable - #12/2C" },
        };

        // format: [Gang]|[Ring Type]|[Ring Depth]
        public static PartLookup DeviceCodeToPlasterRingName = new PartLookup() {
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
    }
} */