# PublisherStudio 3.0.7 source validation

This handoff was validated without invoking `dotnet`, without GitHub access, and without changing LocalGPT.

Scope is limited to the supplied PublisherStudio documentation failure:

- the full documentation/PDF release path remains enabled;
- the website-theme function no longer owns HTML language repair;
- generated HTML language repair runs after DocFX/theme processing and immediately before the unchanged strict accessibility/link preflight;
- the repair changes only generated opening `<html>` tags that do not already contain a `lang` attribute;
- version identity remains within the single-digit minor/patch policy.

Repository source audits and ZIP integrity checks were run locally. A real .NET release build still has to be run on the target build machine.
