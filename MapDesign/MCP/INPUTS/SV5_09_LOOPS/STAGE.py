#!/usr/bin/env python3
"""Verify SV5_09 package/live predecessor and optionally stage one INBOX MD. Never Apply/Finalize/commit."""
import argparse
import collections
import hashlib
import json
import os
import re
import subprocess
import sys
from pathlib import Path, PurePosixPath

TASK = "SV5_09_LOOPS"
PREV = "SV5_08_FIX02"
NEXT = "SV5_10_SIDEPATH"
HERE = Path(__file__).resolve().parent
PREFIX = "MapDesign/MCP/INPUTS/SV5_09_LOOPS"
STATUS = "MapDesign/MCP/06_IMPLEMENTATION_STATUS.md"
MASTER = "MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md"
PROTOCOL = "MapDesign/MCP/SV5/02_PROTOCOL_V5.md"
INBOX = "MapDesign/MCP_INBOX"

class Blocked(Exception):
    pass

def need(ok, reason):
    if not ok:
        raise Blocked(reason)

def sha(data):
    return hashlib.sha256(data).hexdigest()

def safe(root, relative):
    rel = PurePosixPath(relative)
    need(relative and not rel.is_absolute() and "\\" not in relative and ":" not in relative
         and all(p not in ("", ".", "..") for p in relative.split("/")), "Unsafe path: " + relative)
    out = root.joinpath(*rel.parts)
    for p in (out, *out.parents):
        if p == root:
            break
        need(not p.is_symlink(), "Symlink path rejected: " + relative)
    need(out.resolve().is_relative_to(root.resolve()), "Path escapes project: " + relative)
    return out

def load(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))

def package(expected):
    need(re.fullmatch(r"[0-9a-f]{64}", expected) is not None, "Expected manifest SHA required")
    need(sha((HERE / "FILES.json").read_bytes()) == expected, "Package manifest SHA mismatch")
    manifest = load(HERE / "FILES.json")
    need(manifest["format"] == "sv5_09_loops_package_v1" and manifest["task"] == TASK, "Wrong package")
    names = [f["path"] for f in manifest["files"]]
    need(len(names) == len(set(names)), "Duplicate package path")
    for f in manifest["files"]:
        b = safe(HERE, f["path"]).read_bytes()
        need(len(b) == f["bytes"] and sha(b) == f["sha256"], "Package bytes mismatch: " + f["path"])
    lock = load(HERE / "SOURCE_LOCK.json")
    need(lock["format"] == "sv5_09_loops_source_lock_v1", "Wrong source lock")
    body = (HERE / (TASK + ".md")).read_bytes()
    need(sha(body) == manifest["bound_task_sha256"], "Bound Task SHA mismatch")
    text = body.decode("utf-8")
    need(len(text.splitlines()) <= 300, "Task exceeds 300 lines")
    header = ("---\nmcp_patch:\n  format: single_task_v1\n  task_id: " + TASK +
        "\n  task_file: TASKS/" + TASK + ".md\n  requires_current_task: NONE\n  requires_completed_task: " + PREV +
        "\n  requires_result:\n    path: REPORTS/" + PREV + "_RESULT.md\n    status: PASS\n    sha256: " +
        lock["predecessor"]["result_sha256"] + "\n  requires_installed_task:\n    path: TASKS/" + PREV +
        ".md\n    sha256: " + lock["predecessor"]["task_sha256"] + "\n  sets_current_task: " + TASK + "\n---\n")
    need(text.startswith(header), "Native metadata mismatch")
    need(len(lock["files"]) == len({f["path"] for f in lock["files"]}), "Duplicate source pin")
    return lock, manifest

def git(root, *args, check=True):
    p = subprocess.run(["git", "-C", str(root), *args], stdout=subprocess.PIPE, stderr=subprocess.PIPE)
    if check:
        need(p.returncode == 0, "Git failed: " + " ".join(args[:4]))
    return p.stdout

def repo(root):
    top = Path(os.fsdecode(git(root, "rev-parse", "--show-toplevel")).strip()).resolve()
    need(root.is_relative_to(top), "Unity root is outside repository")
    prefix = root.relative_to(top).as_posix()
    return top, "" if prefix == "." else prefix + "/"

def status_state(data):
    text = data.decode("utf-8-sig")
    rows = re.findall(r"^\|\s*([A-Z][A-Z0-9_]+)\s*\|\s*(COMPLETE|CURRENT|LOCKED)\s*\|\s*$", text, re.M)
    need(len(rows) == 292 and len(dict(rows)) == 292, "Status row count/duplicate mismatch")
    cur = re.findall(r"## Current Task\s+```text\s+([^\r\n]+)\s+```", text)
    need(len(cur) == 1, "Current Task block mismatch")
    return dict(rows), cur[0].strip(), dict(collections.Counter(v for _, v in rows))

def check_state(root, lock, post):
    data = safe(root, STATUS).read_bytes()
    rows, current, counts = status_state(data)
    need(rows.get(PREV) == "COMPLETE" and rows.get(NEXT) == "LOCKED", "Predecessor/successor state mismatch")
    need(sha(safe(root, MASTER).read_bytes()) == lock["master_sha256"], "Master bytes changed")
    master = safe(root, MASTER).read_text(encoding="utf-8-sig")
    need(len(re.findall(r"^\|[^\r\n]*\b" + TASK + r"\b[^\r\n]*\|\s*$", master, re.M)) == 1,
         "SV5_09 must be registered exactly once")
    phase = rows.get(TASK)
    if not post:
        need(phase == "LOCKED" and current == "NONE" and counts == {"COMPLETE": 253, "LOCKED": 39},
             "Stage requires 253 COMPLETE / 39 LOCKED / Current NONE")
        need(sha(data) == lock["status_before_sha256"], "Preflight Status bytes changed")
    else:
        need(phase in ("CURRENT", "COMPLETE"), "Post requires CURRENT or COMPLETE")
        need(current == ("TASKS/" + TASK + ".md" if phase == "CURRENT" else "NONE"), "Current block mismatch")
        expected = {"COMPLETE": 253, "CURRENT": 1, "LOCKED": 38} if phase == "CURRENT" else {"COMPLETE": 254, "LOCKED": 38}
        need(counts == expected, "Post status counts differ")
        original, n = re.subn(rb"(?m)^(\|\s*" + TASK.encode() + rb"\s*\|\s*)(CURRENT|COMPLETE)(\s*\|)",
                              rb"\g<1>LOCKED\3", data)
        need(n == 1, "Could not project task row")
        if phase == "CURRENT":
            original, n = re.subn(rb"(## Current Task\s+```text\s+)TASKS/" + TASK.encode() + rb"\.md",
                                  rb"\g<1>NONE", original)
            need(n == 1, "Could not project Current block")
        need(sha(original) == lock["status_before_sha256"], "Status changed outside native lifecycle fields")
    return phase, counts

def discover_predecessor(root, lock):
    top, prefix = repo(root)
    spec = lock["predecessor"]["finalize_commit_discovery"]
    result_path = prefix + spec["result_path"]
    commits = git(root, "log", "--all", "--format=%H", "--", result_path).decode().splitlines()
    matches = []
    for commit in commits:
        b = git(root, "show", commit + ":" + result_path, check=False)
        if sha(b) != lock["predecessor"]["result_sha256"]:
            continue
        parents = git(root, "rev-list", "--parents", "-n", "1", commit).decode().strip().split()
        if parents != [commit, spec["expected_parent"]]:
            continue
        paths = git(root, "diff-tree", "--no-commit-id", "--name-only", "-r", commit).decode().splitlines()
        if sorted(paths) != sorted(prefix + p for p in spec["expected_commit_paths"]):
            continue
        probes = [("MapDesign/MCP/TASKS/SV5_08_FIX02.md", lock["predecessor"]["task_sha256"]),
                  ("MapDesign/MCP/GENERATED/SV5_08_FIX02/finalize_audit.json", lock["predecessor"]["audit_sha256"]),
                  (STATUS, lock["status_before_sha256"]), (MASTER, lock["master_sha256"])]
        if all(sha(git(root, "show", commit + ":" + prefix + p, check=False)) == h for p, h in probes):
            matches.append(commit)
    need(len(matches) == 1, "Could not uniquely discover exact SV5_08_FIX02 finalize commit")
    git(root, "merge-base", "--is-ancestor", matches[0], "HEAD")
    return matches[0], top, prefix

def check_sources(root, lock, post, phase):
    checked = 0
    for f in lock["files"]:
        if post and f["phase"] == "BEFORE_ONLY":
            continue
        b = safe(root, f["path"]).read_bytes()
        need(sha(b) == f["worktree_sha256"] and len(b) == f["worktree_bytes"], "Worktree bytes mismatch: " + f["path"])
        checked += 1
    for path, expected in [("MapDesign/MCP/REPORTS/SV5_08_FIX02_RESULT.md", lock["predecessor"]["result_sha256"]),
                           ("MapDesign/MCP/TASKS/SV5_08_FIX02.md", lock["predecessor"]["task_sha256"])]:
        need(sha(safe(root, path).read_bytes()) == expected, "Predecessor bytes mismatch: " + path)
    prev = safe(root, "MapDesign/MCP/REPORTS/SV5_08_FIX02_RESULT.md").read_text(encoding="utf-8-sig")
    need(re.search(r"^TASK: SV5_08_FIX02\r?$", prev, re.M) and re.search(r"^STATUS: PASS\r?$", prev, re.M),
         "Predecessor Result marker mismatch")
    if post:
        b, suffix = safe(root, PROTOCOL).read_bytes(), (HERE / "PROTOCOL_APPEND.md").read_bytes()
        need(b.endswith(suffix) and sha(b[:-len(suffix)]) == lock["protocol_before_sha256"], "Protocol exact append mismatch")
    if phase == "COMPLETE":
        report = safe(root, lock["result_path"]).read_text(encoding="utf-8-sig")
        need(re.search(r"^TASK: " + TASK + r"\r?$", report, re.M) and re.search(r"^STATUS: PASS\r?$", report, re.M),
             "COMPLETE requires matching PASS Result")
    return checked

def check_git(root, lock, post):
    fix02, _, prefix = discover_predecessor(root, lock)
    impl = lock["predecessor"]["implementation_commit"]
    git(root, "merge-base", "--is-ancestor", impl, "HEAD")
    need(git(root, "rev-list", "--parents", "-n", "1", impl).decode().strip().split() ==
         [impl, lock["predecessor"]["implementation_parent"]], "Implementation commit parent mismatch")
    for f in lock["commit_blobs"]:
        b = git(root, "show", impl + ":" + prefix + f["path"])
        oid = hashlib.sha1(b"blob " + str(len(b)).encode() + b"\0" + b).hexdigest()
        need(sha(b) == f["sha256"] and len(b) == f["bytes"] and oid == f["git_blob_oid"], "Implementation blob mismatch: " + f["path"])
    if not post:
        scope = lock["owned_existing"] + lock["owned_new"] + lock["owned_new_roots"] + [lock["result_path"]]
        need(not git(root, "status", "--porcelain", "--untracked-files=all", "--", *scope), "Dirty changes overlap SV5_09 ownership")
    return {"fix02_finalize_commit": fix02, "implementation_commit": impl, "commit_blobs": len(lock["commit_blobs"])}

def candidates(root):
    d = safe(root, INBOX)
    if not d.exists():
        return []
    out = []
    for p in d.iterdir():
        need(not p.is_symlink(), "INBOX symlink rejected: " + p.name)
        if (p.is_file() and p.suffix.lower() == ".md") or (p.is_dir() and not (p / ".APPLIED").exists()):
            out.append(p)
    return sorted(out)

def local(root, lock, manifest, post=False):
    need(all((root / p).is_dir() for p in ("Assets", "Packages", "ProjectSettings", "MapDesign/MCP")), "Use Unity project root")
    need(HERE == safe(root, PREFIX).resolve(), "Run installed INPUTS helper")
    phase, counts = check_state(root, lock, post)
    checked = check_sources(root, lock, post, phase)
    proof = check_git(root, lock, post)
    bound = (HERE / (TASK + ".md")).read_bytes()
    target = safe(root, INBOX + "/" + TASK + ".md")
    if post:
        need(not candidates(root), "INBOX must be empty after native Apply")
        for p in ("MapDesign/MCP/TASKS/" + TASK + ".md", "MapDesign/MCP_ARCHIVE/" + TASK + ".md"):
            need(safe(root, p).read_bytes() == bound, "Installed/Archive Task mismatch: " + p)
    else:
        inbox = candidates(root)
        need(not inbox or (inbox == [target] and target.read_bytes() == bound), "INBOX has another candidate")
        for p in lock["owned_new"] + lock["owned_new_roots"] + [lock["result_path"]]:
            need(not safe(root, p).exists(), "Unexpected pre-existing output: " + p)
    return {"task": TASK, "phase": phase, "counts": counts, "worktree_files_checked": checked, "git": proof,
            "bound_task_sha256": manifest["bound_task_sha256"]}

def stage(root, lock, manifest):
    result = local(root, lock, manifest)
    target = safe(root, INBOX + "/" + TASK + ".md")
    data = (HERE / (TASK + ".md")).read_bytes()
    changed = []
    if not target.exists():
        target.parent.mkdir(parents=True, exist_ok=True)
        with target.open("xb") as f:
            f.write(data); f.flush(); os.fsync(f.fileno())
        changed.append(INBOX + "/" + TASK + ".md")
    need(target.read_bytes() == data and candidates(root) == [target], "Staged candidate mismatch")
    result.update(status="PASS_STAGED_ONLY", changed=changed, native_apply=False)
    return result

def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--mode", choices=("package", "preflight", "stage", "post-readonly"), required=True)
    ap.add_argument("--expected-manifest-sha", required=True)
    ap.add_argument("--project-root", type=Path)
    a = ap.parse_args()
    lock, manifest = package(a.expected_manifest_sha)
    if a.mode == "package":
        result = {"status": "PASS_PACKAGE_ONLY", "task": TASK, "files": len(manifest["files"]), "native_apply": False}
    else:
        need(a.project_root is not None, "--project-root is required")
        root = a.project_root.resolve()
        result = stage(root, lock, manifest) if a.mode == "stage" else local(root, lock, manifest, a.mode == "post-readonly")
        if a.mode != "stage":
            result["status"] = "PASS_READONLY_LOCAL_AND_GIT" if a.mode == "post-readonly" else "PASS_PREFLIGHT_LOCAL_AND_GIT"
    print(json.dumps(result, ensure_ascii=False, indent=2))

if __name__ == "__main__":
    try:
        main()
    except (Blocked, OSError, ValueError, KeyError, TypeError) as e:
        print(json.dumps({"status": "BLOCKED", "reason": str(e), "native_apply": False}, ensure_ascii=False))
        sys.exit(2)
