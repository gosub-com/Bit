// Copyright 2011-2019 by Jeremy Spiller
// See Lincense.txt for details

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace Gosub.Bit
{
    partial class FormSetupSimulation : Form
    {
        public List<CodeBox>	Boxes = new List<CodeBox>();
        public Form				FormResult;

        /// <summary>
        /// Quickie struct to show boxes in the list
        /// </summary>
        struct sListBoxes
        {
            public string Name;
            public CodeBox Box;
            public override string ToString()
            {
                return Name == null ? "(null)" : Name;
            }
            public sListBoxes(string name, CodeBox box)
            {
                Name = name;
                Box = box;
            }
        }


        public FormSetupSimulation()
        {
            InitializeComponent();
        }

        private void FormSetupSimulation_Load(object sender, EventArgs e)
        {

            listBoxes.UseTabStops = true;
            listBoxes.UseCustomTabOffsets = true;
            listBoxes.ColumnWidth = 1;
            listBoxes.CustomTabOffsets.Clear();
            // WTF - We need to scale by 67%????
            listBoxes.CustomTabOffsets.Add((labelGates.Left - labelName.Left)*67/100);

            // Add all the boxes to the list box
            foreach (CodeBox box in Boxes)
            {
                listBoxes.Items.Add(new sListBoxes(
                    "" +  box.ParseBox.NameDecl.VariableName + "\t"
                    + box.GatesInUnlinkedCode()
                    + (box.Error ? " (has errors)" : ""), box));
            }
        }

        /// <summary>
        /// Returns the currently selected box (or NULL if nothing is selected)
        /// </summary>
        CodeBox CurrentBox
        {
            get
            {
                if (listBoxes.SelectedIndex < 0)
                    return null;
                return ((sListBoxes)listBoxes.Items[listBoxes.SelectedIndex]).Box;
            }
        }


        private void listBoxes_SelectedIndexChanged(object sender, EventArgs e)
        {
            CodeBox box = CurrentBox;
            if (box == null)
            {
                groupCompile.Enabled = false;
                return;
            }
            groupCompile.Enabled = !box.Error;
        }

        /// <summary>
        /// Split a string in to lines, then add to the text.
        /// </summary>
        void AddSplitString(List<string> text, StringBuilder sb)
        {
            // Split the string
            int start = 0;
            int end = Math.Min(78, sb.Length);
            do
            {
                // Find beginning of word
                int removed = 0;
                int oldEnd = end;
                while (end < sb.Length && end > start+1 && sb[end-1] != ' ')
                {
                    end--;
                    removed++;
                }
                // Ensure word is not too long
                if (removed > 40)
                    end = oldEnd;
                text.Add(sb.ToString(start, end-start));
                start = end;
                end = Math.Min(start + 78, sb.Length);

                // Append final line
                if (end >= sb.Length && end > start+1)
                    text.Add(sb.ToString(start, end-start));

            } while (end < sb.Length);
        }


        void LinkAndOptimize(CodeBox box)
        {
            box.LinkedCode = null;  // Force top-level re-link
            box.LinkCode();

            int minOpt = box.Symbol.CodeExprIndex + box.Symbol.ArraySize;
            if (box.Params.Count != 0)
                minOpt = Math.Max(minOpt, box.Params[box.Params.Count-1].CodeExprIndex
                                            + box.Params[box.Params.Count-1].ArraySize);

            if (box.Locals.Count != 0)
                minOpt = Math.Max(minOpt, box.Locals[box.Locals.Count-1].CodeExprIndex
                                            + box.Locals[box.Locals.Count-1].ArraySize);
            
            // Optimize, unless disabled
            if (!checkDisableOptimizer.Checked)
                new Optimizer(box.LinkedCode, minOpt);
        }


        private void buttonViewCompiledCode_Click(object sender, EventArgs e)
        {
            CodeBox box = CurrentBox;
            if (box == null)
                return;

            LinkAndOptimize(box);

            // Setup box comments
            List<string> code = new List<string>();
            code.Add("// Compiled code, " + box.GatesInLinkedCode() + " gates");
            if (box.Error)
                code.Add("// NOTE: This box has errors.  The code is incorrect.");

            // Show box info
            code.Add(box.Symbol.ResolvedName);
            code.Add("{");

            // Show all the code
            foreach (OpCodeExpr expr in box.LinkedCode)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("    ");
                expr.PrintExpression(sb);
                sb.Append(";");
                AddSplitString(code, sb);				
            }

            code.Add("}");


            // Setup to display the "view code" form
            // after closing this form
            FormViewText form = new FormViewText();
            form.Text = "" + box + " (compiled)";
            form.TextLines = code.ToArray();
            FormResult = form;
            Close();
        }


        private void buttonSimulate_Click(object sender, EventArgs e)
        {
            CodeBox box = CurrentBox;
            if (box == null)
                return;
    
            LinkAndOptimize(box);

            // Setup to display the simulation form
            // after closing this form
            FormSimulate form = new FormSimulate();
            form.Box = box;
            FormResult = form;
            Close();
        }

        private void FormSetupSimulation_KeyPress(object sender, KeyPressEventArgs e)
        {
            // User hit ESC
            if (e.KeyChar == 27)
                Close();
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void checkDisableOptimizer_CheckedChanged(object sender, EventArgs e)
        {

        }
    }
}
