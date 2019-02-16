// Copyright 2011-2019 by Jeremy Spiller
// See Lincense.txt for details

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Reflection;

namespace Gosub.Bit
{
    public partial class FormMain:Form
    {
        FormHoverMessage mHoverMessageForm;
        Token			mHoverToken;
        DateTime		mLastMouseMoveTime;
        DateTime		mLastEditorChangedTime;
        List<FormSimulate> mSimulationForms = new List<FormSimulate>();
        string			mThisNameSpace = "Gosub.Bit.";

        public FormMain()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Initialize this form
        /// </summary>
        private void FormMain_Load(object sender, EventArgs e)
        {
            // Read the CPU from the embedded resource
            List<string> lines = new List<string>();
            Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream(mThisNameSpace + "CPU.txt");
            StreamReader sr = new StreamReader(s);
            while (!sr.EndOfStream)
                lines.Add(sr.ReadLine());
            sr.Close();
            string []file = lines.ToArray();
            
            editor1.Lexer.ScanLines(file);
            editor1_TextChanged2(null, null);
            Text += " - " + "V" + App.Version;

            mHoverMessageForm = new FormHoverMessage();
        }

        private void editor1_Load(object sender, EventArgs e)
        {
            // Setup keywords
            string reservedWords = "box if else elif end is";
            string []reserved2 = reservedWords.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string s in reserved2)
                editor1.Lexer.AddKeyword(s, eTokenType.Reserved, false);

            string reservedNames = "in out const int range bit bus dup set "
                + "unused true false";

            string []reserved = reservedNames.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string s in reserved)
                editor1.Lexer.AddKeyword(s, eTokenType.ReservedName, false);
        }


        /// <summary>
        /// Setup to re-compile after the user stops typing for a short while
        /// </summary>
        private void editor1_TextChanged2(object sender, EventArgs e)
        {
            mLastEditorChangedTime = DateTime.Now;
        }

        private void editor1_BlockedByReadOnly(object sender, EventArgs e)
        {
            MessageBox.Show(this, "You can not modify text while the similation window is open.", App.Name);
        }

        /// <summary>
        /// Setup to display the message for the hover token.
        /// Immediately show connected tokens.
        /// </summary>
        private void editor1_MouseHoverTokenChanged(Token previousToken, Token newToken)
        {
            // Setup to display the hover token
            mHoverToken = newToken;
            mHoverMessageForm.Visible = false;

            // Update hover token colors
            editor1.TokenColorOverrides = null;
            if (newToken != null && newToken.Type != eTokenType.Comment)
            {
                // Make a list of connecting tokens
                List<TokenColorOverride> overrides = new List<TokenColorOverride>();
                overrides.Add(new TokenColorOverride(newToken, Brushes.LightGray));
                Token []connectors = newToken.Info as Token[];
                if (connectors != null)
                    foreach (Token s in connectors)
                        overrides.Add(new TokenColorOverride(s, Brushes.LightGray));

                // Update editor to show them
                editor1.TokenColorOverrides = overrides.ToArray();
            }
        }


        /// <summary>
        /// When the user click the editor, hide the message box until a
        /// new token is hovered over.
        /// </summary>
        private void editor1_MouseDown(object sender, MouseEventArgs e)
        {
            mHoverToken = null;
            mHoverMessageForm.Visible = false;
        }

        /// <summary>
        /// Keep track of mouse movement time for hover message display
        /// </summary>
        private void editor1_MouseMove(object sender, MouseEventArgs e)
        {
            mLastMouseMoveTime = DateTime.Now;
        }

        /// <summary>
        /// Recompile or display hover message when necessary
        /// </summary>
        private void timer1_Tick(object sender, EventArgs e)
        {
            // Recompile 250 milliseconds after the user stops typing
            if (mLastEditorChangedTime != new DateTime()
                && (DateTime.Now - mLastEditorChangedTime).TotalMilliseconds > 250)
            {
                try
                {
                    // Reset the lexer, re-parse, and compile
                    mLastEditorChangedTime = new DateTime();
                    Parser parser = new Parser(editor1.Lexer);
                    DateTime t1 = DateTime.Now;
                    SyntaxBox program = parser.Parse();
                    DateTime t2 = DateTime.Now;
                    CodeBox code = new CodeBox(null, program);
                    DateTime t3 = DateTime.Now;
                    editor1.Invalidate();

                    // Debug times
                    TimeSpan parseTime = t2 - t1;
                    TimeSpan genTime = t3 - t2;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Error compiling: " + ex.Message, App.Name);
                }
            }

            // Find out if all simulation form windows have closed
            for (int i = 0;  i < mSimulationForms.Count;  i++)
                if (mSimulationForms[i].IsDisposed)
                    mSimulationForms.RemoveAt(i);

            // Make the editor read-only if there is a simulation form open
            editor1.ReadOnly = mSimulationForms.Count != 0;

            // Display the hover message (after no mouse movement for 150 milliseconds)
            if ( mHoverToken != null
                    && mHoverToken.Type != eTokenType.Comment
                    && mHoverToken.InfoString != ""
                    && (DateTime.Now - mLastMouseMoveTime).TotalMilliseconds > 150
                    && !mHoverMessageForm.Visible)
            {
                // Set form size, location, and text
                mHoverMessageForm.Message.Text = mHoverToken.InfoString;
                Size s = mHoverMessageForm.Message.Size;
                mHoverMessageForm.ClientSize = new Size(s.Width + 8, s.Height + 8);
                Point p = editor1.PointToScreen(editor1.LocationToken(mHoverToken.Location));
                p.Y -= s.Height + 32;
                mHoverMessageForm.Location = p;

                // Display the form
                mHoverMessageForm.Show(this);
                this.Focus();
            }
        }


        /// <summary>
        /// Show simulation form
        /// </summary>
        private void menuSimulateNew_Click(object sender, EventArgs e)
        {
            // Compile everything
            Parser parser = new Parser(editor1.Lexer);
            SyntaxBox program = parser.Parse();
            CodeBox code = new CodeBox(null, program);
            editor1.Invalidate();

            // Get the code boxes
            List<CodeBox> boxes = new List<CodeBox>();
            foreach (var box in code.Boxes)
                if (box.Decl.TypeName == "box" && box.CodeBoxValue != null)
                    boxes.Add(box.CodeBoxValue);

            if (boxes.Count == 0)
            {
                MessageBox.Show(this, "No boxes were defined", App.Name);
                return;
            }

            // Show the simulation form
            FormSetupSimulation setupForm = new FormSetupSimulation();
            setupForm.Boxes = boxes;
            setupForm.ShowDialog(this);

            // Show a form if requested by setup form
            if (setupForm.FormResult != null)
                setupForm.FormResult.Show();

            // Go to read-only mode if running a simulation
            if (setupForm.FormResult as FormSimulate != null)
            {
                mSimulationForms.Add(setupForm.FormResult as FormSimulate);
                editor1.ReadOnly = true;
            }
        }

        private void menuFileOpen_Click(object sender, EventArgs e)
        {
            MessageBox.Show(this, "This function is not implemented", App.Name);
        }

        private void menuFileSave_Click(object sender, EventArgs e)
        {
            MessageBox.Show(this, "This function is not implemented", App.Name);
        }

        private void FormMain_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F5)
                menuSimulateNew_Click(null, null);

            // Display search form
            if (e.Control && e.KeyCode == Keys.F)
                FormSearch.Show(this, editor1 );
            if (e.KeyCode == Keys.F3)
                FormSearch.FindNext(this, editor1);

        }

        private void menuHelpAbout_Click(object sender, EventArgs e)
        {
            MessageBox.Show(this, "Bit version " + App.Version, App.Name);
        }

        private void menuHelpLicense_Click(object sender, EventArgs e)
        {
            // Read license from resource
            List<string> lines = new List<string>();
            Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream(mThisNameSpace + "License.txt");
            StreamReader sr = new StreamReader(s);
            while (!sr.EndOfStream)
                lines.Add(sr.ReadLine());
            sr.Close();

            // Display the license
            FormRtf form = new FormRtf();
            form.ShowText(lines.ToArray());
        }

        private void menuEditFind_Click(object sender, EventArgs e)
        {
            FormSearch.Show(this, editor1);
        }

        private void menuEditFindNext_Click(object sender, EventArgs e)
        {
            FormSearch.FindNext(this, editor1);
        }

        private void viewRTFToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FormRtf form = new FormRtf();
            form.ShowLexer(editor1.Lexer);
        }





    }
}
