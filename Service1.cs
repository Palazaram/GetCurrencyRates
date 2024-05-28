using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Configuration;
using System.Timers;
using System.IO.Pipes;
using System.Text.RegularExpressions;


namespace GetCurrencyRates
{
    public partial class Service1 : ServiceBase
    {
        private readonly HttpClient _httpClient;    // HTTP client for API requests
        private string _apiUrl;                     // API URL for currency rates
        private System.Timers.Timer _timer;         // Timer for periodic execution
        private int _intervalMinutes;               // Interval in minutes for timer

        public Service1()
        {
            InitializeComponent();
            _httpClient = new HttpClient();         // Initialize HTTP client
        }

        protected override async void OnStart(string[] args)
        {
            LogMessage("Service started.");

            _intervalMinutes = GetConfigurationValue("IntervalMinutes", 1); // Get interval from config. Second default parameter if there is no value in the config file
            _apiUrl = ConfigurationManager.AppSettings["ApiUrl"];           // Get API URL from config

            int intervalMilliseconds = _intervalMinutes * 60 * 1000;        // Convert minutes to milliseconds

            // Initialize and start the timer
            _timer = new System.Timers.Timer(intervalMilliseconds);
            _timer.Elapsed += Timer_Elapsed; // Set timer event handler
            _timer.Start();

            await GetCurrencyRates(); // Fetch data immediately on start

            StartNamedPipeServer();   // Start named pipe server for command input
        }

        protected override void OnStop()
        {
            LogMessage("Service stopped.");
            _timer.Stop();
            _timer.Dispose();
            _httpClient.Dispose();
        }

        // Get configuration value or default if not present or invalid
        private int GetConfigurationValue(string key, int defaultValue)
        {
            if (int.TryParse(ConfigurationManager.AppSettings[key], out int value))
                return value;
            return defaultValue;
        }

        private async void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            await GetCurrencyRates(); // Fetch currency rates on timer event
        }

        private async Task GetCurrencyRates()
        {
            const int maxRetries = 3;                   // Max retries for fetching data
            int retryCount = 0;                         // Retry counter
            TimeSpan delay = TimeSpan.FromSeconds(2);   // Initial delay between retries

            if (string.IsNullOrWhiteSpace(_apiUrl))
            {
                LogError("API URL is empty or null.");  // Log error if URL is invalid
                return;
            }

            while (retryCount < maxRetries)
            {
                try
                {
                    // Make HTTP request to fetch data
                    var response = await _httpClient.GetAsync(_apiUrl);
                    response.EnsureSuccessStatusCode(); // Ensure response is successful

                    var responseBody = await response.Content.ReadAsStringAsync(); // Read response content
                    var directoryPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                    var filePath = Path.Combine(directoryPath, "currency_rates.json");

                    await Task.Run(() => File.WriteAllText(filePath, responseBody)); // Save data to file
                    LogMessage($"Data successfully saved to file: {filePath}");
                    return;
                }
                catch (HttpRequestException e)
                {
                    retryCount++;
                    LogError($"HttpRequestException (attempt {retryCount}): {e.Message}");

                    if (retryCount >= maxRetries)
                    {
                        LogError($"Failed to get data after {maxRetries} attempts.");
                        return;
                    }

                    await Task.Delay(delay); // Wait before retrying
                    delay = TimeSpan.FromTicks(delay.Ticks * 2); // Exponential backoff
                }
                catch (Exception ex)
                {
                    LogError($"Unexpected exception: {ex.Message}");
                    return;
                }
            }
        }

        // Start named pipe server for receiving commands
        private void StartNamedPipeServer()
        {
            var pipeServer = new NamedPipeServerStream("GetCurrencyRatesPipe", PipeDirection.In);
            var serverThread = new Thread(() =>
            {
                while (true)
                {
                    try
                    {
                        pipeServer.WaitForConnection();                     // Wait for connection from client
                        var streamReader = new StreamReader(pipeServer);    
                        var commandLine = streamReader.ReadLine();          // Read command from pipe
                        if (!string.IsNullOrEmpty(commandLine))
                        {
                            ProcessCommandLine(commandLine);                // Process command line input
                        }
                        pipeServer.Disconnect();                            // Disconnect pipe after processing
                    }
                    catch (Exception ex)
                    {
                        LogError($"Error in named pipe server: {ex.Message}");
                    }
                }
            });
            serverThread.Start(); // Start server thread
        }

        // Process command received from named pipe
        private void ProcessCommandLine(string commandLine)
        {
            var regex = new Regex(@"(\w+)\s*=\s*(\S+)");
            var matches = regex.Matches(commandLine);
            foreach (Match match in matches)
            {
                var key = match.Groups[1].Value.ToLower();
                var value = match.Groups[2].Value;

                switch (key)
                {
                    case "interval":
                        if (int.TryParse(value, out int newInterval))
                        {
                            if (newInterval > 0)
                            {
                                UpdateInterval(newInterval); // Update timer interval
                            }
                            else
                            {
                                LogError("Invalid interval value. It must be greater than 0.");
                            }
                        }
                        else
                        {
                            LogError("Invalid interval value. It must be an integer.");
                        }
                        break;
                    case "apiurl":
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            UpdateApiUrl(value); // Update API URL
                        }
                        else
                        {
                            LogError("API URL cannot be empty.");
                        }
                        break;
                    default:
                        LogError($"Unknown command: {key}");
                        break;
                }
            }
        }

        // Update timer interval
        private void UpdateInterval(int newInterval)
        {
            _intervalMinutes = newInterval;
            _timer.Interval = _intervalMinutes * 60 * 1000;
            LogMessage($"Interval updated to {_intervalMinutes} minutes.");
            UpdateAppConfig("IntervalMinutes", _intervalMinutes.ToString()); // Save to config
        }

        // Update API URL
        private void UpdateApiUrl(string newApiUrl)
        {
            _apiUrl = newApiUrl;
            LogMessage($"API URL updated to {_apiUrl}.");
            UpdateAppConfig("ApiUrl", _apiUrl); // Save to config
        }

        // Update app configuration
        private void UpdateAppConfig(string key, string value)
        {
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var settings = config.AppSettings.Settings;

            if (settings[key] == null)
            {
                settings.Add(key, value);    // Add new setting if not exist
            }
            else
            {
                settings[key].Value = value; // Update existing setting
            }

            config.Save(ConfigurationSaveMode.Modified);        // Save configuration changes
            ConfigurationManager.RefreshSection("appSettings"); // Refresh config section
        }

        private void LogError(string message)
        {
            LogMessage($"ERROR: {message}");
        }

        // Log message to file
        private void LogMessage(string message)
        {
            try
            {
                var logDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                var logFilePath = Path.Combine(logDirectory, "service_log.txt");
                var logMessage = $"{DateTime.Now}: {message}{Environment.NewLine}";

                File.AppendAllText(logFilePath, logMessage); // Append message to log file
            }
            catch (Exception ex)
            {
                try
                {
                    var logDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                    var errorLogFilePath = Path.Combine(logDirectory, "log_error.txt");
                    var errorLogMessage = $"{DateTime.Now}: Error logging message: {ex.Message}{Environment.NewLine}";

                    File.AppendAllText(errorLogFilePath, errorLogMessage); // Append error to error log
                }
                catch
                {
                    // Ignore further exceptions to prevent recursive failures
                }
            }
        }
    }
}
