﻿using IronVelocity.Binders;
using NVelocity.Runtime.Parser.Node;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace IronVelocity.Compilation.AST
{
    public class DynamicInvokeExpression : VelocityExpression
    {
        public Expression Target {get; private set;}
        public string Name { get; private set; }
        public IReadOnlyList<Expression> Arguments { get; private set; }

        public DynamicInvokeExpression(INode node, Expression target)
            :base(node)
        {
            if (node == null)
                throw new ArgumentNullException("node");

            if (target == null)
                throw new ArgumentNullException("target");


            var arguments = new List<Expression>(node.ChildrenCount - 1);
            //Subsequent arguments are the parameters
            for (int i = 1; i < node.ChildrenCount; i++)
            {
                arguments.Add(ConversionHelpers.Operand(node.GetChild(i)));
            }
            Arguments = arguments;
            Target = target;
            Name = node.FirstToken.Image;
        }

        protected override Expression ReduceInternal()
        {
            var args = new Expression[Arguments.Count + 1];
            args[0] = Target;

            for (int i = 0; i < Arguments.Count; i++)
            {
                args[i + 1] = Arguments[i];
            }

            //TODO: allow for reuse of callsites
            return Expression.Dynamic(
                new VelocityInvokeMemberBinder(Name, new CallInfo(Arguments.Count)),
                typeof(object),
                args
            );
        }
    }
}
