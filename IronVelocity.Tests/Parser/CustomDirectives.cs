﻿using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronVelocity.Tests.Parser
{
    public class CustomDirectives : ParserTestBase
    {
        [Test]
        public void ShouldParseSingleLineCustomDirectiveWithNoArguments()
        {
            var input = "#test";

            var result = Parse(input, x => x.custom_directive_single_line());

            Assert.That(result, Is.Not.Null);
            Assert.That(result.IDENTIFIER()?.GetText(), Is.EqualTo("test"));

        }

        [TestCase("#custom()")]
        [TestCase("#custom(   )")]
        [TestCase("#custom( 123, 456 )")]
        public void ShouldParseSingleLineCustomDirectiveWithArguments(string input)
        {
            var result = Parse(input, x => x.custom_directive_single_line());

            Assert.That(result, Is.Not.Null);
            Assert.That(result.IDENTIFIER()?.GetText(), Is.EqualTo("custom"));

            Assert.That(result.argument_list(), Is.Not.Null);
        }
    }
}
