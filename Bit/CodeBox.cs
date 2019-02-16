// Copyright 2011-2019 by Jeremy Spiller
// See Lincense.txt for details

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Globalization;

namespace Gosub.Bit
{

    /// <summary>
    /// Master code generator.
    /// </summary>
    class CodeBox
    {
        const int				MAX_ARRAY_SIZE = 256;

        int						mGatesInUnlinkedCode = -1;
        int						mOptimizedConst;
        int						mOptimizedConstExpr;
        int						mOptimizedUnary;
        List<IfScope>			mIfScopes = new List<IfScope>();
        List<LinkBox>			mLinks = new List<LinkBox>();

        public bool				Error;
        public SyntaxBox		ParseBox;
        public List<OpCodeExpr> Code = new List<OpCodeExpr>();
        public List<OpCodeExpr> LinkedCode;

        // Symbols
        public Symbol		Symbol;
        public List<Symbol>	Params = new List<Symbol>();
        public List<Symbol>	Locals = new List<Symbol>();

        // Sub-boxes (TBD: Move to CodeUnit)
        public List<Symbol> Boxes = new List<Symbol>();

        SymbolTable			mSymbolTable;


        public override string ToString()
        {
            return ParseBox == null ? "(no name)" : ParseBox.ToString();
        }


        /// <summary>
        /// Find a declaration, given the token (returns NULL if it was not found)
        /// </summary>
        Symbol FindSymbol(Token token)
        {
            return mSymbolTable.FindSymbol(token);
        }

        /// <summary>
        /// Add a symbol.  
        /// Returns TRUE if the symbol was added (or FALSE on duplicate).
        /// Errors are marked with an error message on the variable name token.
        /// </summary>
        void AddSymbol(Symbol symbol)
        {
            // Verify we don't already have this symbol
            Symbol duplicate;
            if (!mSymbolTable.AddSymbol(symbol, out duplicate))
            {
                // Duplicate symbol
                symbol.Decl.VariableName.Reject("This symbol is already defined");
                Error = true;
            }
        }

        /// <summary>
        /// Returns the size of box (in gates + 1 for each additional input)
        /// </summary>
        public int GatesInUnlinkedCode()
        {
            // Return cached value (if we have it)
            if (mGatesInUnlinkedCode >= 0)
                return mGatesInUnlinkedCode;
            mGatesInUnlinkedCode = 0;  // Prevent infinite recursion

            // Count gates in this box
            int count = 0;
            foreach (OpCodeExpr expr in Code)
                expr.CountedNegative = false;

            foreach (OpCodeExpr expr in Code)
                if (expr.Expr != null)
                    count += expr.Expr.CountGates();

            foreach (LinkBox link in mLinks)
            {
                foreach (OpCodeExpr expr in link.Parameters)
                    expr.CountedNegative = false;
                foreach (OpCodeExpr expr in link.Parameters)
                    if (expr.Expr != null)
                        count += expr.Expr.CountGates();
                count += link.CodeBox.GatesInUnlinkedCode();
            }
            mGatesInUnlinkedCode = count;
            return mGatesInUnlinkedCode;
        }


        /// <summary>
        /// Returns the size of box after optimization
        /// (in gates + 1 for each additional input)
        /// </summary>
        public int GatesInLinkedCode()
        {
            int count = 0;
            foreach (OpCodeExpr expr in LinkedCode)
                expr.CountedNegative = false;
            foreach (OpCodeExpr expr in LinkedCode)
                if (expr != null)
                    count += expr.Expr.CountGates();
            return count;
        }


        /// <summary>
        /// Reject a token, and keep track of errors
        /// </summary>
        RValue Reject(Token token, string message)
        {
            token.Reject(message);
            Error = true;
            return RValue.Failed;
        }

        /// <summary>
        /// Reject an expression, and keep track of errors
        /// </summary>
        RValue Reject(SyntaxExpr expr, string message)
        {
            return Reject(expr.Function, message);
        }

        /// <summary>
        /// Converts to the given value from an integer to bit.
        /// If there is an error, mark op with the error message.
        /// Returns TRUE for success, and the new base type is "bit"
        /// </summary>
        bool ConvertIntToBits(SyntaxExpr op, ref RValue value, int numBits, bool checkForOverflow)
        {
            if (checkForOverflow)
            {
                long max = numBits <= 0 ? 0 : 1L << numBits;
                if (value.IntValue >= max || value.IntValue < -max/2)
                {
                    Reject(op, "Overflow: The value " + value.IntValue + 
                                " is too big/small to fit in " + numBits + " bits");
                    value.Success = false;
                    return false;
                }
            }

            value.BaseType = "bit";
            value.BitValue = new List<OpCode>();
            for (int i = 0; i < numBits; i++)
            {
                value.BitValue.Add(new OpCodeConst((int)value.IntValue & 1));
                value.IntValue >>= 1;
            }
            value.IntValue = 0;
            return true;
        }


        /// <summary>
        /// Generate an op-code, given the op-code and two operands.
        /// This function performs some very basic non-recursive optimization:
        ///		Remove parenthisis for associative operator/operand pairs
        ///		Remove constants (A*0=0, A*1=A, etc.)
        ///	Doing this at code generation time gives us a lot of bang for
        ///	the buck.  It doesn't take much time, but eliminates a lot of
        ///	otherwise wasted memory and compile time.
        /// </summary>
        OpCode GenOp(OpCodeMath op, OpCode p1, OpCode p2)
        {
            Debug.Assert(op.Operands == null);

            // Math type must be associative
            Type opType = op.GetType();
            Type p1Type = p1.GetType();
            Type p2Type = p2.GetType();
            Debug.Assert(opType == typeof(OpCodeOr)
                            || opType == typeof(OpCodeAnd)
                            || opType == typeof(OpCodeXor));

            // Check for p1 associative
            if (opType == p1Type && p1.Negate == OpState.Zero)
            {
                // P1 is associative - use the operand list
                op.Operands = ((OpCodeMath)p1).Operands;
            }
            else
            {
                // P1 is not associative (make new operand list, append p1 as parameter)
                op.Operands = new List<OpCode>();
                op.Operands.Add(p1);
            }

            // Check for p2 associative
            if (opType == p2Type && p2.Negate == OpState.Zero)
            {
                // P2 is associative - append the operand list
                foreach (OpCode operand in ((OpCodeMath)p2).Operands)
                    op.Operands.Add(operand);
            }
            else
            {
                // P2 is not associative (append p2 as parameter)
                op.Operands.Add(p2);
            }
            return Optimizer.RemoveConstNR(op, ref mOptimizedConst,
                                               ref mOptimizedConstExpr,
                                               ref mOptimizedUnary);
        }

        /// <summary>
        /// Make an operator for '+', '*', '#'
        /// </summary>
        OpCode GenOp(string opType, OpCode p1, OpCode p2)
        {
            switch (opType)
            {
                case "+": return GenOp(new OpCodeOr(), p1, p2);
                case "*": return GenOp(new OpCodeAnd(), p1, p2);
                case "#": return GenOp(new OpCodeXor(), p1, p2);
            }
            return null;
        }


        /// <summary>
        /// Promotes the operands of the left and right bits to be
        /// the same length (if allowed by 'dup').
        /// Returns TRUE if it worked and array lengths are the same,
        /// or FALSE if an error has been detected (will be reported)
        /// </summary>
        bool PromoteOperatorBitBit(SyntaxExpr expr, ref RValue left, ref RValue right)
        {
            // Generate a 'dup' if necessary (and allowed)
            if (left.BitCount == 1 && right.BitCount != 1 && left.AllowDup != null)
                if (!GenerateDup(left.AllowDup, ref left, right.BitCount))
                    return false;

            // Generate a 'dup' if necessary (and allowed)
            if (right.BitCount == 1 && left.BitCount != 1 && right.AllowDup != null)
                if (!GenerateDup(right.AllowDup, ref right, left.BitCount))
                    return false;

            // If the bit array is a single bit and could have been expanded 
            // with 'dup', give the user a different error message
            if (left.BitCount == 1 && right.BitCount != 1
                || right.BitCount == 1 && left.BitCount != 1)
            {
                Reject(expr, "Error: Needs 'dup' function.  Operator '"
                                    + expr.Function
                                    + "' can not be applied to operands of type '"
                                    + left.GetTypeName() + "' and '" + right.GetTypeName()
                                    + " unless 'dup' is used on the single bit operand.");
                return false;
            }

            // Verify parameters are correct array sizes 
            if (left.BitCount == 0 || right.BitCount == 0
                    || right.BitCount != left.BitCount)
            {
                Reject(expr, "Error: Operator '" 
                                    + expr.Function 
                                    + "' can not be applied to operands of type '"
                                    + left.GetTypeName() + "' and '" + right.GetTypeName()
                                    + "' (array lengths are incompatible)");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Generate operator '=' (bit,bit)
        /// </summary>
        RValue GenerateAssign_Bit_BitInt(SyntaxExpr expr, RValue left, RValue right)
        {
            // Verify types
            if (left.BaseType != "bit"
                    || right.BaseType != "bit" && right.BaseType != "int")
                return Reject(expr, "Can not perform '"
                                    + expr.Function 
                                    + "' operator on '"
                                    + left.GetTypeName() + "' and '" + right.GetTypeName()
                                    + "' (types are incompatible)");

            // Optionally convert int to bit
            if (right.BaseType == "int")
                if (!ConvertIntToBits(expr, ref right, left.BitCount, true))
                    return RValue.Failed;

            // Do not allow DUP on left side
            if (left.AllowDup != null)
                return Reject(left.AllowDup, "Dup not allowed on left side of assignment operator");

            // Promote the operator arrays to the same length (or give error)
            if (!PromoteOperatorBitBit(expr, ref left, ref right))
                return RValue.Failed;

            // Perform assignment (detect errors at the same time)
            for (int i = 0; i < left.BitCount; i++)
            {
                OpCodeTerminal opAssign = left.BitValue[i] as OpCodeTerminal;

                // Ensure we have an LValue that can be assigned to (an OpCodeExpr)
                if (opAssign == null)
                    return Reject(expr, "Left hand side can not be assigned (wrong expression type)");

                // Perform assignment (must already be un-assigned)
                if (opAssign.Expression.Expr == null)
                {
                    // Assign
                    opAssign.Expression.Expr = right.BitValue[i];

                    // Optionally save to IF scope, and generate product
                    if (mIfScopes.Count != 0)
                    {
                        // Save
                        IfScope scope = mIfScopes[mIfScopes.Count-1];
                        scope.Assigned.Add(opAssign.Expression);

                        // Generate product (for sum of products later)
                        opAssign.Expression.Expr = GenOp(new OpCodeAnd(),
                                                    opAssign.Expression.Expr,
                                                    new OpCodeTerminal(Code[scope.MasterConditionIndex]));
                    }
                }
                else
                {
                    // Bit already assigned
                    return Reject(expr, "Left hand side was already assigned"
                            + (opAssign.Expression.Expr as OpCodeInParam == null 
                                    ? "" : " (as an 'in' parameter)"));
                }
            }

            return RValue.Failed;
        }

        /// <summary>
        /// Generate code for "==" and "!=" (bit,bit), (bit,int), or (int,bit)
        /// </summary>
        RValue GenerateCompare_BitInt_BitInt(SyntaxExpr expr, RValue left, RValue right)
        {
            // Optionally convert int to bit
            if (right.BaseType == "int")
                if (!ConvertIntToBits(expr, ref right, left.BitCount, true))
                    return RValue.Failed;

            // Optionally convert int to bit
            if (left.BaseType == "int")
                if (!ConvertIntToBits(expr, ref left, right.BitCount, true))
                    return RValue.Failed;

            // Verify array lengths
            if (left.BitCount == 0 
                || right.BitCount == 0
                || right.BitCount != left.BitCount)
                return Reject(expr, "Error: Operator '" 
                                    + expr.Function 
                                    + "' can not be applied to operands of type '"
                                    + left.GetTypeName() + "' and '" + right.GetTypeName()
                                    + "' (array lengths are incompatible)");

            RValue result = new RValue("bit");
            result.BitValue = new List<OpCode>(1);

            // Create the "==" operator
            if (left.BitCount == 1)
            {
                // Single bit comparision
                // (left == right) = !(left # right)
                result.BitValue.Add(NegOp(GenOp(new OpCodeXor(), left.BitValue[0], right.BitValue[0])));
            }
            else
            {
                // Multi-bit comparison
                // (left == right) = !(left[0]#right[0]) * !(left[1]#right[1]) ...
                OpCodeAnd andResult = new OpCodeAnd(new List<OpCode>(left.BitCount));
                for (int i = 0; i < left.BitCount; i++)
                    andResult.Operands.Add(NegOp(GenOp(new OpCodeXor(), left.BitValue[i], right.BitValue[i])));
                result.BitValue.Add(Optimizer.RemoveConstNR(andResult,
                                                    ref mOptimizedConst,
                                                    ref mOptimizedConstExpr,
                                                    ref mOptimizedUnary));
            }

            // For "!=", negate the output
            if (expr.Function == "!=")
                result.BitValue[0].Negate ^= OpState.One;

            return result;
        }


        /// <summary>
        /// Generate a bit math operation: '+', '*', '#' (bit,bit)
        /// </summary>
        RValue GenerateMath_BitBit(SyntaxExpr expr, RValue left, RValue right)
        {
            // Promote the operator arrays to the same length (or give error)
            if (!PromoteOperatorBitBit(expr, ref left, ref right))
                return RValue.Failed;

            // Create expression result
            RValue result = new RValue("bit");
            result.BitValue = new List<OpCode>(left.BitCount);

            // Create math function
            for (int i = 0; i < left.BitCount; i++)
            {
                // Generate an operand
                OpCode op = GenOp(expr.Function, left.BitValue[i], right.BitValue[i]);
                if (op == null)
                    return Reject(expr, "Compiler error: Unrecognized symbol while generating code");
                result.BitValue.Add(op);
            }

            return result;
        }


        /// <summary>
        /// Generate operator '!'
        /// </summary>
        RValue GenerateNot_Bit(SyntaxExpr expr, RValue left)
        {
            // Verify types
            if (left.BaseType != "bit" && left.BaseType != "bus")
                return Reject(expr, "Can not perform '"
                                    + expr.Function 
                                    + "' operator on '"
                                    + left.GetTypeName()
                                    + "' (unknown operator type)");

            // Generate not
            foreach (OpCode op in left.BitValue)
                op.Negate ^= OpState.One;

            return left;
        }

        /// <summary>
        /// Negate the given op-code
        /// </summary>
        OpCode NegOp(OpCode op)
        {
            op.Negate ^= OpState.One;
            return op;
        }

        /// <summary>
        /// Generate the index function: bit[range]
        /// </summary>
        RValue GenerateIndex_BitInt_RangeInt(SyntaxExpr expr, RValue left, RValue right)
        {
            // Optionally convert right side from int to range 
            // (without actually changing the type)
            if (right.BaseType == "int")
                right.RangeLengthValue = 1;

            // Optionally convert left side from int to bits
            if (left.BaseType == "int")
            {
                int maxBits = (int)Math.Min(64L, right.IntValue + right.RangeLengthValue);
                ConvertIntToBits(expr, ref left, maxBits, false);
            }

            // Verify ranges are ok
            long start = right.IntValue;
            long end = right.IntValue + right.RangeLengthValue;
            if (right.RangeLengthValue <= 0)
                return Reject(expr, "Range length must be >= 1: Range is " 
                                + right.IntValue + ":" + right.RangeLengthValue);
            if (start < 0 || start > left.BitValue.Count
                    || end < 0 || end > left.BitValue.Count)
                return Reject(expr, "Index out of range: Type is " 
                                    + left.GetTypeName()
                                    + ", index is " + right.GetTypeName());

            RValue result = new RValue("bit");
            result.BitValue = new List<OpCode>((int)right.RangeLengthValue);
            for (int i = (int)start; i < (int)end; i++)
                result.BitValue.Add(left.BitValue[i]);
            return result;
        }


        /// <summary>
        /// Generate code for an identifier (a variable used in an expression)
        /// </summary>
        RValue GenerateIdentifier(SyntaxExpr expr)
        {
            Symbol symbol = FindSymbol(expr.Function);

            // Mark this symbol with the resolved name (or error message)
            if (symbol == null)
                return Reject(expr, "Undefined symbol");
            if (symbol.ResolvedName == "")
                return Reject(expr, "Unresolved symbol (see symbol declarion)");
            expr.Function.AppendMessage(symbol.ResolvedName);

            // Create the return value

            // Handle INT
            string typeName = symbol.Decl.TypeName;
            if (typeName == "int")
            {
                RValue intValue = new RValue("int");
                intValue.IntValue = symbol.ConstValue;
                return intValue;
            }

            // Handle IN, OUT, BIT, and BOX
            RValue value = new RValue();
            value.Success = true;
            if (typeName == "in" || typeName == "out" || typeName == "bit")
            {
                // In and OUT parameters are converted to BIT
                value.BaseType = "bit";
            }
            else if (typeName == "box")
            {
                // A BOX value is converted to BIT (return value for a box)
                // NOTE: This is called for an identifier on the left side 
                // of an assignment, not for a function call identifier.
                if (symbol.CodeBoxValue != this)
                    return Reject(expr, "Only this box '" 
                                + ParseBox.NameDecl.VariableName + "' may be used");
                value.BaseType = "bit";
            }
            else
                return Reject(expr, "Unknown type: '" 
                            + symbol.Decl.VariableName + "' is a '" + typeName + "'");

            // Internal check: Ensure our symbol names match what we expect
            int vindex = symbol.CodeExprIndex;
            int vlength = (int)symbol.ArraySize;
            if (vindex < 0 || vindex >= Code.Count
                    || vindex + vlength - 1 >= Code.Count)
            {
                return Reject(expr, "Internal compiler error: array index error for " 
                            + symbol.Decl.VariableName + "(" + vindex + ", " + vlength + ")");
            }

            // Generate a bunch of OpCodeTerminal's
            value.BitValue = new List<OpCode>(vlength);
            for (int i = 0; i < vlength; i++)
                value.BitValue.Add(new OpCodeTerminal(Code[vindex + i]));
            return value;
        }

        /// <summary>
        /// Generate code for a constant
        /// </summary>
        RValue GenerateConstant(SyntaxExpr expr)
        {
            RValue result = new RValue("int");

            // Attempt to scan hexadecimal value 
            if (expr.Function.Name.Length >= 3 
                && char.ToUpper(expr.Function.Name[1]) == 'X'
                 && long.TryParse(expr.Function.Name.Substring(2),
                                    NumberStyles.AllowHexSpecifier,
                                    CultureInfo.CurrentCulture, out result.IntValue))
            {
                return result;
            }

            // Attempt to scan a binary value 
            if (expr.Function.Name.Length >= 3 
                && char.ToUpper(expr.Function.Name[1]) == 'B')
            {
                // Scan a binary number
                bool ok = expr.Function.Name.Length <= 65;
                for (int i = 2; i < expr.Function.Name.Length; i++)
                {
                    char ch = expr.Function.Name[i];
                    if (ch != '0' && ch != '1')
                        ok = false;
                    result.IntValue *= 2;
                    if (ch == '1')
                        result.IntValue++;
                }
                if (ok)
                    return result;
            }

            // Attempt to scan decimal value
            if (long.TryParse(expr.Function, out result.IntValue))
                return result;

            return Reject(expr, "Error reading value");
        }

        /// <summary>
        /// Generate code for the ternary operator
        /// </summary>
        RValue GenerateTernary(SyntaxExpr expr)
        {
            if (expr.Function != "?" || expr.Count != 2
                || expr[1].Function != ":"
                || expr[1].Count != 2)
            {
                return Reject(expr, "Compiler error: Malformed ternary operator");
            }
            RValue condition = GenerateCodeExpr(expr[0]);
            RValue left = GenerateCodeExpr(expr[1][0]);
            RValue right = GenerateCodeExpr(expr[1][1]);
            if (condition.Error || left.Error || right.Error)
                return RValue.Failed;

            if (condition.BaseType != "bit")
                return Reject(expr, "Error: Left hand side expects type 'bit', but got type '"
                                    + condition.BaseType + "'");

            if (left.BaseType != "bit")
                return Reject(expr[1],
                        "Error: Left hand side expects type 'bit', but got type '"
                                    + left.BaseType + "'");

            if (right.BaseType != "bit")
                return Reject(expr[1],
                        "Error: Right hand side expects type 'bit', but got type '"
                                    + left.BaseType + "'");

            // Promote the ':' operator arrays to the same length (or give error)
            if (!PromoteOperatorBitBit(expr[1], ref left, ref right))
                return RValue.Failed;

            // Require left side to be either 1 or same length as right side
            if (condition.BitCount != 1 && condition.BitCount != left.BitCount)
                return Reject(expr, "Error: Left hand side of '?' expects type 'bit [1]'"
                    +(left.BitCount == 1 ? "" : " or 'bit [" + left.BitCount + "]'")
                                    + ", but got type '"
                                    + condition.GetTypeName() + "' instead");

            // Save the condition(s) so the code doesn't get duplicated
            int condBase = Code.Count;
            foreach (var op in condition.BitValue)
                Code.Add(new OpCodeExpr(op));

            // Create expression result: cond*left + !cond*right
            RValue result = new RValue("bit");
            result.BitValue = new List<OpCode>(left.BitCount);
            for (int i = 0; i < left.BitCount; i++)
            {
                // Get condition index (single bit fields just get re-used)
                int condIndex = Math.Min(condBase + i, Code.Count-1);
                OpCodeTerminal c = new OpCodeTerminal(Code[condIndex]);
                OpCodeTerminal cn = new OpCodeTerminal(Code[condIndex]);
                cn.Negate = OpState.One;
                OpCode op = new OpCodeOr(new OpCodeAnd(c, left.BitValue[i]),
                                         new OpCodeAnd(cn, right.BitValue[i]));
                result.BitValue.Add(op);
            }
            return result;
        }

        /// <summary>
        /// Generate code for an operator
        /// </summary>
        RValue GenerateOperator(SyntaxExpr expr)
        {
            // Give simple error message for undefined operator
            string token = expr.Function;
            if (token != "["
                && token != "!"
                && token != "*" 
                && token != "/"
                && token != "%"
                && token != "#"
                && token != "+"
                && token != "-"
                && token != "=="
                && token != "!="
                && token != ".."
                && token != ":"
                && token != "="
                && token != "?")
                return Reject(expr, "Un-recognized symbol");

            // Ternary operator
            if (token == "?")
                return GenerateTernary(expr);

            // Verify proper number of parameters from parser
            if (expr.Function == "!")
            {
                // Unary operators
                if (expr.Count != 1)
                    return Reject(expr, "Internal compiler error: Unary operator must have one parameter");
            }
            else if (expr.Count != 2)
            {
                // Binary operators
                return Reject(expr, "Internal compiler error: Binary operator must have two parameters");
            }

            // Evaluate left and right sides (or just left if unary operator)
            RValue left = GenerateCodeExpr(expr[0]);
            RValue right = new RValue("void");
            if (expr.Count >= 2)
                right = GenerateCodeExpr(expr[1]);

            // Exit if there was an error parsing parameters
            if (left.Error || right.Error)
                return RValue.Failed;

            // Create an operator function with type names
            string functionNameAndType = expr.Function + left.BaseType + "," + right.BaseType;
            switch (functionNameAndType)
            {
                case "[bit,int":
                    return GenerateIndex_BitInt_RangeInt(expr, left, right);
                case "[bit,range":
                    return GenerateIndex_BitInt_RangeInt(expr, left, right);
                case "[int,int":
                    return GenerateIndex_BitInt_RangeInt(expr, left, right);
                case "[int,range":
                    return GenerateIndex_BitInt_RangeInt(expr, left, right);
                case "!bit,void":
                    return GenerateNot_Bit(expr, left);
                case "*bit,bit":
                    return GenerateMath_BitBit(expr, left, right);
                case "*int,int":
                    return new RValue("int", left.IntValue * right.IntValue);
                case "/int,int":
                    return new RValue("int", left.IntValue / right.IntValue);
                case "%int,int":
                    return new RValue("int", left.IntValue % right.IntValue);
                case "#bit,bit":
                    return GenerateMath_BitBit(expr, left, right);
                case "+int,int":
                    return new RValue("int", left.IntValue + right.IntValue);
                case "+bit,bit":
                    return GenerateMath_BitBit(expr, left, right);
                case "-int,int":
                    return new RValue("int", left.IntValue - right.IntValue);
                case "==bit,bit":
                    return GenerateCompare_BitInt_BitInt(expr, left, right);
                case "==bit,int":
                    return GenerateCompare_BitInt_BitInt(expr, left, right);
                case "==int,bit":
                    return GenerateCompare_BitInt_BitInt(expr, left, right);
                case "!=bit,bit":
                    return GenerateCompare_BitInt_BitInt(expr, left, right);
                case "!=bit,int":
                    return GenerateCompare_BitInt_BitInt(expr, left, right);
                case "!=int,bit":
                    return GenerateCompare_BitInt_BitInt(expr, left, right);
                case ":int,int":
                    RValue r1 = new RValue("range", left.IntValue, right.IntValue);
                    expr.Function.AppendMessage(r1.GetTypeName());
                    return r1;
                case "..int,int":
                    RValue r2 = new RValue("range", left.IntValue, right.IntValue - left.IntValue + 1);
                    expr.Function.AppendMessage(r2.GetTypeName());
                    return r2;
                case "=bit,bit":
                    return GenerateAssign_Bit_BitInt(expr, left, right);
                case "=bit,int":
                    return GenerateAssign_Bit_BitInt(expr, left, right);
            }

            // Create user readable error message
            if (token == "[")
                token = "[]";
            if (token == "(")
                token = "()";
            Reject(expr, "Error: Operator '"
                            + token
                            + "' can not be applied to operands of type '"
                            + left.BaseType + "' and '" + right.BaseType + "'");

            return RValue.Failed;
        }

        /// <summary>
        /// Generate code for the 'dup' keyword.
        /// Returns TRUE if it worked, FALSE if not.
        /// </summary>
        bool GenerateDup(Token dupToken, ref RValue expression, int numBits)
        {
            // This should never happen (was checked when keyword was parsed)
            if (expression.BaseType != "bit" || expression.BitCount != 1)
            {
                Reject(dupToken, "Expecting a type of 'bit [1]', got a type of '"
                                        + expression.GetTypeName() + "'");
                return false;
            }
            OpCodeExpr dup = new OpCodeExpr(expression.BitValue[0]);
            Code.Add(dup);

            // Duplicate the expression
            expression.BitValue.Clear();
            for (int i = 0; i < numBits; i++)
                expression.BitValue.Add(new OpCodeTerminal(dup));

            // Generate info for user
            dupToken.AppendMessage(expression.GetTypeName());
            return true;
        }

        /// <summary>
        /// Parse the 'dup' keyword.  This evaluates the argument, and then
        /// sets the BitAllowDup symbol.  Later on (when the value is used),
        /// the 'dup' symbol will be marked with the type in GenerateDup
        /// </summary>
        RValue ParseDup(SyntaxExpr expr, SyntaxExpr fcall)
        {
            // NOTE: The function name is the first parameter
            if (expr.Count < 2)
                return Reject(fcall, "Error: 'dup' requires at least one parameter");
            if (expr.Count > 3)
                return Reject(fcall, "Error: 'dup' must not have more than two parameters");

            // Evaluate parameter (verify we have a bit type)
            RValue param1 = GenerateCodeExpr(expr[1]);
            RValue param2 = new RValue();
            if (expr.Count >= 3)
            {
                param2 = GenerateCodeExpr(expr[2]);
                if (param2.Error)
                    return param2;
            }

            if (param1.Error)
                return param1; // Error already recorded

            if (param1.BaseType != "bit")
                return Reject(fcall, "Error: 'dup' requires a parameter of type "
                                + "'bit', not of type '" + param1.BaseType + "'");

            if (param1.BitCount != 1)
                return Reject(fcall, "Error: 'dup' requires a parameter of type 'bit [1]', "
                                + "not of type '" + param1.GetTypeName() + "'");

            // Parse dup(bit, int)
            if (expr.Count >= 3)
            {
                if (param2.BaseType != "int")
                    return Reject(fcall, "Error: Parameter 2 must evaluate to 'int'"
                                        + " not '" + param2.GetTypeName() + "'");
                if (param2.IntValue < 1 || param2.IntValue > 128)
                    return Reject(fcall, "Error: Parameter 2 must be from 1 to 128, "
                                        + " not '" + param2.GetTypeName() + "'");
                // Duplicate now
                if (!GenerateDup(fcall.Function, ref param1, (int)param2.IntValue))
                    return RValue.Failed;
            }
            else
            {
                // Allow this bit to be duplicated when necessary
                param1.AllowDup = fcall.Function;
            }
            return param1;
        }

        /// <summary>
        /// Build a set of bits
        /// </summary>
        RValue ParseSet(SyntaxExpr expr, SyntaxExpr fcall)
        {
            // NOTE: The function name is the first parameter
            if (expr.Count < 2)
                return Reject(fcall, "Error: 'set' requires atleast one parameter");

            // Build a set of bits
            RValue result = new RValue("bit");
            result.BitValue = new List<OpCode>();
            for (int i = 1; i < expr.Count; i++)
            {
                // Evaluate next parameter
                RValue append = GenerateCodeExpr(expr[i]);
                if (append.Error)
                    return append; // Error already recorded

                if (append.BaseType != "bit")
                    return Reject(fcall, "Error: 'set' (parameter " + i 
                                    + ") requires a parameter of type "
                                    + "'bit', not of type '" + append.BaseType + "'");

                // Insert the new parameter at the beginning of the list
                result.BitValue.InsertRange(0, append.BitValue);
                //foreach (OpCode op in append.BitValue)
                //	result.BitValue.Add(op);
            }
            // Show info to user
            fcall.Function.AppendMessage(result.GetTypeName());
            return result;
        }


        /// <summary>
        /// Generate a function call parameter, mark fcall with any errors.
        /// </summary>
        private RValue GenerateFCallParam(SyntaxExpr expr,
                                            Symbol paramSymbol,
                                            int paramNum,
                                            List<OpCodeExpr> parameters)
        {
            // Special case: 'unused' for out parameter
            if (expr[paramNum].Function == "unused")
            {
                // Create array of 'out' expressions that are unused
                if (paramSymbol.Decl.TypeName != "out")
                    return Reject(expr[paramNum], "'unused' can only be used with 'out' parameters");
                for (int i = 0; i < paramSymbol.ArraySize; i++)
                    parameters.Add(new OpCodeExpr(new OpCodeOutParam()));
                return new RValue();  // Success - don't care
            }


            // Evaluate parameter
            RValue param = GenerateCodeExpr(expr[paramNum]);
            if (param.Error)
                return RValue.Failed;

            // Ensure parameter types match
            // NOTE: In/out parameter qualifiers are checked after compilation
            SyntaxExpr fcall = expr[0];
            if (param.BaseType != "bit")
                return Reject(fcall, "Parameter " + paramNum + " is expecting type 'bit' "
                                    + "but was given type '" + param.BaseType + "'");

            if (param.BitCount != paramSymbol.ArraySize)
                return Reject(fcall, "Parameter " + paramNum + " is expecting type 'bit['"
                                    + paramSymbol.ArraySize 
                                    + "]' but was given type 'bit['" 
                                    + param.BitCount + "]'");

            // Generate parameter expressions
            for (int i = 0; i < param.BitCount; i++)
            {
                // Make the parameter expression (parameter = expression)
                OpCodeExpr opExpr = new OpCodeExpr(param.BitValue[i]);

                if (paramSymbol.Decl.TypeName == "out")
                {
                    // Out parameter must be assigned to the terminal
                    OpCodeTerminal assignedBit = opExpr.Expr as OpCodeTerminal;
                    if (assignedBit == null)
                        return Reject(fcall, "Parameter " + paramNum + " is an 'out' parameter, "
                                + "and must not be an expression");

                    // Verify that the terminal has not already been assigned
                    if (assignedBit.Expression.Expr != null)
                        return Reject(fcall, "Parameter " + paramNum + " is an 'out' parameter, "
                            + "and has already been assigned a value");

                    // Make the assignment (terminal expression = parameter)
                    // and nuke the parameter (which gets linked in later)
                    assignedBit.Expression.Expr = new OpCodeTerminal(opExpr);
                    opExpr.Expr = new OpCodeOutParam();
                    parameters.Add(opExpr);
                }
                else if (paramSymbol.Decl.TypeName == "in")
                {
                    parameters.Add(opExpr);
                }
                else
                {
                    // Unknown parameter type
                    return Reject(fcall, "Parameter " + paramNum + " is of unknwon type '"
                            + paramSymbol.Decl.TypeName + "' when expecting "
                            + "'in', 'out', or 'bus'");
                }
            }
            return param;
        }


        /// <summary>
        /// Generate a function call.  The box parameters are
        /// included in the code, and a link box is added.
        /// </summary>
        RValue GenerateFunctionCall(SyntaxExpr expr)
        {
            // The first parameter is the function call
            if (expr.Count == 0)
                return Reject(expr, "Compiler error: Function name not specified?");

            // Check for ill-formed function call
            SyntaxExpr fcall = expr[0];
            if (fcall.Count != 0)
                return Reject(fcall, "Compiler error: Ill-formed function call (call can not have parameters)");

            // Reserved words
            if (fcall.Function.Type == eTokenType.Reserved)
                return Reject(fcall, "Reserved word not allowed in expression");

            // RESERVED FUNCTION NAMES - variables are in GenerateReservedWord
            if (fcall.Function.Type == eTokenType.ReservedName)
            {
                if (fcall.Function == "dup")
                    return ParseDup(expr, fcall);
                if (fcall.Function == "set")
                    return ParseSet(expr, fcall);
                return Reject(fcall, "Unrecognized reserved word: '" + fcall.Function + "'");
            }

            // Verify the function is an identifier
            if (fcall.Function.Type != eTokenType.Identifier)
                return Reject(fcall, "Unexpected symbol");

            // Look up function call
            Symbol fsymbol = FindSymbol(fcall.Function);
            if (fsymbol == null)
                return Reject(fcall, "Undefined symbol");
            if (fsymbol.ResolvedName == "")
                return Reject(fcall, "Unresolved symbol");

            // Verify this is a valid box
            CodeBox fcode = fsymbol.CodeBoxValue;
            if (fsymbol.Decl.TypeName != "box")
                return Reject(fcall, "Expecting the name of a box, but '" + fcall.Function 
                                        + "' is of type '" + fsymbol.ResolvedName);
            if (fsymbol.CodeBoxValue == this)
                return Reject(fcall, "Box must not call itself (recursion not allowed)");
            if (fcode == null)
                return Reject(fcall, "Compiler error: Code box not found");


            // Give type info for this symbol
            fcall.Function.AppendMessage(fsymbol.ResolvedName);

            // Detect box errors, but also continue compiling to find more errors.
            if (fcode.Error)
                Reject(fcall, "The box '" + fcall.Function + "' has errors that must be fixed");

            // Verify correct number of parameters
            // NOTE: expr has an extra parameter (the first parameter is the box name)
            if (fcode.Params.Count != expr.Count-1)
            {
                // Even though this is an error, evaluate the parameters
                // so the user gets more info
                for (int i = 1; i < expr.Count; i++)
                    GenerateCodeExpr(expr[i]);
                return Reject(fcall, "Incorrect number of parameters: '"
                                    + fcall.Function + "' expects " + fcode.Params.Count
                                    + " but is given " + (expr.Count-1));
            }

            // Generate expressions for return value
            List<OpCodeExpr> parameters = new List<OpCodeExpr>();
            RValue result = new RValue("void");
            for (int i = 0; i < fcode.Symbol.ArraySize; i++)
            {
                // Build function call parameters (return value is an 'out')
                OpCodeExpr opExpr = new OpCodeExpr(new OpCodeOutParam());
                parameters.Add(opExpr);

                // Build return value expressions
                result.BaseType = "bit";
                if (result.BitValue == null)
                    result.BitValue = new List<OpCode>();
                result.BitValue.Add(new OpCodeTerminal(opExpr));
            }

            // Generate expressions for parameters
            for (int p = 0; p < fcode.Params.Count; p++)
            {
                // Generate this parameter
                GenerateFCallParam(expr, fcode.Params[p], p+1, parameters);
            }

            // Save link info
            LinkBox link = new LinkBox();
            link.CodeBox = fcode;
            link.FCall = fcall;
            link.Parameters = parameters;
            mLinks.Add(link);

            return result;
        }

        /// <summary>
        /// Build the master 'IF' condition, which is only true
        /// if all nested 'if' conditions are also true.
        /// </summary>
        int BuildMasterIfCondition()
        {
            // Single if - not else = just use the condition
            if (mIfScopes.Count == 1 && !mIfScopes[0].InElse)
                return mIfScopes[0].InnerConditionIndex;

            // Build master condition
            OpCode cond;
            if (mIfScopes.Count == 1)
            {
                // Single if - in else - Use a negated terminal
                cond = new OpCodeTerminal(Code[mIfScopes[0].InnerConditionIndex]);
                cond.Negate ^= OpState.One;
            }
            else
            {
                // Nested 'if' condition
                cond = new OpCodeAnd();
                cond.Operands = new List<OpCode>();
                foreach (IfScope scope in mIfScopes)
                {
                    OpCodeTerminal term = new OpCodeTerminal(Code[scope.InnerConditionIndex]);
                    if (scope.InElse)
                        term.Negate ^= OpState.One;
                    cond.Operands.Add(term);
                }
            }
            Code.Add(new OpCodeExpr(cond));
            return Code.Count-1;
        }

        /// <summary>
        /// Generate an IF statement
        /// </summary>
        RValue GenerateIfStatement(SyntaxExpr expr)
        {
            if (expr.Count != 2 && expr.Count != 3)
                return Reject(expr, "Compiler error: If statement must have 2 or 3 parameters");


            // Parse the condition
            RValue condition = GenerateCodeExpr(expr[0]);

            // Even if there is an error, we still want to parse the statements
            if (!condition.Error)
                if (condition.BaseType != "bit" || condition.BitCount != 1)
                {
                    Reject(expr[0], "Error: Expecting 'if' condition to be "
                                      + "of type 'bit[1]', but found type '"
                                      + condition.GetTypeName() + "'");
                    condition = RValue.Failed;
                }

            // Setup new IF scope (with master condition)
            IfScope ifScope = new IfScope();
            mIfScopes.Add(ifScope);
            ifScope.InnerConditionIndex = Code.Count;
            if (condition.Error)
                Code.Add(new OpCodeExpr(new OpCodeConst()));
            else
                Code.Add(new OpCodeExpr(condition.BitValue[0]));
            ifScope.MasterConditionIndex = BuildMasterIfCondition();

            // Generate code for IF statement (and collect all assignments)
            GenerateCodeExpr(expr[1]);

            // Clear out IF scope (so we can re-assign in else part)
            List<OpCodeExpr> assignedInIf = ifScope.Assigned;
            List<OpCode>	 assignedExpr = new List<OpCode>();
            ifScope.Assigned = new List<OpCodeExpr>();
            foreach (OpCodeExpr assigned in assignedInIf)
            {
                // Save expression, then clear it out
                assignedExpr.Add(assigned.Expr);
                assigned.Expr = null;
            }

            // Parse the else part
            if (expr.Count == 3)
            {
                // Setup new scope
                ifScope.InElse = true;
                ifScope.MasterConditionIndex = BuildMasterIfCondition();

                // Generate code for the else part
                GenerateCodeExpr(expr[2]);
            }

            // Now we need to generate sum of products
            // NOTE: Conditional products were generated by assignment statement
            for (int i = 0; i < assignedInIf.Count; i++)
            {
                // Generate sum of products only when in assigned
                // in both 'if' branch and 'else' branch
                if (assignedInIf[i].Expr == null)
                {
                    // Only assigned in 'if' branch
                    assignedInIf[i].Expr = assignedExpr[i];
                }
                else
                {
                    // Assigned in both branches
                    assignedInIf[i].Expr = GenOp(new OpCodeOr(), assignedExpr[i], assignedInIf[i].Expr);
                }
            }

            // Everything that was assigned in either the 'if' branch
            // or the 'else' branch needs to be propagated to the outter
            // if statement (if we are in a nested scope)
            if (mIfScopes.Count > 1)
            {
                // Copy 'if' branch
                IfScope outter = mIfScopes[mIfScopes.Count-2];
                foreach (OpCodeExpr expression in assignedInIf)
                    outter.Assigned.Add(expression);

                // Copy 'else' branch (only if not already copied from 'if')
                foreach (OpCodeExpr expression in ifScope.Assigned)
                    if (!assignedInIf.Contains(expression))
                        outter.Assigned.Add(expression);
            }

            // Pop the IfScope
            mIfScopes.RemoveAt(mIfScopes.Count-1);

            // Since this is a statement, the result should not be used
            return RValue.Failed;
        }

        /// <summary>
        /// Paser
        /// </summary>
        RValue GenerateReservedWord(SyntaxExpr expr)
        {
            RValue result = new RValue("bit");
            result.BitValue = new List<OpCode>();

            // RESERVED VARIABLE NAMES - variables are in GenerateFunctionCall
            if (expr.Function == "true")
                result.BitValue.Add(new OpCodeConst(1));
            else if (expr.Function == "false")
                result.BitValue.Add(new OpCodeConst(0));
            else if (expr.Function == "if")
                return GenerateIfStatement(expr);
            else if (expr.Function == "bit")
                return GenerateBitStatement(expr);
            else if (expr.Function == "const")
                return GenerateConstStatement(expr);
            else
                return Reject(expr, "Unrecognized reserved word: '" + expr.Function + "'");
            return result;
        }


        /// <summary>
        /// Generate an expression (recurse the parse tree)
        /// </summary>
        RValue GenerateCodeExpr(SyntaxExpr expr)
        {
            string token = expr.Function;

            // This was a specially generated token, to indicate statements
            if (token == "{")
            {
                // Generate statements
                for (int i = 0;  i < expr.Count;  i++)
                    GenerateCodeExpr(expr[i]);
                return new RValue("(ok)");
            }
            // Process function call
            if (token == "(")
                return GenerateFunctionCall(expr);

            // Process reserved words (if, bit, etc.)
            if (expr.Function.Type == eTokenType.Reserved
                || expr.Function.Type == eTokenType.ReservedName)
                return GenerateReservedWord(expr);

            // Generate a variable
            if (expr.Function.Type == eTokenType.Identifier)
                return GenerateIdentifier(expr);

            if (token.Length >= 1 && char.IsDigit(token, 0))
                return GenerateConstant(expr);

            // This call will generate an error message for un-defines symbols
            return GenerateOperator(expr);
        }


        /// <summary>
        /// Verify that this declaration has been assigned.
        /// Returns TRUE if it has been assigned (FALSE for error)
        /// </summary>
        bool CheckDeclForUnassigned(Symbol symbol)
        {
            for (int i = 0; i < symbol.ArraySize; i++)
                if (Code[symbol.CodeExprIndex+i].Expr == null)
                {
                    Reject(symbol.Decl.VariableName, "Error: This declaration must be fully assigned");
                    return false;
                }
            return true;
        }

        /// <summary>
        /// Assign memory in mExpressions for one declaration
        /// </summary>
        void AddLocalPoolDeclaration(Symbol symbol)
        {
            string typeName = symbol.Decl.TypeName;
            if (typeName == "in"
                || typeName == "out"
                || typeName == "bit"
                || typeName == "bus"
                || typeName == "box")
            {
                // Add one code expression for each bit in the array
                symbol.CodeExprIndex = Code.Count;
                for (int i = 0; i < symbol.ArraySize; i++)
                {
                    // Add an un-assigned declaration
                    OpCodeExpr expression = new OpCodeExpr(symbol.Decl.VariableName
                                            + (symbol.ArraySize == 1 ? "" : "." + i));
                    Code.Add(expression);

                    // In parameters are pre-assigned
                    if (typeName == "in")
                        expression.Expr = new OpCodeInParam();
                }
            }
        }

        /// <summary>
        /// Generate code for bit statement (declaration, and optional assignment)
        /// </summary>
        RValue GenerateBitStatement(SyntaxExpr expr)
        {
            // The parser didn't generate a SyntaxDecl for us.
            // Intead, we are creating it here based on the known expr layout.
            // TODO: Have SyntaxDecl inherit from SyntaxExpr.  Have the parser
            // generate a SyntaxDecl for us and use it here (via cast)
            RValue r = new RValue();
            int arraySize = 1;
            SyntaxExpr declaration = expr[0];
            if (declaration.Count >= 1)
            {
                // Evaluate array size (optional parameter)
                EvaluateConst c = EvaluateConst.Eval(mSymbolTable, declaration[0]);
                if (!c.Resolved)
                    return Reject(declaration.Function, "Unresolved symbol");
                if (c.ValueInt < 1 || c.ValueInt > MAX_ARRAY_SIZE)
                    return Reject(declaration.Function, "The array size must be in the range of 1 to " + MAX_ARRAY_SIZE);
                arraySize = (int)c.ValueInt;
            }

            // Generate declaration info (see note above)
            SyntaxDecl decl = new SyntaxDecl();
            decl.TypeName = expr.Function;
            decl.VariableName = declaration.Function;

            // Generate the symbol info
            Symbol symbol = new Symbol();
            symbol.Decl = decl;
            symbol.ArraySize = arraySize;
            symbol.ResolvedName = decl.TypeName
                                + " " + decl.VariableName
                                + (symbol.ArraySize == 1 ? "" : "[" + symbol.ArraySize + "]");

            Locals.Add(symbol);
            AddLocalPoolDeclaration(symbol);
            AddSymbol(symbol);

            // Generate optional assignment
            if (expr.Count >= 2)
                r = GenerateCodeExpr(expr[1]);
            else
                decl.VariableName.AppendMessage(symbol.ResolvedName);

            return r;
        }

        /// <summary>
        /// Generate code for const statement
        /// </summary>
        RValue GenerateConstStatement(SyntaxExpr expr)
        {
            // The parser didn't generate a SyntaxDecl for us.
            // Intead, we are creating it here based on the known expr layout.
            // TODO: Have SyntaxDecl inherit from SyntaxExpr.  Have the parser
            // generate a SyntaxDecl for us and use it here (via cast)
            SyntaxExpr assignment = expr[1];
            Token variableName = assignment[0].Function;
            EvaluateConst c = EvaluateConst.Eval(mSymbolTable, assignment[1]);

            // Resolve constant expression
            if (!c.Resolved)
                return Reject(variableName, "Unresolved symbol");

            // Generate declaration info (see note above)
            SyntaxDecl decl = new SyntaxDecl();
            decl.TypeName = expr[0].Function;
            decl.VariableName = variableName;
            
            // Generate the symbol info
            Symbol symbol = new Symbol();
            symbol.Decl = decl;
            symbol.ConstValue = c.ValueInt;
            symbol.ResolvedName = decl.TypeName
                            + " " + decl.VariableName
                            + " = " + c.ToString();
            c.WasHex = !c.WasHex;
            symbol.ResolvedName += " (" + c.ToString() + ")";

            Locals.Add(symbol);
            AddSymbol(symbol);
            variableName.AppendMessage(symbol.ResolvedName);

            return new RValue();
        }


        /// <summary>
        /// Resolve in, out, bus, or bit (locals or parameters)
        /// </summary>
        void ResolveLocalParameter(SyntaxDecl decl)
        {
            Symbol symbol = new Symbol();
            symbol.Decl = decl;

            // Evaluate the array portion of the declaration
            EvaluateConst c = EvaluateConst.Eval(mSymbolTable, decl.ArraySizeExpr);
            if (decl.ArraySizeExpr == null)
                c = new EvaluateConst(1); // Default to an array size of 1

            // Verify resolved, and within proper array size
            if (!c.Resolved)
            {
                Reject(decl.VariableName, "Unresolved symbol");
                return;
            }
            if (c.ValueInt < 1 || c.ValueInt > MAX_ARRAY_SIZE)
            {
                Reject(decl.VariableName, "The array size must be in the range of 1 to " + MAX_ARRAY_SIZE);
                return;
            }

            // Set resolved value, and display to user
            symbol.ArraySize = (int)c.ValueInt;
            symbol.ResolvedName = decl.TypeName
                                + " " + decl.VariableName
                                + (c.ValueInt == 1 ? "" : "[" + c.ValueInt + "]");

            // Show the type info
            decl.VariableName.AppendMessage(symbol.ResolvedName);

            Params.Add(symbol);
            AddSymbol(symbol);
        }

        /// <summary>
        /// Resolve a box declaration
        /// NOTE: The parameters must be resolved before
        /// the box name is resolved.
        /// </summary>
        void ResolveBox(Symbol symbol)
        {
            SyntaxDecl decl = symbol.Decl;

            // Evaluate the array portion of the declaration
            EvaluateConst c = EvaluateConst.Eval(mSymbolTable, decl.ArraySizeExpr);
            if (decl.ArraySizeExpr == null)
                c = new EvaluateConst(0); // Default to no return value

            // Verify resolved, and within proper array size
            if (!c.Resolved)
            {
                Reject(decl.VariableName, "Unresolved symbol");
                return;
            }
            if (c.ValueInt < 0 || c.ValueInt > MAX_ARRAY_SIZE)
            {
                Reject(decl.VariableName, "The array size must be in the range of 0 to " + MAX_ARRAY_SIZE);
                return;
            }

            // Prepare a resolved name
            StringBuilder name = new StringBuilder();
            name.Append(decl.TypeName);
            name.Append(" ");
            name.Append(decl.VariableName);
            name.Append(c.ValueInt == 0 ? " (" : "[" + c.ValueInt + "] (");

            // Figure the resolved name
            // Verify that all parameters are resolved
            for (int i = 0; i < Params.Count; i++)
            {
                // Parameter resolved?
                if (Params[i].ResolvedName == "")
                {
                    Reject(decl.VariableName,
                        "Error resolving parameter: " + Params[i].Decl.VariableName);
                    return;
                }

                // Figure name
                name.Append(Params[i]);
                if (i != Params.Count-1)
                    name.Append(", ");
            }
            name.Append(")");

            // Set resolved value, and display to user
            symbol.ArraySize = (int)c.ValueInt;
            symbol.ResolvedName = name.ToString();

            // Show the type info
            decl.VariableName.AppendMessage(symbol.ResolvedName);
        }


        /// <summary>
        /// Resolve all the box declarations (build mExpressions list)
        /// </summary>
        void GenerateBoxCode()
        {
            // Add this box to the symbol table to ensure the
            // no parameter names match
            Symbol = new Symbol();
            Symbol.Decl = ParseBox.NameDecl;
            Symbol.CodeBoxValue = this;
            AddSymbol(Symbol);

            // Add constants to symbol table
            foreach (SyntaxExpr constant in ParseBox.Constants)
                GenerateConstStatement(constant);

            // Resolve parameters before resolving box name
            foreach (SyntaxDecl decl in ParseBox.Params)
                ResolveLocalParameter(decl);

            // Resolve this box name (now that we have the parameters)
            if (Symbol.Decl.TypeName == "box")
                ResolveBox(Symbol);

            // Process all sub-boxes first
            foreach (SyntaxBox subBox in ParseBox.Boxes)
            {
                // Compile baby box
                CodeBox box = new CodeBox(mSymbolTable, subBox); 
                AddSymbol(box.Symbol);
                Boxes.Add(box.Symbol);
            }

            // If this is not a box, exit
            if (ParseBox.NameDecl.TypeName != "box")
                return;

            // ---------------------------------------------------
            // BOX: Process local scope
            // ---------------------------------------------------

            // Go through all the parameter declarations, and
            // assign memory in mExpressions for the code.
            AddLocalPoolDeclaration(Symbol);
            foreach (Symbol symbol in Params)
                AddLocalPoolDeclaration(symbol);

            GenerateCodeExpr(ParseBox.Statements);


            // Check the code for unassigned variables (only if there isn't an error)
            if (!Error)
            {
                // After generating the code, we need to verify that
                // all expressions have been assigned.
                bool failed = false;

                // Check return value
                if (!CheckDeclForUnassigned(Symbol))
                    failed = true;

                // Check parameters
                foreach (Symbol symbol in Params)
                    if (!CheckDeclForUnassigned(symbol))
                        failed = true;

                // Check locals
                foreach (Symbol symbol in Locals)
                    if (!CheckDeclForUnassigned(symbol))
                        failed = true;

                // If something failed, the user has been notified of the problem
                // Now check to see if we missed something (should never happen)
                if (!failed)
                    foreach (OpCodeExpr expression in Code)
                        if (expression.Expr == null)
                            Reject(ParseBox.NameDecl.VariableName, "Compiler error: Unassigned expression slipped through");
            }
        }


        /// <summary>
        /// Link the code (include all sub-boxes)
        /// </summary>
        public void LinkCode()
        {
            if (LinkedCode != null)
                return;
            LinkedCode = new List<OpCodeExpr>();

            // Link all sub-boxes first
            foreach (LinkBox link in mLinks)
                link.CodeBox.LinkCode();

            // Shallow copy base box expressions
            for (int i = 0; i < Code.Count; i++)
            {
                // Set expression index (so the deep copy gets the new expression)
                Code[i].Index = LinkedCode.Count;
                LinkedCode.Add(new OpCodeExpr(Code[i].Name, Code[i].Expr));
            }

            // Deep copy each sub-box
            foreach (LinkBox link in mLinks)
            {
                // Shallow copy sub-box
                CodeBox baby = link.CodeBox;
                link.BaseIndex = LinkedCode.Count;
                for (int i = 0; i < baby.LinkedCode.Count; i++)
                {
                    // Set baby box parameter indices
                    baby.LinkedCode[i].Index = LinkedCode.Count;
                    string paramName = "";
                    OpCode paramExpr = baby.LinkedCode[i].Expr;
                    if (i < link.Parameters.Count)
                    {
                        // Setup parameter indices
                        link.Parameters[i].Index = LinkedCode.Count;
                        paramName = link.Parameters[i].Name;

                        // If the baby box parameter is IN (use our expression)
                        if (paramExpr is OpCodeInParam)
                            paramExpr = link.Parameters[i].Expr;
                        else if (!(link.Parameters[i].Expr is OpCodeOutParam))
                            throw new Exception("Link error: In/Out param mismatch");

                        // Ensure we got the proper thing
                        if (paramExpr is OpCodeInParam || paramExpr is OpCodeOutParam)
                            throw new Exception("Link error: In/Out param mismatch");
                    }

                    // Parameter names are kept, function body names are nuked
                    LinkedCode.Add(new OpCodeExpr(paramName, paramExpr));
                }
                // Deep copy baby box expressions
                // NOTE: This must be done *here* for each baby box, because
                // the indices are setup in the loop above, and will need to
                // change each time.
                for (int i = link.BaseIndex; i < LinkedCode.Count; i++)
                    LinkedCode[i].Expr = LinkedCode[i].Expr.DeepClone(
                            delegate(OpCodeTerminal term) { term.Expression = LinkedCode[term.Expression.Index]; });
            }

            // Deep copy base box expressions
            for (int i = 0; i < Code.Count; i++)
                LinkedCode[i].Expr = LinkedCode[i].Expr.DeepClone(
                        delegate(OpCodeTerminal term) { term.Expression = LinkedCode[term.Expression.Index]; });

            // Assign names to un-named things
            int eNum = 0;
            foreach (OpCodeExpr expr in LinkedCode)
            {
                if (expr.Name == "")
                    expr.Name = "X" + eNum;
                eNum++;
            }
        }

        /// <summary>
        /// Create code from a parse box.  Parent can be NULL if
        /// this is the top level.
        /// </summary>
        public CodeBox(SymbolTable enclosingScope, SyntaxBox box)
        {
            // First, add our primary symbol to the symbol table
            mSymbolTable = new SymbolTable(enclosingScope);
            ParseBox = box;
            Error = box.Error;	// Parse error means we also have an error
            GenerateBoxCode();
        }
    }
}
