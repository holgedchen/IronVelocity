﻿using System;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

namespace IronVelocity.Compilation.AST
{
    public class ForeachExpression : VelocityExpression
    {
        private static readonly MethodInfo _moveNextMethodInfo = typeof(IEnumerator).GetMethod("MoveNext", new Type[] { });
        private static readonly MethodInfo _enumeratorMethodInfo = typeof(IEnumerable).GetMethod("GetEnumerator", new Type[] { });
        private static readonly PropertyInfo _currentPropertyInfo = typeof(IEnumerator).GetProperty("Current");

        public Expression Enumerable { get; private set; }
        public Expression Body { get; private set; }
        public Expression CurrentItem { get; private set; }
        public LabelTarget BreakLabel { get; private set; }

        public ForeachExpression(Expression enumerable, Expression body, Expression currentItem, LabelTarget breakLabel)
        {
            if (enumerable == null)
                throw new ArgumentNullException("enumerable");

            if (body == null)
                throw new ArgumentNullException("body");

            if (currentItem == null)
                throw new ArgumentNullException("currentItem");

            if (breakLabel == null)
                throw new ArgumentNullException("breakLabel");

            Enumerable = enumerable;
            Body = body;
            CurrentItem = currentItem;
            BreakLabel = breakLabel;
        }

        public override Expression Reduce()
        {
            var enumerator = Expression.Parameter(typeof(IEnumerator), "enumerator");
            var @continue = Expression.Label("continue");
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
                            @continue
                        );

            var enumerable = VelocityExpressions.ConvertIfNeeded(Enumerable, typeof(IEnumerable));

            return Expression.Block(
                new[] { enumerator },
                    Expression.Assign(enumerator, Expression.Call(enumerable, _enumeratorMethodInfo)),
                    loop
                );
        }

        public override Type Type { get { return typeof(void); } }
    }


}
