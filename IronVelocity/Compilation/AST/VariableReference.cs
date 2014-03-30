﻿using NVelocity.Runtime.Parser.Node;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace IronVelocity.Compilation.AST
{
    public class VariableReference : VelocityExpression
    {
        public string Name { get; private set; }
        public VariableReference(string name)
        {
            Name = name;
        }

        private static readonly PropertyInfo _indexerProperty = typeof(VelocityContext).GetProperty("Item", typeof(Expression), new[] { typeof(string) });
        protected override Expression ReduceInternal()
        {
            return Expression.MakeIndex(Constants.InputParameter, _indexerProperty, new[] { Expression.Constant(Name) });
        }
    }
}