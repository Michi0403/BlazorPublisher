#!/usr/bin/env python3
"""Checks that public and protected C# declarations have adjacent XML summaries."""
from __future__ import annotations

import argparse
import re
from pathlib import Path

DECLARATION = re.compile(r"^(?P<indent>\s*)(?P<attrs>(?:\[[^\]]+\]\s*)*)(?P<mods>(?:(?:public|protected|internal|private|static|sealed|abstract|partial|virtual|override|async|unsafe|readonly|required|new|extern)\s+)+)(?P<body>.+)$")
TARGET = re.compile(r"\b(class|interface|struct|record|enum|delegate|event)\b|\b[A-Za-z_][A-Za-z0-9_]*(?:<[^>{};=]+>)?\s*\(|\b[A-Za-z_][A-Za-z0-9_]*\s*(?:\{|=>|=|;)")


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


def has_docs(lines: list[str], insertion: int) -> bool:
    index = insertion - 1
    while index >= 0 and not lines[index].strip():
        index -= 1
    return index >= 0 and lines[index].lstrip().startswith("///")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("root", type=Path)
    args = parser.parse_args()
    failures: list[str] = []
    declarations = 0
    for path in sorted(args.root.rglob("*.cs")):
        if "obj" in path.parts or "bin" in path.parts:
            continue
        lines = path.read_text(encoding="utf-8-sig", errors="replace").splitlines()
        for index, line in enumerate(lines):
            match = DECLARATION.match(line)
            if not match:
                continue
            modifiers = set(match.group("mods").split())
            if "public" not in modifiers and "protected" not in modifiers:
                continue
            if not TARGET.search(match.group("body")):
                continue
            declarations += 1
            insertion = attribute_start(lines, index)
            if not has_docs(lines, insertion):
                failures.append(f"{path.as_posix()}:{index + 1}: {line.strip()}")
    if failures:
        print("XML documentation coverage failed:")
        for failure in failures[:200]:
            print(f"  - {failure}")
        if len(failures) > 200:
            print(f"  - ... and {len(failures) - 200} more")
        return 1
    print(f"XML documentation coverage passed for {declarations} public/protected C# declarations.")
    return 0

if __name__ == "__main__":
    raise SystemExit(main())
