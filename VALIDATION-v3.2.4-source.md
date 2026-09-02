# PublisherStudio 3.2.4 source validation

Static validation only; no .NET build was run.

- Confirmed PublisherStudio application and installer-console versions are 3.2.4.
- Confirmed generated macOS launcher contains runtime endpoint-file lookup, HTTP fallback probe for port 58071, five-minute startup allowance, browser open, and Terminal log helper.
- Confirmed Unix release packaging removes transient RID staging and transient `.app` working copies only after native artifacts return successfully.
- Confirmed 3.2.3 headless DMG and explicit PKG payload validation code remains present.
- Confirmed no GitHub access or .NET compilation was used for this patch.
