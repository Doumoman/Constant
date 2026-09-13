#!/usr/bin/env python3
"""Read-only verifier for SV5_11 CHECKER_SCOPE_FIX01."""
import argparse
import collections
import hashlib
import json
import re
import subprocess
import sys
from pathlib import Path, PurePosixPath


TASK = "SV5_11_HUB_SHELL"
HERE = Path(__file__).resolve().parent
PREFIX = "MapDesign/MCP/INPUTS/SV5_11_HUB_SHELL/CORRECTIONS/CHECKER_SCOPE_FIX01"
ORIGINAL = "MapDesign/MCP/INPUTS/SV5_11_HUB_SHELL"
STATUS = "MapDesign/MCP/06_IMPLEMENTATION_STATUS.md"
BASE = "3408a0ce540f0c9c947b5f2661f68122e06137c3"


class Blocked(Exception):
    pass


def need(condition, message):
    if not condition:
        raise Blocked(message)


def sha(data):
    return hashlib.sha256(data).hexdigest()


def safe(root, rel):
    item = PurePosixPath(rel)
    need(rel and not item.is_absolute() and "\\" not in rel and ":" not in rel, "unsafe path " + rel)
    need(all(part not in ("", ".", "..") for part in rel.split("/")), "unsafe path " + rel)
    path = root.joinpath(*item.parts)
    need(path.resolve().is_relative_to(root.resolve()), "path escape " + rel)
    return path


def git(root, *args):
    process = subprocess.run(["git", "-C", str(root), *args], stdout=subprocess.PIPE, stderr=subprocess.PIPE)
    need(process.returncode == 0, "git failed: " + " ".join(args))
    return process.stdout


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", type=Path, required=True)
    parser.add_argument("--expected-manifest-sha256", required=True)
    args = parser.parse_args()
    try:
        root = args.root.resolve()
        need(HERE == safe(root, PREFIX).resolve(), "run installed correction verifier")
        manifest_bytes = (HERE / "FILES.json").read_bytes()
        need(sha(manifest_bytes) == args.expected_manifest_sha256, "correction manifest SHA mismatch")
        manifest = json.loads(manifest_bytes.decode("utf-8-sig"))
        need(manifest["schema"] == "SV5_11_CHECKER_SCOPE_FIX_PACKAGE_V1", "wrong correction package")
        for entry in manifest["files"]:
            data = safe(HERE, entry["path"]).read_bytes()
            need(len(data) == entry["bytes"] and sha(data) == entry["sha256"], "correction bytes mismatch: " + entry["path"])

        head = git(root, "rev-parse", "HEAD").decode().strip()
        need(head == BASE, "HEAD changed: " + head)
        original_manifest = safe(root, ORIGINAL + "/FILES.json").read_bytes()
        need(sha(original_manifest) == "ab9a99b7e3d3e924908bcfff062a4b1c764ecf2fa88058d6e50ee208a4adac1b", "original package manifest changed")
        original_checker = safe(root, ORIGINAL + "/tools/check_hub_shell.py").read_bytes()
        need(len(original_checker) == 8075 and sha(original_checker) == "5319076e860df0414bef587cbd6739da03b7f1916af09bbc8e6989fdf20b0d83", "original checker changed")

        text = safe(root, STATUS).read_text(encoding="utf-8-sig")
        rows = re.findall(r"^\|\s*([A-Z][A-Z0-9_]+)\s*\|\s*(COMPLETE|CURRENT|LOCKED)\s*\|\s*$", text, re.M)
        need(len(rows) == 293 and len(dict(rows)) == 293, "status row count/duplicate mismatch")
        state = dict(rows)
        counts = dict(collections.Counter(value for _, value in rows))
        current = re.findall(r"## Current Task\s+```text\s+([^\r\n]+)\s+```", text)
        need(state.get(TASK) == "CURRENT", "SV5_11 is not CURRENT")
        need(counts == {"COMPLETE": 256, "CURRENT": 1, "LOCKED": 36}, "status counts mismatch")
        need(current == ["TASKS/SV5_11_HUB_SHELL.md"], "Current block mismatch")

        installed = safe(root, "MapDesign/MCP/TASKS/SV5_11_HUB_SHELL.md").read_bytes()
        need(sha(installed) == "491f9b9df0b498bc6351efe1c9ff60415fa2bdb91b7e559cc20af558e5f8b97b", "installed Task mismatch")
        print("PASS_CHECKER_SCOPE_FIX")
        print(json.dumps({"task": TASK, "phase": "CURRENT", "head": head, "counts": counts}, indent=2))
    except (Blocked, OSError, ValueError, KeyError, json.JSONDecodeError) as error:
        print("BLOCKED:", error, file=sys.stderr)
        raise SystemExit(2)


if __name__ == "__main__":
    main()
