using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.UI;
using SmartViewFilter.Revit.Infrastructure;
using SmartViewFilter.Revit.Services;
using SmartViewFilter.Revit.ViewModels;
using SmartViewFilter.Revit.Views;

namespace SmartViewFilter.Revit
{
    internal sealed class SmartFilterHost : IDisposable
    {
        private MainWindow _window;
        private ExternalEvent _externalEvent;
        private SmartFilterExternalEventHandler _handler;

        public void Show(UIApplication uiApplication)
        {
            if (uiApplication?.ActiveUIDocument?.Document == null)
            {
                TaskDialog.Show(Constants.AppName, "Open a Revit document before starting Smart View Filter.");
                return;
            }

            if (_window != null)
            {
                _window.Activate();
                return;
            }

            _handler = new SmartFilterExternalEventHandler();
            _externalEvent = ExternalEvent.Create(_handler);
            var filterService = new RevitFilterService(new FilterEngine());
            var configurationStore = new ConfigurationStore();

            var viewModel = new MainViewModel(
                readSelection =>
                {
                    if (_handler == null || _externalEvent == null)
                    {
                        return;
                    }

                    _handler.Update(application =>
                    {
                        var dataService = new RevitDataService(application);
                        var records = dataService.ReadSelectedElements();
                        _window?.Dispatcher.Invoke(() => readSelection(records));
                    });
                    _externalEvent.Raise();
                },
                (request, reportResult) =>
                {
                    if (_handler == null || _externalEvent == null)
                    {
                        return;
                    }

                    _handler.Update(application =>
                    {
                        SmartFilterResult result;
                        try
                        {
                            result = filterService.Execute(application, request);
                        }
                        catch (Exception ex)
                        {
                            result = SmartFilterResult.Error(ex.Message);
                        }

                        _window?.Dispatcher.Invoke(() => reportResult(result));
                    });
                    _externalEvent.Raise();
                },
                configurationStore);

            _window = new MainWindow
            {
                DataContext = viewModel
            };

            _window.Closed += (_, _) =>
            {
                ReleaseWindowResources();
            };

            _window.Show();
        }

        public void Dispose()
        {
            _window?.Close();
            ReleaseWindowResources();
        }

        private void ReleaseWindowResources()
        {
            _window = null;
            _externalEvent?.Dispose();
            _externalEvent = null;
            _handler = null;
        }
    }
}
