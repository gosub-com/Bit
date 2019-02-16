// Copyright 2011-2019 by Jeremy Spiller
// See Lincense.txt for details

using System;
using System.Collections.Generic;

namespace Gosub.Bit
{
    /// <summary>
    /// Basic types recognized by the lexer
    /// </summary>
    public enum eTokenType : short
    {
        // Lexer recognized types:
        Normal,
        Reserved,
        ReservedName,
        Identifier,
        Comment
    }


    /// <summary>
    /// Each lexical element is assigned a new token.  This class is
    /// also used by the parser and code generator to mark tokens with 
    /// error codes and other information.
    /// </summary>
    public class Token
    {
        // Fields
        public string		Name = "";
        public TokenLoc		Location;
        public Object		Info;
        public eTokenType	Type;
        public bool			Error;

        /// <summary>
        /// This property is an alias for Info (when assigning InfoString,
        /// Info is set to the string).  If Info is null or is not a string,
        /// the empty string ("") is returned.
        /// </summary>
        public string InfoString
        {
            get
            {
                string infoString = infoString = Info as string;
                if (infoString == null)
                    return "";
                return infoString;
            }
            set
            {
                Info = value;
            }
        }
    
        public int Line
        {
            get { return Location.Line; }
            set { Location.Line = value; }
        }
        public int Char
        {
            get { return Location.Char; }
            set { Location.Char = value; }
        }

        public Token()
        {
        }
        

        public Token(string tokenName, int lineIndex, int charIndex)
        {
            Name = tokenName;
            Line = lineIndex;
            Char = charIndex;
        }

        public Token(string tokenName, int lineIndex, int charIndex, eTokenType tokenType)
        {
            Name = tokenName;
            Line = lineIndex;
            Char = charIndex;
            Type = tokenType;
        }

        public override string ToString()
        {
            return Name;
        }

        /// <summary>
        /// Append a message to the message string (adding a line
        /// feed if necessary, and removing duplicate messages)
        /// </summary>
        public void AppendMessage(string message)
        {
            if (InfoString == "")
                InfoString = message;
            else if (!InfoString.Contains(message))
                InfoString += "\r\n" + message;
        }

        /// <summary>
        /// Reject this token (set error flag and append the message)
        /// NOTE: Duplicate error messages are ignored
        /// </summary>
        public void Reject(string errorMessage)
        {
            // Display error message
            Error = true;
            AppendMessage(errorMessage);
        }

        /// <summary>
        /// Convert this token to a string
        /// </summary>
        public static implicit operator string(Token token)
        {
            return token.Name;
        }

    }

    /// <summary>
    /// Keep track of the token location (Line, Char)
    /// </summary>
    public struct TokenLoc
    {
        /// <summary>
        /// Line number of token
        /// </summary>
        public int		Line;

        /// <summary>
        /// Column of token
        /// </summary>
        public int		Char;

        public TokenLoc(int lineIndex, int charIndex)
        {
            Line = lineIndex;
            Char = charIndex;
        }

        /// <summary>
        /// Ensure that low >= high (swap them if they are not)
        /// </summary>
        public static void FixOrder(ref TokenLoc low, ref TokenLoc high)
        {
            if (low > high)
            {
                TokenLoc temp = low;
                low = high;
                high = temp;
            }
        }

        public static bool operator==(TokenLoc a, TokenLoc b)
        {
            return a.Line == b.Line && a.Char == b.Char;
        }
        public static bool operator!=(TokenLoc a, TokenLoc b)
        {
            return !(a == b);
        }
        public static bool operator>(TokenLoc a, TokenLoc b)
        {
            return a.Line > b.Line || a.Line == b.Line && a.Char > b.Char;
        }
        public static bool operator<(TokenLoc a, TokenLoc b)
        {
            return a.Line < b.Line || a.Line == b.Line && a.Char < b.Char;
        }
        public static bool operator>=(TokenLoc a, TokenLoc b)
        {
            return a.Line > b.Line || a.Line == b.Line && a.Char >= b.Char;
        }
        public static bool operator<=(TokenLoc a, TokenLoc b)
        {
            return a.Line < b.Line || a.Line == b.Line && a.Char <= b.Char;
        }
        public override int GetHashCode()
        {
            return (Line << 12) + Char;
        }
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (!(obj is TokenLoc))
                return false;
            return this == (TokenLoc)obj;
        }
        public override string ToString()
        {
            return "" + "Line: " + Line + ", Char: " + Char;
        }

    }

}
