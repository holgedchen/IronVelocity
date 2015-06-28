﻿using IronVelocity.Parser.AST;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace IronVelocity.Parser
{
    public class VelocityParser
    {
        private readonly Lexer _lexer;
        private Token _currentToken;

        public bool HasReachedEndOfFile { get { return CurrentToken.TokenKind == TokenKind.EndOfFile; } }

        private Token CurrentToken { get { return _currentToken; } }

        public VelocityParser(string input)
            : this(input, LexerState.Text)
        {
        }

        public VelocityParser(string input, LexerState state)
        {
            _lexer = new Lexer(input, state);
            MoveNext();
        }


        private void MoveNext()
        {
            _currentToken = _lexer.GetNextToken();
        }

        public RenderedOutputNode Parse()
        {
            var children = ImmutableList.CreateBuilder<SyntaxNode>();


            var textSoFar = new StringBuilder();
            while(true)
            {
                var token = CurrentToken;
                switch (token.TokenKind)
                {
                    case TokenKind.Text:
                        textSoFar.Append(token.Value);
                        Eat(TokenKind.Text);
                        break;
                    case TokenKind.Dollar:
                        if (textSoFar.Length > 0)
                        {
                            children.Add(new TextNode(textSoFar.ToString()));
                            textSoFar.Clear();
                        }
                        _lexer.State = LexerState.Vtl;
                        children.Add(Reference());
                        _lexer.State = LexerState.Text;
                        break;
                    case TokenKind.EndOfFile:
                        if (textSoFar.Length > 0)
                        {
                            children.Add(new TextNode(textSoFar.ToString()));
                            textSoFar.Clear();
                        }
                        return new RenderedOutputNode(children.ToImmutableArray());
                    default:
                        if (String.IsNullOrEmpty(token.Value))
                            throw new Exception("Token value not defined");
                        textSoFar.Append(token.Value);
                        MoveNext();
                        break;
                }
            }

        }



        protected virtual ReferenceNode Reference()
        {

            Eat(TokenKind.Dollar);
            bool isSilent = TryEat(TokenKind.Exclamation);
            bool isFormal = TryEat(TokenKind.LeftCurley);

            var identifier = Eat(TokenKind.Identifier);

            //Root variable
            ReferenceNodePart value = Variable(identifier);

            //Methods & Properties
            while (TryEat(TokenKind.Dot))
            {
                identifier = Eat(TokenKind.Identifier);

                if (TryEat(TokenKind.LeftParenthesis))
                {
                    value = Method(value, identifier);
                }
                else
                {
                    value = Property(value, identifier);
                }
            }

            if (isFormal)
            {
                Eat(TokenKind.RightCurley);
            }

            return new ReferenceNode
            {
                IsSilent = isSilent,
                IsFormal = isFormal,
                Value = value,
            };

        }

        protected virtual Variable Variable(Token identifier)
        {
            return new Variable {
                Name = identifier.Value
            };
        }

        protected virtual Method Method(ReferenceNodePart target, Token identifier)
        {
            return new Method {
                Name = identifier.Value, 
                Target = target,
                Arguments = ArgumentList(TokenKind.RightParenthesis)
            };
        }

        protected virtual Property Property(ReferenceNodePart target, Token identifier)
        {
            return new Property {
                Name = identifier.Value,
                Target = target
            };
        }

        private void AddRemainingArguments(ICollection<ExpressionNode> arguments, TokenKind closingToken)
        {
            if (!TryEat(closingToken))
            {
                do
                {
                    arguments.Add(Expression());
                }
                while (TryEat(TokenKind.Comma));
                Eat(closingToken);
            }
        }

        public ArgumentsNode ArgumentList(TokenKind closingToken)
        {
            TryEatWhitespace();

            var builder = ImmutableList.CreateBuilder<ExpressionNode>();
            AddRemainingArguments(builder, closingToken);

            return new ArgumentsNode { Arguments = builder.ToImmutableArray() };
        }

        protected virtual ExpressionNode Number()
        {
            bool isNegative = TryEat(TokenKind.Minus);

            var numberToken = Eat(TokenKind.NumericLiteral);

            var integerPart = isNegative
                ? "-" + numberToken.Value
                : numberToken.Value;

            if(TryEat(TokenKind.Dot))
                return FloatingPointLiteral(integerPart);
            else
                return IntegerLiteral(integerPart);
        }

        protected virtual IntegerLiteralNode IntegerLiteral(string integerPart)
        {
            var integerValue = int.Parse(integerPart);
            return new IntegerLiteralNode { Value = integerValue };
        }

        protected virtual FloatingPointLiteralNode FloatingPointLiteral(string integerPart)
        {
            var numberToken = Eat(TokenKind.NumericLiteral);
            var fractionalPart = numberToken.Value;

            var floatValue = float.Parse(integerPart + "." + fractionalPart);
            return new FloatingPointLiteralNode { Value = floatValue };

        }

        protected virtual StringNode StringLiteral()
        {
            var token = Eat(TokenKind.StringLiteral);
            return new StringNode
            {
                IsInterpolated = false,
                Value = token.Value
            };
        }

        protected virtual StringNode InterpolatedString()
        {
            var token = Eat(TokenKind.InterpolatedStringLiteral);
            return new StringNode
            {
                IsInterpolated = true,
                Value = token.Value
            };

        }

        protected virtual UnaryExpressionNode Not()
        {
            Eat(TokenKind.Exclamation);

            return new UnaryExpressionNode{
                Operation = UnaryOperation.Not,
                Value = Expression()
            };

        }

        protected virtual UnaryExpressionNode Parenthesised()
        {
            Eat(TokenKind.LeftParenthesis);
            var result = new UnaryExpressionNode
            {
                Operation = UnaryOperation.Parenthesised,
                Value = Expression()
            };
            Eat(TokenKind.RightParenthesis);
            return result;
        }

        public virtual ExpressionNode Expression()
        { 
            TryEatWhitespace();

            ExpressionNode result;
            switch (CurrentToken.TokenKind)
            {
                case TokenKind.LeftParenthesis:
                    result = Parenthesised();
                    break;
                case TokenKind.Exclamation:
                    result = Not();
                    break;
                case TokenKind.Dollar:
                    result = Reference();
                    break;
                case TokenKind.Minus: //Negative numbers
                case TokenKind.NumericLiteral:
                    result = Number();
                    break;
                case TokenKind.StringLiteral:
                    result = StringLiteral();
                    break;
                case TokenKind.InterpolatedStringLiteral:
                    result = InterpolatedString();
                    break;
                case TokenKind.Identifier:
                    result = BooleanLiteralOrWord();
                    break;
                case TokenKind.LeftSquareBracket:
                    result = RangeOrList();
                    break;
                case TokenKind.EndOfFile:
                    throw new Exception("Unexpected end of file");
                case TokenKind.LeftCurley: //Dictionary

                    throw new NotImplementedException(String.Format("Can't yet parse token {0} starting an expression", CurrentToken.TokenKind));
                default:
                    throw new Exception("Unrecognised token parsing an expression: " + CurrentToken.TokenKind);
            }

            TryEatWhitespace();

            return result;
        }

        private bool HasHigherPrecedence(BinaryOperation left, BinaryOperation right)
        {
            left &= (BinaryOperation)0xFFFF0000;
            right &= (BinaryOperation)0xFFFF0000;

            return left > right;
        }

        public virtual ExpressionNode CompoundExpression()
        {
            var soFar = Expression();
            BinaryExpressionNode soFarBinary = null;
            while (true)
            {
                TokenKind tokenKind = CurrentToken.TokenKind;
                BinaryOperation operation;
                switch (tokenKind)
                {
                    case TokenKind.Plus:
                        operation = BinaryOperation.Adddition;
                        break;
                    case TokenKind.Minus:
                        operation = BinaryOperation.Subtraction;
                        break;
                    case TokenKind.Multiply:
                        operation = BinaryOperation.Multiplication;
                        break;
                    case TokenKind.Divide:
                        operation = BinaryOperation.Division;
                        break;
                    case TokenKind.Modulo:
                        operation = BinaryOperation.Modulo;
                        break;
                    default:
                        return soFar;
                }
                Eat(tokenKind);
                var operand = Expression();

                if (soFarBinary != null && HasHigherPrecedence(operation, soFarBinary.Operation))
                {
                    soFar = soFarBinary = new BinaryExpressionNode
                    {
                        Left = soFarBinary.Left,
                        Operation = soFarBinary.Operation,
                        Right = new BinaryExpressionNode {
                            Left = soFarBinary.Right,
                            Right = operand,
                            Operation = operation
                        }
                    };
                }
                else
                { 
                    soFar= soFarBinary = new BinaryExpressionNode
                    {
                        Left = soFar,
                        Right = operand,
                        Operation = operation
                    };
                }

            }
        }

        protected virtual ExpressionNode BooleanLiteralOrWord()
        {
            var token = Eat(TokenKind.Identifier);
            var value = token.Value;
            if (value == "true")
                return BooleanLiteralNode.True;
            else if (value == "false")
                return BooleanLiteralNode.False;
            else
                return new WordNode { Name = value };
        }

        protected virtual ExpressionNode RangeOrList()
        {
            Eat(TokenKind.LeftSquareBracket);

            TryEatWhitespace();

            if (TryEat(TokenKind.RightSquareBracket))
                return new ListExpressionNode(new ExpressionNode[0]);

            var firstArg = Expression();

            if (TryEat(TokenKind.DotDot))
                return Range(firstArg);
            else if (TryEat(TokenKind.Comma))
                return List(firstArg);
            else if (TryEat(TokenKind.RightSquareBracket))
                return new ListExpressionNode(firstArg);
            else
                throw UnexpectedTokenException(TokenKind.DotDot, TokenKind.Comma, TokenKind.RightSquareBracket, TokenKind.Whitespace);
        }

        private ListExpressionNode List(ExpressionNode firstValue)
        {
            var builder = ImmutableList.CreateBuilder<ExpressionNode>();
            builder.Add(firstValue);

            AddRemainingArguments(builder, TokenKind.RightSquareBracket);

            return new ListExpressionNode(builder.ToImmutableArray());
        }

        private BinaryExpressionNode Range(ExpressionNode left)
        {
            var right = Expression();
            Eat(TokenKind.RightSquareBracket);

            return new BinaryExpressionNode { Left = left, Right = right, Operation = BinaryOperation.Range };
        }

        private Token Eat(TokenKind tokenKind)
        {
            var token = CurrentToken;
            if (CurrentToken.TokenKind != tokenKind)
            {
                throw UnexpectedTokenException(tokenKind);
            }
            MoveNext();
            return token;
        }

        private bool TryEat(TokenKind tokenKind)
        {
            if (CurrentToken.TokenKind == tokenKind)
            {
                MoveNext();
                return true;
            }
            else
            {
                return false;
            }
        }

        private Exception UnexpectedTokenException(TokenKind expectedToken)
        {
            //TODO: Throw specific exception type, include line & column data.
            return new Exception(String.Format("Unexpected Token {0}. Expected {1}.", CurrentToken.TokenKind, expectedToken));
        }

        private Exception UnexpectedTokenException(params TokenKind[] expectedTokenKinds)
        {
            //TODO: Throw specific exception type, include line & column data.
            return new Exception(String.Format("Unexpected Token {0}. Expected one of: {1}.", CurrentToken.TokenKind, String.Join(",", expectedTokenKinds)));
        }

        private bool TryEatWhitespace()
        {
            return TryEat(TokenKind.Whitespace);
        }
        

    }

}
