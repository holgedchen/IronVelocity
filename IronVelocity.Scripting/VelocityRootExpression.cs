﻿using IronVelocity.Compilation;
using IronVelocity.Compilation.AST;
using IronVelocity.Compilation.Directives;
using NVelocity.Runtime.Parser.Node;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace IronVelocity.Scripting
{
    public class VelocityRootExpression : Expression
    {
        public IReadOnlyCollection<Expression> Children { get; private set; }
        private readonly IDictionary<Type, DirectiveExpressionBuilder> _directiveHandlers;
        private readonly string _name;
        private readonly bool _staticTypeWherePossible;
        private readonly VelocityCompilerOptions _compilerOptions;
        public readonly VelocityExpressionBuilder _builder;

        public VelocityRootExpression(ASTprocess node, VelocityCompilerOptions compilerOptions, string name)
        {
            _builder = new VelocityExpressionBuilder(_directiveHandlers);
            Children = _builder.GetBlockExpressions(node);
        }

        public override Expression Reduce()
        {
            return GetLambda();
        }

        public Expression<VelocityTemplateMethod> GetLambda()
        {
            var content = new RenderedBlock(Children, _builder);

            return Expression.Lambda<VelocityTemplateMethod>(content, _name, new[] { Constants.InputParameter, _builder.OutputParameter });
        }

        public override Type Type { get { return typeof(VelocityTemplateMethod); } }
        public override ExpressionType NodeType { get { return ExpressionType.Extension; } }
    }
}