using System.Text;

namespace nexxtporter
{
    class LoggerConfig
    {
        public bool? Echo { get; set; }
        public string? LogFile { get; set; }
    }

    class Logger
    {
        public Logger(LoggerConfig? config = null)
        {
            if (config != null)
            {
                _config = config;
            }
            else
            {
                _config = new LoggerConfig();
                _config.Echo = true;
                _config.LogFile = null;
            }
        }

        private LoggerConfig _config;
        private StringBuilder _log = new StringBuilder();
        private StringBuilder _errors = new StringBuilder();

        public void Log(string data)
        {
            _log.AppendLine(data);
            if (_config.Echo.HasValue && _config.Echo.Value)
            {
                Console.WriteLine(data);
            }
        }

        public void LogError(string message, Exception? e)
        {
            _log.AppendLine(message);
            _errors.AppendLine(message);
            if (_config.Echo.HasValue && _config.Echo.Value)
            {
                Console.WriteLine(string.Format("ERROR: {0}", message));
            }
            if (e != null)
            {
                _log.AppendLine("Exception Info:");
                _log.AppendLine(e.ToString());
                _errors.AppendLine("Exception Info:");
                _errors.AppendLine(e.ToString());
                if (_config.Echo.HasValue && _config.Echo.Value)
                {
                    Console.WriteLine("Exception Info:");
                    Console.WriteLine(e.ToString());
                }
            }
        }

        public async Task WriteLogFile()
        {
            if (string.IsNullOrWhiteSpace(_config.LogFile))
            {
                return;
            }

            StreamWriter? writer = null;
            try
            {
                writer = new StreamWriter(_config.LogFile, false);
                await writer.WriteLineAsync("NexxtPorter Log");
                await writer.WriteLineAsync("--------------------------------------------------------------------------------");
                await writer.WriteAsync(_log.ToString());
                await writer.WriteLineAsync("ERRORS");
                await writer.WriteLineAsync("--------------------------------------------------------------------------------");
                await writer.WriteAsync(_errors.ToString());
                await writer.FlushAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(string.Format("Failed to write log file {0}", _config.LogFile));
                Console.WriteLine(e.ToString());
            }
            finally
            {
                if (writer != null)
                {
                    writer.Dispose();
                }
            }
        }
    }
}
