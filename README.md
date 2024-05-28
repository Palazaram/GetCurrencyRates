# 1. Installation and uninstallation procedures

In order to install the service to obtain currency rates, you need to open a command prompt as administrator and, depending on the task, enter such commands:

## Creating a service

Use the `sc create ServiceName` command, specifying the name of the service and the path to the exe file in quotes.

Example:

sc create GetCurrencyRate binPath= "C:\Users\Anton\Desktop\WindowsServise\GetCurrencyRates\bin\Debug\GetCurrencyRates.exe"

## Service Startup

Once you have created the service, you need to start it using the `sc start ServiceName` command.

Example:

sc start GetCurrencyRate

## Service stop

To stop the service, use the `sc stop ServiceName` command.

Example:

sc stop GetCurrencyRate

## Service Deletion

To delete a service, use the `sc delete ServiceName` command.

Example:

sc delete GetCurrencyRate

# 2. Configuration options and how to customize them

The service for obtaining exchange rates supports setting the following configuration parameters:

- **IntervalMinutes:** The time interval in minutes, after which the service will make a request to the API to get the current exchange rate data:
- **ApiUrl:** URL of the API, from which the service will receive data on exchange rates.

## Configuring parameters via configuration file

These parameters are set in the App.config configuration file, which is located in the same directory as the service executable.

# 3. Dependencies and external libraries used

The service for getting currency rates is developed using standard .NET Framework libraries and has no external dependencies. The main standard libraries used in the project are listed below:

## 1. System.Net.Http

- Used to make HTTP requests to the exchange rate API:
- The HttpClient class is used for asynchronous requests and processing HTTP responses.

## 2. System.IO

- Used for file system operations, including reading and writing files;
- The File, StreamReader and StreamWriter classes are used to read and write data to a file.

## 3. System.Timers

- Used to create timers that periodically make requests to the API;
- The Timer class is used to trigger events at specified intervals.

## 4. System.IO.Pipes

- It is used to implement Named Pipes for dynamic configuration of service parameters;
- NamedPipeServerStream and NamedPipeClientStream classes are used to organize communication through named channels.

## 5. System.Configuration

- It is used to work with configuration files;
- Класс ConfigurationManager используется для чтения и обновления значений в конфигурационном файле App.config.

# 4. Usage instructions for control codes.

## Changing the data retrieval interval

To change the request interval (in minutes), send the command through the named channel as follows:

- The service should already be running. If it is not running, start it;
- Enter the command: `echo interval=5 > \\.\pipe\GetCurrencyRatesPipe`
- **Only the interval time can be changed in the command.**

## API URL changes

To change the API URL, send the command through a named channel as follows:

- The service should already be running. If it is not running, start it;
- Enter the command:
  
  `echo apiurl=https://bank.gov.ua/NBUStatService/v1/statdirectory/exchange^?json > \\.\pipe\GetCurrencyRatesPipe`

  or `echo apiurl=(https://bank.gov.ua/NBU_uonia?id_api=REF-SWAP_Swaps^&json > \\.\pipe\GetCurrencyRatesPipe`

- **Only the URL of the API can be changed in the command.**
- **You need to insert the “^” character before the '?' or '&' character in the URL API.”**

## Saving JSON file

The JSON file is saved in the folder with the exe file with which you created the service.

# 5. Troubleshooting tips and common issues

## 1. Using API other than NBU

- If you decide to use another provider's API instead of the API of the National Bank of Ukraine (NBU), make sure that the API response format and data structure match what is expected in the code. Other APIs may return data in a different format, which may cause errors when processing and saving data.
- Check the URL and request parameters. Some APIs may require additional parameters or access tokens.
- Make sure the new API maintains a stable connection and does not impose restrictions on the frequency of requests, which may result in IP address blocking.

## 2. Connectivity issues

- If the service cannot connect to the API, check the internet connection and availability of the API server.
- Check that the URLs in the configuration file are correct. Make sure there are no typos and that the URL is accessible through a browser.
- If an HttpRequestException error occurs, the service will automatically retry the connection. If the error persists, check the log files for more information.

## 3. Problems with the configuration file

- If changes to the configuration file are not applied, make sure the service has been restarted after making the changes. The service loads settings from the configuration file at startup.
- Ensure that the parameter values are correct. For example, the data update interval must be a positive number.

## 4. Errors of writing to the file

- Make sure that the service has the necessary access rights to write to the directory where the data and log files are saved.
- Check for available disk space.

## 5. Problems with Named Pipes (Named Pipes)

- Make sure you are entering the command to change the parameters correctly. For the Windows command line, make sure you escape special characters (e.g., & should be replaced with ^&).
- If the error The system cannot find the file specified occurs, make sure that the named channel has been created and the service is running. Try restarting the service.

## 6. Common problems and debugging

- Carefully study the messages in the log files. They contain important information about the service operation and may indicate the cause of the error.
- If the service stops unexpectedly, check the Windows system logs for more information about failures or exceptions.
- If debugging and testing is required, temporarily enable more detailed logging.


