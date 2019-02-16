// Copyright 2011-2019 by Jeremy Spiller
// See Lincense.txt for details

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Gosub.Bit
{
    public partial class FormRtf:Form
    {
        int mTabSize = 4;
        Lexer mLexer;
        string []mText;

        public FormRtf()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Convert character index to column index.  If charIndex is
        /// too large, the end of the line is returned.
        /// NOTE: The column index accounts for extra spaces
        /// inserted because of TABS.
        /// </summary>
        public int IndexToCol(string line, int charIndex)
        {
            int col = 0;
            for (int i = 0; i < charIndex && i < line.Length; i++)
            {
                if (line[i] == '\t')
                    col += mTabSize - col%mTabSize;
                else
                    col++;
            }
            return col;
        }

        private void FormRtf_Load(object sender, EventArgs e)
        {
            Show();
            Refresh();

            if (mText != null)
            {
                // Show the text in mText
                foreach (string s in mText)
                {
                    rtf.AppendText(s);
                    rtf.AppendText("\r\n");
                }
            }

            if (mLexer != null)
            {
                // Show the text in mLexer
                int line = 0;
                int column = 0;
                foreach (Token token in mLexer)
                {
                    // Append new line when moving to next line
                    if (token.Line != line)
                    {
                        rtf.AppendText("\r\n");
                        line = token.Line;
                        column = 0;
                    }
                    // Prepend white space
                    int tokenColumn = IndexToCol(mLexer.GetLine(token.Line), token.Char);
                    while (column < tokenColumn)
                    {
                        rtf.AppendText(" ");
                        column++;
                    }

                    switch (token.Type)
                    {
                        case eTokenType.Comment:
                            rtf.SelectionColor = Color.Green;
                            break;

                        case eTokenType.Reserved:
                        case eTokenType.ReservedName:
                            rtf.SelectionColor = Color.Blue;
                            break;

                        default:
                            rtf.SelectionColor = Color.Black;
                            break;
                    }
                    // Append token
                    rtf.AppendText(token.Name);
                    column += token.Name.Length;
                }

            }

            labelLoading.Visible = false;
            rtf.SelectionStart = 0;
        }

        /// <summary>
        /// Show the lexer (with syntax coloring)
        /// </summary>
        public void ShowLexer(Lexer lexer)
        {
            mLexer = lexer;
            Show();
        }

        /// <summary>
        /// Show a text file
        /// </summary>
        public void ShowText(string[] text)
        {
            mText = text;
            Show();
        }
    }
}
