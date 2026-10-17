using Axiom.Semantics;

namespace Axiom.Runtime;

/// <summary>
/// Строит независимый снимок графа значений: записей, массивов и их полей.
/// Карта посещённых объектов сохраняет взаимные ссылки внутри снимка, а
/// правила обычного присваивания не применяются, поэтому снимок не разделяет
/// хранилище с исполняемой программой.
/// </summary>
internal static class RuntimeSnapshotFactory
{
    public static RuntimeValue CreateValueSnapshot(
        RuntimeValue value,
        int? fixedStringLength,
        Dictionary<object, object> visited)
    {
        ArgumentNullException.ThrowIfNull(visited);

        if (value.Type is UserDefinedTypeSymbol recordType)
        {
            var record = value.AsRecord();
            if (visited.TryGetValue(record, out var existing))
            {
                return RuntimeValue.FromRecord(recordType, (RuntimeRecordValue)existing);
            }

            // Запись регистрируется до обхода полей, поэтому взаимные ссылки
            // и повторный вход в ту же запись дают один и тот же объект снимка.
            var target = RuntimeRecordValue.CreateForSnapshot(recordType);
            visited[record] = target;

            foreach (var field in recordType.Fields)
            {
                target.SetSnapshotField(
                    field,
                    CreateValueSnapshot(record.GetField(field), field.FixedStringLength, visited));
            }

            return RuntimeValue.FromRecord(recordType, target);
        }

        if (value.Type is ArrayTypeSymbol arrayType)
        {
            var array = value.AsArray();
            if (visited.TryGetValue(array, out var existing))
            {
                return RuntimeValue.FromArray(arrayType, (RuntimeArrayValue)existing);
            }

            var target = array.CreateEmptyForSnapshot();
            visited[array] = target;
            array.FillSnapshotInto(target, element => CreateValueSnapshot(element, array.FixedStringLength, visited));
            return RuntimeValue.FromArray(arrayType, target);
        }

        return value.CopyForStorage(fixedStringLength);
    }
}
