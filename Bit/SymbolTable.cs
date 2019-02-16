// Copyright 2011-2019 by Jeremy Spiller
// See Lincense.txt for details

using System;
using System.Collections.Generic;

namespace Gosub.Bit
{
    /// <summary>
    /// Symbol table
    /// </summary>
    class SymbolTable
    {
        public SymbolTable	Parent;
        public Dictionary<string, Symbol> Symbols = new Dictionary<string, Symbol>();

        public SymbolTable(SymbolTable enclosingScope)
        {
            Parent = enclosingScope;
        }

        /// <summary>
        /// Find a declaration, given the token (returns NULL if it was not found).
        /// Walk up the scope levels to find the symbol if not found at current scope.
        /// </summary>
        public Symbol FindSymbol(Token token)
        {
            // Search all scopes for this symbol (moving up levels until it is found)
            SymbolTable scope = this;
            do
            {
                Symbol findSymbol;
                if (scope.Symbols.TryGetValue(token, out findSymbol))
                    return findSymbol; // Symbol found

                // Move up to next level
                scope = scope.Parent;
            } while (scope != null);

            // Error - symbol not found
            return null;
        }

        /// <summary>
        /// Add a symbol to the symbol table.
        /// Returns TRUE if the symbol was added (or FALSE on duplicate).
        /// If symbol was not added, the duplicate is returned.
        /// </summary>
        public bool AddSymbol(Symbol symbol, out Symbol duplicate)
        {
            // Verify we don't already have this symbol
            if (Symbols.TryGetValue(symbol.Decl.VariableName, out duplicate))
                return false; // Duplicate symbol

            // Add new symbol to table
            Symbols[symbol.Decl.VariableName] = symbol;
            return true;
        }

    }
}
