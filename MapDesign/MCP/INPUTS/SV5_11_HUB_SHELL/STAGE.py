#!/usr/bin/env python3
"""Package/base/state verifier and one-file INBOX stager. Never Apply/Finalize/commit/push."""
import argparse
import collections
import hashlib
import json
import re
import subprocess
import sys
from pathlib import Path, PurePosixPath

TASK = "SV5_11_HUB_SHELL"
PREV = "SV5_10_SIDEPATH"
NEXT = "SV5_12_TREE_GRAB"
HERE = Path(__file__).resolve().parent
PREFIX = "MapDesign/MCP/INPUTS/" + TASK
STATUS = "MapDesign/MCP/06_IMPLEMENTATION_STATUS.md"
MASTER = "MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md"
INBOX = "MapDesign/MCP_INBOX"


class Blocked(Exception):
    pass


def need(condition, message):
    if not condition:
        raise Blocked(message)


def sha(data):
    return hashlib.sha256(data).hexdigest()


def load(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def safe(root, rel):
    item = PurePosixPath(rel)
    need(rel and not item.is_absolute() and "\\" not in rel and ":" not in rel, "unsafe path " + rel)
    need(all(part not in ("", ".", "..") for part in rel.split("/")), "unsafe path " + rel)
    path = root.joinpath(*item.parts)
    need(path.resolve().is_relative_to(root.resolve()), "path escape " + rel)
    return path


def git(root, *args, check=True):
    process = subprocess.run(["git", "-C", str(root), *args], stdout=subprocess.PIPE, stderr=subprocess.PIPE)
    if check:
        need(process.returncode == 0, "git failed: " + " ".join(args))
    return process


def verify_package(expected):
    need(re.fullmatch(r"[0-9a-f]{64}", expected or "") is not None, "expected manifest SHA required")
    manifest_path = HERE / "FILES.json"
    need(sha(manifest_path.read_bytes()) == expected, "package manifest SHA mismatch")
    manifest = load(manifest_path)
    need(manifest["schema"] == "SV5_11_HUB_SHELL_PACKAGE_V1" and manifest["task"] == TASK, "wrong package")
    names = []
    for entry in manifest["files"]:
        names.append(entry["path"])
        data = safe(HERE, entry["path"]).read_bytes()
        need(len(data) == entry["bytes"] and sha(data) == entry["sha256"], "package bytes mismatch: " + entry["path"])
    need(len(names) == len(set(names)), "duplicate package path")
    task = (HERE / (TASK + ".md")).read_bytes()
    need(sha(task) == manifest["bound_task_sha256"], "bound task mismatch")
    need(len(task.decode("utf-8").splitlines()) <= 300, "task over 300 lines")
    return manifest, load(HERE / "SOURCE_LOCK.json")


def status_state(data):
    text = data.decode("utf-8-sig")
    rows = re.findall(r"^\|\s*([A-Z][A-Z0-9_]+)\s*\|\s*(COMPLETE|CURRENT|LOCKED)\s*\|\s*$", text, re.M)
    need(len(rows) == 293 and len(dict(rows)) == 293, "status row count/duplicate mismatch")
    current = re.findall(r"## Current Task\s+```text\s+([^\r\n]+)\s+```", text)
    need(len(current) == 1, "Current block mismatch")
    return dict(rows), current[0].strip(), dict(collections.Counter(value for _, value in rows))


def verify_predecessor(root, lock):
    top = Path(git(root, "rev-parse", "--show-toplevel").stdout.decode().strip()).resolve()
    prefix = root.relative_to(top).as_posix()
    prefix = "" if prefix == "." else prefix + "/"
    predecessor = lock["predecessor"]
    commit = predecessor["commit"]
    head = git(root, "rev-parse", "HEAD").stdout.decode().strip()
    need(head == commit, "HEAD must equal predecessor commit: " + head)
    parents = git(root, "rev-list", "--parents", "-n", "1", commit).stdout.decode().strip().split()
    need(parents == [commit, predecessor["parent"]], "predecessor parent mismatch")
    for entry in predecessor["commit_entries"]:
        data = git(root, "show", commit + ":" + prefix + entry["path"]).stdout
        need(len(data) == entry["bytes"] and sha(data) == entry["sha256"], "predecessor blob mismatch: " + entry["path"])
    return commit


def inbox_candidates(root):
    directory = safe(root, INBOX)
    if not directory.exists():
        return []
    output = []
    for path in directory.iterdir():
        need(not path.is_symlink(), "INBOX symlink rejected: " + path.name)
        if path.is_file() and path.suffix.lower() == ".md":
            output.append(path)
        elif path.is_dir() and not (path / ".APPLIED").exists():
            output.append(path)
    return sorted(output)


def local_check(root, manifest, lock, post=False):
    need(all((root / path).is_dir() for path in ("Assets", "Packages", "ProjectSettings", "MapDesign/MCP")), "use Unity project root")
    need(HERE == safe(root, PREFIX).resolve(), "run installed INPUTS helper")
    predecessor = verify_predecessor(root, lock)
    rows, current, counts = status_state(safe(root, STATUS).read_bytes())
    need(rows.get(PREV) == "COMPLETE" and rows.get(NEXT) == "LOCKED", "predecessor/next state mismatch")
    master = safe(root, MASTER).read_text(encoding="utf-8-sig")
    need(len(re.findall(r"^\|[^\r\n]*\b" + TASK + r"\b[^\r\n]*\|\s*$", master, re.M)) == 1, "task must occur once in Master")
    phase = rows.get(TASK)
    if not post:
        need(phase == "LOCKED" and current == "NONE", "pre state mismatch")
        need(counts == {"COMPLETE": 256, "LOCKED": 37}, "pre counts mismatch")
        need(git(root, "diff", "--quiet", "HEAD", "--", STATUS, MASTER, check=False).returncode == 0, "Status/Master dirty")
        owned = lock["owned_existing"]
        need(git(root, "diff", "--quiet", "HEAD", "--", *owned, check=False).returncode == 0, "dirty changes overlap existing ownership")
        need(git(root, "diff", "--cached", "--quiet", "HEAD", "--", *owned, check=False).returncode == 0, "staged changes overlap ownership")
        for rel in lock["owned_new"]:
            need(not safe(root, rel).exists(), "unexpected pre-existing output: " + rel)
        target = safe(root, INBOX + "/" + TASK + ".md")
        candidates = inbox_candidates(root)
        need(not candidates or (candidates == [target] and target.read_bytes() == (HERE / (TASK + ".md")).read_bytes()), "INBOX has another candidate")
    else:
        need(phase in ("CURRENT", "COMPLETE"), "post state must be CURRENT or COMPLETE")
        expected_current = "TASKS/" + TASK + ".md" if phase == "CURRENT" else "NONE"
        need(current == expected_current, "Current block mismatch")
        expected_counts = {"COMPLETE": 256, "CURRENT": 1, "LOCKED": 36} if phase == "CURRENT" else {"COMPLETE": 257, "LOCKED": 36}
        need(counts == expected_counts, "post counts mismatch")
        need(not inbox_candidates(root), "INBOX must be empty after Apply")
        bound = (HERE / (TASK + ".md")).read_bytes()
        need(safe(root, "MapDesign/MCP/TASKS/" + TASK + ".md").read_bytes() == bound, "installed task mismatch")
        archive = safe(root, "MapDesign/MCP_ARCHIVE/" + TASK + ".md")
        if archive.exists():
            need(archive.read_bytes() == bound, "archive task mismatch")
        if phase == "COMPLETE":
            need(archive.exists(), "COMPLETE requires Archive")
            report = safe(root, "MapDesign/MCP/REPORTS/" + TASK + "_RESULT.md").read_text(encoding="utf-8-sig")
            need(re.search(r"^TASK: " + TASK + r"\r?$", report, re.M) is not None, "Result task mismatch")
            need(re.search(r"^STATUS: PASS\r?$", report, re.M) is not None, "COMPLETE requires PASS Result")
    return {
        "task": TASK,
        "phase": phase,
        "counts": counts,
        "predecessor": predecessor,
        "bound_task_sha256": manifest["bound_task_sha256"]
    }


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", type=Path, required=True)
    parser.add_argument("--expected-manifest-sha256", required=True)
    parser.add_argument("--mode", choices=("check", "stage", "post"), default="check")
    args = parser.parse_args()
    try:
        manifest, lock = verify_package(args.expected_manifest_sha256)
        root = args.root.resolve()
        result = local_check(root, manifest, lock, args.mode == "post")
        if args.mode == "stage":
            target = safe(root, INBOX + "/" + TASK + ".md")
            target.parent.mkdir(parents=True, exist_ok=True)
            if not target.exists():
                target.write_bytes((HERE / (TASK + ".md")).read_bytes())
            result["inbox"] = target.relative_to(root).as_posix()
        print("PASS_" + args.mode.upper())
        print(json.dumps(result, indent=2))
    except (Blocked, OSError, ValueError, KeyError, json.JSONDecodeError) as error:
        print("BLOCKED:", error, file=sys.stderr)
        raise SystemExit(2)


if __name__ == "__main__":
    main()
