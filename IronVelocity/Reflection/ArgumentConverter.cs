﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace IronVelocity.Reflection
{
    public class ArgumentConverter : IArgumentConverter
    {
        /// <summary>
        /// A list of implicit numeric conversions as defined in section 6.1.2 of the C# 5.0 spec
        /// The value array indicates types which the Key type can be implicitly converted to.
        /// </summary>
        private static readonly IDictionary<Type, Type[]> _implicitNumericConversions = new Dictionary<Type, Type[]>(){
                    { typeof(sbyte), new[]{typeof(short), typeof(int), typeof(long), typeof(float), typeof(double), typeof(decimal)}},
                    { typeof(byte), new[]{typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal)}},
                    { typeof(short), new[]{typeof(int), typeof(long), typeof(float), typeof(double), typeof(decimal)}},
                    { typeof(ushort), new[]{typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal)}},
                    { typeof(int), new[]{typeof(long), typeof(float), typeof(double), typeof(decimal)}},
                    { typeof(uint), new[]{typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal)}},
                    { typeof(long), new[]{typeof(float), typeof(double), typeof(decimal)}},
                    { typeof(ulong), new[]{typeof(float), typeof(double), typeof(decimal)}},
                    { typeof(char), new[]{typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal)}},
                    { typeof(float), new[]{typeof(double)}},
                };

        public bool CanBeConverted(Type from, Type to)
        {
            return GetConverter(from, to) != null;
        }

        private static readonly IExpressionConverter _implicitConverter = new ImplicitExpressionConverter();
        public IExpressionConverter GetConverter(Type from, Type to)
        {
            //from may be null, but to may not be
            if (to == null)
                throw new ArgumentNullException("to");

            if (from == null)
                return to.IsValueType
                    ? null
                    : _implicitConverter;

            if (to.IsAssignableFrom(from))
                return _implicitConverter;

            /*
            if (from.IsEnum)
                return GetConverter(Enum.GetUnderlyingType(from), to);
            */

            if (from.IsPrimitive)
            {
                Type[] supportedConversions;
                if (_implicitNumericConversions.TryGetValue(from, out supportedConversions))
                    return supportedConversions.Contains(to)
                        ? _implicitConverter
                        : null;
            }

            return null;
        }
    }
}
