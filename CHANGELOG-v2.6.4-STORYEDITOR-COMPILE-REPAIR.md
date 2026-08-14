# PublisherStudio 2.6.4 — StoryEditor compile repair

- Repairs the Razor parser break introduced by the StoryEditor AI selection prompt. The invalid same-line multiline raw string is replaced with a normal interpolated string containing explicit newlines.
- Restores `NavigationManager` injection in `MainLayout`, matching the existing `Navigation.Uri` error-boundary key.
- Keeps all 2.6.3 preview presets, LocalGPT AI, website compression, language-bar, DevExtreme Chat, StoryEditor AI and export behavior intact.
- Publication format remains **1.58**; no document migration is required.
- PublisherStudio Web and InstallerConsole are **2.6.4**.
- No LocalGPT source is rolled forward solely for this PublisherStudio repair.
