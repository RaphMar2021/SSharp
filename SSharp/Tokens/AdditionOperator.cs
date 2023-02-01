﻿using SSharp.VM;
using System;

namespace SSharp.Tokens
{
    public class AdditionOperator : Operator
    {
        public override string ToString()
        {
            return "+";
        }

        public override VMObject Operate(VMObject a, VMObject b)
        {
            if (a is VMNumber numA && b is VMNumber numB)
            {
                return new VMNumber(numA.Value + numB.Value);
            }
            if (a is VMString strA && b is VMString strB)
            {
                return new VMString(strA.Value + strB.Value);
            }
            throw new Exception($"Cannot add {a.ToString()} and {b.ToString()}.");
        }
    }
}
