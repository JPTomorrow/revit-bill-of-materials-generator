using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using JPMorrow.Data.Globals;
using JPMorrow.Revit.Measurements;
using JPMorrow.Revit.VoltageDrop;
using JPMorrow.Tools.Diagnostics;

namespace JPMorrow.UI.ViewModels
{
    public partial class ParentViewModel
    {
        // Add Voltage Drop Rule
        public void AddVoltageDropRule(Window window)
        {
            try
            {
                var voltage = Panel_Voltage_Items[Sel_Voltage_Drop_Voltage];
                var wire_size = Dist_Wire_Size_Items[Sel_Voltage_Drop_Wire_Size];
                var mind = RMeasure.LengthDbl(ALS.Info.DOC, Voltage_Drop_Min_Distance_Txt);
                var maxd = RMeasure.LengthDbl(ALS.Info.DOC, Voltage_Drop_Max_Distance_Txt);
                
                var vd_rule = VoltageDropRule.DeclareRule(mind, maxd, wire_size, voltage);

                var idx = ALS.AppData.VoltageDropRules
                    .FindIndex(x =>
                               x.Voltage.Equals(vd_rule.Voltage) &&
                               (x.IsInRange(vd_rule.MinDistance) ||
                               x.IsInRange(vd_rule.MaxDistance)));

                if(idx == -1) {

                    var rules = new List<VoltageDropRule>();
                    rules.AddRange(ALS.AppData.VoltageDropRules);
                    rules.Add(vd_rule);
                    ALS.AppData.VoltageDropRules.Clear();
                    
                    rules = rules
                        .OrderBy(x => x.Voltage)
                        .ThenBy(x => x.MinDistance).ToList();

                    ALS.AppData.VoltageDropRules.AddRange(rules);

                    debugger.show(err:string.Join("\n", ALS.AppData.VoltageDropRules.Select(x => x.ToString(ALS.Info))));
                    Voltage_Drop_Items.Clear();
                    ALS.AppData.VoltageDropRules.ForEach(x => Voltage_Drop_Items.Add(new VoltageDropPresenter(x, ALS.Info)));
                    Update("Voltage_Drop_Items");
                }
                else {
                    
                    debugger.show(err:
                                  "The following rule conflicts with the rule you are trying to make:\n\n" +
                                  ALS.AppData.VoltageDropRules[idx].Voltage + " " +  ALS.AppData.VoltageDropRules[idx].ToString(ALS.Info) + "\n\n" +
                                  "The range for a rule may not lie within the range of any already created rules.");
                }
            }
            catch(Exception ex)
            {
                debugger.show(err:ex.ToString());
            }
        }

        // Add Voltage Drop Rule
        public void RemoveVoltageDropRule(Window window)
        {
            try
            {
                var selected = Voltage_Drop_Items.Where(x => x.IsSelected).ToList();

                if(!selected.Any()) return;

                selected.ForEach(x => ALS.AppData.VoltageDropRules.Remove(x.Value));
                Voltage_Drop_Items.Clear();
                ALS.AppData.VoltageDropRules.ForEach(x => Voltage_Drop_Items.Add(new VoltageDropPresenter(x, ALS.Info)));
                Update("Voltage_Drop_Items");
                
            }
            catch(Exception ex)
            {
                debugger.show(err:ex.ToString());
            }
        }
	}
}
