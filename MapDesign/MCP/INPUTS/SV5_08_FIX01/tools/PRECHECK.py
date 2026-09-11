#!/usr/bin/env python3
"""SV5_08_FIX01 package verification and single-MD staging; no Apply, Unity, Finalize or commit."""
import argparse
import collections
import hashlib
import json
import os
import re
import subprocess
import sys
from pathlib import Path, PurePosixPath

TASK = 'SV5_08_FIX01'
PREV = 'SV5_08_INFILL'
NEXT = 'SV5_09_LOOPS'
HERE = Path(__file__).resolve().parent.parent
PREFIX = 'MapDesign/MCP/INPUTS/SV5_08_FIX01'
STATUS = 'MapDesign/MCP/06_IMPLEMENTATION_STATUS.md'
MASTER = 'MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md'
PROTOCOL = 'MapDesign/MCP/SV5/02_PROTOCOL_V5.md'
INBOX = 'MapDesign/MCP_INBOX'

class Blocked(Exception):
    pass

def need(ok, reason):
    if not ok:
        raise Blocked(reason)

def sha(data):
    return hashlib.sha256(data).hexdigest()

def read_json(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))

def safe(root, relative):
    rel = PurePosixPath(relative)
    need(relative and not rel.is_absolute() and '\\' not in relative and ':' not in relative
         and all(p not in ('', '.', '..') for p in relative.split('/')), 'Unsafe path: ' + relative)
    result = root.joinpath(*rel.parts)
    for p in (result, *result.parents):
        if p == root:
            break
        need(not p.is_symlink(), 'Symlink path is not accepted: ' + relative)
    need(result.resolve().is_relative_to(root.resolve()), 'Path escapes project: ' + relative)
    return result

def package(expected):
    need(re.fullmatch('[0-9a-f]{64}', expected) is not None, 'Expected manifest SHA is required')
    need(sha((HERE / 'FILES.json').read_bytes()) == expected, 'Package manifest SHA mismatch')
    m = read_json(HERE / 'FILES.json')
    need(m['task'] == TASK and m['format'] == 'sv5_08_fix01_package_v1', 'Wrong package')
    names = [f['path'] for f in m['files']]
    need(len(names) == len(set(names)), 'Duplicate package path')
    for f in m['files']:
        b = safe(HERE, f['path']).read_bytes()
        need(sha(b) == f['sha256'] and len(b) == f['bytes'], 'Package bytes mismatch: ' + f['path'])
    lock = read_json(HERE / 'SOURCE_LOCK.json')
    need(lock['format'] == 'sv5_08_fix01_source_lock_v1', 'Wrong source lock format')
    body = (HERE / (TASK + '.md')).read_bytes()
    need(sha(body) == m['bound_task_sha256'], 'Bound Task SHA mismatch')
    text = body.decode('utf-8')
    need(len(text.splitlines()) <= 300 and text.startswith('---\nmcp_patch:\n'), 'Invalid native Task')
    expected_header = ('---\nmcp_patch:\n  format: single_task_v1\n  task_id: ' + TASK +
        '\n  task_file: TASKS/' + TASK + '.md\n  requires_current_task: NONE\n  requires_completed_task: ' + PREV +
        '\n  requires_result:\n    path: REPORTS/' + PREV + '_RESULT.md\n    status: PASS\n    sha256: ' +
        lock['predecessor']['result_sha256'] + '\n  requires_installed_task:\n    path: TASKS/' + PREV +
        '.md\n    sha256: ' + lock['predecessor']['task_sha256'] + '\n  sets_current_task: ' + TASK + '\n---\n')
    need(text.startswith(expected_header), 'Native metadata mismatch')
    need(len(lock['files']) == len({f['path'] for f in lock['files']}), 'Duplicate source lock')
    for f in lock['files']:
        safe(HERE, f['path'])
        need(f['phase'] in ('ALWAYS', 'BEFORE_ONLY') and re.fullmatch('[0-9a-f]{64}', f['worktree_sha256']),
             'Invalid worktree pin: ' + f['path'])
    for f in lock['commit_blobs']:
        need(re.fullmatch('[0-9a-f]{64}', f['sha256']) and re.fullmatch('[0-9a-f]{40}', f['git_blob_oid'])
             and isinstance(f['bytes'], int), 'Missing separate commit blob pin: ' + f['path'])
    reg = read_json(HERE / 'REGISTRATION.json')
    need(reg['target_task']==TASK and reg['status']['after_sha256']==lock['status_before_sha256']
         and reg['master']['after_sha256']==lock['master_sha256'], 'Registration/source lock mismatch')
    return lock, m

def status_state(data):
    text = data.decode('utf-8-sig')
    rows = re.findall(r'^\|\s*([A-Z][A-Z0-9_]+)\s*\|\s*(COMPLETE|CURRENT|LOCKED)\s*\|\s*$', text, re.M)
    need(len(rows) in (290,291) and len(dict(rows)) == len(rows), 'Status row count/duplicate mismatch')
    current = re.findall(r'## Current Task\s+```text\s+([^\r\n]+)\s+```', text)
    need(len(current) == 1, 'Current Task block must occur once')
    return dict(rows), current[0].strip(), dict(collections.Counter(v for k, v in rows))

def check_state(root, lock, post):
    data = safe(root, STATUS).read_bytes()
    rows, current, counts = status_state(data)
    need(rows.get(PREV) == 'COMPLETE' and rows.get(NEXT) == 'LOCKED', 'Predecessor/successor state mismatch')
    master = safe(root, MASTER).read_bytes()
    need(sha(master) == lock['master_sha256'], 'Master bytes changed')
    registered = re.findall(r'^\|[^\r\n]*\b' + TASK + r'\b[^\r\n]*\|\s*$', master.decode('utf-8-sig'), re.M)
    need(len(registered) == 1, 'SV5_08_FIX01 must already be registered exactly once in Master')
    need(len(rows)==291, 'Registered status must have291 rows')
    phase = rows.get(TASK)
    if not post:
        need(phase == 'LOCKED' and current == 'NONE', 'Stage requires SV5_08_FIX01 LOCKED / Current NONE')
        need(counts == {'COMPLETE': 251, 'LOCKED': 40}, 'Preflight status counts differ')
        need(sha(data) == lock['status_before_sha256'], 'Preflight Status bytes changed')
    else:
        need(phase in ('CURRENT', 'COMPLETE'), 'Post-readonly requires SV5_08_FIX01 CURRENT or COMPLETE')
        need(current == ('TASKS/' + TASK + '.md' if phase == 'CURRENT' else 'NONE'), 'Current Task mismatch')
        expected = {'COMPLETE': 251, 'CURRENT': 1, 'LOCKED': 39} if phase == 'CURRENT' else {'COMPLETE': 252, 'LOCKED': 39}
        need(counts == expected, 'Post status counts differ')
        original, n = re.subn(rb'(?m)^(\|\s*' + TASK.encode() + rb'\s*\|\s*)(CURRENT|COMPLETE)(\s*\|)',
                             rb'\g<1>LOCKED\3', data)
        need(n == 1, 'Could not project Status row')
        if phase == 'CURRENT':
            original, n = re.subn(rb'(## Current Task\s+```text\s+)TASKS/' + TASK.encode() + rb'\.md',
                                 rb'\g<1>NONE', original)
            need(n == 1, 'Could not project Current block')
        need(sha(original) == lock['status_before_sha256'], 'Status changed outside native own-row/Current fields')
    return phase, counts

def candidates(root):
    directory = safe(root, INBOX)
    if not directory.exists():
        return []
    need(directory.is_dir(), 'INBOX is not a directory')
    result = []
    for p in directory.iterdir():
        need(not p.is_symlink(), 'INBOX contains a symlink: ' + p.name)
        if (p.is_file() and p.suffix.lower() == '.md') or (p.is_dir() and not (p / '.APPLIED').exists()):
            result.append(p)
    return sorted(result)

def check_sources(root, lock, post, phase):
    checked = 0
    for f in lock['files']:
        if post and f['phase'] == 'BEFORE_ONLY':
            continue
        b = safe(root, f['path']).read_bytes()
        need(sha(b) == f['worktree_sha256'] and ('worktree_bytes' not in f or len(b) == f['worktree_bytes']),
             'Worktree bytes mismatch: ' + f['path'])
        checked += 1
    previous = safe(root, 'MapDesign/MCP/REPORTS/' + PREV + '_RESULT.md').read_text(encoding='utf-8-sig')
    need(re.search(r'^TASK: ' + PREV + r'\r?$', previous, re.M) and re.search(r'^STATUS: PASS\r?$', previous, re.M),
         'Predecessor is not a matching PASS Result')
    if post:
        b = safe(root, PROTOCOL).read_bytes()
        suffix = (HERE / 'PROTOCOL_APPEND.md').read_bytes()
        appended = b.endswith(suffix) and sha(b[:-len(suffix)]) == lock['protocol_before_sha256']
        need(appended or (phase == 'CURRENT' and sha(b) == lock['protocol_before_sha256']),
             'Protocol must preserve original bytes and append the exact suffix once')
    if phase == 'COMPLETE':
        report = safe(root, lock['result_path']).read_text(encoding='utf-8-sig')
        need(re.search(r'^TASK: ' + TASK + r'\r?$', report, re.M) and re.search(r'^STATUS: PASS\r?$', report, re.M)
             and not re.search(r'^STATUS: (FAIL|BLOCKED)\r?$', report, re.M), 'COMPLETE requires matching PASS Result')
    return checked

def git(root, *args):
    p = subprocess.run(['git', '-C', str(root), *args], stdout=subprocess.PIPE, stderr=subprocess.PIPE)
    need(p.returncode == 0, 'Live Git verification failed: ' + ' '.join(args[:3]))
    return p.stdout

def check_git(root, lock, post):
    top = Path(os.fsdecode(git(root, 'rev-parse', '--show-toplevel')).strip()).resolve()
    need(root.is_relative_to(top), 'Unity root is outside repository')
    prefix = root.relative_to(top).as_posix()
    prefix = '' if prefix == '.' else prefix + '/'
    commit, parent = lock['predecessor']['commit'], lock['predecessor']['parent']
    git(root, 'merge-base', '--is-ancestor', commit, 'HEAD')
    need(git(root, 'rev-list', '--parents', '-n', '1', commit).decode().strip().split() == [commit, parent],
         'Predecessor commit parent mismatch')
    for f in lock['commit_blobs']:
        b = git(root, 'show', commit + ':' + prefix + f['path'])
        oid = hashlib.sha1(b'blob ' + str(len(b)).encode() + b'\0' + b).hexdigest()
        need(sha(b) == f['sha256'] and len(b) == f['bytes'] and oid == f['git_blob_oid'],
             'Predecessor commit blob mismatch: ' + f['path'])
    if not post:
        owned = lock['owned_existing'] + lock['owned_new'] + lock['owned_new_roots'] + [lock['result_path']]
        need(not git(root, 'status', '--porcelain', '--untracked-files=all', '--', *owned),
             'Existing dirty changes overlap SV5_08_FIX01 implementation/evidence paths')
    return {'predecessor_commit': commit, 'parent': parent, 'head': git(root, 'rev-parse', 'HEAD').decode().strip(),
            'commit_blob_checks': len(lock['commit_blobs']), 'verified_in_live_git': True}

def local(root, lock, manifest, post=False):
    need(all((root / p).is_dir() for p in ('Assets', 'Packages', 'ProjectSettings', 'MapDesign/MCP')),
         'Use the actual Unity project root')
    need(HERE == safe(root, PREFIX).resolve(), 'Run the helper in this project INPUTS/SV5_08_FIX01')
    phase, counts = check_state(root, lock, post)
    checked = check_sources(root, lock, post, phase)
    proof = check_git(root, lock, post)
    bound = (HERE / (TASK + '.md')).read_bytes()
    inbox = candidates(root)
    installed = 'MapDesign/MCP/TASKS/' + TASK + '.md'
    archived = 'MapDesign/MCP_ARCHIVE/' + TASK + '.md'
    if post:
        need(not inbox, 'INBOX must contain no candidate after native Apply')
        for p in (installed, archived):
            need(safe(root, p).read_bytes() == bound, 'Installed/Archive Task mismatch: ' + p)
    else:
        target = safe(root, INBOX + '/' + TASK + '.md')
        need(not inbox or (inbox == [target] and target.read_bytes() == bound),
             'INBOX has another or ambiguous candidate; preserve it and report its path')
        for p in (installed, archived):
            dest = safe(root, p)
            need(not dest.exists() or (dest.is_file() and dest.read_bytes() == bound), 'Task destination collision: ' + p)
        for p in lock['owned_new'] + lock['owned_new_roots'] + [lock['result_path']]:
            need(not safe(root, p).exists(), 'Unexpected pre-existing new output: ' + p)
    return {'task': TASK, 'phase': phase, 'counts': counts, 'worktree_files_checked': checked,
            'git': proof, 'bound_task_sha256': manifest['bound_task_sha256']}

def stage(root, lock, manifest):
    result = local(root, lock, manifest)
    target = safe(root, INBOX + '/' + TASK + '.md')
    data = (HERE / (TASK + '.md')).read_bytes()
    changed = []
    if target.exists():
        need(target.read_bytes() == data, 'INBOX byte collision')
    else:
        target.parent.mkdir(parents=True, exist_ok=True)
        with target.open('xb') as f:
            f.write(data)
            f.flush()
            os.fsync(f.fileno())
        changed.append(INBOX + '/' + TASK + '.md')
    need(target.read_bytes() == data and candidates(root) == [target], 'Staged byte/candidate mismatch')
    result.update(status='PASS_STAGED_ONLY', changed=changed, native_apply=False)
    return result

def pre_registration(root, lock, manifest):
    need(all((root / p).is_dir() for p in ('Assets', 'Packages', 'ProjectSettings', 'MapDesign/MCP')), 'Use actual Unity project root')
    need(HERE == safe(root, PREFIX).resolve(), 'Run helper inside this project INPUTS')
    reg = read_json(HERE / 'REGISTRATION.json')
    for key in ('status', 'master'):
        e=reg[key]
        need(sha(safe(root,e['path']).read_bytes())==e['before_sha256'], 'Registration before bytes mismatch: '+e['path'])
    rows,current,counts=status_state(safe(root,STATUS).read_bytes())
    need(len(rows)==290 and TASK not in rows and current=='NONE' and counts=={'COMPLETE':251,'LOCKED':39}, 'Registration before state mismatch')
    need(rows.get(PREV)=='COMPLETE' and rows.get(NEXT)=='LOCKED', 'Registration predecessor/successor mismatch')
    need(TASK not in safe(root,MASTER).read_text(encoding='utf-8-sig'), 'Task already occurs in Master')
    checked=check_sources(root,lock,False,'UNREGISTERED')
    proof=check_git(root,lock,False)
    need(not candidates(root), 'INBOX must have no candidate before registration; preserve existing entries')
    for p in lock['owned_new']+lock['owned_new_roots']+[lock['result_path'], 'MapDesign/MCP/TASKS/'+TASK+'.md','MapDesign/MCP_ARCHIVE/'+TASK+'.md']:
        need(not safe(root,p).exists(), 'Unexpected pre-existing output: '+p)
    need(not git(root,'status','--porcelain','--',STATUS,MASTER), 'Registration files have pre-existing dirty changes')
    return {'status':'PASS_PRE_REGISTRATION_READONLY','task':TASK,'worktree_files_checked':checked,'git':proof,'native_apply':False}

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--mode', choices=('package', 'pre-registration', 'preflight', 'stage', 'post-readonly'), required=True)
    parser.add_argument('--expected-manifest-sha', required=True)
    parser.add_argument('--project-root', type=Path)
    args = parser.parse_args()
    lock, manifest = package(args.expected_manifest_sha)
    if args.mode == 'package':
        result = {'status': 'PASS_PACKAGE_ONLY', 'task': TASK, 'files': len(manifest['files']), 'native_apply': False}
    else:
        need(args.project_root is not None, '--project-root is required')
        root = args.project_root.resolve()
        if args.mode == 'pre-registration':
            result = pre_registration(root, lock, manifest)
        elif args.mode == 'stage':
            result = stage(root, lock, manifest)
        else:
            result = local(root, lock, manifest, args.mode == 'post-readonly')
            result['status'] = 'PASS_READONLY_LOCAL_AND_GIT' if args.mode == 'post-readonly' else 'PASS_PREFLIGHT_LOCAL_AND_GIT'
    print(json.dumps(result, ensure_ascii=False, indent=2))

if __name__ == '__main__':
    try:
        main()
    except (Blocked, OSError, ValueError, KeyError, TypeError) as e:
        print(json.dumps({'status': 'BLOCKED', 'reason': str(e), 'native_apply': False}, ensure_ascii=False))
        sys.exit(2)
