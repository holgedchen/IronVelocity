﻿using IronVelocity.Parser;
using IronVelocity.Parser.AST;
using NUnit.Framework;

namespace IronVelocity.Tests.ParserTests
{
    [TestFixture]
    public class ListExpressionTests
    {
        [TestCase("[]", 0)]
        [TestCase("[123]", 1)]
        [TestCase("['test',4.5,$variable,true]", 4)]
        public void ListHasExpectedNumberOfNestedExpressionCalls(string input, int elementCount)
        {
            var parser = new VelocityParserWithStatistics(input, LexerState.Vtl);
            var result = parser.Expression();

            Assert.That(parser.RangeOrListCallCount, Is.EqualTo(1));
            Assert.That(parser.ExpressionCallCount, Is.EqualTo(elementCount + 1));
            Assert.That(parser.HasReachedEndOfFile, Is.True);

            Assert.That(result, Is.TypeOf<ListExpressionNode>());
            var node = (ListExpressionNode)result;
            Assert.That(node.Elements.Count, Is.EqualTo(elementCount));
        }

        [Test]
        public void NestedLists()
        {
            var input = "[[123]]";
            var parser = new VelocityParserWithStatistics(input, LexerState.Vtl);
            var result = parser.Expression();

            Assert.That(parser.RangeOrListCallCount, Is.EqualTo(2));
            Assert.That(parser.ExpressionCallCount, Is.EqualTo(3));
            Assert.That(parser.HasReachedEndOfFile, Is.True);


            Assert.That(result, Is.TypeOf<ListExpressionNode>());
            var outerList = (ListExpressionNode)result;

            Assert.That(outerList.Elements.Count, Is.EqualTo(1));
            Assert.That(outerList.Elements[0], Is.TypeOf<ListExpressionNode>());

            var innerList = (ListExpressionNode)outerList.Elements[0];
            Assert.That(innerList.Elements.Count, Is.EqualTo(1));

            Assert.That(innerList.Elements[0], Is.TypeOf<IntegerLiteralNode>());
            var innerListValue = (IntegerLiteralNode)innerList.Elements[0];

            Assert.That(innerListValue.Value, Is.EqualTo(123));
        }


        [Test]
        public void SameListExpressionInstanceReturnedIfElementsDontChange()
        {
            var list = new ListExpressionNode(new ExpressionNode[0]);
            var updated = list.Update(list.Elements);

            Assert.That(updated, Is.EqualTo(list));
        }

        [Test]
        public void DifferentListExpressionInstanceReturnedIfElementsDontChange()
        {
            var list = new ListExpressionNode(new ExpressionNode[0]);
            var updated = list.Update(new ExpressionNode[] { null });

            Assert.That(updated, Is.Not.EqualTo(list));
        }
    }
}
