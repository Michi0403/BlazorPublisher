using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PublisherStudio.BusinessObjects;
using PublisherStudio.BusinessObjects.Enums;
using PublisherStudio.Logging;

namespace PublisherStudio.Services;

/// <summary>Configures PublisherStudio's optional logging providers during composition-root startup.</summary>
/// <param name="services">Service collection receiving provider and option registrations.</param>
/// <param name="configuration">Application configuration containing the LoggingCore sections.</param>
/// <param name="logger">Bootstrap logger used while the normal provider pipeline is being configured.</param>
public sealed class LoggingConfigurationService(
    IServiceCollection services,
    IConfiguration configuration,
    ILogger logger)
{
    /// <summary>Configures the optional PublisherStudio logging providers on the supplied logging builder.</summary>
    /// <param name="loggingBuilder">Logging builder receiving enabled providers.</param>
    public void Configure(ILoggingBuilder loggingBuilder)
    {
        ArgumentNullException.ThrowIfNull(loggingBuilder);
        try
        {
            logger.LogInformation("Configuring PublisherStudio logging providers.");
            services.AddOptions<LoggingCoreOptions>()
                .Bind(configuration.GetSection("LoggingCore"));
            services.Configure<LoggingCoreOptions>(options =>
                configuration.GetSection("LoggingCore").Bind(options));

            var loggingOptions = configuration.GetSection("LoggingCore").Get<LoggingCoreOptions>();
            if (loggingOptions is null || loggingOptions.CoreLogLevel == CoreLogLevel.None)
            {
                logger.LogInformation("Optional PublisherStudio logging providers are disabled by configuration.");
                return;
            }

            loggingBuilder.AddJsonConsole();
            loggingBuilder.AddConsole();
#if DEBUG
            loggingBuilder.AddDebug();
#endif
            AddFileLoggerIfConfigured(loggingOptions);
            logger.LogInformation("Configured the enabled PublisherStudio logging providers.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not configure the PublisherStudio logging providers; startup will continue with providers configured before the failure.");
        }
    }

    /// <summary>Adds the optional file logger when its LoggingCore section enables file persistence.</summary>
    /// <param name="loggingOptions">Resolved PublisherStudio logging configuration.</param>
    private void AddFileLoggerIfConfigured(LoggingCoreOptions loggingOptions)
    {
        try
        {
            logger.LogInformation("Evaluating the optional file logger configuration.");
            services.Configure<FileLoggerCoreOptions>(options =>
                configuration.GetSection("LoggingCore:FileCore").Bind(options));

            if (loggingOptions.FileCore is null || loggingOptions.FileCore.CoreLogLevel == CoreLogLevel.None)
                return;

            services.AddSingleton<ILoggerProvider>(provider =>
                new FileLoggerProvider(provider.GetRequiredService<IOptionsMonitor<FileLoggerCoreOptions>>()));
            logger.LogInformation("Registered the optional file logger provider. Blank FilePath writes PublisherStudio.log beside the running application.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not configure the optional file logger provider; startup will continue without it.");
        }
    }
}
