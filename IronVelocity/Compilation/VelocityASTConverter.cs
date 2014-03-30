﻿using IronVelocity.Binders;
using IronVelocity.Compilation.AST;
using IronVelocity.Compilation.Directives;
using NVelocity.Runtime.Parser;
using NVelocity.Runtime.Parser.Node;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace IronVelocity.Compilation
{
    public class VelocityASTConverter
    {
 
        private IDictionary<Type, DirectiveExpressionBuilder> _directiveHandlers;
        private IScope _scope = new BaseScope(Constants.InputParameter);
        private SymbolDocumentInfo _symbolDocument;
        private static ParameterExpression _evaulatedResult = Expression.Parameter(typeof(object), "tempEvaulatedResult");

        public VelocityASTConverter(IDictionary<Type, DirectiveExpressionBuilder> directiveHandlers)
        {
            _directiveHandlers = directiveHandlers;
        }



        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public Expression BuildExpressionTree(ASTprocess ast, string fileName)
        {
            if (ast == null)
                throw new ArgumentNullException("ast");

            if (!(ast is ASTprocess))
                throw new ArgumentOutOfRangeException("ast");

            List<Expression> expressions = new List<Expression>();
            _symbolDocument = Expression.SymbolDocument(fileName);


            expressions.AddRange(GetBlockExpressions(ast, true));

            if (!expressions.Any())
                return Expression.Default(typeof(void));

            return Expression.Block(new[] { _evaulatedResult }, expressions);
        }

        public Expression GetVariable(string name)
        {
            return _scope.GetVariable(name);
        }



        public static Expression DebugInfo(INode node, Expression expression)
        {
            if (node == null)
                throw new ArgumentNullException("node");

            return expression;
            /*
             * return Expression.Block(
                Expression.DebugInfo(_symbolDocument, node.FirstToken.BeginLine + 1, node.FirstToken.BeginColumn, node.LastToken.EndLine + 1, node.LastToken.EndColumn),
                expression
                //, Expression.ClearDebugInfo(_symbolDocument)
            );
            */
        }

        public IEnumerable<Expression> GetBlockExpressions(INode node, bool output)
        {
            if (node == null)
                throw new ArgumentNullException("node");

            //ASTprocess is a special case for the root, otherwise it behaves exactly like ASTBlock
            if (!(node is ASTBlock || node is ASTprocess))
                throw new ArgumentOutOfRangeException("node");

            var expressions = new List<Expression>();


            foreach (var child in node.GetChildren())
            {
                Expression expr;
                switch (child.Type)
                {
                    case ParserTreeConstants.TEXT:
                    case ParserTreeConstants.ESCAPE:
                    case ParserTreeConstants.ESCAPED_DIRECTIVE:
                        expr = Text(child);
                        if (output)
                            expr = Output(expr);
                        break;
                    case ParserTreeConstants.REFERENCE:
                        expr = Reference(child, output);
                        if (output)
                            expr = Output(expr);
                        break;
                    case ParserTreeConstants.IF_STATEMENT:
                        expr = IfStatement(child);
                        break;
                    case ParserTreeConstants.SET_DIRECTIVE:
                        expr = Set(child);
                        break;
                    case ParserTreeConstants.DIRECTIVE:
                        expr = Directive(child);
                        if (output)
                            expr = Output(expr);
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



        private Expression Directive(INode node)
        {
            if (node == null)
                throw new ArgumentNullException("node");

            var directive = node as ASTDirective;
            if (directive == null)
                throw new ArgumentOutOfRangeException("node");

            if (directive.Directive == null)
                return Text(node);

            DirectiveExpressionBuilder builder;

            if (_directiveHandlers.TryGetValue(directive.Directive.GetType(), out builder))
                return builder.Build(directive, this);
            else
                throw new NotSupportedException(String.Format(CultureInfo.InvariantCulture, "Unable to handle directive type '{0}'", directive.DirectiveName));
        }



        public Expression Block(INode node)
        {
            if (node == null)
                throw new ArgumentNullException("node");

            if (!(node is ASTBlock))
                throw new ArgumentOutOfRangeException("node");

            var children = GetBlockExpressions(node, true);

            Expression expr;
            if (children.Any())
                expr = Expression.Block(children);
            else
                expr = Expression.Empty();

            return DebugInfo(node, expr);
        }

        private Expression Set(INode node)
        {
            if (node == null)
                throw new ArgumentNullException("node");

            if (!(node is ASTSetDirective))
                throw new ArgumentOutOfRangeException("node");

            if (node.ChildrenCount != 1)
                throw new ArgumentOutOfRangeException("node", "Expected only one child");

            return Expr(node.GetChild(0));
        }





        public static Expression Reference(INode node, bool renderable)
        {
            if (node == null)
                throw new ArgumentNullException("node");

            return renderable
                ? new RenderableDynamicReference(node)
                : new DynamicReference(node);
        }



   

        private static Expression NumberLiteral(INode node)
        {
            if (node == null)
                throw new ArgumentNullException("node");

            if (!(node is ASTNumberLiteral))
                throw new ArgumentOutOfRangeException("node");

            return Expression.Constant(int.Parse(node.Literal, CultureInfo.InvariantCulture));
        }

        private Expression IfStatement(INode node)
        {
            if (node == null)
                throw new ArgumentNullException("node");

            if (!(node is ASTIfStatement))
                throw new ArgumentOutOfRangeException("node");

            return new IfStatement(node, this);            
        }


        public static Expression Operand(INode node)
        {
            return ConversionHelpers.Operand(node);
 
        }

        public Expression Expr(INode node)
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

       

        private static Expression Text(INode node)
        {
            if (node == null)
                throw new ArgumentNullException("node");

            var text = NodeUtils.tokenLiteral(node.FirstToken);
            if (text != node.Literal)
            {
            }
            return DebugInfo(node, Expression.Constant(text));
        }

        private static Expression Output(Expression expression)
        {
            if (expression == null)
                throw new ArgumentNullException("expression");

            //If the expression is of type void, we can't print anything, so just return the current expression
            if (expression.Type == typeof(void))
                return expression;

            if (expression.Type != typeof(string))
                expression = Expression.Call(expression, MethodHelpers.ToStringMethodInfo);


            return Expression.Call(Constants.OutputParameter, MethodHelpers.AppendMethodInfo, expression);
        }

    }
}
