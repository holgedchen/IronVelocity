﻿using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronVelocity.RuntimeHelpers;

namespace IronVelocity.Tests.RuntimeHelpers
{
    public class AdditionTests
    {
        [TestCase(3, 5, 8, TestName = "Addition Positive Integer")]
        [TestCase(-3, -5, -8, TestName = "Addition Negative Integer")]
        [TestCase(5, -3, 2, TestName = "Addition Mixed Integers")]
        [TestCase(null, 5, null, TestName = "Addition Null Left")]
        [TestCase(2, null, null, TestName = "Addition Null Right")]
        [TestCase(null, null, null, TestName = "Addition Null Both")]
        [TestCase(1f, 4, 5f, TestName = "Addition Integer Float")]
        [TestCase(2, 5f, 7f, TestName = "Addition Float Integer")]
        //[TestCase(2147483647, 1, 2147483648, TestName = "Addition Integer Overflow")]
        //[TestCase(-2147483648, -1, -2147483649, TestName = "Addition Integer Underflow")]
        public void BasicTest(object left, object right, object expected)
        {
            var result = Operators.Addition(left, right);

            Assert.AreEqual(expected, result);
        }

        [Test]
        public void AdditionOperatorOverload()
        {
            var left = new OverloadedAdd(1);
            var right = new OverloadedAdd(3);
            var result = Operators.Addition(left, right);

            Assert.IsInstanceOf<OverloadedAdd>(result);
            Assert.AreEqual(4, ((OverloadedAdd)result).Value);
        }



        public class OverloadedAdd
        {
            public int Value { get; private set; }
            public OverloadedAdd(int value)
            {
                Value = value;
            }

            public static OverloadedAdd operator +(OverloadedAdd left, OverloadedAdd right)
            {
                return new OverloadedAdd(left.Value + right.Value);
            }
        }
    }
}