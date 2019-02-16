// Copyright 2011-2019 by Jeremy Spiller
// See Lincense.txt for details

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;

namespace Gosub.Bit
{
    partial class FormSimulate : Form
    {
        public CodeBox Box;
        
        List<EditField> mFields = new List<EditField>();
        List<EditField> mFieldTabs = new List<EditField>();
        List<EditField> mFieldInputs = new List<EditField>();
        List<EditField> mFieldOutputs = new List<EditField>();
        List<EditField> mFieldLocals = new List<EditField>();
        int				mFieldTabIndex;

        // Threaded simulation variables
        Thread mThread;
        volatile bool mThreadExit;
        volatile Exception mThreadException;
        volatile int mThreadSpeedGPS;
        volatile bool mThreadPaused;
        object mThreadLock = new object();
        int mThreadSimGen;

        int mRealTotalGen;
        int mTotalGen;
        int mThisGen;
        int mPrevGen;
        bool mSimWasStopped;

        /// <summary>
        /// Quickie struct to put in a combo box
        /// </summary>
        struct sComboSpeed
        {
            public string	Name;
            public int		GPS;
            
            public override string ToString()
            {
                return Name;
            }
            public sComboSpeed(string name, int gps)
            {
                Name = name;
                GPS = gps;
            }
        }

        public FormSimulate()
        {
            InitializeComponent();
        }


        private void FormSimulate_Load(object sender, EventArgs e)
        {
            // Initialize form variables
            Text = Box.ParseBox.NameDecl.VariableName + " Simulation";
            comboSpeed.Items.Add(new sComboSpeed("Speed: 100 gates/sec", 100));
            comboSpeed.Items.Add(new sComboSpeed("Speed: 1000 gates/sec", 1000));
            comboSpeed.Items.Add(new sComboSpeed("Speed: Fast as possible", 0));
            comboSpeed.SelectedIndex = 0;
            mThreadSpeedGPS = 100;
            labelStats.Text = "Simulating " + Box.GatesInLinkedCode() + " gates";


            // Setup edit field parameters IN/OUT
            SetupEditField(Box.Symbol, true);
            foreach (Symbol symbol in Box.Params)
                SetupEditField(symbol, true);

            // Setup edit field locals
            foreach (Symbol symbol in Box.Locals)
                SetupEditField(symbol, false);
            
            // Give the first input field the focus
            if (mFieldTabs.Count != 0)
            {
                mFieldInputs[0].Editor.CursorLoc = new TokenLoc(0, 1000);
                mFieldTabs[0].Focus();
            }


            // Setup input field locations
            int left = 4;
            int inY = 4;
            int outY = 4;
            foreach (EditField field in mFieldInputs)
            {
                panelParams.Controls.Add(field);
                field.Location = new Point(left, inY);
                inY += field.Height;
                field.Visible = true;
            }
            // Setup output field locations
            foreach (EditField field in mFieldOutputs)
            {
                panelParams.Controls.Add(field);
                field.Location = new Point(left + field.Width, outY);
                outY += field.Height;
                field.Visible = true;
            }
            // Setup "Locals" label
            panelParams.Height = Math.Max(inY, outY) + 8;
            labelLocals.Top = panelParams.Bottom + 8;
            panelLocals.Top = labelLocals.Bottom;
            panelLocals.Height = Math.Max(1, ClientRectangle.Height - panelLocals.Top - 4);

            // Setup local field locations (two columns)
            int half = (mFieldLocals.Count+1)/2;
            for (int i = 0;  i < mFieldLocals.Count;  i++)
            {
                EditField field = mFieldLocals[i];
                panelLocals.Controls.Add(field);
                field.Location = new Point(4 + (i >= half ? field.Width : 0),
                                    4 + (i >= half ? i-half : i) *field.Height);
                field.Visible = true;
            }


            // Show this form before displaying an error message
            Show();

            // Initialize the simulation (wait for stabilization)
            if (!ResetSim(OpState.Zero))
            {
                MessageBox.Show(this, "Error: The simulation is un-stable.  This can be "
                                      + "caused by something like: A = !A");
            }

            // Start thread running
            Application.ApplicationExit += new EventHandler(Application_ApplicationExit);
            mThread = new Thread((ThreadStart)delegate{SimulationThread();});
            mThread.Start();
        }


        /// <summary>
        /// Reset the simulation.  Returns TRUE if it worked, or FALSE
        /// if the simulation did not stabilize.
        /// </summary>
        /// <returns></returns>
        bool ResetSim(OpState resetState)
        {
            // Clear state of all gates
            int changes = 0;
            lock (mThreadLock)
            {
                // Clear state
                foreach (OpCodeExpr expr in Box.LinkedCode)
                    expr.Expr.VisitNodes(
                        delegate(OpCode op){ op.State = op.PrevState = resetState; });

                // Simulate expressions (try to stabilize the simulation)
                SimGen(Box.LinkedCode, null, true);
                changes = SimGen(Box.LinkedCode, null, true);
                for (int i = 0;  i < 100 && changes != 0;  i++)
                    changes = SimGen(Box.LinkedCode, null, true);

                // Simulate gates (see if something is un-stable)
                SimGen(Box.LinkedCode, null, false);
                SimGen(Box.LinkedCode, null, false);
                changes = SimGen(Box.LinkedCode, null, false);

                // Update fields
                foreach (EditField field in mFields)
                    field.UpdateTextValue(false);
            }
            return changes == 0;
        }


        /// <summary>
        /// Exit the thread when the application shuts down
        /// </summary>
        void Application_ApplicationExit(object sender, EventArgs e)
        {
            mThreadExit = true;
        }

        /// <summary>
        /// Exit the thread when this form closes
        /// </summary>
        private void FormSimulate_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Shut down thread
            mThreadExit = true;
            if (mThread != null)
                mThread.Join();
            Application.ApplicationExit -= Application_ApplicationExit;
        }

        /// <summary>
        /// Create an edit field from the declaration
        /// </summary>
        void SetupEditField(Symbol symbol, bool parameter)
        {
            // Non bit value?
            if (symbol.CodeExprIndex < 0 || symbol.ArraySize <= 0)
                return;

            // Create array of bits
            List<OpCodeExpr> expr = new List<OpCodeExpr>();
            for (int i = 0; i < symbol.ArraySize; i++)
                expr.Add(Box.LinkedCode[i+symbol.CodeExprIndex]);

            // Create control
            EditField field = new EditField();
            field.Text = symbol.Decl.VariableName;
            field.Expressions = expr;
            field.Visible = false;
            if (parameter)
            {
                if (field.IsOutput())
                {
                    // Setup output only field (user output)
                    mFieldOutputs.Add(field);
                }
                else
                {
                    // Setup an input field (user input)
                    mFieldInputs.Add(field);
                    mFieldTabs.Add(field);
                }
            }
            else
            {
                // Setup a local field
                mFieldLocals.Add(field);
            }

            field.TabStop = !field.IsOutput();
            mFields.Add(field);
        }



        /// <summary>
        /// Simulate a generation of expressions.  All expressions in the
        /// simulation are executed.  The simulation list is not changed.
        /// All expressions that report a change are recorded in changes.
        /// </summary>
        int SimGen(List<OpCodeExpr> simulate, List<OpCodeExpr> changes, bool quick)
        {
            int numChanges = 0;
            if (changes != null)
                changes.Clear();
            foreach (OpCodeExpr expr in simulate)
            {
                if (expr.Expr.Eval(quick))
                {
                    if (changes != null)
                        changes.Add(expr);
                    numChanges++;
                }
            }
            return numChanges;
        }


        /// <summary>
        /// This is the simulation loop, which runs in its own thread
        /// </summary>
        void SimulationThread()
        {
            try
            {
                DateTime baseTime = DateTime.Now;
                int		 gatesSinceBaseTime = 0;
                while (!mThreadExit)
                {
                    // Check for paused mode
                    if (mThreadPaused)
                    {
                        Thread.Sleep(10);
                        continue;
                    }

                    // See if we need to throttle the simulation
                    if (mThreadSpeedGPS != 0)
                    {
                        int maxGatesAllowed = (int)((DateTime.Now - baseTime).TotalSeconds*mThreadSpeedGPS);
                        if (gatesSinceBaseTime >= maxGatesAllowed)
                        {
                            Thread.Sleep(10);
                            continue;
                        }
                    }

                    // For now, simulate EVERYTHING
                    int changes;
                    lock (mThreadLock)
                        changes = SimGen(Box.LinkedCode, null, false);

                    if (changes != 0)
                    {
                        Interlocked.Increment(ref mThreadSimGen);
                        gatesSinceBaseTime++;
                    }
                    else
                    {
                        // Reset the gate counter
                        Thread.Sleep(10);
                        baseTime = DateTime.Now;
                        gatesSinceBaseTime = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                // Marshall exceptions back to main windows thread
                mThreadException = ex;  // Once set, we are officially done
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            // Since we're doing all the focus stuff,
            // ensure we know which child control has it
            for (int i = 0;  i < mFieldTabs.Count;  i++)
                if (mFieldTabs[i].Focused || mFieldTabs[i].Editor.Focused)
                    mFieldTabIndex = i;

            // Don't let controls get the focus
            if (mFieldTabs.Count != 0 && comboSpeed.Focused && !comboSpeed.DroppedDown)
                mFieldTabs[mFieldTabIndex].Editor.Focus();

            // Update generation labels
            if (mRealTotalGen != mThreadSimGen)
            {
                // When the simulation starts, rotate generations
                if (mSimWasStopped)
                {
                    mPrevGen = mThisGen;
                    mThisGen = 0;
                    mSimWasStopped = false;
                }

                int diff = mThreadSimGen - mRealTotalGen;
                mRealTotalGen += diff;
                mTotalGen += diff;
                mThisGen += diff;
                labelTotalGen.Text = "" + mTotalGen;
                labelThisGen.Text = "" + mThisGen;
                labelPrevGen.Text = "" + mPrevGen;
            }
            else
            {
                // Simulation was not running
                mSimWasStopped = true;
            }
    
            // Display data in the fields
            lock (mThreadLock)
                foreach (EditField field in mFields)
                    field.UpdateReadValue();

            // Marshal exceptions back from the simulation thread
            if (mThreadException != null)
            {
                timer1.Enabled = false;
                MessageBox.Show(this, "The simulation thread stopped working.\r\n\r\n"
                                     + "Error Message: " + mThreadException.Message
                                     + "\r\n\r\nThis window will now close");
                // Shutdown this form (NOTE: The thread has exited)
                mThread = null;
                Close();
            }

        }


        /// <summary>
        /// Tab and up/down arrow keys must move the focus
        /// manually, since the editor eats them up.
        /// </summary>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Up || (e.KeyCode == Keys.Tab) && e.Shift)
                ProcessTabKey(false);
            if (e.KeyCode == Keys.Down || (e.KeyCode == Keys.Tab) && !e.Shift)
                ProcessTabKey(true);
            base.OnKeyDown(e);
        }

        /// <summary>
        /// Override form tab handling to get what we want
        /// </summary>
        protected override bool ProcessTabKey(bool forward)
        {
            if (mFieldTabs.Count == 0)
                return true;
            mFieldTabIndex += mFieldTabs.Count + (forward ? 1 : -1);
            mFieldTabIndex %= mFieldTabs.Count;
            mFieldTabs[mFieldTabIndex].Focus();
            Editor editor = mFieldTabs[mFieldTabIndex].Editor;
            editor.Focus();
            editor.CursorLoc = new TokenLoc(0, 1000);
            return true;
        }

        /// <summary>
        /// User changes simulation speed
        /// </summary>
        private void comboSpeed_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboSpeed.SelectedIndex < 0)
                return;
            mThreadSpeedGPS = ((sComboSpeed)comboSpeed.Items[comboSpeed.SelectedIndex]).GPS;
        }



        private void buttonPause_Click(object sender, EventArgs e)
        {
            mThreadPaused = !mThreadPaused;
            if (mThreadPaused)
            {
                labelRunning.Text = "Stopped";
                buttonPause.Text = "Run (F5)";
            }
            else
            {
                labelRunning.Text = "Running";
                buttonPause.Text = "Stop (F5)";
            }
        }

        private void buttonClearCounts_Click(object sender, EventArgs e)
        {
            labelTotalGen.Text = "0";
            labelThisGen.Text = "0";
            labelPrevGen.Text = "0";
            mRealTotalGen = mThreadSimGen;
            mTotalGen = 0;
            mThisGen = 0;
            mPrevGen = 0;
        }

        private void buttonReset0_Click(object sender, EventArgs e)
        {
            // Initialize the simulation (wait for stabilization)
            if (!ResetSim(OpState.Zero))
            {
                MessageBox.Show(this, "Error: The simulation is un-stable.  This can be "
                                      + "caused by something like: A = !A");
            }
            buttonClearCounts_Click(null, null);
        }

        private void buttonReset1_Click(object sender, EventArgs e)
        {
            // Initialize the simulation (wait for stabilization)
            if (!ResetSim(OpState.One))
            {
                MessageBox.Show(this, "Error: The simulation is un-stable.  This can be "
                                      + "caused by something like: A = !A");
            }
            buttonClearCounts_Click(null, null);
        }

        
    }
}
