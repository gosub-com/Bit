// Copyright 2011-2019 by Jeremy Spiller
// See Lincense.txt for details

using System;
using System.Collections.Generic;

namespace Gosub.Bit
{
    /// <summary>
    /// Intofmation about a code symbol (box, bit, in, out, const)
    /// </summary>
    class Symbol
    {
        public SyntaxDecl	Decl;
        public string		ResolvedName = "";		// Human readable name: "in x[3]", etc.

        public long			ConstValue;
        public int			ArraySize;
        public int			CodeExprIndex = -1;
        public CodeBox		CodeBoxValue;

        public override string ToString()
        {
            return ResolvedName;
        }
    }
}
