#!/usr/bin/env python3
"""Validates comprehensive XML documentation for maintained C# and Razor component source."""
from __future__ import annotations
import argparse
from pathlib import Path
from xml_documentation import run as run_csharp
from razor_xml_documentation import run as run_razor

if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('root', type=Path)
    args = parser.parse_args()
    source_root = args.root.resolve()
    repository_root = source_root.parent
    csharp = run_csharp(source_root, 'validate')
    if csharp != 0:
        raise SystemExit(csharp)
    raise SystemExit(run_razor(repository_root, 'validate'))
