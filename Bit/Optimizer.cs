// Copyright 2011-2019 by Jeremy Spiller
// See Lincense.txt for details

using System;
using System.Collections.Generic;

namespace Gosub.Bit
{
    /// <summary>
    /// This class optimizes the code
    /// </summary>
    class Optimizer
    {
        int	mAssociative;
        int	mDemorgans;
        int	mRemoveConst;
        int	mRemoveConstExpr;
        int	mRemoveUnary;
        int	mEmbeddedSingletons;
        int	mIdentityToConst;
        int	mIdentityToSelf;
        int	mUnused;
        int mWires;
        int mConstWires;
        int mIterations;

        /// <summary>
        /// Non recursively remove constants:
        ///     A * 1 = A
        ///     A + 0 = A
        ///     A # 0 = A
        ///     A # 1 = !A
        ///     A * 0 = 0
        ///     A + 1 = 1
        /// </summary>
        public static OpCode RemoveConstNR(OpCodeMath op,
                                            ref int removeConst, 
                                            ref int removeConstExpr,
                                            ref int removeUnary)
        {
            // Remove all constants
            Type opType = op.GetType();
            for (int i = 0; i < op.Operands.Count; i++)
            {
                // Must leave atleast one operand to know the value
                if (op.Operands.Count <= 1)
                    break;

                // Skip non-constant values
                OpCodeConst constTerm = op.Operands[i] as OpCodeConst;
                if (constTerm == null)
                    continue; // Skip non-constants
                constTerm.Eval(true);  // Ensure Negate is not set
                if (constTerm.State != OpState.Zero && constTerm.State != OpState.One)
                    continue; // Skip non-boolean states

                // Remove constant:
                //      A * 1 = A
                //      A + 0 = A
                //      A # 0 = A
                //      A # 1 = !A
                if (opType == typeof(OpCodeXor)
                    || opType == typeof(OpCodeAnd) && constTerm.State == OpState.One
                    || opType == typeof(OpCodeOr) && constTerm.State == OpState.Zero)
                {
                    // Check for (A # 1) = !A
                    if (opType == typeof(OpCodeXor) && constTerm.State == OpState.One)
                        op.Negate ^= OpState.One;

                    // Remove constant
                    op.Operands.RemoveAt(i);
                    i--; // Retry this operand
                    removeConst++;
                    continue; // Continue processing all operands
                }

                // Remove constant:
                //	    A * 0 = 0
                //	    A + 1 = 1
                if (opType == typeof(OpCodeAnd) && constTerm.State == OpState.Zero
                    || opType == typeof(OpCodeOr) && constTerm.State == OpState.One)
                {
                    // Remove entire sub-expression
                    op.Operands.Clear();
                    op.Operands.Add(constTerm);
                    removeConstExpr++;
                    break; // Done with all processing
                }
            }

            // Check for only one operand
            if (op.Operands.Count == 1)
            {
                // Remove this gate, and return the operand.
                // AND, OR, XOR = nop.  NAND, NOR, XNOR = inverter.
                removeUnary++;
                op.Operands[0].Negate ^= op.Negate;
                return op.Operands[0];
            }
            return op;
        }


        /// <summary>
        /// Scan assignment chain for the last expression
        /// (e.g. TraceTerm(a) on a = b, b = c, c = (x+c) returns c)
        /// </summary>
        OpCodeTerminal TraceTerm(OpCodeTerminal term)
        {
            int tries = 8;
            OpCodeTerminal trace = term.Expression.Expr as OpCodeTerminal;
            while (trace != null && trace.Expression.Expr.Negate == OpState.Zero
                    && --tries != 0)
            {
                term = trace;
                trace = term.Expression.Expr as OpCodeTerminal;
            }
            return term;
        }

        /// <summary>
        /// Non-recursively remove identities:
        ///	    a+!a = 1
        ///	    a*!a = 0
        ///	    a+a = a
        ///	    a*a = a
        /// </summary>
        OpCode RemoveIdentityNR(OpCode op)
        {
            if (op.Operands == null)
                return op;
            int identity = 0;
            if (op is OpCodeOr)
                identity = 1;
            else if (!(op is OpCodeAnd))
                return op;

            for (int i = 0;  i < op.Operands.Count;  i++)
            {
                OpCodeTerminal t1 = op.Operands[i] as OpCodeTerminal;
                if (t1 == null)
                    continue;
                for (int j = i+1;  j < op.Operands.Count;  j++)
                {
                    OpCodeTerminal t2 = op.Operands[j] as OpCodeTerminal;
                    if (t2 == null)
                        continue;

                    // Follow wires to their terminal value
                    OpCodeTerminal trTerm1 = TraceTerm(t1);
                    OpCodeTerminal trTerm2 = TraceTerm(t2);

                    // a+!a = 1
                    // a*!a = 0
                    if (trTerm1.Expression == trTerm2.Expression
                        && (t1.Negate ^ t2.Negate) == OpState.One)
                    {
                        mIdentityToConst++;
                        return new OpCodeConst(identity);
                    }
                    //	a+a = a
                    //	a*a = a
                    if (trTerm1.Expression == trTerm2.Expression
                            && ((t1.Negate == OpState.Zero && t2.Negate == OpState.Zero)
                                    || (t1.Negate == OpState.One && t2.Negate == OpState.One)))
                    {
                        op.Operands.RemoveAt(j);
                        j--;  // Retry this operand
                        mIdentityToSelf++;
                    }
                }
            }
            return op;
        }


        /// <summary>
        /// Recursively remove extraneuous levels of parenthises:
        ///	    a+(b+c) = a+b+c			Associative
        ///	    a*(b*c) = a*b*c
        ///	    a#(b#c) = a#b#c
        ///	    a#!(b#c) = !(a#b#c)
        ///	    a+!(b*c) = a+!b+!c		Demorgan's law
        ///	    a*!(b+c) = a*!b*!c
        ///	    Remove constants
        ///	    Remove identity
        ///	Same for # and *
        /// </summary>
        public OpCode Flatten(OpCode op)
        {
            OpCodeMath math = op as OpCodeMath;
            if (math == null)
                return op;
            Type opType = op.GetType();
            if (opType != typeof(OpCodeAnd)
                    && opType != typeof(OpCodeOr)
                    && opType != typeof(OpCodeXor))
                return op;

            // Flatten all sub-trees first
            for (int i = 0; i < math.Operands.Count; i++)
                math.Operands[i] = Flatten(math.Operands[i]);

            // Flatten this branch
            for (int i = 0; i < math.Operands.Count; i++)
            {
                OpCodeMath newMath = math.Operands[i] as OpCodeMath;
                if (newMath == null)
                    continue;

                // Associative:
                //      a+(b+c) = a+b+c
                //      a*(b*c) = a*b*c
                //      a#(b#c) = a#b#c
                //      a#!(b#c) = !(a#b#c)
                if (newMath.GetType() == opType  // Op codes must be same
                        && (newMath.Negate == OpState.Zero
                            || newMath.Negate == OpState.One && newMath is OpCodeXor))
                {
                    math.Operands.RemoveAt(i);
                    math.Operands.AddRange(newMath.Operands);
                    math.Negate ^= newMath.Negate;
                    mAssociative++;
                    i--;  // Retry this operand
                    continue;
                }

                // Demorgan's law:
                //      a+!(b*c) = a+!b+!c		Demorgan's law
                //      a*!(b+c) = a*!b*!c		Demorgan's law
                if (newMath.Negate == OpState.One)
                    if (op is OpCodeOr && newMath is OpCodeAnd
                        || op is OpCodeAnd && newMath is OpCodeOr)
                    {
                        math.Operands.RemoveAt(i);
                        foreach (OpCode newOp in newMath.Operands)
                        {
                            // Perform demorgans law (negate each sub-op code)
                            newOp.Negate ^= OpState.One;
                            math.Operands.Add(newOp);
                        }
                        mDemorgans++;
                        i--;  // Retry this operand
                        continue;
                    }
            }
            return RemoveIdentityNR(RemoveConstNR(math, ref mRemoveConst, ref mRemoveConstExpr, ref mRemoveUnary));
        }

        /// <summary>
        /// Embed expressions that have only been used once
        /// </summary>
        OpCode EmbedSingleUses(List<OpCodeExpr> expressions, int index, OpCode op, 
                                int[] timesUsed, int minOpt)
        {
            // Recurse down the expression tree
            if (op.Operands != null)
                for (int i = 0;  i < op.Operands.Count;  i++)
                    op.Operands[i] = EmbedSingleUses(expressions, index, op.Operands[i],
                                                    timesUsed, minOpt);

            // Embed terminal expressions that have only been used once,
            // and are within the optimization range, and are not this expression
            OpCodeTerminal term = op as OpCodeTerminal;
            if (term == null || timesUsed[term.Expression.Index] != 1
                             || term.Expression.Index < minOpt
                             || index == term.Expression.Index)
                return op;


            // Replace this terminal with the expression 
            // (i.e. make it a sub-expression)
            OpCode newOp = expressions[term.Expression.Index].Expr;
            newOp.Negate ^= term.Negate;
            expressions[term.Expression.Index].Expr = null;
            expressions[term.Expression.Index] = null;
            term.Expression = null;
            mEmbeddedSingletons++;
            return newOp;
        }

        /// <summary>
        /// Embed wires and constant wires
        /// </summary>
        OpCode EmbedWires(List<OpCodeExpr> expressions, OpCode op, int minOpt)
        {
            // Recurse down the expression tree
            if (op.Operands != null)
                for (int i = 0;  i < op.Operands.Count;  i++)
                    op.Operands[i] = EmbedWires(expressions, op.Operands[i], minOpt);

            // Replace only terminal
            OpCodeTerminal term = op as OpCodeTerminal;
            if (term == null)
                return op;
            
            // Replace wire expressions (if the wire doesn't point outside of minOpt)
            OpCodeTerminal wire = term.Expression.Expr as OpCodeTerminal;
            if (wire != null && term.Expression.Index >= minOpt)
            {
                // Replace this terminal with the wire's target
                // (i.e. follow the wire)
                term.Negate ^= wire.Negate;
                term.Expression = wire.Expression;
                mWires++;
                return term;
            }
            // Replace constant expressions
            OpCodeConst constTerm = term.Expression.Expr as OpCodeConst;
            if (constTerm != null)
            {
                constTerm.Eval(true);
                if (constTerm.State == OpState.Zero || constTerm.State == OpState.One)
                {
                    mConstWires++;
                    return new OpCodeConst( ((int)term.Negate ^ (int)constTerm.State) & 1);
                }
            }

            return term;
        }

        /// <summary>
        /// Embed single uses and wires
        /// </summary>
        void Embed(List<OpCodeExpr> expressions, int minOpt)
        {
            if (minOpt >= expressions.Count)
                return;

            // Ensure indices are correct
            int index = 0;
            foreach (OpCodeExpr expr in expressions)
                expr.Index = index++;

            // Make times used list
            int []timesUsed = new int[expressions.Count];
            for (int i = 0; i < expressions.Count; i++)
                expressions[i].Expr.VisitTerminals(delegate(OpCodeTerminal term)
                    { timesUsed[term.Expression.Index]++; });

            // Embed singly used expressions
            for (int i = 0; i < expressions.Count; i++)
            {
                if (expressions[i] != null)
                    expressions[i].Expr = EmbedSingleUses(expressions, i,
                                            expressions[i].Expr, timesUsed, minOpt);
            }

            // Trace wires
            for (int i = 0; i < expressions.Count; i++)
            {
                if (expressions[i] != null)
                    expressions[i].Expr = EmbedWires(expressions,
                                            expressions[i].Expr, minOpt);
            }

            // Re-make times used list
            Array.Clear(timesUsed, 0, timesUsed.Length);
            for (int i = 0; i < expressions.Count; i++)
                if (expressions[i] != null)
                    expressions[i].Expr.VisitTerminals(delegate(OpCodeTerminal term)
                        { timesUsed[term.Expression.Index]++; });

            // Nuke unused expressions
            for (int i = minOpt;  i < expressions.Count;  i++)
                if (expressions[i] != null && timesUsed[i] == 0)
                {
                    // Nuke everything so there can't be a mistake down the line
                    expressions[i].Expr.Operands = null;
                    expressions[i].Expr = null;
                    expressions[i] = null;
                }

            // Remove un-used expressions
            int endIndex = 0;
            for (int i = 0; i < expressions.Count; i++)
            {
                if (expressions[i] != null)
                    expressions[endIndex++] = expressions[i];
                else
                    mUnused++;
            }
            expressions.RemoveRange(endIndex, expressions.Count-endIndex);
        }

        class HashOpCode
        {
            public OpCode		Op;
            public OpCodeExpr	Expr;
            public OpCodeExpr	Dup;
            public int			Count;

            public HashOpCode(OpCode op, OpCodeExpr expr)
            {
                Op = op;
                Expr = expr;
            }

            public override string ToString()
            {
                return "" + Op + ": " + Expr;
            }
        }

        /// <summary>
        /// Remove duplicate expressions
        /// </summary>
        void RemoveDuplicateExpressions(List<OpCodeExpr> expressions)
        {
            Dictionary<OpCode, HashOpCode> subExpressions = new Dictionary<OpCode, HashOpCode>();

            // Must use temp count because we are adding expressions as we go
            int count = expressions.Count;
            for (int i = 0;  i < count;  i++)
                if (expressions[i].Expr != null)
                    expressions[i].Expr.VisitNodes(
                        delegate(OpCode op)
                        {
                            // If this is an expression that has operands
                            if (op.Operands != null && op.Operands.Count != 0)
                            {
                                // Look up expression
                                HashOpCode info;
                                if (!subExpressions.TryGetValue(op, out info))
                                {
                                    // First time encountered (create info record)
                                    info = new HashOpCode(op, expressions[i]);
                                    subExpressions[op] = info;
                                }
                                // The first time
                                info.Count++;

                                switch (info.Count)
                                {
                                    case 0: // The first time, don't do anything
                                    case 1:
                                        break;
                                    case 2: // The second time, create a new expression
                                        info.Dup = new OpCodeExpr("D" + expressions.Count, op);
                                        info.Dup.Index = expressions.Count;
                                        expressions.Add(info.Dup);

                                        // Replace the first and second with the new expression
                                        info.Expr.ReplaceOperandRef(info.Op, new OpCodeTerminal(info.Dup));
                                        expressions[i].ReplaceOperandRef(op, new OpCodeTerminal(info.Dup));
                                        break;
                                    default:
                                    case 3:
                                        // Replace the new op with the duplicate
                                        expressions[i].ReplaceOperandRef(op, new OpCodeTerminal(info.Dup));
                                        break;
                                }
                            }
                        });
        }


        /// <summary>
        /// Optimize the given expressions (the given list is optimized)
        /// </summary>
        public Optimizer(List<OpCodeExpr> expressions, int minOptimizeIndex)
        {
            string prevStats = "";

            // Iteratively optimize the boxes
            for (int i = 0;  i < 16;  i++)
            {
                // Perform one optimization iteration
                Embed(expressions, minOptimizeIndex);
                foreach (OpCodeExpr expr in expressions)
                    expr.Expr = Flatten(expr.Expr);
                mIterations++;

                // Easy way to compare a bunch of stuff
                string newStats = ""
                        + mAssociative + " "
                        + mDemorgans + " "
                        + mRemoveConst + " "
                        + mRemoveConstExpr + " "
                        + mRemoveUnary + " "
                        + mEmbeddedSingletons + " "
                        + mIdentityToConst + " "
                        + mIdentityToSelf + " "
                        + mUnused + " "
                        + mWires + " "
                        + mConstWires;
                
                if (newStats == prevStats)
                    break;
                prevStats = newStats;		
            }

            RemoveDuplicateExpressions(expressions);
            RemoveDuplicateExpressions(expressions);

            // NOTE: This causes us to generate invalid operands (or operator with no operands)
            //Embed(expressions, minOptimizeIndex);


        }
    }
}
