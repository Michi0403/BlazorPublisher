#!/usr/bin/env python3
"""Validate and package the pinned PublisherStudio Kawaii documentation snapshot.

Generated DocFX trees are intentionally ignored by Git. GitHub Actions publishes a
single tracked ZIP snapshot, but only after validating its identity, version, PDF,
local links, theme assets, path safety, and API payload.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import shutil
import stat
import sys
import tempfile
from html.parser import HTMLParser
from pathlib import Path, PurePosixPath
from urllib.parse import unquote, urlsplit
from zipfile import BadZipFile, ZipFile


REQUIRED_FILES = (
    "index.html",
    "api/index.html",
    "documentation-status.json",
    "styles/publisherstudio-kawaii.css",
    "styles/publisherstudio-kawaii.js",
    "favicon.svg",
    "favicon.ico",
    "logo.svg",
)

INDEX_MARKERS = (
    "publisherstudio-kawaii-docs",
    "data-publisherstudio-theme-bootstrap",
    "data-publisherstudio-favicon",
    "data-publisherstudio-kawaii-style",
    "data-publisherstudio-kawaii-script",
)

CSS_MARKERS = (
    "publisherstudio-theme-control",
    "publisherstudio-kawaii-sky",
    "publisherstudio-cursor-paw",
)

JS_MARKERS = (
    "mountThemeControl",
    "publisherstudio-docs-theme",
    "persistTheme",
    "publisherstudio-cursor-paw",
)

LINK_ATTRIBUTES = frozenset(("href", "src", "action", "poster"))
DOCFX_NAMESPACE_PAGE_MARKER = "data-publisherstudio-generated-namespace-page"
MAX_UNCOMPRESSED_BYTES = 512 * 1024 * 1024
MAX_FILE_COUNT = 20_000


class LocalLinkCollector(HTMLParser):
    def __init__(self) -> None:
        super().__init__(convert_charrefs=True)
        self.urls: list[str] = []

    def handle_starttag(self, _tag: str, attrs: list[tuple[str, str | None]]) -> None:
        for name, value in attrs:
            if name.lower() in LINK_ATTRIBUTES and value and value.strip():
                self.urls.append(value.strip())


def read_text(path: Path) -> str:
    return path.read_text(encoding="utf-8-sig")


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def fail(message: str) -> None:
    raise RuntimeError(message)


def html_escape(value: str) -> str:
    return (
        value.replace("&", "&amp;")
        .replace("<", "&lt;")
        .replace(">", "&gt;")
        .replace('"', "&quot;")
        .replace("'", "&#39;")
    )


def materialize_docfx_namespace_pages(source: Path) -> int:
    """Create landing pages for namespace XRefs that DocFX omitted.

    DocFX may render breadcrumb links to parent namespaces without emitting the
    corresponding namespace HTML file. The pipeline creates small namespace indexes
    so the site contains no dead navigation while general link validation stays strict.
    """

    api_root = source / "api"
    if not api_root.is_dir():
        return 0

    pages = sorted(api_root.glob("*.html"))
    existing_stems = {page.stem for page in pages if page.name.casefold() != "index.html"}
    missing_namespaces: set[str] = set()

    for page in pages:
        parser = LocalLinkCollector()
        try:
            parser.feed(read_text(page))
        except (UnicodeDecodeError, ValueError) as error:
            fail(f"HTML could not be parsed as UTF-8: {page}: {error}")

        for raw_url in parser.urls:
            parsed = urlsplit(raw_url)
            if parsed.scheme or parsed.netloc:
                continue
            local_url = unquote(parsed.path).replace("\\", "/")
            if not local_url or "/" in local_url or not local_url.lower().endswith(".html"):
                continue
            target = api_root / local_url
            if target.is_file():
                continue

            candidate = Path(local_url).stem
            if not candidate.startswith("PublisherStudio."):
                continue
            prefix = candidate + "."
            if any(stem.startswith(prefix) for stem in existing_stems):
                missing_namespaces.add(candidate)

    created = 0
    for namespace in sorted(missing_namespaces, key=lambda value: (value.count("."), value.casefold())):
        destination = api_root / f"{namespace}.html"
        if destination.exists():
            continue

        descendants = sorted(
            (stem for stem in existing_stems if stem.startswith(namespace + ".")),
            key=str.casefold,
        )
        if not descendants:
            continue

        items = "\n".join(
            f'          <li><a class="xref" href="{html_escape(stem)}.html">{html_escape(stem)}</a></li>'
            for stem in descendants
        )
        title = html_escape(namespace)
        page_html = f"""<!DOCTYPE html>
<html class="publisherstudio-kawaii-docs" data-bs-theme="light" {DOCFX_NAMESPACE_PAGE_MARKER}="true">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>{title} namespace | PublisherStudio</title>
  <script data-publisherstudio-theme-bootstrap="true">
  (function () {{
    var key = "publisherstudio-docs-theme";
    var value = null;
    try {{ value = localStorage.getItem(key) || localStorage.getItem("theme"); }} catch (_) {{ }}
    if (value !== "light" && value !== "dark" && value !== "auto") value = "auto";
    var resolved = value === "auto" && window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : (value === "auto" ? "light" : value);
    document.documentElement.dataset.publisherstudioThemePreference = value;
    document.documentElement.setAttribute("data-bs-theme", resolved);
  }})();
  </script>
  <link rel="icon" type="image/svg+xml" href="../favicon.svg" data-publisherstudio-favicon="true" />
  <link rel="alternate icon" href="../favicon.ico" />
  <link rel="stylesheet" href="../styles/publisherstudio-kawaii.css" data-publisherstudio-kawaii-style="true" />
</head>
<body>
  <main class="container-xxl py-4">
    <nav aria-label="Breadcrumb"><a href="../index.html">PublisherStudio documentation</a> · <a href="index.html">API reference</a></nav>
    <article>
      <header><p>Namespace</p><h1>{title}</h1></header>
      <p>This namespace index is materialized by the PublisherStudio documentation pipeline because DocFX referenced the namespace without emitting its landing page.</p>
      <h2>Documented descendants</h2>
      <ul>
{items}
      </ul>
    </article>
  </main>
  <script type="module" src="../styles/publisherstudio-kawaii.js" data-publisherstudio-kawaii-script="true"></script>
</body>
</html>
"""
        destination.write_text(page_html, encoding="utf-8", newline="\n")
        created += 1

    return created


def validate_local_links(source: Path) -> None:
    source_root = source.resolve()
    failures: list[str] = []

    for page in sorted(source.rglob("*.html")):
        parser = LocalLinkCollector()
        try:
            parser.feed(read_text(page))
        except (UnicodeDecodeError, ValueError) as error:
            fail(f"HTML could not be parsed as UTF-8: {page}: {error}")

        for raw_url in parser.urls:
            parsed = urlsplit(raw_url)
            if parsed.scheme or parsed.netloc or raw_url.startswith(("#", "data:", "mailto:", "javascript:")):
                continue

            local_url = unquote(parsed.path).replace("\\", "/")
            if not local_url:
                continue
            if local_url.startswith(("/", "~/")):
                failures.append(f"{page.relative_to(source)} -> root-relative URL {raw_url!r}")
                continue

            target = (page.parent / local_url).resolve(strict=False)
            try:
                target.relative_to(source_root)
            except ValueError:
                failures.append(f"{page.relative_to(source)} -> escaping URL {raw_url!r}")
                continue

            if target.is_dir() or local_url.endswith("/"):
                target = target / "index.html"
            if not target.is_file():
                failures.append(f"{page.relative_to(source)} -> missing target {raw_url!r}")

    if failures:
        preview = "; ".join(failures[:20])
        remainder = len(failures) - 20
        if remainder > 0:
            preview += f"; and {remainder} more"
        fail("Documentation contains invalid local links: " + preview)


def validate_source(
    source: Path,
    expected_version: str | None = None,
    generated_namespace_pages: int = 0,
) -> dict[str, object]:
    if not source.is_dir():
        fail(f"Documentation source does not exist: {source}")

    for path in source.rglob("*"):
        if path.is_symlink():
            fail(f"Documentation tree must not contain symbolic links: {path}")

    missing = [name for name in REQUIRED_FILES if not (source / name).is_file()]
    if missing:
        fail("Documentation tree is incomplete; missing: " + ", ".join(missing))

    index_text = read_text(source / "index.html")
    missing_index_markers = [marker for marker in INDEX_MARKERS if marker not in index_text]
    if missing_index_markers:
        fail("index.html is not the themed PublisherStudio build; missing: " + ", ".join(missing_index_markers))

    css_text = read_text(source / "styles/publisherstudio-kawaii.css")
    missing_css_markers = [marker for marker in CSS_MARKERS if marker not in css_text]
    if missing_css_markers:
        fail("Kawaii CSS is incomplete; missing: " + ", ".join(missing_css_markers))

    js_text = read_text(source / "styles/publisherstudio-kawaii.js")
    missing_js_markers = [marker for marker in JS_MARKERS if marker not in js_text]
    if missing_js_markers:
        fail("Kawaii JavaScript is incomplete; missing: " + ", ".join(missing_js_markers))

    favicon_text = read_text(source / "favicon.svg")
    if "PublisherStudio cat paw" not in favicon_text or "<svg" not in favicon_text:
        fail("favicon.svg is not the PublisherStudio cat-paw icon")

    try:
        status = json.loads(read_text(source / "documentation-status.json"))
    except (UnicodeDecodeError, json.JSONDecodeError) as error:
        fail(f"documentation-status.json is invalid: {error}")
    if not isinstance(status, dict):
        fail("documentation-status.json must contain an object")

    version = str(status.get("version") or status.get("Version") or "").strip()
    if not version:
        fail("documentation-status.json does not declare a version")
    if expected_version and version != expected_version:
        fail(f"Documentation version {version!r} does not match PublisherStudio source version {expected_version!r}")

    pdf_file_name = status.get("pdfFileName")
    if not isinstance(pdf_file_name, str) or not pdf_file_name.strip():
        fail("documentation-status.json does not declare pdfFileName")
    pdf_file_name = pdf_file_name.strip()
    pdf_path = PurePosixPath(pdf_file_name.replace("\\", "/"))
    if pdf_path.is_absolute() or ".." in pdf_path.parts or len(pdf_path.parts) != 1 or pdf_path.suffix.lower() != ".pdf":
        fail(f"documentation-status.json contains an unsafe PDF name: {pdf_file_name!r}")
    published_pdf = source / pdf_file_name
    if not published_pdf.is_file() or published_pdf.stat().st_size <= 0:
        fail(f"Declared documentation PDF is missing or empty: {pdf_file_name}")
    declared_pdf_bytes = status.get("pdfBytes")
    if isinstance(declared_pdf_bytes, int) and declared_pdf_bytes != published_pdf.stat().st_size:
        fail(
            f"documentation-status.json declares {declared_pdf_bytes} PDF bytes, "
            f"but {pdf_file_name} contains {published_pdf.stat().st_size} bytes"
        )

    html_files = list(source.rglob("*.html"))
    api_html_files = list((source / "api").rglob("*.html"))
    if len(html_files) < 20 or len(api_html_files) < 1:
        fail(f"Documentation output looks incomplete ({len(html_files)} HTML, {len(api_html_files)} API HTML)")

    validate_local_links(source)

    return {
        "source": source.as_posix(),
        "version": version,
        "pdfFileName": pdf_file_name,
        "pdfBytes": published_pdf.stat().st_size,
        "htmlFiles": len(html_files),
        "apiHtmlFiles": len(api_html_files),
        "localLinksValidated": True,
        "generatedDocfxNamespacePages": generated_namespace_pages,
        "themePersistence": True,
        "catPawFavicon": True,
        "kawaiiStyleSha256": sha256(source / "styles/publisherstudio-kawaii.css"),
        "kawaiiScriptSha256": sha256(source / "styles/publisherstudio-kawaii.js"),
        "faviconSvgSha256": sha256(source / "favicon.svg"),
    }


def safe_extract_zip(archive: Path, destination: Path) -> Path:
    if not archive.is_file():
        fail(f"Pinned Pages archive does not exist: {archive}")

    try:
        with ZipFile(archive) as bundle:
            entries = bundle.infolist()
            if not entries:
                fail(f"Pinned Pages archive is empty: {archive}")
            if len(entries) > MAX_FILE_COUNT:
                fail(f"Pinned Pages archive contains too many entries: {len(entries)}")

            total_size = sum(entry.file_size for entry in entries)
            if total_size > MAX_UNCOMPRESSED_BYTES:
                fail(f"Pinned Pages archive is too large after extraction: {total_size} bytes")

            normalized_names: set[str] = set()
            for entry in entries:
                raw_name = entry.filename.replace("\\", "/")
                path = PurePosixPath(raw_name)
                if path.is_absolute() or ".." in path.parts:
                    fail(f"Unsafe path in pinned Pages archive: {entry.filename}")

                normalized_name = "/".join(part for part in path.parts if part not in ("", "."))
                folded_name = normalized_name.casefold()
                if folded_name in normalized_names:
                    fail(f"Duplicate path in pinned Pages archive: {entry.filename}")
                normalized_names.add(folded_name)

                unix_mode = entry.external_attr >> 16
                if stat.S_ISLNK(unix_mode):
                    fail(f"Symbolic links are not allowed in pinned Pages archive: {entry.filename}")

            bundle.extractall(destination)
    except BadZipFile as error:
        fail(f"Pinned Pages archive is not a valid ZIP: {error}")

    if (destination / "index.html").is_file():
        return destination

    candidates = [path for path in destination.iterdir() if path.is_dir() and (path / "index.html").is_file()]
    if len(candidates) == 1:
        return candidates[0]

    fail("Pinned Pages archive must contain index.html at its root or in one top-level directory")


def copy_tree(source: Path, output: Path) -> None:
    if output.exists():
        shutil.rmtree(output)
    shutil.copytree(source, output, symlinks=False)
    (output / ".nojekyll").write_text("", encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser()
    source_group = parser.add_mutually_exclusive_group(required=True)
    source_group.add_argument("--archive", type=Path)
    source_group.add_argument("--source", type=Path)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--expected-version")
    args = parser.parse_args()

    try:
        output = args.output.resolve(strict=False)
        expected_version = args.expected_version.strip() if args.expected_version else None

        if args.archive is not None:
            archive = args.archive.resolve(strict=True)
            with tempfile.TemporaryDirectory(prefix="publisherstudio-pages-") as temp_dir:
                extracted_source = safe_extract_zip(archive, Path(temp_dir))
                copy_tree(extracted_source, output)
            generated_namespace_pages = materialize_docfx_namespace_pages(output)
            metadata = validate_source(output, expected_version, generated_namespace_pages)
            metadata["source"] = "tracked Kawaii documentation snapshot"
            metadata["deploymentSource"] = "tracked Kawaii documentation snapshot"
            metadata["sourceArchive"] = archive.as_posix()
            metadata["sourceArchiveSha256"] = sha256(archive)
        else:
            source = args.source.resolve(strict=True)
            copy_tree(source, output)
            generated_namespace_pages = materialize_docfx_namespace_pages(output)
            metadata = validate_source(output, expected_version, generated_namespace_pages)
            metadata["source"] = source.as_posix()
            metadata["deploymentSource"] = "explicit documentation directory"

        metadata["artifact"] = output.as_posix()
        (output / "github-pages-deployment.json").write_text(
            json.dumps(metadata, indent=2, ensure_ascii=False) + "\n",
            encoding="utf-8",
        )
        print(json.dumps(metadata, indent=2, ensure_ascii=False))
        return 0
    except (OSError, RuntimeError) as error:
        print(f"Pages artifact preparation failed: {error}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
