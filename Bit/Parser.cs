// Copyright 2011-2019 by Jeremy Spiller
// See Lincense.txt for details

using System;
using System.Collections.Generic;

namespace Gosub.Bit
{
    /// <summary>
    /// Parse part of a program
    /// </summary>
    class Parser
    {
        bool				mParseError;	// Flag is set whenever a parse error occurs
        Lexer				mLexer;			// Lexer to be paresed
        Lexer.Enumerator	mLexerEnum;		// Enumerator for the Lexer
        string				mTokenName;
        Token				mToken;

        string []REJECT_BOX = new string[] { "", "box" };
        string []REJECT_BOX_PROTOTYPE = new string[] { "", "box", "is", ";" };
        string []REJECT_LINE = new string[] { "", "box", ";" };
        string []REJECT_PAREN = new string[]{"", "box", ";", ")"};
        string []REJECT_BRACKET = new string[]{"", "box", ";", "]"};
        string []REJECT_PARAMETER =  new string[]{"", "box", ";", ",", ")" };

        // These tokens are generated and used internally
        Token mInternalTokenStatement = new Token("{", 0, 0);
        Token mInternalTokenIf = new Token("if", 0, 0);

        /// <summary>
        /// Parse the given lexer
        /// </summary>
        public Parser(Lexer tokens)
        {
            mLexer = tokens;
            mLexerEnum = new Lexer.Enumerator(mLexer);
            Accept();
        }

        /// <summary>
        /// Accept the current token and advance to the next, skipping all comments.
        /// The new token is saved in mToken and the token name is saved in mTokenName.
        /// Returns the token that was accepted.
        /// </summary>
        Token Accept()
        {
            // Already at end of file?
            Token previousToken = mToken;
            if (mTokenName == "")
                return previousToken;

            // Read next token, and skip comments
            do
            {
                // Read next token (set EOF flag if no more tokens on line)
                if (mLexerEnum.MoveNext())
                    mToken = mLexerEnum.Current;
                else
                    mToken = new Token("", 0, 0, eTokenType.Reserved);
            } while (mToken.Type == eTokenType.Comment);

            
            // Reset token info
            mTokenName = mToken.Name;
            mToken.Info = null;
            mToken.Error = false;
            return previousToken;
        }
        
        /// <summary>
        /// Accept the given token (throw an exception if name is incorrect)
        Token Accept(string name)	
        {
            if (mTokenName != name)
                throw new Exception("Error: Expecing \'" + name + "\', found \'" + mTokenName + "\'");
            return Accept();
        }

        /// <summary>
        /// Mark the given token as having an error
        /// </summary>
        void Reject(Token token, string errorMessage)
        {
            mParseError = true;
            token.Reject(errorMessage);
        }

        /// <summary>
        /// Reject the current token with the given error message.
        /// The new error message is appended to the list of errors.
        /// </summary>
        void Reject(string errorMessage)
        {
            Reject(mToken, errorMessage);
        }

        /// <summary>
        /// Rejects tokens until one of the stopTokens is found.
        /// Marks at least one token with the error message.
        /// </summary>
        SyntaxExpr Reject(string errorMessage, string []stopTokens)
        {
            // Mark all tokens to end of line with an error
            Reject(errorMessage);
            SyntaxExpr result = new SyntaxExpr(mToken);

            // Skip the rest of this statement
            while (true)
            {
                if (mTokenName == "")
                    return result;
                foreach (string token in stopTokens)
                    if (mTokenName == token)
                        return result;
                Accept();
            }
        }

        /// <summary>
        /// Connect the tokens so the user sees the same info
        /// for both tokens (and both are grayed out when
        /// hovering with the mouse).  
        /// </summary>
        void Connect(Token s1, Token s2)
        {
            // Find tokens that are already connected
            List <Token> tokens = new List<Token>();
            Token []s1Connectors = s1.Info as Token[];
            if (s1Connectors != null)
                foreach (Token s in s1Connectors)
                    tokens.Add(s);
            Token []s2Connectors = s2.Info as Token[];
            if (s2Connectors != null)
                foreach (Token s in s2Connectors)
                    tokens.Add(s);

            // Add these tokens to the list
            tokens.Add(s1);
            tokens.Add(s2);

            // Set token info
            Token []sa = tokens.ToArray();
            foreach (Token s in sa)
                s.Info = sa;
        }


        /// <summary>
        /// Read the open '(' or '[' parse the expression and connect
        /// the ']' or ')'.  Returns the expression that was parsed.
        /// </summary>
        SyntaxExpr ParseParen()
        {
            // Shouldn't ever happen
            if (mTokenName != "(" && mTokenName != "[")
                throw new Exception("Compiler error: Expecting '[' or '(' while parsing paren");

            // Read open '(' or '[' and create an Expr
            Token openToken = mToken;
            string tokenExpected = openToken == "(" ? ")" : "]";
            string []rejectTokens = openToken == "(" ? REJECT_PAREN : REJECT_BRACKET;
            Accept();

            
            // Empty () or []?
            if (mTokenName == tokenExpected)
            {
                // Return an empty () or []
                Connect(openToken, mToken);
                Reject("Expecting a parameter");
                SyntaxExpr emptyExpr = new SyntaxExpr(Accept()); // Error, use ')' or ']'
                return emptyExpr;
            }

            // Parse all parameters (then add separator)
            SyntaxExpr result = ParseExpr();

            // If not ended properly, append a reject line
            if (mTokenName != tokenExpected)
            {
                // The rest of the line is rejected
                Reject("Expecting '" + tokenExpected + "'", rejectTokens);
            }
            if (mTokenName == tokenExpected)
            {
                Connect(openToken, mToken);
                Accept();
            }
            return result;
        }


        /// <summary>
        /// Parse parameters '(' param1 ',' param2 ... ')' 
        /// NOTE: firstParam is the function name, and will be the first
        /// parameter of the result.  The open and close '()' or '[]' will
        /// be connected.
        /// </summary>
        SyntaxExpr ParseParameters(SyntaxExpr firstParam)
        {
            // Read open '(' or '['
            Token openToken = mToken;
            if (openToken != "(" && openToken != "[")
                throw new Exception("Compiler error: Expecting '(' or '[' while parsing parameters");

            // Create an expression with '(' or '['
            SyntaxExpr result = new SyntaxExpr(Accept(), firstParam);
            string tokenExpected = openToken == "(" ? ")" : "]";
            string []rejectTokens = openToken == "(" ? REJECT_PAREN : REJECT_BRACKET;
            
            // Empty () function call?
            if (mTokenName == tokenExpected)
            {
                // Return an empty () or []
                Connect(openToken, mToken);
                Accept();
                return result;
            }

            // Parse all parameters
            result.AddParam(ParseExpr());
            while (mTokenName == ",")
            {
                Accept();
                result.AddParam(ParseExpr());
            }

            // If not ended properly, reject this expression
            if (mTokenName != tokenExpected)
            {
                // The rest of the line is rejected
                Reject("Expecting '" + tokenExpected + "' or ','", rejectTokens);
            }

            if (mTokenName == tokenExpected)
            {
                Connect(openToken, mToken);
                Accept();
            }
            return result;
        }


        /// <summary>
        /// Parse an atom - number, variable, parenthises
        /// </summary>
        SyntaxExpr ParseExprAtom()
        {
            if (mTokenName == "")
                return Reject("Unexpected end of file", REJECT_LINE);

            // Parse parentheses (order of operation - not function call)
            if (mTokenName == "(")
                return ParseParen();

            // Parse number
            if (char.IsDigit(mTokenName, 0))
                return new SyntaxExpr(Accept());

            // Parse variable name
            if (char.IsLetter(mTokenName, 0))
                return new SyntaxExpr(Accept());

            return Reject("Expecting an identifier, number, "
                                + "parentheses, or expression", REJECT_LINE);
        }

        /// <summary>
        /// Parse an array or function call
        /// </summary>
        SyntaxExpr ParseExprArrayOrFunction()
        {
            SyntaxExpr result = ParseExprAtom();
            if (mTokenName == "(" || mTokenName == "[")
                result = ParseParameters(result);
            return result;
        }



        /// <summary>
        /// Parse unary operator (prefix)
        /// </summary>
        SyntaxExpr ParseUnary()
        {
            if (mTokenName == "!")
                return new SyntaxExpr(Accept(), ParseExprArrayOrFunction());
            return ParseExprArrayOrFunction();
        }

        /// <summary>
        /// Parse (*, /, %)
        /// </summary>
        SyntaxExpr ParseExprMult()
        {
            SyntaxExpr result = ParseUnary();

            // Build multiplication function
            while (mTokenName == "*" 	|| mTokenName == "/" || mTokenName == "%")
                result = new SyntaxExpr(Accept(), result, ParseUnary());
            return result;
        }


        /// <summary>
        /// Parse (#)
        /// </summary>
        SyntaxExpr ParseExprXor()
        {
            SyntaxExpr result = ParseExprMult();

            // Build addition function
            while (mTokenName == "#")
                result = new SyntaxExpr(Accept(), result, ParseExprMult());

            return result;
        }

        /// <summary>
        /// Parse (+, -)
        /// </summary>
        SyntaxExpr ParseExprPlus()
        {
            SyntaxExpr result = ParseExprXor();

            // Build addition function
            while (mTokenName == "+" || mTokenName == "-")
                result = new SyntaxExpr(Accept(), result, ParseExprXor());

            return result;
        }

        /// <summary>
        /// Parse equality
        /// </summary>
        SyntaxExpr ParseExprCompare()
        {
            SyntaxExpr result = ParseExprPlus();
            // Optionally build equality function
            if (mTokenName == "==" || mTokenName == "!=")
                return new SyntaxExpr(Accept(), result, ParseExprPlus());
            return result;
        }

        /// <summary>
        /// Parse ternary expression (a ? (b : c))
        /// </summary>
        SyntaxExpr ParseExprTernary()
        {
            SyntaxExpr result = ParseExprCompare();
            
            // Or a ternary operator
            if (mTokenName == "?")
            {
                Token q = Accept();
                SyntaxExpr t1 = ParseExprTernary();
                if (mTokenName != ":")
                {
                    q.Reject("Matching ':' was not found");
                    return Reject("Expecting a ':' to separate expression for the ternary '?' operator", REJECT_LINE);
                }
                result = new SyntaxExpr(q, result, new SyntaxExpr(Accept(), t1, ParseExprTernary()));
            }
            return result;
        }

        
        /// <summary>
        /// Parse an expression
        /// </summary>
        public SyntaxExpr ParseExpr()
        {
            SyntaxExpr result = ParseExprTernary();

            // Optionally build a range function
            if (mTokenName == ":" || mTokenName == "..")
                return new SyntaxExpr(Accept(), result, ParseExprTernary());
            return result;
        }


        /// <summary>
        /// Parse an 'if' or 'elif' condition
        /// </summary>
        SyntaxExpr ParseIfCond()
        {
            // Parse condition
            if (mTokenName != "(")
                return Reject("Expecting '('", REJECT_LINE);
            Token open = Accept();
            SyntaxExpr result = ParseExpr();
            if (mTokenName != ")")
                return Reject("Expecting ')' - end of of expression", REJECT_LINE);
            Connect(open, mToken);
            Accept(")");
            return result;
        }

        /// <summary>
        /// Parse an if statement (expects 'if' at the input)
        /// </summary>
        SyntaxExpr ParseIfStatement(SyntaxBox box)
        {
            // Parse "if" token, condition, and statements
            Token ifToken = mToken;
            Accept("if");
            SyntaxExpr ifExpr = new SyntaxExpr(ifToken);
            ifExpr.AddParam(ParseIfCond());
            ifExpr.AddParam(ParseStatements(box, false));
            
            // Parse "elif" statements
            SyntaxExpr elseExpr = ifExpr;
            while (mTokenName == "elif")
            {
                // Parse "elif" token, condition, and statements
                // NOTE: "elif" token is converted to "if"
                Connect(ifToken, mToken);
                Accept("elif");
                SyntaxExpr newIf = new SyntaxExpr(ifToken);
                newIf.AddParam(ParseIfCond());
                newIf.AddParam(ParseStatements(box, false));
                
                // Convert the new "elif" to "else if"
                elseExpr.AddParam(newIf);
                elseExpr = newIf;
            }

            // Parse the "else" clause (if it exists)
            if (mTokenName == "else")
            {
                Connect(ifToken, mToken);
                Accept("else");
                elseExpr.AddParam(ParseStatements(box, false));
            }

            // Parse end of box statement
            if (mTokenName == "end")
            {
                mToken.InfoString = "end if";
                Connect(ifToken, mToken);
                Accept();
            }
            else
                Reject("Expecting 'end' - end of if statement body", REJECT_BOX);

            // Parse "else" part
            return ifExpr;
        }

        /// <summary>
        /// Make a bit declaration.  First parameter is declaration (with optional
        /// array parameter).  Second parameter is optional assignment.
        /// </summary>
        public SyntaxExpr ParseBitStatement(string[] rejects)
        {
            // Create "bit" statement
            SyntaxExpr expr = new SyntaxExpr(Accept("bit"));
            if (mToken.Type != eTokenType.Identifier)
            {
                Reject("Expecting a variable name", rejects);
                return expr;
            }
            // Create declaration (array size is optional parameter)
            Token variableName = Accept();
            SyntaxExpr declaration = new SyntaxExpr(variableName);
            if (mTokenName == "[")
                declaration.AddParam(ParseParen());

            // First parameter is declaration
            expr.AddParam(declaration);

            // Second parameter (optional) is assignment statement
            if (mTokenName == "=")
                expr.AddParam(new SyntaxExpr(Accept(), new SyntaxExpr(variableName), ParseExpr()));

            return expr;
        }

        /// <summary>
        /// Parse a constant declaration (only "int" type is supported)
        /// </summary>
        SyntaxExpr ParseConstStatement()
        {
            // Create "const" statement
            SyntaxExpr expr = new SyntaxExpr(Accept("const"));
            if (mTokenName != "int")
            {
                Reject("Expecting keyword 'int'", REJECT_LINE);
                return expr;
            }
            // First parameter is the type, "int"
            expr.AddParam(new SyntaxExpr(Accept()));

            // Variable name must be identifier
            if (mToken.Type != eTokenType.Identifier)
            {
                Reject("Expecting a variable name", REJECT_LINE);
                return expr;
            }
            Token variableName = Accept();

            // Const statement requires an assignment
            if (mTokenName != "=")
            {
                Reject("Expecting '=' - Assignment is required");
                return expr;
            }
            // Second parameter is the assignment expression
            expr.AddParam(new SyntaxExpr(Accept(), new SyntaxExpr(variableName), ParseExpr()));

            return expr;
        }
    

        /// <summary>
        /// Parse a single statement, which could be an expression.
        /// Adds declaratins to the box locals.
        /// NOTE: This can return NULL if there are no statements.
        /// </summary>
        public SyntaxExpr ParseStatement(SyntaxBox box, bool topLevel)
        {
            while (mTokenName == "else" || mTokenName == "elif")
            {
                Reject("This '" + mTokenName + "' is not inside an 'if' statement");
                Accept();
            }
    
            // Check statement keywords first
            bool needsSemicolon = true;
            SyntaxExpr result = null;
            if (mTokenName == "bit")
            {
                // Parse bit statement (optionally followed by assignment)
                if (!topLevel)
                    Reject("'bit' declarations are only allowed at the top level");
                result = ParseBitStatement(REJECT_LINE);
            }
            else if (mTokenName == "const")
            {
                // Parse const declaration
                if (!topLevel)
                    Reject("'const' declarations are only allowed at the top level");
                result = ParseConstStatement();
            }
            else if (mTokenName == "if")
            {
                // Parse if statement
                needsSemicolon = false;
                result = ParseIfStatement(box);
            }
            else
            {
                // Parse an expression
                result = ParseExpr();
                
                // Assignment statement?
                if (mTokenName == "=")
                {
                    // Generate an assignment statement
                    result = new SyntaxExpr(Accept(), result, ParseExpr());
                }
            }

            // Ensure we have a valid statement
            if (needsSemicolon && mTokenName != ";")
                Reject("Expecting end of statement separator ';'", REJECT_LINE);
            if (mTokenName == ";")
                Accept();

            return result;
        }

        /// <summary>
        /// Parse multiple statements until 'box', 'end', 'else', or 'elif'
        /// is found.  Returns the statements that are parsed.
        /// Adds declarations to the box locals.  Never returns NULL.
        /// </summary>
        public SyntaxExpr ParseStatements(SyntaxBox box, bool topLevel)
        {
            // Create an "{" expression, which (for now means "statements")
            SyntaxExpr statements = new SyntaxExpr(mInternalTokenStatement);

            // While not end of file and not end of box and not new box
            while (mTokenName != "" 
                    && mTokenName != "box"
                    && mTokenName != "end"
                    && mTokenName != "else"
                    && mTokenName != "elif")
            {
                // Skip blank statements
                while (mTokenName == ";")
                    Accept();

                // Parse the statement
                SyntaxExpr statement = ParseStatement(box, topLevel);
                if (statement != null)
                    statements.AddParam(statement);

                while (mTokenName == ";")
                    Accept();
            }
            return statements;
        }


        /// <summary>
        /// Parse the name of a declaration (but not the assignment expression)
        /// </summary>
        public SyntaxDecl ParseBoxDeclarationName(string []rejects)
        {
            // Accept "box", "in", "out", "bus", "bit", "int", etc.
            SyntaxDecl decl = new SyntaxDecl();
            decl.TypeName = mToken;
            Accept();

            if (mToken.Type != eTokenType.Identifier)
            {
                Reject("Expecting an identifier (the name of a box, parameter, or variable)", rejects);
                return decl;
            }

            // Accept the identifier
            decl.VariableName = mToken;
            Accept();

            if (mTokenName == "[")
                decl.ArraySizeExpr = ParseParen();

            return decl;
        }

        /// <summary>
        /// Parse a box declaration parameter
        /// </summary>
        public SyntaxDecl ParseBoxDeclarationParameter()
        {
            if (mTokenName != "in" && mTokenName != "out" && mTokenName != "bus")
            {
                Reject("Expecting in, out, or bus keyword", REJECT_PARAMETER);
                SyntaxDecl decl2 = new SyntaxDecl();
                return decl2;
            }
            // Parse the declaration
            SyntaxDecl decl = ParseBoxDeclarationName(REJECT_PARAMETER);

            // Additional info for error
            if (mTokenName == "=")
                Reject("Declaration parameters can not be assigned", REJECT_PARAMETER);
            return decl;
        }

        /// <summary>
        /// Parse a box declatation parameters
        /// </summary>
        public List<SyntaxDecl> ParseBoxDeclarationParameters()
        {
            List<SyntaxDecl> parameters = new List<SyntaxDecl>();
            Token open = mToken;
            Accept("(");
            if (mTokenName == ")")
            {
                Connect(open, mToken);
                Accept();
                return parameters;
            }

            // Parse first parameter
            parameters.Add(ParseBoxDeclarationParameter());

            if (mTokenName != "," && mTokenName != ")")
                Reject("Expecting ')' or ',' - end of parameter list or another parameter",
                            REJECT_PARAMETER);

            // Parse additional parameters
            while (mTokenName == ",")
            {
                Accept();
                parameters.Add(ParseBoxDeclarationParameter());

                if (mTokenName != "," && mTokenName != ")")
                    Reject("Expecting ')' or ',' - end of parameter list or another parameter",
                                REJECT_PARAMETER);
            }

            if (mTokenName == ")")
            {
                Connect(open, mToken);
                Accept();
            }
            else
            {
                Reject("Expecting ')' - end of parameter list", REJECT_LINE);
            }

            return parameters;
        }

        /// <summary>
        /// Parse a box prototype, returns new box with parameters filled in.
        /// </summary>
        public SyntaxBox ParseBoxPrototype()
        {
            if (mTokenName != "box")
                throw new Exception("ParseBoxPrototype: expecting 'box'");

            // Read box declaration name
            SyntaxBox box = new SyntaxBox();
            box.NameDecl = ParseBoxDeclarationName(REJECT_BOX_PROTOTYPE);

            if (mTokenName == "(")
                box.Params = ParseBoxDeclarationParameters();
            else
                Reject("Expecting \'(\' or \'[\' in box declaration", REJECT_BOX_PROTOTYPE);

            return box;
        }


        /// <summary>
        /// Parse a box
        /// </summary>
        public SyntaxBox ParseBox()
        {
            SyntaxBox box = ParseBoxPrototype();

            // Skip box if we don't find 'is' after the prototype
            if (mTokenName != "is")
            {
                Reject("Expecting 'is' after box prototype", REJECT_BOX);
                return box;
            }
            Accept("is");

            box.Statements = ParseStatements(box, true);

            // Parse end of box statement
            if (mTokenName == "end")
            {
                mToken.InfoString = box.NameDecl.ToString();
                Connect(box.NameDecl.TypeName, mToken);
                Accept();
            }
            else
                Reject("Expecting 'end' - end of box body", REJECT_BOX);

            return box;
        }

        /// <summary>
        /// Parse an entire file.
        /// </summary>
        public SyntaxBox Parse()
        {
            // Top level scope
            SyntaxBox program = new SyntaxBox();
            program.NameDecl.TypeName = new Token("SCOPE:0", 0, 0);
            program.NameDecl.VariableName = new Token("SCOPE:0", 0, 0);

            while (mTokenName != "")
            {
                // Parse a box
                if (mTokenName == "box")
                {
                    mParseError = false;
                    SyntaxBox box = ParseBox();
                    box.Parent = program;
                    program.Boxes.Add(box);
                    box.Error = mParseError;
                }
                else if (mTokenName == "const")
                {
                    program.Constants.Add(ParseConstStatement());

                    // Ensure we have a valid statement
                    if (mTokenName != ";")
                        Reject("Expecting end of statement separator ';'", REJECT_LINE);
                    while (mTokenName == ";")
                        Accept();
                }
                else
                {
                    Reject("Invalid token \'" + mTokenName 
                                + "\' was found when expecting the keyword 'box' or 'const'",
                                REJECT_BOX);
                }
            }
            return program;
        }
    }

}
