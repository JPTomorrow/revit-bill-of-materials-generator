
using JPMorrow.Revit.BOMPackage;
using JPMorrow.Revit.Documents;
using JPMorrow.Revit.ElectricalRoom;
using JPMorrow.Revit.Hangers;
using JPMorrow.Revit.WirePackage;

namespace JPMorrow.Data.Globals
{
    /// <summary>
    /// App lifetime storage for things 
    /// that need to persist
    /// </summary>
    public static class ALS
    {
        public static ModelInfo Info { get; set; }
        public static MasterDataPackage AppData { get; set; } = new MasterDataPackage();

        public static WirePackageSettings WirePackSettings { get; set; }
        public static ElecRoom ElecRoom { get; set; } = new ElecRoom();
    }
}