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
        Assert.Empty(result.Diagnostics);
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
    [InlineData("samples/acceptance/negative/select_case_invalid_types.axm", ExecutionStatus.SemanticFailure, "MB2003")]
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
    public void NegativeRuntimeAcceptanceProgram_HasStableDiagnosticParityBetweenBytecodeAndInterpreter(
        string relativePath,
        string expectedCode)
    {
        var sourceText = TestProgramLoader.LoadText(relativePath);

        var bytecodeResult = Run(sourceText, mode: ExecutionMode.Bytecode);
        var interpreterResult = Run(sourceText, mode: ExecutionMode.Interpreter);

        Assert.Equal(ExecutionStatus.RuntimeFailure, bytecodeResult.Status);
        Assert.Equal(ExecutionStatus.RuntimeFailure, interpreterResult.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, bytecodeResult.ActualEngine);
        Assert.Equal(ExecutionEngineKind.Interpreter, interpreterResult.ActualEngine);
        Assert.False(bytecodeResult.UsedInterpreterFallback);
        Assert.False(interpreterResult.UsedInterpreterFallback);
        Assert.Null(bytecodeResult.InterpreterFallbackReason);
        Assert.Null(interpreterResult.InterpreterFallbackReason);
        Assert.Equal(bytecodeResult.Output, interpreterResult.Output);

        var bytecodeDiagnostic = Assert.Single(bytecodeResult.Diagnostics);
        var interpreterDiagnostic = Assert.Single(interpreterResult.Diagnostics);
        Assert.Equal(expectedCode, bytecodeDiagnostic.Code);
        Assert.Equal(expectedCode, interpreterDiagnostic.Code);
        Assert.True(bytecodeDiagnostic.Location.Line > 0);
        Assert.True(bytecodeDiagnostic.Location.Column > 0);
        Assert.True(interpreterDiagnostic.Location.Line > 0);
        Assert.True(interpreterDiagnostic.Location.Column > 0);
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
        Assert.Empty(result.Diagnostics);
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
        Assert.Empty(result.Diagnostics);
        Assert.NotEmpty(result.Output);
    }

    [Theory]
    [InlineData("samples/control-flow/if/basic_if_else.axm", new[] { "not positive" })]
    [InlineData("samples/control-flow/if/elseif_chain.axm", new[] { "medium" })]
    [InlineData("samples/control-flow/for/basic_loop.axm", new[] { "1", "2", "3" })]
    [InlineData("samples/control-flow/for/step_loop.axm", new[] { "0", "2", "4", "6" })]
    [InlineData("samples/control-flow/for/nested_loops.axm", new[] { "1:1", "1:2", "2:1", "2:2" })]
    [InlineData("samples/control-flow/for/exit_for.axm", new[] { "1", "2", "done" })]
    [InlineData("samples/control-flow/for/predeclared_counter.axm", new[] { "1", "2", "3", "after=4" })]
    [InlineData("samples/control-flow/while/basic_loop.axm", new[] { "0", "1", "2" })]
    [InlineData("samples/control-flow/do/do_while_loop.axm", new[] { "0", "1", "2", "done" })]
    [InlineData("samples/control-flow/do/do_until_loop.axm", new[] { "0", "1", "2" })]
    [InlineData("samples/control-flow/do/loop_while.axm", new[] { "0", "1", "2" })]
    [InlineData("samples/control-flow/do/loop_until.axm", new[] { "0", "1", "2" })]
    [InlineData("samples/control-flow/do/do_loop_exit.axm", new[] { "0", "1", "done" })]
    [InlineData("samples/control-flow/select_case/exact_values.axm", new[] { "two" })]
    [InlineData("samples/control-flow/select_case/ranges_and_is.axm", new[] { "good" })]
    [InlineData("samples/control-flow/select_case/string_values.axm", new[] { "save" })]
    [InlineData("samples/control-flow/select_case/mixed_selectors.axm", new[] { "matched" })]
    [InlineData("samples/control-flow/select_case/string_ranges.axm", new[] { "after", "first" })]
    [InlineData("samples/control-flow/select_case/boolean_values.axm", new[] { "enabled" })]
    [InlineData("samples/control-flow/select_case/first_match.axm", new[] { "first" })]
    [InlineData("samples/control-flow/select_case/no_match_no_else.axm", new[] { "after" })]
    [InlineData("samples/control-flow/return/function_value.axm", new[] { "42" })]
    [InlineData("samples/control-flow/return/sub_early_exit.axm", new[] { "before return", "after if" })]
    public void ControlFlowAcceptanceSamples_RunOnBytecodeVm(string relativePath, string[] expectedOutput)
    {
        var sourceText = TestProgramLoader.LoadText(relativePath);

        var result = Run(sourceText, mode: ExecutionMode.Bytecode);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
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

        var result = Run(sourceText, mode: ExecutionMode.Bytecode);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
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

        var result = Run(sourceText, mode: ExecutionMode.Bytecode);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
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
    [InlineData("samples/operators/print/empty_print.axm", new[] { "Before", "", "After" })]
    [InlineData("samples/operators/print/separator_semicolon.axm", new[] { "Hello, world", "123" })]
    [InlineData("samples/operators/print/separator_comma.axm", new[] { "A B C", "1 2 3 4 5 " })]
    [InlineData("samples/operators/print/mixed_list.axm", new[] { "12 3" })]
    [InlineData("samples/operators/print/using_basic.axm", new[] { "Name: Bob   |", "Total:  12.50", "Flag: X" })]
    public void DeclarationAssignmentAndPrintAcceptanceSamples_RunOnBytecodeVm(string relativePath, string[] expectedOutput)
    {
        var sourceText = TestProgramLoader.LoadText(relativePath);

        var result = Run(sourceText, mode: ExecutionMode.Bytecode);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Equal(expectedOutput, result.Output);
    }

    [Fact]
    public void InferredDimAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/dim/inferred_type_from_initializer.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(["15", "ALICE", "YES", "2", "14"], result.Output);
    }

    [Fact]
    public void LongInferenceAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/dim/long_inference.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(["21474836479", "42949672958"], result.Output);
    }

    [Fact]
    public void DecimalExplicitDeclarationAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/dim/decimal_explicit_declaration.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(["10.30", "5.15", "10.30"], result.Output);
    }

    [Fact]
    public void PrintUsingAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/print/using_basic.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(["Name: Bob   |", "Total:  12.50", "Flag: X"], result.Output);
    }

    [Fact]
    public void DateAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/functions/date/basic_usage.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
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
        Assert.Empty(result.Diagnostics);
        Assert.Equal(["TRUE"], result.Output);
    }

    [Fact]
    public void TimeAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/functions/time/basic_usage.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
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
        Assert.Empty(result.Diagnostics);
        Assert.Equal(["TRUE"], result.Output);
    }

    [Fact]
    public void NowAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/functions/now/basic_usage.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
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
        Assert.Empty(result.Diagnostics);
        Assert.Equal(["TRUE"], result.Output);
    }

    [Fact]
    public void TimerAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/functions/timer/basic_usage.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(["TRUE", "TRUE", "TRUE"], result.Output);
    }

    [Fact]
    public void UtcDateAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/functions/utcdate/current_utc_date.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
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
        Assert.Empty(result.Diagnostics);
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
        Assert.Empty(result.Diagnostics);
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
        Assert.Empty(result.Diagnostics);
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
        Assert.Empty(result.Diagnostics);
        Assert.Equal(["TRUE"], result.Output);
    }

    [Fact]
    public void UtcOffsetNamedZoneAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/functions/utcoffset/named_timezone_specific_offset.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(["TRUE"], result.Output);
    }

    [Fact]
    public void ToUtcAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/functions/toutc/roundtrip_to_local.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(["TRUE", "2026-04-20 14:05:06"], result.Output);
    }

    [Fact]
    public void ToUtcNamedZoneAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/functions/toutc/named_timezone_to_utc.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(["TRUE"], result.Output);
    }

    [Fact]
    public void FromUtcAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/functions/fromutc/roundtrip_to_utc.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(["TRUE", "2026-04-20 11:05:06"], result.Output);
    }

    [Fact]
    public void FromUtcNamedZoneAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/functions/fromutc/named_timezone_from_utc.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
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
        Assert.Empty(result.Diagnostics);
        Assert.Equal([expectedOutput], result.Output);
    }

    [Fact]
    public void BlockScopeSmokeSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/smoke/block_scope_semantics.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(["branch", "1", "2", "done"], result.Output);
    }

    [Fact]
    public void FullBlockScopeManualSmokeSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/smoke/block_scope_full_manual.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
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

        var result = Run(sourceText, inputProvider: new QueueInputProvider("Ника, 7"), mode: ExecutionMode.Bytecode);

        Assert.True(
            result.Succeeded,
            string.Join(" | ", result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}@{diagnostic.Location.Line}:{diagnostic.Location.Column}")));
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
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
        Assert.Empty(result.Diagnostics);
        Assert.Equal(["2"], result.Output);
    }

    [Fact]
    public void LocalFunctionAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/function/local_nested_call.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(["12"], result.Output);
    }

    [Fact]
    public void LocalSubCaptureAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/sub/capture_outer_local.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(["4"], result.Output);
    }

    [Fact]
    public void LocalFunctionCaptureAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/function/capture_outer_local.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(["23"], result.Output);
    }

    [Fact]
    public void FunctionFirstClassValueAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/function/first_class_value.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(["12"], result.Output);
    }

    [Fact]
    public void FunctionReturnedClosureAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/function/returned_closure.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(["15", "7"], result.Output);
    }

    [Fact]
    public void FunctionSharedClosureStateAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/function/shared_closure_state.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(["1", "2", "3"], result.Output);
    }

    [Fact]
    public void TypeFieldAccessAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/type/field_access.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(["", "0", "Ada", "36", "London"], result.Output);
    }

    [Fact]
    public void TypeArrayElementsAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/type/array_elements.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(["Ada", "36", "London", "Alan", "Grace", "1", "2"], result.Output);
    }

    [Fact]
    public void TypeArrayFieldsAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/type/array_fields.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.Empty(result.Diagnostics);
        Assert.Equal(["10", "20", "FALSE", "7", "9", "9", "0"], result.Output);
    }

    [Fact]
    public void TypeMultidimensionalArrayFieldsAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/type/multidimensional_array_fields.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.Empty(result.Diagnostics);
        Assert.Equal(["10", "20", "4", "FALSE", "9", "0"], result.Output);
    }

    [Fact]
    public void TypeArrayFieldViewsAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/type/array_field_views.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.Empty(result.Diagnostics);
        Assert.Equal(["23", "24", "2", "4", "4", "99"], result.Output);
    }

    [Fact]
    public void TypeNamedLiteralsAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/type/named_literals.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.Empty(result.Diagnostics);
        Assert.Equal(["Ada", "36", "Paris", "0", "0", "TRUE"], result.Output);
    }

    [Fact]
    public void TypePositionalLiteralAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/type/positional_constructor.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.Empty(result.Diagnostics);
        Assert.Equal(["Ada", "36", "Paris", "Grace", "40", "TRUE"], result.Output);
    }

    [Fact]
    public void TypeMethodsAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/type/methods_self.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.Empty(result.Diagnostics);
        Assert.Equal(["5", "5", "8", "8"], result.Output);
    }

    [Fact]
    public void TypeMethodReceiversFieldAndArrayAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/type/method_receivers_field_and_array.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.Empty(result.Diagnostics);
        Assert.Equal(["5", "7", "8", "9"], result.Output);
    }

    [Fact]
    public void TypeConstructorNewAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/type/constructor_new.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.Empty(result.Diagnostics);
        Assert.Equal(["Zero", "18", "Ada", "19", "20", "RUS", "Grace", "18", "19"], result.Output);
    }

    [Fact]
    public void TypeDefaultFieldValuesAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/type/default_field_values.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.Empty(result.Diagnostics);
        Assert.Equal(["Ada", "36", "TRUE"], result.Output);
    }

    [Fact]
    public void TypePartialNamedLiteralDefaultsAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/type/partial_named_literal_defaults.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.Empty(result.Diagnostics);
        Assert.Equal(["Grace", "36", "TRUE"], result.Output);
    }

    [Fact]
    public void TypeStructuralEqualityAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/type/structural_equality.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(["TRUE", "FALSE", "FALSE", "TRUE"], result.Output);
    }

    [Fact]
    public void TypeFixedArrayFieldsAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/type/fixed_array_fields.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(["0", "10", "30", "10", "20", "3"], result.Output);
    }

    [Fact]
    public void TypeFixedMultidimensionalArrayFieldsAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/type/fixed_multidimensional_array_fields.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
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

        var result = Run(sourceText, mode: ExecutionMode.Bytecode);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Equal(expectedOutput, result.Output);
    }

    [Fact]
    public void TypeMultidimensionalArrayFieldsAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/type/multidimensional_array_fields.axm");

        var result = Run(sourceText, mode: ExecutionMode.Bytecode);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Equal(["10", "20", "4", "FALSE", "9", "0"], result.Output);
    }

    [Fact]
    public void TypeArrayFieldViewsAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/type/array_field_views.axm");

        var result = Run(sourceText, mode: ExecutionMode.Bytecode);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Equal(["23", "24", "2", "4", "4", "99"], result.Output);
    }

    [Fact]
    public void TypeNamedLiteralsAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/type/named_literals.axm");

        var result = Run(sourceText, mode: ExecutionMode.Bytecode);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Equal(["Ada", "36", "Paris", "0", "0", "TRUE"], result.Output);
    }

    [Fact]
    public void TypePositionalLiteralAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/type/positional_constructor.axm");

        var result = Run(sourceText, mode: ExecutionMode.Bytecode);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
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
    [InlineData("samples/operators/sub/byref_record_argument.axm", new[] { "37" })]
    [InlineData("samples/operators/sub/byval_record_copy.axm", new[] { "Ada", "36" })]
    public void TypeDefaultAndEqualityAcceptanceSamples_RunOnBytecodeVm(string relativePath, string[] expectedOutput)
    {
        var sourceText = TestProgramLoader.LoadText(relativePath);

        var result = Run(sourceText, mode: ExecutionMode.Bytecode);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Equal(expectedOutput, result.Output);
    }

    [Fact]
    public void TypeConstFixedArrayFieldsAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/type/const_fixed_array_fields.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(["0", "3", "30"], result.Output);
    }

    [Fact]
    public void TypeConstFixedArrayFieldsForwardAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/type/const_fixed_array_fields_forward.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(["0", "3", "30"], result.Output);
    }

    [Fact]
    public void TypeFixedStringFieldsAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/type/fixed_string_fields.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(["[     ]", "[Ada  ]", "[Analyst ]", "[Architec]"], result.Output);
    }

    [Fact]
    public void DimFixedStringAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/dim/fixed_string.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
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
        Assert.Empty(result.Diagnostics);
        Assert.Equal(["", "", "0", "Ada", "Analyst", "99", "20", "7"], result.Output);
    }

    [Fact]
    public void TryCatchAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/try/handled_runtime_error.axm");

        var result = Run(sourceText, mode: ExecutionMode.Bytecode);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(["before", "caught", "MB3001", "4", "Деление на ноль.", "cleanup", "after"], result.Output);
    }

    [Fact]
    public void ErrorAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/error/user_code_catch.axm");

        var result = Run(sourceText, mode: ExecutionMode.Bytecode);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(["APP001", "Manual failure", "after"], result.Output);
    }

    [Fact]
    public void ErrorRethrowAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/error/rethrow_caught_error.axm");

        var result = Run(sourceText, mode: ExecutionMode.Bytecode);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(["inner", "APP003", "Original failure", "4"], result.Output);
    }

    [Fact]
    public void InKeyAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/functions/inkey/poll_keys.axm");

        var result = Run(
            sourceText,
            inputProvider: new QueueKeyboardInputProvider("LEFT"),
            mode: ExecutionMode.Bytecode);

        Assert.True(
            result.Status == ExecutionStatus.Success,
            string.Join(" | ", result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}@{diagnostic.Location.Line}:{diagnostic.Location.Column}")));
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
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
            inputProvider: new QueueKeyboardInputProvider("SPACE"),
            mode: ExecutionMode.Bytecode);

        Assert.True(
            result.Status == ExecutionStatus.Success,
            string.Join(" | ", result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}@{diagnostic.Location.Line}:{diagnostic.Location.Column}")));
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
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
            inputProvider: new QueueKeyboardInputProvider("LEFT", "ESC"),
            mode: ExecutionMode.Bytecode);

        Assert.True(
            result.Status == ExecutionStatus.Success,
            string.Join(" | ", result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}@{diagnostic.Location.Line}:{diagnostic.Location.Column}")));
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
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
            inputProvider: new QueueKeyboardInputProvider("RIGHT", "ESC"),
            mode: ExecutionMode.Bytecode);

        Assert.True(
            result.Status == ExecutionStatus.Success,
            string.Join(" | ", result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}@{diagnostic.Location.Line}:{diagnostic.Location.Column}")));
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
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
            inputProvider: new QueueKeyboardInputProvider("LEFT", "ESC"),
            mode: ExecutionMode.Bytecode);

        Assert.True(
            result.Status == ExecutionStatus.Success,
            string.Join(" | ", result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}@{diagnostic.Location.Line}:{diagnostic.Location.Column}")));
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
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
            inputProvider: new QueueKeyboardInputProvider(keys: ["LEFT", "ESC"], pressedKeys: ["CTRL", "LEFT"]),
            mode: ExecutionMode.Bytecode);

        Assert.True(
            result.Status == ExecutionStatus.Success,
            string.Join(" | ", result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}@{diagnostic.Location.Line}:{diagnostic.Location.Column}")));
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
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
            inputProvider: new QueueKeyboardInputProvider("", "", "LEFT", "LEFT", "ESC"),
            mode: ExecutionMode.Bytecode);

        Assert.True(
            result.Status == ExecutionStatus.Success,
            string.Join(" | ", result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}@{diagnostic.Location.Line}:{diagnostic.Location.Column}")));
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
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
            inputProvider: new QueueKeyboardInputProvider(keys: [], pressedKeys: ["CTRL", "LEFT"]),
            mode: ExecutionMode.Bytecode);

        Assert.True(
            result.Status == ExecutionStatus.Success,
            string.Join(" | ", result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}@{diagnostic.Location.Line}:{diagnostic.Location.Column}")));
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
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
            inputProvider: new QueueKeyboardInputProvider(keys: [], pressedKeys: ["CTRL", "PAGEUP"]),
            mode: ExecutionMode.Bytecode);

        Assert.True(
            result.Status == ExecutionStatus.Success,
            string.Join(" | ", result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}@{diagnostic.Location.Line}:{diagnostic.Location.Column}")));
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
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
            inputProvider: new QueueKeyboardInputProvider("LEFT"),
            mode: ExecutionMode.Bytecode);

        Assert.True(
            result.Status == ExecutionStatus.Success,
            string.Join(" | ", result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}@{diagnostic.Location.Line}:{diagnostic.Location.Column}")));
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
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
            inputProvider: new QueueKeyboardInputProvider(keys: ["LEFT"], pressedKeys: ["CTRL", "LEFT"]),
            mode: ExecutionMode.Bytecode);

        Assert.True(
            result.Status == ExecutionStatus.Success,
            string.Join(" | ", result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}@{diagnostic.Location.Line}:{diagnostic.Location.Column}")));
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
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
            inputProvider: inputProvider,
            mode: ExecutionMode.Bytecode);

        Assert.True(
            result.Status == ExecutionStatus.Success,
            string.Join(" | ", result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}@{diagnostic.Location.Line}:{diagnostic.Location.Column}")));
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Empty(result.Output);
        Assert.Equal(
            [
                "320",
                "240",
                "done"
            ],
            inputProvider.GraphicsOutput);
    }

    [Fact]
    public void MouseButtonAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/functions/mousebutton/button_states.axm");
        var inputProvider = new QueueKeyboardInputProvider(keys: [], pressedKeys: [], mouseX: 320, mouseY: 240, pressedMouseButtons: [1, 3]);

        var result = Run(
            sourceText,
            inputProvider: inputProvider,
            mode: ExecutionMode.Bytecode);

        Assert.True(
            result.Status == ExecutionStatus.Success,
            string.Join(" | ", result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}@{diagnostic.Location.Line}:{diagnostic.Location.Column}")));
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Empty(result.Output);
        Assert.Equal(
            [
                "TRUE",
                "FALSE",
                "TRUE",
                "FALSE",
                "done"
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
            inputProvider: inputProvider,
            mode: ExecutionMode.Bytecode);

        Assert.True(
            result.Status == ExecutionStatus.Success,
            string.Join(" | ", result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}@{diagnostic.Location.Line}:{diagnostic.Location.Column}")));
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Empty(result.Output);
        Assert.Equal(
            [
                "TRUE",
                "FALSE",
                "TRUE",
                "done"
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
            inputProvider: inputProvider,
            mode: ExecutionMode.Bytecode);

        Assert.True(
            result.Status == ExecutionStatus.Success,
            string.Join(" | ", result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}@{diagnostic.Location.Line}:{diagnostic.Location.Column}")));
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Empty(result.Output);
        Assert.Equal(
            [
                "TRUE",
                "1",
                "320",
                "240",
                "done"
            ],
            inputProvider.GraphicsOutput);
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
            inputProvider: inputProvider,
            mode: ExecutionMode.Bytecode);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
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
                "done"
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
            inputProvider: inputProvider,
            mode: ExecutionMode.Bytecode);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Empty(result.Output);
        Assert.Equal(
            [
                "TRUE",
                "-120",
                "320",
                "240",
                "done"
            ],
            inputProvider.GraphicsOutput);
    }

    [Fact]
    public void ScreenModeInfoAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/graphics/screen/mode_info.axm");
        var inputProvider = new QueueKeyboardInputProvider(keys: ["SPACE", "SPACE", "ESC"], pressedKeys: []);

        var result = Run(sourceText, inputProvider, ExecutionMode.Bytecode);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Empty(result.Output);
        Assert.Equal(["text mode screen"], inputProvider.GraphicsOutput);
    }

    [Fact]
    public void Screen10BestFitAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/graphics/screen/best_fit_dimensions.axm");
        var inputProvider = new QueueKeyboardInputProvider(keys: [], pressedKeys: []);

        var result = Run(sourceText, inputProvider, ExecutionMode.Bytecode);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
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

        var result = Run(sourceText, inputProvider, ExecutionMode.Bytecode);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
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

        var result = Run(sourceText, inputProvider, ExecutionMode.Bytecode);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Empty(result.Output);
        Assert.Empty(inputProvider.GraphicsOutput);
    }

    [Fact]
    public void ScreenTransparentTextAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/graphics/text/transparent_text.axm");
        var inputProvider = new QueueKeyboardInputProvider(keys: ["SPACE", "SPACE", "ESC"], pressedKeys: []);

        var result = Run(sourceText, inputProvider, ExecutionMode.Bytecode);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Empty(result.Output);
        Assert.Equal(["Transparent text sample finished"], inputProvider.GraphicsOutput);
    }

    [Fact]
    public void ScreenInputAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/graphics/input/input_on_screen.axm");
        var inputProvider = new QueueKeyboardInputProvider(keys: ["ESC"], pressedKeys: [], inputs: ["Ada"]);

        var result = Run(sourceText, inputProvider, ExecutionMode.Bytecode);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Empty(result.Output);
        Assert.Equal(["Input sample finished"], inputProvider.GraphicsOutput);
    }

    [Fact]
    public void ScreenZeroTextInputAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/graphics/input/text_screen_input.axm");
        var inputProvider = new QueueKeyboardInputProvider(keys: [], pressedKeys: [], inputs: ["Ada", "7"]);

        var result = Run(sourceText, inputProvider, ExecutionMode.Bytecode);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Empty(result.Output);
        Assert.Equal(["Ваше имя? Ada", "Ваш возраст? 7", "Привет, Ada тебе 7 лет."], inputProvider.GraphicsOutput);
    }

    [Fact]
    public void ScreenPrimitivesAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/graphics/primitives/pset_line_circle.axm");
        var inputProvider = new QueueKeyboardInputProvider(keys: ["SPACE", "ESC"], pressedKeys: []);

        var result = Run(sourceText, inputProvider, ExecutionMode.Bytecode);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Empty(result.Output);
        Assert.Equal(["Graphics primitives sample finished"], inputProvider.GraphicsOutput);
    }

    [Fact]
    public void ScreenCircleFullFormsAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/graphics/primitives/circle_arcs_step_aspect.axm");
        var inputProvider = new QueueKeyboardInputProvider(keys: ["ESC"], pressedKeys: []);

        var result = Run(sourceText, inputProvider, ExecutionMode.Bytecode);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Empty(result.Output);
        Assert.Equal(["Circle full forms sample finished"], inputProvider.GraphicsOutput);
    }

    [Fact]
    public void ScreenCircleFullTurnAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/graphics/primitives/circle_full_turn.axm");
        var inputProvider = new QueueKeyboardInputProvider(keys: ["ESC"], pressedKeys: []);

        var result = Run(sourceText, inputProvider, ExecutionMode.Bytecode);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Empty(result.Output);
        Assert.Equal(["Circle full turn sample finished"], inputProvider.GraphicsOutput);
    }

    [Fact]
    public void ScreenStepAndPointPositionAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/graphics/primitives/step_and_point_position.axm");
        var inputProvider = new QueueKeyboardInputProvider(keys: ["ESC"], pressedKeys: []);

        var result = Run(sourceText, inputProvider, ExecutionMode.Bytecode);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Empty(result.Output);
        Assert.Equal(["STEP and POINT position sample finished"], inputProvider.GraphicsOutput);
    }

    [Fact]
    public void ScreenViewAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/view/screen_and_relative.axm");
        var inputProvider = new QueueKeyboardInputProvider(keys: ["ESC"], pressedKeys: []);

        var result = Run(sourceText, inputProvider, ExecutionMode.Bytecode);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
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

        var result = Run(sourceText, inputProvider, ExecutionMode.Bytecode);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Empty(result.Output);
        Assert.Contains("VIEW PRINT sample finished", inputProvider.GraphicsOutput);
    }

    [Fact]
    public void ScreenWindowAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/window/cartesian_and_screen.axm");
        var inputProvider = new QueueKeyboardInputProvider(keys: ["ESC"], pressedKeys: []);

        var result = Run(sourceText, inputProvider, ExecutionMode.Bytecode);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
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

        var result = Run(sourceText, inputProvider, ExecutionMode.Bytecode);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
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

        var result = Run(sourceText, inputProvider, ExecutionMode.Bytecode);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
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

        var result = Run(sourceText, inputProvider, ExecutionMode.Bytecode);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
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

        var result = Run(sourceText, inputProvider, ExecutionMode.Bytecode);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
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

        var result = Run(sourceText, inputProvider, ExecutionMode.Bytecode);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Empty(result.Output);
        Assert.Equal(["6", "9", "12", "15", "GET sample finished"], inputProvider.GraphicsOutput);
    }

    [Fact]
    public void PutAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/put/default_xor_and_preset.axm");
        var inputProvider = new QueueKeyboardInputProvider(keys: [], pressedKeys: []);

        var result = Run(sourceText, inputProvider, ExecutionMode.Bytecode);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Empty(result.Output);
        Assert.Equal(["1", "2", "254", "253", "PUT sample finished"], inputProvider.GraphicsOutput);
    }

    [Fact]
    public void ScreenPaintAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/paint/pattern_repaint.axm");
        var inputProvider = new QueueKeyboardInputProvider(keys: ["ESC"], pressedKeys: []);

        var result = Run(sourceText, inputProvider, ExecutionMode.Bytecode);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Empty(result.Output);
        Assert.Equal(["PAINT pattern sample finished"], inputProvider.GraphicsOutput);
    }

    [Fact]
    public void ScreenPaintStepAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/operators/paint/step_default_fill.axm");
        var inputProvider = new QueueKeyboardInputProvider(keys: ["ESC"], pressedKeys: []);

        var result = Run(sourceText, inputProvider, ExecutionMode.Bytecode);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Empty(result.Output);
        Assert.Equal(["PAINT STEP sample finished"], inputProvider.GraphicsOutput);
    }

[Theory]
[InlineData("samples/operators/view/invalid_bounds_error.axm", "MB3007")]
[InlineData("samples/operators/window/zero_size_error.axm", "MB3007")]
[InlineData("samples/operators/paint/empty_pattern_error.axm", "MB3007")]
[InlineData("samples/operators/view-print/invalid_rows_error.axm", "MB3007")]
[InlineData("samples/functions/pmap/invalid_selector_error.axm", "MB3007")]
[InlineData("samples/graphics/screen/invalid_page_error.axm", "MB3007")]
[InlineData("samples/operators/pcopy/invalid_page_error.axm", "MB3007")]
[InlineData("samples/operators/pcopy/text_screen_error.axm", "MB3010")]
public void GraphicsNegativeAcceptanceSamples_HaveStableDiagnosticParity(string relativePath, string expectedCode)
{
        var sourceText = TestProgramLoader.LoadText(relativePath);
        var bytecodeInputProvider = new QueueKeyboardInputProvider(keys: [], pressedKeys: []);
        var interpreterInputProvider = new QueueKeyboardInputProvider(keys: [], pressedKeys: []);

        var bytecodeResult = Run(sourceText, bytecodeInputProvider, ExecutionMode.Bytecode);
        var interpreterResult = Run(sourceText, interpreterInputProvider, ExecutionMode.Interpreter);

        Assert.Equal(ExecutionStatus.RuntimeFailure, bytecodeResult.Status);
        Assert.Equal(ExecutionStatus.RuntimeFailure, interpreterResult.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, bytecodeResult.ActualEngine);
        Assert.Equal(ExecutionEngineKind.Interpreter, interpreterResult.ActualEngine);
        Assert.False(bytecodeResult.UsedInterpreterFallback);
        Assert.False(interpreterResult.UsedInterpreterFallback);

        var bytecodeDiagnostic = Assert.Single(bytecodeResult.Diagnostics);
        var interpreterDiagnostic = Assert.Single(interpreterResult.Diagnostics);
        Assert.Equal(expectedCode, bytecodeDiagnostic.Code);
        Assert.Equal(expectedCode, interpreterDiagnostic.Code);
        Assert.True(bytecodeDiagnostic.Location.Line > 0);
        Assert.True(bytecodeDiagnostic.Location.Column > 0);
        Assert.True(interpreterDiagnostic.Location.Line > 0);
        Assert.True(interpreterDiagnostic.Location.Column > 0);
    }

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

        var result = Run(sourceText, inputProvider, ExecutionMode.Bytecode);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
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

        var result = Run(sourceText, inputProvider, ExecutionMode.Bytecode);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Empty(result.Output);
        Assert.Equal(["Mouse sample finished"], inputProvider.GraphicsOutput);
    }

    [Fact]
    public void ScreenGameAcceptanceSample_RunsOnBytecodeVm()
    {
        var sourceText = TestProgramLoader.LoadText("samples/graphics/game/paddle_ball.axm");
        var inputProvider = new QueueKeyboardInputProvider(keys: ["ESC"], pressedKeys: []);

        var result = Run(sourceText, inputProvider, ExecutionMode.Bytecode);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Empty(result.Output);
        Assert.Equal(["Paddle ball sample finished: esc"], inputProvider.GraphicsOutput);
    }

    [Fact]
    public void ReadAllLinesUtf8BomAcceptanceSample_RunsSuccessfully()
    {
        var sourceText = TestProgramLoader.LoadText("samples/functions/readalllines/utf8_bom.axm");

        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
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
        Assert.Empty(result.Diagnostics);
        Assert.Equal(expectedOutput, result.Output);
    }

    [Theory]
    [InlineData("samples/functions/ascw/unicode_codepoint.axm", "1071", "9608", "128512")]
    [InlineData("samples/functions/chrw/unicode_character.axm", "Я", "█", "128512")]
    public void UnicodeCharacterFunctionAcceptanceSamples_RunOnBytecodeVm(string relativePath, params string[] expectedOutput)
    {
        var sourceText = TestProgramLoader.LoadText(relativePath);

        var result = Run(sourceText, mode: ExecutionMode.Bytecode);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
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
        Assert.Empty(result.Diagnostics);
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
        Assert.Empty(result.Diagnostics);
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
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
    }

    public static IEnumerable<object[]> PositiveAcceptanceSamplePaths()
    {
        var samplesRoot = Path.Combine(FindRepositoryRoot(), "samples");

        return Directory
            .EnumerateFiles(samplesRoot, "*.axm", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(FindRepositoryRoot(), path).Replace(Path.DirectorySeparatorChar, '/'))
            .Where(IsPositiveAcceptanceSample)
            .Order(StringComparer.OrdinalIgnoreCase)
            .Select(static relativePath => new object[] { relativePath });
    }

    private static bool IsPositiveAcceptanceSample(string relativePath)
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

        return true;
    }

    private static ExecutionResult RunAuditSample(string sourceText, string relativePath)
    {
        lock (WorkingDirectoryLock)
        {
            var originalDirectory = Environment.CurrentDirectory;
            var repositoryRoot = FindRepositoryRoot();

            try
            {
                Environment.CurrentDirectory = repositoryRoot;

                return Run(sourceText, inputProvider: CreateAuditInputProvider(relativePath), mode: ExecutionMode.Bytecode);
            }
            finally
            {
                Environment.CurrentDirectory = originalDirectory;

                if (relativePath.Equals("samples/smoke/repair_plans_mojibake.axm", StringComparison.OrdinalIgnoreCase))
                {
                    File.Delete(Path.Combine(repositoryRoot, "README_copy.md"));
                }
            }
        }
    }

    private static IInputProvider CreateAuditInputProvider(string relativePath)
    {
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

        if (relativePath.Equals("samples/operators/palette/change_and_reset_colors.axm", StringComparison.OrdinalIgnoreCase))
        {
            return new QueueKeyboardInputProvider(keys: ["SPACE", "SPACE", "ESC"], pressedKeys: []);
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

        if (relativePath.Equals("samples/smoke/engine-compare/input_record_intake.axm", StringComparison.OrdinalIgnoreCase))
        {
            return new QueueInputProvider(CreateInputRecordIntakeLines());
        }

        return new QueueInputProvider("Ника", "7", "TRUE");
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
        IInputProvider? inputProvider = null,
        ExecutionMode mode = ExecutionMode.Bytecode)
    {
        var pipeline = new AxiomExecutionPipeline();
        return pipeline.Run(new ExecutionRequest(sourceText, inputProvider, mode: mode));
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
        private readonly Dictionary<(long X, long Y), int> _pixels = [];
        private readonly Dictionary<int, Dictionary<(long X, long Y), int>> _screenPages = [];
        private bool _isScreenActive;
        private bool _isScreenOutputActive;
        private int _screenMode;
        private int _activeScreenPage;
        private int _visualScreenPage;
        private int _screenWidth;
        private int _screenHeight;
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
            IEnumerable<string>? inputs = null)
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
        }

        public List<string> GraphicsOutput { get; } = [];

        public string ReadInput(InputRequest request) => _inputs.Count > 0 ? _inputs.Dequeue() : string.Empty;

        public string ReadKey() => _keys.Count > 0 ? _keys.Dequeue() : string.Empty;

        public bool IsKeyDown(string keyText) => _pressedKeys.Contains(KeyboardInputNormalizer.Normalize(keyText));

        public long GetMouseX() => _mouseX;

        public long GetMouseY() => _mouseY;

        public bool IsMouseButtonDown(int buttonNumber) => _pressedMouseButtons.Contains(buttonNumber);

        public int ReadMouseButtonPress() => _mousePresses.Count > 0 ? _mousePresses.Dequeue() : 0;

        public MouseInputEvent ReadMouseEvent() => _mouseEvents.Count > 0 ? _mouseEvents.Dequeue() : default;

        public bool IsScreenActive => _isScreenActive;

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

        public void DeactivateScreen()
        {
            _isScreenActive = false;
            _isScreenOutputActive = false;
            _screenMode = 0;
            _activeScreenPage = 0;
            _visualScreenPage = 0;
            _screenWidth = 0;
            _screenHeight = 0;
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
            if (!_isScreenActive || left > right || top > bottom)
            {
                return false;
            }

            var baseLeft = _viewportEnabled ? _viewportLeft : 0L;
            var baseTop = _viewportEnabled ? _viewportTop : 0L;
            var absoluteLeft = useScreenCoordinates ? left : GraphicsLineMath.SaturatingAdd(baseLeft, left);
            var absoluteTop = useScreenCoordinates ? top : GraphicsLineMath.SaturatingAdd(baseTop, top);
            var absoluteRight = useScreenCoordinates ? right : GraphicsLineMath.SaturatingAdd(baseLeft, right);
            var absoluteBottom = useScreenCoordinates ? bottom : GraphicsLineMath.SaturatingAdd(baseTop, bottom);
            if (!IsInsideScreen(absoluteLeft, absoluteTop) || !IsInsideScreen(absoluteRight, absoluteBottom))
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

        private bool IsInsideScreen(long x, long y) => x >= 0 && y >= 0 && x < _screenWidth && y < _screenHeight;
    }
}

