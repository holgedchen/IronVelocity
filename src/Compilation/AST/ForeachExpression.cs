﻿using System;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

namespace IronVelocity.Compilation.AST
{
    public class ForeachExpression : VelocityExpression
    {
        private static readonly MethodInfo _moveNextMethodInfo = typeof(IEnumerator).GetMethod(nameof(IEnumerator.MoveNext), Type.EmptyTypes);
        private static readonly MethodInfo _enumeratorMethodInfo = typeof(IEnumerable).GetMethod(nameof(IEnumerable.GetEnumerator), Type.EmptyTypes);
        private static readonly PropertyInfo _currentPropertyInfo = typeof(IEnumerator).GetProperty(nameof(IEnumerator.Current));

        public Expression Enumerable { get; }
        public Expression Body { get; }
        public Expression CurrentItem { get; }
        public LabelTarget BreakLabel { get; }
        public LabelTarget ContinueLabel { get; }

        public override Type Type => typeof(void);
        public override VelocityExpressionType VelocityExpressionType => VelocityExpressionType.Foreach;


        public ForeachExpression(Expression enumerable, Expression body, Expression currentItem, LabelTarget breakLabel, LabelTarget continueLabel)
        {
            if (enumerable == null)
                throw new ArgumentNullException(nameof(enumerable));

            if (!typeof(IEnumerable).IsAssignableFrom(enumerable.Type))
                throw new ArgumentOutOfRangeException(nameof(enumerable));

            if (body == null)
                throw new ArgumentNullException(nameof(body));

            if (currentItem == null)
                throw new ArgumentNullException(nameof(currentItem));


            Enumerable = enumerable;
            Body = body;
            CurrentItem = currentItem;
            BreakLabel = breakLabel ?? Expression.Label("break");
            ContinueLabel = continueLabel ?? Expression.Label("continue");
        }

        public override Expression Reduce()
        {
            var enumerator = Expression.Parameter(typeof(IEnumerator), "enumerator");

            var body = Expression.Block(
                            Expression.Assign(CurrentItem, Expression.Property(enumerator, _currentPropertyInfo)),
                            Body
                        );

            var loop = Expression.Loop(
                            Expression.IfThenElse(
                                Expression.IsTrue(Expression.Call(enumerator, _moveNextMethodInfo)),
                                body,
                                Expression.Break(BreakLabel)
                            ),
                            BreakLabel,
                            ContinueLabel
                        );

            return new TemporaryVariableScopeExpression(
                    enumerator,
                    Expression.Block(
                        Expression.Assign(enumerator, Expression.Call(Enumerable, _enumeratorMethodInfo)),
                        loop
                    )
                );
        }
    }
}
