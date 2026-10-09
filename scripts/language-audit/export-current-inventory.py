"""Сохраняет текущий реестр; старые отчёты не изменяет."""
import argparse
import csv
import json
from collections import Counter
from datetime import datetime
from pathlib import Path

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--snapshot-date', required=True, help='Дата нового снимка в формате YYYYMMDD')
args = parser.parse_args()
if len(args.snapshot_date) != 8 or not args.snapshot_date.isascii() or not args.snapshot_date.isdigit():
    parser.error('--snapshot-date должна содержать восемь цифр YYYYMMDD')
try:
    datetime.strptime(args.snapshot_date, '%Y%m%d')
except ValueError:
    parser.error('--snapshot-date должна быть существующей календарной датой')
root = Path(__file__).resolve().parents[2]
inventory = json.loads((root / '.artifacts/language-inventory-current/inventory.json').read_text(encoding='utf-8-sig'))
metadata = json.loads((root / '.artifacts/language-inventory-current/cli-metadata.json').read_text(encoding='utf-8-sig'))

def registry_signature(row):
    return (row['Name'], tuple((p['Name'], p['Type']) for p in row['Parameters']),
            row['ReturnType'], row['Binding'], row['OperandPolicy'], row['ResultPolicy'])

def metadata_signature(row):
    return (row['name'], tuple((p['name'], p['type']) for p in row['params']),
            row['returnType'], row['bindingKind'], row['operandBindingPolicy'], row['resultTypePolicy'])

assert Counter(map(registry_signature, inventory['Rows'])) == Counter(map(metadata_signature, metadata['builtins'])), 'Реестр и метаданные CLI расходятся'
assert not inventory['Integrity']['MissingVmDispatch'], 'Нет поддержки VM'
assert not inventory['Integrity']['DuplicateSignatures'], 'Повторяющиеся сигнатуры'

audit = root / 'docs/audit'
candidates = {}
registered_forms = {(row['Id'], row['Arity']) for row in inventory['Rows']}
registered_names = {(row['Id'], row['Arity'], row['Name']) for row in inventory['Rows']}
for path in sorted(audit.glob('*EVIDENCE_????????.csv')):
    if path.stem[-8:] > args.snapshot_date:
        continue
    for line, row in enumerate(csv.DictReader(path.open(encoding='utf-8-sig', newline='')), 2):
        identifier = row.get('BuiltinId') or row.get('builtin_id') or row.get('Id')
        if identifier:
            arity = row.get('Arity')
            if arity:
                assert (identifier, int(arity)) in registered_forms, f'{path.name}:{line}: неизвестная форма {identifier}/{arity}'
                if row.get('Name'):
                    assert (identifier, int(arity), row['Name']) in registered_names, f'{path.name}:{line}: имя не соответствует ID/Arity'
            candidates.setdefault(identifier, []).append((row.get('Arity'), f'{path.name}:{line}'))

target = audit / f'LANGUAGE_FUNCTION_MATRIX_{args.snapshot_date}.csv'
fields = ['Name', 'Id', 'Arity', 'Parameters', 'DeclaredReturn', 'Binding', 'OperandPolicy',
          'ResultPolicy', 'VmDispatch', 'Light', 'Dark', 'HistoricalIssue', 'SemanticStatus',
          'CandidateEvidence', 'CandidateLimit', 'TestMentions', 'SampleMentions', 'ManualMentions']
linked = 0
with target.open('w', encoding='utf-8-sig', newline='') as output:
    writer = csv.DictWriter(output, fieldnames=fields)
    writer.writeheader()
    for row in inventory['Rows']:
        links = [link for arity, link in candidates.get(row['Id'], [])
                 if not arity or int(arity) == row['Arity']]
        linked += bool(links)
        writer.writerow({
            **{key: row[key] for key in ['Name', 'Id', 'Arity', 'Binding', 'OperandPolicy', 'ResultPolicy', 'VmDispatch', 'Light', 'Dark', 'HistoricalIssue']},
            'Parameters': '; '.join(p['Name'] + ': ' + p['Type'] for p in row['Parameters']),
            'DeclaredReturn': row['ReturnType'], 'SemanticStatus': row['Status'],
            'CandidateEvidence': '; '.join(links),
            'CandidateLimit': 'Указатель по ID; при отсутствии Arity нужна отдельная сверка формы. Не подтверждает границы, типы и ожидаемый результат.',
            'TestMentions': '; '.join(row['Tests']), 'SampleMentions': '; '.join(row['Samples']),
            'ManualMentions': '; '.join(row['Manual']),
        })

summary = {
    'GeneratedAt': inventory['GeneratedAt'], 'Descriptors': inventory['Descriptors'],
    'Names': inventory['Names'], 'EnumIds': inventory['Ids'],
    'RegisteredIds': len({row['Id'] for row in inventory['Rows']}),
    'Reserved': inventory['Reserved'], 'MetadataMatches': True,
    'RowsWithEvidenceCandidates': linked, 'RowsWithoutEvidenceCandidates': len(inventory['Rows']) - linked,
    'MissingLight': [row['Id'] for row in inventory['Rows'] if not row['Light']],
    'MissingDark': [row['Id'] for row in inventory['Rows'] if not row['Dark']],
    'MissingInterpreterMentions': inventory['Integrity']['MissingInterpreterDispatch'],
    'MissingVmDispatch': inventory['Integrity']['MissingVmDispatch'],
    'DuplicateSignatures': inventory['Integrity']['DuplicateSignatures'],
}
(audit / f'LANGUAGE_REGISTRY_RECONCILIATION_{args.snapshot_date}.json').write_text(json.dumps(summary, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
print(json.dumps(summary, ensure_ascii=True))
