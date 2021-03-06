﻿using IronVelocity.Parser;
using NUnit.Framework;

namespace IronVelocity.Tests.Parser
{
    public class AssignmentTests : ParserTestBase
    {
        [TestCase("$x=123", "$x", "123")]
        [TestCase("$x.prop=123", "$x.prop", "123")]
        [TestCase("$x.method().prop=123", "$x.method().prop", "123")]
        [TestCase("$x = 123", "$x", "123")]
        [TestCase(" $x = 123 ", "$x", "123")]
        [TestCase("$x = $y.stuff(123)", "$x", "$y.stuff(123)")]
        public void ParseAssignmentExpression(string input, string left, string right)
        {
            ParseBinaryExpressionTest(input, left, right, VelocityParser.Assign, x => x.assignment());
        }
    }
}
