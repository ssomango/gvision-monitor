using GVisionWpf.DomainLayer.Extensions;
using GVisionWpf.UIs.UiUpdaters;
using Microsoft.Extensions.DependencyInjection;
using System.Configuration;
using System.Data.SQLite;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using GVisionWpf.Api;
using LoadingWindow = GVisionWpf.UIs.Frames.Windows.LoadingWindow;

namespace GVisionWpf
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private ApiServer? _apiServer;
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            checkAndCloseBackgroundInstances();

            subscribeUnhandledException();

            // Start the API server
            _apiServer = ApiServer.Instance;
            await _apiServer.StartAsync();
            //Debug.WriteLine("API server started successfully");

            LoadingWindow loadingWindow = new LoadingWindow();
            loadingWindow.Show();

            await loadingWindow.LoadSystem(5000);
            await ensureDataColumnExists();

            Dispatcher.Invoke(() =>
            {
                var mainWindow = new MainWindow();
                Application.Current.MainWindow = mainWindow;
                Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
                mainWindow.Show();

                loadingWindow.Close();
            });
        }

        private void checkAndCloseBackgroundInstances()
        {
            var processes = Process.GetProcessesByName("GVisionWpf");

            if (processes.Count() > 1)
            {
                var result = MessageBox.Show(
                    "The application is already running in the background.\nDo you want to close the other instance?",
                    "Duplicate Instance Detected",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning
                    );

                if (result == MessageBoxResult.Yes)
                {
                    var currentProcess = Process.GetCurrentProcess();

                    foreach (var process in processes)
                    {
                        if (process.Id != currentProcess.Id)
                            process.Kill();
                    }
                }
                else
                {
                    Application.Current.Shutdown();
                }
            }
        }

        private void configureServices(IServiceCollection services)
        {
            //services.AddSingleton<MainWindow>();
        }

        private void subscribeUnhandledException()
        {
            // UI thread에서 발생하는 예외 처리
            DispatcherUnhandledException += onDispatcherUnhandledException;
            // 모든 스레드에서 발생하는 처리되지 않은 예외 처리
            AppDomain.CurrentDomain.UnhandledException += onCurrentDomainUnhandledException;

            // 비동기 작업에서 발생하고 관찰되지 않은 예외 처리
            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                e.SetObserved();
                GlobalErrorHandler.HandleException(e.Exception);
            };
        }

        private static void onDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            GlobalErrorHandler.HandleException(e.Exception);
            e.Handled = true;
        }

        private static void onCurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            GlobalErrorHandler.HandleException(e.ExceptionObject as Exception);
        }

        private async Task ensureDataColumnExists()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;

            using (var connection = new SQLiteConnection(connectionString))
            {
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText = "PRAGMA table_info(emap);";
                using (var reader = await command.ExecuteReaderAsync())
                {
                    bool hasDataColumn = false;
                    while (await reader.ReadAsync())
                    {
                        if (reader["name"].ToString().ToLower() == "data")
                        {
                            hasDataColumn = true;
                            break;
                        }
                    }

                    if (!hasDataColumn)
                    {
                        var alterCommand = connection.CreateCommand();
                        alterCommand.CommandText = "ALTER TABLE Emap ADD COLUMN Data INTEGER DEFAULT 1;";
                        await alterCommand.ExecuteNonQueryAsync();
                    }
                }
            }
        }
    }
}