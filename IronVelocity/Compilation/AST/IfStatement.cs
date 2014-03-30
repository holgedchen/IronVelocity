﻿using NVelocity.Runtime.Parser.Node;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace IronVelocity.Compilation.AST
{
    public class IfStatement : VelocityExpression
    {
        public Expression Value { get; private set; }

        public IfStatement(INode node, VelocityASTConverter converter)
            : base(node)
        {
            if (node == null)
                throw new ArgumentNullException("node");

            if (!(node is ASTIfStatement))
                throw new ArgumentOutOfRangeException("node");

            if (converter == null)
                throw new ArgumentNullException("converter");

            var condition = converter.Expr(node.GetChild(0));
            var trueContent = converter.Block(node.GetChild(1));
            Expression falseContent = null;

            //Build the false expression recursively from the bottom up
            for (int i = node.ChildrenCount - 1; i > 1; i--)
            {
                var child = node.GetChild(i);
                if (child is ASTElseStatement)
                {
                    if (falseContent != null)
                        throw new InvalidOperationException("Cannot have two 'else' statements");

                    if (child.ChildrenCount != 1)
                        throw new InvalidOperationException("Expected ASTElseStatement to only have 1 child");

                    falseContent = converter.Block(child.GetChild(0));
                }
                else if (child is ASTElseIfStatement)
                {
                    var innerCondition = converter.Expr(child.GetChild(0));
                    var innerContent = converter.Block(child.GetChild(1));

                    falseContent = If(child, innerCondition, innerContent, falseContent);
                }
                else

                    throw new InvalidOperationException("Expected: ASTElseStatement, Actual: " + child.GetType().Name);
            }
            Value = If(node, condition, trueContent, falseContent);
        }


        /// <summary>
        /// Helper to build an If or an IfElse statement based on whether we have the falseContent block is not null
        /// </summary>
        /// <returns></returns>
        private static Expression If(INode node, Expression condition, Expression trueContent, Expression falseContent)
        {
            if (condition.Type != typeof(bool))
                condition = VelocityExpressions.CoerceToBoolean(condition);

            var expr = falseContent != null
                ? Expression.IfThenElse(condition, trueContent, falseContent)
                : Expression.IfThen(condition, trueContent);

            return expr;
        }
        protected override Expression ReduceInternal()
        {
            return Value;
        }

        public override Type Type{get{return typeof(void);}}
    }
}