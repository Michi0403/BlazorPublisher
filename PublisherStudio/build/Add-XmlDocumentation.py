#!/usr/bin/env python3
"""Adds conservative XML summaries to public C# declarations that do not already have them.

The script is intentionally source-only and deterministic. It does not move types, change signatures,
or invent runtime behavior. Maintainers can refine any generated summary in the normal review flow.
"""
from __future__ import annotations

import argparse
import re
from pathlib import Path

DECLARATION = re.compile(
    r"^(?P<indent>\s*)(?P<attrs>(?:\[[^\]]+\]\s*)*)(?P<mods>(?:(?:public|protected|internal|private|static|sealed|abstract|partial|virtual|override|async|unsafe|readonly|required|new|extern)\s+)+)(?P<body>.+)$"
)
TYPE_DECLARATION = re.compile(r"\b(class|interface|struct|record(?:\s+class|\s+struct)?|enum|delegate)\s+([A-Za-z_][A-Za-z0-9_]*)")
METHOD_NAME = re.compile(r"(?:operator\s*[^\s(]+|([A-Za-z_][A-Za-z0-9_]*))\s*(?:<[^>{};=]+>)?\s*\(")
PROPERTY_NAME = re.compile(r"([A-Za-z_][A-Za-z0-9_]*)\s*(?:\{|=>)")
EVENT_NAME = re.compile(r"\bevent\s+[^;=]+\s+([A-Za-z_][A-Za-z0-9_]*)")
FIELD_NAME = re.compile(r"([A-Za-z_][A-Za-z0-9_]*)\s*(?:=|;)")


def split_identifier(name: str) -> list[str]:
    value = re.sub(r"([a-z0-9])([A-Z])", r"\1 \2", name).replace("_", " ")
    tokens = re.sub(r"\s+", " ", value).strip().split()
    replacements = {
        "id": "identifier", "ids": "identifiers", "utc": "UTC", "url": "URL", "uri": "URI",
        "html": "HTML", "xml": "XML", "json": "JSON", "pdf": "PDF", "api": "API",
        "dpi": "DPI", "ffmpeg": "FFmpeg", "http": "HTTP", "https": "HTTPS",
        "css": "CSS", "js": "JavaScript", "svg": "SVG", "qr": "QR", "ui": "UI",
        "lan": "LAN", "tcp": "TCP", "udp": "UDP", "oauth": "OAuth", "dx": "DevExpress",
        "mm": "millimetres", "x": "horizontal position", "y": "vertical position",
    }
    return [replacements.get(token.lower(), token.lower()) for token in tokens]


def words(name: str) -> str:
    return " ".join(split_identifier(name))


def with_article(value: str) -> str:
    return ("an " if value[:1].lower() in "aeiou" else "a ") + value


def verb_summary(name: str) -> str:
    rules = (
        ("Try", "Attempts to"), ("Get", "Gets"), ("Set", "Sets"), ("Create", "Creates"),
        ("Build", "Builds"), ("Load", "Loads"), ("Save", "Saves"), ("Read", "Reads"),
        ("Write", "Writes"), ("Open", "Opens"), ("Close", "Closes"), ("Start", "Starts"),
        ("Stop", "Stops"), ("Add", "Adds"), ("Remove", "Removes"), ("Delete", "Deletes"),
        ("Update", "Updates"), ("Apply", "Applies"), ("Resolve", "Resolves"),
        ("Validate", "Validates"), ("Convert", "Converts"), ("Parse", "Parses"),
        ("Normalize", "Normalizes"), ("Calculate", "Calculates"), ("Compute", "Computes"),
        ("Find", "Finds"), ("Search", "Searches"), ("Handle", "Handles"),
        ("Publish", "Publishes"), ("Export", "Exports"), ("Import", "Imports"),
        ("Register", "Registers"), ("Unregister", "Unregisters"), ("Ensure", "Ensures"),
        ("Is", "Determines whether"), ("Can", "Determines whether"), ("Has", "Determines whether"),
    )
    for prefix, verb in rules:
        if name.startswith(prefix) and len(name) > len(prefix):
            rest = words(name[len(prefix):])
            return f"{verb} {rest}."
    return f"Runs the {words(name)} operation."


def is_public_api(mods: str) -> bool:
    tokens = set(mods.split())
    return "public" in tokens or "protected" in tokens


def find_name_and_kind(body: str, current_type: str | None, path: Path) -> tuple[str, str] | None:
    type_match = TYPE_DECLARATION.search(body)
    if type_match:
        kind = type_match.group(1).split()[-1]
        return type_match.group(2), kind
    event_match = EVENT_NAME.search(body)
    if event_match:
        return event_match.group(1), "event"

    property_match = PROPERTY_NAME.search(body)
    first_parenthesis = body.find("(")
    property_marker = min([value for value in (body.find("{"), body.find("=>")) if value >= 0], default=-1)
    if property_match and property_marker >= 0 and (first_parenthesis < 0 or property_marker < first_parenthesis):
        return property_match.group(1), "property"

    method_match = METHOD_NAME.search(body)
    if method_match:
        name = method_match.group(1) or "Operator"
        if current_type and name == current_type:
            return name, "constructor"
        return name, "method"
    field_match = FIELD_NAME.search(body)
    if field_match:
        return field_match.group(1), "field"
    return None


def summary_for(name: str, kind: str, body: str, path: Path) -> str:
    friendly = words(name)
    if kind == "interface":
        contract = words(name[1:]) if name.startswith("I") and len(name) > 1 and name[1].isupper() else friendly
        return f"Defines the {contract} contract."
    if kind in {"class", "struct", "record"}:
        if "BusinessObjects" in path.parts or "Models" in path.parts:
            return f"Represents {with_article(friendly)}."
        suffixes = ("Service", "Factory", "Store", "Catalog", "Registry", "Resolver", "Provider", "Controller", "Filter", "Writer", "Reader", "Renderer")
        if name.endswith(suffixes):
            return f"Provides {friendly} operations."
        return f"Represents {with_article(friendly)}."
    if kind == "enum":
        return f"Lists supported {friendly} values."
    if kind == "delegate":
        return f"Represents the {friendly} callback."
    if kind == "constructor":
        return f"Initializes a new instance of the <see cref=\"{name}\"/> class."
    if kind == "property":
        common = {
            "Id": "the stable identifier", "Name": "the display name", "Version": "the version",
            "CreatedUtc": "the UTC creation time", "ModifiedUtc": "the UTC modification time",
            "UpdatedUtc": "the UTC update time", "Enabled": "whether the feature is enabled",
            "Visible": "whether the item is visible", "Locked": "whether the item is locked",
        }
        description = common.get(name, friendly)
        settable = " set;" in body or " init;" in body or " set =>" in body or "private set;" in body
        return f"{'Gets or sets' if settable else 'Gets'} {description}."
    if kind == "event":
        return f"Occurs when {friendly}."
    if kind == "field":
        return f"Stores {friendly}."
    return verb_summary(name)

def existing_documentation(lines: list[str], insertion: int) -> bool:
    index = insertion - 1
    while index >= 0 and not lines[index].strip():
        index -= 1
    return index >= 0 and lines[index].lstrip().startswith("///")


def attribute_start(lines: list[str], declaration_index: int) -> int:
    """Returns the first line of the contiguous attribute block before a declaration."""
    insertion = declaration_index
    index = declaration_index - 1
    while index >= 0 and not lines[index].strip():
        index -= 1

    while index >= 0:
        stripped = lines[index].strip()
        if not stripped.endswith("]"):
            break

        balance = 0
        block_end = index
        while index >= 0:
            current = lines[index].strip()
            balance += current.count("]") - current.count("[")
            if current.startswith("[") and balance <= 0:
                insertion = index
                index -= 1
                break
            index -= 1
        else:
            return insertion

        while index >= 0 and not lines[index].strip():
            index -= 1
        if index == block_end:
            break

    return insertion


def process(path: Path) -> int:
    text = path.read_text(encoding="utf-8-sig")
    newline = "\r\n" if "\r\n" in text else "\n"
    lines = text.splitlines()
    insertions: list[tuple[int, list[str]]] = []
    type_stack: list[tuple[int, str]] = []
    brace_depth = 0
    in_block_comment = False

    for index, line in enumerate(lines):
        stripped = line.strip()
        # A lightweight comment state is enough because declarations are required at line start.
        if in_block_comment:
            if "*/" in stripped:
                in_block_comment = False
            brace_depth += line.count("{") - line.count("}")
            continue
        if stripped.startswith("/*") and "*/" not in stripped:
            in_block_comment = True
            brace_depth += line.count("{") - line.count("}")
            continue
        while type_stack and brace_depth < type_stack[-1][0]:
            type_stack.pop()

        match = DECLARATION.match(line)
        if match and is_public_api(match.group("mods")):
            body = match.group("body")
            current_type = type_stack[-1][1] if type_stack else None
            result = find_name_and_kind(body, current_type, path)
            if result:
                name, kind = result
                insertion = attribute_start(lines, index)
                if not existing_documentation(lines, insertion):
                    indent = match.group("indent")
                    summary = summary_for(name, kind, body, path)
                    insertions.append((insertion, [f"{indent}/// <summary>", f"{indent}/// {summary}", f"{indent}/// </summary>"]))
            type_match = TYPE_DECLARATION.search(body)
            if type_match and "{" in line:
                type_stack.append((brace_depth + line.count("{") - line.count("}"), type_match.group(2)))

        brace_depth += line.count("{") - line.count("}")

    if not insertions:
        return 0
    for insertion, docs in reversed(insertions):
        lines[insertion:insertion] = docs
    path.write_text(newline.join(lines) + (newline if text.endswith(("\n", "\r")) else ""), encoding="utf-8-sig")
    return len(insertions)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("root", type=Path)
    args = parser.parse_args()
    total = 0
    files = sorted(p for p in args.root.rglob("*.cs") if "obj" not in p.parts and "bin" not in p.parts)
    for path in files:
        count = process(path)
        if count:
            print(f"{path}: {count}")
            total += count
    print(f"Added {total} XML documentation summaries across {len(files)} C# files.")
    return 0

if __name__ == "__main__":
    raise SystemExit(main())
