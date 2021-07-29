

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Linq;
using JPMorrow.Tools.Diagnostics;

namespace JPMorrow.Revit.ConduitRuns
{
    public static class ConduitParameter
    {
        private static ConduitRunToFromPush handler_conduit_run_to_from_push = null;
        private static ExternalEvent exEvent_conduit_run_to_from_push = null;

        // This is a NHA project function
        // tag all the low voltage devices with a standard device ID based on parameter data that they already have
        public static async void PushToFromParam(Document doc, IEnumerable<ConduitRunInfo> runs) 
        {
            foreach(var cri in runs)
            {
                var ids = cri.WireIds.ToList();
                var idx = ids.FindIndex(x =>
                {
                    var el = doc.GetElement(new ElementId(x));
                    bool s = !string.IsNullOrWhiteSpace(el.LookupParameter("From").AsString());
                    s = !string.IsNullOrWhiteSpace(el.LookupParameter("To").AsString());
                    return s;
                });

                if(idx == -1)
                {
                    debugger.show(err:"There is no valid to/from filled in for this conduit in the model on any sub-section. skipping.");
                    continue;
                }
                else
                {
                    var id = ids[idx];
                    var el = doc.GetElement(new ElementId(id));
                    var to = el.LookupParameter("To").AsString();
                    var from = el.LookupParameter("From").AsString();
                    await PushToFrom(doc, to, from, ids.Select(x => new ElementId(x)));
                }
            }
        }
        
        // Sign up the event handlers
        public static void ConduitPushToFromParamSignUp()
        {
            handler_conduit_run_to_from_push = new ConduitRunToFromPush();
            exEvent_conduit_run_to_from_push = ExternalEvent.Create(handler_conduit_run_to_from_push.Clone() as IExternalEventHandler);
        }

        // Create a strut hanger in the revit model and bind it to this program
        private static async Task PushToFrom(Document doc, string to_param, string from_param, IEnumerable<ElementId> push_ids)
        {

            handler_conduit_run_to_from_push.Doc = doc;
            handler_conduit_run_to_from_push.To = to_param;
            handler_conduit_run_to_from_push.From = from_param;
            handler_conduit_run_to_from_push.IdsToPropogate = push_ids.ToList();
            exEvent_conduit_run_to_from_push.Raise();

            while (exEvent_conduit_run_to_from_push.IsPending)
            {
                await Task.Delay(100);
            }
        }

        private class ConduitRunToFromPush : IExternalEventHandler, ICloneable
        {
            public Document Doc { get; set; }
            public List<ElementId> IdsToPropogate { get; set; }
            public string To { get; set; }
            public string From { get; set; }

            public object Clone() => this;

            public void Execute(UIApplication app)
            {
                try
                {
                    using TransactionGroup tx_grp = new TransactionGroup(Doc, "Fix Conduit To/Froms grp");
                    using Transaction tx = new Transaction(Doc, "Fix Conduit To/Froms");
                    tx_grp.Start();

                    foreach(var id in IdsToPropogate)
                    {   
                        tx.Start();
                        var el = Doc.GetElement(id);
                        var to = el.LookupParameter("To");
                        var from = el.LookupParameter("From");
                        to.Set(To);
                        from.Set(From);
                        tx.Commit();
                    }

                    tx_grp.Assimilate();
                }
                catch (Exception ex)
                {
                    debugger.show(header: "Fix Conduit To/From", err: ex.ToString());
                }
            }

            public string GetName()
            {
                return "Fix Conduit To/Froms";
            }
        }
    }
}