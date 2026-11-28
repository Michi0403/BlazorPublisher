# PublisherStudio 4.5.4 offline validation

- 4.5.3 full source as baseline, no GitHub use.
- `node --test tests/js/*.test.*`: 15/15 passed.
- `node --check docs/templates/publisherstudio/public/main.js`: passed.
- Synthetic Chromium popup geometry verified selected publisher-owned modal while an unrelated popup retained its spacing.
- Synthetic Chromium docs checked both modes, repeated drag and navigation reset with unchanged document text.
- Not executed: Windows PowerShell build guards, .NET compilation, real DevExpress popup rendering, full DocFX build.
