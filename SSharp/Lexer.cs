﻿using SSharp.Tokens;
using System;
using System.Collections.Generic;

namespace SSharp
{
    public class Lexer
    {
        // Lexer state.
        private Block rootToken;
        private Token current;
        private List<Token> stack;
        private string sequence;

        private string[] keywords = new string[]
        {
            "if",
            "else",
            "while",
            "true",
            "false",
            "null",
            "def",
            "for",
            "in"
        };

        // Operators should be longest to shortest here.
        // Excludes alphabetical operators. (i.e. 'and', 'or'.)
        private string[] operators = new string[]
        {
            "==",
            "!=",
            ">=",
            "<=",
            "..",
            ">",
            "<",
            "+",
            "-",
            "*",
            "/",
            "%"
        };

        private Token GetSequenceToken(string sequence)
        {
            foreach (var key in keywords)
            {
                if (key == sequence)
                {
                    return new Keyword(key);
                }
            }
            switch (sequence)
            {
                case ",":
                    return new Comma();
                case "==":
                    return new EqualsOperator();
                case "!=":
                    return new NotEqualsOperator();
                case ">=":
                    return new GreaterEqualOperator();
                case "<=":
                    return new LessEqualOperator();
                case "..":
                    return new RangeOperator();
                case ">":
                    return new GreaterOperator();
                case "<":
                    return new LessOperator();
                case "+":
                    return new AdditionOperator();
                case "-":
                    return new SubtractionOperator();
                case "*":
                    return new MultiplicationOperator();
                case "/":
                    return new DivisionOperator();
                case "%":
                    return new RemainderOperator();
                case "and":
                    return new AndOperator();
                case "or":
                    return new OrOperator();
                case "not":
                    return new NotOperator();
            }
            if (double.TryParse(sequence, out double n))
            {
                return new NumberLiteral(n);
            }
            return new Identifier(sequence);
        }

        private void GoUp()
        {
            if (current is ContainerToken container)
            {
                if (current.Parent == null)
                {
                    throw new Exception("Mismatched braces.");
                }
                current = current.Parent;
            }
            else
            {
                current = stack[stack.Count - 1];
                stack.RemoveAt(stack.Count - 1);
            }
        }

        private void Nest(Token newToken)
        {
            if (current is ContainerToken currentContainer)
            {
                currentContainer.AddToken(newToken);
                if (newToken is not ContainerToken)
                {
                    stack.Add(current);
                }
                current = newToken;
            }
            else
            {
                throw new Exception("Cannot nest within non-container tokens.");
            }
        }

        private void EndSequence()
        {
            if (!string.IsNullOrWhiteSpace(sequence) && current is ContainerToken containerToken)
            {
                Token token = GetSequenceToken(sequence);
                ((ContainerToken)current).AddToken(token);
            }
            sequence = string.Empty;
        }

        private void CloseAssignment()
        {
            if (current is Assignment)
            {
                GoUp();
            }
        }

        public Block Lex(string source)
        {
            rootToken = new Block();
            current = rootToken;
            stack = new();
            sequence = string.Empty;

            bool inComment = false;
            for (int i = 0; i < source.Length; i++)
            {
                char c = source[i];
                char nextChar = '\0';
                if (i < source.Length - 1)
                {
                    nextChar = source[i + 1];
                }

                if (inComment)
                {
                    if (c == '\n' || c == '\r')
                    {
                        inComment = false;
                    }
                    else
                    {
                        continue;
                    }
                }

                if (current is not StringLiteral)
                {
                    string substr = source.Substring(i);
                    bool shouldContinue = false;
                    foreach (string @operator in operators)
                    {
                        if (substr.StartsWith(@operator))
                        {
                            EndSequence();
                            sequence += @operator;
                            EndSequence();
                            i += @operator.Length - 1;
                            shouldContinue = true;
                            break;
                        }
                    }
                    if (shouldContinue)
                    {
                        continue;
                    }
                }

                switch (c)
                {
                    case '#' when current is not StringLiteral:
                        inComment = true;
                        break;
                    case '=' when current is not StringLiteral && current is not Parentheses:
                        EndSequence();
                        Nest(new Assignment());
                        break;
                    case ',' when current is not StringLiteral:
                        EndSequence();
                        sequence += c;
                        EndSequence();
                        break;
                    case '(' when current is not StringLiteral:
                        EndSequence();
                        Nest(new Parentheses());
                        break;
                    case ')' when current is not StringLiteral:
                        EndSequence();
                        if (current is Parentheses)
                        {
                            GoUp();
                        }
                        else
                        {
                            throw new Exception("Cannot close parentheses outside of parentheses.");
                        }
                        break;
                    case '[' when current is not StringLiteral:
                        EndSequence();
                        Nest(new Block());
                        break;
                    case ']' when current is not StringLiteral:
                        EndSequence();
                        CloseAssignment();
                        if (current is Block)
                        {
                            GoUp();
                        }
                        else
                        {
                            throw new Exception("Too many closing braces.");
                        }
                        break;
                    case '"':
                        if (current is StringLiteral stringLiteral)
                        {
                            stringLiteral.Value = sequence;
                            sequence = string.Empty;
                            GoUp();
                        }
                        else if (current is ContainerToken containerToken)
                        {
                            EndSequence();
                            Nest(new StringLiteral());
                        }
                        break;
                    case char whitespace when char.IsWhiteSpace(c) && current is ContainerToken containerToken1:
                        EndSequence();
                        if (c == '\n' || c == '\r')
                        {
                            CloseAssignment();
                        }
                        break;
                    default:
                        sequence += c;
                        break;
                }
            }

            EndSequence();
            CloseAssignment();

            if (current != rootToken)
            {
                throw new Exception($"Unclosed {current.ToString()}.");
            }

            return rootToken;
        }
    }
}
