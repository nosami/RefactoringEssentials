using NUnit.Framework;
using RefactoringEssentials.CSharp.CodeRefactorings;

namespace RefactoringEssentials.Tests.CSharp.CodeRefactorings
{
    [TestFixture]
    public class AddArgumentNameTests : CSharpCodeRefactoringTestBase
    {
        [Test]
        public void MethodInvocation1()
        {
            Test<AddNameToArgumentCodeRefactoringProvider>(@"
class TestClass
{
    public void Foo(int a, int b, float c = 0.1) { }
    public void F()
    {
        Foo($1,b: 2);
    }
}", @"
class TestClass
{
    public void Foo(int a, int b, float c = 0.1) { }
    public void F()
    {
        Foo(a: 1, b: 2);
    }
}");
        }

        [Test]
        public void MethodInvocation1WithComment()
        {
            Test<AddNameToArgumentCodeRefactoringProvider>(@"
class TestClass
{
    public void Foo(int a, int b, float c = 0.1) { }
    public void F()
    {
        // Some comment
        Foo($1,b: 2);
    }
}", @"
class TestClass
{
    public void Foo(int a, int b, float c = 0.1) { }
    public void F()
    {
        // Some comment
        Foo(a: 1, b: 2);
    }
}");
        }

        [Test]
        public void MethodInvocation2()
        {
            Test<AddNameToArgumentCodeRefactoringProvider>(@"
class TestClass
{
    public void Foo(int a, int b, float c = 0.1) { }
    public void F()
    {
        Foo($1, 2);
    }
}", @"
class TestClass
{
    public void Foo(int a, int b, float c = 0.1) { }
    public void F()
    {
        Foo(a: 1, b: 2);
    }
}");
        }

        [Test]
        public void AttributeUsage()
        {
            Test<AddNameToArgumentCodeRefactoringProvider>(@"
using System;
public class AnyClass
{
    [Obsolete(…"" "", error: true)]
    static void Old() { }
}", @"
using System;
public class AnyClass
{
    [Obsolete(message: "" "", error: true)]
    static void Old() { }
}");
        }

        [Test]
        public void AttributeUsageWithComment()
        {
            Test<AddNameToArgumentCodeRefactoringProvider>(@"
using System;
public class AnyClass
{
    // Some comment
    [Obsolete(…"" "", error: true)]
    static void Old() { }
}", @"
using System;
public class AnyClass
{
    // Some comment
    [Obsolete(message: "" "", error: true)]
    static void Old() { }
}");
        }

        [Test]
        public void AttributeNamedArgument()
        {
            Test<AddNameToArgumentCodeRefactoringProvider>(@"
class MyAttribute : System.Attribute
{
    public string Name1 { get; set; }
    public string Name2 { get; set; }
    public string Name3 { get; set; }
    private int foo;

    public MyAttribute(int foo)
    {
        this.foo = foo;
    }
}


[My($1, Name1 = """", Name2 = """")]
public class Test
{
}
", @"
class MyAttribute : System.Attribute
{
    public string Name1 { get; set; }
    public string Name2 { get; set; }
    public string Name3 { get; set; }
    private int foo;

    public MyAttribute(int foo)
    {
        this.foo = foo;
    }
}


[My(foo: 1, Name1 = """", Name2 = """")]
public class Test
{
}
");
        }

        [Test]
        public void AttributeNamedArgumentInvalidCase()
        {
            TestWrongContext<AddNameToArgumentCodeRefactoringProvider>(@"
class MyAttribute : System.Attribute
{
    public string Name1 { get; set; }
    public string Name2 { get; set; }
    public string Name3 { get; set; }
    private int foo;

    public MyAttribute(int foo)
    {
        this.foo = foo;
    }
}


[My(1, $Name1 = """", Name2 = """")]
public class Test
{
}
");
        }

        [Test]
        public void IndexerInvocation()
        {
            Test<AddNameToArgumentCodeRefactoringProvider>(@"
public class TestClass
{
    public int this[int i, int j]
    {
        set { }
        get { return 0; }
    }
}
internal class Test
{
    private void Foo()
    {
        var TestBases = new TestClass();
        int a = TestBases[$1, 2];
    }
}", @"
public class TestClass
{
    public int this[int i, int j]
    {
        set { }
        get { return 0; }
    }
}
internal class Test
{
    private void Foo()
    {
        var TestBases = new TestClass();
        int a = TestBases[i: 1, j: 2];
    }
}");
        }

        [Test]
        public void TestParamsInvalidContext()
        {

            TestWrongContext<AddNameToArgumentCodeRefactoringProvider>(@"
class TestClass
{
    public void F()
    {
        System.Console.WriteLine (""foo"", 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, $12);
    }
}");
        }

    }
}