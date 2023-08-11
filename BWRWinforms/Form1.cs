using System;
using System.Text;
using System.Windows.Forms;

namespace BWRWinforms
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            sim = new Sim(this);
            listBox1.SelectedIndex = 1;
            listBox2.SelectedIndex = 0;
        }

        Sim sim;

        internal double BypassValve { get; private set; }
        internal double CondenserCirculation { get; private set; }
        internal double CondenserPumps { get; private set; }
        internal double FeedwaterPumps { get; private set; }
        internal double MakeupWater { get; private set; }
        internal double MSIV { get; private set; }
        internal double MWMultiplier { get; private set; }
        internal double PCM { get; private set; }
        internal double Recirculation { get; private set; }
        internal double Rods { get; private set; }
        internal double ToCST { get; private set; }
        internal double TurbineValve { get; private set; }

        private void Timer1_Tick(object sender, EventArgs e)
        {
            if (checkBoxPause.Checked == false)
            {
                GrabTexts();
                sim.SimTick(true);
            }            
        }

        private void GrabTexts()
        {
            if (numericUpDownBypassValve.Focused == false) BypassValve = (double)numericUpDownBypassValve.Value;
            if (numericUpDownCondenserCirculation.Focused == false) CondenserCirculation = (double)numericUpDownCondenserCirculation.Value;
            if (numericUpDownCondenserPumps.Focused == false) CondenserPumps = (double)numericUpDownCondenserPumps.Value;
            if (numericUpDownFeedwaterPumps.Focused == false) FeedwaterPumps = (double)numericUpDownFeedwaterPumps.Value;
            if (numericUpDownMakeupWater.Focused == false) MakeupWater = (double)numericUpDownMakeupWater.Value;
            if (numericUpDownMSIV.Focused == false) MSIV = (double)numericUpDownMSIV.Value;
            if (numericUpDownMWMultiplier.Focused == false) MWMultiplier = (double)numericUpDownMWMultiplier.Value;
            if (numericUpDownPCM.Focused == false) PCM = (double)numericUpDownPCM.Value;
            if (numericUpDownRecirculation.Focused == false) Recirculation = (double)numericUpDownRecirculation.Value;
            if (numericUpDownRods.Focused == false) Rods = (double)numericUpDownRods.Value;
            if (numericUpDownToCST.Focused == false) ToCST = (double)numericUpDownToCST.Value;
            if (numericUpDownTurbineValve.Focused == false) TurbineValve = (double)numericUpDownTurbineValve.Value;
        }

        void CheckSet(NumericUpDown nud, double value, bool forced)
        {
            if (nud.Focused == false || forced)
                nud.Value = (decimal)value;
        }

        internal void SetBypassValve(double v, bool forced) => CheckSet(numericUpDownBypassValve, v, forced);
        internal void SetCondenserPumps(double v, bool forced) => CheckSet(numericUpDownCondenserPumps, v, forced);
        internal void SetFeedwaterPumps(double v, bool forced) => CheckSet(numericUpDownFeedwaterPumps, v, forced);
        internal void SetMSIV(double v, bool forced) => CheckSet(numericUpDownBypassValve, v, forced);
        internal void SetTurbineValve(double v, bool forced) => CheckSet(numericUpDownTurbineValve, v, forced);
        internal void SetRecirculation(double v, bool forced) => CheckSet(numericUpDownRecirculation, v, forced);
        internal void SetMWMultiplier(double v, bool forced) => CheckSet(numericUpDownMWMultiplier, v, forced);
        internal void SetRods(int v, bool forced) => CheckSet(numericUpDownRods, v, forced);



        private void Button_Scram(object sender, EventArgs e)
        {
            sim.Scram();
        }

        private void Button_Sync(object sender, EventArgs e)
        {
            sim.SyncTurbine();
        }

        private void Button_SetCondenser(object sender, EventArgs e)
        {
            sim.SetCondenserVacuum();
        }

        private void Button_Advance100(object sender, EventArgs e)
        {
            GrabTexts();
            sim.AdvanceFrames(100);
        }

        private void Button_SetFullLoad(object sender, EventArgs e)
        {
            sim = new Sim(this);
            sim.SetToFullLoad();
        }

        private void Button_ResetTrackedEnergy(object sender, EventArgs e)
        {
            sim.ResetTrackedEnergy();
        }

        private void Button8_Click(object sender, EventArgs e)
        {
            sim = new Sim(this);
            sim.BoilStart();
        }

        private void Button_Advance600(object sender, EventArgs e)
        {
            GrabTexts();
            sim.AdvanceFrames(600);
        }

        private void ListBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            sim.ListBox2_SelectedIndexChanged();
        }

        private void Button_SetDecayHeat(object sender, EventArgs e)
        {
            sim.SetDecayHeat();
        }

        private void ButtonReset_Click(object sender, EventArgs e)
        {
            sim = new Sim(this);
        }

        private void CheckBoxTooltips_CheckedChanged(object sender, EventArgs e)
        {
            toolTip1.Active = checkBoxTooltips.Checked;
        }

        private void NumericUpDownRods_ValueChanged(object sender, EventArgs e)
        {
            Rods = (double)numericUpDownRods.Value;
        }

        private void NumericUpDownRecirculation_ValueChanged(object sender, EventArgs e)
        {
            Recirculation = (double)numericUpDownRecirculation.Value;
        }

        private void NumericUpDownMSIV_ValueChanged(object sender, EventArgs e)
        {
            MSIV = (double)numericUpDownMSIV.Value;
        }

        private void NumericUpDownBypassValve_ValueChanged(object sender, EventArgs e)
        {
            BypassValve = (double)numericUpDownBypassValve.Value;
        }

        private void NumericUpDownTurbineValve_ValueChanged(object sender, EventArgs e)
        {
            TurbineValve = (double)numericUpDownTurbineValve.Value;
        }

        private void NumericUpDownCondenserPumps_ValueChanged(object sender, EventArgs e)
        {
            CondenserPumps = (double)numericUpDownCondenserPumps.Value;
        }

        private void NumericUpDownFeedwaterPumps_ValueChanged(object sender, EventArgs e)
        {
            FeedwaterPumps = (double)numericUpDownFeedwaterPumps.Value;
        }

        private void NumericUpDownCondenserCirculation_ValueChanged(object sender, EventArgs e)
        {
            CondenserCirculation = (double)numericUpDownCondenserCirculation.Value;
        }

        private void NumericUpDownMakeupWater_ValueChanged(object sender, EventArgs e)
        {
            MakeupWater = (double)numericUpDownMakeupWater.Value;
        }

        private void NumericUpDownToCST_ValueChanged(object sender, EventArgs e)
        {
            ToCST = (double)numericUpDownToCST.Value;
        }

        private void NumericUpDownPCM_ValueChanged(object sender, EventArgs e)
        {
            PCM = (double)numericUpDownPCM.Value;
        }
    }
}
