﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Completion;
using Roslyn.Test.Utilities;
using Xunit;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders
{
    public class OperatorIndexerCompletionProviderTests : AbstractCSharpCompletionProviderTests
    {
        public OperatorIndexerCompletionProviderTests(CSharpTestWorkspaceFixture workspaceFixture) : base(workspaceFixture)
        {
        }

        internal override Type GetCompletionProviderType()
            => typeof(OperatorIndexerCompletionProvider);

        protected override string? ItemPartiallyWritten(string? expectedItemOrNull) =>
            expectedItemOrNull?.Length >= 2 && (expectedItemOrNull.StartsWith("(") || expectedItemOrNull.StartsWith("["))
            ? expectedItemOrNull.Substring(1, 1)
            : base.ItemPartiallyWritten(expectedItemOrNull);

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
        public async Task OperatorIndexerCompletionItemsShouldBePlacedLastInCompletionList()
        {
            var castCompletionItem = (await GetCompletionItemsAsync(@"
public class C
{
    public static explicit operator float(C c) => 0;
}

public class Program
{
    public void Main()
    {
        var c = new C();
        c.$$
    }
}
", SourceCodeKind.Regular)).Single();

            var completionList = new[] {
                CompletionItem.Create("SomeText0"),
                castCompletionItem,
                CompletionItem.Create("SomeText1"),
                CompletionItem.Create("\uffdcStartWith_FFDC_Identifier"), // http://www.fileformat.info/info/unicode/char/ffdc/index.htm
                CompletionItem.Create("SomeText2"),
                CompletionItem.Create("\uD884\uDF4AStartWith_3134A_Identifier"), // http://www.fileformat.info/info/unicode/char/3134a/index.htm
                CompletionItem.Create("SomeText3"),
            };
            Array.Sort(completionList);
            Assert.Collection(completionList,
                c => Assert.Equal("SomeText0", c.DisplayText),
                c => Assert.Equal("SomeText1", c.DisplayText),
                c => Assert.Equal("SomeText2", c.DisplayText),
                c => Assert.Equal("SomeText3", c.DisplayText),
                c => Assert.Equal("\uD884\uDF4AStartWith_3134A_Identifier", c.DisplayText),
                c => Assert.Equal("\uffdcStartWith_FFDC_Identifier", c.DisplayText),
                c =>
                {
                    Assert.Same(c, castCompletionItem);
                    Assert.Equal("(float)", c.DisplayText);
                    Assert.Equal("\uFFFDfloat", c.SortText);
                    Assert.Equal("float", c.FilterText);
                });
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
        public async Task ExplicitUserDefinedConversionIsSuggestedAfterDot()
        {
            await VerifyItemExistsAsync(@"
public class C
{
    public static explicit operator float(C c) => 0;
}

public class Program
{
    public void Main()
    {
        var c = new C();
        c.$$
    }
}
", "(float)");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
        public async Task ExplicitUserDefinedConversionIsSuggestedIfMemberNameIsPartiallyWritten()
        {
            await VerifyItemExistsAsync(@"
public class C
{
    public void fly() { }
    public static explicit operator float(C c) => 0;
}

public class Program
{
    public void Main()
    {
        var c = new C();
        c.fl$$
    }
}
", "(float)");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
        [InlineData("c.$$", true)]
        [InlineData("c.fl$$", true)]
        [InlineData("c.  $$", true)]
        [InlineData("c.fl  $$", false)]
        [InlineData("c.($$", false)]
        [InlineData("c$$", false)]
        [InlineData(@"""c.$$", false)]
        [InlineData("c?.$$", true)]
        [InlineData("((C)c).$$", true)]
        [InlineData("(true ? c : c).$$", true)]
        [InlineData("c.$$ var x=0;", false)]
        public async Task ExplicitUserDefinedConversionDifferentInvocations(string invocation, bool shouldSuggestConversion)
        {
            Func<string, string, Task> verifyFunc = shouldSuggestConversion
                ? (markup, expectedItem) => VerifyItemExistsAsync(markup, expectedItem)
                : (markup, expectedItem) => VerifyItemIsAbsentAsync(markup, expectedItem);

            await verifyFunc(@$"
public class C
{{
    public static explicit operator float(C c) => 0;
}}

public class Program
{{
    public void Main()
    {{
        var c = new C();
        {invocation}
    }}
}}
", "(float)");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
        [InlineData("", "(Nested1.C)", "(Nested2.C)")]
        [InlineData("using N1.Nested1;", "(C)", "(Nested2.C)")]
        [InlineData("using N1.Nested2;", "(C)", "(Nested1.C)")]
        [InlineData("using N1.Nested1;using N1.Nested2;", "(Nested1.C)", "(Nested2.C)")]
        public async Task ExplicitUserDefinedConversionTypeDisplayStringIsMinimal(string usingDirective, string displayText1, string displayText2)
        {
            var items = await GetCompletionItemsAsync(@$"
namespace N1.Nested1
{{
    public class C
    {{
    }}
}}

namespace N1.Nested2
{{
    public class C
    {{
    }}
}}
namespace N2
{{
    public class Conversion
    {{
        public static explicit operator N1.Nested1.C(Conversion _) => new N1.Nested1.C();
        public static explicit operator N1.Nested2.C(Conversion _) => new N1.Nested2.C();
    }}
}}
namespace N1
{{
    {usingDirective}
    public class Test
    {{
        public void M()
        {{
            var conversion = new N2.Conversion();
            conversion.$$
        }}
    }}
}}
", SourceCodeKind.Regular);
            Assert.Collection(items,
                i => Assert.Equal(displayText1, i.DisplayText),
                i => Assert.Equal(displayText2, i.DisplayText));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
        public async Task ExplicitUserDefinedConversionIsSuggestedForAllExplicitConversionsToOtherTypesAndNotForImplicitConversions()
        {
            var items = await GetCompletionItemsAsync(@"
public class C
{
    public static explicit operator float(C c) => 0;
    public static explicit operator int(C c) => 0;
    
    public static explicit operator C(float f) => new C();
    public static implicit operator C(string s) => new C();
    public static implicit operator string(C c) => "";
}

public class Program
{
    public void Main()
    {
        var c = new C();
        c.$$
    }
}
", SourceCodeKind.Regular);
            Assert.Collection(items,
                i => Assert.Equal("(float)", i.DisplayText),
                i => Assert.Equal("(int)", i.DisplayText));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
        public async Task ExplicitUserDefinedConversionFromOtherTypeToTargetIsNotSuggested()
        {
            await VerifyNoItemsExistAsync(@"
public class C
{
    public static explicit operator C(float f) => new C();
}

public class Program
{
    public void Main()
    {
        float f = 1;
        f.$$
    }
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
        public async Task ExplicitUserDefinedConversionIsApplied()
        {
            await VerifyCustomCommitProviderAsync(@"
public class C
{
    public static explicit operator float(C c) => 0;
}

public class Program
{
    public void Main()
    {
        var c = new C();
        c.$$
    }
}
", "(float)", @"
public class C
{
    public static explicit operator float(C c) => 0;
}

public class Program
{
    public void Main()
    {
        var c = new C();
        ((float)c).$$
    }
}
");
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
        [InlineData("white.$$", "(Black)",
           "((Black)white).$$")]
        [InlineData("white.$$;", "(Black)",
           "((Black)white).$$;")]
        [InlineData("white.Bl$$", "(Black)",
           "((Black)white).$$")]
        [InlineData("white.Bl$$;", "(Black)",
           "((Black)white).$$;")]
        [InlineData("white?.Bl$$;", "(Black)",
           "((Black)white)?.$$;")]
        [InlineData("white.$$Bl;", "(Black)",
           "((Black)white).$$Bl;")]
        [InlineData("var f = white.$$;", "(Black)",
           "var f = ((Black)white).$$;")]
        [InlineData("white?.$$", "(Black)",
           "((Black)white)?.$$")]
        [InlineData("white?.$$b", "(Black)",
           "((Black)white)?.$$b")]
        [InlineData("white?.$$b.c()", "(Black)",
           "((Black)white)?.$$b.c()")]
        [InlineData("white?.$$b()", "(Black)",
           "((Black)white)?.$$b()")]
        [InlineData("white.Black?.$$", "(White)",
           "((White)white.Black)?.$$")]
        [InlineData("white.Black.$$", "(White)",
           "((White)white.Black).$$")]
        [InlineData("white?.Black?.$$", "(White)",
           "((White)white?.Black)?.$$")]
        [InlineData("white?.Black?.fl$$", "(White)",
           "((White)white?.Black)?.$$")]
        [InlineData("white?.Black.fl$$", "(White)",
           "((White)white?.Black).$$")]
        [InlineData("white?.Black.White.Bl$$ack?.White", "(Black)",
           "((Black)white?.Black.White).$$?.White")]
        [InlineData("((White)white).$$", "(Black)",
           "((Black)((White)white)).$$")]
        [InlineData("(true ? white : white).$$", "(Black)",
           "((Black)(true ? white : white)).$$")]
        public async Task ExplicitUserDefinedConversionIsAppliedForDifferentInvcations(string invocation, string conversionOffering, string fixedCode)
        {
            await VerifyCustomCommitProviderAsync($@"
namespace N
{{
    public class Black
    {{
        public White White {{ get; }}
        public static explicit operator White(Black _) => new White();
    }}
    public class White
    {{
        public Black Black {{ get; }}
        public static explicit operator Black(White _) => new Black();
    }}
    
    public class Program
    {{
        public void Main()
        {{
            var white = new White();
            {invocation}
        }}
    }}
}}
", conversionOffering, @$"
namespace N
{{
    public class Black
    {{
        public White White {{ get; }}
        public static explicit operator White(Black _) => new White();
    }}
    public class White
    {{
        public Black Black {{ get; }}
        public static explicit operator Black(White _) => new Black();
    }}
    
    public class Program
    {{
        public void Main()
        {{
            var white = new White();
            {fixedCode}
        }}
    }}
}}
");
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
        [InlineData("/* Leading */c.$$", "/* Leading */((float)c).$$")]
        [InlineData("/* Leading */c.fl$$", "/* Leading */((float)c).$$")]
        [InlineData("c.  $$", "((float)c).  $$")]
        [InlineData("(true ? /* Inline */ c : c).$$", "((float)(true ? /* Inline */ c : c)).$$")]
        public async Task ExplicitUserDefinedConversionTriviaHandling(string invocation, string fixedCode)
        {
            await VerifyCustomCommitProviderAsync($@"
public class C
{{
    public static explicit operator float(C c) => 0;
}}

public class Program
{{
    public void Main()
    {{
        var c = new C();
        {invocation}
    }}
}}
", "(float)", @$"
public class C
{{
    public static explicit operator float(C c) => 0;
}}

public class Program
{{
    public void Main()
    {{
        var c = new C();
        {fixedCode}
    }}
}}
");
        }

        // 
        // Indexer
        //
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
        public async Task IndexerIsSuggestedAfterDot()
        {
            await VerifyItemExistsAsync(@"
public class C
{
    public int this[int i] => i;
}

public class Program
{
    public void Main()
    {
        var c = new C();
        c.$$
    }
}
", "this[]");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
        public async Task IndexerSuggestionCommitsOpenAndClosingBraces()
        {
            await VerifyCustomCommitProviderAsync(@"
public class C
{
    public int this[int i] => i;
}

public class Program
{
    public void Main()
    {
        var c = new C();
        c.$$
    }
}
", "this[]", @"
public class C
{
    public int this[int i] => i;
}

public class Program
{
    public void Main()
    {
        var c = new C();
        c[$$]
    }
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
        public async Task IndexerWithTwoParametersSuggestionCommitsOpenAndClosingBraces()
        {
            await VerifyCustomCommitProviderAsync(@"
public class C
{
    public int this[int x, int y] => i;
}

public class Program
{
    public void Main()
    {
        var c = new C();
        c.$$
    }
}
", "this[]", @"
public class C
{
    public int this[int x, int y] => i;
}

public class Program
{
    public void Main()
    {
        var c = new C();
        c[$$]
    }
}
");
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
        [InlineData("c.$$", "c[$$]")]
        [InlineData("c. $$", "c[$$] ")]
        [InlineData("c.$$;", "c[$$];")]
        [InlineData("var f = c.$$;", "var f = c[$$];")]
        [InlineData("c?.$$", "c?[$$]")]
        [InlineData("((C)c).$$", "((C)c)[$$]")]
        [InlineData("(true ? c : c).$$", "(true ? c : c)[$$]")]
        public async Task IndexerCompletionForDifferentInvocations(string invocation, string fixedCode)
        {
            await VerifyCustomCommitProviderAsync($@"
public class C
{{
    public int this[int i] => i;
}}

public class Program
{{
    public void Main()
    {{
        var c = new C();
        {invocation}
    }}
}}
", "this[]", @$"
public class C
{{
    public int this[int i] => i;
}}

public class Program
{{
    public void Main()
    {{
        var c = new C();
        {fixedCode}
    }}
}}
");
        }
    }
}
