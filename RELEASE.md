# PublisherStudio 2.6.3 preview, AI component, and website-export release

PublisherStudio 2.6.3 improves Panel Studio authoring ergonomics and extends the existing PublisherStudio↔LocalGPT integration into authoring and publication components without making normal exported publications dependent on LocalGPT.

- Adds persistent user-defined Panel Studio preview viewport presets and repairs the right-side inspector/tooltip/footer UX.
- Moves language selection into the publication title/modified-time bar.
- Adds compression choices to single-file website and interactive presentation website export while reusing the existing structured-site media conversion helpers.
- Adds LocalGPT-aware **AI Text** and **AI Chat** quick inserts when the paired LocalGPT capability directory advertises Council execution.
- Adds StoryEditor selection AI actions while preserving review-before-replace semantics.
- Reuses the existing DevExtreme Chat component/runtime and routes optional AI messages through PublisherStudio's same-origin controller to its established LocalGPT 1-Wire connection.
- Keeps non-AI publication data/rendering self-contained and gracefully degrades explicitly AI-enabled Chat when the external AI bridge is unavailable.
- Bumps publication format to 1.58 and application/installer version to 2.6.3.

The owner-side Windows .NET 10 / DevExpress build remains authoritative for compilation and release publishing.
