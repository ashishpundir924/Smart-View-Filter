using System;
using System.Linq;
using System.Reflection;
using Autodesk.Revit.UI;

namespace SmartViewFilter.Revit
{
    public class App : IExternalApplication
    {
        internal static SmartFilterHost Host { get; private set; }

        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                try
                {
                    application.CreateRibbonTab(Constants.RibbonTabName);
                }
                catch
                {
                    // The tab may already exist when several internal tools share it.
                }

                RibbonPanel panel = application
                    .GetRibbonPanels(Constants.RibbonTabName)
                    .FirstOrDefault(p => p.Name == Constants.RibbonPanelName)
                    ?? application.CreateRibbonPanel(Constants.RibbonTabName, Constants.RibbonPanelName);

                string assemblyPath = Assembly.GetExecutingAssembly().Location;
                var buttonData = new PushButtonData(
                    "SmartViewFilterCommand",
                    "Live\nFilter",
                    assemblyPath,
                    typeof(Command).FullName);

                buttonData.ToolTip = "Open Smart View Filter for modeless selection-based Revit filtering.";

                if (!panel.GetItems().Any(item => item.Name == buttonData.Name))
                {
                    panel.AddItem(buttonData);
                }

                Host = new SmartFilterHost();
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show(Constants.AppName, "Failed to load ribbon button:\n" + ex.Message);
                return Result.Failed;
            }
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            Host?.Dispose();
            Host = null;
            return Result.Succeeded;
        }
    }
}
