// Copyright 2011-2019 by Jeremy Spiller
// See Lincense.txt for details

using System;
using System.Collections.Generic;

namespace Gosub.Bit
{
    /// <summary>
    /// Struct to hold the result of a calculation (RValue = return value)
    /// Currently this class supports 3 types: bit, int, and range.
    /// AllowDup is a special kinds of bit that allows us to duplicate the bit.
    /// NOTE: In and out parameter types are converted to bit.
    /// </summary>
    struct RValue
    {
        public	bool		Success;
        public	string		BaseType;				// bit, int, range
        public	long		IntValue;
        public	long		RangeLengthValue;

        public	Token		AllowDup;
        public	List<OpCode> BitValue;

        /// <summary>
        /// Create a new RValue with the given type (Success = true)
        /// </summary>
        public RValue(string baseType)
        {
            Success = true;
            BaseType = baseType;
            IntValue = 0;
            RangeLengthValue = 0;

            AllowDup = null;
            BitValue = null;
        }


        public RValue(string baseType, long intValue)
        {
            Success = true;
            BaseType = baseType;
            IntValue = intValue;
            RangeLengthValue = 0;

            AllowDup = null;
            BitValue = null;
        }

        public RValue(string baseType, long intValue, long rangeLengthValue)
        {
            Success = true;
            BaseType = baseType;
            IntValue = intValue;
            RangeLengthValue = rangeLengthValue;

            AllowDup = null;
            BitValue = null;
        }

        /// <summary>
        /// Shortcut for Value.Count
        /// </summary>
        public int BitCount { get { return BitValue.Count; } }
        public bool Error { get { return !Success; } }
        
        /// <summary>
        /// Return a failed RValue
        /// </summary>
        public static RValue Failed 
        { 
            get 
            { 
                RValue fail = new RValue();
                fail.BaseType = "(error)";
                return fail;
            } 
        }

        /// <summary>
        /// Get the human readable type name (for error display)
        /// </summary>
        public string GetTypeName()
        {
            if (BaseType == null || !Success)
                return "(error)";
            if ((BaseType == "bit" || BaseType == "bus") && BitValue != null)
                return BaseType + " [" + BitValue.Count + "]";
            if (BaseType == "int")
                return "int(" + IntValue + ")";
            if (BaseType == "range")
                return  RangeLengthValue <= 0 ?
                    "range(" + IntValue + ":" + RangeLengthValue + ")"
                    : "range (" + IntValue + ":" + RangeLengthValue
                        + ") or (" + IntValue + ".." + (IntValue + RangeLengthValue - 1) + ")";
            return BaseType;
        }

        public override string ToString()
        {
            return GetTypeName();
        }

    }

    /// <summary>
    /// Info about a function call (box gets linked in later)
    /// </summary>
    class LinkBox
    {
        public CodeBox			CodeBox;
        public SyntaxExpr		FCall;
        public List<OpCodeExpr> Parameters;
        public int				BaseIndex;

        public override string ToString()
        {
            if (CodeBox == null)
                return "(unused code box)";
            return CodeBox.ToString();
        }
    }

    /// <summary>
    /// Info about an if scope
    /// </summary>
    class IfScope
    {
        public int				InnerConditionIndex;
        public int				MasterConditionIndex;
        public bool				InElse;
        public List<OpCodeExpr>	Assigned = new List<OpCodeExpr>();

    }
}
