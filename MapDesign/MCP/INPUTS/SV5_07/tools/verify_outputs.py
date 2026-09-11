"""Independent read-only release checks for actual SV5_07 output content."""
import csv
import hashlib
import io
import json
import pathlib
import subprocess
import xml.etree.ElementTree as ET

ROOT = pathlib.Path(__file__).resolve().parents[5]
OUT = ROOT / 'MapDesign/MCP/GENERATED/SV5_07'

def main():
    lock = json.loads((ROOT / 'MapDesign/MCP/INPUTS/SV5_07/SOURCE_LOCK.json').read_text(encoding='utf-8'))
    isolated = []
    for path in lock['owned_existing']:
        if '/Tests/' not in path:
            continue
        old = subprocess.check_output(['git', '-C', str(ROOT), 'show', lock['predecessor']['commit'] + ':' + path]).decode('utf-8').replace('\r\n', '\n')
        new = (ROOT / path).read_text(encoding='utf-8')
        expected = old.replace('"SV5_06_FIX04", "legacy_exports"', '"SV5_07", "_work", "legacy_exports"')
        if path.endswith('Sv5SpaceGraphFix04Tests.cs'):
            expected = expected.replace('"../MapDesign/MCP/GENERATED/SV5_06_FIX04"', '"../MapDesign/MCP/GENERATED/SV5_07/_work/legacy_exports/sv5_06_fix04"')
        assert new == expected and new != old, 'Existing test modified beyond output isolation: ' + path
        isolated.append(path)
    assert len(isolated) == 8
    cases = json.loads((OUT / 'diversity.json').read_text(encoding='utf-8'))['cases']
    assert [c['case'] for c in cases] == ['default/OFF', 'default/ON', 'repeat/OFF', 'repeat/ON']
    assert [c['eligible_near_pairs'] for c in cases] == [0, 0, 3, 2]
    assert cases[1]['observation'] == 'NO_ELIGIBLE_REPEAT'
    decisions = list(csv.DictReader(io.StringIO((OUT / 'decisions.csv').read_text(encoding='utf-8'))))
    pairs = list(csv.DictReader(io.StringIO((OUT / 'pairs.csv').read_text(encoding='utf-8'))))
    metrics = []
    for i, c in enumerate(cases):
        p = c['plan']
        assert p['seed'] == 1304 and p['world']['width'] == 624 and p['world']['height'] == 416
        assert c['validation']['status'] == 'PASS'
        assert c['validation']['physical_product_errors'] == 0 and c['validation']['gate_geometry_errors'] == 0
        assert c['validation']['projection_orders'] == 6
        assert not p['readiness']['composed_geometry'] and not p['readiness']['player']
        selected = [d for d in decisions if d['case'] == c['case'] and d['decision'] != 'SOFT_REJECTED']
        assert len(selected) == len(c['requests'])
        for d in selected:
            place = next(v for v in p['places'] if v['formation_id'] == d['formation_id'])
            assert place['bounds'] == {k: int(d[k]) for k in ['x', 'y', 'width', 'height']}
            assert d['plan_digest'] == p['plan_digest'] and d['diversity_digest'] == c['diversity_digest']
        cpairs = [r for r in pairs if r['case'] == c['case']]
        assert sum(r['eligible_near'] == 'true' for r in cpairs) == c['eligible_near_pairs']
        by_id = {v['formation_id']: v for v in p['places']}
        for r in cpairs:
            assert r['first_formation'] != r['second_formation']
            a, b = by_id[r['first_formation']]['bounds'], by_id[r['second_formation']]['bounds']
            gap = max(0, b['x']-a['x']-a['width'], a['x']-b['x']-b['width']) + max(0, b['y']-a['y']-a['height'], a['y']-b['y']-b['height'])
            assert gap == int(r['gap']) and (gap <= 36) == (r['near'] == 'true')
        if i % 2:
            previous = cases[i-1]['plan']
            assert sorted((v['family'], v['kind'], v['bounds']['width'], v['bounds']['height']) for v in p['places']) == sorted((v['family'], v['kind'], v['bounds']['width'], v['bounds']['height']) for v in previous['places'])
            assert p['gates'] == previous['gates']
            sub = OUT / ('default' if i == 1 else 'repeat')
            assert json.loads((sub / 'space_graph.json').read_text()) == p
            matrix = json.loads((sub / 'physical_transition_matrix.json').read_text())
            assert matrix['plan_digest'] == p['plan_digest'] and matrix['transitions']
            assert all(r['success'] and r['physical_reachable'] == r['expected_open'] for r in matrix['transitions'])
            orders = sorted(set(r['resource_order'] for r in matrix['transitions']))
            assert len(orders) == 6
            metrics.append({'case': c['case'], 'matrix_rows': len(matrix['transitions']), 'orders': orders,
                            'places': len(p['places']), 'plan_digest': p['plan_digest'], 'fallbacks': c['fallback_count']})
    old = ET.parse(ROOT / 'MapDesign/MCP/GENERATED/SV5_06_FIX04/focused_results.xml').getroot()
    raw = (OUT / 'focused_results.xml').read_bytes()
    new = ET.fromstring(raw)
    old_names = {e.get('fullname') for e in old.findall('.//test-case')}
    test_cases = new.findall('.//test-case')
    names = {e.get('fullname') for e in test_cases}
    assert len(old_names) == 70 and old_names <= names
    assert all(e.get('result') == 'Passed' for e in test_cases)
    assert len(test_cases) == int(new.get('total')) == int(new.get('passed'))
    assert new.get('failed') == new.get('skipped') == '0'
    print(json.dumps({'existing_tests_unchanged': len(old_names), 'new_tests': sorted(names-old_names),
                      'actual_run': new.attrib, 'xml_raw_sha256': hashlib.sha256(raw).hexdigest(),
                      'xml_raw_bytes': len(raw), 'output_path_only_test_diffs': len(isolated), 'metrics': metrics}, indent=2))

if __name__ == '__main__':
    main()
