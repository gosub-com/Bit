// Copyright 2011-2019 by Jeremy Spiller
// See Lincense.txt for details

using System;
using System.Collections.Generic;
using System.Text;

namespace Gosub.Bit
{
    enum OpState : byte
    {
        Zero = 0,	// Value is zero
        One = 1,
        Z1 = 2,
        Z2 = 3
    }


    abstract class OpCode
    {
        public List<OpCode>	Operands;
        public OpState	State;		// Current state
        public OpState	PrevState;	// Previous state
        public OpState	Negate;		// Must be Zero or One
        
        /// <summary>
        /// Delegate used to copy terminals.  The old expression is
        /// passed to the delegate, and the return value is expected
        /// to be a reference to the new expression.
        /// </summary>
        public delegate void VisitTerminalsDelegate(OpCodeTerminal terminal);
        public delegate void VisitNodesDelegate(OpCode opExpr);

        /// <summary>
        /// Evaluate the expression.  If quick is true, the entire
        /// expression will be evaluated immediately.  If quick is
        /// false, only one gate level will be evaluated at a time.
        /// Returns TRUE if this expression (or any sub-expression)
        /// was changed during evaluation.  NOTE: This function
        /// should not be called to evaluate an expression, since
        /// the recursion stops there.
        /// </summary>
        public abstract bool Eval(bool quick);

        /// <summary>
        /// Clone an expression.  Calls the delegate when a 
        /// terminal expression is encountered.  The delegate
        /// may replace the old expression in the copy.
        /// </summary>
        public virtual OpCode DeepClone(OpCode.VisitTerminalsDelegate copyTerm)
        {
            OpCode newOp = (OpCode)MemberwiseClone();
            if (Operands != null)
            {
                newOp.Operands = new List<OpCode>(Operands.Count);
                foreach (OpCode op in Operands)
                    newOp.Operands.Add(op.DeepClone(copyTerm));
            }
            return newOp;
        }

        /// <summary>
        /// Walk the tree, and replace the old OpCode with the new OpCode.
        /// Uses object reference comparisons instead of Equals()
        /// </summary>
        public int ReplaceOperandRef(OpCode oldOp, OpCode newOp)
        {
            if (Operands == null)
                return 0;
            int replaced = 0;
            for (int i = 0; i < Operands.Count; i++)
            {
                replaced += Operands[i].ReplaceOperandRef(oldOp, newOp);
                if ((object)Operands[i] == (object)oldOp)
                {
                    Operands[i] = newOp;
                    replaced++;
                }
            }
            return replaced;
        }

        /// <summary>
        /// Visit all the terminals in this expression
        /// </summary>
        public virtual void VisitTerminals(OpCode.VisitTerminalsDelegate visit)
        {
            if (Operands != null)
                foreach (OpCode op in Operands)
                    op.VisitTerminals(visit);
        }


        /// <summary>
        /// Visit all the nodes in this expression
        /// </summary>
        public virtual void VisitNodes(VisitNodesDelegate visit)
        {
            visit(this);
            if (Operands != null)
                for (int i = 0;  i < Operands.Count;  i++)
                    Operands[i].VisitNodes(visit);
        }


        /// <summary>
        /// Return a shallow clone of this expression
        /// </summary>
        public OpCode ShallowClone()
        {
            return (OpCode)MemberwiseClone();
        }

        /// <summary>
        /// Print this expression (recursive print with parenthisis)
        /// </summary>
        public virtual void Print(StringBuilder sb)
        {
            sb.Append("(null)");
        }

        /// <summary>
        /// Print expression
        /// </summary>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            Print(sb);
            return sb.ToString();
        }
        
        /// <summary>
        /// Counts the number of gates in this expression.
        /// </summary>
        public virtual int CountGates()
        {
            return 0;
        }

        public bool HasOperands
        {
            get { return Operands != null; }
        }

        public override bool Equals(object obj)
        {
            OpCode op = obj as OpCode;
            if (op == null)
                return false;
            if (Negate != op.Negate)
                return false;
            if (Operands == null && op.Operands == null)
                return true;
            if (Operands == null || op.Operands == null)
                return false;
            if (Operands.Count != op.Operands.Count)
                return false;
            int i = 0;
            foreach (OpCode opCode in Operands)
            {
                if (!opCode.Equals(op.Operands[i++]))
                    return false;
            }
            return true;
        }

        public int MixHash(int a)
        {
            return (a << 7) ^ (a >> 25);
        }

        public override int GetHashCode()
        {
            int hash = (int)Negate + 1;
            if (Operands == null)
                return hash;
            hash = MixHash(hash) + 1;
            foreach (OpCode op in Operands)
                hash = MixHash(hash) + op.GetHashCode();
            return hash;
        } 


    }

    class OpCodeConst : OpCode
    {
        OpState mState;

        public OpCodeConst()
        {
        }

        /// <summary>
        /// c must be 0 or 1
        /// </summary>
        public OpCodeConst(int c)
        {
            if ((c & 1) != 0)
                mState = OpState.One;
        }

        public override bool Eval(bool quick)
        {
            OpState newState = mState ^ Negate;
            bool changed = newState != State || newState != PrevState;;
            State = newState;
            PrevState = newState;
            return changed;
        }

        public override void Print(StringBuilder sb)
        {
            Eval(false); // Ensure we have the current state
            if (State == OpState.Zero)
                sb.Append("0");
            else if (State == OpState.One)
                sb.Append("1");
            else
                sb.Append("Z");
        }

        public override bool Equals(object obj)
        {
            OpCodeConst op = obj as OpCodeConst;
            if (op == null)
                return false;
            Eval(false); // Ensure we have the current state
            op.Eval(false);
            return State == op.State;
        }

        public override int GetHashCode()
        {
            Eval(false); // Ensure we have the current state
            return (int)State;
        }

    }

    class OpCodeInParam : OpCode
    {
        public override void Print(StringBuilder sb)
        {
            sb.Append("(in)");
        }
        public override bool Eval(bool quick)
        {
            bool changed = PrevState != State;
            PrevState = State;
            return changed;
        }
        public override bool Equals(object obj)
        {
            OpCodeInParam op = obj as OpCodeInParam;
            if (op == null)
                return false;
            return true;
        
        }
        public override int GetHashCode()
        {
            return 10;
        }
    }

    class OpCodeOutParam : OpCode
    {
        public override void Print(StringBuilder sb)
        {
            sb.Append("(out)");
        }
        public override bool Eval(bool quick)
        {
            bool changed = PrevState == State;
            PrevState = State;
            return changed;
        }
        public override bool Equals(object obj)
        {
            OpCodeOutParam op = obj as OpCodeOutParam;
            if (op == null)
                return false;
            return true;

        }
        public override int GetHashCode()
        {
            return 11;
        }
    }


    class OpCodeTerminal : OpCode
    {
        // The terminal expression
        public OpCodeExpr Expression;

        public OpCodeTerminal(OpCodeExpr term)
        {
            Expression = term;
        }

        public override OpCode DeepClone(OpCode.VisitTerminalsDelegate copyTerm)
        {
            OpCodeTerminal term = (OpCodeTerminal)MemberwiseClone();
            copyTerm(term);
            return term;
        }
        public override void VisitTerminals(OpCode.VisitTerminalsDelegate visit)
        {
            visit(this);
        }
        public override void Print(StringBuilder sb)
        {
            if (Expression == null)
                sb.Append("(null)");
            else
            {
                if (Negate != OpState.Zero)
                    sb.Append("!");
                Expression.PrintTerminal(sb);
            }
        }

        /// <summary>
        /// Evaluate a terminal (wires are instant, NOT gates are not)
        /// </summary>
        public override bool Eval(bool quick)
        {
            // Trace through wires (do not allow infinite recursion)
            OpCodeTerminal next = this;
            OpCodeTerminal final = this;
            int max = 32;
            while (next != null && next.Negate == OpState.Zero && --max >= 0)
            {
                final = next;
                next = next.Expression.Expr as OpCodeTerminal;
            }

            // Calculate new value
            OpState oldState = State;
            State = PrevState;
            PrevState = final.Expression.Expr.State ^ final.Negate;
            bool changed = PrevState != State || PrevState != oldState;

            // Wires are always quick (no time delay).
            if (final.Negate == OpState.Zero || quick)
                State = PrevState;
            return changed;
        }
    
        public override int CountGates()
        {
            if (Negate == OpState.Zero || Expression == null)
                return 0;
            if (Expression.CountedNegative)
                return 0;
            Expression.CountedNegative = true;
            return 1;
        }

        public override bool Equals(object obj)
        {
            OpCodeTerminal op = obj as OpCodeTerminal;
            if (op == null)
                return false;
            return Expression.Index == op.Expression.Index
                    && Expression.Name == op.Expression.Name
                    && Negate == op.Negate;
        }
        public override int GetHashCode()
        {
            return (Expression.Index << 16) + Expression.Index
                    + Expression.Name.GetHashCode() + (int)Negate;
        }

    }


    class OpCodeMath:OpCode
    {
        public override int CountGates()
        {
            int count = Math.Max(1, Operands.Count-1);
            foreach (OpCode op in Operands)
                count += op.CountGates();
            return count;		
        }

        public virtual string Name()
        {
            return "(math)";
        }

        public override void Print(StringBuilder sb)
        {
            if (Negate == OpState.Zero)
                sb.Append("(");
            else
                sb.Append("!(");

            if (Operands.Count <= 1)
                sb.Append(Name());

            int i = 0;
            foreach (OpCode op in Operands)
            {
                Operands[i].Print(sb);
                if (++i != Operands.Count)
                {
                    sb.Append(" ");
                    sb.Append(Name());
                    sb.Append(" ");
                }
            }
            sb.Append(")");
        }

        public override bool Eval(bool quick)
        {
            return false;
        }

        public override bool Equals(object obj)
        {
            OpCodeMath op = obj as OpCodeMath;
            if (op == null)
                return false;
            if (Name() != op.Name())
                return false;
            return base.Equals(op);
        }
        public override int GetHashCode()
        {
            return Name().GetHashCode() + MixHash(base.GetHashCode());
        }

    }


    class OpCodeAnd : OpCodeMath
    {
        public OpCodeAnd()
        {
        }

        public OpCodeAnd(List<OpCode> operands)
        {
            Operands = operands;
        }

        public OpCodeAnd(OpCode p1, OpCode p2)
        {
            Operands = new List<OpCode>(2);
            Operands.Add(p1);
            Operands.Add(p2);
        }

        public override string Name()
        {
            return "*";
        }

        public override bool Eval(bool quick)
        {
            // Allow all sub-expressions to caluclate
            bool changed = false;
            foreach (OpCode c in Operands)
                changed |= c.Eval(quick);

            // Calculate new value
            OpState oldState = State;
            State = PrevState;
            OpState answer = OpState.One;  // Default for AND gate
            foreach (OpCode c in Operands)
                answer &= c.State; // AND GATE

            // Optionally negate output and make it quick
            PrevState = (answer ^ Negate) & OpState.One;
            if (quick)
                State = PrevState;
            return changed || PrevState != State || PrevState != oldState;
        }


    }

    class OpCodeOr:OpCodeMath
    {

        public OpCodeOr()
        {
        }

        public OpCodeOr(List<OpCode> operands)
        {
            Operands = operands;
        }

        public OpCodeOr(OpCode p1, OpCode p2)
        {
            Operands = new List<OpCode>(2);
            Operands.Add(p1);
            Operands.Add(p2);
        }

        public override string Name()
        {
            return "+";
        }

        public override bool Eval(bool quick)
        {
            // Allow all sub-expressions to caluclate
            bool changed = false;
            foreach (OpCode c in Operands)
                changed |= c.Eval(quick);

            // Calculate new value
            OpState oldState = State;
            State = PrevState;
            OpState answer = OpState.Zero;  // Default for OR gate
            foreach (OpCode c in Operands)
                answer |= c.State; // OR GATE

            // Optionally negate output and make it quick
            PrevState = (answer ^ Negate) & OpState.One;
            if (quick)
                State = PrevState;
            return changed || PrevState != State || PrevState != oldState;
        }
    
    }

    class OpCodeXor:OpCodeMath
    {
        public OpCodeXor()
        {
        }

        public OpCodeXor(OpCode p1, OpCode p2)
        {
            Operands = new List<OpCode>(2);
            Operands.Add(p1);
            Operands.Add(p2);
        }

        public override string Name()
        {
            return "#";
        }

        public override bool Eval(bool quick)
        {
            // Allow all sub-expressions to caluclate
            bool changed = false;
            foreach (OpCode c in Operands)
                changed |= c.Eval(quick);

            // Calculate new value
            OpState oldState = State;
            State = PrevState;
            OpState answer = OpState.Zero;  // Default for XOR gate
            foreach (OpCode c in Operands)
                answer ^= c.State; // XOR GATE

            // Optionally negate output and make it quick
            PrevState = (answer ^ Negate) & OpState.One;
            if (quick)
                State = PrevState;
            return changed || PrevState != State || PrevState != oldState;
        }
    }


    class OpCodeExpr
    {
        public string	Name = "";	// Optional name (use "" if no name)
        public OpCode	Expr;		// Is NULL when unassigned

        /// <summary>
        /// Absolute index of this expression.  Set by linker after
        /// the index of the expression is known.
        /// </summary>
        public int		Index = -1;		
        
        // Cheap trick to not count repeat negative expressions
        public bool		CountedNegative;


        /// <summary>
        /// Create an OpCodeExpression with the given type and name.  The offset
        /// is the position within the field (or -1 if a single bit field).
        /// </summary>
        public OpCodeExpr(string name)
        {
            Name = name;
        }

        public OpCodeExpr(string name, OpCode expr)
        {
            Name = name;
            Expr = expr;
        }

        public OpCodeExpr(OpCode expr)
        {
            Name = "";
            Expr = expr;
        }

        /// <summary>
        /// Print the name of an expression
        /// </summary>
        public void PrintTerminal(StringBuilder sb)
        {
            if (Name == "" && Index < 0)
                sb.Append("(not named)");
            else if (Name == "")
                sb.Append("E" + Index);
            else
                sb.Append(Name);
        }

        /// <summary>
        /// Print the expression and all of its sub-expressions
        /// </summary>
        public void PrintExpression(StringBuilder sb)
        {
            PrintTerminal(sb);
            sb.Append(" = ");
            if (Expr == null)
                sb.Append("(unassigned)");
            else
                Expr.Print(sb);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            PrintExpression(sb);
            return sb.ToString();
        }

        /// <summary>
        /// Walk the tree, and replace the old OpCode with the new OpCode.
        /// Uses object reference comparisons instead of Equals()
        /// </summary>
        public int ReplaceOperandRef(OpCode oldOp, OpCode newOp)
        {
            int r = 0;
            if (Expr == null)
                return 0;
            r = Expr.ReplaceOperandRef(oldOp, newOp);
            if ((object)Expr == (object)oldOp)
            {
                Expr = newOp;
                r++;
            }
            return r;
        }


    }


}
