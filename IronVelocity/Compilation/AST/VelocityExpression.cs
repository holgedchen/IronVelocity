﻿using NVelocity.Runtime.Parser.Node;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace IronVelocity.Compilation.AST
{
    public abstract class VelocityExpression : Expression
    {
        protected VelocityExpression() { }

        protected VelocityExpression(INode node)
        {
            if (node == null)
                throw new ArgumentNullException("node");
        }

        public override Expression Reduce()
        {
            return ReduceInternal();
        }

        public override bool CanReduce { get { return true; } }
        public override ExpressionType NodeType { get { return ExpressionType.Extension; } }
        public override Type Type { get { return typeof(object); } }

        protected abstract Expression ReduceInternal();
    }
}