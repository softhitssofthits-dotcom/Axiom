using Axiom.Core;
using Axiom.Runtime;

namespace Axiom.Execution.Tests;

public sealed class AcceptanceProgramTests
{
    private static readonly object WorkingDirectoryLock = new();

    [Fact]
    public void SmokeV01Program_RunsWithExpectedOutput()
    {
        var sourceText = TestProgramLoader.LoadText("samples/acceptance/smoke_v0_1.axm");
        var expectedOutput = TestProgramLoader.LoadLines("samples/acceptance/smoke_v0_1.expected.txt");

        var result = Run(sourceText, inputProvider: new QueueInputProvider("Иван", "45"));

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(expectedOutput, result.Output);
    }

    [Theory]
    [InlineData("samples/acceptance/negative/undeclared_variable.axm", ExecutionStatus.SemanticFailure, "MB2001")]
    [InlineData("samples/acceptance/negative/dim_requires_type_or_initializer.axm", ExecutionStatus.SemanticFailure, "MB2017")]
    [InlineData("samples/acceptance/negative/if_requires_boolean.axm", ExecutionStatus.SemanticFailure, "MB2008")]
    [InlineData("samples/acceptance/negative/block_scope_visibility.axm", ExecutionStatus.SemanticFailure, "MB2001")]
    [InlineData("samples/acceptance/negative/division_by_zero.axm", ExecutionStatus.RuntimeFailure, "MB3001")]
    [InlineData("samples/acceptance/negative/for_step_zero.axm", ExecutionStatus.RuntimeFailure, "MB3004")]
    [InlineData("samples/acceptance/negative/leading_zero_literal.axm", ExecutionStatus.SyntaxFailure, "MB0003")]
    [InlineData("samples/expressions/numeric_literals/long_out_of_range_error.axm", ExecutionStatus.SyntaxFailure, "MB0004")]
    [InlineData("samples/expressions/numeric_literals/decimal_out_of_range_error.axm", ExecutionStatus.SemanticFailure, "MB2018")]
    [InlineData("samples/expressions/arithmetic/long_overflow_error.axm", ExecutionStatus.RuntimeFailure, "MB3008")]
    [InlineData("samples/expressions/arithmetic/double_overflow_error.axm", ExecutionStatus.RuntimeFailure, "MB3008")]
    [InlineData("samples/expressions/arithmetic/decimal_overflow_error.axm", ExecutionStatus.RuntimeFailure, "MB3008")]
    [InlineData("samples/acceptance/negative/wrong_argument_count.axm", ExecutionStatus.SemanticFailure, "MB2004")]
    [InlineData("samples/functions/sqr/negative_error.axm", ExecutionStatus.RuntimeFailure, "MB3007")]
    [InlineData("samples/functions/log/non_positive_error.axm", ExecutionStatus.RuntimeFailure, "MB3007")]
    [InlineData("samples/functions/clamp/invalid_bounds_error.axm", ExecutionStatus.RuntimeFailure, "MB3007")]
    [InlineData("samples/functions/pow/invalid_domain_error.axm", ExecutionStatus.RuntimeFailure, "MB3007")]
    [InlineData("samples/functions/asin/out_of_range_error.axm", ExecutionStatus.RuntimeFailure, "MB3007")]
    [InlineData("samples/functions/acos/out_of_range_error.axm", ExecutionStatus.RuntimeFailure, "MB3007")]
    [InlineData("samples/functions/acosh/less_than_one_error.axm", ExecutionStatus.RuntimeFailure, "MB3007")]
    [InlineData("samples/functions/atanh/out_of_range_error.axm", ExecutionStatus.RuntimeFailure, "MB3007")]
    [InlineData("samples/functions/ascw/empty_text_error.axm", ExecutionStatus.RuntimeFailure, "MB3007")]
    [InlineData("samples/functions/chrw/invalid_code_error.axm", ExecutionStatus.RuntimeFailure, "MB3007")]
    [InlineData("samples/operators/const/fixed_string_error.axm", ExecutionStatus.SemanticFailure, "MB2026")]
    [InlineData("samples/operators/function/fixed_string_parameter_error.axm", ExecutionStatus.SemanticFailure, "MB2026")]
    [InlineData("samples/operators/function/fixed_string_return_error.axm", ExecutionStatus.SemanticFailure, "MB2026")]
    [InlineData("samples/acceptance/negative/type_default_non_constant.axm", ExecutionStatus.SemanticFailure, "MB2027")]
    [InlineData("samples/acceptance/negative/byref_requires_variable.axm", ExecutionStatus.SemanticFailure, "MB2028")]
    [InlineData("samples/acceptance/negative/type_literal_mixed_modes.axm", ExecutionStatus.SemanticFailure, "MB2031")]
    [InlineData("samples/acceptance/negative/type_literal_positional_arity.axm", ExecutionStatus.SemanticFailure, "MB2032")]
    [InlineData("samples/acceptance/negative/type_method_overload_ambiguous.axm", ExecutionStatus.SemanticFailure, "MB2033")]
    [InlineData("samples/acceptance/negative/type_constructor_invariant_false.axm", ExecutionStatus.RuntimeFailure, "MB3005")]
    [InlineData("samples/acceptance/negative/type_cyclic_reference.axm", ExecutionStatus.SemanticFailure, "MB2022")]
    [InlineData("samples/acceptance/negative/type_duplicate_declaration.axm", ExecutionStatus.SemanticFailure, "MB2002")]
    [InlineData("samples/acceptance/negative/type_unknown_field.axm", ExecutionStatus.SemanticFailure, "MB2023")]
    [InlineData("samples/acceptance/negative/type_invalid_field_type.axm", ExecutionStatus.SemanticFailure, "MB2022")]
    [InlineData("samples/acceptance/negative/select_case_invalid_types.axm", ExecutionStatus.SemanticFailure, "MB2003")]
    [InlineData("samples/acceptance/negative/select_case_boolean_range.axm", ExecutionStatus.SemanticFailure, "MB2015")]
    [InlineData("samples/acceptance/negative/if_block_scope_escape.axm", ExecutionStatus.SemanticFailure, "MB2001")]
    [InlineData("samples/acceptance/negative/select_case_empty_selector.axm", ExecutionStatus.SyntaxFailure, "MB1003")]
    [InlineData("samples/acceptance/negative/select_case_else_before_case.axm", ExecutionStatus.SyntaxFailure, "MB1004")]
    [InlineData("samples/acceptance/negative/if_duplicate_else.axm", ExecutionStatus.SyntaxFailure, "MB1005")]
    [InlineData("samples/acceptance/negative/select_case_duplicate_case_else.axm", ExecutionStatus.SyntaxFailure, "MB1006")]
    [InlineData("samples/acceptance/negative/if_shadowing_outer_variable.axm", ExecutionStatus.SemanticFailure, "MB2016")]
    [InlineData("samples/acceptance/negative/for_counter_must_be_local.axm", ExecutionStatus.SemanticFailure, "MB2016")]
    [InlineData("samples/control-flow/for/predeclared_counter_invalid_type.axm", ExecutionStatus.SemanticFailure, "MB2009")]
    [InlineData("samples/operators/on-key/invalid_handler_parameter.axm", ExecutionStatus.SemanticFailure, "MB2034")]
    [InlineData("samples/acceptance/negative/unknown_callable.axm", ExecutionStatus.SemanticFailure, "MB2005")]
    [InlineData("samples/acceptance/negative/return_value_in_sub.axm", ExecutionStatus.SemanticFailure, "MB2006")]
    [InlineData("samples/acceptance/negative/missing_return_value.axm", ExecutionStatus.SemanticFailure, "MB2007")]
    [InlineData("samples/acceptance/negative/sub_name_as_value.axm", ExecutionStatus.SemanticFailure, "MB2019")]
    [InlineData("samples/acceptance/negative/calling_non_function.axm", ExecutionStatus.SemanticFailure, "MB2020")]
    [InlineData("samples/acceptance/negative/scope_field_requires_self.axm", ExecutionStatus.SemanticFailure, "MB2044")]
    [InlineData("samples/acceptance/negative/scope_field_typo_suggestion.axm", ExecutionStatus.SemanticFailure, "MB2023")]
    [InlineData("samples/acceptance/negative/call_operator_hint.axm", ExecutionStatus.SemanticFailure, "MB2001")]
    [InlineData("samples/acceptance/negative/temporal_truncation_bypass.axm", ExecutionStatus.SemanticFailure, "MB2003")]
    [InlineData("samples/acceptance/negative/temporal_truncation_in_call_and_return.axm", ExecutionStatus.SemanticFailure, "MB2003")]
    public void NegativeAcceptanceProgram_ReturnsExpectedDiagnostic(string relativePath, ExecutionStatus expectedStatus, string expectedCode)
    {
        var sourceText = TestProgramLoader.LoadText(relativePath);

        var result = Run(sourceText);

        Assert.Equal(expectedStatus, result.Status);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(expectedCode, diagnostic.Code);
        Assert.True(diagnostic.Location.Line > 0);
        Assert.True(diagnostic.Location.Column > 0);
    }

    [Fact]
    public void ForCounterMustBeLocal_ReportsShadowing()
    {
        var sourceText = TestProgramLoader.LoadText("samples/acceptance/negative/for_counter_must_be_local.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.SemanticFailure, result.Status);
        var error = Assert.Single(result.Diagnostics, static d => d.Code == "MB2016");
        Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        Assert.True(error.Location.Line > 0);
        Assert.True(error.Location.Column > 0);
    }

    [Theory]
    [InlineData("samples/acceptance/negative/division_by_zero.axm", "MB3001")]
    [InlineData("samples/acceptance/negative/for_step_zero.axm", "MB3004")]
    [InlineData("samples/expressions/arithmetic/long_overflow_error.axm", "MB3008")]
    [InlineData("samples/expressions/arithmetic/decimal_overflow_error.axm", "MB3008")]
    [InlineData("samples/functions/sqr/negative_error.axm", "MB3007")]
    [InlineData("samples/functions/log/non_positive_error.axm", "MB3007")]
    [InlineData("samples/functions/clamp/invalid_bounds_error.axm", "MB3007")]
    [InlineData("samples/functions/pow/invalid_domain_error.axm", "MB3007")]
    [InlineData("samples/functions/ascw/empty_text_error.axm", "MB3007")]
    [InlineData("samples/functions/chrw/invalid_code_error.axm", "MB3007")]
    public void NegativeRuntimeAcceptanceProgram_HasStableDiagnosticOnBytecodeVm(
        string relativePath,
        string expectedCode)
    {
        var sourceText = TestProgramLoader.LoadText(relativePath);

        var bytecodeResult = Run(sourceText);

        Assert.Equal(ExecutionStatus.RuntimeFailure, bytecodeResult.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, bytecodeResult.ActualEngine);

        var bytecodeDiagnostic = Assert.Single(bytecodeResult.Diagnostics);
        Assert.Equal(expectedCode, bytecodeDiagnostic.Code);
        Assert.True(bytecodeDiagnostic.Location.Line > 0);
        Assert.True(bytecodeDiagnostic.Location.Column > 0);
    }

    [Theory]
    [InlineData("samples/control-flow/for/predeclared_counter_invalid_type.axm", "MB2009")]
    [InlineData("samples/operators/on-key/invalid_handler_parameter.axm", "MB2034")]
    [InlineData("samples/acceptance/negative/scope_field_requires_self.axm", "MB2044")]
    [InlineData("samples/acceptance/negative/scope_field_typo_suggestion.axm", "MB2023")]
    [InlineData("samples/acceptance/negative/call_operator_hint.axm", "MB2001")]
    public void NegativeSemanticAcceptanceProgram_HasStableDiagnosticOnBytecodeVm(
        string relativePath,
        string expectedCode)
    {
        var sourceText = TestProgramLoader.LoadText(relativePath);

        var bytecodeResult = Run(sourceText);

        Assert.Equal(ExecutionStatus.SemanticFailure, bytecodeResult.Status);

        var bytecodeDiagnostic = Assert.Single(bytecodeResult.Diagnostics);
        Assert.Equal(expectedCode, bytecodeDiagnostic.Code);
    }

    [Fact]
    public void InvalidNumericInputProgram_ReturnsExpectedRuntimeDiagnostic()
    {
        var sourceText = TestProgramLoader.LoadText("samples/acceptance/negative/invalid_numeric_input.axm");

        var result = Run(sourceText, inputProvider: new QueueInputProvider("oops"));

        Assert.Equal(ExecutionStatus.RuntimeFailure, result.Status);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3003", diagnostic.Code);
        Assert.Equal(2, diagnostic.Location.Line);
        Assert.Equal(7, diagnostic.Location.Column);
    }

    [Theory]
    [InlineData("samples/control-flow/do/do_while_loop.axm")]
    [InlineData("samples/control-flow/do/do_until_loop.axm")]
    [InlineData("samples/control-flow/do/loop_while.axm")]
    [InlineData("samples/control-flow/do/loop_until.axm")]
    [InlineData("samples/control-flow/do/do_loop_exit.axm")]
    public void DoLoopAcceptanceSamples_RunSuccessfully(string relativePath)
    {
        var sourceText = TestProgramLoader.LoadText(relativePath);

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.NotEmpty(result.Output);
    }

    [Theory]
    [InlineData("samples/control-flow/select_case/exact_values.axm")]
    [InlineData("samples/control-flow/select_case/ranges_and_is.axm")]
    [InlineData("samples/control-flow/select_case/string_values.axm")]
    [InlineData("samples/control-flow/select_case/mixed_selectors.axm")]
    [InlineData("samples/control-flow/select_case/string_ranges.axm")]
    [InlineData("samples/control-flow/select_case/boolean_values.axm")]
    [InlineData("samples/control-flow/select_case/first_match.axm")]
    [InlineData("samples/control-flow/select_case/no_match_no_else.axm")]
    public void SelectCaseAcceptanceSamples_RunSuccessfully(string relativePath)
    {
        var sourceText = TestProgramLoader.LoadText(relativePath);

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.NotEmpty(result.Output);
    }

    [Theory]
    [InlineData("samples/control-flow/if/basic_if_else.axm", new[] { "не положительное" })]
    [InlineData("samples/control-flow/if/elseif_chain.axm", new[] { "среднее значение" })]
    [InlineData("samples/control-flow/for/basic_loop.axm", new[] { "1", "2", "3" })]
    [InlineData("samples/control-flow/for/step_loop.axm", new[] { "0", "2", "4", "6" })]
    [InlineData("samples/control-flow/for/nested_loops.axm", new[] { "1:1", "1:2", "2:1", "2:2" })]
    [InlineData("samples/control-flow/for/exit_for.axm", new[] { "1", "2", "Готово" })]
    [InlineData("samples/control-flow/for/predeclared_counter.axm", new[] { "1", "2", "3", "После цикла: 3" })]
    [InlineData("samples/control-flow/while/basic_loop.axm", new[] { "0", "1", "2" })]
    [InlineData("samples/control-flow/do/do_while_loop.axm", new[] { "0", "1", "2", "done" })]
    [InlineData("samples/control-flow/do/do_until_loop.axm", new[] { "0", "1", "2" })]
    [InlineData("samples/control-flow/do/loop_while.axm", new[] { "0", "1", "2" })]
    [InlineData("samples/control-flow/do/loop_until.axm", new[] { "0", "1", "2" })]
    [InlineData("samples/control-flow/do/do_loop_exit.axm", new[] { "0", "1", "done" })]
    [InlineData("samples/control-flow/select_case/exact_values.axm", new[] { "two" })]
    [InlineData("samples/control-flow/select_case/ranges_and_is.axm", new[] { "Хорошо" })]
    [InlineData("samples/control-flow/select_case/string_values.axm", new[] { "Сохранить файл" })]
    [InlineData("samples/control-flow/select_case/mixed_selectors.axm", new[] { "Значение подошло" })]
    [InlineData("samples/control-flow/select_case/string_ranges.axm", new[] { "После буквы M", "Первая часть алфавита" })]
    [InlineData("samples/control-flow/select_case/boolean_values.axm", new[] { "enabled" })]
    [InlineData("samples/control-flow/select_case/first_match.axm", new[] { "first" })]
    [InlineData("samples/control-flow/select_case/no_match_no_else.axm", new[] { "after" })]
    [InlineData("samples/control-flow/return/function_value.axm", new[] { "42" })]
    [InlineData("samples/control-flow/return/sub_early_exit.axm", new[] { "До досрочного RETURN", "После блока IF" })]
    public void ControlFlowAcceptanceSamples_RunOnBytecodeVm(string relativePath, string[] expectedOutput)
    {
        var sourceText = TestProgramLoader.LoadText(relativePath);

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(expectedOutput, result.Output);
    }

    [Theory]
    [InlineData("samples/expressions/arithmetic/basic_math.axm", new[] { "7", "3" })]
    [InlineData("samples/expressions/arrays/index_access.axm", new[] { "30" })]
    [InlineData("samples/expressions/comparisons/basic_compare.axm", new[] { "TRUE", "FALSE", "TRUE" })]
    [InlineData("samples/expressions/comparisons/string_order.axm", new[] { "TRUE", "TRUE", "TRUE" })]
    [InlineData("samples/expressions/conversions/automatic_numeric.axm", new[] { "1.5", "8", "0" })]
    [InlineData("samples/expressions/hex-literals/basic_usage.axm", new[] { "255", "2147483648" })]
    [InlineData("samples/expressions/logical/basic_logic.axm", new[] { "FALSE", "TRUE", "TRUE" })]
    [InlineData("samples/expressions/string_concat/basic_concat.axm", new[] { "Hello, world" })]
    public void ExpressionAcceptanceSamples_RunOnBytecodeVm(string relativePath, string[] expectedOutput)
    {
        var sourceText = TestProgramLoader.LoadText(relativePath);

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(expectedOutput, result.Output);
    }

    [Theory]
    [InlineData("samples/operators/and/bitwise_usage.axm", new[] { "2", "FALSE" })]
    [InlineData("samples/operators/ampersand/mixed_scalar_concat.axm", new[] { "Привет, Иван тебе 7 лет.", "flag=TRUE score=12.5" })]
    [InlineData("samples/operators/eqv/basic_usage.axm", new[] { "TRUE", "FALSE", "-6" })]
    [InlineData("samples/operators/imp/basic_usage.axm", new[] { "FALSE", "TRUE", "-5" })]
    [InlineData("samples/operators/integer-division/basic_usage.axm", new[] { "3", "-3", "-3", "10737418239" })]
    [InlineData("samples/operators/mod/basic_usage.axm", new[] { "1", "-1", "1", "1" })]
    [InlineData("samples/operators/not/bitwise_usage.axm", new[] { "-1", "FALSE" })]
    [InlineData("samples/operators/or/bitwise_usage.axm", new[] { "7", "TRUE" })]
    [InlineData("samples/operators/power/basic_usage.axm", new[] { "8", "512", "-4", "4" })]
    [InlineData("samples/operators/shift-left/basic_usage.axm", new[] { "8", "6" })]
    [InlineData("samples/operators/shift-right/basic_usage.axm", new[] { "4", "268435456" })]
    [InlineData("samples/operators/xor/basic_usage.axm", new[] { "5", "TRUE" })]
    public void ScalarOperatorAcceptanceSamples_RunOnBytecodeVm(string relativePath, string[] expectedOutput)
    {
        var sourceText = TestProgramLoader.LoadText(relativePath);

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(expectedOutput, result.Output);
    }

    [Theory]
    [InlineData("samples/operators/dim/basic_declaration.axm", new[] { "42" })]
    [InlineData("samples/operators/dim/multiple_declarations.axm", new[] { "3", "4", "TRUE", "TRUE", "2014-05-01" })]
    [InlineData("samples/operators/dim/inferred_type_from_initializer.axm", new[] { "15", "ALICE", "YES", "2", "14" })]
    [InlineData("samples/operators/dim/long_inference.axm", new[] { "21474836479", "42949672958" })]
    [InlineData("samples/operators/dim/decimal_explicit_declaration.axm", new[] { "10.30", "5.15", "10.30" })]
    [InlineData("samples/operators/dim/fixed_string.axm", new[] { "[    ]", "[AB  ]", "[Alph]" })]
    [InlineData("samples/operators/const/basic_constant.axm", new[] { "Axiom" })]
    [InlineData("samples/operators/assignment/basic_assignment.axm", new[] { "15" })]
    [InlineData("samples/operators/assignment/array_element.axm", new[] { "7" })]
    [InlineData("samples/operators/print/basic_expression.axm", new[] { "7" })]
    [InlineData("samples/operators/print/empty_print.axm", new[] { "До", "", "После" })]
    [InlineData("samples/operators/print/separator_semicolon.axm", new[] { "Привет, мир", "123" })]
    [InlineData("samples/operators/print/separator_comma.axm", new[] { "A B C", "1 2 3 4 5 " })]
    [InlineData("samples/operators/print/mixed_list.axm", new[] { "12 3" })]
    [InlineData("samples/operators/print/using_basic.axm", new[] { "Имя: Боб   |", "Итого:  12.50", "Флаг: Д" })]
    public void DeclarationAssignmentAndPrintAcceptanceSamples_RunOnBytecodeVm(string relativePath, string[] expectedOutput)
    {
        var sourceText = TestProgramLoader.LoadText(relativePath);

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(expectedOutput, result.Output);
    }

    [Fact]
    public void InferredDimAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/dim/inferred_type_from_initializer.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(["15", "ALICE", "YES", "2", "14"], result.Output);
    }

    [Fact]
    public void LongInferenceAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/dim/long_inference.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(["21474836479", "42949672958"], result.Output);
    }

    [Fact]
    public void DecimalExplicitDeclarationAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/dim/decimal_explicit_declaration.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(["10.30", "5.15", "10.30"], result.Output);
    }

    [Fact]
    public void PrintUsingAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/print/using_basic.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(["Имя: Боб   |", "Итого:  12.50", "Флаг: Д"], result.Output);
    }

    [Fact]
    public void DateAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/functions/date/basic_usage.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        var output = Assert.Single(result.Output);
        Assert.Matches(@"^\d{4}-\d{2}-\d{2}$", output);
    }

    [Fact]
    public void DateNamedZoneAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/functions/date/named_timezone_current_date.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(["TRUE"], result.Output);
    }

    [Fact]
    public void TimeAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/functions/time/basic_usage.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        var output = Assert.Single(result.Output);
        Assert.Matches(@"^\d{2}:\d{2}:\d{2}$", output);
    }

    [Fact]
    public void TimeNamedZoneAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/functions/time/named_timezone_current_time.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(["TRUE"], result.Output);
    }

    [Fact]
    public void NowAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/functions/now/basic_usage.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        var output = Assert.Single(result.Output);
        Assert.Matches(@"^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}$", output);
    }

    [Fact]
    public void NowNamedZoneAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/functions/now/named_timezone_current_datetime.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(["TRUE"], result.Output);
    }

    [Fact]
    public void TimerAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/functions/timer/basic_usage.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(["TRUE", "TRUE", "TRUE"], result.Output);
    }

    [Fact]
    public void UtcDateAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/functions/utcdate/current_utc_date.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        var output = Assert.Single(result.Output);
        Assert.Matches(@"^\d{4}-\d{2}-\d{2}$", output);
    }

    [Fact]
    public void UtcTimeAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/functions/utctime/current_utc_time.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        var output = Assert.Single(result.Output);
        Assert.Matches(@"^\d{2}:\d{2}:\d{2}$", output);
    }

    [Fact]
    public void UtcNowAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/functions/utcnow/current_utc_now.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        var output = Assert.Single(result.Output);
        Assert.Matches(@"^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}$", output);
    }

    [Fact]
    public void UtcOffsetAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/functions/utcoffset/current_local_offset.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        var output = Assert.Single(result.Output);
        Assert.Matches(@"^-?\d+$", output);
    }

    [Fact]
    public void UtcOffsetValueAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/functions/utcoffset/specific_local_datetime_offset.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(["TRUE"], result.Output);
    }

    [Fact]
    public void UtcOffsetNamedZoneAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/functions/utcoffset/named_timezone_specific_offset.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(["TRUE"], result.Output);
    }

    [Fact]
    public void ToUtcAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/functions/toutc/roundtrip_to_local.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(["TRUE", "2026-04-20 14:05:06"], result.Output);
    }

    [Fact]
    public void ToUtcNamedZoneAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/functions/toutc/named_timezone_to_utc.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(["TRUE"], result.Output);
    }

    [Fact]
    public void FromUtcAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/functions/fromutc/roundtrip_to_utc.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(["TRUE", "2026-04-20 11:05:06"], result.Output);
    }

    [Fact]
    public void FromUtcNamedZoneAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/functions/fromutc/named_timezone_from_utc.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(["TRUE"], result.Output);
    }

    [Theory]
    [InlineData("samples/functions/dateserial/basic_usage.axm", "2026-04-20")]
    [InlineData("samples/functions/timeserial/basic_usage.axm", "14:05:06")]
    [InlineData("samples/functions/year/basic_usage.axm", "2026")]
    [InlineData("samples/functions/month/basic_usage.axm", "4")]
    [InlineData("samples/functions/day/basic_usage.axm", "20")]
    [InlineData("samples/functions/hour/basic_usage.axm", "14")]
    [InlineData("samples/functions/minute/basic_usage.axm", "5")]
    [InlineData("samples/functions/second/basic_usage.axm", "6")]
    [InlineData("samples/functions/weekday/basic_usage.axm", "1")]
    [InlineData("samples/functions/dateadd/month_end_shift.axm", "2026-02-28")]
    [InlineData("samples/functions/datediff/day_boundary.axm", "1")]
    [InlineData("samples/functions/datepart/day_of_year.axm", "111")]
    [InlineData("samples/functions/datevalue/from_datetime.axm", "2026-04-20")]
    [InlineData("samples/functions/timevalue/from_datetime.axm", "14:05:06")]
    [InlineData("samples/functions/isdate/canonical_datetime.axm", "TRUE")]
    [InlineData("samples/functions/monthname/calendar_month_name.axm", "April")]
    [InlineData("samples/functions/weekdayname/weekend_name.axm", "Sunday")]
    [InlineData("samples/functions/isleapyear/leap_year_true_false.axm", "TRUE")]
    [InlineData("samples/functions/daysinmonth/february_leap_year.axm", "29")]
    [InlineData("samples/functions/startofmonth/from_datetime.axm", "2026-04-01")]
    [InlineData("samples/functions/endofmonth/leap_february.axm", "2024-02-29")]
    [InlineData("samples/functions/startofyear/from_datetime.axm", "2026-01-01")]
    [InlineData("samples/functions/endofyear/from_datetime.axm", "2026-12-31")]
    public void DateTimeStage2AcceptanceSample_RunsSuccessfully(string relativePath, string expectedOutput)
    {
        var sourceText = TestProgramLoader.LoadText(relativePath);

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal([expectedOutput], result.Output);
    }

    [Fact]
    public void DateAddTypedTemporalValuesSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/functions/dateadd/typed_temporal_values.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(
            [
                "2026-02-28",
                "14:06:59",
                "2026-04-21 01:05:06",
                "2",
                "1",
                "1",
                "1",
                "14",
                "110"
            ],
            result.Output);
    }

    [Fact]
    public void BlockScopeSmokeSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/smoke/block_scope_semantics.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(["branch", "1", "2", "done"], result.Output);
    }

    [Fact]
    public void FullBlockScopeManualSmokeSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/smoke/block_scope_full_manual.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(
            [
                "scope-smoke-start",
                "if-branch",
                "elseif-branch",
                "else-branch",
                "case-two",
                "case-else",
                "for-1",
                "for-2",
                "nested-if-2",
                "while-0",
                "while-1",
                "do-0",
                "do-1",
                "sub-start",
                "sub-for-1",
                "sub-for-2",
                "sub-if",
                "sub-end",
                "fn=7",
                "scope-smoke-done"
            ],
            result.Output);
    }

    [Fact]
    public void FullLanguageBytecodeSmokeSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/smoke/full_language_bytecode.axm");

        var result = Run(sourceText, inputProvider: new QueueInputProvider("Ника, 7"));

        Assert.True(
            result.Succeeded,
            string.Join(" | ", result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}@{diagnostic.Location.Line}:{diagnostic.Location.Column}")));
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(
            [
                "full-smoke-start",
                "input:Ника:7",
                "const-for:Axiom:6",
                "if:ok",
                "while:0",
                "while:1",
                "do:2",
                "case:six",
                "3",
                "1",
                "8",
                "8",
                "4",
                "5",
                "TRUE",
                "FALSE",
                "Name: Bob   |",
                "Total:  12.50",
                "Flag: X",
                "20",
                "alpha",
                "gamma",
                "beta",
                "alpha|beta|gamma",
                "7",
                "hello",
                "HELLO",
                "007",
                "[ A ]",
                "ab-cd",
                "aef",
                "65",
                "B",
                "[    ]",
                "aaaaa",
                "2",
                "TRUE",
                "TRUE",
                "TRUE",
                "3",
                "alpha/beta/gamma",
                "5",
                "3",
                "16",
                "3",
                "3",
                "10",
                "10",
                "5",
                "TRUE",
                "2",
                "4",
                "60",
                "1",
                "30",
                "95",
                "6",
                "40",
                "10",
                "30",
                "15",
                "20,30",
                "Ada",
                "100",
                "7",
                "10",
                "sub:ok",
                "TRUE",
                "2",
                "42",
                "A-12 B-34",
                "2026-04-20",
                "14:05:06",
                "2026",
                "April",
                "2026-02-28",
                "2",
                "TRUE",
                "TRUE",
                "TRUE",
                "TRUE",
                "TRUE",
                "2026-04-20 11:05:06",
                "2026-04-20 11:05:06",
                "0",
                "TRUE",
                "TRUE",
                "TRUE",
                "2",
                "alpha",
                "TRUE",
                "TRUE",
                "TRUE",
                "TRUE",
                "INPUT",
                "alpha",
                "be",
                "TRUE",
                "FALSE",
                "TRUE",
                "full-smoke-done"
            ],
            result.Output);
    }

    [Fact]
    public void LocalSubAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/sub/local_nested_call.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(["2"], result.Output);
    }

    [Fact]
    public void LocalFunctionAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/function/local_nested_call.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(["12"], result.Output);
    }

    [Fact]
    public void LocalSubCaptureAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/sub/capture_outer_local.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(["4"], result.Output);
    }

    [Fact]
    public void LocalFunctionCaptureAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/function/capture_outer_local.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(["23"], result.Output);
    }

    [Fact]
    public void FunctionFirstClassValueAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/function/first_class_value.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(["12"], result.Output);
    }

    [Fact]
    public void FunctionReturnedClosureAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/function/returned_closure.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(["15", "7"], result.Output);
    }

    [Fact]
    public void FunctionSharedClosureStateAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/function/shared_closure_state.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(["1", "2", "3"], result.Output);
    }

    [Fact]
    public void TypeCyclicStructuralComparisonSample_ReportsCycleAndComparisonWithoutCrash()
    {
        var sourceText = TestProgramLoader.LoadText("samples/acceptance/negative/type_cyclic_structural_comparison.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.SemanticFailure, result.Status);
        Assert.Contains(result.Diagnostics, static d => d.Code == "MB2022");
        Assert.All(
            result.Diagnostics.Where(static d => d.Code == "MB2003"),
            static d => Assert.Contains("Node", d.Message, StringComparison.Ordinal));
        Assert.Equal(3, result.Diagnostics.Count);
    }

    [Fact]
    public void TypeFieldAccessAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/type/field_access.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(["", "0", "Ada", "36", "London"], result.Output);
    }

    [Fact]
    public void TypeArrayElementsAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/type/array_elements.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(["Ada", "36", "London", "Alan", "Grace", "1", "2"], result.Output);
    }

    [Fact]
    public void TypeArrayFieldsAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/type/array_fields.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(["10", "20", "FALSE", "7", "9", "9", "0"], result.Output);
    }

    [Fact]
    public void TypeMultidimensionalArrayFieldsAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/type/multidimensional_array_fields.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(["10", "20", "4", "FALSE", "9", "0"], result.Output);
    }

    [Fact]
    public void TypeArrayFieldViewsAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/type/array_field_views.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(["23", "24", "2", "4", "4", "99"], result.Output);
    }

    [Fact]
    public void TypeNamedLiteralsAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/type/named_literals.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(["Ada", "36", "Paris", "0", "0", "TRUE"], result.Output);
    }

    [Fact]
    public void TypePositionalLiteralAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/type/positional_constructor.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(["Ada", "36", "Paris", "Grace", "40", "TRUE"], result.Output);
    }

    [Fact]
    public void TypeMethodsAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/type/methods_self.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(["5", "5", "8", "8"], result.Output);
    }

    [Fact]
    public void TypeMethodReceiversFieldAndArrayAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/type/method_receivers_field_and_array.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(["5", "7", "8", "9"], result.Output);
    }

    [Fact]
    public void TypeMethodReceiverFieldChangesAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/type/method_receiver_field_changes.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.Equal(["10", "10", "99"], result.Output);
    }

    [Fact]
    public void TypeMethodReceiverArrayChangesAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/type/method_receiver_array_changes.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.Equal(["1", "10", "99"], result.Output);
    }

    [Fact]
    public void TypeDefaultNumericConversionAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/type/default_numeric_conversion.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.Equal(["1", "1", "1", "-1"], result.Output);
    }

    [Fact]
    public void TypeLargeFixedFieldAllocationAcceptanceSample_ReportsRuntimeDiagnosticInsideTry()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/type/large_fixed_field_allocation.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.Equal(["42", "MB3013", "42"], result.Output);
    }

    [Fact]
    public void TypeConstructorNewAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/type/constructor_new.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(["Zero", "18", "Ada", "19", "20", "RUS", "Grace", "18", "19"], result.Output);
    }

    [Fact]
    public void TypeDefaultFieldValuesAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/type/default_field_values.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(["Ada", "36", "TRUE"], result.Output);
    }

    [Fact]
    public void TypePartialNamedLiteralDefaultsAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/type/partial_named_literal_defaults.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(["Grace", "36", "TRUE"], result.Output);
    }

    [Fact]
    public void TypeStructuralEqualityAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/type/structural_equality.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(["TRUE", "FALSE", "FALSE", "TRUE"], result.Output);
    }

    [Fact]
    public void TypeFixedArrayFieldsAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/type/fixed_array_fields.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(["0", "10", "30", "10", "20", "3"], result.Output);
    }

    [Fact]
    public void TypeFixedMultidimensionalArrayFieldsAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/type/fixed_multidimensional_array_fields.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(["0", "10", "30", "4", "10", "20"], result.Output);
    }

    [Theory]
    [InlineData("samples/operators/type/fixed_array_fields.axm", new[] { "0", "10", "30", "10", "20", "3" })]
    [InlineData("samples/operators/type/const_fixed_array_fields.axm", new[] { "0", "3", "30" })]
    [InlineData("samples/operators/type/const_fixed_array_fields_forward.axm", new[] { "0", "3", "30" })]
    [InlineData("samples/operators/type/fixed_multidimensional_array_fields.axm", new[] { "0", "10", "30", "4", "10", "20" })]
    public void TypeFixedArrayFieldAcceptanceSamples_RunOnBytecodeVm(string relativePath, string[] expectedOutput)
    {
        var sourceText = TestProgramLoader.LoadText(relativePath);

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(expectedOutput, result.Output);
    }

    [Fact]
    public void TypeMultidimensionalArrayFieldsAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/type/multidimensional_array_fields.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(["10", "20", "4", "FALSE", "9", "0"], result.Output);
    }

    [Fact]
    public void TypeArrayFieldViewsAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/type/array_field_views.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(["23", "24", "2", "4", "4", "99"], result.Output);
    }

    [Fact]
    public void TypeNamedLiteralsAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/type/named_literals.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(["Ada", "36", "Paris", "0", "0", "TRUE"], result.Output);
    }

    [Fact]
    public void TypePositionalLiteralAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/type/positional_constructor.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(["Ada", "36", "Paris", "Grace", "40", "TRUE"], result.Output);
    }

    [Theory]
    [InlineData("samples/operators/type/methods_self.axm", new[] { "5", "5", "8", "8" })]
    [InlineData("samples/operators/type/method_receivers_field_and_array.axm", new[] { "5", "7", "8", "9" })]
    [InlineData("samples/operators/type/method_receivers_temporary.axm", new[] { "1", "2", "3", "4", "6" })]
    [InlineData("samples/operators/type/method_overloads.axm", new[] { "2", "Alpha", "7", "Beta" })]
    [InlineData("samples/operators/type/constructor_new.axm", new[] { "Zero", "18", "Ada", "19", "20", "RUS", "Grace", "18", "19" })]
    [InlineData("samples/operators/type/constructor_invariant.axm", new[] { "Ada", "20" })]
    [InlineData("samples/operators/type/positional_constructor.axm", new[] { "Ada", "36", "Paris", "Grace", "40", "TRUE" })]
    [InlineData("samples/operators/type/default_field_values.axm", new[] { "Ada", "36", "TRUE" })]
    [InlineData("samples/operators/type/partial_named_literal_defaults.axm", new[] { "Grace", "36", "TRUE" })]
    [InlineData("samples/operators/type/structural_equality.axm", new[] { "TRUE", "FALSE", "FALSE", "TRUE" })]
    [InlineData("samples/operators/sub/byref_scalar_argument.axm", new[] { "11" })]
    [InlineData("samples/operators/sub/byref_record_argument.axm", new[] { "37" })]
    [InlineData("samples/operators/sub/byval_record_copy.axm", new[] { "Ada", "36" })]
    public void TypeDefaultAndEqualityAcceptanceSamples_RunOnBytecodeVm(string relativePath, string[] expectedOutput)
    {
        var sourceText = TestProgramLoader.LoadText(relativePath);

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(expectedOutput, result.Output);
    }

    [Fact]
    public void TypeConstFixedArrayFieldsAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/type/const_fixed_array_fields.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(["0", "3", "30"], result.Output);
    }

    [Fact]
    public void TypeConstFixedArrayFieldsForwardAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/type/const_fixed_array_fields_forward.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(["0", "3", "30"], result.Output);
    }

    [Fact]
    public void TypeFixedStringFieldsAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/type/fixed_string_fields.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(["[     ]", "[Ada  ]", "[Analyst ]", "[Architec]"], result.Output);
    }

    [Fact]
    public void DimFixedStringAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/dim/fixed_string.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(["[    ]", "[AB  ]", "[Alph]"], result.Output);
    }

    [Fact]
    public void FixedStringConstAcceptanceSample_ReturnsExpectedDiagnostic()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/const/fixed_string_error.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.SemanticFailure, result.Status);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB2026", diagnostic.Code);
    }

    [Fact]
    public void FixedStringFunctionParameterAcceptanceSample_ReturnsExpectedDiagnostic()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/function/fixed_string_parameter_error.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.SemanticFailure, result.Status);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB2026", diagnostic.Code);
    }

    [Fact]
    public void FixedStringFunctionReturnAcceptanceSample_ReturnsExpectedDiagnostic()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/function/fixed_string_return_error.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.SemanticFailure, result.Status);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB2026", diagnostic.Code);
    }

    [Fact]
    public void TypeGroupedFieldsAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/type/grouped_fields.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(["", "", "0", "Ada", "Analyst", "99", "20", "7"], result.Output);
    }

    [Fact]
    public void TryCatchAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/try/handled_runtime_error.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(["before", "caught", "MB3001", "4", "Division by zero.", "cleanup", "after"], result.Output);
    }

    [Fact]
    public void ErrorAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/error/user_code_catch.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(["APP001", "Manual failure", "after"], result.Output);
    }

    [Fact]
    public void ErrorRethrowAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/error/rethrow_caught_error.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(["inner", "APP003", "Original failure", "4"], result.Output);
    }

    [Fact]
    public void InKeyAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/functions/inkey/poll_keys.axm");

        var result = Run(
            sourceText,
            inputProvider: new QueueKeyboardInputProvider("LEFT"));

        Assert.True(
            result.Status == ExecutionStatus.Success,
            string.Join(" | ", result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}@{diagnostic.Location.Line}:{diagnostic.Location.Column}")));
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(
            [
                "Щёлкните по панели вывода и нажмите LEFT.",
                "LEFT"
            ],
            result.Output);
    }

    [Fact]
    public void SleepAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/sleep/delay_and_key_wait.axm");

        var result = Run(
            sourceText,
            inputProvider: new QueueKeyboardInputProvider("SPACE"));

        Assert.True(
            result.Status == ExecutionStatus.Success,
            string.Join(" | ", result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}@{diagnostic.Location.Line}:{diagnostic.Location.Column}")));
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(
            [
                "Пауза 500 мс...",
                "Нажмите любую клавишу.",
                "Готово"
            ],
            result.Output);
    }

    [Fact]
    public void OnKeyAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/on-key/left_handler.axm");

        var result = Run(
            sourceText,
            inputProvider: new QueueKeyboardInputProvider("LEFT", "ESC"));

        Assert.True(
            result.Status == ExecutionStatus.Success,
            string.Join(" | ", result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}@{diagnostic.Location.Line}:{diagnostic.Location.Column}")));
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(
            [
                "Щёлкните по панели вывода, нажмите LEFT, затем нажмите ESC.",
                "обработчик LEFT",
                "готово"
            ],
            result.Output);
    }

    [Fact]
    public void OnKeyAnyAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/on-key/any_key_handler.axm");

        var result = Run(
            sourceText,
            inputProvider: new QueueKeyboardInputProvider("RIGHT", "ESC"));

        Assert.True(
            result.Status == ExecutionStatus.Success,
            string.Join(" | ", result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}@{diagnostic.Location.Line}:{diagnostic.Location.Column}")));
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(
            [
                "Нажмите любую клавишу, затем нажмите ESC.",
                "любая клавиша",
                "готово"
            ],
            result.Output);
    }

    [Fact]
    public void OnKeyHandlerKeyNameAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/on-key/handler_key_name.axm");

        var result = Run(
            sourceText,
            inputProvider: new QueueKeyboardInputProvider("LEFT", "ESC"));

        Assert.True(
            result.Status == ExecutionStatus.Success,
            string.Join(" | ", result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}@{diagnostic.Location.Line}:{diagnostic.Location.Column}")));
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(
            [
                "Нажмите любую клавишу, затем нажмите ESC.",
                "LEFT",
                "готово"
            ],
            result.Output);
    }

    [Fact]
    public void OnKeyChordAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/on-key/chord_handler.axm");

        var result = Run(
            sourceText,
            inputProvider: new QueueKeyboardInputProvider(keys: ["LEFT", "ESC"], pressedKeys: ["CTRL", "LEFT"]));

        Assert.True(
            result.Status == ExecutionStatus.Success,
            string.Join(" | ", result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}@{diagnostic.Location.Line}:{diagnostic.Location.Column}")));
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(
            [
                "Удерживайте CTRL, нажмите LEFT, затем нажмите ESC.",
                "CTRL+LEFT",
                "готово"
            ],
            result.Output);
    }

    [Fact]
    public void OnKeyStopResumeAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/on-key/stop_resume_handler.axm");

        var result = Run(
            sourceText,
            inputProvider: new QueueKeyboardInputProvider("", "", "LEFT", "LEFT", "ESC"));

        Assert.True(
            result.Status == ExecutionStatus.Success,
            string.Join(" | ", result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}@{diagnostic.Location.Line}:{diagnostic.Location.Column}")));
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(
            [
                "Нажмите LEFT при отключённом обработчике, затем LEFT после ON, затем ESC.",
                "LEFT",
                "обработчик LEFT",
                "готово"
            ],
            result.Output);
    }

    [Fact]
    public void KeyDownAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/functions/keydown/held_modifiers.axm");

        var result = Run(
            sourceText,
            inputProvider: new QueueKeyboardInputProvider(keys: [], pressedKeys: ["CTRL", "LEFT"]));

        Assert.True(
            result.Status == ExecutionStatus.Success,
            string.Join(" | ", result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}@{diagnostic.Location.Line}:{diagnostic.Location.Column}")));
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(
            [
                "Удерживайте CTRL+LEFT.",
                "CTRL+LEFT",
                "готово"
            ],
            result.Output);
    }

    [Fact]
    public void KeyDownCanonicalAliasesAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/functions/keydown/canonical_aliases.axm");

        var result = Run(
            sourceText,
            inputProvider: new QueueKeyboardInputProvider(keys: [], pressedKeys: ["CTRL", "PAGEUP"]));

        Assert.True(
            result.Status == ExecutionStatus.Success,
            string.Join(" | ", result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}@{diagnostic.Location.Line}:{diagnostic.Location.Column}")));
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(
            [
                "Удерживайте CTRL+PAGEUP.",
                "CONTROL+PAGE UP",
                "готово"
            ],
            result.Output);
    }

    [Fact]
    public void KeyPressAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/functions/keypress/single_press.axm");

        var result = Run(
            sourceText,
            inputProvider: new QueueKeyboardInputProvider("LEFT"));

        Assert.True(
            result.Status == ExecutionStatus.Success,
            string.Join(" | ", result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}@{diagnostic.Location.Line}:{diagnostic.Location.Column}")));
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(
            [
                "Нажмите LEFT один раз.",
                "LEFT нажата",
                "FALSE",
                "готово"
            ],
            result.Output);
    }

    [Fact]
    public void ModifiedKeyPressAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/functions/keypress/modified_keypress.axm");

        var result = Run(
            sourceText,
            inputProvider: new QueueKeyboardInputProvider(keys: ["LEFT"], pressedKeys: ["CTRL", "LEFT"]));

        Assert.True(
            result.Status == ExecutionStatus.Success,
            string.Join(" | ", result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}@{diagnostic.Location.Line}:{diagnostic.Location.Column}")));
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(
            [
                "Удерживайте CTRL и нажмите LEFT.",
                "CTRL+LEFT",
                "готово"
            ],
            result.Output);
    }

    [Fact]
    public void MouseCoordinatesAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/functions/mousex/current_position.axm");
        var inputProvider = new QueueKeyboardInputProvider(keys: [], pressedKeys: [], mouseX: 320, mouseY: 240);

        var result = Run(
            sourceText,
            inputProvider: inputProvider);

        Assert.True(
            result.Status == ExecutionStatus.Success,
            string.Join(" | ", result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}@{diagnostic.Location.Line}:{diagnostic.Location.Column}")));
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Empty(result.Output);
        Assert.Equal(
            [
                "320",
                "240",
                "Готово"
            ],
            inputProvider.GraphicsOutput);
    }

    [Fact]
    public void MouseCoordinatesCanvasAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/functions/mousex/canvas_world_position.axm");
        var inputProvider = new QueueKeyboardInputProvider(keys: ["ESC"], pressedKeys: [], mouseX: 40, mouseY: 90);

        var result = Run(
            sourceText,
            inputProvider: inputProvider);

        Assert.True(
            result.Status == ExecutionStatus.Success,
            string.Join(" | ", result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}@{diagnostic.Location.Line}:{diagnostic.Location.Column}")));
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(
            [
                "110",
                "60"
            ],
            result.Output);
    }

    [Fact]
    public void MouseYCoordinatesCanvasAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/functions/mousey/canvas_world_position.axm");
        var inputProvider = new QueueKeyboardInputProvider(keys: ["ESC"], pressedKeys: [], mouseX: 40, mouseY: 90);

        var result = Run(
            sourceText,
            inputProvider: inputProvider);

        Assert.True(
            result.Status == ExecutionStatus.Success,
            string.Join(" | ", result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}@{diagnostic.Location.Line}:{diagnostic.Location.Column}")));
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(
            [
                "60",
                "110"
            ],
            result.Output);
    }

    [Fact]
    public void MouseButtonAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/functions/mousebutton/button_states.axm");
        var inputProvider = new QueueKeyboardInputProvider(keys: [], pressedKeys: [], mouseX: 320, mouseY: 240, pressedMouseButtons: [1, 3]);

        var result = Run(
            sourceText,
            inputProvider: inputProvider);

        Assert.True(
            result.Status == ExecutionStatus.Success,
            string.Join(" | ", result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}@{diagnostic.Location.Line}:{diagnostic.Location.Column}")));
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Empty(result.Output);
        Assert.Equal(
            [
                "TRUE",
                "FALSE",
                "TRUE",
                "FALSE",
                "Готово"
            ],
            inputProvider.GraphicsOutput);
    }

    [Fact]
    public void MousePressAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/functions/mousepress/button_presses.axm");
        var inputProvider = new QueueKeyboardInputProvider(keys: [], pressedKeys: [], mousePresses: [1, 3]);

        var result = Run(
            sourceText,
            inputProvider: inputProvider);

        Assert.True(
            result.Status == ExecutionStatus.Success,
            string.Join(" | ", result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}@{diagnostic.Location.Line}:{diagnostic.Location.Column}")));
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Empty(result.Output);
        Assert.Equal(
            [
                "TRUE",
                "FALSE",
                "TRUE",
                "Готово"
            ],
            inputProvider.GraphicsOutput);
    }

    [Fact]
    public void OnMouseAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/on-mouse/left_button_handler.axm");
        var inputProvider = new QueueKeyboardInputProvider(keys: ["ESC"], pressedKeys: [], mouseX: 320, mouseY: 240, mousePresses: [1]);

        var result = Run(
            sourceText,
            inputProvider: inputProvider);

        Assert.True(
            result.Status == ExecutionStatus.Success,
            string.Join(" | ", result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}@{diagnostic.Location.Line}:{diagnostic.Location.Column}")));
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Empty(result.Output);
        Assert.Equal(
            [
                "TRUE",
                "1",
                "320",
                "240",
                "Готово"
            ],
            inputProvider.GraphicsOutput);
    }

    [Fact]
    public void OnMouseCanvasAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/on-mouse/canvas_world_handler.axm");
        var inputProvider = new QueueKeyboardInputProvider(
            keys: ["ESC"],
            pressedKeys: [],
            mouseX: 40,
            mouseY: 90,
            mouseEvents: [new MouseInputEvent(MouseInputEventKind.Move, 0)]);

        var result = Run(
            sourceText,
            inputProvider: inputProvider);

        Assert.True(
            result.Status == ExecutionStatus.Success,
            string.Join(" | ", result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}@{diagnostic.Location.Line}:{diagnostic.Location.Column}")));
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(
            [
                "TRUE",
                "0",
                "110",
                "60",
                "TRUE",
                "TRUE"
            ],
            result.Output);
    }

    [Fact]
    public void OnMouseReleaseMoveAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/on-mouse/release_move_handler.axm");
        var inputProvider = new QueueKeyboardInputProvider(
            keys: ["ESC"],
            pressedKeys: [],
            mouseX: 320,
            mouseY: 240,
            mouseEvents:
            [
                new MouseInputEvent(MouseInputEventKind.Move, 0),
                new MouseInputEvent(MouseInputEventKind.Release, 1)
            ]);

        var result = Run(
            sourceText,
            inputProvider: inputProvider);

        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Empty(result.Output);
        Assert.Equal(
            [
                "TRUE",
                "320",
                "240",
                "TRUE",
                "1",
                "320",
                "240",
                "Готово"
            ],
            inputProvider.GraphicsOutput);
    }

    [Fact]
    public void OnMouseWheelAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/on-mouse/wheel_handler.axm");
        var inputProvider = new QueueKeyboardInputProvider(
            keys: ["ESC"],
            pressedKeys: [],
            mouseX: 320,
            mouseY: 240,
            mouseEvents: [new MouseInputEvent(MouseInputEventKind.Wheel, -120)]);

        var result = Run(
            sourceText,
            inputProvider: inputProvider);

        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Empty(result.Output);
        Assert.Equal(
            [
                "TRUE",
                "-120",
                "320",
                "240",
                "Готово"
            ],
            inputProvider.GraphicsOutput);
    }

    [Fact]
    public void ScreenModeInfoAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/graphics/screen/mode_info.axm");
        var inputProvider = new QueueKeyboardInputProvider(keys: ["SPACE", "SPACE", "ESC"], pressedKeys: []);

        var result = Run(sourceText, inputProvider);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Empty(result.Output);
        Assert.Equal(["Текстовый экран"], inputProvider.GraphicsOutput);
    }

    [Fact]
    public void Screen10BestFitAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/graphics/screen/best_fit_dimensions.axm");
        var inputProvider = new QueueKeyboardInputProvider(keys: [], pressedKeys: []);

        var result = Run(sourceText, inputProvider);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Empty(result.Output);
        Assert.Equal(
            [
                "mode10 active=TRUE",
                "mode10 mode=10",
                "mode10 width=1918",
                "mode10 height=1079",
                "mode10 cols=239",
                "mode10 rows=89",
                "Проверка SCREEN 10 завершена"
            ],
            inputProvider.GraphicsOutput);
    }

    [Fact]
    public void ScreenZeroTextScrollAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/graphics/screen/text_scroll.axm");
        var inputProvider = new QueueKeyboardInputProvider(keys: [], pressedKeys: []);

        var result = Run(sourceText, inputProvider);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Empty(result.Output);
        Assert.Equal(
            [
                "SCREEN 0 text scroll",
                "line 1",
                "line 2",
                "line 3",
                "line 4",
                "line 5",
                "line 6",
                "line 7",
                "line 8",
                "line 9",
                "line 10",
                "line 11",
                "line 12",
                "line 13",
                "line 14",
                "line 15",
                "line 16",
                "line 17",
                "line 18",
                "line 19",
                "line 20",
                "line 21",
                "line 22",
                "line 23",
                "line 24",
                "line 25",
                "line 26",
                "line 27",
                "visible after scroll"
            ],
            inputProvider.GraphicsOutput);
    }

    [Fact]
    public void ScreenTextAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/graphics/text/print_locate_color.axm");
        var inputProvider = new QueueKeyboardInputProvider(keys: ["SPACE", "SPACE", "ESC"], pressedKeys: []);

        var result = Run(sourceText, inputProvider);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Empty(result.Output);
        Assert.Empty(inputProvider.GraphicsOutput);
    }

    [Fact]
    public void ScreenTransparentTextAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/graphics/text/transparent_text.axm");
        var inputProvider = new QueueKeyboardInputProvider(keys: ["SPACE", "SPACE", "ESC"], pressedKeys: []);

        var result = Run(sourceText, inputProvider);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Empty(result.Output);
        Assert.Equal(["Проверка прозрачного текста завершена"], inputProvider.GraphicsOutput);
    }

    [Fact]
    public void ScreenInputAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/graphics/input/input_on_screen.axm");
        var inputProvider = new QueueKeyboardInputProvider(keys: ["ESC"], pressedKeys: [], inputs: ["Ada"]);

        var result = Run(sourceText, inputProvider);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Empty(result.Output);
        Assert.Equal(["Ввод завершён"], inputProvider.GraphicsOutput);
    }

    [Fact]
    public void ScreenZeroTextInputAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/graphics/input/text_screen_input.axm");
        var inputProvider = new QueueKeyboardInputProvider(keys: [], pressedKeys: [], inputs: ["Ada", "7"]);

        var result = Run(sourceText, inputProvider);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Empty(result.Output);
        Assert.Equal(["Ваше имя? Ada", "Ваш возраст? 7", "Привет, Ada тебе 7 лет."], inputProvider.GraphicsOutput);
    }

    [Fact]
    public void ScreenPrimitivesAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/graphics/primitives/pset_line_circle.axm");
        var inputProvider = new QueueKeyboardInputProvider(keys: ["SPACE", "ESC"], pressedKeys: []);

        var result = Run(sourceText, inputProvider);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Empty(result.Output);
        Assert.Equal(["Проверка графических примитивов завершена"], inputProvider.GraphicsOutput);
    }

    [Fact]
    public void ScreenCircleFullFormsAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/graphics/primitives/circle_arcs_step_aspect.axm");
        var inputProvider = new QueueKeyboardInputProvider(keys: ["ESC"], pressedKeys: []);

        var result = Run(sourceText, inputProvider);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Empty(result.Output);
        Assert.Equal(["Проверка окружностей завершена"], inputProvider.GraphicsOutput);
    }

    [Fact]
    public void ScreenCircleFullTurnAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/graphics/primitives/circle_full_turn.axm");
        var inputProvider = new QueueKeyboardInputProvider(keys: ["ESC"], pressedKeys: []);

        var result = Run(sourceText, inputProvider);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Empty(result.Output);
        Assert.Equal(["Проверка полного оборота завершена"], inputProvider.GraphicsOutput);
    }

    [Fact]
    public void ScreenStepAndPointPositionAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/graphics/primitives/step_and_point_position.axm");
        var inputProvider = new QueueKeyboardInputProvider(keys: ["ESC"], pressedKeys: []);

        var result = Run(sourceText, inputProvider);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Empty(result.Output);
        Assert.Equal(["Проверка STEP и POINT завершена"], inputProvider.GraphicsOutput);
    }

    [Fact]
    public void ScreenViewAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/view/screen_and_relative.axm");
        var inputProvider = new QueueKeyboardInputProvider(keys: ["ESC"], pressedKeys: []);

        var result = Run(sourceText, inputProvider);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Empty(result.Output);
        Assert.Equal(
            [
                "abs border=9 abs fill=3",
                "rel pixel=7",
                "rel pos=0,0",
                "reset pos=11,11",
                "VIEW sample finished"
            ],
            inputProvider.GraphicsOutput);
    }

    [Fact]
    public void ViewPrintAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/view-print/scrolling_region.axm");
        var inputProvider = new QueueKeyboardInputProvider(keys: ["ESC"], pressedKeys: []);

        var result = Run(sourceText, inputProvider);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Empty(result.Output);
        Assert.Contains("VIEW PRINT sample finished", inputProvider.GraphicsOutput);
    }

    [Fact]
    public void ScreenWindowAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/window/cartesian_and_screen.axm");
        var inputProvider = new QueueKeyboardInputProvider(keys: ["ESC"], pressedKeys: []);

        var result = Run(sourceText, inputProvider);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Empty(result.Output);
        Assert.Equal(
            [
                "draw pixel=15",
                "screen pos=15,15",
                "screen logical=105,205",
                "reset pos=15,15 logical=15,15",
                "view pos=3,2 logical=3,2",
                "cart pixel=7",
                "cart pos=2,3",
                "cart logical=104,205",
                "WINDOW sample finished"
            ],
            inputProvider.GraphicsOutput);
    }

    [Fact]
    public void PMapAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/functions/pmap/window_and_viewport_conversion.axm");
        var inputProvider = new QueueKeyboardInputProvider(keys: ["ESC"], pressedKeys: []);

        var result = Run(sourceText, inputProvider);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Empty(result.Output);
        Assert.Equal(
            [
                "abs=14,15",
                "back=104,205",
                "view only=3,2 back=3,2",
                "frac=1.5,102.5",
                "cart=3,3",
                "cart back=105,205",
                "PMAP sample finished"
            ],
            inputProvider.GraphicsOutput);
    }

    [Fact]
    public void PaletteAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/palette/change_and_reset_colors.axm");
        var inputProvider = new QueueKeyboardInputProvider(keys: ["SPACE", "SPACE", "ESC"], pressedKeys: []);

        var result = Run(sourceText, inputProvider);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Empty(result.Output);
        Assert.Equal(
            [
                "default index=1",
                "changed index=1",
                "reset index=1",
                "PALETTE sample finished"
            ],
            inputProvider.GraphicsOutput);
    }

    [Fact]
    public void PaletteUsingAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/palette/using_array_colors.axm");
        var inputProvider = new QueueKeyboardInputProvider(keys: ["ESC"], pressedKeys: []);

        var result = Run(sourceText, inputProvider);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Empty(result.Output);
        Assert.Equal(
            [
                "before using=1",
                "after using=1",
                "after reset=1",
                "PALETTE USING sample finished"
            ],
            inputProvider.GraphicsOutput);
    }

    [Fact]
    public void PCopyAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/pcopy/page_copy_buffer.axm");
        var inputProvider = new QueueKeyboardInputProvider(keys: ["ESC"], pressedKeys: []);

        var result = Run(sourceText, inputProvider);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Empty(result.Output);
        Assert.Equal(
            [
                "visible page=1",
                "PCOPY sample finished"
            ],
            inputProvider.GraphicsOutput);
    }

    [Fact]
    public void GetAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/get/rectangle_capture.axm");
        var inputProvider = new QueueKeyboardInputProvider(keys: [], pressedKeys: []);

        var result = Run(sourceText, inputProvider);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Empty(result.Output);
        Assert.Equal(["6", "9", "12", "15", "Проверка GET завершена"], inputProvider.GraphicsOutput);
    }

    [Fact]
    public void PutAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/put/default_xor_and_preset.axm");
        var inputProvider = new QueueKeyboardInputProvider(keys: [], pressedKeys: []);

        var result = Run(sourceText, inputProvider);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Empty(result.Output);
        Assert.Equal(["1", "2", "254", "253", "Проверка PUT завершена"], inputProvider.GraphicsOutput);
    }

    [Fact]
    public void ScreenPaintAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/paint/pattern_repaint.axm");
        var inputProvider = new QueueKeyboardInputProvider(keys: ["ESC"], pressedKeys: []);

        var result = Run(sourceText, inputProvider);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Empty(result.Output);
        Assert.Equal(["PAINT pattern sample finished"], inputProvider.GraphicsOutput);
    }

    [Fact]
    public void ScreenPaintStepAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/paint/step_default_fill.axm");
        var inputProvider = new QueueKeyboardInputProvider(keys: ["ESC"], pressedKeys: []);

        var result = Run(sourceText, inputProvider);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Empty(result.Output);
        Assert.Equal(["PAINT STEP sample finished"], inputProvider.GraphicsOutput);
    }

    [Fact]
    public void DrawImageAcceptanceSample_RunsOnBytecodeVm()
    {
        var relativePath = "samples/graphics/images/load_and_draw_image.axm";
        var sourceText = TestProgramLoader.LoadText(relativePath);
        var inputProvider = CreateGraphicsImageSampleInputProvider(relativePath);

        var result = Run(sourceText, inputProvider);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Empty(result.Output);
        Assert.Equal(["image size=16x16", "DRAWIMAGE sample finished"], inputProvider.GraphicsOutput);

        Assert.Equal(2, inputProvider.DrawImages.Count);
        Assert.Equal((16L, 24L, 16, 16), ToDrawImageShape(inputProvider.DrawImages[0]));
        Assert.Equal((96L, 24L, 64, 64), ToDrawImageShape(inputProvider.DrawImages[1]));
    }

    [Fact]
    public void DrawSpriteAcceptanceSample_RunsOnBytecodeVm()
    {
        var relativePath = "samples/graphics/sprites/walk_cycle.axm";
        var sourceText = TestProgramLoader.LoadText(relativePath);
        var inputProvider = CreateGraphicsSpriteSampleInputProvider(relativePath, ["SPACE", "SPACE", "ESC"]);

        var result = Run(sourceText, inputProvider);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Empty(result.Output);
        Assert.Equal(
            [
                "Количество кадров в листе: 4",
                "Первый кадр показан. Нажмите любую клавишу.",
                "Второй кадр показан. Нажмите любую клавишу.",
                "Проверка DRAWSPRITE завершена. Нажмите ESC."
            ],
            inputProvider.GraphicsOutput);

        Assert.Equal(2, inputProvider.DrawSprites.Count);
        Assert.Equal((1, 16L, 24L, 16, 16), ToDrawSpriteShape(inputProvider.DrawSprites[0]));
        Assert.Equal((3, 64L, 24L, 32, 32), ToDrawSpriteShape(inputProvider.DrawSprites[1]));
    }

    [Fact]
    public void DrawSpritePageFlipAcceptanceSample_RunsOnBytecodeVm()
    {
        var relativePath = "samples/graphics/sprites/page_flip_walk.axm";
        var sourceText = TestProgramLoader.LoadText(relativePath);
        var inputProvider = CreateGraphicsSpriteSampleInputProvider(relativePath, ["SPACE", "SPACE", "ESC"]);

        var result = Run(sourceText, inputProvider);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Empty(result.Output);
        Assert.Equal(
            [
                "Второй кадр готов. Нажмите любую клавишу.",
                "Количество кадров в листе: 4",
                "Проверка спрайта через PCOPY завершена. Нажмите ESC."
            ],
            inputProvider.GraphicsOutput);

        Assert.Equal(2, inputProvider.DrawSprites.Count);
        Assert.Equal((0, 20L, 24L, 16, 16), ToDrawSpriteShape(inputProvider.DrawSprites[0]));
        Assert.Equal((1, 44L, 24L, 16, 16), ToDrawSpriteShape(inputProvider.DrawSprites[1]));
    }

    [Fact]
    public void DrawTileAcceptanceSample_RunsOnBytecodeVm()
    {
        var relativePath = "samples/operators/drawtile/basic_tileset.axm";
        var sourceText = TestProgramLoader.LoadText(relativePath);
        var inputProvider = CreateGraphicsModernSampleInputProvider(relativePath, ["SPACE", "ESC"]);

        var result = Run(sourceText, inputProvider);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Empty(result.Output);
        Assert.Equal([(160L, 90L)], inputProvider.CanvasActivations);
        Assert.Equal(2, inputProvider.PresentFrameCount);
        Assert.Equal(2, inputProvider.DrawImageRegions.Count);
        Assert.Equal((0, 0, 16, 16, 24L, 24L, 16, 16), ToDrawImageRegionShape(inputProvider.DrawImageRegions[0]));
        Assert.Equal((16, 16, 16, 16, 88L, 40L, 16, 16), ToDrawImageRegionShape(inputProvider.DrawImageRegions[1]));
    }

    [Fact]
    public void DrawTilemapAcceptanceSample_RunsOnBytecodeVm()
    {
        var relativePath = "samples/operators/drawtilemap/visible_and_layer.axm";
        var sourceText = TestProgramLoader.LoadText(relativePath);
        var inputProvider = CreateGraphicsModernSampleInputProvider(relativePath, ["SPACE", "ESC"]);

        var result = Run(sourceText, inputProvider);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Empty(result.Output);
        Assert.Equal([(160L, 90L)], inputProvider.CanvasActivations);
        Assert.Equal(2, inputProvider.PresentFrameCount);
        Assert.Equal(6, inputProvider.DrawImageRegions.Count);
        Assert.Equal((0, 0, 16, 16, 24L, 32L, 16, 16), ToDrawImageRegionShape(inputProvider.DrawImageRegions[0]));
        Assert.Equal((16, 0, 16, 16, 40L, 32L, 16, 16), ToDrawImageRegionShape(inputProvider.DrawImageRegions[1]));
        Assert.Equal((16, 16, 16, 16, 40L, 48L, 16, 16), ToDrawImageRegionShape(inputProvider.DrawImageRegions[2]));
        Assert.Equal((16, 0, 16, 16, 88L, 32L, 16, 16), ToDrawImageRegionShape(inputProvider.DrawImageRegions[3]));
    }

    [Fact]
    public void SetMapTileAcceptanceSample_RunsOnBytecodeVm()
    {
        var relativePath = "samples/operators/setmaptile/cell_update.axm";
        var sourceText = TestProgramLoader.LoadText(relativePath);
        var inputProvider = CreateGraphicsModernSampleInputProvider(relativePath, ["SPACE", "SPACE", "ESC"]);

        var result = Run(sourceText, inputProvider);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Empty(result.Output);
        Assert.Equal([(160L, 90L)], inputProvider.CanvasActivations);
        Assert.Equal(3, inputProvider.PresentFrameCount);
        Assert.Equal(10, inputProvider.DrawImageRegions.Count);
        Assert.Equal((0, 0, 16, 16, 24L, 32L, 16, 16), ToDrawImageRegionShape(inputProvider.DrawImageRegions[0]));
        Assert.Equal((16, 0, 16, 16, 24L, 48L, 16, 16), ToDrawImageRegionShape(inputProvider.DrawImageRegions[5]));
        Assert.Equal((0, 0, 16, 16, 24L, 32L, 16, 16), ToDrawImageRegionShape(inputProvider.DrawImageRegions[7]));
    }

    [Fact]
    public void CanvasFrameAcceptanceSample_RunsOnBytecodeVm()
    {
        var relativePath = "samples/graphics/modern/rectangles_frame.axm";
        var sourceText = TestProgramLoader.LoadText(relativePath);
        var inputProvider = CreateGraphicsModernSampleInputProvider(relativePath, ["SPACE", "SPACE"]);

        var result = Run(sourceText, inputProvider);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal([(320L, 180L)], inputProvider.CanvasActivations);
        Assert.Equal(2, inputProvider.BeginFrameCount);
        Assert.Equal([0x12161E, 0x080A10], inputProvider.FrameClears);
        Assert.Equal(2, inputProvider.FillRects.Count);
        Assert.Equal(2, inputProvider.DrawRects.Count);
        Assert.Single(inputProvider.DrawImages);
        Assert.Equal(2, inputProvider.DrawSprites.Count);
        Assert.Equal(2, inputProvider.PresentFrameCount);
    }

    [Fact]
    public void ModernHudTextAcceptanceSample_RunsOnBytecodeVm()
    {
        var relativePath = "samples/graphics/modern/hud_text.axm";
        var sourceText = TestProgramLoader.LoadText(relativePath);
        var inputProvider = CreateGraphicsModernSampleInputProvider(relativePath, ["SPACE", "SPACE"]);

        var result = Run(sourceText, inputProvider);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal([(320L, 180L)], inputProvider.CanvasActivations);
        Assert.True(inputProvider.CanvasDrawTexts.Count >= 2);
        Assert.True(inputProvider.ResetCanvasCameraCount >= 1);
        Assert.True(inputProvider.FillRects.Count >= 1);
        Assert.True(inputProvider.PresentFrameCount >= 2);
    }

    [Fact]
    public void TextWidthAcceptanceSample_RunsOnBytecodeVm()
    {
        var relativePath = "samples/functions/textwidth/hud_metrics.axm";
        var sourceText = TestProgramLoader.LoadText(relativePath);
        var inputProvider = CreateGraphicsModernSampleInputProvider(relativePath, ["SPACE"]);

        var result = Run(sourceText, inputProvider);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(["72"], result.Output);
        Assert.Equal([(320L, 180L)], inputProvider.CanvasActivations);
        Assert.Single(inputProvider.CanvasDrawTexts);
        Assert.True(inputProvider.FillRects.Count >= 1);
        Assert.True(inputProvider.DrawRects.Count >= 1);
    }

    [Fact]
    public void TextHeightAcceptanceSample_RunsOnBytecodeVm()
    {
        var relativePath = "samples/functions/textheight/multiline_label.axm";
        var sourceText = TestProgramLoader.LoadText(relativePath);
        var inputProvider = CreateGraphicsModernSampleInputProvider(relativePath, ["SPACE"]);

        var result = Run(sourceText, inputProvider);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(["24"], result.Output);
        Assert.Equal([(320L, 180L)], inputProvider.CanvasActivations);
        Assert.Single(inputProvider.CanvasDrawTexts);
        Assert.True(inputProvider.FillRects.Count >= 1);
        Assert.True(inputProvider.DrawRects.Count >= 1);
    }

    [Fact]
    public void ModernCameraAcceptanceSample_RunsOnBytecodeVm()
    {
        var relativePath = "samples/graphics/modern/camera_world.axm";
        var sourceText = TestProgramLoader.LoadText(relativePath);
        var inputProvider = CreateGraphicsModernSampleInputProvider(relativePath, ["SPACE", "SPACE"]);

        var result = Run(sourceText, inputProvider);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal([(320L, 180L)], inputProvider.CanvasActivations);
        Assert.True(inputProvider.CanvasCameraCalls.Count >= 1);
        Assert.True(inputProvider.ResetCanvasCameraCount >= 1);
        Assert.True(inputProvider.DrawRects.Count >= 1 || inputProvider.FillRects.Count >= 1);
        Assert.True(inputProvider.CanvasDrawTexts.Count >= 1);
        Assert.True(inputProvider.PresentFrameCount >= 2);
    }

    [Fact]
    public void ModernLinesAndCirclesAcceptanceSample_RunsOnBytecodeVm()
    {
        var relativePath = "samples/graphics/modern/lines_and_circles.axm";
        var sourceText = TestProgramLoader.LoadText(relativePath);
        var inputProvider = CreateGraphicsModernSampleInputProvider(relativePath, ["SPACE", "SPACE"]);

        var result = Run(sourceText, inputProvider);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal([(320L, 180L)], inputProvider.CanvasActivations);
        Assert.True(inputProvider.CanvasDrawLines.Count >= 2);
        Assert.True(inputProvider.CanvasDrawCircles.Count >= 1);
        Assert.True(inputProvider.CanvasFillCircles.Count >= 1);
        Assert.True(inputProvider.PresentFrameCount >= 2);
    }

    [Fact]
    public void PointCanvasAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/functions/point/canvas_pixel_read.axm");
        var inputProvider = new QueueKeyboardInputProvider(keys: [], pressedKeys: []);

        var result = Run(sourceText, inputProvider);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(["0", "15"], result.Output);
        Assert.Equal([(16L, 16L)], inputProvider.CanvasActivations);
        Assert.Equal([0], inputProvider.FrameClears);
        Assert.Equal(1, inputProvider.PresentFrameCount);
    }

    [Fact]
    public void PSetCanvasAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/pset/canvas_world_pixel.axm");
        var inputProvider = new QueueKeyboardInputProvider(keys: [], pressedKeys: []);

        var result = Run(sourceText, inputProvider);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(["12", "0"], result.Output);
        Assert.Equal([(24L, 18L)], inputProvider.CanvasActivations);
        Assert.Equal([0], inputProvider.FrameClears);
        Assert.Equal(1, inputProvider.PresentFrameCount);
    }

    [Fact]
    public void PResetCanvasAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/preset/canvas_background_pixel.axm");
        var inputProvider = new QueueKeyboardInputProvider(keys: [], pressedKeys: []);

        var result = Run(sourceText, inputProvider);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(["0", "15"], result.Output);
        Assert.Equal([(24L, 18L)], inputProvider.CanvasActivations);
        Assert.Equal([0xFFFFFF], inputProvider.FrameClears);
        Assert.Equal(1, inputProvider.PresentFrameCount);
    }

    [Fact]
    public void PaintCanvasAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/paint/canvas_region_fill.axm");
        var inputProvider = new QueueKeyboardInputProvider(keys: [], pressedKeys: []);

        var result = Run(sourceText, inputProvider);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(["6", "9"], result.Output);
        Assert.Equal([(24L, 18L)], inputProvider.CanvasActivations);
        Assert.Equal([0], inputProvider.FrameClears);
        Assert.Equal(1, inputProvider.PresentFrameCount);
    }

    [Fact]
    public void GetCanvasAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/get/canvas_block_capture.axm");
        var inputProvider = new QueueKeyboardInputProvider(keys: [], pressedKeys: []);

        var result = Run(sourceText, inputProvider);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(["6", "9", "12", "15"], result.Output);
        Assert.Equal([(24L, 18L)], inputProvider.CanvasActivations);
        Assert.Equal([0], inputProvider.FrameClears);
        Assert.Equal(1, inputProvider.PresentFrameCount);
    }

    [Fact]
    public void PutCanvasAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/put/canvas_block_restore.axm");
        var inputProvider = new QueueKeyboardInputProvider(keys: [], pressedKeys: []);

        var result = Run(sourceText, inputProvider);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(["6", "9", "12", "15"], result.Output);
        Assert.Equal([(24L, 18L)], inputProvider.CanvasActivations);
        Assert.Equal([0], inputProvider.FrameClears);
        Assert.Equal(1, inputProvider.PresentFrameCount);
    }

    [Theory]
    [InlineData("samples/operators/circle/text_screen_error.axm", "MB2046")]
    [InlineData("samples/functions/point/invalid_selector_error.axm", "MB3007")]
    [InlineData("samples/operators/color/text_screen_error.axm", "MB2046")]
    [InlineData("samples/operators/paint/text_screen_error.axm", "MB2046")]
    [InlineData("samples/operators/view/invalid_bounds_error.axm", "MB3007")]
    [InlineData("samples/operators/window/zero_size_error.axm", "MB3007")]
    [InlineData("samples/operators/paint/empty_pattern_error.axm", "MB3007")]
    [InlineData("samples/operators/view-print/invalid_rows_error.axm", "MB3007")]
    [InlineData("samples/functions/pmap/invalid_selector_error.axm", "MB3007")]
    [InlineData("samples/graphics/screen/invalid_page_error.axm", "MB3007")]
    [InlineData("samples/operators/pcopy/invalid_page_error.axm", "MB3007")]
    [InlineData("samples/operators/pcopy/text_screen_error.axm", "MB2046")]
    [InlineData("samples/operators/palette/text_screen_error.axm", "MB2046")]
    [InlineData("samples/operators/palette/using_without_screen_error.axm", "MB2046")]
    [InlineData("samples/graphics/images/missing_image_error.axm", "MB3005")]
    [InlineData("samples/graphics/sprites/invalid_frame_error.axm", "MB3007")]
    [InlineData("samples/graphics/modern/missing_canvas_error.axm", "MB2046")]
    [InlineData("samples/graphics/modern/negative_radius_error.axm", "MB3007")]
    [InlineData("samples/graphics/modern/invalid_text_position_error.axm", "MB3007")]
    public void GraphicsNegativeAcceptanceSamples_HaveStableDiagnosticParity(string relativePath, string expectedCode)
    {
        var sourceText = TestProgramLoader.LoadText(relativePath);
        var bytecodeInputProvider = CreateGraphicsNegativeInputProvider(relativePath);

        var bytecodeResult = Run(sourceText, bytecodeInputProvider);

        if (expectedCode == "MB2046")
        {
            Assert.Equal(ExecutionStatus.SemanticFailure, bytecodeResult.Status);
            Assert.Null(bytecodeResult.ActualEngine);
        }
        else
        {
            Assert.Equal(ExecutionStatus.RuntimeFailure, bytecodeResult.Status);
            Assert.Equal(ExecutionEngineKind.BytecodeVm, bytecodeResult.ActualEngine);
        }

        var bytecodeDiagnostic = Assert.Single(bytecodeResult.Diagnostics);
        Assert.Equal(expectedCode, bytecodeDiagnostic.Code);
        Assert.True(bytecodeDiagnostic.Location.Line > 0);
        Assert.True(bytecodeDiagnostic.Location.Column > 0);
    }

    private static (long X, long Y, int Width, int Height) ToDrawImageShape(DrawImageCall call) =>
        (call.X, call.Y, call.Width, call.Height);

    private static (int SourceX, int SourceY, int SourceWidth, int SourceHeight, long X, long Y, int Width, int Height) ToDrawImageRegionShape(DrawImageRegionCall call) =>
        (call.SourceX, call.SourceY, call.SourceWidth, call.SourceHeight, call.X, call.Y, call.Width, call.Height);

    private static (int Frame, long X, long Y, int Width, int Height) ToDrawSpriteShape(DrawSpriteCall call) =>
        (call.Frame, call.X, call.Y, call.Width, call.Height);

    [Theory]
    [InlineData("samples/operators/draw/square.axm", "DRAW square sample finished")]
    [InlineData("samples/operators/draw/octagon.axm", "DRAW octagon sample finished")]
    [InlineData("samples/operators/draw/blind_and_return.axm", "DRAW blind and return sample finished")]
    [InlineData("samples/operators/draw/rotated_spokes.axm", "DRAW rotated spokes sample finished")]
    [InlineData("samples/operators/draw/paint_segments.axm", "DRAW paint segments sample finished")]
    [InlineData("samples/operators/draw/paint_default_border.axm", "DRAW paint default border sample finished")]
    [InlineData("samples/operators/draw/dynamic_string.axm", "DRAW dynamic string sample finished")]
    public void ScreenDrawAcceptanceSamples_RunOnBytecodeVm(string relativePath, string expectedLine)
    {
        var sourceText = TestProgramLoader.LoadText(relativePath);
        var inputProvider = new QueueKeyboardInputProvider(keys: ["ESC"], pressedKeys: []);

        var result = Run(sourceText, inputProvider);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Empty(result.Output);
        Assert.Equal([expectedLine], inputProvider.GraphicsOutput);
    }

    [Fact]
    public void ScreenMouseAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/graphics/mouse/mouse_events.axm");
        var inputProvider = new QueueKeyboardInputProvider(
            keys: ["ESC"],
            pressedKeys: [],
            mouseX: 320,
            mouseY: 240,
            mouseEvents:
            [
                new MouseInputEvent(MouseInputEventKind.Move, 0),
                new MouseInputEvent(MouseInputEventKind.Press, 1),
                new MouseInputEvent(MouseInputEventKind.Wheel, -120)
            ]);

        var result = Run(sourceText, inputProvider);

        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Empty(result.Output);
        Assert.Equal(["Проверка мыши завершена"], inputProvider.GraphicsOutput);
    }

    [Fact]
    public void ScreenGameAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/graphics/game/paddle_ball.axm");
        var inputProvider = new QueueKeyboardInputProvider(keys: ["ESC"], pressedKeys: []);

        var result = Run(sourceText, inputProvider);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Empty(result.Output);
        Assert.Equal(["Игра завершена: выход по ESC"], inputProvider.GraphicsOutput);
    }

    [Fact]
    public void MazeChaseAcceptanceSample_RunsOnBytecodeVm()
    {
        var relativePath = "samples/graphics/game/maze_chase.axm";
        var sourceText = TestProgramLoader.LoadText(relativePath);
        var inputProvider = CreateGraphicsModernSampleInputProvider(relativePath, ["ESC"]);

        var result = RunFromSampleDirectory(relativePath, sourceText, inputProvider);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal([(1024L, 660L)], inputProvider.CanvasActivations);
        Assert.True(inputProvider.CanvasCameraCalls.Count >= 1);
        Assert.True(inputProvider.ResetCanvasCameraCount >= 1);
        Assert.True(inputProvider.FillRects.Count > 0);
        Assert.True(inputProvider.CanvasFillCircles.Count > 0);
        Assert.True(inputProvider.CanvasDrawTexts.Count > 0);
        Assert.True(inputProvider.DrawSprites.Count >= 2);
        Assert.True(inputProvider.PresentFrameCount >= 1);

        Assert.Single(inputProvider.DrawImages);
        Assert.Equal((0L, 0L, 448, 576), ToDrawImageShape(inputProvider.DrawImages[0]));

        var firstPowerPellet = Assert.Single(inputProvider.CanvasFillCircles.Where(static circle => circle.Radius == 5).Take(1));
        Assert.Equal(27L, firstPowerPellet.X);
        Assert.Equal(31L, firstPowerPellet.Y);

        var playerSprite = inputProvider.DrawSprites[0];
        Assert.Equal(211L, playerSprite.X);
        Assert.Equal(435L, playerSprite.Y);
    }

    [Fact]
    public void MazeChaseAcceptanceSample_SpaceShowsPauseHud()
    {
        var relativePath = "samples/graphics/game/maze_chase.axm";
        var sourceText = TestProgramLoader.LoadText(relativePath);
        var inputProvider = CreateGraphicsModernSampleInputProvider(relativePath, ["SPACE", "ESC"]);

        var result = RunFromSampleDirectory(relativePath, sourceText, inputProvider);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Contains(
            inputProvider.CanvasDrawTexts,
            text => text.Text.Contains("Пауза. Нажмите SPACE, чтобы продолжить.", StringComparison.Ordinal));
    }

    [Fact]
    public void MazeChaseAcceptanceSample_LowerKShowsCalibrationHints()
    {
        var relativePath = "samples/graphics/game/maze_chase.axm";
        var sourceText = TestProgramLoader.LoadText(relativePath);
        var inputProvider = CreateGraphicsModernSampleInputProvider(relativePath, ["k", "", "", "", "", "", "", "", "ESC"]);

        var result = RunFromSampleDirectory(relativePath, sourceText, inputProvider);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Contains(
            inputProvider.CanvasDrawTexts,
            text => text.Text.Contains("t: записать всё", StringComparison.Ordinal));
        Assert.Contains(
            inputProvider.CanvasDrawTexts,
            text => text.Text.Contains("строка 5, значение -33", StringComparison.Ordinal));
        Assert.True(inputProvider.CanvasDrawLines.Count > 70);
    }

    [Fact]
    public void MazeChaseAcceptanceSample_TWritesAllCalibrationValues()
    {
        var relativePath = "samples/graphics/game/maze_chase.axm";
        var sourceText = TestProgramLoader.LoadText(relativePath);
        var calibrationDirectory = CreateArtifactsTestDirectory("maze-chase-calibration-");
        var inputProvider = CreateGraphicsModernSampleInputProvider(
            relativePath,
            keys: ["k", "t", "", "", "", "", "ESC"],
            pressedKeys: []);

        try
        {
            var result = RunFromWorkingDirectory(calibrationDirectory, sourceText, inputProvider);

            Assert.True(result.Succeeded, FormatDiagnostics(result));
            Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
            Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
            Assert.Contains(
                inputProvider.CanvasDrawTexts,
                text => text.Text.Contains("Записана вся таблица.", StringComparison.Ordinal));
            Assert.Contains(
                inputProvider.CanvasDrawTexts,
                text => text.Text.Contains("Строки и столбцы.", StringComparison.Ordinal));

            var calibrationPath = Path.Combine(calibrationDirectory, "maze_chase_pellet_calibration.txt");
            var calibrationText = File.ReadAllText(calibrationPath);
            Assert.StartsWith("' Полный снимок текущей калибровки maze_chase", calibrationText, StringComparison.Ordinal);
            Assert.Contains("pelletCenterRows(5) = -33", calibrationText, StringComparison.Ordinal);
            Assert.DoesNotContain("pelletCenterRows(5) = 12", calibrationText, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(calibrationDirectory, recursive: true);
        }
    }

    [Fact]
    public void MazeChaseApplyCalibrationSample_RewritesInitPelletAlignmentFromCalibrationFile()
    {
        var relativePath = "samples/graphics/game/maze_chase_apply_calibration.axm";
        var sourceText = TestProgramLoader.LoadText(relativePath);
        var workingDirectory = CreateArtifactsTestDirectory("maze-chase-apply-");
        var repositoryRoot = FindRepositoryRoot();
        var sourceGamePath = Path.Combine(repositoryRoot, "samples", "graphics", "game", "maze_chase.axm");
        var targetGamePath = Path.Combine(workingDirectory, "maze_chase.axm");
        var targetCalibrationPath = Path.Combine(workingDirectory, "maze_chase_pellet_calibration.txt");

        try
        {
            File.Copy(sourceGamePath, targetGamePath, overwrite: true);

            var calibrationText = File.ReadAllText(Path.Combine(repositoryRoot, "samples", "graphics", "game", "maze_chase_pellet_calibration.txt"));
            calibrationText = calibrationText.Replace("pelletCenterRows(5) = -33", "pelletCenterRows(5) = -44", StringComparison.Ordinal);
            calibrationText = calibrationText.Replace("pelletCenterCols(26) = 0", "pelletCenterCols(26) = 9", StringComparison.Ordinal);
            File.WriteAllText(targetCalibrationPath, calibrationText);

            var result = RunFromWorkingDirectory(workingDirectory, sourceText);

            Assert.True(result.Succeeded, FormatDiagnostics(result));
            Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
            Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
            Assert.Equal(
                [
                    "Калибровка перенесена в maze_chase.axm.",
                    "Процедура InitPelletAlignment() полностью обновлена из файла калибровки."
                ],
                result.Output);

            var updatedGameText = File.ReadAllText(targetGamePath);
            Assert.Contains("pelletCenterRows(5) = -44", updatedGameText, StringComparison.Ordinal);
            Assert.Contains("pelletCenterCols(26) = 9", updatedGameText, StringComparison.Ordinal);
            Assert.DoesNotContain("pelletCenterRows(5) = -33", updatedGameText, StringComparison.Ordinal);
            Assert.DoesNotContain("pelletCenterCols(26) = 0", updatedGameText, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(workingDirectory, recursive: true);
        }
    }

    [Theory]
    [InlineData(ExecutionEngineKind.BytecodeVm)]
    public void MazeChaseAcceptanceSample_MovesPlayerInBytecodeVm(ExecutionEngineKind expectedEngine)
    {
        var relativePath = "samples/graphics/game/maze_chase.axm";
        var sourceText = TestProgramLoader.LoadText(relativePath);
        var inputProvider = CreateGraphicsModernSampleInputProvider(relativePath, keys: ["", "", "", "", "ESC"], pressedKeys: ["RIGHT"]);

        var result = RunFromSampleDirectory(relativePath, sourceText, inputProvider);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(expectedEngine, result.ActualEngine);

        var playerDraws = inputProvider.DrawSprites
            .Where((_, index) => index % 2 == 0)
            .ToArray();

        Assert.True(playerDraws.Length >= 1);
        Assert.True(playerDraws[0].X > 16L);
    }

    [Fact]
    public void ReadAllLinesUtf8BomAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/functions/readalllines/utf8_bom.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(["1", "alpha"], result.Output);
    }

    [Theory]
    [InlineData("samples/functions/regexmatches/all_matches.axm", "2", "42", "100", "0")]
    [InlineData("samples/functions/regexextractall/group_values.axm", "2", "12", "34", "a", "b")]
    [InlineData("samples/functions/regexsplit/split_on_pattern.axm", "3", "one", "two", "three")]
    [InlineData("samples/functions/replacemany/multiple_tokens.axm", "Y X z", "one two")]
    public void StringArrayFunctionAcceptanceSamples_RunSuccessfully(string relativePath, params string[] expectedOutput)
    {
        var sourceText = TestProgramLoader.LoadText(relativePath);

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(expectedOutput, result.Output);
    }

    [Theory]
    [InlineData("samples/functions/ascw/unicode_codepoint.axm", "1071", "9608", "128512")]
    [InlineData("samples/functions/chrw/unicode_character.axm", "Я", "█", "128512")]
    public void UnicodeCharacterFunctionAcceptanceSamples_RunOnBytecodeVm(string relativePath, params string[] expectedOutput)
    {
        var sourceText = TestProgramLoader.LoadText(relativePath);

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(expectedOutput, result.Output);
    }

    [Fact]
    public void RegexExtractAllNegativeGroupAcceptanceSample_ReturnsExpectedDiagnostic()
    {
        var sourceText = TestProgramLoader.LoadText("samples/functions/regexextractall/negative_group_error.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.RuntimeFailure, result.Status);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3007", diagnostic.Code);
    }

    [Fact]
    public void NestedRuntimeDiagnosticSmokeSample_ReturnsExpandedRuntimeMessage()
    {
        var sourceText = TestProgramLoader.LoadText("samples/smoke/nested_runtime_diagnostic.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.RuntimeFailure, result.Status);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3001", diagnostic.Code);
        Assert.Contains("Цепочка вызовов", diagnostic.Message, StringComparison.Ordinal);
        Assert.Contains("'Inner'", diagnostic.Message, StringComparison.Ordinal);
        Assert.Contains("'Outer'", diagnostic.Message, StringComparison.Ordinal);
        Assert.Equal(8, diagnostic.Location.Line);
        Assert.Equal(19, diagnostic.Location.Column);
    }

    [Theory]
    [InlineData("samples/functions/abs/basic_usage.axm")]
    [InlineData("samples/functions/rnd/basic_usage.axm")]
    [InlineData("samples/functions/sgn/basic_usage.axm")]
    [InlineData("samples/functions/min/basic_usage.axm")]
    [InlineData("samples/functions/max/basic_usage.axm")]
    [InlineData("samples/functions/clamp/basic_usage.axm")]
    [InlineData("samples/functions/sin/basic_usage.axm")]
    [InlineData("samples/functions/cos/basic_usage.axm")]
    [InlineData("samples/functions/tan/basic_usage.axm")]
    [InlineData("samples/functions/atn/basic_usage.axm")]
    [InlineData("samples/functions/atn2/basic_usage.axm")]
    [InlineData("samples/functions/sqr/basic_usage.axm")]
    [InlineData("samples/functions/cbrt/basic_usage.axm")]
    [InlineData("samples/functions/exp/basic_usage.axm")]
    [InlineData("samples/functions/log/basic_usage.axm")]
    [InlineData("samples/functions/log10/basic_usage.axm")]
    [InlineData("samples/functions/log2/basic_usage.axm")]
    [InlineData("samples/functions/pow/basic_usage.axm")]
    [InlineData("samples/functions/floor/basic_usage.axm")]
    [InlineData("samples/functions/ceil/basic_usage.axm")]
    [InlineData("samples/functions/round/basic_usage.axm")]
    [InlineData("samples/functions/fix/basic_usage.axm")]
    [InlineData("samples/functions/asin/basic_usage.axm")]
    [InlineData("samples/functions/acos/basic_usage.axm")]
    [InlineData("samples/functions/asinh/basic_usage.axm")]
    [InlineData("samples/functions/acosh/basic_usage.axm")]
    [InlineData("samples/functions/atanh/basic_usage.axm")]
    [InlineData("samples/functions/sinh/basic_usage.axm")]
    [InlineData("samples/functions/cosh/basic_usage.axm")]
    [InlineData("samples/functions/tanh/basic_usage.axm")]
    [InlineData("samples/functions/setsign/basic_usage.axm")]
    public void MathFunctionAcceptanceSamples_RunSuccessfully(string relativePath)
    {
        var sourceText = TestProgramLoader.LoadText(relativePath);

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.NotEmpty(result.Output);
    }

    [Theory]
    [InlineData("samples/expressions/comparisons/basic_compare.axm")]
    [InlineData("samples/expressions/comparisons/string_order.axm")]
    public void ComparisonAcceptanceSamples_RunSuccessfully(string relativePath)
    {
        var sourceText = TestProgramLoader.LoadText(relativePath);

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity != DiagnosticSeverity.Warning);
        Assert.NotEmpty(result.Output);
    }

    [Theory]
    [MemberData(nameof(PositiveAcceptanceSamplePaths))]
    public void PositiveAcceptanceSample_RunsOnBytecodeVmWithoutFallback(string relativePath)
    {
        var sourceText = TestProgramLoader.LoadText(relativePath);

        var result = RunAuditSample(sourceText, relativePath);

        Assert.True(
            result.Succeeded,
            $"{relativePath}: {string.Join(" | ", result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}@{diagnostic.Location.Line}:{diagnostic.Location.Column}"))}");
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.DoesNotContain(result.Diagnostics, static diagnostic => diagnostic.Severity != DiagnosticSeverity.Warning);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
    }

    public static IEnumerable<object[]> PositiveAcceptanceSamplePaths()
    {
        var repositoryRoot = FindRepositoryRoot();
        var samplesRoot = Path.Combine(repositoryRoot, "samples");

        return Directory
            .EnumerateFiles(samplesRoot, "*.axm", SearchOption.AllDirectories)
            .Select(path => (Relative: Path.GetRelativePath(repositoryRoot, path).Replace(Path.DirectorySeparatorChar, '/'), Full: path))
            .Where(static pair => IsPositiveAcceptanceSample(pair.Relative, pair.Full))
            .OrderBy(static pair => pair.Relative, StringComparer.OrdinalIgnoreCase)
            .Select(static pair => new object[] { pair.Relative });
    }

    private static bool IsPositiveAcceptanceSample(string relativePath, string fullPath)
    {
        if (relativePath.StartsWith("samples/acceptance/negative/", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (relativePath.Contains("_error.", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (relativePath.EndsWith("/nested_runtime_diagnostic.axm", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (relativePath.Equals("samples/smoke/repair_plans_mojibake.axm", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (IsInteractiveSample(fullPath))
        {
            return false;
        }

        return true;
    }

    private static bool IsInteractiveSample(string fullPath)
    {
        try
        {
            var sourceText = File.ReadAllText(fullPath);

            if (sourceText.Contains('\0'))
            {
                return true;
            }

            if (sourceText.Contains("SLEEP", StringComparison.Ordinal) ||
                sourceText.Contains("INKEY", StringComparison.Ordinal) ||
                sourceText.Contains("SCREEN ", StringComparison.Ordinal) ||
                sourceText.Contains("ON KEY", StringComparison.Ordinal) ||
                sourceText.Contains("ON MOUSE", StringComparison.Ordinal) ||
                sourceText.Contains("CANVAS ", StringComparison.Ordinal) ||
                sourceText.Contains("DRAWIMAGE", StringComparison.Ordinal) ||
                sourceText.Contains("DRAWSPRITE", StringComparison.Ordinal) ||
                sourceText.Contains("LOADIMAGE", StringComparison.Ordinal) ||
                sourceText.Contains("LOADSPRITE", StringComparison.Ordinal) ||
                sourceText.Contains("LOADTILESET", StringComparison.Ordinal) ||
                sourceText.Contains("LOADTILEMAP", StringComparison.Ordinal) ||
                sourceText.Contains("DRAWTILEMAP", StringComparison.Ordinal) ||
                sourceText.Contains("DRAWTILELAYER", StringComparison.Ordinal) ||
                sourceText.Contains("DRAWTILE", StringComparison.Ordinal) ||
                sourceText.Contains("DIM ", StringComparison.Ordinal) && sourceText.Contains(" AS IMAGE", StringComparison.Ordinal) ||
                sourceText.Contains("DIM ", StringComparison.Ordinal) && sourceText.Contains(" AS SPRITE", StringComparison.Ordinal) ||
                sourceText.Contains("DIM ", StringComparison.Ordinal) && sourceText.Contains(" AS TILESET", StringComparison.Ordinal) ||
                sourceText.Contains("DIM ", StringComparison.Ordinal) && sourceText.Contains(" AS TILEMAP", StringComparison.Ordinal) ||
                sourceText.Contains("Ожидаемая диагностика", StringComparison.Ordinal))
            {
                return true;
            }
        }
        catch (IOException)
        {
        }

        return false;
    }

    private static ExecutionResult RunAuditSample(string sourceText, string relativePath)
    {
        lock (WorkingDirectoryLock)
        {
            var repositoryRoot = FindRepositoryRoot();
            var directory = relativePath.Equals("samples/functions/readfile/single_byte_round_trip.axm", StringComparison.OrdinalIgnoreCase)
                || relativePath.Equals("samples/functions/readfile/windows1251_fallback.axm", StringComparison.OrdinalIgnoreCase)
                ? Path.GetDirectoryName(Path.Combine(repositoryRoot, relativePath))!
                : repositoryRoot;
            try
            {
                return RunFromWorkingDirectory(directory, sourceText, CreateAuditInputProvider(relativePath));
            }
            finally
            {
                if (relativePath.Equals("samples/smoke/repair_plans_mojibake.axm", StringComparison.OrdinalIgnoreCase))
                    File.Delete(Path.Combine(repositoryRoot, "README_copy.md"));
            }
        }
    }

    private static IInputProvider CreateAuditInputProvider(string relativePath)
    {
        if (relativePath.Equals("samples/smoke/ide-acceptance/screen_mouse.axm", StringComparison.OrdinalIgnoreCase))
        {
            // Общий проход выполняет программу с графическим хостом и завершением по ESC.
            // Физические события мыши проверяются отдельной ручной приёмкой.
            return new QueueKeyboardInputProvider(keys: ["ESC"], pressedKeys: []);
        }
        if (relativePath.Equals("samples/operators/input/cancel_wait.axm", StringComparison.OrdinalIgnoreCase))
        {
            // Общий проход проверяет запуск с корректным числовым вводом.
            // Отмена проверяется отдельным Runtime-тестом и настоящим DesktopHost.
            return new QueueInputProvider("123");
        }
        if (relativePath.Equals("samples/graphics/text/print_locate_color.axm", StringComparison.OrdinalIgnoreCase))
        {
            return new QueueKeyboardInputProvider(keys: ["SPACE", "SPACE"], pressedKeys: []);
        }

        if (relativePath.Equals("samples/graphics/text/transparent_text.axm", StringComparison.OrdinalIgnoreCase))
        {
            return new QueueKeyboardInputProvider(keys: ["ESC"], pressedKeys: []);
        }

        if (relativePath.Equals("samples/graphics/input/text_screen_input.axm", StringComparison.OrdinalIgnoreCase))
        {
            return new QueueKeyboardInputProvider(keys: [], pressedKeys: [], inputs: ["Ada", "7"]);
        }

        if (relativePath.StartsWith("samples/graphics/images/", StringComparison.OrdinalIgnoreCase))
        {
            return CreateGraphicsImageSampleInputProvider(relativePath);
        }

        if (relativePath.StartsWith("samples/graphics/sprites/", StringComparison.OrdinalIgnoreCase))
        {
            return CreateGraphicsSpriteSampleInputProvider(relativePath);
        }

        if (relativePath.Equals("samples/operators/palette/change_and_reset_colors.axm", StringComparison.OrdinalIgnoreCase))
        {
            return new QueueKeyboardInputProvider(keys: ["SPACE", "SPACE", "ESC"], pressedKeys: []);
        }

        if (relativePath.StartsWith("samples/operators/drawtile/", StringComparison.OrdinalIgnoreCase))
        {
            return CreateGraphicsModernSampleInputProvider(relativePath, ["SPACE", "SPACE"]);
        }

        if (relativePath.StartsWith("samples/operators/drawtilemap/", StringComparison.OrdinalIgnoreCase))
        {
            return CreateGraphicsModernSampleInputProvider(relativePath, ["SPACE", "SPACE"]);
        }

        if (relativePath.StartsWith("samples/graphics/", StringComparison.OrdinalIgnoreCase)
            || relativePath.StartsWith("samples/operators/view/", StringComparison.OrdinalIgnoreCase)
            || relativePath.StartsWith("samples/operators/view-print/", StringComparison.OrdinalIgnoreCase)
            || relativePath.StartsWith("samples/operators/window/", StringComparison.OrdinalIgnoreCase)
            || relativePath.StartsWith("samples/operators/palette/", StringComparison.OrdinalIgnoreCase)
            || relativePath.StartsWith("samples/operators/pcopy/", StringComparison.OrdinalIgnoreCase)
            || relativePath.StartsWith("samples/operators/get/", StringComparison.OrdinalIgnoreCase)
            || relativePath.StartsWith("samples/operators/put/", StringComparison.OrdinalIgnoreCase)
            || relativePath.StartsWith("samples/operators/paint/", StringComparison.OrdinalIgnoreCase)
            || relativePath.StartsWith("samples/operators/draw/", StringComparison.OrdinalIgnoreCase)
            || relativePath.StartsWith("samples/functions/pmap/", StringComparison.OrdinalIgnoreCase))
        {
            return new QueueKeyboardInputProvider(keys: ["ESC"], pressedKeys: []);
        }

        if (relativePath.Equals("samples/operators/sleep/delay_and_key_wait.axm", StringComparison.OrdinalIgnoreCase))
        {
            return new QueueKeyboardInputProvider(keys: ["SPACE"], pressedKeys: []);
        }

        if (relativePath.Equals("samples/functions/inkey/poll_keys.axm", StringComparison.OrdinalIgnoreCase)
            || relativePath.Equals("samples/functions/keypress/single_press.axm", StringComparison.OrdinalIgnoreCase))
        {
            return new QueueKeyboardInputProvider(keys: ["LEFT"], pressedKeys: []);
        }

        if (relativePath.Equals("samples/functions/keypress/modified_keypress.axm", StringComparison.OrdinalIgnoreCase))
        {
            return new QueueKeyboardInputProvider(keys: ["LEFT"], pressedKeys: ["CTRL", "LEFT"]);
        }

        if (relativePath.Equals("samples/functions/keydown/held_modifiers.axm", StringComparison.OrdinalIgnoreCase))
        {
            return new QueueKeyboardInputProvider(keys: [], pressedKeys: ["CTRL", "LEFT"]);
        }

        if (relativePath.Equals("samples/functions/keydown/canonical_aliases.axm", StringComparison.OrdinalIgnoreCase))
        {
            return new QueueKeyboardInputProvider(keys: [], pressedKeys: ["CTRL", "PAGEUP"]);
        }

        if (relativePath.Equals("samples/functions/mousex/canvas_world_position.axm", StringComparison.OrdinalIgnoreCase))
        {
            return new QueueKeyboardInputProvider(keys: ["ESC"], pressedKeys: [], mouseX: 40, mouseY: 90);
        }

        if (relativePath.Equals("samples/functions/mousey/canvas_world_position.axm", StringComparison.OrdinalIgnoreCase))
        {
            return new QueueKeyboardInputProvider(keys: ["ESC"], pressedKeys: [], mouseX: 40, mouseY: 90);
        }

        if (relativePath.StartsWith("samples/functions/mousex/", StringComparison.OrdinalIgnoreCase)
            || relativePath.StartsWith("samples/functions/mousey/", StringComparison.OrdinalIgnoreCase))
        {
            return new QueueKeyboardInputProvider(keys: [], pressedKeys: [], mouseX: 320, mouseY: 240);
        }

        if (relativePath.StartsWith("samples/functions/mousebutton/", StringComparison.OrdinalIgnoreCase))
        {
            return new QueueKeyboardInputProvider(keys: [], pressedKeys: [], mouseX: 320, mouseY: 240, pressedMouseButtons: [1, 3]);
        }

        if (relativePath.StartsWith("samples/functions/mousepress/", StringComparison.OrdinalIgnoreCase))
        {
            return new QueueKeyboardInputProvider(keys: [], pressedKeys: [], mouseX: 320, mouseY: 240, mousePresses: [1, 3]);
        }

        if (relativePath.Equals("samples/operators/on-key/chord_handler.axm", StringComparison.OrdinalIgnoreCase))
        {
            return new QueueKeyboardInputProvider(keys: ["LEFT", "ESC"], pressedKeys: ["CTRL", "LEFT"]);
        }

        if (relativePath.Equals("samples/operators/on-key/stop_resume_handler.axm", StringComparison.OrdinalIgnoreCase))
        {
            return new QueueKeyboardInputProvider(keys: ["", "", "LEFT", "LEFT", "ESC"], pressedKeys: []);
        }

        if (relativePath.StartsWith("samples/operators/on-key/", StringComparison.OrdinalIgnoreCase))
        {
            return new QueueKeyboardInputProvider(keys: ["LEFT", "ESC"], pressedKeys: []);
        }

        if (relativePath.Equals("samples/operators/on-mouse/canvas_world_handler.axm", StringComparison.OrdinalIgnoreCase))
        {
            return new QueueKeyboardInputProvider(
                keys: ["ESC"],
                pressedKeys: [],
                mouseX: 40,
                mouseY: 90,
                mouseEvents: [new MouseInputEvent(MouseInputEventKind.Move, 0)]);
        }

        if (relativePath.StartsWith("samples/operators/on-mouse/", StringComparison.OrdinalIgnoreCase))
        {
            return new QueueKeyboardInputProvider(
                keys: ["ESC"],
                pressedKeys: [],
                mouseX: 320,
                mouseY: 240,
                mouseEvents: [new MouseInputEvent(MouseInputEventKind.Move, 0), new MouseInputEvent(MouseInputEventKind.Press, 1), new MouseInputEvent(MouseInputEventKind.Wheel, -120)]);
        }

        if (relativePath.Equals("samples/smoke/smoke_v0_1.axm", StringComparison.OrdinalIgnoreCase)
            || relativePath.Equals("samples/acceptance/smoke_v0_1.axm", StringComparison.OrdinalIgnoreCase))
        {
            return new QueueInputProvider("Иван", "45");
        }

        if (relativePath.Equals("samples/smoke/full_language_bytecode.axm", StringComparison.OrdinalIgnoreCase)
            || relativePath.Equals("samples/operators/input/multiple_variables.axm", StringComparison.OrdinalIgnoreCase)
            || relativePath.Equals("samples/operators/input/prompt_semicolon.axm", StringComparison.OrdinalIgnoreCase)
            || relativePath.Equals("samples/operators/input/prompt_comma.axm", StringComparison.OrdinalIgnoreCase))
        {
            return new QueueInputProvider("Ника, 7");
        }

        if (relativePath.Equals("samples/operators/input/temporal_values.axm", StringComparison.OrdinalIgnoreCase))
        {
            return new QueueInputProvider("2024-02-29", "12:34:56.125", "2024-02-29 12:34:56.125");
        }

        if (relativePath.Equals("samples/smoke/engine-compare/input_record_intake.axm", StringComparison.OrdinalIgnoreCase))
        {
            return new QueueInputProvider(CreateInputRecordIntakeLines());
        }

        return new QueueInputProvider("Ника", "7", "TRUE");
    }

    private static QueueKeyboardInputProvider CreateGraphicsNegativeInputProvider(string relativePath) =>
        relativePath.StartsWith("samples/graphics/images/", StringComparison.OrdinalIgnoreCase)
            ? CreateGraphicsImageSampleInputProvider(relativePath)
            : relativePath.StartsWith("samples/graphics/sprites/", StringComparison.OrdinalIgnoreCase)
                ? CreateGraphicsSpriteSampleInputProvider(relativePath)
            : new QueueKeyboardInputProvider(keys: [], pressedKeys: []);

    private static QueueKeyboardInputProvider CreateGraphicsImageSampleInputProvider(string relativePath)
    {
        var fullPath = Path.Combine(FindRepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));
        var sampleDirectory = Path.GetDirectoryName(fullPath) ?? FindRepositoryRoot();
        return new QueueKeyboardInputProvider(keys: ["SPACE", "SPACE", "ESC"], pressedKeys: [], resourceBaseDirectory: sampleDirectory);
    }

    private static QueueKeyboardInputProvider CreateGraphicsSpriteSampleInputProvider(string relativePath)
    {
        return CreateGraphicsSpriteSampleInputProvider(relativePath, ["SPACE", "ESC"]);
    }

    private static QueueKeyboardInputProvider CreateGraphicsSpriteSampleInputProvider(string relativePath, IEnumerable<string> keys)
    {
        var fullPath = Path.Combine(FindRepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));
        var sampleDirectory = Path.GetDirectoryName(fullPath) ?? FindRepositoryRoot();
        return new QueueKeyboardInputProvider(keys: keys, pressedKeys: [], resourceBaseDirectory: sampleDirectory);
    }

    private static QueueKeyboardInputProvider CreateGraphicsModernSampleInputProvider(string relativePath, IEnumerable<string> keys)
    {
        return CreateGraphicsModernSampleInputProvider(relativePath, keys, pressedKeys: []);
    }

    private static QueueKeyboardInputProvider CreateGraphicsModernSampleInputProvider(
        string relativePath,
        IEnumerable<string> keys,
        IEnumerable<string> pressedKeys)
    {
        var fullPath = Path.Combine(FindRepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));
        var sampleDirectory = Path.GetDirectoryName(fullPath) ?? FindRepositoryRoot();
        return new QueueKeyboardInputProvider(keys: keys, pressedKeys: pressedKeys, resourceBaseDirectory: sampleDirectory);
    }

    private static string[] CreateInputRecordIntakeLines()
    {
        var lines = new string[180];

        for (var index = 0; index < lines.Length; index++)
        {
            var personNumber = index + 1;
            var name = $"USER{personNumber:000}";
            var age = 20 + (personNumber % 37);
            var score = 45 + ((personNumber * 7) % 51);
            lines[index] = $"{name}, {age}, {score}";
        }

        return lines;
    }

    private static ExecutionResult Run(
        string sourceText,
        IInputProvider? inputProvider = null)
    {
        var pipeline = new AxiomExecutionPipeline();
        return pipeline.Run(new ExecutionRequest(sourceText, inputProvider));
    }

    private static ExecutionResult RunFromSampleDirectory(
        string relativePath,
        string sourceText,
        IInputProvider? inputProvider = null)
    {
        var fullPath = Path.Combine(FindRepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));
        var sampleDirectory = Path.GetDirectoryName(fullPath) ?? FindRepositoryRoot();
        return RunFromWorkingDirectory(sampleDirectory, sourceText, inputProvider);
    }

    private static ExecutionResult RunFromWorkingDirectory(
        string workingDirectory,
        string sourceText,
        IInputProvider? inputProvider = null)
    {
        return new AxiomExecutionPipeline().Run(new ExecutionRequest(
            sourceText, inputProvider, resourceBaseDirectory: workingDirectory));
    }

    private static string CreateArtifactsTestDirectory(string prefix)
    {
        var artifactsDirectory = Path.Combine(FindRepositoryRoot(), ".artifacts", "tests");
        Directory.CreateDirectory(artifactsDirectory);

        var testDirectory = Path.Combine(artifactsDirectory, prefix + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testDirectory);
        return testDirectory;
    }

    private static string FormatDiagnostics(ExecutionResult result) =>
        string.Join(
            " | ",
            result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}@{diagnostic.Location.Line}:{diagnostic.Location.Column}"));

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Axiom.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root was not found from the current test output directory.");
    }

    private sealed class QueueInputProvider : IInputProvider
    {
        private readonly Queue<string> _values;

        public QueueInputProvider(params string[] values)
        {
            _values = new Queue<string>(values);
        }

        public string ReadInput(InputRequest request) => _values.Count > 0 ? _values.Dequeue() : string.Empty;
    }

    private sealed class QueueKeyboardInputProvider : IInputProvider, IKeyboardInputProvider, IMouseInputProvider, IGraphicsHost
    {
        private readonly Queue<string> _inputs;
        private readonly Queue<string> _keys;
        private readonly Queue<int> _mousePresses;
        private readonly Queue<MouseInputEvent> _mouseEvents;
        private readonly HashSet<string> _pressedKeys;
        private readonly HashSet<int> _pressedMouseButtons;
        private readonly long _mouseX;
        private readonly long _mouseY;
        private readonly string? _resourceBaseDirectory;
        private readonly Dictionary<(long X, long Y), int> _pixels = [];
        private readonly Dictionary<int, Dictionary<(long X, long Y), int>> _screenPages = [];
        private readonly List<DrawImageCall> _drawImages = [];
        private readonly List<DrawImageRegionCall> _drawImageRegions = [];
        private readonly List<DrawSpriteCall> _drawSprites = [];
        private readonly List<DrawRectCall> _drawRects = [];
        private readonly List<FillRectCall> _fillRects = [];
        private readonly List<CanvasCameraCall> _canvasCameraCalls = [];
        private readonly List<CanvasLineCall> _canvasDrawLines = [];
        private readonly List<CanvasCircleCall> _canvasDrawCircles = [];
        private readonly List<CanvasCircleCall> _canvasFillCircles = [];
        private readonly List<CanvasTextCall> _canvasDrawTexts = [];
        private bool _isScreenActive;
        private bool _isCanvasActive;
        private bool _isScreenOutputActive;
        private int _screenMode;
        private int _activeScreenPage;
        private int _visualScreenPage;
        private int _screenWidth;
        private int _screenHeight;
        private long _canvasCameraX;
        private long _canvasCameraY;
        private int _currentForegroundColor = 15;
        private int _currentBackgroundColor;
        private bool _isTextBackgroundTransparent;
        private long _currentGraphicsX;
        private long _currentGraphicsY;
        private bool _viewportEnabled;
        private bool _viewportRelativeCoordinates;
        private long _viewportLeft;
        private long _viewportTop;
        private long _viewportRight = -1;
        private long _viewportBottom = -1;
        private bool _windowEnabled;
        private bool _windowUsesScreenCoordinates;
        private double _windowLeft;
        private double _windowTop;
        private double _windowRight;
        private double _windowBottom;
        private string _pendingGraphicsLine = string.Empty;

        public QueueKeyboardInputProvider(params string[] keys)
            : this(keys, pressedKeys: null)
        {
        }

        public QueueKeyboardInputProvider(
            IEnumerable<string>? keys,
            IEnumerable<string>? pressedKeys,
            long mouseX = -1,
            long mouseY = -1,
            IEnumerable<int>? pressedMouseButtons = null,
            IEnumerable<int>? mousePresses = null,
            IEnumerable<MouseInputEvent>? mouseEvents = null,
            IEnumerable<string>? inputs = null,
            string? resourceBaseDirectory = null)
        {
            _inputs = new Queue<string>(inputs ?? []);
            _keys = new Queue<string>(keys ?? []);
            _mousePresses = new Queue<int>(mousePresses ?? []);
            _mouseEvents = new Queue<MouseInputEvent>(mouseEvents ?? (mousePresses ?? []).Select(static buttonNumber => new MouseInputEvent(MouseInputEventKind.Press, buttonNumber)));
            _pressedKeys = new HashSet<string>(
                (pressedKeys ?? []).Select(KeyboardInputNormalizer.Normalize),
                StringComparer.Ordinal);
            _mouseX = mouseX;
            _mouseY = mouseY;
            _pressedMouseButtons = new HashSet<int>(pressedMouseButtons ?? []);
            _resourceBaseDirectory = resourceBaseDirectory;
        }

        public List<string> GraphicsOutput { get; } = [];

        public IReadOnlyList<DrawImageCall> DrawImages => _drawImages;

        public IReadOnlyList<DrawImageRegionCall> DrawImageRegions => _drawImageRegions;

        public IReadOnlyList<DrawSpriteCall> DrawSprites => _drawSprites;

        public IReadOnlyList<DrawRectCall> DrawRects => _drawRects;

        public IReadOnlyList<FillRectCall> FillRects => _fillRects;

        public IReadOnlyList<CanvasCameraCall> CanvasCameraCalls => _canvasCameraCalls;

        public IReadOnlyList<CanvasLineCall> CanvasDrawLines => _canvasDrawLines;

        public IReadOnlyList<CanvasCircleCall> CanvasDrawCircles => _canvasDrawCircles;

        public IReadOnlyList<CanvasCircleCall> CanvasFillCircles => _canvasFillCircles;

        public IReadOnlyList<CanvasTextCall> CanvasDrawTexts => _canvasDrawTexts;

        public List<(long Width, long Height)> CanvasActivations { get; } = [];

        public List<int> FrameClears { get; } = [];

        public int BeginFrameCount { get; private set; }

        public int PresentFrameCount { get; private set; }

        public int ResetCanvasCameraCount { get; private set; }

        public string ReadInput(InputRequest request) => _inputs.Count > 0 ? _inputs.Dequeue() : string.Empty;

        public string ReadKey() => _keys.Count > 0 ? _keys.Dequeue() : string.Empty;

        public bool IsKeyDown(string keyText) => _pressedKeys.Contains(KeyboardInputNormalizer.Normalize(keyText));

        public long GetMouseX() => _mouseX;

        public long GetMouseY() => _mouseY;

        public bool IsMouseButtonDown(int buttonNumber) => _pressedMouseButtons.Contains(buttonNumber);

        public int ReadMouseButtonPress() => _mousePresses.Count > 0 ? _mousePresses.Dequeue() : 0;

        public MouseInputEvent ReadMouseEvent() => _mouseEvents.Count > 0 ? _mouseEvents.Dequeue() : default;

        public bool IsScreenActive => _isScreenActive;

        public bool IsCanvasActive => _isCanvasActive;

        public bool IsScreenOutputActive => _isScreenOutputActive;

        public int ScreenMode => _screenMode;

        public int ActiveScreenPage => _activeScreenPage;

        public int VisualScreenPage => _visualScreenPage;

        public int ScreenWidth => _screenWidth;

        public int ScreenHeight => _screenHeight;

        public int CurrentForegroundColor => _currentForegroundColor;

        public int CurrentBackgroundColor => _currentBackgroundColor;

        public bool IsTextBackgroundTransparent => _isTextBackgroundTransparent;

        public long CurrentGraphicsX => _currentGraphicsX;

        public long CurrentGraphicsY => _currentGraphicsY;

        public long CanvasCameraX => _canvasCameraX;

        public long CanvasCameraY => _canvasCameraY;

        public bool ViewportEnabled => _viewportEnabled;

        public bool ViewportRelativeCoordinates => _viewportRelativeCoordinates;

        public long ViewportLeft => _viewportLeft;

        public long ViewportTop => _viewportTop;

        public long ViewportRight => _viewportRight;

        public long ViewportBottom => _viewportBottom;

        public bool WindowEnabled => _windowEnabled;

        public bool WindowUsesScreenCoordinates => _windowUsesScreenCoordinates;

        public double WindowLeft => _windowLeft;

        public double WindowTop => _windowTop;

        public double WindowRight => _windowRight;

        public double WindowBottom => _windowBottom;

        public void ActivateScreen(int mode)
        {
            ActivateScreen(mode, activePage: 0, visualPage: 0);
        }

        public void ActivateScreen(int mode, int activePage, int visualPage)
        {
            var sameMode = _screenMode == mode;
            _isScreenActive = mode != 0;
            _isCanvasActive = false;
            _isScreenOutputActive = true;
            _screenMode = mode;
            _activeScreenPage = _isScreenActive ? activePage : 0;
            _visualScreenPage = _isScreenActive ? visualPage : 0;
            (_screenWidth, _screenHeight) = mode switch
            {
                0 => (640, 300),
                1 => (320, 200),
                2 => (640, 480),
                3 => (800, 600),
                4 => (1024, 768),
                5 => (1280, 720),
                6 => (1366, 768),
                7 => (1600, 900),
                8 => (1920, 1080),
                9 => (2560, 1440),
                10 => (1918, 1079),
                _ => (0, 0)
            };
            _pixels.Clear();
            if (!_isScreenActive || !sameMode)
            {
                _screenPages.Clear();
            }

            if (_isScreenActive)
            {
                _screenPages.TryAdd(_activeScreenPage, []);
                _screenPages.TryAdd(_visualScreenPage, []);
                foreach (var item in _screenPages[_activeScreenPage])
                {
                    _pixels[item.Key] = item.Value;
                }
            }

            GraphicsOutput.Clear();
            _pendingGraphicsLine = string.Empty;
            _currentForegroundColor = 15;
            _currentBackgroundColor = 0;
            _isTextBackgroundTransparent = false;
            _currentGraphicsX = 0;
            _currentGraphicsY = 0;
            ResetViewport();
            ResetWindow();
        }

        public void ActivateCanvas(long width, long height)
        {
            _isScreenActive = false;
            _isCanvasActive = true;
            _isScreenOutputActive = false;
            _screenMode = 0;
            _activeScreenPage = 0;
            _visualScreenPage = 0;
            _screenWidth = (int)width;
            _screenHeight = (int)height;
            _canvasCameraX = 0;
            _canvasCameraY = 0;
            _pixels.Clear();
            _screenPages.Clear();
            GraphicsOutput.Clear();
            _pendingGraphicsLine = string.Empty;
            _currentGraphicsX = 0;
            _currentGraphicsY = 0;
            ResetViewport();
            ResetWindow();
            CanvasActivations.Add((width, height));
        }

        public void BeginFrame()
        {
            BeginFrameCount++;
        }

        public void PresentFrame()
        {
            PresentFrameCount++;
        }

        public void ClearFrame(int color)
        {
            FrameClears.Add(color);
            if (!_isCanvasActive)
            {
                return;
            }

            var paletteIndex = MapCanvasColorToPaletteIndex(color);
            _pixels.Clear();
            for (var y = 0L; y < _screenHeight; y++)
            {
                for (var x = 0L; x < _screenWidth; x++)
                {
                    _pixels[(x, y)] = paletteIndex;
                }
            }
        }

        public void DrawRect(long x, long y, long width, long height, int color)
        {
            _drawRects.Add(new DrawRectCall(x, y, width, height, color));
        }

        public void FillRect(long x, long y, long width, long height, int color)
        {
            _fillRects.Add(new FillRectCall(x, y, width, height, color));
            if (_isCanvasActive)
            {
                RasterizeCanvasFillRect(x, y, width, height, color);
            }
        }

        public void SetCanvasCamera(long x, long y)
        {
            _canvasCameraCalls.Add(new CanvasCameraCall(x, y));
            _canvasCameraX = x;
            _canvasCameraY = y;
        }

        public void ResetCanvasCamera()
        {
            ResetCanvasCameraCount++;
            _canvasCameraX = 0;
            _canvasCameraY = 0;
        }

        public void DrawCanvasLine(long x1, long y1, long x2, long y2, int color)
        {
            _canvasDrawLines.Add(new CanvasLineCall(x1, y1, x2, y2, color));
        }

        public void DrawCanvasCircle(long x, long y, long radius, int color)
        {
            _canvasDrawCircles.Add(new CanvasCircleCall(x, y, radius, color));
        }

        public void FillCanvasCircle(long x, long y, long radius, int color)
        {
            _canvasFillCircles.Add(new CanvasCircleCall(x, y, radius, color));
        }

        public void DrawCanvasText(string text, long x, long y, int color)
        {
            _canvasDrawTexts.Add(new CanvasTextCall(text, x, y, color));
        }

        public void DeactivateScreen()
        {
            _isScreenActive = false;
            _isCanvasActive = false;
            _isScreenOutputActive = false;
            _screenMode = 0;
            _activeScreenPage = 0;
            _visualScreenPage = 0;
            _screenWidth = 0;
            _screenHeight = 0;
            _canvasCameraX = 0;
            _canvasCameraY = 0;
            _pixels.Clear();
            _screenPages.Clear();
            _currentGraphicsX = 0;
            _currentGraphicsY = 0;
            ResetViewport();
            ResetWindow();
        }

        public void ClearGraphics()
        {
            _pixels.Clear();
            if (_isScreenActive)
            {
                _screenPages[_activeScreenPage] = [];
            }
            GraphicsOutput.Clear();
            _pendingGraphicsLine = string.Empty;
            _currentGraphicsX = 0;
            _currentGraphicsY = 0;
        }

        public void SetTextColor(int foreground, TextBackgroundMode backgroundMode, int background = 0)
        {
            _currentForegroundColor = foreground;
            switch (backgroundMode)
            {
                case TextBackgroundMode.Color:
                    _currentBackgroundColor = background;
                    _isTextBackgroundTransparent = false;
                    break;
                case TextBackgroundMode.Transparent:
                    _isTextBackgroundTransparent = true;
                    break;
                default:
                    break;
            }
        }

        public void LocateText(int row, int column)
        {
        }

        public bool SetTextViewport(int topRow, int bottomRow)
        {
            if (!_isScreenActive)
            {
                return false;
            }

            var visibleRows = Math.Max(1, _screenHeight / 12);
            if (topRow < 1 || bottomRow < topRow || bottomRow > visibleRows)
            {
                return false;
            }

            return true;
        }

        public void ResetTextViewport()
        {
        }

        public void WriteGraphicsText(string text, bool newLine)
        {
            _pendingGraphicsLine += text;

            if (!newLine)
            {
                return;
            }

            GraphicsOutput.Add(_pendingGraphicsLine);
            _pendingGraphicsLine = string.Empty;
        }

        public string ReadGraphicsInput(InputRequest request)
        {
            var text = ReadInput(request);
            WriteGraphicsText(CreateGraphicsPrompt(request) + text, newLine: true);
            return text;
        }

        public RuntimeImageValue? LoadImage(string path)
        {
            var resolvedPath = ResolveResourcePath(path);
            return File.Exists(resolvedPath)
                ? new RuntimeImageValue(16, 16, Enumerable.Repeat(0xFFFF00FFu, 16 * 16).ToArray())
                : null;
        }

        public RuntimeSpriteValue? LoadSprite(string path, int frameWidth, int frameHeight)
        {
            var resolvedPath = ResolveResourcePath(path);
            return File.Exists(resolvedPath)
                ? new RuntimeSpriteValue(new RuntimeImageValue(32, 32, Enumerable.Repeat(0xFFFF00FFu, 32 * 32).ToArray()), frameWidth, frameHeight)
                : null;
        }

        public RuntimeTilesetValue? LoadTileset(string path, int tileWidth, int tileHeight, int margin = 0, int spacing = 0)
        {
            var resolvedPath = ResolveResourcePath(path);
            if (!File.Exists(resolvedPath))
            {
                return null;
            }

            var size = Path.GetFileName(resolvedPath).Equals("walk_sheet.png", StringComparison.OrdinalIgnoreCase)
                ? 32
                : 16;
            var image = new RuntimeImageValue(size, size, Enumerable.Repeat(0xFFFF00FFu, size * size).ToArray());

            try
            {
                return new RuntimeTilesetValue(image, tileWidth, tileHeight, margin, spacing);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        public RuntimeTilemapValue? LoadTilemap(string path)
        {
            var resolvedPath = ResolveResourcePath(path);
            if (!File.Exists(resolvedPath))
            {
                return null;
            }

            try
            {
                return RuntimeTilemapValue.LoadFromTiledJson(
                    resolvedPath,
                    resolvedImagePath => File.Exists(resolvedImagePath)
                        ? CreateFakeImage(Path.GetFileName(resolvedImagePath).Equals("walk_sheet.png", StringComparison.OrdinalIgnoreCase) ? 32 : 16)
                        : null);
            }
            catch (Exception exception) when (exception is IOException or InvalidDataException or System.Text.Json.JsonException or OverflowException)
            {
                return null;
            }
        }

        public void DrawImage(RuntimeImageValue image, long x, long y, int width, int height)
        {
            _drawImages.Add(new DrawImageCall(image, x, y, width, height));
        }

        public void DrawImageRegion(RuntimeImageValue image, int sourceX, int sourceY, int sourceWidth, int sourceHeight, long x, long y, int width, int height)
        {
            _drawImageRegions.Add(new DrawImageRegionCall(image, sourceX, sourceY, sourceWidth, sourceHeight, x, y, width, height));
        }

        public void DrawSprite(RuntimeSpriteValue sprite, int frame, long x, long y, int width, int height)
        {
            _drawSprites.Add(new DrawSpriteCall(sprite, frame, x, y, width, height));
        }

        private string ResolveResourcePath(string path)
        {
            var expandedPath = Environment.ExpandEnvironmentVariables(path);
            if (Path.IsPathFullyQualified(expandedPath))
            {
                return expandedPath;
            }

            var baseDirectory = string.IsNullOrWhiteSpace(_resourceBaseDirectory)
                ? Environment.CurrentDirectory
                : _resourceBaseDirectory!;
            return Path.GetFullPath(Path.Combine(baseDirectory, expandedPath));
        }

        private static RuntimeImageValue CreateFakeImage(int size) =>
            new(size, size, Enumerable.Repeat(0xFFFF00FFu, size * size).ToArray());

        private static string CreateGraphicsPrompt(InputRequest request)
        {
            var promptText = request.PromptText?.TrimEnd();
            if (string.IsNullOrWhiteSpace(promptText))
            {
                return "? ";
            }

            return promptText.EndsWith('?') || promptText.EndsWith(':')
                ? $"{promptText} "
                : $"{promptText}? ";
        }

        public void SetPixel(long x, long y, int color)
        {
            if (_isCanvasActive)
            {
                if (!TryTranslateCanvasWorldRectangleToScreen(x, y, 1, 1, out var left, out var top, out _, out _))
                {
                    return;
                }

                _pixels[(left, top)] = Math.Abs(color) % 256;
                _currentGraphicsX = x;
                _currentGraphicsY = y;
                return;
            }

            if (!GraphicsCoordinateSpaceMath.TryTranslateLogicalToAbsolute(this, x, y, out var absoluteX, out var absoluteY))
            {
                return;
            }

            _pixels[(absoluteX, absoluteY)] = color;
            if (_isScreenActive)
            {
                _screenPages.TryAdd(_activeScreenPage, []);
                _screenPages[_activeScreenPage][(absoluteX, absoluteY)] = color;
            }
            _currentGraphicsX = x;
            _currentGraphicsY = y;
        }

        public void SetAbsolutePixel(long x, long y, int color)
        {
            if (IsInsideScreen(x, y))
            {
                _pixels[(x, y)] = color;
                if (_isScreenActive)
                {
                    _screenPages.TryAdd(_activeScreenPage, []);
                    _screenPages[_activeScreenPage][(x, y)] = color;
                }
            }
        }

        public bool CopyScreenPage(int sourcePage, int destinationPage)
        {
            if (!_isScreenActive || sourcePage is < 0 or > 3 || destinationPage is < 0 or > 3)
            {
                return false;
            }

            _screenPages.TryAdd(sourcePage, []);
            _screenPages[destinationPage] = new Dictionary<(long X, long Y), int>(_screenPages[sourcePage]);
            if (destinationPage == _activeScreenPage)
            {
                _pixels.Clear();
                foreach (var item in _screenPages[destinationPage])
                {
                    _pixels[item.Key] = item.Value;
                }
            }

            return true;
        }

        public void SetGraphicsCurrentPosition(long x, long y)
        {
            _currentGraphicsX = x;
            _currentGraphicsY = y;
        }

        public int GetPixel(long x, long y)
        {
            if (_isCanvasActive)
            {
                return _pixels.TryGetValue((x, y), out var canvasColor) ? canvasColor : 0;
            }

            if (!GraphicsCoordinateSpaceMath.TryTranslateLogicalToAbsolute(this, x, y, out var absoluteX, out var absoluteY))
            {
                return 0;
            }

            return _pixels.TryGetValue((absoluteX, absoluteY), out var color) ? color : 0;
        }

        public int GetAbsolutePixel(long x, long y) =>
            _pixels.TryGetValue((x, y), out var color) ? color : 0;

        public bool SetViewport(long left, long top, long right, long bottom, int? fillColor, int? borderColor, bool useScreenCoordinates)
        {
            if ((!_isScreenActive && !_isCanvasActive) || left > right || top > bottom)
            {
                return false;
            }

            var baseLeft = _viewportEnabled ? _viewportLeft : 0L;
            var baseTop = _viewportEnabled ? _viewportTop : 0L;
            var absoluteLeft = useScreenCoordinates ? left : GraphicsLineMath.SaturatingAdd(baseLeft, left);
            var absoluteTop = useScreenCoordinates ? top : GraphicsLineMath.SaturatingAdd(baseTop, top);
            var absoluteRight = useScreenCoordinates ? right : GraphicsLineMath.SaturatingAdd(baseLeft, right);
            var absoluteBottom = useScreenCoordinates ? bottom : GraphicsLineMath.SaturatingAdd(baseTop, bottom);
            if (!IsInsideActiveGraphicsSurface(absoluteLeft, absoluteTop) || !IsInsideActiveGraphicsSurface(absoluteRight, absoluteBottom))
            {
                return false;
            }

            var currentAbsolute = ResolveCurrentAbsolutePosition();

            if (fillColor.HasValue)
            {
                for (var y = absoluteTop; y <= absoluteBottom; y++)
                {
                    for (var x = absoluteLeft; x <= absoluteRight; x++)
                    {
                        _pixels[(x, y)] = fillColor.Value;
                    }
                }
            }

            if (borderColor.HasValue)
            {
                for (var x = absoluteLeft; x <= absoluteRight; x++)
                {
                    _pixels[(x, absoluteTop)] = borderColor.Value;
                    _pixels[(x, absoluteBottom)] = borderColor.Value;
                }

                for (var y = absoluteTop; y <= absoluteBottom; y++)
                {
                    _pixels[(absoluteLeft, y)] = borderColor.Value;
                    _pixels[(absoluteRight, y)] = borderColor.Value;
                }
            }

            _viewportEnabled = true;
            _viewportRelativeCoordinates = !useScreenCoordinates;
            _viewportLeft = absoluteLeft;
            _viewportTop = absoluteTop;
            _viewportRight = absoluteRight;
            _viewportBottom = absoluteBottom;
            if (GraphicsCoordinateSpaceMath.TryTranslateAbsoluteToLogical(this, currentAbsolute.X, currentAbsolute.Y, out var logicalX, out var logicalY))
            {
                _currentGraphicsX = logicalX;
                _currentGraphicsY = logicalY;
            }
            return true;
        }

        public void ResetViewport()
        {
            var currentAbsolute = ResolveCurrentAbsolutePosition();
            _viewportEnabled = false;
            _viewportRelativeCoordinates = false;
            _viewportLeft = 0;
            _viewportTop = 0;
            _viewportRight = _screenWidth - 1L;
            _viewportBottom = _screenHeight - 1L;
            if (GraphicsCoordinateSpaceMath.TryTranslateAbsoluteToLogical(this, currentAbsolute.X, currentAbsolute.Y, out var logicalX, out var logicalY))
            {
                _currentGraphicsX = logicalX;
                _currentGraphicsY = logicalY;
            }
        }

        public bool SetWindow(double left, double top, double right, double bottom, bool useScreenCoordinates)
        {
            var viewport = _viewportEnabled
                ? new GraphicsViewport(_viewportLeft, _viewportTop, _viewportRight, _viewportBottom, _viewportRelativeCoordinates)
                : new GraphicsViewport(0, 0, _screenWidth - 1L, _screenHeight - 1L, UsesRelativeCoordinates: false);
            if (!_isScreenActive || viewport.Width < 2 || viewport.Height < 2)
            {
                return false;
            }

            if (!GraphicsCoordinateSpaceMath.TryCreateWindow(left, top, right, bottom, useScreenCoordinates, out var window))
            {
                return false;
            }

            var currentAbsolute = ResolveCurrentAbsolutePosition();
            _windowEnabled = true;
            _windowUsesScreenCoordinates = window.UsesScreenCoordinates;
            _windowLeft = window.Left;
            _windowTop = window.Top;
            _windowRight = window.Right;
            _windowBottom = window.Bottom;
            if (GraphicsCoordinateSpaceMath.TryTranslateAbsoluteToLogical(this, currentAbsolute.X, currentAbsolute.Y, out var logicalX, out var logicalY))
            {
                _currentGraphicsX = logicalX;
                _currentGraphicsY = logicalY;
            }

            return true;
        }

        public void ResetWindow()
        {
            var currentAbsolute = ResolveCurrentAbsolutePosition();
            _windowEnabled = false;
            _windowUsesScreenCoordinates = false;
            _windowLeft = 0d;
            _windowTop = 0d;
            _windowRight = 0d;
            _windowBottom = 0d;
            if (GraphicsCoordinateSpaceMath.TryTranslateAbsoluteToLogical(this, currentAbsolute.X, currentAbsolute.Y, out var logicalX, out var logicalY))
            {
                _currentGraphicsX = logicalX;
                _currentGraphicsY = logicalY;
            }
        }

        private (long X, long Y) ResolveCurrentAbsolutePosition()
        {
            if (GraphicsCoordinateSpaceMath.TryResolveCurrentAbsolutePosition(this, out var absoluteX, out var absoluteY))
            {
                return (absoluteX, absoluteY);
            }

            return (_currentGraphicsX, _currentGraphicsY);
        }

        private void RasterizeCanvasFillRect(long x, long y, long width, long height, int color)
        {
            if (!TryTranslateCanvasWorldRectangleToScreen(x, y, width, height, out var left, out var top, out var right, out var bottom))
            {
                return;
            }

            var paletteIndex = MapCanvasColorToPaletteIndex(color);
            for (var pixelY = top; pixelY <= bottom; pixelY++)
            {
                for (var pixelX = left; pixelX <= right; pixelX++)
                {
                    _pixels[(pixelX, pixelY)] = paletteIndex;
                }
            }
        }

        private bool TryTranslateCanvasWorldRectangleToScreen(long x, long y, long width, long height, out long left, out long top, out long right, out long bottom)
        {
            left = 0;
            top = 0;
            right = -1;
            bottom = -1;
            if (!_isCanvasActive || width <= 0 || height <= 0)
            {
                return false;
            }

            var viewportLeft = _viewportEnabled ? _viewportLeft : 0L;
            var viewportTop = _viewportEnabled ? _viewportTop : 0L;
            var viewportRight = _viewportEnabled ? _viewportRight : _screenWidth - 1;
            var viewportBottom = _viewportEnabled ? _viewportBottom : _screenHeight - 1;
            left = viewportLeft + (x - _canvasCameraX);
            top = viewportTop + (y - _canvasCameraY);
            right = left + width - 1;
            bottom = top + height - 1;
            left = Math.Max(left, viewportLeft);
            top = Math.Max(top, viewportTop);
            right = Math.Min(right, viewportRight);
            bottom = Math.Min(bottom, viewportBottom);
            left = Math.Max(left, 0);
            top = Math.Max(top, 0);
            right = Math.Min(right, _screenWidth - 1);
            bottom = Math.Min(bottom, _screenHeight - 1);
            return left <= right && top <= bottom;
        }

        private static int MapCanvasColorToPaletteIndex(int color)
        {
            var red = (color >> 16) & 0xFF;
            var green = (color >> 8) & 0xFF;
            var blue = color & 0xFF;
            var bestIndex = 0;
            var bestDistance = int.MaxValue;

            for (var index = 0; index < 256; index++)
            {
                var (paletteRed, paletteGreen, paletteBlue) = MapPaletteIndexToRgb(index);
                var distance =
                    Square(red - paletteRed) +
                    Square(green - paletteGreen) +
                    Square(blue - paletteBlue);
                if (distance >= bestDistance)
                {
                    continue;
                }

                bestDistance = distance;
                bestIndex = index;
                if (distance == 0)
                {
                    break;
                }
            }

            return bestIndex;
        }

        private static (int Red, int Green, int Blue) MapPaletteIndexToRgb(int colorIndex)
        {
            var normalized = Math.Abs(colorIndex) % 256;
            return normalized switch
            {
                0 => (0, 0, 0),
                1 => (0, 0, 170),
                2 => (0, 170, 0),
                3 => (0, 170, 170),
                4 => (170, 0, 0),
                5 => (170, 0, 170),
                6 => (170, 85, 0),
                7 => (170, 170, 170),
                8 => (85, 85, 85),
                9 => (85, 85, 255),
                10 => (85, 255, 85),
                11 => (85, 255, 255),
                12 => (255, 85, 85),
                13 => (255, 85, 255),
                14 => (255, 255, 85),
                15 => (255, 255, 255),
                >= 16 and <= 231 => MapRgbCubeIndexToRgb(normalized),
                _ => MapGrayIndexToRgb(normalized)
            };
        }

        private static (int Red, int Green, int Blue) MapRgbCubeIndexToRgb(int colorIndex)
        {
            var cubeIndex = colorIndex - 16;
            var red = cubeIndex / 36;
            var green = cubeIndex / 6 % 6;
            var blue = cubeIndex % 6;
            return (MapRgbCubeComponent(red), MapRgbCubeComponent(green), MapRgbCubeComponent(blue));
        }

        private static (int Red, int Green, int Blue) MapGrayIndexToRgb(int colorIndex)
        {
            var component = 8 + ((colorIndex - 232) * 10);
            return (component, component, component);
        }

        private static int MapRgbCubeComponent(int component) => component == 0 ? 0 : 55 + (component * 40);

        private static int Square(int value) => value * value;

        private bool IsInsideActiveGraphicsSurface(long x, long y) => x >= 0 && y >= 0 && x < ScreenWidth && y < ScreenHeight;

        private bool IsInsideScreen(long x, long y) => x >= 0 && y >= 0 && x < _screenWidth && y < _screenHeight;
    }

    private sealed record DrawImageCall(RuntimeImageValue Image, long X, long Y, int Width, int Height);

    private sealed record DrawImageRegionCall(RuntimeImageValue Image, int SourceX, int SourceY, int SourceWidth, int SourceHeight, long X, long Y, int Width, int Height);

    private sealed record DrawSpriteCall(RuntimeSpriteValue Sprite, int Frame, long X, long Y, int Width, int Height);

    private sealed record DrawRectCall(long X, long Y, long Width, long Height, int Color);

    private sealed record FillRectCall(long X, long Y, long Width, long Height, int Color);

    private sealed record CanvasCameraCall(long X, long Y);

    private sealed record CanvasLineCall(long X1, long Y1, long X2, long Y2, int Color);

    private sealed record CanvasCircleCall(long X, long Y, long Radius, int Color);

    private sealed record CanvasTextCall(string Text, long X, long Y, int Color);
}

