﻿using IronVelocity.Binders;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Linq.Expressions;

namespace IronVelocity
{
    [EventSource(Name = "IronVelocity-Binding")]
    public sealed class BindingEventSource : EventSource
    {
        internal static readonly BindingEventSource Log = new BindingEventSource();

        [Event(1, Opcode = EventOpcode.Info, Level = EventLevel.Warning, Keywords = Keywords.GetMember)]
        public void GetMemberResolutionFailure(string memberName, string targetType)
        {
            WriteEvent(1, memberName, targetType);
        }

        [Event(2, Opcode = EventOpcode.Info, Level = EventLevel.Warning, Keywords = Keywords.GetMember)]
        public void GetMemberResolutionAmbiguous(string memberName, string targetType)
        {
            WriteEvent(2, memberName, targetType);
        }

        [Event(3, Level = EventLevel.Warning, Keywords = Keywords.InvokeMember)]
        public void InvokeMemberResolutionFailure(string memberName, string targetType, string argTypes)
        {
            WriteEvent(3, memberName, targetType, argTypes);
        }

        [Event(4, Opcode = EventOpcode.Info, Level = EventLevel.Warning, Keywords = Keywords.InvokeMember)]
        public void InvokeMemberResolutionAmbiguous(string memberName, string targetType, string argTypes)
        {
            WriteEvent(4, memberName, targetType, argTypes);
        }

        [Event(5, Opcode = EventOpcode.Info, Level = EventLevel.Warning, Keywords = Keywords.SetMember)]
        public void SetMemberResolutionFailure(string memberName, string targetType, string valueType)
        {
            WriteEvent(5, memberName, targetType, valueType);
        }

		[Event(6, Opcode = EventOpcode.Info, Level = EventLevel.Warning, Keywords = Keywords.BinaryOperation)]
		public void BinaryOperationResolutionFailure(ExpressionType operation, string leftType, string rightType)
		{
			WriteEvent(6, operation, leftType, rightType);
		}


        [SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible", Justification = "Required to be public by ETW")]
        public static class Keywords
        {
            public const EventKeywords GetMember = (EventKeywords)1;
            public const EventKeywords SetMember = (EventKeywords)2;
            public const EventKeywords InvokeMember = (EventKeywords)4;
            public const EventKeywords BinaryOperation = (EventKeywords)8;
        }
    }
}
