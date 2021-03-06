using System;
using NUnit.Framework;
using System.Threading;
using System.Linq;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using RefactoringEssentials.Tests.CSharp.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CodeActions;

namespace RefactoringEssentials.Tests.CSharp.CodeRefactorings
{
    public abstract class CSharpCodeRefactoringTestBase : CodeRefactoringTestBase
    {
        public void Test<T>(string input, string output, int action = 0, bool expectErrors = false, CSharpParseOptions parseOptions = null)
            where T : CodeRefactoringProvider, new()
        {
            Test(new T(), input, output, action, expectErrors, parseOptions);
        }

        public void Test(CodeRefactoringProvider provider, string input, string output, int action = 0, bool expectErrors = false, CSharpParseOptions parseOptions = null)
        {
            string result = HomogenizeEol(RunContextAction(provider, HomogenizeEol(input), action, expectErrors, parseOptions));
            output = HomogenizeEol(output);
            bool passed = result == output;
            if (!passed)
            {
                Console.WriteLine("-----------Expected:");
                Console.WriteLine(output);
                Console.WriteLine("-----------Got:");
                Console.WriteLine(result);
            }
            Assert.AreEqual(output, result);
        }

        internal static List<Microsoft.CodeAnalysis.CodeActions.CodeAction> GetActions<T>(string input) where T : CodeRefactoringProvider, new()
        {
            CSharpDiagnosticTestBase.TestWorkspace workspace;
            Document doc;
            return GetActions(new T(), input, out workspace, out doc);
        }

        static List<CodeAction> GetActions(CodeRefactoringProvider action, string input, out CSharpDiagnosticTestBase.TestWorkspace workspace, out Document doc, CSharpParseOptions parseOptions = null)
        {
            TextSpan selectedSpan;
            TextSpan markedSpan;
            string text = ParseText(input, out selectedSpan, out markedSpan);
            workspace = new CSharpDiagnosticTestBase.TestWorkspace();
            var projectId = ProjectId.CreateNewId();
            var documentId = DocumentId.CreateNewId(projectId);
            if (parseOptions == null)
            {
                parseOptions = new CSharpParseOptions(
                    LanguageVersion.CSharp6,
                    DocumentationMode.Diagnose | DocumentationMode.Parse,
                    SourceCodeKind.Regular,
                    ImmutableArray.Create("DEBUG", "TEST")
                );
            }
            workspace.Options.WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInControlBlocks, false);
            workspace.Open(ProjectInfo.Create(
                projectId,
                VersionStamp.Create(),
                "TestProject",
                "TestProject",
                LanguageNames.CSharp,
                null,
                null,
                new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
                    "",
                    "",
                    "Script",
                    null,
                    OptimizationLevel.Debug,
                    false,
                    true
                ),
                parseOptions,
                new[] {
                    DocumentInfo.Create(
                        documentId,
                        "a.cs",
                        null,
                        SourceCodeKind.Regular,
                        TextLoader.From(TextAndVersion.Create(SourceText.From(text), VersionStamp.Create()))
                    )
                },
                null,
                DiagnosticTestBase.DefaultMetadataReferences
            )
            );
            doc = workspace.CurrentSolution.GetProject(projectId).GetDocument(documentId);
            var actions = new List<CodeAction>();
            var context = new CodeRefactoringContext(doc, selectedSpan, actions.Add, default(CancellationToken));
            action.ComputeRefactoringsAsync(context).Wait();
            if (markedSpan.Start > 0)
            {
                foreach (var nra in actions.OfType<NRefactoryCodeAction>())
                {
                    Assert.AreEqual(markedSpan, nra.TextSpan, "Activation span does not match.");
                }
            }
            return actions;
        }

        protected string RunContextAction(CodeRefactoringProvider action, string input, int actionIndex = 0, bool expectErrors = false, CSharpParseOptions parseOptions = null)
        {
            Document doc;
            CSharpDiagnosticTestBase.TestWorkspace workspace;
            var actions = GetActions(action, input, out workspace, out doc, parseOptions);
            if (actions.Count < actionIndex)
                Console.WriteLine("invalid input is:" + input);
            var a = actions[actionIndex];
            foreach (var op in a.GetOperationsAsync(default(CancellationToken)).Result)
            {
                op.Apply(workspace, default(CancellationToken));
            }
            return workspace.CurrentSolution.GetDocument(doc.Id).GetTextAsync().Result.ToString();
        }


        protected void TestWrongContext(CodeRefactoringProvider action, string input)
        {
            Document doc;
            RefactoringEssentials.Tests.CSharp.Diagnostics.CSharpDiagnosticTestBase.TestWorkspace workspace;
            var actions = GetActions(action, input, out workspace, out doc);
            Assert.IsTrue(actions == null || actions.Count == 0, action.GetType() + " shouldn't be valid there.");
        }


        protected void TestWrongContext<T>(string input) where T : CodeRefactoringProvider, new()
        {
            TestWrongContext(new T(), input);
        }

        //		protected List<CodeAction> GetActions<T> (string input) where T : CodeActionProvider, new ()
        //		{
        //			var ctx = TestRefactoringContext.Create(input);
        //			ctx.FormattingOptions = formattingOptions;
        //			return new T().GetActions(ctx).ToList();
        //		}
        //
        //		protected void TestActionDescriptions (CodeActionProvider provider, string input, params string[] expected)
        //		{
        //			var ctx = TestRefactoringContext.Create(input);
        //			ctx.FormattingOptions = formattingOptions;
        //			var actions = provider.GetActions(ctx).ToList();
        //			Assert.AreEqual(
        //				expected,
        //				actions.Select(a => a.Description).ToArray());
        //		}
    }
}
