# Offline validation — PublisherStudio 4.4.9

Input: customer source ZIP 4.4.8. Changes and inherited limitations are recorded in the corresponding changelog.

`node --test tests/js/*.test.*`: **9 passed**, including existing canvas/GIF/Editor source tests and new GIF frame-strip source check.

These are not actual Razor or .NET builds. **No .NET SDK, Windows installer, PowerShell guards, browser-hosted Editor smoke test, or published application runtime validation**. The canvas visual defect is not claimed resolved on the user's machine.
