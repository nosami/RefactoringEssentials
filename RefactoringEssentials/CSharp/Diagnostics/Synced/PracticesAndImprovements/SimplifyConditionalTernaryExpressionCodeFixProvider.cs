using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Formatting;

namespace RefactoringEssentials.CSharp.Diagnostics
{
    [ExportCodeFixProvider(LanguageNames.CSharp), System.Composition.Shared]
    public class SimplifyConditionalTernaryExpressionCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds
        {
            get
            {
                return ImmutableArray.Create(CSharpDiagnosticIDs.SimplifyConditionalTernaryExpressionAnalyzerID);
            }
        }

        public override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        internal static bool? GetBool(ExpressionSyntax trueExpression)
        {
            var pExpr = trueExpression as LiteralExpressionSyntax;
            if (pExpr == null || !(pExpr.Token.Value is bool))
                return null;
            return (bool)pExpr.Token.Value;
        }

        public async override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var cancellationToken = context.CancellationToken;
            var span = context.Span;
            var diagnostics = context.Diagnostics;
            var root = await document.GetSyntaxRootAsync(cancellationToken);

            var diagnostic = diagnostics.First();
            var node = root.FindNode(context.Span, getInnermostNodeForTie: true) as ConditionalExpressionSyntax;
            var newRoot = root;

            bool? trueBranch = GetBool(node.WhenTrue.SkipParens());
            bool? falseBranch = GetBool(node.WhenFalse.SkipParens());

            if (trueBranch == false && falseBranch == true)
            {
                newRoot = newRoot.ReplaceNode(node, CSharpUtil.InvertCondition(node.Condition).WithAdditionalAnnotations(Formatter.Annotation));
            }
            else if (trueBranch == true)
            {
                newRoot = newRoot.ReplaceNode(
                    (SyntaxNode)node,
                    SyntaxFactory.BinaryExpression(
                        SyntaxKind.LogicalOrExpression,
                        node.Condition,
                        SyntaxFactory.ParseToken(" || "),
                        node.WhenFalse
                    ).WithAdditionalAnnotations(Formatter.Annotation)
                );
            }
            else if (trueBranch == false)
            {
                newRoot = newRoot.ReplaceNode(
                    (SyntaxNode)node,
                    SyntaxFactory.BinaryExpression(
                        SyntaxKind.LogicalAndExpression,
                        CSharpUtil.InvertCondition(node.Condition),
                        SyntaxFactory.ParseToken(" && "),
                        node.WhenFalse
                    ).WithAdditionalAnnotations(Formatter.Annotation)
                );
            }
            else if (falseBranch == true)
            {
                newRoot = newRoot.ReplaceNode(
                    (SyntaxNode)node,
                    SyntaxFactory.BinaryExpression(
                        SyntaxKind.LogicalOrExpression,
                        CSharpUtil.InvertCondition(node.Condition),
                        SyntaxFactory.ParseToken(" || "),
                        node.WhenTrue
                    ).WithAdditionalAnnotations(Formatter.Annotation)
                );
            }
            else if (falseBranch == false)
            {
                newRoot = newRoot.ReplaceNode(
                    (SyntaxNode)node,
                    SyntaxFactory.BinaryExpression(
                        SyntaxKind.LogicalAndExpression,
                        node.Condition,
                        SyntaxFactory.ParseToken(" && "),
                        node.WhenTrue
                    ).WithAdditionalAnnotations(Formatter.Annotation)
                );
            }

            context.RegisterCodeFix(CodeActionFactory.Create(node.Span, diagnostic.Severity, "Simplify conditional expression", document.WithSyntaxRoot(newRoot)), diagnostic);
        }
    }
}