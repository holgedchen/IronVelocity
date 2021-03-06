﻿using System;
using System.Linq.Expressions;

namespace IronVelocity.Compilation.AST
{
    public class UnrecognisedDirective : Directive
    {
        private readonly string _literal;
        public override Type Type => typeof(string);

        public string Name { get; }

        public UnrecognisedDirective(string name, string literal)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentOutOfRangeException(nameof(name));

            if (string.IsNullOrWhiteSpace(literal))
                throw new ArgumentOutOfRangeException(nameof(literal));

            _literal = literal;
            Name = name;
        }


        public override Expression Reduce()
            => Expression.Constant(_literal);


    }
}
