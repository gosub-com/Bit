// Copyright 2011-2019 by Jeremy Spiller
// See Lincense.txt for details

using System;
using System.Collections.Generic;

namespace Gosub.Bit
{
    /// <summary>
    /// Lexical analyzer - scan and separate tokens in a file.
    /// </summary>
    public class Lexer
    {
        // Strings and tokens buffer
        List<string> mLines = new List<string>();
        List<List<Token>> mTokens = new List<List<Token>>();
        MinTern mMinTern = new MinTern();
        Dictionary<string, TokenTypeLu> mKeywordsInsensative = new Dictionary<string, TokenTypeLu>();
        Dictionary<string, TokenTypeLu> mKeywordsSensative = new Dictionary<string, TokenTypeLu>();

        /// <summary>
        /// Create an empty lexer
        /// </summary>
        public Lexer()
        {
            ScanLines(new string[] { "" });
        }
        
        /// <summary>
        /// Create a new lexer from the given text
        /// </summary>
        public Lexer(string[] lines)
        {
            ScanLines(lines);
        }

        /// <summary>
        /// Returns the number of lines of text
        /// </summary>
        public int LineCount { get { return mLines.Count; } }

        /// <summary>
        /// Returns a line of text
        /// </summary>
        public string GetLine(int index) { return mLines[index]; }


        /// <summary>
        /// Add a keyword to the default type dictionary.
        /// </summary>
        public void AddKeyword(string keyword, eTokenType type, bool caseSensative)
        {
            /// NOTE: The keyword is interned, and the lexer will use
            /// the interned string when possible.  In other cases,
            /// common strings are pooled by the lexer (see MinTern)
            keyword = string.Intern(keyword);
            if (caseSensative)
            {
                // Add to case sensative
                if (!mKeywordsSensative.ContainsKey(keyword))
                    mKeywordsSensative[string.Intern(keyword)] 
                        = new TokenTypeLu(keyword, type);
            }
            else
            {
                // Add to case insensative
                string lower = string.Intern(keyword.ToLower());
                if (!mKeywordsInsensative.ContainsKey(keyword))
                    mKeywordsInsensative[string.Intern(lower)]
                        = new TokenTypeLu(keyword, type);

            }
        }

        /// <summary>
        /// Sets the identifier type, based on a list of key words words.
        /// Resets the token string with the interned (or min-terned)
        /// version of the string to conserve memory
        /// </summary>
        public Token SetIdentifierType(Token token)
        {
            TokenTypeLu tokenType;
            if (mKeywordsSensative.TryGetValue(token.Name, out tokenType))
            {
                // Case sensative keyword
                token.Name = tokenType.TokenInterned;
                token.Type = tokenType.TokenType;
                return token;
            }

            if (mKeywordsInsensative.Count != 0)
            {
                string lower = token.Name.ToLower();
                if (mKeywordsInsensative.TryGetValue(lower, out tokenType))
                {
                    // Case in-sensative key
                    if (token.Name == tokenType.TokenInterned)
                        token.Name = tokenType.TokenInterned;
                    else
                        token.Name = mMinTern[token.Name];
                    token.Type = tokenType.TokenType;
                    return token;
                }
            }

            // Other tokens
            token.Type = eTokenType.Identifier;
            token.Name = mMinTern[token.Name];
            return token;
        }


        /// <summary>
        /// Returns v, bounded by min and max (or min if min >= max)
        /// </summary>
        int Bound(int v, int min, int max)
        {
            return Math.Max(Math.Min(v, max), min);
        }

        /// <summary>
        /// Gets a section of text.
        /// </summary>
        public string[] GetText(TokenLoc start, TokenLoc end)
        {
            // Bounds check parameters
            start.Line = Bound(start.Line, 0, mLines.Count-1);
            start.Char = Bound(start.Char, 0, mLines[start.Line].Length);
            end.Line = Bound(end.Line, 0, mLines.Count-1);
            end.Char = Bound(end.Char, 0, mLines[end.Line].Length);
            if (end.Line < start.Line)
                end.Line = start.Line;
            if (start.Line == end.Line && end.Char < start.Char)
                end.Char = start.Char;

            int startIndex = start.Char;
            int endIndex = end.Char;

            if (start.Line == end.Line && startIndex >= endIndex)
                return new string[0];

            if (start.Line == end.Line)
            {
                return new string[] { mLines[start.Line].Substring(startIndex, endIndex-startIndex) };
            }

            // Break up the first and last line at the start position
            string []lines = new string[end.Line-start.Line+1];
            lines[0] = mLines[start.Line].Substring(startIndex);
            for (int i = 1; i < lines.Length-1; i++)
                lines[i] = mLines[start.Line+i];
            lines[end.Line-start.Line] = mLines[end.Line].Substring(0, endIndex);
            return lines;
        }

        /// <summary>
        /// Returns a copy of all the text
        /// </summary>
        public string[] GetText()
        {
            return mLines.ToArray();
        }

        /// <summary>
        /// Replace a section of text.  This function is used to insert, 
        /// delete, and change a section of text.  It will re-analyze the
        /// text (replacing all tokens on all lines that were changed)
        /// and updating the positions of the tokens.
        /// RETURNS: The new end location of the inserted text
        /// </summary>
        public TokenLoc ReplaceText(string[] replacementText,
                         TokenLoc start, TokenLoc end)
        {
            // Bounds check parameters
            start.Line = Bound(start.Line, 0, mLines.Count-1);
            start.Char = Bound(start.Char, 0, mLines[start.Line].Length);
            end.Line = Bound(end.Line, 0, mLines.Count-1);
            end.Char = Bound(end.Char, 0, mLines[end.Line].Length);
            if (end.Line < start.Line)
                end.Line = start.Line;
            if (start.Line == end.Line && end.Char < start.Char)
                end.Char = start.Char;

            int startIndex = start.Char;
            int endIndex = end.Char;

            // Adjust first line
            if (start.Line != end.Line || startIndex != endIndex)
                mLines[start.Line] = mLines[start.Line].Substring(0, startIndex)
                                    + mLines[end.Line].Substring(endIndex);

            // Remove unused lines
            if (start.Line != end.Line)
            {
                mLines.RemoveRange(start.Line+1, end.Line-start.Line);
                mTokens.RemoveRange(start.Line+1, end.Line-start.Line);
            }

            // Start and end are the same
            end.Line = start.Line;
            endIndex = startIndex;

            // Insert new text
            if (replacementText != null && replacementText.Length != 0)
            {
                // Break up the first line at the start position
                string startStr = mLines[start.Line].Substring(0, startIndex);
                string endStr = mLines[start.Line].Substring(startIndex);

                if (replacementText.Length <= 1)
                {
                    mLines[start.Line] = startStr + replacementText[0] + endStr;
                    endIndex = startStr.Length + replacementText[0].Length;
                }
                else
                {
                    // Insert new lines
                    mLines[start.Line] = startStr + replacementText[0];
                    for (int i = 1; i < replacementText.Length; i++)
                    {
                        mLines.Insert(start.Line+i, replacementText[i]);
                        mTokens.Insert(start.Line+i, new List<Token>());
                    }
                    end.Line = start.Line + replacementText.Length-1;
                    endIndex = replacementText[replacementText.Length-1].Length;
                    mLines[end.Line] += endStr;
                }
            }

            // Re-scan the updated text lines
            for (int i = start.Line; i <= end.Line; i++)
                mTokens[i] = ScanLine(mLines[i], i);

            // Re-adjust token line positions
            for (int i = start.Line; i < mTokens.Count; i++)
                foreach (Token token in mTokens[i])
                    token.Line = i;

            // Calculate end of inserted text
            end.Char = endIndex;
            return end;
        }

        /// <summary>
        /// Get the next token on the line.  
        /// Returns a "" token if there are none left.
        /// NOTE: The token's LineIndex is set to zero
        /// NOTE: The token is stripped of TABs
        /// </summary>
        Token ScanToken(string line, ref int charIndex)
        {
            // Skip white space
            while (charIndex < line.Length && char.IsWhiteSpace(line[charIndex]))
                charIndex++;

            // Calculate column
            int startIndex = charIndex;

            // End of line?
            if (charIndex >= line.Length)
                return new Token("", 0, startIndex);

            // Identifier
            if (char.IsLetter(line, charIndex))
            {
                // Hop over identifier
                int endIndex = charIndex;
                while (endIndex < line.Length && 
                        (char.IsLetterOrDigit(line, endIndex) || line[endIndex] == '_'))
                    endIndex++;
                string token = line.Substring(charIndex, endIndex - charIndex);
                charIndex = endIndex; // Skip token
                return SetIdentifierType(new Token(token, 0, startIndex));
            }
            // Number
            if (char.IsDigit(line, charIndex))
            {
                // Hop over number
                int endIndex = charIndex;
                while (endIndex < line.Length && char.IsLetterOrDigit(line, endIndex))
                    endIndex++;
                string token = mMinTern[line.Substring(charIndex, endIndex - charIndex)];
                charIndex = endIndex;  // Skip token
                return new Token(token, 0, startIndex);
            }
            // Comment
            if (line[charIndex] == '/' && line.Length > charIndex+1 && line[charIndex+1] == '/')
            {
                int endTIndex = charIndex;
                while (endTIndex < line.Length  && (line[endTIndex] != '\n' ||  line[endTIndex] != '\r'))
                    endTIndex++;
                string token = line.Substring(charIndex, endTIndex - charIndex);
                charIndex = endTIndex;
                return new Token(token, 0, startIndex, eTokenType.Comment);
            }
            // Two char tokens
            if (charIndex+1 < line.Length)
            {
                if (line[charIndex] == '=' && line[charIndex+1] == '=')
                { charIndex += 2; return new Token("==", 0, startIndex); }
                if (line[charIndex] == '!' && line[charIndex+1] == '=')
                { charIndex += 2; return new Token("!=", 0, startIndex); }
                if (line[charIndex] == '.' && line[charIndex+1] == '.')
                { charIndex += 2; return new Token("..", 0, startIndex); }

            }

            // Get single character string
            return new Token(mMinTern[line[charIndex++].ToString()], 0, startIndex);
        }

        /// <summary>
        /// Scan a line
        /// </summary>
        List<Token> ScanLine(string line, int lineIndex)
        {
            int charIndex = 0;

            // Build an array of tokens for this line
            List<Token> tokens = new List<Token>();
            Token token;
            do
            {
                token = ScanToken(line, ref charIndex);
                token.Line = lineIndex;

                // Add everything except LF to this line
                if (token.Name.Length != 0)
                    tokens.Add(token);
            } while (token.Name.Length != 0);

            // Copy tokens to array
            tokens.TrimExcess();
            return tokens;
        }

        /// <summary>
        /// Scan lines of text from an array, completely 
        /// replacing all text in Lines, and all tokens
        /// </summary>
        public void ScanLines(string[] lines)
        {
            // Must have at least one line of text
            if (lines.Length == 0)
                lines = new string[1] { "" };
            mLines = new List<string>(lines.Length);

            mTokens.Clear();

            // For each line
            for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                mLines.Add(lines[lineIndex]);
                mTokens.Add(ScanLine(lines[lineIndex], lineIndex));
            }
        }

        /// <summary>
        /// Iterator to return all tokens
        /// </summary>
        public IEnumerator<Token> GetEnumerator()
        {
            return new Enumerator(this);
        }

        /// <summary>
        /// Iterate through all tokens, starting at startLine
        /// </summary>
        public Enumerator GetEnumeratorStartAtLine(int startLine)
        {
            return new Enumerator(this, startLine);
        }

        /// <summary>
        /// Enumerate tokens in the lexer
        /// </summary>
        public struct Enumerator:IEnumerator<Token>
        {
            static List<Token>	sEmpty = new List<Token>();

            Lexer		mLexer;
            int			mIndexLine;
            int			mIndexToken;
            Token		mCurrent;
            List<Token>	mCurrentLine;

            /// <summary>
            /// Enumerate all tokens
            /// </summary>
            public Enumerator(Lexer lexer)
            {
                mLexer = lexer;
                mIndexLine = 0;
                mIndexToken = 0;
                mCurrentLine = mLexer.mTokens.Count <= 0 ? sEmpty : mLexer.mTokens[0];
                mCurrent = null;
            }

            /// <summary>
            /// Enumerate all tokens, starting at startLine
            /// </summary>
            public Enumerator(Lexer lexer, int startLine)
            {
                mLexer = lexer;
                mIndexLine = Math.Max(0, startLine);
                mIndexToken = 0;
                mCurrentLine = mLexer.mTokens.Count <= mIndexLine ? sEmpty : mLexer.mTokens[mIndexLine];
                mCurrent = null;
            }

            public IEnumerator<Token> GetEnumerator() { return this; }
            public void Dispose() { }
            public Token Current { get { return mCurrent; } }			
            object System.Collections.IEnumerator.Current { get { return mCurrent; } }
            
            public void Reset()
            {
                throw new NotSupportedException("Reset on lexer enumerator is not supported");
            }

            public bool MoveNext()
            {
                // More tokens on this line?
                if (mIndexToken < mCurrentLine.Count)
                {
                    mCurrent = mCurrentLine[mIndexToken++];
                    return true;
                }
                // Move to next non-empty line
                mIndexToken = 0;
                do
                {
                    mIndexLine++;
                } while (mIndexLine < mLexer.mTokens.Count && mLexer.mTokens[mIndexLine].Count == 0);
                
                // Return next token
                if (mIndexLine < mLexer.mTokens.Count)
                {
                    mCurrentLine = mLexer.mTokens[mIndexLine];
                    mCurrent = mCurrentLine[mIndexToken++];
                    return true;
                }
                mCurrent = null;
                return false;
            }
        }


        /// <summary>
        /// Helper class to look up token types and save the interned string
        /// </summary>
        class TokenTypeLu
        {
            public string		TokenInterned;
            public eTokenType	TokenType;
            public TokenTypeLu(string tokenInterned, eTokenType tokenType)
            {
                TokenInterned = tokenInterned;
                TokenType = tokenType;
            }
        }

    }
}
