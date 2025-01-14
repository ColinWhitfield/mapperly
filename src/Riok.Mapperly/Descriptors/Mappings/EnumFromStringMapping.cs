using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Riok.Mapperly.Helpers;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Riok.Mapperly.Emit.SyntaxFactoryHelper;

namespace Riok.Mapperly.Descriptors.Mappings;

/// <summary>
/// Represents a mapping from a string to an enum.
/// Uses a switch expression for performance reasons (in comparison to <see cref="Enum.Parse(System.Type,string)"/>).
/// </summary>
public class EnumFromStringMapping : MethodMapping
{
    private const string EnumClassName = "Enum";
    private const string ParseMethodName = "Parse";
    private const string IgnoreCaseSwitchDesignatedVariableName = "s";
    private const string StringEqualsMethodName = nameof(string.Equals);
    private const string StringComparisonFullName = "System.StringComparison.OrdinalIgnoreCase";

    private readonly IEnumerable<IFieldSymbol> _enumMembers;
    private readonly bool _ignoreCase;

    public EnumFromStringMapping(
        ITypeSymbol sourceType,
        ITypeSymbol targetType,
        IEnumerable<IFieldSymbol> enumMembers,
        bool ignoreCase)
        : base(sourceType, targetType)
    {
        _enumMembers = enumMembers;
        _ignoreCase = ignoreCase;
    }

    public override IEnumerable<StatementSyntax> BuildBody(ExpressionSyntax source)
    {
        // fallback switch arm: _ => (TargetType)Enum.Parse(typeof(TargetType), source, ignoreCase)
        var enumParseInvocation = Invocation(
            MemberAccess(EnumClassName, ParseMethodName),
            TypeOfExpression(IdentifierName(TargetType.ToDisplayString())), source, BooleanLiteral(_ignoreCase));
        var fallbackArm = SwitchExpressionArm(
            DiscardPattern(),
            CastExpression(IdentifierName(TargetType.ToDisplayString()), enumParseInvocation));

        // switch for each name to the enum value
        var arms = _ignoreCase
            ? _enumMembers.Select(BuildArmIgnoreCase)
            : _enumMembers.Select(BuildArm);
        arms = arms.Append(fallbackArm);

        var switchExpr = SwitchExpression(source)
            .WithArms(CommaSeparatedList(arms, true));

        yield return ReturnStatement(switchExpr);
    }

    private SwitchExpressionArmSyntax BuildArmIgnoreCase(IFieldSymbol field)
    {
        // { } s
        var pattern = RecursivePattern()
            .WithPropertyPatternClause(PropertyPatternClause())
            .WithDesignation(SingleVariableDesignation(Identifier(IgnoreCaseSwitchDesignatedVariableName)));

        // source.Value1
        var typeMemberAccess = MemberAccess(
            IdentifierName(field.ContainingType.WithNullableAnnotation(NullableAnnotation.None).ToDisplayString()),
            field.Name);

        // when s.Equals(nameof(source.Value1), StringComparison.OrdinalIgnoreCase)
        var whenClause = WhenClause(
            Invocation(
                MemberAccess(IgnoreCaseSwitchDesignatedVariableName, StringEqualsMethodName),
                NameOf(typeMemberAccess),
                IdentifierName(StringComparisonFullName)));

        // { } s when s.Equals(nameof(source.Value1), StringComparison.OrdinalIgnoreCase) => source.Value1;
        return SwitchExpressionArm(pattern, typeMemberAccess)
            .WithWhenClause(whenClause);
    }

    private SwitchExpressionArmSyntax BuildArm(IFieldSymbol field)
    {
        // nameof(source.Value1) => source.Value1;
        var typeMemberAccess = MemberAccess(
            IdentifierName(field.ContainingType.NonNullable().ToDisplayString()),
            field.Name);
        var pattern = ConstantPattern(NameOf(typeMemberAccess));
        return SwitchExpressionArm(pattern, typeMemberAccess);
    }
}
