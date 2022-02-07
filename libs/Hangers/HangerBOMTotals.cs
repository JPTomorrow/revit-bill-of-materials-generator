using System.Collections.Generic;
using Autodesk.Revit.DB;
using JPMorrow.Data.Globals;
using JPMorrow.Revit.Measurements;

namespace JPMorrow.Revit.Hangers
{

    public class HangerAnchorTotal
    {
        public string Type { get; set; }
        public string Diameter { get; set; }
        public int Count { get; set; }

        public HangerAnchorTotal(string type, string dia, int count)
        {
            Type = type;
            Diameter = dia;
            Count = count;
        }
    }

    public class HangerHexNutsTotal
    {
        public string Type { get; set; }
        public string Diameter { get; set; }
        public int Count { get; set; }

        public HangerHexNutsTotal(string type, string dia, int count)
        {
            Type = type;
            Diameter = dia;
            Count = count;
        }
    }

    public class HangerSpringNutsTotal
    {
        public string Type { get; set; }
        public string Diameter { get; set; }
        public int Count { get; set; }

        public HangerSpringNutsTotal(string type, string dia, int count)
        {
            Type = type;
            Diameter = dia;
            Count = count;
        }
    }

    public class HangerWashersTotal
    {
        public string Type { get; set; }
        public string Diameter { get; set; }
        public int Count { get; set; }

        public HangerWashersTotal(string type, string dia, int count)
        {
            Type = type;
            Diameter = dia;
            Count = count;
        }
    }

    public class HangerLockWashersTotal
    {
        public string Type { get; set; }
        public string Diameter { get; set; }
        public int Count { get; set; }

        public HangerLockWashersTotal(string type, string dia, int count)
        {
            Type = type;
            Diameter = dia;
            Count = count;
        }
    }

    public class HangerRodCouplingsTotal
    {
        public string Type { get; set; }
        public string Diameter { get; set; }
        public int Count { get; set; }

        public HangerRodCouplingsTotal(string type, string dia, int count)
        {
            Type = type;
            Diameter = dia;
            Count = count;
        }
    }

    public class HangerConduitStrapsTotal
    {
        public string Type { get; set; }
        public string Diameter { get; set; }
        public int Count { get; set; }

        public HangerConduitStrapsTotal(string type, string dia, int count)
        {
            Type = type;
            Diameter = dia;
            Count = count;
        }
    }

    public class HangerThreadedRodTotal
    {
        public string Type { get; set; }
        public string Diameter { get; set; }
        public double Length { get; set; }

        public HangerThreadedRodTotal(string type, string dia, double length)
        {
            Type = type;
            Diameter = dia;
            Length = length;
        }
    }

    public class HangerStrutTotal
    {
        public string Type { get; set; }
        public string Size { get; set; }
        public double Length { get; set; }

        public HangerStrutTotal(string type, string size, double length)
        {
            Type = type;
            Size = size;
            Length = length;
        }
    }

    public class HangerSingleAttachmentsTotal
    {
        public string Type { get; set; }
        public string Size { get; set; }
        public int Count { get; set; }

        public HangerSingleAttachmentsTotal(string type, string size, int count)
        {
            Type = type;
            Size = size;
            Count = count;
        }
    }

    /// <summary>
    /// Total hanger material up for BOM output
    /// </summary>
    public class HangerTotal
    {
        public List<HangerAnchorTotal> Anchors { get; set; }
        public List<HangerHexNutsTotal> HexNuts { get; set; }
        public List<HangerSpringNutsTotal> SpringNuts { get; set; }
        public List<HangerWashersTotal> Washers { get; set; }
        public List<HangerLockWashersTotal> LockWashers { get; set; }
        public List<HangerRodCouplingsTotal> RodCouplings { get; set; }
        public List<HangerThreadedRodTotal> ThreadedRod { get; set; }
        public List<HangerStrutTotal> Strut { get; set; }
        public List<HangerSingleAttachmentsTotal> SingleAttachments { get; set; }
        public List<HangerConduitStrapsTotal> ConduitStraps { get; set; }

        // Constructor
        public HangerTotal()
        {
            Anchors = new List<HangerAnchorTotal>();
            HexNuts = new List<HangerHexNutsTotal>();
            SpringNuts = new List<HangerSpringNutsTotal>();
            Washers = new List<HangerWashersTotal>();
            LockWashers = new List<HangerLockWashersTotal>();
            RodCouplings = new List<HangerRodCouplingsTotal>();
            ThreadedRod = new List<HangerThreadedRodTotal>();
            Strut = new List<HangerStrutTotal>();
            SingleAttachments = new List<HangerSingleAttachmentsTotal>();
            ConduitStraps = new List<HangerConduitStrapsTotal>();
        }

        /// <summary>
        /// Add Anchors to the total
        /// </summary>
        public void PushAnchors(string type, string dia, int cnt)
        {
            int index = Anchors.FindIndex(ind => ind.Type.Equals(type) && ind.Diameter.Equals(dia));

            if (index >= 0)
                Anchors[index].Count += cnt;
            else
                Anchors.Add(new HangerAnchorTotal(type, dia, cnt));
        }

        /// <summary>
        /// Add Hex Nuts to the total
        /// </summary>
        public void PushHexNuts(string type, string dia, int cnt)
        {
            int index = HexNuts.FindIndex(ind => ind.Type.Equals(type) && ind.Diameter.Equals(dia));

            if (index >= 0)
                HexNuts[index].Count += cnt;
            else
                HexNuts.Add(new HangerHexNutsTotal(type, dia, cnt));
        }

        /// <summary>
        /// Add Hex Nuts to the total
        /// </summary>
        public void PushSpringNuts(string type, string dia, int cnt)
        {
            int index = SpringNuts.FindIndex(ind => ind.Type.Equals(type) && ind.Diameter.Equals(dia));

            if (index >= 0)
                SpringNuts[index].Count += cnt;
            else
                SpringNuts.Add(new HangerSpringNutsTotal(type, dia, cnt));
        }

        /// <summary>
        /// Add Washers to the total
        /// </summary>
        public void PushWashers(string type, string dia, int cnt)
        {
            int idx = Washers.FindIndex(ind => ind.Type.Equals(type) && ind.Diameter.Equals(dia));

            if (idx >= 0)
                Washers[idx].Count += cnt;
            else
                Washers.Add(new HangerWashersTotal(type, dia, cnt));

        }

        /// <summary>
        /// Add Lock Washers to the total
        /// </summary>
        public void PushLockWashers(string type, string dia, int cnt)
        {
            int index = LockWashers.FindIndex(ind => ind.Type.Equals(type) && ind.Diameter.Equals(dia));

            if (index >= 0)
                LockWashers[index].Count += cnt;
            else
                LockWashers.Add(new HangerLockWashersTotal(type, dia, cnt));
        }

        /// <summary>
        /// Add Rod Couplings to the total
        /// </summary>
        public void PushRodCouplings(string type, string dia, int cnt)
        {
            int index = RodCouplings.FindIndex(ind => ind.Type.Equals(type) && ind.Diameter.Equals(dia));

            if (index >= 0)
                RodCouplings[index].Count += cnt;
            else
                RodCouplings.Add(new HangerRodCouplingsTotal(type, dia, cnt));
        }

        /// <summary>
        /// Add Threaded Rod to the total
        /// </summary>
        public void PushThreadedRod(string type, string dia, double len)
        {
            int index = ThreadedRod.FindIndex(ind => ind.Type.Equals(type) && ind.Diameter.Equals(dia));

            if (index >= 0)
                ThreadedRod[index].Length += len;
            else
                ThreadedRod.Add(new HangerThreadedRodTotal(type, dia, len));
        }

        /// <summary>
        /// Add strut to the total
        /// </summary>
        public void PushStrut(string type, string size, double len)
        {
            if (string.IsNullOrWhiteSpace(size)) return;

            int index = Strut.FindIndex(ind => ind.Type.Equals(type) && ind.Size.Equals(size));

            if (index >= 0)
                Strut[index].Length += len;
            else
                Strut.Add(new HangerStrutTotal(type, size, len));
        }

        /// <summary>
        /// Add attachmnets to the total
        /// </summary>
        public void PushIndividualAttachments(Document doc, SingleHanger hanger)
        {
            var batwing_cuttoff = RMeasure.LengthDbl(doc, "1\"");
            var caddy_clip_cuttoff = RMeasure.LengthDbl(doc, "2\"");

            var type = hanger.AttachmentType;
            var size = hanger.AttachmentSize;
            var size_dbl = RMeasure.LengthDbl(doc, size);

            string final_type = "Batwing - K-12";

            if (size_dbl >= batwing_cuttoff && size_dbl <= caddy_clip_cuttoff)
            {
                if (size.Equals("1 1/4\""))
                    final_type = "Caddy 20M4I Conduit Clip";
                else if (size.Equals("1 1/2\""))
                    final_type = "Caddy 24M4I Conduit Clip";
                else if (size.Equals("2\""))
                    final_type = "Caddy 32M4I Conduit Clip";
            }
            else if (size_dbl >= caddy_clip_cuttoff)
            {
                final_type = "Mineralac";
            }

            int index = SingleAttachments
                .FindIndex(ind => ind.Type.Equals(final_type) && ind.Size.Equals(size));

            if (index > -1)
            {
                var existing_cnt = SingleAttachments[index].Count;
                var new_entry = new HangerSingleAttachmentsTotal(final_type, size, existing_cnt + 1);
                SingleAttachments[index] = new_entry;
            }
            else
                SingleAttachments.Add(new HangerSingleAttachmentsTotal(final_type, size, 1));
        }

        /// <summary>
        /// Add attachmnets to the total
        /// </summary>
        public void PushConduitStraps(string type, string dia, int cnt)
        {
            int index = ConduitStraps.FindIndex(ind => ind.Type.Equals(type) && ind.Diameter.Equals(dia));

            if (index > -1)
            {
                var existing_cnt = ConduitStraps[index].Count;
                var new_entry = new HangerConduitStrapsTotal(type, dia, existing_cnt + cnt);
                ConduitStraps[index] = new_entry;
            }
            else
                ConduitStraps.Add(new HangerConduitStrapsTotal(type, dia, cnt));
        }
    }
}