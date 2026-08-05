# Privacy and security

PublisherStudio is local-first, but local software still needs careful boundaries.

## Data ownership

Publication projects, recordings, workbooks, pictures, settings, and protected connection state remain under the signed-in user's account. Exporters do not upload content by default.

## Untrusted input

Imported archives, SVG/XML, websites, media, JSON, spreadsheet files, LocalGPT envelopes, and browser messages are treated as untrusted. Importers validate paths, sizes, required files, and dangerous content before committing changes.

## Credentials

OAuth sessions, stream keys, LAN secrets, and machine-specific capture settings are stored outside publications and templates. They are not copied into normal interchange exports.

## Loopback host

The default web endpoint binds to `127.0.0.1`. Opening PublisherStudio to another interface is a deliberate deployment decision and should be paired with appropriate authentication, firewall, and transport controls.
