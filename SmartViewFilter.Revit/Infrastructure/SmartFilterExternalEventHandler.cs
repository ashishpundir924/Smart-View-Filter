using System;
using Autodesk.Revit.UI;

namespace SmartViewFilter.Revit.Infrastructure
{
    internal sealed class SmartFilterExternalEventHandler : IExternalEventHandler
    {
        private readonly object _sync = new object();
        private Action<UIApplication> _pendingWork;

        public string GetName()
        {
            return "Smart View Filter";
        }

        public void Update(Action<UIApplication> action)
        {
            lock (_sync)
            {
                _pendingWork = action;
            }
        }

        public void Execute(UIApplication app)
        {
            Action<UIApplication> pendingWork;
            lock (_sync)
            {
                pendingWork = _pendingWork;
                _pendingWork = null;
            }

            pendingWork?.Invoke(app);
        }
    }
}
