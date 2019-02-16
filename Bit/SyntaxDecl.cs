// Copyright 2011-2019 by Jeremy Spiller
// See Lincense.txt for details

using System;
using System.Collections.Generic;

namespace Gosub.Bit
{
    /// <summary>
    /// Declaration for a constant, variable, or box.
    /// </summary>
    class SyntaxDecl
    {
        public Token		TypeName = new Token();		// Box, bit, in, out, bus, int, etc.
        public Token		VariableName = new Token();
        public SyntaxExpr	ArraySizeExpr;				// NULL if not an array

        public override string ToString()
        {
            return TypeName + " " + VariableName 
                    + (ArraySizeExpr == null ? "" : "[" + ArraySizeExpr.ToString() + "]");
        }
    }
}
