﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Simplification
{
    internal partial class CSharpMiscellaneousReducer
    {
        private class Rewriter : AbstractReductionRewriter
        {
            public Rewriter(ObjectPool<IReductionRewriter> pool)
                : base(pool)
            {
                _simplifyDefaultExpression = SimplifyDefaultExpression;
            }

            public override SyntaxNode VisitParameter(ParameterSyntax node)
            {
                return SimplifyNode(
                    node,
                    newNode: base.VisitParameter(node),
                    parentNode: node.Parent,
                    simplifier: s_simplifyParameter);
            }

            public override SyntaxNode VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node)
            {
                return SimplifyNode(
                    node,
                    newNode: base.VisitParenthesizedLambdaExpression(node),
                    parentNode: node.Parent,
                    simplifier: s_simplifyParenthesizedLambdaExpression);
            }

            public override SyntaxNode VisitBlock(BlockSyntax node)
            {
                return SimplifyNode(
                    node,
                    newNode: base.VisitBlock(node),
                    parentNode: node.Parent,
                    simplifier: s_simplifyBlock);
            }

            private readonly Func<DefaultExpressionSyntax, SemanticModel, OptionSet, CancellationToken, SyntaxNode> _simplifyDefaultExpression;

            private SyntaxNode SimplifyDefaultExpression(
                DefaultExpressionSyntax node,
                SemanticModel semanticModel,
                OptionSet optionSet,
                CancellationToken cancellationToken)
            {
                if (optionSet.GetOption(CSharpCodeStyleOptions.PreferDefaultLiteral).Value &&
                    node.CanReplaceWithDefaultLiteral(semanticModel, cancellationToken) &&
                    ((CSharpParseOptions)ParseOptions).LanguageVersion >= LanguageVersion.CSharp7_1)
                {
                    return SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression)
                                        .WithTriviaFrom(node);
                }

                return node;
            }

            public override SyntaxNode VisitDefaultExpression(DefaultExpressionSyntax node)
            {
                return SimplifyNode(
                    node,
                    newNode: base.VisitDefaultExpression(node),
                    parentNode: node.Parent,
                    simplifier: _simplifyDefaultExpression);
            }
        }
    }
}