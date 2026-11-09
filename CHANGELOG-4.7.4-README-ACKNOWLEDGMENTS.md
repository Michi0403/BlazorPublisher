# PublisherStudio 4.7.4 — README acknowledgments

4.7.4 is 4.7.3 plus one documentation change. No code changed.

## Added

- `README.md` has a new section, **Provenance and acknowledgments**, matching LocalGPT's README:
  - It names the maintainer, Michael Fleischer (`Michi0403`).
  - It acknowledges co-development sessions with OpenAI's ChatGPT and Anthropic's Claude, and LocalGPT Council reviews used as engineering input.
  - It lists Claude's contribution, the 4.7.3 1-Wire traffic journal.
  - It states that design decisions, ownership, release decisions and responsibility remain with the maintainer.
- Synchronized the Web and Setup project versions from 4.7.3 to 4.7.4.

## Verification status

- The only files that differ from 4.7.3 are `README.md`, the two project files and this changelog.
- `tests/js/*.test.cjs` still pass under Node.
- No compiler run; none is needed for a README change.

## Interaction / frontend release checklist

- No object, editor mode, toolbar, overlay, renderer, stacking context, input routing or frontend failure boundary changed.
