// Copyright 2011-2019 by Jeremy Spiller
// See Lincense.txt for details

using System;
using System.Collections.Generic;
using System.Text;

namespace Gosub.Bit
{
    /// <summary>
    /// Expression tree.  
    /// </summary>
    class SyntaxExpr
    {
        protected readonly static Token	sBlankToken = new Token("", 0, 0);

        // Filled in by parser
        Token				mFunction;
        List<SyntaxExpr>	mParameters;

        /// <summary>
        /// Construct a blank expression 
        /// </summary>
        public SyntaxExpr()
        {
            mFunction = sBlankToken;
        }

        /// <summary>
        /// Construct an expression with only a function
        /// </summary>
        public SyntaxExpr(Token function)
        {
            mFunction = function;
        }

        /// <summary>
        /// Create a unary expression
        /// </summary>
        public SyntaxExpr(Token function, SyntaxExpr param1)
        {
            mParameters = new List<SyntaxExpr>();
            mFunction = function;
            mParameters.Add(param1);
        }

        /// <summary>
        /// Create a binary expression
        /// </summary>
        public SyntaxExpr(Token function, SyntaxExpr param1, SyntaxExpr param2)
        {
            mParameters = new List<SyntaxExpr>();
            mFunction = function;
            mParameters.Add(param1);
            mParameters.Add(param2);
        }

        /// <summary>
        /// Return the primary token (the function, operator, or variable name)
        /// </summary>
        public Token Function { get { return mFunction; } }

        /// <summary>
        /// Return the number of parameters
        /// </summary>
        public int Count { get { return mParameters == null ? 0 : mParameters.Count; } }

        /// <summary>
        /// Return the i'th parameter
        /// </summary>
        public SyntaxExpr this[int i] { get { return mParameters[i]; } }

        /// <summary>
        /// Append a parameter to the parameters list. 
        /// </summary>
        public void AddParam(SyntaxExpr e)
        {
            if (mParameters == null)
                mParameters = new List<SyntaxExpr>();
            mParameters.Add(e);
        }

        /// <summary>
        /// Generate an expression list (as if this were lisp)
        /// </summary>
        void ToString(SyntaxExpr expr, StringBuilder sb, int level)
        {
            if (level >= 7)
            {
                sb.Append("*OVF*");
                return;
            }

            if (Count == 0)
            {
                sb.Append(expr.mFunction);
                return;
            }

            if (level != 0)
                sb.Append("(");

            if (expr.mFunction == "(")
                sb.Append("'");
            sb.Append(expr.mFunction);
            if (expr.mFunction == "(")
                sb.Append("'");
    
            foreach (SyntaxExpr param in expr.mParameters)
            {
                sb.Append(", ");
                ToString(param, sb, level+1);
            }
            
            if (level != 0)
                sb.Append(")");
        }

        /// <summary>
        /// Display the expression list (as if this were lisp)
        /// </summary>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            ToString(this, sb, 0);
            return sb.ToString();
        }
    }
}
