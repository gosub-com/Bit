// Copyright 2011-2019 by Jeremy Spiller
// See Lincense.txt for details

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Gosub.Bit
{
    partial class EditField : UserControl
    {
        public List<OpCodeExpr> mExpressions;
        
        long	mPreviousValue = long.MinValue+0x1A;
        Base	mBase = Base.Hex;
        Base	mPreviousBase = Base.None;
        Color	mEditor1BackColor;
        bool	mComputerUpdate;
        bool	mResetCursor;
        EventArgs mEventArgs = new EventArgs();

        /// <summary>
        /// This event happens after the value was changed by the user.
        /// </summary>
        public event EventHandler				ValueChangedByUser;


        enum Base
        {
            Bin,
            Dec,
            Hex,
            None
        }

        public EditField()
        {
            InitializeComponent();
            comboBase.SelectedIndex = (int)Base.Hex;
        }

        private void EditField_Load(object sender, EventArgs e)
        {
        }


        public Editor Editor
        {
            get { return editor1; }
        }

        /// <summary>
        /// This must be periodically called from a timer to update the field.
        /// </summary>
        public void UpdateReadValue()
        {
            // Don't update user editable fields
            if (editor1.ReadOnly)
            {
                // Output value
                UpdateTextValue(false);
                editor1.OverwriteMode = false;
            }
            else
            {
                // If user selected text, put cursor at end
                if (mResetCursor)
                {
                    editor1.CursorLoc = new TokenLoc(editor1.CursorLoc.Line, editor1.Text.Length);
                    mResetCursor = false;
                }
                // Ensure user isn't on a space (move right if so)
                while (editor1.CursorLoc.Char < editor1.Text.Length
                        && editor1.Text[editor1.CursorLoc.Char] == ' ')
                    editor1.CursorLoc = new TokenLoc(editor1.CursorLoc.Line, editor1.CursorLoc.Char+1);
                
                // Overwrite mode, except on final char
                editor1.OverwriteMode = editor1.CursorLoc.Char < editor1.Text.Length;
            }
        }


        /// <summary>
        /// Update an edit field to ensure its value has been set correctly
        /// </summary>
        public void UpdateTextValue(bool alwaysSet)
        {
            if (Expressions == null)
                return;

            // Figure value
            long value = 0;
            for (int i = mExpressions.Count - 1;  i >= 0;  i--)
                value = ((long)mExpressions[i].Expr.State & 1) | (value << 1);

            // If nothing changed, exit
            if (!alwaysSet && value == mPreviousValue && mBase == mPreviousBase)
                return;

            mPreviousValue = value;
            mPreviousBase = mBase;

            const int FIELD_WIDTH = 9;
            char digitFill;
            int digits;

            string valueStr;
            switch (mBase)
            {
                default:
                case Base.Hex: 
                    valueStr = value.ToString("X");
                    digitFill = '0';
                    digits = Math.Max((mExpressions.Count+3)/4, 1);
                    break;
                case Base.Dec:
                    valueStr = value.ToString();
                    digitFill = ' ';
                    digits = valueStr.Length;
                    break;
                case Base.Bin:
                    valueStr = Convert.ToString(value, 2);
                    digitFill = '0';
                    digits = mExpressions.Count;
                    break;
            }
            valueStr = new string(digitFill, Math.Max(0, digits-valueStr.Length)) + valueStr;
            valueStr = new string(' ', Math.Max(0, FIELD_WIDTH-valueStr.Length)) + valueStr;
            
            // Set new text value
            editor1.Text = valueStr;
        }

        public bool IsOutput()
        {
            bool isIn = true;
            foreach (OpCodeExpr expr in mExpressions)
                if (expr.Expr as OpCodeInParam == null)
                    isIn = false;
            return !isIn;
        }

        public List<OpCodeExpr> Expressions
        {
            get { return mExpressions; }
            set
            {
                mExpressions = value;

                // Setup to allow user input
                editor1.ReadOnly = IsOutput();
                mEditor1BackColor = IsOutput() ? BackColor : Color.White;
                editor1.BackColor = mEditor1BackColor;
                UpdateTextValue(true);
            }
        }

        public override string Text
        {
            get
            {
                return base.Text;
            }
            set
            {
                labelFieldName.Text = value;
                base.Text = value;
            }
        }



        private void comboBase_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBase.SelectedIndex < 0)
                return;
            mBase = (Base)comboBase.SelectedIndex;
            UpdateTextValue(true);
        }

        bool IllegalChar(char ch)
        {
            if (mBase == Base.Bin && (ch != '0' && ch != '1'))
                return true;
            if (mBase == Base.Dec && !(ch >= '0' && ch <= '9'))
                return true;
            if (mBase == Base.Hex && !(ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F'))
                return true;
            return false;
        }

        /// <summary>
        /// Override the editor key handling behavior.
        /// </summary>
        private void editor1_KeyDown(object sender, KeyEventArgs e)
        {
            // Don't allow tabs
            if ( e.KeyCode == Keys.Tab)
                e.Handled = true;

            if (e.KeyCode == Keys.Insert && !e.Control && !e.Shift)
                e.Handled = true;
        }

        /// <summary>
        /// Override the editor key handling behavior
        /// </summary>
        private void editor1_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Don't allow CR or tabs
            if (e.KeyChar == '\r' || e.KeyChar == '\t')
            {
                e.Handled = true;
                return;
            }

            // ESCAPE
            if (e.KeyChar == 27)
            {
                foreach (OpCodeExpr expr in mExpressions)
                    expr.Expr.State = OpState.Zero;
                UpdateTextValue(true);
                e.Handled = true;
                mResetCursor = true;
            }
            if (e.KeyChar == '!')
            {
                foreach (OpCodeExpr expr in mExpressions)
                    expr.Expr.State = OpState.One;
                UpdateTextValue(true);
                e.Handled = true;
                mResetCursor = true;
            }

            // Trap illegal keys (must be BIN/HEX/DEC digits)
            if (e.KeyChar < ' ')
                return;
            e.KeyChar = char.ToUpper(e.KeyChar);
            if (IllegalChar(e.KeyChar))
            {
                // When it's a valid HEX char and not excepted,
                // give a warning beep.  All other chars are
                // considered to be handled by the form.
                if (e.KeyChar >= '0' && e.KeyChar <= '9'
                    || e.KeyChar >= 'A' && e.KeyChar <= 'F'
                    || e.KeyChar >= 'a' && e.KeyChar <= 'f')
                    System.Media.SystemSounds.Beep.Play();

                e.Handled = true;
                return;
            }
        }


        /// <summary>
        /// When user changes the field, generate a new value
        /// </summary>
        private void editor1_TextChanged2(object sender, EventArgs e)
        {
            // If change is not from user, skip
            if (editor1.ReadOnly || mComputerUpdate)
                return;

            mComputerUpdate = true;
            int baseValue = 16;
            if (mBase == Base.Dec)
                baseValue = 10;
            if (mBase == Base.Bin)
                baseValue = 2;
            
            // Setup to reset cursor if anything is selected
             mResetCursor = editor1.HasSel();

            // Get user entered value
            long value = 0;
            try
            {
                string textValue = editor1.Text.Trim();
                if (textValue.Length == 0)
                    value = 0;
                else
                    value = Convert.ToInt64(textValue, baseValue);
                editor1.BackColor = mEditor1BackColor;
            }
            catch
            {
                editor1.BackColor = Color.Red;
            }

            // Write values
            foreach (OpCodeExpr expr in mExpressions)
            {
                expr.Expr.State = (OpState)(value & 1);
                value >>= 1;
            }

            // Show user updated value
            UpdateTextValue(true);

            // Trigger changed delegate
            if (ValueChangedByUser != null)
                ValueChangedByUser(this, mEventArgs);


            mComputerUpdate = false;

        }




    }
}
