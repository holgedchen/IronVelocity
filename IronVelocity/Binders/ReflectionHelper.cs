﻿using IronVelocity.Compilation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace IronVelocity.Binders
{
    public static class ReflectionHelper
    {
        private const BindingFlags _caseSensitiveBindingFlags = BindingFlags.Public | BindingFlags.Instance;
        private const BindingFlags _caseInsensitiveBindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase;

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


        private static MemberInfo GetMember(string name, Type type, bool caseSensitive)
        {
            var flags = caseSensitive
                ? _caseSensitiveBindingFlags
                : _caseInsensitiveBindingFlags;

            var property = type.GetProperty(name, flags);

            if (caseSensitive && property != null)
                return property;

            var field = type.GetField(name, flags);

            if (property != null && field != null)
                throw new AmbiguousMatchException();

            if (property != null)
                return property;
            else
                return field;

        }

        public static Expression MemberExpression(string name, DynamicMetaObject target)
        {
            if (target == null)
                throw new ArgumentNullException("target");

            MemberInfo member = null;
            try
            {
                member = ReflectionHelper.GetMember(name, target.LimitType, false);
            }
            catch (AmbiguousMatchException)
            {
                try
                {
                    member = ReflectionHelper.GetMember(name, target.LimitType, true);
                }
                catch (AmbiguousMatchException)
                {
                    Debug.WriteLine(string.Format(CultureInfo.InvariantCulture, "Ambiguous match for member '{0}' on type '{1}'", name, target.LimitType.AssemblyQualifiedName), "Velocity");
                }
            }


            if (member == null)
            {
                //If no matching property or field, fall back to indexer with string param
                var indexer = target.LimitType.GetProperty("Item", _caseSensitiveBindingFlags, null, null, new[] { typeof(string) }, null);
                if (indexer == null)
                {
                    Debug.WriteLine(string.Format(CultureInfo.InvariantCulture, "Unable to resolve Property '{0}' on type '{1}'", name, target.LimitType.AssemblyQualifiedName), "Velocity");
                    return null;
                }
                else
                {
                    return Expression.MakeIndex(
                            VelocityExpressions.ConvertReturnTypeIfNeeded(target, indexer),
                            (PropertyInfo)indexer,
                            new[] { Expression.Constant(name) }
                        );
                }
            }
            else
            {
                var property = member as PropertyInfo;
                if (property != null)
                {
                    return Expression.Property(
                            VelocityExpressions.ConvertReturnTypeIfNeeded(target, member),
                            property
                        );
                }
                else
                {
                    var field = member as FieldInfo;
                    if (field != null)
                    {
                        return Expression.Field(
                                VelocityExpressions.ConvertReturnTypeIfNeeded(target, member),
                                field
                            );
                    }
                }
            }
            return null;
        }


        public static MethodInfo ResolveMethod(Type type, string name, params Type[] argTypes)
        {
            // Loosely based on C# resolution algorithm
            // C# 1.0 resolution algorithm at http://msdn.microsoft.com/en-us/library/aa691336(v=vs.71).aspx
            // C# 5.0 algorithm in section 7.5.3 of spec - http://www.microsoft.com/en-gb/download/details.aspx?id=7029
            //Given the set of applicable candidate function members, the best function member in that set is located.
            var candidates = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
                .Where(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                .Where(x => IsMethodApplicable(x, argTypes))
                .ToList();

            //If the set contains only one function member, then that function member is the best function member.
            if (candidates.Count == 1)
                return candidates.First();

            if (!candidates.Any())
                return null;

            return GetBestFunctionMember(candidates);
        }

        public static MethodInfo GetBestFunctionMember(IEnumerable<MethodInfo> applicableFunctionMembers)
        {
            //Otherwise, the best function member is the one function member that is better than all other function
            //members with respect to the given argument list, provided that each function member is compared to
            //all other function members using the rules in §7.5.3.2.
            var best = new List<MethodInfo>();
            foreach (var candidate in applicableFunctionMembers)
            {
                var candidateArguments = candidate.GetParameters();
                bool lessSpecific = false;
                foreach (var better in best.ToArray())
                {
                    switch (IsBetterFunctionMember(candidate, better))
                    {
                        //If the current candidate is better than the 'better', remove 'better' from
                        case MethodSpecificityComparison.Better:
                            best.Remove(better);
                            break;
                        case MethodSpecificityComparison.Incomparable:
                            break;
                        case MethodSpecificityComparison.Worse:
                            lessSpecific = true;
                            continue;
                        default:
                            throw new InvalidOperationException();
                    }
                }
                if (!lessSpecific)
                {
                    best.Add(candidate);
                }
            }

            //If the set contains only one function member, then that function member is the best function member.
            if (best.Count == 1)
                return best.First();

            //TODO: Implement Tie-break rules

            //If there is not exactly one function member that is better than all other function members, then the
            //function member invocation is ambiguous and a compile-time error occurs.
            throw new AmbiguousMatchException();
        }

        public static MethodSpecificityComparison IsBetterFunctionMember(MethodInfo left, MethodInfo right)
        {
            var leftArgs = left.GetParameters();
            var rightArgs = right.GetParameters();

            if (leftArgs.Length > rightArgs.Length)
                return MethodSpecificityComparison.Better;
            else if (rightArgs.Length > leftArgs.Length)
                return MethodSpecificityComparison.Worse;

            bool leftMoreSpecific = false;
            bool rightMoreSpecific = false;

            for (int i = 0; i < leftArgs.Length; i++)
            {
                var leftType = leftArgs[i].ParameterType;
                var rightType = rightArgs[i].ParameterType;
                //If types the same, then neither is more specific
                if (leftType != rightType)
                {
                    leftMoreSpecific |= CanBeImplicitlyConverted(leftType, rightType);
                    rightMoreSpecific |= CanBeImplicitlyConverted(rightType, leftType);
                }
            }

            if (leftMoreSpecific == rightMoreSpecific)
                return MethodSpecificityComparison.Incomparable;
            else if (leftMoreSpecific)
                return MethodSpecificityComparison.Better;
            else if (rightMoreSpecific)
                return MethodSpecificityComparison.Worse;

            //Should be impossible to get here right??
            throw new InvalidProgramException();
        }

        public enum MethodSpecificityComparison
        {
            Better,
            Incomparable,
            Worse
        }

        public static bool IsMethodApplicable(MethodInfo method, params Type[] argTypes)
        {
            if (method == null)
                throw new ArgumentNullException("method");

            if (argTypes == null)
                argTypes = new Type[0];

            //Don't support generic method definitions
            if (method.IsGenericMethod)
                return false;

            var args = method.GetParameters();
            //Do we have a param array?
            var lastArg = args.LastOrDefault();
            ParameterInfo paramsArrayInfo = null;
            if (lastArg != null)
            {
                if (ReflectionHelper.IsParamsArrayArgument(lastArg))
                    paramsArrayInfo = lastArg;
            }


            if (paramsArrayInfo == null && args.Length != argTypes.Length)
                return false;
            else if (argTypes.Length < args.Length - 1)
                return false;


            for (int i = 0; i < argTypes.Length; i++)
            {
                var paramToValidateAgainst = i >= args.Length
                    ? paramsArrayInfo
                    : args[i];

                if (!IsArgumentCompatible(argTypes[i], paramToValidateAgainst))
                    return false;
            }

            return true;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter")]
        public static bool CanBeImplicitlyConverted<TFrom, TTo>()
        {
            return CanBeImplicitlyConverted(typeof(TFrom), typeof(TTo));
        }

        public static bool CanBeImplicitlyConverted(Type from, Type to)
        {
            //from may be null, but to may not be
            if (to == null)
                throw new ArgumentNullException("to");


            if (from == null)
                return !to.IsPrimitive;

            if (to.IsAssignableFrom(from))
                return true;

            Type[] supportedConversions;
            if (_implicitNumericConversions.TryGetValue(from, out supportedConversions))
                return supportedConversions.Contains(to);

            return false;
        }

        public static bool IsArgumentCompatible(Type runtimeType, ParameterInfo parameter)
        {
            if (parameter == null)
                throw new ArgumentNullException("parameter");

            if (CanBeImplicitlyConverted(runtimeType, parameter.ParameterType))
                return true;

            return IsParamsArrayArgument(parameter)
                && CanBeImplicitlyConverted(runtimeType, parameter.ParameterType.GetElementType());
        }

        public static bool IsParamsArrayArgument(ParameterInfo param)
        {
            return param != null
                && param.GetCustomAttributes<ParamArrayAttribute>().Any();
        }

    }
}