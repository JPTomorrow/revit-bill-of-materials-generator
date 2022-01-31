/*
    // Make a strut hanger system using the Revit API.
    // The input into the system will be a set of ElementIds that are conduits selected by the user with a length greater than 1 or equal to foot, and a Revit 3DView passed as parameters.
    // The selected conduit will be all of the conduit in a conduit rack.
    // A placement of the strut hangers will be determined by the longest conduit in the conduit rack.
    // If the length of the longest conduit is less than 6 feet, work out a point half way down the longest conduit. this will be the number of hangers.
    // If the length of the longest conduit is greater than 6 feet, take the total feet of the longest conduit and subtract 6 feet from it and divide by 8 feet. this will be the number of hangers.
    // The Revit 3DView will be used to determine length of the two threaded rods for the strut hanger. 
    // An RRay will be shot in the Up direction vector and the length between the start point of the ray and the first collision will be the length of the threaded rod.
    // Each strut hanger will have 4 washers and two hex nuts that correspond to the threaded rod size of the two threaded rods on the strut hanger. The size of the wasthers and hex nuts will match the size of the threaded rods on the strut hanger.
    // Each strut hanger will have two Hilti Concrete Anchors. The size of the anchors will match the threaded rod size of the two threaded rods on the strut hanger.
    // The valid threaded rod sizes are: [ 1/4", 3/8", 1/2"]
    // The strut hanger will store a list of the conduit element ids that were provided as input to make the strut hanger.
    // Each strut hanger will have a count of rod coupling that is equal to the total length of each threaded rod divided by 10 feet.
    // Each strut hanger will have a set of conduit straps. There will be one conduit strap for each pipe in the conduit rack. The diameter of the conduit will be the diameter of the strap.


using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Autodesk.Revit.DB;
using JPMorrow.Revit.Documents;
using JPMorrow.Revit.Measurements;
using JPMorrow.Revit.Tools;

namespace JPMorrow.NewNeoHangers
{
    [DataContract]
    public class ConduitStrap
    {
        // The strap diameter
        [DataMember]
        public double Diameter { get; set; }
        // The name of the conduit strap
        [DataMember]
        public static string Name { get; set; } = "Conduit Strap";
    }

    [DataContract]
    public class TierSpacing
    {
        // The spacing of the tier
        [DataMember]
        public double Spacing { get; private set; }
        // The radius offset 
        [DataMember]
        public double RadiusOffset { get; private set; }
    }

    [DataContract]
    public class StrutHanger
    {
        // default strut size
        [DataMember]
        public static string DefaultStrutSize { get; } = "1 5/8\"";
        // Dictionary with the keys as the washer, and hex nut. The values are the integer counts of each.
        [DataMember]
        public static Dictionary<string, int> StrutHangerHardware { get; set; } = new Dictionary<string, int>() { { "Washer", 4 }, { "HexNut", 2 } };
        // The conduit rack element ids
        [DataMember]
        public List<int> ConduitRackIds { get; set; } = new List<int>();
        public List<ElementId> ConduitRackElemetnIds { get => ConduitRackIds.Select(x => new ElementId(x)).ToList(); }

        // The length of the first threaded rod
        [DataMember]
        public double FirstThreadedRodLength { get; set; } = 0.0;
        // The length of the second threaded rod
        [DataMember]
        public double SecondThreadedRodLength { get; set; } = 0.0;
        // The tyoe name of the anchor for the first threaded rod
        [DataMember]
        public string FirstAnchorTypeName { get; set; } = "Hilti Concrete Anchor";
        // The type name of the anchor for the second threaded rod
        [DataMember]
        public string SecondAnchorTypeName { get; set; } = "Hilti Concrete Anchor";
        // Conduit Straps
        [DataMember]
        public List<ConduitStrap> ConduitStraps { get; set; } = new List<ConduitStrap>();
        // Rod Coupling Count
        [DataMember]
        public int RodCouplingCount { get => (int)Math.Ceiling(FirstThreadedRodLength / 10.0) + (int)Math.Ceiling(SecondThreadedRodLength / 10.0); }
        // The 2 endpoints of the strut line as XYZSerializable
        [DataMember]
        public XYZSerializable[] StrutLineEndpoints { get; set; } = new XYZSerializable[2];
        // the size of the strut
        [DataMember]
        public string StrutSize { get; set; } = DefaultStrutSize;
        // Tier spacings
        [DataMember]
        public List<TierSpacing> TierSpacings { get; set; } = new List<TierSpacing>();
        // the origin point that generated the hanger
        [DataMember]
        public XYZSerializable Origin { get; set; }

        // constructor
        public StrutHanger() { }

        // use provided element ids to return a list of created strut hangers
        public static IEnumerable<StrutHanger> CreateStrutHangers(ModelInfo info, List<ElementId> ids, View3D view)
        {
            // check that all ids are conduit
            if (ids.Select(x => info.DOC.GetElement(x)).Any(x => x.Category.Name != "Conduit"))
            {
                throw new Exception("All element ids must be conduit.");
            }

            // get placement point data for strut hanger from ids
            var placementPoints = GetPlacementPoints(info, ids, view);

            return new List<StrutHanger>();
        }

        // get placement points for the strut hanger
        private static List<XYZSerializable> GetPlacementPoints(ModelInfo info, List<ElementId> ids, View3D view)
        {
            List<XYZSerializable> pts = new List<XYZSerializable>();
            var leftmost = info.DOC.GetElement(GetLeftmostId(info, ids));
            var longest = info.DOC.GetElement(GetLongestId(info, ids));

            // get the length of all the ids
            var lengths = from i in Enumerable.Range(0, ids.Count - 1)
                          from j in Enumerable.Range(i + 1, ids.Count - i - 1)
                          select Tuple.Create(info.DOC.GetElement(x).LookupParameter("Length").AsDouble(),
                              info.DOC.GetElement(j).LookupParameter("Length").AsDouble());

            // get the longest length
            var longestLength = lengths



            return pts;
        }

        // get conduit rack extremes
        private static ElementId[] GetExtremes(ModelInfo info, List<ElementId> ids)
        {
            ElementId[] extremes = new ElementId[2];
            extremes[0] = GetLeftmostId(info, ids);
            extremes[1] = GetRightmostId(info, ids);
            return extremes;
        }

        // get the leftmost conduit ElemetntId in a conduit rack
        private static ElementId GetLeftmostId(ModelInfo info, List<ElementId> ids) => throw new NotImplementedException("GetLeftmostId");
        // get the rightmost conduit ElemetntId in a conduit rack
        private static ElementId GetRightmostId(ModelInfo info, List<ElementId> ids) => throw new NotImplementedException("GetRightmostId");
        // get the longest conduit element id in the conduit rack
        private static ElementId GetLongestId(ModelInfo info, List<ElementId> ids) => throw new NotImplementedException("GetLongestId");
        // get the shortest conduit element id in the conduit rack
        private static ElementId GetShortestId(ModelInfo info, List<ElementId> ids) => throw new NotImplementedException("GetShortestId");
    } */