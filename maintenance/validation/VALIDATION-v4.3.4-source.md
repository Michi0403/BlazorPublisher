# PublisherStudio 4.3.4 — source-only validation

- Source base: supplied PublisherStudio 4.3.3 ZIP.
- Static Python tests verify existing 1-Wire Council dispatch, approval preservation, optional stable goal identity extraction and paired version tags.
- Existing Node GIF-frame regression test is exercised separately when Node is available.
- No PowerShell architecture gate or .NET/DevExpress build has been run; such validations are required on a supported developer host.
- Mandatory architecture rules and PowerShell build-guard files are retained unchanged.
