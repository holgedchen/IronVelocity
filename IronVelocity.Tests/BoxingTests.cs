﻿using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tests;

namespace IronVelocity.Tests
{
    /// <summary>
    /// This class tests that we are properly unboxing any boxed value types before accessing child members which may mutate the value type
    /// 
    /// Remember that value types (primitives & structs) are passed by value rather than by reference.  
    /// If we fail to properly unbox an object before calling a member which mutates the value type
    /// we will be modifying the boxed copy, and not the underlying value type.
    /// </summary>
    public class BoxingTests
    {
        [Test]
        public void BoxTestWithPropery()
        {
            var context = new Dictionary<string, object>();
            context["x"] = new TestStruct();
            Utility.TestExpectedMarkupGenerated("$x.CallCount, $x.CallCount, $x.CallCount", "0, 1, 2", context);

            var x = (TestStruct)context["x"];
            Assert.AreEqual(3, x.CallCount);
        }

        [Test]
        public void BoxTestWithMethod()
        {
            var context = new Dictionary<string, object>();
            context["x"] = new TestStruct();
            Utility.TestExpectedMarkupGenerated("$x.GetCallCount(), $x.GetCallCount(), $x.GetCallCount()", "0, 1, 2", context);

            var x = (TestStruct)context["x"];
            Assert.AreEqual(3, x.CallCount);
        }

        public struct TestStruct
        {
            private int _callCount;
            public int GetCallCount() { return _callCount++; }
            public int CallCount { get { return _callCount++; } }
        }


    }
}