﻿using IronVelocity.Parser;
using NUnit.Framework;

namespace IronVelocity.Tests.Parser
{
    public class BooleanLiteralTests : ParserTestBase
    {
        [TestCase("true")]
        [TestCase("false")]
        public void ParseBooleanLiteral(string input)
        {
            var result = Parse(input, x => x.primaryExpression(), VelocityLexer.ARGUMENTS);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.GetText(), Is.EqualTo(input));
        }
    }
}
