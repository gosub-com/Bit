// Copyright 2011-2019 by Jeremy Spiller
// See Lincense.txt for details

using System;
using System.Collections.Generic;

namespace Gosub.Bit
{
    /// <summary>
    /// Minimalist string intern system. This uses a small amount
    /// of memory to combine strings, and gets 98% of the repeats.
    /// </summary>
    class MinTern
    {
        const int WIDTH = 47;	// Number of different hash values
        const int DEPTH = 5;	// Number of symbols per has value
        string [][]mIntern;

        /// <summary>
        /// Returns the same string, except that it is intern'ed
        /// in this data structure.
        /// </summary>
        public string this[string newStr]
        {
            get
            {
                if (newStr == null)
                    return null;
                if (newStr.Length == 0)
                    return "";

                // Search table for newStr
                string []table = mIntern[(newStr.GetHashCode() & 0xFFFFFFF) % WIDTH];
                for (int i = 0; i < table.Length; i++)
                {
                    string tableString = table[i];
                    if (newStr == tableString)
                    {
                        // Shift strings, and re-insert at beginning
                        for (int ir = i-1; ir >= 0;  ir--)
                            table[ir+1] = table[ir];
                        table[0] = tableString;
                        return tableString;
                    }
                }

                // Insert new string at beginning of table
                string result = newStr;
                for (int i = 0;  i < table.Length && newStr != null;  i++)
                {
                    string temp = table[i];
                    table[i] = newStr;
                    newStr = temp;
                }
                return result;
            }
        }

        /// <summary>
        /// Clear the string in the table
        /// </summary>
        public void Clear()
        {
            foreach (string []list in mIntern)
                for (int i = 0;  i < list.Length;  i++)
                    list[i] = null;
        }

        /// <summary>
        /// Create a MinTern to combine strings.
        /// </summary>
        public MinTern()
        {
            mIntern = new string[WIDTH][];
            for (int i = 0; i < mIntern.Length; i++)
                mIntern[i] = new string[DEPTH];
        }

    }
}
