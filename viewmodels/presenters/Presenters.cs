using System.ComponentModel;
using System.Runtime.CompilerServices;
using JPMorrow.Revit.Wires;
using JPMorrow.P3;
using JPMorrow.Revit.ConduitRuns;
using JPMorrow.Revit.Custom.GroundBar;
using JPMorrow.Revit.Custom.Unistrut;
using JPMorrow.Revit.Documents;
using JPMorrow.Revit.Hangers;
using JPMorrow.Revit.Hardware;
using JPMorrow.Revit.Labor;
using JPMorrow.Revit.Panels;
using System;
using System.Linq;
using JPMorrow.Revit.ElectricalRoom;
using JPMorrow.Revit.Measurements;
using JPMorrow.Revit.VoltageDrop;
using JPMorrow.Data.Globals;

namespace JPMorrow.UI.ViewModels
{
    public partial class ParentViewModel
    {
        /// <summary>
        /// Single Run Entry ListBox Binding
        /// </summary>
        public class RunPresenter : Presenter
        {
            public ConduitRunInfo Value;
            public RunPresenter(ConduitRunInfo value, ModelInfo info)
            {
                Value = value;
                RefreshDisplay(info);
            }

            public void RefreshDisplay(ModelInfo info)
            {
                Has_Wire = ALS.AppData.GetSelectedConduitPackage().WireManager.CheckConduitWire(Value.WireIds) ? "Yes" : "No";
                Length = RMeasure.LengthFromDbl(info.DOC, Value.Length);
                From = Value.From;
                To = Value.To;
                Diameter = RMeasure.LengthFromDbl(info.DOC, Value.Diameter);
                Conduit_Type = Value.ConduitMaterialType;
                Reported_Wires = Value.ReportedWireSizes;
            }

            private string hw;
            public string Has_Wire {get => hw;
            set {
                hw = value;
                Update("Has_Wire");
            }}

            private string _from;
            public string From {get => _from;
            set {
                _from = value;
                Update("From");
            }}

            private string _to;
            public string To {get => _to;
            set {
                _to = value;
                Value.OverrideToStr(_to);
                Update("To");
            }}

            private string _length;
            public string Length {get => _length;
            set {
                _length = value;
                Update("Length");
            }}

            private string _diameter;
            public string Diameter {get => _diameter;
            set {
                _diameter = value;
                Update("Diameter");
            }}

            private string _conduit_type;
            public string Conduit_Type {get => _conduit_type;
            set {
                _conduit_type = value;
                Update("Conduit_Type");
                }
            }

            private string _wire_size;
            public string Reported_Wires {get => _wire_size;
            set {
                _wire_size = value;
                Update("Reported_Wires");
                }
            }

            //Item Selection Bindings
            private bool _isSelected;
            public bool IsSelected { get => _isSelected;
                set {
                    _isSelected = value;
                    Update("Run_Items");
            }}
        }

        /// <summary>
        /// Single Run Entry ListBox Binding
        /// </summary>
        public class MigrantRunPresenter : Presenter
        {
            public ConduitRunInfo Value;
            public MigrantRunPresenter(ConduitRunInfo value, ModelInfo info)
            {
                Value = value;
                RefreshDisplay(info);
            }

            public void RefreshDisplay(ModelInfo info)
            {
                Has_Wire = ALS.AppData.GetSelectedConduitPackage().WireManager.CheckConduitWire(Value.WireIds) ? "Yes" : "No";
                Length = RMeasure.LengthFromDbl(info.DOC, Value.Length);
                From = Value.From;
                To = Value.To;
                Diameter = RMeasure.LengthFromDbl(info.DOC, Value.Diameter);
                Conduit_Type = Value.ConduitMaterialType;
                Reported_Wires = Value.ReportedWireSizes;
            }

            private string hw;
            public string Has_Wire {get => hw;
            set {
                hw = value;
                Update("Has_Wire");
            }}

            private string _from;
            public string From {get => _from;
            set {
                _from = value;
                Update("From");
            }}

            private string _to;
            public string To {get => _to;
            set {
                _to = value;
                Value.OverrideToStr(_to);
                Update("To");
            }}

            private string _length;
            public string Length {get => _length;
            set {
                _length = value;
                Update("Length");
            }}

            private string _diameter;
            public string Diameter {get => _diameter;
            set {
                _diameter = value;
                Update("Diameter");
            }}

            private string _conduit_type;
            public string Conduit_Type {get => _conduit_type;
            set {
                _conduit_type = value;
                Update("Conduit_Type");
                }
            }

            private string _wire_size;
            public string Reported_Wires {get => _wire_size;
            set {
                _wire_size = value;
                Update("Reported_Wires");
                }
            }

            //Item Selection Bindings
            private bool _isSelected;
            public bool IsSelected { get => _isSelected;
                set {
                    _isSelected = value;
                    Update("Migrate_Run_Items");
            }}
        }

        /// <summary>
        /// Single Run Entry ListBox Binding
        /// </summary>
        public class WirePresenter : Presenter
        {
            public Wire Value;
            public WirePresenter(Wire value, ModelInfo info)
            {
                Value = value;
                RefreshDisplay(info);
            }

            public void RefreshDisplay(ModelInfo info)
            {
                C_Num = Value.CircuitNumber;
                Color = Value.Color;
                Size = Value.Size;
                Type = Wire.WireTypeName(Value.WireType);
                Mat_Type = Enum.GetName(typeof(WireMaterialType), Value.WireMaterialType);
            }

            private string c;
            public string C_Num {get => c;
            set {
                c = value;
                Update("C_Num");
            }}

            private string color;
            public string Color {get => color;
            set {
                color = value;
                Update("Color");
            }}

            private string size;
            public string Size {get => size;
            set {
                size = value;
                Update("Size");
            }}

            private string type;
            public string Type {get => type;
            set {
                type = value;
                Update("Type");
            }}

            private string mat_type;
            public string Mat_Type {get => mat_type;
            set {
                mat_type = value;
                Update("Mat_Type");
            }}

            //Item Selection Bindings
            private bool _isSelected;
            public bool IsSelected { get => _isSelected;
                set {
                    _isSelected = value;
                    Update("Wire_Items");
            }}
        }

        // Single Hanger Presenter
        public class SingleHangerPresenter : Presenter
        {
            public SingleHanger Value;
            public SingleHangerPresenter(SingleHanger value, ModelInfo info)
            {
                Value = value;
                RefreshDisplay(info);
            }

            public void RefreshDisplay(ModelInfo info)
            {
                Rod_Diameter = RMeasure.LengthFromDbl(info.DOC, Value.RodDiameter);
                Rod_Length = RMeasure.LengthFromDbl(info.DOC, Value.RodLength);
                Att_Type = Value.AttachmentType;
                Anchor_Type = Value.AnchorType;
                Attachment_Size = Value.AttachmentSize;
            }

            private string h_type = "Single Hanger";
            public string Hanger_Type { get => h_type;
                set {
                    h_type = value;
                    Update("Hanger_Type");
                } }

            private string rod_len;
            public string Rod_Length { get => rod_len;
                set {
                    rod_len = value;
                    Update("Rod_Length");
                } }

            private string rod_dia;
            public string Rod_Diameter { get => rod_dia;
                set {
                    rod_dia = value;
                    Update("Rod_Diameter");
                } }

            private string AttachmentType;
            public string Att_Type { get => AttachmentType;
                set {
                    AttachmentType = value;
                    Update("Att_Type");
                } }

            private string anch_type;
            public string Anchor_Type { get => anch_type;
                set {
                    anch_type = value;
                    Update("Anchor_Type");
                } }

            private string attachment_size;
            public string Attachment_Size { get => attachment_size;
                set {
                    attachment_size = value;
                    Update("Attachment_Size");
                } }

            //Item Selection Bindings
            private bool _isSelected;
            public bool IsSelected { get => _isSelected;
                set {
                    _isSelected = value;
                    Update("Single_Hanger_Items");
            }}
        }

        // Strut Hanger Presenter
        public class StrutHangerPresenter : Presenter
        {
            public StrutHanger Value;
            public StrutHangerPresenter(StrutHanger value, ModelInfo info)
            {
                Value = value;
                RefreshDisplay(info);
            }

            public void RefreshDisplay(ModelInfo info)
            {
                Rod_Diameter = RMeasure.LengthFromDbl(info.DOC, Value.RodDiameter);
                Rod_Length = RMeasure.LengthFromDbl(info.DOC, Value.RodOneLength);
                Att_Type = "Slotted Strut - " + Value.StrutSize;
                Anchor_Type = Value.AnchorOneType;
            }

            private string h_type = "Strut Hanger";
            public string Hanger_Type { get => h_type;
                set {
                    h_type = value;
                    Update("Hanger_Type");
                } }

            private string rod_len;
            public string Rod_Length { get => rod_len;
                set {
                    rod_len = value;
                    Update("Rod_Length");
                } }

            private string rod_dia;
            public string Rod_Diameter { get => rod_dia;
                set {
                    rod_dia = value;
                    Update("Rod_Diameter");
                } }

            private string AttachmentType;
            public string Att_Type { get => AttachmentType;
                set {
                    AttachmentType = value;
                    Update("Att_Type");
                } }

            private string anch_type;
            public string Anchor_Type { get => anch_type;
                set {
                    anch_type = value;
                    Update("Anchor_Type");
                } }

            //Item Selection Bindings
            private bool _isSelected;
            public bool IsSelected { get => _isSelected;
                set {
                    _isSelected = value;
                    Update("Strut_Hanger_Items");
            }}
        }


        // Fixture Hanger Presenter
        public class FixtureHangerPresenter : Presenter
        {
            public FixtureHanger Value;
            public FixtureHangerPresenter(FixtureHanger value, ModelInfo info)
            {
                Value = value;
                RefreshDisplay(info);
            }

            public void RefreshDisplay(ModelInfo info)
            {
                Rod_Diameter = RMeasure.LengthFromDbl(info.DOC, Value.RodDiameter);
                Rod_Length = RMeasure.LengthFromDbl(info.DOC, Value.RodLength);
                Anchor_Type = Value.AnchorType;
            }

            private string h_type = "Fixture Hanger";
            public string Hanger_Type { get => h_type;
                set {
                    h_type = value;
                    Update("Hanger_Type");
                } }

            private string rod_len;
            public string Rod_Length { get => rod_len;
                set {
                    rod_len = value;
                    Update("Rod_Length");
                } }

            private string rod_dia;
            public string Rod_Diameter { get => rod_dia;
                set {
                    rod_dia = value;
                    Update("Rod_Diameter");
                } }

            private string anch_type;
            public string Anchor_Type { get => anch_type;
                set {
                    anch_type = value;
                    Update("Anchor_Type");
                } }

            //Item Selection Bindings
            private bool _isSelected;
            public bool IsSelected { get => _isSelected;
                set {
                    _isSelected = value;
                    Update("Fixture_Hanger_Items");
            }}
        }

        /// <summary>
        /// Misc Hardware Entry
        /// </summary>
        public class HardwarePresenter : Presenter
        {
            public HardwareEntry Value;
            public HardwarePresenter(HardwareEntry value, ModelInfo info)
            {
                Value = value;
                RefreshDisplay(info);
            }

            public void RefreshDisplay(ModelInfo info)
            {
                Entry_Name = Value.name;
                Quantity = Value.qty.ToString();

            }

            private string entry_name;
            public string Entry_Name { get => entry_name;
                set {
                    entry_name = value;
                    Update("Entry_Name");
                } }

            private string qty;
            public string Quantity { get => qty;
                set {
                    qty = value;
                    Update("Quantity");
                } }
            //Item Selection Bindings
            private bool _isSelected;
            public bool IsSelected { get => _isSelected;
                set {
                    _isSelected = value;
                    Update("Hardware_Items");
            }}
        }

        /// <summary>
        /// Misc Hardware Entry
        /// </summary>
        public class LaborPresenter : Presenter
        {
            public LaborEntry Value;
            public LaborPresenter(LaborEntry value, ModelInfo info)
            {
                Value = value;
                RefreshDisplay(info);
            }

            public void RefreshDisplay(ModelInfo info)
            {
                Entry_Name = Value.EntryName;
                Per_Unit_Labor = Value.LaborData.PerUnitLabor.ToString();
                Letter_Code = Value.LaborData.LaborCodePair.Letter;
            }

            private string entry_name;
            public string Entry_Name { get => entry_name;
                set {
                    entry_name = value;
                    Update("Entry_Name");
                } }

            private string per_unit;
            public string Per_Unit_Labor { get => per_unit;
                set {
                    per_unit = value;
                    Update("Per_Unit_Labor");
                } }

            private string letter_code;
            public string Letter_Code { get => letter_code;
                set {
                    letter_code = value;
                    Update("Letter_Code");
                } }

            //Item Selection Bindings
            private bool _isSelected;
            public bool IsSelected { get => _isSelected;
                set {
                    _isSelected = value;
                    Update("Labor_Items");
            }}
        }

        // Electrical Room Presenters
        // panel backing Presenter
        public class ElecRoomPresenter : Presenter
        {
            public ElecRoom Value;
            public ElecRoomPresenter(ElecRoom value)
            {
                Value = value;
                RefreshDisplay();
            }

            public void RefreshDisplay()
            {
                RoomName = Value.RoomName;
                UnistrutCount = Value.Unistrut.Count().ToString();
                GrdBarCount = Value.GroundBar.Count().ToString();
                PbCount = Value.Panelboard.Count().ToString();
                BackingCount = Value.PanelBacking.Count().ToString();
                ConduitCount = Value.Conduit.Count().ToString();
            }

            private string name;
            public string RoomName { get => name;
                set {
                    name = value;
                    Update("RoomName");
                } }

            private string uni;
            public string UnistrutCount { get => uni;
                set {
                    uni = value;
                    Update("UnistrutCount");
                } }

            private string grd;
            public string GrdBarCount { get => grd;
                set {
                    grd = value;
                    Update("GrdBarCount");
                } }

            private string pb;
            public string PbCount { get => pb;
                set {
                    pb = value;
                    Update("PbCount");
                } }

            private string backing;
            public string BackingCount { get => backing;
                set {
                    backing = value;
                    Update("BackingCount");
                } }

            private string con;
            public string ConduitCount { get => con;
                set {
                    con = value;
                    Update("ConduitCount");
                } }

            //Item Selection Bindings
            private bool _isSelected;
            public bool IsSelected { get => _isSelected;
                set {
                    _isSelected = value;
                    Update("Elec_Room_Items");
            }}
        }

        // Unistrut Presenter
        public class UnistrutPresenter : Presenter
        {
            public Unistrut Value;
            public UnistrutPresenter(Unistrut value, ModelInfo info)
            {
                Value = value;
                RefreshDisplay(info);
            }

            public void RefreshDisplay(ModelInfo info)
            {
                Name = Value.Name;
                Length = RMeasure.LengthFromDbl(info.DOC, Value.Length);
                Size = Value.Size;
                Hardware_Cnt = Value.Hardware.FastenerCount.ToString();
            }

            private string name;
            public string Name { get => name;
                set {
                    name = value;
                    Update("Name");
                } }

            private string len;
            public string Length { get => len;
                set {
                    len = value;
                    Update("Length");
                } }

            private string size;
            public string Size { get => size;
                set {
                    size = value;
                    Update("Size");
                } }

            private string hdw_cnt;
            public string Hardware_Cnt { get => hdw_cnt;
                set {
                    hdw_cnt = value;
                    Update("Hardware_Cnt");
                } }

            //Item Selection Bindings
            private bool _isSelected;
            public bool IsSelected { get => _isSelected;
                set {
                    _isSelected = value;
                    Update("Unistrut_Items");
            }}
        }

        // Ground bar Presenter
        public class GrdBarPresenter : Presenter
        {
            public GroundBar Value;
            public GrdBarPresenter(GroundBar value, ModelInfo info)
            {
                Value = value;
                RefreshDisplay(info);
            }

            public void RefreshDisplay(ModelInfo info)
            {
                Name = Value.Name;
                Length = RMeasure.LengthFromDbl(info.DOC, Value.Length);
                Hardware_Cnt = Value.Hardware.HardwareCount.ToString();
                Dimensions =  Value.GetDimensions(info);
            }

            private string name;
            public string Name { get => name;
                set {
                    name = value;
                    Update("Name");
                } }

            private string len;
            public string Length { get => len;
                set {
                    len = value;
                    Update("Length");
                } }

            private string hdw_cnt;
            public string Hardware_Cnt { get => hdw_cnt;
                set {
                    hdw_cnt = value;
                    Update("Hardware_Cnt");
                } }

            private string dim;
            public string Dimensions { get => dim;
                set {
                    dim = value;
                    Update("Dimensions");
                } }

            //Item Selection Bindings
            private bool _isSelected;
            public bool IsSelected { get => _isSelected;
                set {
                    _isSelected = value;
                    Update("Grd_Bar_Items");
            } }
        }

        // panel backing Presenter
        public class BackingPresenter : Presenter
        {
            public PanelBacking Value;
            public BackingPresenter(PanelBacking value, ModelInfo info)
            {
                Value = value;
                RefreshDisplay(info);
            }

            public void RefreshDisplay(ModelInfo info)
            {
                Name = PanelBacking.Name;
                Hardware_Cnt = Value.Hardware.HardwareCount.ToString();
                Dimensions =  Value.GetDimensions(info);
            }

            private string name;
            public string Name { get => name;
                set {
                    name = value;
                    Update("Name");
                } }

            private string hdw_cnt;
            public string Hardware_Cnt { get => hdw_cnt;
                set {
                    hdw_cnt = value;
                    Update("Hardware_Cnt");
                } }

            private string dim;
            public string Dimensions { get => dim;
                set {
                    dim = value;
                    Update("Dimensions");
                } }

            //Item Selection Bindings
            private bool _isSelected;
            public bool IsSelected { get => _isSelected;
                set {
                    _isSelected = value;
                    Update("Backing_Items");
            }}
        }

        // panel backing Presenter
        public class PanelboardPresenter : Presenter
        {
            public Panelboard Value;
            public PanelboardPresenter(Panelboard value)
            {
                Value = value;
                RefreshDisplay();
            }

            public void RefreshDisplay()
            {
                Name = Value.Name;
                Amperage = Value.Amperage;
                Hardware_Cnt = Value.Hardware.HardwareCount.ToString();
            }

            private string name;
            public string Name { get => name;
                set {
                    name = value;
                    Update("Name");
                } }

            private string amps;
            public string Amperage { get => amps;
                set {
                    amps = value;
                    Update("Amperage");
                } }

            private string hdw_cnt;
            public string Hardware_Cnt { get => hdw_cnt;
                set {
                    hdw_cnt = value;
                    Update("Hardware_Cnt");
                } }

            //Item Selection Bindings
            private bool _isSelected;
            public bool IsSelected { get => _isSelected;
                set {
                    _isSelected = value;
                    Update("Backing_Items");
            }}
        }


        /// <summary>
        /// Misc Hardware Entry
        /// </summary>
        public class P3BoxPresenter : Presenter
        {
            public P3Box Value;
            public P3BoxPresenter(P3Box value, ModelInfo info)
            {
                Value = value;
                RefreshDisplay(info);
            }

            public void RefreshDisplay(ModelInfo info)
            {
                Box_Config = Value.Box_Config;
                Connected_Conduit_Count = Value.Connector_Count.ToString();
                Unplaced_Whips = (
                    Value.One_Circuit_Whip_Count +
                    Value.Two_Circuit_Whip_Count +
                    Value.Three_Circuit_Whip_Count -
                    Value.Circuit_Whips.Length).ToString();
                Box_Voltage = Value.Voltage;
            }

            private string entry_name = "P3 Box";
            public string Entry_Name { get => entry_name;
                set {
                    entry_name = value;
                    Update("Entry_Name");
                } }

            private string box_voltage;
            public string Box_Voltage { get => box_voltage;
            set {
                box_voltage = value;
                Update("Box_Voltage");
            } }

            private string config;
            public string Box_Config { get => config;
                set {
                    config = value;
                    Update("Box_Config");
                } }

            private string conn_conduit;
            public string Connected_Conduit_Count { get => conn_conduit;
                set {
                    conn_conduit = value;
                    Update("Connected_Conduit_Count");
                } }

            private string unplaced_whips;
            public string Unplaced_Whips { get => unplaced_whips;
            set {
                unplaced_whips = value;
                Update("Unplaced_Whips");
            } }

            //Item Selection Bindings
            private bool _isSelected;
            public bool IsSelected { get => _isSelected;
                set {
                    _isSelected = value;
                    Update("P3_Box_Items");
            }}
        }

        /// <summary>
        /// Misc Hardware Entry
        /// </summary>
        public class P3LightingFixturePresenter : Presenter
        {
            public P3LightingFixture Value;
            public P3LightingFixturePresenter(P3LightingFixture value, ModelInfo info)
            {
                /*
                Value = value;
                RefreshDisplay(info);
                */
            }

            public void RefreshDisplay(ModelInfo info)
            {
                /*
                Box_Config = Value.Box_Config;
                Connected_Conduit_Count = Value.Connector_Count.ToString();
                Unplaced_Whips = (
                    Value.One_Circuit_Whip_Count +
                    Value.Two_Circuit_Whip_Count +
                    Value.Three_Circuit_Whip_Count -
                    Value.Circuit_Whips.Length).ToString();
                Box_Voltage = Value.Voltage;
                */
            }

            private string entry_name = "P3 Box";
            public string Entry_Name { get => entry_name;
                set {
                    entry_name = value;
                    Update("Entry_Name");
                } }

            private string box_voltage;
            public string Box_Voltage { get => box_voltage;
            set {
                box_voltage = value;
                Update("Box_Voltage");
            } }

            private string config;
            public string Box_Config { get => config;
                set {
                    config = value;
                    Update("Box_Config");
                } }

            private string conn_conduit;
            public string Connected_Conduit_Count { get => conn_conduit;
                set {
                    conn_conduit = value;
                    Update("Connected_Conduit_Count");
                } }

            private string unplaced_whips;
            public string Unplaced_Whips { get => unplaced_whips;
            set {
                unplaced_whips = value;
                Update("Unplaced_Whips");
            } }

            //Item Selection Bindings
            private bool _isSelected;
            public bool IsSelected { get => _isSelected;
                set {
                    _isSelected = value;
                    Update("P3_Lighting_Fixture_Items");
            }}
        }

        /// <summary>
        /// Voltage Drop Rule Presenter
        /// </summary>
        public class VoltageDropPresenter : Presenter
        {
            public VoltageDropRule Value;
            public VoltageDropPresenter(VoltageDropRule value, ModelInfo info)
            {
                Value = value;
                RefreshDisplay(info);
            }

            public void RefreshDisplay(ModelInfo info)
            {
                Voltage = Value.Voltage;
                VoltageDropString = Value.ToString(info);
            }

            private string voltage;
            public string Voltage { get => voltage;
                set {
                    voltage = value;
                    Update("Voltage");
                } }

            private string voltage_drop_string;
            public string VoltageDropString { get => voltage_drop_string;
            set {
                voltage_drop_string = value;
                Update("VoltageDropString");
            } }

            //Item Selection Bindings
            private bool _isSelected;
            public bool IsSelected { get => _isSelected;
                set {
                    _isSelected = value;
                    Update("Voltage_Drop_Items");
            }}
        }

        /// <summary>
        /// Device Wire Pairing Presenter
        /// </summary>
        public class DevicePairingPresenter : Presenter
        {
            public LowVoltageDevicePairing Value;
            public DevicePairingPresenter(LowVoltageDevicePairing value)
            {
                Value = value;
                RefreshDisplay();
            }

            public void RefreshDisplay()
            {
                PanelName = Value.PanelName;
                DeviceNumber = Value.DeviceNumber;
                WireNumber = Value.WireNumber;
            }

            private string pname;
            public string PanelName { get => pname;
                set {
                    pname = value;
                    Update("PanelName");
                } }

            private string dnum;
            public string DeviceNumber { get => dnum;
            set {
                dnum = value;
                Update("DeviceNumber");
            } }

            private string wnum;
            public string WireNumber { get => wnum;
            set {
                wnum = value;
                Update("WireNumber");
            } }

            //Item Selection Bindings
            private bool _isSelected;
            public bool IsSelected { get => _isSelected;
                set {
                    _isSelected = value;
                    Update("Device_Pairing_Items");
            }}
        }

        /// <summary>
        /// Device Wire Pairing Presenter
        /// </summary>
        public class WirePairingPresenter : Presenter
        {
            public LowVoltageWirePairing Value;
            public WirePairingPresenter(LowVoltageWirePairing value)
            {
                Value = value;
                RefreshDisplay();
            }

            public void RefreshDisplay()
            {
                WireNumber = Value.WireNumber;
                WireNames = string.Join(", ", Value.WireNames);
            }

            private string wnum;
            public string WireNumber { get => wnum;
            set {
                wnum = value;
                Update("WireNumber");
            } }

            private string wnames;
            public string WireNames { get => wnames;
            set {
                wnames = value;
                Update("WireNames");
            } }

            //Item Selection Bindings
            private bool _isSelected;
            public bool IsSelected { get => _isSelected;
                set {
                    _isSelected = value;
                    Update("Wire_Pairing_Items");
            }}
        }
    }

    /// <summary>
    /// Default Presenter: Just Presents a string value as a listbox item,
    /// can replace with an object for more complex listbox databindings
    /// </summary>
    public class ItemPresenter : Presenter
    {
        private readonly string _value;
        public ItemPresenter(string value) => _value = value;
    }

    #region Inherited Classes
    public abstract class Presenter : INotifyPropertyChanged
    {
         public event PropertyChangedEventHandler PropertyChanged;

         public void Update(string val) => RaisePropertyChanged(val);

         protected void RaisePropertyChanged([CallerMemberName] string propertyName = null)
         {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
         }
    }
    #endregion
}
