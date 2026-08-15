using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PublisherStudio.BusinessObjects;
using PublisherStudio.BusinessObjects.Enums;
using PublisherStudio.Services.Logging;

namespace PublisherStudio.Services;

/// <summary>Configures PublisherStudio's optional application logging providers from the same LoggingCore contract used by the companion app.</summary>
public sealed class LoggingConfigurationService(
    IServiceCollection services,
    IConfiguration configuration,
    ILogger logger)
{
    public void Configure(ILoggingBuilder loggingBuilder)
    {
        ArgumentNullException.ThrowIfNull(loggingBuilder);
        try
        {
            logger.LogInformation("Configuring PublisherStudio logging providers.");
            services.AddOptions<LoggingCoreOptions>().Bind(configuration.GetSection("LoggingCore"));
            services.AddOptions<FileLoggerCoreOptions>().Bind(configuration.GetSection("LoggingCore:FileCore"));

            var loggingOptions = configuration.GetSection("LoggingCore").Get<LoggingCoreOptions>() ?? new LoggingCoreOptions();
            if (loggingOptions.CoreLogLevel == CoreLogLevel.None)
            {
                logger.LogInformation("Optional PublisherStudio logging providers are disabled by configuration.");
                return;
            }

            if (loggingOptions.FileCore.CoreLogLevel != CoreLogLevel.None)
            {
                services.AddSingleton<ILoggerProvider, FileLoggerProvider>();
                logger.LogInformation(
                    "Registered PublisherStudio file logger provider for {LogPath}.",
                    loggingOptions.FileCore.ResolvePath());
            }

            logger.LogInformation("Configured the enabled PublisherStudio logging providers.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not configure PublisherStudio logging providers; startup will continue with providers configured before the failure.");
        }
    }
}
