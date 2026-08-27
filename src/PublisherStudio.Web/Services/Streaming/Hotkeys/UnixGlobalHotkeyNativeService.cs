namespace PublisherStudio.Services.Streaming.Hotkeys;

/// <summary>
/// Unix global-hotkey boundary. PublisherStudio currently keeps application-global native hotkeys
/// disabled on macOS/Linux instead of loading Windows user32 bindings on an unsupported host.
/// Keyboard commands inside the application remain available through the normal UI input services.
/// </summary>
public sealed class UnixGlobalHotkeyNativeService(
    ILogger<UnixGlobalHotkeyNativeService> logger) : IGlobalHotkeyNativeService
{
    public bool IsAvailable => false;

    public bool TryInitializeMessageQueue(out uint threadId)
    {
        try
        {
            threadId = 0;
            logger.LogTrace("Native application-global hotkeys are not enabled for this Unix host.");
            return false;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not initialize the Unix global-hotkey boundary.");
            threadId = 0;
            return false;
        }
    }

    public bool TryRegisterHotKey(IntPtr windowHandle, int id, uint modifiers, uint virtualKey)
    {
        try
        {
            logger.LogTrace("Ignoring unsupported Unix global-hotkey registration {HotkeyId}.", id);
            return false;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not process Unix global-hotkey registration {HotkeyId}.", id);
            return false;
        }
    }

    public bool TryUnregisterHotKey(IntPtr windowHandle, int id)
    {
        try
        {
            logger.LogTrace("Ignoring unsupported Unix global-hotkey unregistration {HotkeyId}.", id);
            return false;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not process Unix global-hotkey unregistration {HotkeyId}.", id);
            return false;
        }
    }

    public int ReadMessage(out GlobalHotkeyNativeMessage message)
    {
        try
        {
            message = default;
            logger.LogTrace("Unix global-hotkey message queue is disabled.");
            return 0;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not read from the disabled Unix global-hotkey queue.");
            message = default;
            return 0;
        }
    }

    public bool TryPostThreadMessage(uint threadId, uint message, UIntPtr wordParameter, IntPtr longParameter)
    {
        try
        {
            logger.LogTrace("Ignoring unsupported Unix global-hotkey thread message {MessageId}.", message);
            return false;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not process Unix global-hotkey thread message {MessageId}.", message);
            return false;
        }
    }
}
