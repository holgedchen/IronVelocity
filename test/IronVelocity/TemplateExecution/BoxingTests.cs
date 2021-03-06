﻿using NUnit.Framework;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace IronVelocity.Tests.TemplateExecution
{
    /// <summary>
    /// This class tests that we are properly unboxing any boxed value types before accessing child members which may mutate the value type
    /// 
    /// Remember that value types (primitives & structs) are passed by value rather than by reference.  
    /// If we fail to properly unbox an object before calling a member which mutates the value type
    /// we will be modifying the boxed copy, and not the underlying value type.
    /// </summary>
    /// 
    [TestFixture(StaticTypingMode.AsProvided)]
    [TestFixture(StaticTypingMode.PromoteContextToGlobals)]
    public class BoxingTests : TemplateExeuctionBase
    {
        public BoxingTests(StaticTypingMode mode) : base(mode)
        {
        }

        [Test]
        public void ShouldNotBoxValueTypeVariableBeforeInvokingProperty()
        {
            // If $x is boxed each time a property is invoked, the property is invoked on the copy
            // So any state mutations won't persist for the next use of $x
            var input = "$x.CallCount, $x.CallCount, $x.CallCount";

            var context = new { x = new MutableStruct() };

            var execution = ExecuteTemplate(input, context);

            Assert.That(execution.Output, Is.EqualTo("0, 1, 2"));
        }


        [Test]
        public void ShouldNotBoxValueTypeVariableBeforeSettingProperty()
        {
            // If $x is boxed each time before being set, the property is set on the copy
            // So the next use of $x will be unchanged by the #set.

            var input = "#set($x.ManualInt = 42)$x.ManualInt";
            var context = new { x = new MutableStruct() };

            var execution = ExecuteTemplate(input, context);

            Assert.That(execution.Output, Is.EqualTo("42"));
        }

        [Test]
        public void ShouldNotBoxValueTypeVariableBeforeInvokingIndex()
        {
            // If $x is boxed each time a property is invoked, the property is invoked on the copy
            // So any state mutations won't persist for the next use of $x
            var input = "$x[5], $x[6], $x[7]";

            var context = new { x = new MutableStruct() };

            var execution = ExecuteTemplate(input, context);

            Assert.That(execution.Output, Is.EqualTo("0, 1, 2"));
        }

        [Test]
        public void ShouldNotBoxValueTypeVariableBeforeInvokingMethod()
        {
            // If $x is boxed each time a method is invoked, the method is invoked on the copy
            // So any state mutations won't persist for the next use of $x
            var input = "$x.GetCallCount(), $x.GetCallCount(), $x.GetCallCount()";
            var context = new { x = new MutableStruct() };

            var execution = ExecuteTemplate(input, context);

            Assert.That(execution.Output, Is.EqualTo("0, 1, 2"));
        }

        [Test]
        public void ShouldNotBoxValueTypePropertyBeforeInvokingProperty()
        {
            if (StaticTypingMode == StaticTypingMode.AsProvided)
                Assert.Ignore("Need to add support for wrapping structs in IStrongBox<T> to the binders");

            var input = "$StrongBox.Value.CallCount, $StrongBox.Value.CallCount, $StrongBox.Value.CallCount";
            var context = new { StrongBox = new StrongBox<MutableStruct>(new MutableStruct()) };

            var execution = ExecuteTemplate(input, context);

            Assert.That(execution.Output, Is.EqualTo("0, 1, 2"));
        }

        [Test]
        public void ShouldNotBoxValueTypePropertyBeforeSettingProperty()
        {
            if (StaticTypingMode == StaticTypingMode.AsProvided)
                Assert.Ignore("Need to add support for wrapping structs in IStrongBox<T> to the binders");

            var input = "#set($StrongBox.Value.ManualInt = 42)$StrongBox.Value.ManualInt";
            var context = new { StrongBox = new StrongBox<MutableStruct>(new MutableStruct()) };

            var execution = ExecuteTemplate(input, context);

            Assert.That(execution.Output, Is.EqualTo("42"));
            Assert.That(context.StrongBox.Value.ManualInt, Is.EqualTo(42));
        }

        [Test]
        public void ShouldNotBoxValueTypePropertyBeforeInvokingMethod()
        {
            if (StaticTypingMode == StaticTypingMode.AsProvided)
                Assert.Ignore("Need to add support for wrapping structs in IStrongBox<T> to the binders");

            var input = "$StrongBox.Value.GetCallCount(), $StrongBox.Value.GetCallCount(), $StrongBox.Value.GetCallCount()";
            var context = new { StrongBox = new StrongBox<MutableStruct>(new MutableStruct()) };

            var execution = ExecuteTemplate(input, context);

            Assert.That(execution.Output, Is.EqualTo("0, 1, 2"));
            Assert.That(context.StrongBox.Value.CallCount, Is.EqualTo(3));
        }

        public struct MutableStruct
        {
            private int _callCount;

            public int this[int i] => _callCount++;
            public int GetCallCount() => _callCount++;
            public int CallCount => _callCount++; 
            public int ManualInt { get; set; }
        }


    }
}
