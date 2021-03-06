﻿using IronVelocity.Parser;
using NUnit.Framework;
using System;

namespace IronVelocity.Tests.Parser
{
    public class PrimaryExpressionTests : ParserTestBase
    {
        [TestCase("123", typeof(VelocityParser.IntegerLiteralContext))]
        [TestCase("7.4", typeof(VelocityParser.FloatingPointLiteralContext))]
        [TestCase("true", typeof(VelocityParser.BooleanLiteralContext))]
        [TestCase("'string'", typeof(VelocityParser.StringExpressionContext))]
        [TestCase("\"interpolated\"", typeof(VelocityParser.StringExpressionContext))]
        [TestCase("[]", typeof(VelocityParser.ListContext))]
        [TestCase("[1..3]", typeof(VelocityParser.RangeContext))]
        public void ParsePrimaryExpression(string input, Type parsedNodeType)
        {
            var result = Parse(input, x => x.expression(), VelocityLexer.EXPRESSION);

            Assert.That(result, Is.InstanceOf(parsedNodeType));
            Assert.That(result.GetText(), Is.EqualTo(input));
        }
    }
}
