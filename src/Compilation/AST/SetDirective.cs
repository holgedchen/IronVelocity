﻿using IronVelocity.Binders;
using System;
using System.Linq.Expressions;

namespace IronVelocity.Compilation.AST
{
    public class SetDirective : VelocityBinaryExpression
    {
        private static readonly ParameterExpression _objectTemp = Expression.Parameter(typeof(object), "setDirectiveTemp");
        private readonly IBinderFactory _binderFactory;

        public override Type Type => typeof(void);
        public override VelocityExpressionType VelocityExpressionType => VelocityExpressionType.SetDirective;


        public SetDirective(Expression left, Expression right, SourceInfo sourceInfo, IBinderFactory binderFactory)
            : base(left, right, sourceInfo)
        {
            _binderFactory = binderFactory;
        }


        public override VelocityBinaryExpression Update(Expression left, Expression right)
        {
            if (Left == left && Right == right)
                return this;

            if (left is GlobalVariableExpression)
                throw new NotSupportedException("Cannot assign to a global variable");

            return new SetDirective(left, right, SourceInfo, _binderFactory);
        }


        public override Expression Reduce()
        {
            var left = Left;
            var right = Right;

            if (left is ReferenceExpression)
                left = left.Reduce();

            if (left is GlobalVariableExpression)
                throw new NotSupportedException("Cannot assign to a Global Variable");

            var getMember = left as PropertyAccessExpression;
            if (getMember != null)
            {
                return new SetMemberExpression(getMember.Target, right, _binderFactory.GetSetMemberBinder(getMember.Name));
            }

            var indexer = left as IndexInvocationExpression;
            if (indexer != null)
            {
                return new SetIndexExpression(indexer.Target, right, indexer.Arguments, _binderFactory.GetSetIndexBinder(indexer.Arguments.Count));
            }


            bool rightIsNullableType = TypeHelper.IsNullableType(right.Type);

            bool isVariableExpression = left is VariableExpression;
            if (isVariableExpression)
                left = left.Reduce();
            else if (left is MethodInvocationExpression || left is MethodCallExpression || left is ConstantExpression)
                return Constants.EmptyExpression;

            if (!left.Type.IsAssignableFrom(right.Type))
            {
                //If we can't assign from right to left, but can from left to right
                // Then we may be able to assign at runtime
                if (right.Type.IsAssignableFrom(left.Type))
                {
                    right = Expression.TypeAs(right, left.Type);
                }
                else
                {
                    //TODO: Log
                    return Constants.EmptyExpression;
                }
            }
            else
            {
                right = VelocityExpressions.ConvertIfNeeded(right, left.Type);
            }



            //However, if the expression is guaranteed to be a value type (i.e. not nullable), why bother?
            //Similarly if it's a variable expression, the null handling is handled inside VelocityContext
            if (isVariableExpression || !rightIsNullableType)
                return Expression.Block(typeof(void), Expression.Assign(left, right));

            var tempResult = right.Type == typeof(object)
                ? _objectTemp
                : Expression.Parameter(right.Type, "setDirectiveTemp");

            return new TemporaryVariableScopeExpression(tempResult,
                Expression.IfThen(
                    //Store the result of the right hand side in to a temporary variable
                    //If the temporary variable is not equal to null
                    Expression.NotEqual(Expression.Assign(tempResult, right), Expression.Constant(null, right.Type)),
                    //Make the assignment
                    Expression.Assign(left, tempResult)
                )
            );
        }

    }
}
