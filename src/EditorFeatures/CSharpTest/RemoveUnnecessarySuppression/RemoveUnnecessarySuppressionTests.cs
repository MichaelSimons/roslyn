﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.RemoveUnnecessarySuppression;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.RemoveUnnecessarySuppression
{
    using VerifyCS = CSharpCodeFixVerifier<CSharpRemoveUnnecessarySuppressionDiagnosticAnalyzer, CSharpRemoveUnnecessarySuppressionCodeFixProvider>;

    public class RemoveUnnecessarySuppressionTests
    {
        [Fact, WorkItem(44872, "https://github.com/dotnet/roslyn/issues/44872")]
        public async Task TestRemoveWithIsExpression1()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"
class C
{
    void M(object o)
    {
        if (o [|!|]is string)
        {
        }
    }
}",
@"
class C
{
    void M(object o)
    {
        if (o is string)
        {
        }
    }
}");
        }

        [Fact, WorkItem(44872, "https://github.com/dotnet/roslyn/issues/44872")]
        public async Task TestRemoveWithIsPattern1()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"
class C
{
    void M(object o)
    {
        if (o [|!|]is string s)
        {
        }
    }
}",
@"
class C
{
    void M(object o)
    {
        if (o is string s)
        {
        }
    }
}");
        }

        [Fact, WorkItem(44872, "https://github.com/dotnet/roslyn/issues/44872")]
        public async Task TestNegateWithIsExpression_CSharp8()
        {
            await new VerifyCS.Test
            {
                TestCode =
@"
class C
{
    void M(object o)
    {
        if (o [|!|]is string)
        {
        }
    }
}",
                FixedCode =
@"
class C
{
    void M(object o)
    {
        if (!(o is string))
        {
        }
    }
}",
                CodeActionIndex = 1,
                LanguageVersion = LanguageVersion.CSharp8
            }.RunAsync();
        }

        [Fact, WorkItem(44872, "https://github.com/dotnet/roslyn/issues/44872")]
        public async Task TestNegateWithIsPattern_CSharp8()
        {
            await new VerifyCS.Test
            {
                TestCode =
@"
class C
{
    void M(object o)
    {
        if (o [|!|]is string s)
        {
        }
    }
}",
                FixedCode =
@"
class C
{
    void M(object o)
    {
        if (!(o is string s))
        {
        }
    }
}",
                CodeActionIndex = 1,
                LanguageVersion = LanguageVersion.CSharp8,
            }.RunAsync();
        }

        [Fact, WorkItem(44872, "https://github.com/dotnet/roslyn/issues/44872")]
        public async Task TestNegateWithIsExpression_CSharp9()
        {
            await new VerifyCS.Test
            {
                TestCode =
@"
class C
{
    void M(object o)
    {
        if (o [|!|]is string)
        {
        }
    }
}",
                FixedCode =
@"
class C
{
    void M(object o)
    {
        if (o is not string)
        {
        }
    }
}",
                CodeActionIndex = 1,
                LanguageVersion = LanguageVersionExtensions.CSharp9,
            }.RunAsync();
        }

        [Fact, WorkItem(44872, "https://github.com/dotnet/roslyn/issues/44872")]
        public async Task TestNegateWithIsPattern_CSharp9()
        {
            // this will change to `if (o is not string s)` once it's legal to have declarations under a `not` pattern.
            await new VerifyCS.Test
            {
                TestCode =
@"
class C
{
    void M(object o)
    {
        if (o [|!|]is string s)
        {
        }
    }
}",
                FixedState =
                {
                    Sources =
                    {
@"
class C
{
    void M(object o)
    {
        if (!(o is string s))
        {
        }
    }
}"
                    },
                },
                CodeActionIndex = 1,
                LanguageVersion = LanguageVersionExtensions.CSharp9,
            }.RunAsync();
        }
    }
}
