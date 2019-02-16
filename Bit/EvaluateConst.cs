// Copyright 2011-2019 by Jeremy Spiller
// See Lincense.txt for details

using System;
using System.Collections.Generic;
using System.Globalization;

namespace Gosub.Bit
{
    /// <summary>
    /// Evaluate a constant expression (a long or a range)
    /// </summary>
    struct EvaluateConst
    {
        public bool	Resolved;
        public bool	IsRange;
        public bool WasHex;
        public long	ValueInt;
        public long	RangeLength;

        /// <summary>
        /// Make a fully resolved constant
        /// </summary>
        public EvaluateConst(long low)
        {
            Resolved = true;
            IsRange = false;
            WasHex = false;
            ValueInt = low;
            RangeLength = 0;
        }


        /// <summary>
        /// Convert to string (hexadecimal if it was read in hex)
        /// </summary>
        public override string ToString()
        {
            if (WasHex)
                if (IsRange)
                    return "0x" + ValueInt.ToString("X") + ":" + "0x" + RangeLength.ToString("X");
                else
                    return "0x" + ValueInt.ToString("X");
            else
                if (IsRange)
                    return "" + ValueInt + ":" + RangeLength;
                else
                    return "" + ValueInt;
        }

        /// <summary>
        /// Evaluate a constant.  If there is an error, the symbol  
        /// that caused it is rejected and the error bit is set.
        /// </summary>
        public static EvaluateConst Eval(SymbolTable symbols, SyntaxExpr expr)
        {
            if (expr == null)
                return new EvaluateConst();

            if (expr.Count != 0)
            {
                expr.Function.Reject("Not yet supported");
                return new EvaluateConst();
            }

            if (expr.Function.Name.Length == 0)
                return new EvaluateConst();
            
            // Parse number
            if (char.IsDigit(expr.Function.Name[0]))
            {
                EvaluateConst result = new EvaluateConst();
                result.Resolved = true;

                // Attempt to scan hexadecimal value 
                if (expr.Function.Name.Length >= 3 
                    && char.ToUpper(expr.Function.Name[1]) == 'X'
                     && long.TryParse(expr.Function.Name.Substring(2), 
                                        NumberStyles.AllowHexSpecifier, 
                                        CultureInfo.CurrentCulture, out result.ValueInt))
                {
                    result.WasHex = true;
                    return result;
                }
                
                // Attempt to scan decimal value
                if (long.TryParse(expr.Function.Name, out result.ValueInt))
                    return result;

                expr.Function.Reject("Error reading value");
                return new EvaluateConst();
            }

            // Parse identifier
            if (expr.Function.Type == eTokenType.Identifier)
            {
                Symbol symbol = symbols.FindSymbol(expr.Function);
                if (symbol == null)
                {
                    expr.Function.Reject("Undefined symbol");
                    return new EvaluateConst();
                }
                if (symbol.Decl.TypeName.Name != "int")
                {
                    expr.Function.Reject("Constant value must be of type 'int'");
                    return new EvaluateConst();
                }
                if (symbol.ResolvedName == "")
                {
                    expr.Function.Reject("Unresolved symbol");
                    return new EvaluateConst();
                }
                // We got the type, mark the symbol
                expr.Function.AppendMessage(symbol.ResolvedName);
                return new EvaluateConst(symbol.ConstValue);
            }

            expr.Function.Reject("Unrecognized symbol in constant expression");
            return new EvaluateConst();
        }

    }
}
