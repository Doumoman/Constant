"""Package the verified FIX04 commit plus immutable read-set; never overwrite inputs."""
import collections
import hashlib
import json
import pathlib
import re
import subprocess
import xml.etree.ElementTree as ET
import zipfile

root = pathlib.Path.cwd().resolve()
def git(*args, data=None):
    return subprocess.check_output(['git', *args], cwd=root, input=data)
def digest(data):
    return hashlib.sha256(data).hexdigest()

assert pathlib.Path(git('rev-parse', '--show-toplevel').decode().strip()).resolve() == root
commit = git('rev-parse', 'HEAD').decode().strip()
parent = git('rev-parse', 'HEAD^').decode().strip()
assert parent == 'a0dc996a11d349927917a60410fb356871df15f2'
owned = set(git('diff-tree', '--no-commit-id', '--name-only', '-r', 'HEAD').decode().splitlines())
assert len(owned) == 247
assert not git('diff', '--cached', '--name-only').strip()
out = root / 'MapDesign/MCP/GENERATED/SV5_06_FIX04'
zip_path = out / 'SV5_06_FIX04_REVIEW.zip'
manifest_path = out / '_REVIEW_MANIFEST.json'
assert not zip_path.exists() and not manifest_path.exists(), 'Refuse to overwrite an existing review'
lock = json.loads((root / 'MapDesign/MCP/INPUTS/SV5_06_FIX04/SOURCE_LOCK.json').read_text(encoding='utf-8'))
locked = {x['path']: x for x in lock['files'] if x['phase'] == 'ALWAYS'}
assert len(locked) == 299
assert sum(p.startswith('MapDesign/MCP/GENERATED/SV5_06_FIX03/') for p in locked) == 161
paths = owned | set(locked)
for p in list(paths):
    if p.endswith('.cs') and (root / (p + '.meta')).is_file():
        paths.add(p + '.meta')
paths = sorted(paths)
for p in paths:
    f = root / p
    assert not pathlib.PurePosixPath(p).is_absolute() and '..' not in pathlib.PurePosixPath(p).parts
    assert f.is_file() and not f.is_symlink() and root in f.resolve().parents
    assert '/_work/' not in p and not p.lower().endswith('.zip')
    if p in locked:
        expected = locked[p].get('worktree_sha256', locked[p].get('sha256'))
        assert digest(f.read_bytes()) == expected, p

xml_path = 'MapDesign/MCP/GENERATED/SV5_06_FIX04/focused_results.xml'
xml = (root / xml_path).read_bytes()
x = ET.fromstring(xml)
assert (x.get('total'), x.get('passed'), x.get('failed'), x.get('skipped')) == ('70', '70', '0', '0')
assert digest(xml) == '236168155519ce6721f05b874edf15e3ae581383ba49dcf71f93c6b29344447b'
binding = json.loads((out / 'BINDING.json').read_text(encoding='utf-8'))['final_verification']
for item in binding['tested_source_hashes']:
    assert digest((root / item['path']).read_bytes()).upper() == item['sha'].upper(), item['path']
status = (root / 'MapDesign/MCP/06_IMPLEMENTATION_STATUS.md').read_text(encoding='utf-8')
counts = collections.Counter(re.findall(r'^\| [^|]+ \| (COMPLETE|CURRENT|LOCKED) \|$', status, re.M))
assert counts == {'COMPLETE': 249, 'LOCKED': 41}
assert re.search(r'## Current Task\s+```text\s+NONE\s+```', status)
assert '| SV5_07_DIVERSITY | LOCKED |' in status

# Read commit blobs in one batch. SHA-256 covers content, not the Git SHA-1 object identifier.
stream = git('cat-file', '--batch', data=''.join(commit + ':' + p + '\n' for p in paths).encode())
offset = 0
entries = []
for p in paths:
    end = stream.index(b'\n', offset)
    header = stream[offset:end].decode()
    offset = end + 1
    raw = (root / p).read_bytes()
    item = {'path': p, 'role': 'TASK_COMMIT' if p in owned else 'READ_ONLY_REFERENCE',
            'raw_sha256': digest(raw), 'raw_bytes': len(raw)}
    if header.endswith(' missing'):
        assert p not in owned
        item.update(commit_blob_sha256=None, commit_blob_bytes=None, commit_blob_status='NOT_IN_COMMIT_READ_ONLY_WORKTREE_REFERENCE')
    else:
        oid, kind, size = header.split()
        assert kind == 'blob'
        size = int(size)
        blob = stream[offset:offset + size]
        offset += size + 1
        item.update(git_blob_oid=oid, commit_blob_sha256=digest(blob), commit_blob_bytes=size,
                    raw_equals_blob=raw == blob)
        if p == xml_path:
            assert digest(blob) == '5839db7d956ff83c61df119dcbba839ce89bc222ef83a029c85765bf380242b8' and size == 75436
    entries.append(item)
assert offset == len(stream)
manifest = {'task': 'SV5_06_FIX04', 'status': 'PASS_REVIEW', 'commit': commit, 'parent': parent,
    'archive_contents': 'Raw working-tree bytes. Git commit content hashes are recorded independently.',
    'self_exclusion': 'Manifest and ZIP do not hash themselves; all payload entries below are verified.',
    'excluded': ['_work', 'old intermediate XML', 'DIAG ZIP', 'Unity Library/Temp', 'unrelated dirty files'],
    'task_owned_commit_paths': len(owned), 'payload_count': len(entries),
    'focused': binding, 'state': {'total': 290, 'COMPLETE': 249, 'CURRENT': 0, 'LOCKED': 41,
        'current_task': 'NONE', 'SV5_07_DIVERSITY': 'LOCKED'},
    'ComposedGeometryReady': False, 'PlayerVerified': False, 'files': entries}
manifest_bytes = (json.dumps(manifest, ensure_ascii=False, indent=2) + '\n').encode('utf-8')
with zipfile.ZipFile(zip_path, 'x', compression=zipfile.ZIP_DEFLATED, compresslevel=6) as z:
    for item in entries:
        raw = (root / item['path']).read_bytes()
        assert digest(raw) == item['raw_sha256']
        z.writestr(item['path'], raw)
    z.writestr('_REVIEW_MANIFEST.json', manifest_bytes)
with zipfile.ZipFile(zip_path) as z:
    assert len(z.namelist()) == len(entries) + 1
    assert z.testzip() is None
    for item in entries:
        raw = z.read(item['path'])
        assert len(raw) == item['raw_bytes'] and digest(raw) == item['raw_sha256']
with manifest_path.open('xb') as f:
    f.write(manifest_bytes)
print(json.dumps({'status': 'VERIFIED', 'commit': commit, 'zip': str(zip_path),
    'zip_sha256': digest(zip_path.read_bytes()), 'zip_bytes': zip_path.stat().st_size,
    'payload_files': len(entries), 'manifest_sha256': digest(manifest_bytes)}, ensure_ascii=False))
