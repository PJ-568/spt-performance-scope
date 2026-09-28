using BepInEx.Logging;

namespace PerformanceScope
{
    /// <summary>
    /// 统一的日志出口。普通日志受配置开关控制，错误日志始终输出以便排障。
    /// Unified logging outlet. Normal logs are gated by config switches; error logs are always emitted for troubleshooting.
    /// </summary>
    internal static class Log
    {
        private static ManualLogSource _logger;

        /// <summary>
        /// 注入 BepInEx 提供的日志源。
        /// Injects the BepInEx-provided log source.
        /// </summary>
        public static void Init(ManualLogSource logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 常规信息，仅在启用日志时输出。
        /// Informational message, emitted only when logging is enabled.
        /// </summary>
        public static void Info(string message)
        {
            if (_logger == null || PluginConfig.EnableLogging?.Value != true)
            {
                return;
            }

            _logger.LogInfo(message);
        }

        /// <summary>
        /// 诊断信息，需同时启用日志与详细日志。
        /// Diagnostic message, requires both Enable Logging and Verbose Logging.
        /// </summary>
        public static void Verbose(string message)
        {
            if (_logger == null
                || PluginConfig.EnableLogging?.Value != true
                || PluginConfig.VerboseLogging?.Value != true)
            {
                return;
            }

            _logger.LogInfo(message);
        }

        /// <summary>
        /// 警告，仅在启用日志时输出。
        /// Warning, emitted only when logging is enabled.
        /// </summary>
        public static void Warn(string message)
        {
            if (_logger == null || PluginConfig.EnableLogging?.Value != true)
            {
                return;
            }

            _logger.LogWarning(message);
        }

        /// <summary>
        /// 错误，始终输出。
        /// Error, always emitted.
        /// </summary>
        public static void Error(string message)
        {
            if (_logger == null)
            {
                return;
            }

            _logger.LogError(message);
        }
    }
}
