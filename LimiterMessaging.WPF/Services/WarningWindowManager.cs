using AppLimiterLibrary.Data;
using AppLimiterLibrary.Dtos;
using LimiterMessaging.WPF.Converters;
using LimiterMessaging.WPF.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace LimiterMessaging.WPF.Services
{
    public class WarningWindowManager
    {
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private Application _app;

        private Application CreateApplication()
        {
            var app = new Application();
            var resources = new ResourceDictionary();

            // Add required resources
            resources.Add("BoolToVis", new BooleanToVisibilityConverter());
            resources.Add("PlayPauseConverter", new PlayPauseConverter());

            app.Resources = resources;
            return app;
        }

        public async Task ShowWarning(
            MotivationalMessage message,
            string warning,
            string processName,
            string computerId,
            Action<string, bool> updateIgnoreStatus,
            AppRepository appRepo,
            MotivationalMessageRepository messageRepo,
            SettingsRepository settingsRepo,
            List<MotivationalMessage> messagesSent)
        {
            await _semaphore.WaitAsync();
            try
            {
                if (Application.Current == null)
                {
                    var thread = new Thread(() =>
                    {
                        _app = CreateApplication();

                        var window = new MessagingWindow(
                            message,
                            warning,
                            processName,
                            computerId,
                            updateIgnoreStatus,
                            appRepo,
                            messageRepo,
                            settingsRepo,
                            messagesSent);

                        window.Closed += (s, e) =>
                        {
                            window.Dispatcher.InvokeShutdown();
                        };

                        window.Show();
                        System.Windows.Threading.Dispatcher.Run();
                    });

                    thread.SetApartmentState(ApartmentState.STA);
                    thread.Start();
                    thread.Join(); // Wait for window to close
                }
                else
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        var window = new MessagingWindow(
                            message,
                            warning,
                            processName,
                            computerId,
                            updateIgnoreStatus,
                            appRepo,
                            messageRepo,
                            settingsRepo,
                            messagesSent);
                        window.Show();
                    });
                }
            }
            catch (Exception ex)
            {
                // Reset cache on error
                updateIgnoreStatus(processName, false);
                throw;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public void Cleanup()
        {
            if (_app != null)
            {
                _app.Dispatcher.InvokeShutdown();
                _app = null;
            }
        }
    }
}
