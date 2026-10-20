# Снимок схемы 2

`record-schema2.json` сформирован исходными `BytecodeModuleSerializer.cs` и `BytecodeModuleArtifact.cs` из commit `d4042653592f3e61897cce146735157c76c5d290`. Старые исходники были собраны в изолированном пространстве имён и сериализовали программу:

```basic
TYPE Box
Item AS INTEGER
END TYPE
DIM value AS Box=Box(17)
PRINT value.Item
```

Файл сохраняет настоящий прежний формат: схема 2, короткое имя Box, отсутствие Identity и новых полей. Тест `OriginalSchemaTwoFixtureStillValidatesAndExecutes` проверяет восстановление, валидатор и вывод 17. Не заменять этот файл результатом текущего сериализатора с вручную изменённым номером схемы.
