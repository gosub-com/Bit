// Copyright 2011-2019 by Jeremy Spiller
// See Lincense.txt for details

using System;
using System.Collections.Generic;

namespace Gosub.Bit
{
    /// <summary>
    /// All symbols relating to a box.  A box is also used as a scope level.
    /// </summary>
    class SyntaxBox
    {
        // Setup by parser
        public bool			Error;
        public SyntaxBox	Parent;		// Null when top level
        public SyntaxDecl	NameDecl = new SyntaxDecl();

        public List<SyntaxDecl>	Params = new List<SyntaxDecl>();
        public SyntaxExpr		Statements = new SyntaxExpr();

        // NOTE: This is both a scope and a box.
        public List<SyntaxBox>	Boxes = new List<SyntaxBox>();
        public List<SyntaxExpr> Constants = new List<SyntaxExpr>();


        /// <summary>
        /// Show the type and name of the box
        /// </summary>
        public override string ToString()
        {
            if (NameDecl == null)
                return "(no name)";
            return NameDecl.TypeName + " " + NameDecl.VariableName;
        }
    }
}
