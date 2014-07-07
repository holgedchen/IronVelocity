﻿using IronVelocity.Binders;
using IronVelocity.Compilation.Directives;
using NVelocity.Runtime.Parser;
using NVelocity.Runtime.Parser.Node;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace IronVelocity.Compilation.AST
{
    public class VelocityExpressionBuilder
    {
        private static readonly Expression TrueExpression = Expression.Constant(true);
        private static readonly Expression FalseExpression = Expression.Constant(false);

        private readonly IDictionary<Type, DirectiveExpressionBuilder> _directiveHandlers;
        public ParameterExpression OutputParameter { get; set; }
        public Stack<CustomDirectiveExpression> CustomDirectives { get; private set; }

        public IDictionary<Type, DirectiveExpressionBuilder> DirectiveHandlers
        {
            get { return new Dictionary<Type, DirectiveExpressionBuilder>(_directiveHandlers); }
        }


        public VelocityExpressionBuilder(IDictionary<Type, DirectiveExpressionBuilder> directiveHandlers)
            : this (directiveHandlers, "$output")
        {
        }

        public VelocityExpressionBuilder(IDictionary<Type, DirectiveExpressionBuilder> directiveHandlers, string parameterName)
        {
            _directiveHandlers = directiveHandlers ?? new Dictionary<Type, DirectiveExpressionBuilder>();
            OutputParameter = Expression.Parameter(typeof(StringBuilder), parameterName);
            CustomDirectives = new Stack<CustomDirectiveExpression>();
        }

        public IReadOnlyCollection<Expression> GetBlockExpressions(INode node)
        {
            if (node == null)
                throw new ArgumentNullException("node");

            //ASTprocess is a special case for the root, otherwise it behaves exactly like ASTBlock
            if (!(node is ASTBlock || node is ASTprocess))
                throw new ArgumentOutOfRangeException("node");

            var expressions = new List<Expression>(node.ChildrenCount);

            foreach (var child in GetChildNodes(node))
            {
                Expression expr;
                switch (child.Type)
                {
                    case ParserTreeConstants.TEXT:
                    case ParserTreeConstants.ESCAPE:
                        var content = NodeUtils.tokenLiteral(child.FirstToken);
                        expr = Expression.Constant(content);
                        break;
                    case ParserTreeConstants.ESCAPED_DIRECTIVE:
                        expr = Expression.Constant(child.Literal);
                        break;
                    case ParserTreeConstants.REFERENCE:
                        expr = new ReferenceExpression(child);
                        break;
                    case ParserTreeConstants.IF_STATEMENT:
                        expr = new IfStatement(child, this);
                        break;
                    case ParserTreeConstants.SET_DIRECTIVE:
                        expr = Set(child);
                        break;
                    case ParserTreeConstants.DIRECTIVE:
                        expr = Directive(child);
                        break;
                    case ParserTreeConstants.COMMENT:
                        continue;

                    default:
                        throw new NotSupportedException("Node type not supported in a block: " + child.GetType().Name);
                }

                expressions.Add(expr);
            }

            return expressions;
        }

        public Expression Directive(INode child)
        {
            var directiveNode = (ASTDirective)child;

            if (directiveNode.DirectiveName == "macro")
                throw new NotSupportedException("TODO: #macro support");
            if (directiveNode.DirectiveName == "include")
                throw new NotSupportedException("TODO: #include support");
            if (directiveNode.DirectiveName == "parse")
                throw new NotSupportedException("TODO: #parse support");

            DirectiveExpressionBuilder builder;
            foreach (var customDirective in CustomDirectives)
            {
                var expr = customDirective.ProcessChildDirective(directiveNode.DirectiveName, directiveNode);
                if (expr != null)
                    return expr;
            }

            if (directiveNode.Directive != null && _directiveHandlers.TryGetValue(directiveNode.Directive.GetType(), out builder))
            {
                return builder.Build(directiveNode, this);
            }
            else
                return new UnrecognisedDirective(directiveNode, this);
        }


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity",
            Justification="Cannot really be simplified any furhter - it's just a massive switch statement creating an AST node based on the node type")]
        public static Expression Operand(INode node)
        {
            if (node == null)
                throw new ArgumentNullException("node");

            switch (node.Type)
            {
                case ParserTreeConstants.TRUE:
                    return TrueExpression;
                case ParserTreeConstants.FALSE:
                    return FalseExpression;
                case ParserTreeConstants.NUMBER_LITERAL:
                    return Expression.Constant(int.Parse(node.Literal, CultureInfo.InvariantCulture)); ;

                case ParserTreeConstants.STRING_LITERAL:
                    return new StringExpression(node);
                case ParserTreeConstants.AND_NODE:
                    return And(node);
                    //return new BinaryLogicalExpression(node, LogicalOperation.And);
                case ParserTreeConstants.OR_NODE:
                    return Or(node);
                    //return new BinaryLogicalExpression(node, LogicalOperation.Or);
                case ParserTreeConstants.NOT_NODE:
                    return Not(node);

                //Comparison
                case ParserTreeConstants.LT_NODE:
                    return new BinaryLogicalExpression(node, LogicalOperation.LessThan);
                case ParserTreeConstants.LE_NODE:
                    return new BinaryLogicalExpression(node, LogicalOperation.LessThanOrEqual);
                case ParserTreeConstants.GT_NODE:
                    return new BinaryLogicalExpression(node, LogicalOperation.GreaterThan);
                case ParserTreeConstants.GE_NODE:
                    return new BinaryLogicalExpression(node, LogicalOperation.GreaterThanOrEqual);
                case ParserTreeConstants.EQ_NODE:
                    return new BinaryLogicalExpression(node, LogicalOperation.Equal);
                case ParserTreeConstants.NE_NODE:
                    return new BinaryLogicalExpression(node, LogicalOperation.NotEqual);

                //Mathematical Operations
                case ParserTreeConstants.ADD_NODE:
                    return new BinaryMathematicalExpression(node, MathematicalOperation.Add);
                case ParserTreeConstants.SUBTRACT_NODE:
                    return new BinaryMathematicalExpression(node, MathematicalOperation.Subtract);
                case ParserTreeConstants.MUL_NODE:
                    return new BinaryMathematicalExpression(node, MathematicalOperation.Multiply);
                case ParserTreeConstants.DIV_NODE:
                    return new BinaryMathematicalExpression(node, MathematicalOperation.Divide);
                case ParserTreeConstants.MOD_NODE:
                    return new BinaryMathematicalExpression(node, MathematicalOperation.Modulo);
                //Code
                case ParserTreeConstants.ASSIGNMENT:
                    return new SetDirective(node);
                case ParserTreeConstants.REFERENCE:
                    return new ReferenceExpression(node);
                case ParserTreeConstants.OBJECT_ARRAY:
                    return new ObjectArrayExpression(node);
                case ParserTreeConstants.INTEGER_RANGE:
                    return new IntegerRangeExpression(node);
                case ParserTreeConstants.EXPRESSION:
                    return Expr(node);
                default:
                    throw new NotSupportedException("Node type not supported in an expression: " + node.GetType().Name);
            }
        }


        private static Expression Set(INode node)
        {
            if (node == null)
                throw new ArgumentNullException("node");

            if (!(node is ASTSetDirective))
                throw new ArgumentOutOfRangeException("node");

            if (node.ChildrenCount != 1)
                throw new ArgumentOutOfRangeException("node", "Expected only one child");

            return Expr(node.GetChild(0));
        }


        public static Expression Expr(INode node)
        {
            if (node == null)
                throw new ArgumentNullException("node");

            if (!(node is ASTExpression))
                throw new ArgumentOutOfRangeException("node");

            if (node.ChildrenCount != 1)
                throw new ArgumentOutOfRangeException("node", "Only expected one child");

            var child = node.GetChild(0);
            return Operand(child);
        }


        private static void GetBinaryExpressionOperands(INode node, out Expression left, out Expression right)
        {
            if (node == null)
                throw new ArgumentNullException("node");

            if (node.ChildrenCount != 2)
                throw new NotImplementedException("Expected exactly two children for a binary expression");

            left = Operand(node.GetChild(0));
            right = Operand(node.GetChild(1));
        }

        private static Expression BinaryLogicalExpression(Func<Expression, Expression, MethodInfo, Expression> generator, INode node, MethodInfo implementation = null)
        {
            Expression left, right;
            GetBinaryExpressionOperands(node, out left, out right);

            // The expression tree will fail if the types don't *exactly* match the types on the method signature
            // So ensure everything is converted to object
            left = VelocityExpressions.CoerceToBoolean(left);
            right = VelocityExpressions.CoerceToBoolean(right);

            return generator(left, right, implementation);
        }

        #region Logical

        private static Expression Not(INode node)
        {
            if (node == null)
                throw new ArgumentNullException("node");

            if (!(node is ASTNotNode))
                throw new ArgumentOutOfRangeException("node");

            if (node.ChildrenCount != 1)
                throw new ArgumentOutOfRangeException("node");

            var operand = Operand(node.GetChild(0));
            var expression = VelocityExpressions.CoerceToBoolean(operand);

            return Expression.Not(expression);
        }

        private static Expression And(INode node)
        {
            if (node == null)
                throw new ArgumentNullException("node");

            if (!(node is ASTAndNode))
                throw new ArgumentOutOfRangeException("node");

            return BinaryLogicalExpression(Expression.AndAlso, node);
        }

        private static Expression Or(INode node)
        {
            if (node == null)
                throw new ArgumentNullException("node");

            if (!(node is ASTOrNode))
                throw new ArgumentOutOfRangeException("node");


            return BinaryLogicalExpression(Expression.OrElse, node);
        }


        #endregion

        private static IEnumerable<INode> GetChildNodes(INode node)
        {
            for (int i = 0; i < node.ChildrenCount; i++)
            {
                yield return node.GetChild(i);
            };
        }

    }
}
