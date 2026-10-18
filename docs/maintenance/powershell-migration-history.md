# PublisherStudio PowerShell maintenance migration (4.2.0, rebased on 4.1.9)

Python is no longer an interpreter or a repository build dependency. The former Python-only scripts have been removed from the active source distribution. The existing native PowerShell rules for services, DX components, Panel Studio geometry and interaction, localization, diagnostics, and packaging remain in place. The new PowerShell scripts own the former active Python audit entrypoints and Pages validation.

The following old `audit_release_*` files were historical, version-specific checks and were **not called by either current build entrypoint**. Their history is recorded here; the current source and current controls are validated by maintained PowerShell gates rather than executing over one hundred obsolete version-specific validators.

## Removed historical Python audit filenames

- `build/audit_ai_preview_export_ux.py`
- `build/audit_data_panel_media_repair.py`
- `build/audit_media_studio_drag_effect_localization_2_6_5.py`
- `build/audit_picture_page_effects_repair.py`
- `build/audit_release_2_6_8.py`
- `build/audit_release_2_6_9.py`
- `build/audit_release_2_7_0.py`
- `build/audit_release_2_7_1.py`
- `build/audit_release_2_7_2.py`
- `build/audit_release_2_7_3.py`
- `build/audit_release_2_7_4.py`
- `build/audit_release_2_7_5.py`
- `build/audit_release_2_7_6.py`
- `build/audit_release_2_7_7.py`
- `build/audit_release_2_7_8.py`
- `build/audit_release_2_7_9.py`
- `build/audit_release_2_8_0.py`
- `build/audit_release_2_8_1.py`
- `build/audit_release_2_8_2.py`
- `build/audit_release_2_8_3.py`
- `build/audit_release_2_8_4.py`
- `build/audit_release_2_8_5.py`
- `build/audit_release_2_8_6.py`
- `build/audit_release_2_8_7.py`
- `build/audit_release_2_8_8.py`
- `build/audit_release_2_8_9.py`
- `build/audit_release_2_9_0.py`
- `build/audit_release_2_9_1.py`
- `build/audit_release_2_9_2.py`
- `build/audit_release_2_9_3.py`
- `build/audit_release_2_9_4.py`
- `build/audit_release_2_9_5.py`
- `build/audit_release_2_9_6.py`
- `build/audit_release_2_9_7.py`
- `build/audit_release_2_9_8.py`
- `build/audit_release_2_9_9.py`
- `build/audit_release_3_0_0.py`
- `build/audit_release_3_0_1.py`
- `build/audit_release_3_0_2.py`
- `build/audit_release_3_0_3.py`
- `build/audit_release_3_0_4.py`
- `build/audit_release_3_0_5.py`
- `build/audit_release_3_0_6.py`
- `build/audit_release_3_0_8.py`
- `build/audit_release_3_0_9.py`
- `build/audit_release_3_1_2.py`
- `build/audit_release_3_1_3.py`
- `build/audit_release_3_1_4.py`
- `build/audit_release_3_1_5.py`
- `build/audit_release_3_1_6.py`
- `build/audit_release_3_1_7.py`
- `build/audit_release_3_1_8.py`
- `build/audit_release_3_1_9.py`
- `build/audit_release_3_2_0.py`
- `build/audit_release_3_2_1.py`
- `build/audit_release_3_2_2.py`
- `build/audit_release_3_2_3.py`
- `build/audit_release_3_2_6.py`
- `build/audit_release_3_2_7.py`
- `build/audit_release_3_2_8.py`
- `build/audit_release_3_2_9.py`
- `build/audit_release_3_3_0.py`
- `build/audit_release_3_3_1.py`
- `build/audit_release_3_3_2.py`
- `build/audit_release_3_3_3.py`
- `build/audit_release_3_3_4.py`
- `build/audit_release_3_3_5.py`
- `build/audit_release_3_3_6.py`
- `build/audit_release_3_3_7.py`
- `build/audit_release_3_3_8.py`
- `build/audit_release_3_3_9.py`
- `build/audit_release_3_4_0.py`
- `build/audit_release_3_4_1.py`
- `build/audit_release_3_4_2.py`
- `build/audit_release_3_4_3.py`
- `build/audit_release_3_4_4.py`
- `build/audit_release_3_4_5.py`
- `build/audit_release_3_4_6.py`
- `build/audit_release_3_4_8.py`
- `build/audit_release_3_4_9.py`
- `build/audit_release_3_5_0.py`
- `build/audit_release_3_5_1.py`
- `build/audit_release_3_5_2.py`
- `build/audit_release_3_5_3.py`
- `build/audit_release_3_5_4.py`
- `build/audit_release_3_5_6.py`
- `build/audit_release_3_5_7.py`
- `build/audit_release_3_5_8.py`
- `build/audit_release_3_5_9.py`
- `build/audit_release_3_6_0.py`
- `build/audit_release_3_6_1.py`
- `build/audit_release_3_6_2.py`
- `build/audit_release_3_6_3.py`
- `build/audit_release_3_6_4.py`
- `build/audit_release_3_6_5.py`
- `build/audit_release_3_6_6.py`
- `build/audit_release_3_6_7.py`
- `build/audit_release_3_6_9.py`
- `build/audit_release_3_7_0.py`
- `build/audit_release_3_7_1.py`
- `build/audit_release_3_7_2.py`
- `build/audit_release_3_7_3.py`
- `build/audit_release_3_7_4.py`
- `build/audit_release_3_7_5.py`
- `build/audit_release_3_7_6.py`
- `build/audit_release_3_9_9.py`
- `build/audit_release_4_0_0.py`
- `build/audit_release_4_0_1.py`
- `build/audit_release_4_0_2.py`
- `build/audit_release_4_0_3.py`
- `build/audit_release_4_0_4.py`
- `build/audit_repository_build_storage.py`


## Current non-historical replacements

- `.github/scripts/extract-shipped-docs.py`
- `.github/scripts/prepare-pages-artifact.py`
- `build/Add-XmlDocumentation.py`
- `build/Assert-XmlDocumentationCoverage.py`
- `build/audit_application_architecture.py`
- `build/audit_async_continuations.py`
- `build/audit_async_only_architecture.py`
- `build/audit_component_resilience.py`
- `build/audit_cross_platform_boundaries.py`
- `build/audit_documentation_onewire_contracts.py`
- `build/audit_iterator_exception_policy.py`
- `build/audit_panelstudio_persistence.py`
- `build/audit_powershell_variable_interpolation.py`
- `build/audit_prerender_interop_safety.py`
- `build/audit_razor_maintenance_contract.py`
- `build/audit_service_resilience.py`
- `build/audit_strict_async_policy_2_6_6.py`
- `build/audit_transient_ui_state_ownership.py`
- `build/razor_xml_documentation.py`
- `build/xml_documentation.py`

**Validation boundary:** Static source checks do not establish behavioral equivalence to every historical Python parser. The .NET/compiler/DocFX, Windows PowerShell and cross-platform release commands remain the authoritative end-to-end tests. The migration does not alter the application business services.

### Version 4.1.9 baseline integrity

The complete inventory of 132 removed Python maintenance files is grounded in PublisherStudio 4.1.9. Product services, Razor components, JavaScript, CSS, assets, and SQL are preserved byte-for-byte from 4.1.9. The new native PowerShell guards replace active Python entrypoints; old one-off audit scripts are documented here, not runnable dependencies.
