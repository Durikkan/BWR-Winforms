using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BWRWinforms
{
    internal class EnergyTracker
    {
        private double _fuelGeneratedEnergy;
        internal double totalFuelGeneratedEnergy = 0;

        public double FuelGeneratedEnergy
        {
            get { return _fuelGeneratedEnergy; }
            set { _fuelGeneratedEnergy = value; totalFuelGeneratedEnergy += value; }
        }

        private double _fuelToWaterEnergy;
        internal double totalFuelToWaterEnergy = 0;

        public double FuelToWaterEnergy
        {
            get { return _fuelToWaterEnergy; }
            set { _fuelToWaterEnergy = value; totalFuelToWaterEnergy += value; }
        }

        private double _coolantToSteamEnergy;
        internal double totalCoolantToSteamEnergy = 0;

        public double CoolantToSteamEnergy
        {
            get { return _coolantToSteamEnergy; }
            set { _coolantToSteamEnergy = value; totalCoolantToSteamEnergy += value; }
        }

        private double _coolantToVesselEnergy;
        internal double totalCoolantToVesselEnergy = 0;

        public double CoolantToVesselEnergy
        {
            get { return _coolantToVesselEnergy; }
            set { _coolantToVesselEnergy = value; totalCoolantToVesselEnergy += value; }
        }

        private double _steamToPipeEnergy;
        private double totalSteamToPipeEnergy = 0;

        public double SteamToPipeEnergy
        {
            get { return _steamToPipeEnergy; }
            set { _steamToPipeEnergy = value; totalSteamToPipeEnergy += value; }
        }

        private double _pipeToTurbineEnergy;
        private double totalPipeToTurbineEnergy = 0;

        public double PipeToTurbineEnergy
        {
            get { return _pipeToTurbineEnergy; }
            set { _pipeToTurbineEnergy = value; totalPipeToTurbineEnergy += value; }
        }

        private double _pipeToMainSteamReheat;
        private double totalPipeToMainSteamReheatEnergy = 0;

        public double PipeToMainSteamReheatEnergy
        {
            get { return _pipeToMainSteamReheat; }
            set { _pipeToMainSteamReheat = value; totalPipeToMainSteamReheatEnergy += value; }
        }

        private double _mainSteamReheatApplied;
        private double totalMainSteamReheatApplied = 0;

        public double MainSteamReheatApplied
        {
            get { return _mainSteamReheatApplied; }
            set { _mainSteamReheatApplied = value; totalMainSteamReheatApplied += value; }
        }

        private double _bypassEnergy;
        private double totalBypassEnergy = 0;


        public double BypassEnergy
        {
            get { return _bypassEnergy; }
            set { _bypassEnergy = value; totalBypassEnergy += value; }
        }

        private double _steamEnteringCondenserEnergy;
        private double totalSteamEnteringCondenserEnergy = 0;

        public double SteamEnteringCondenserEnergy
        {
            get { return _steamEnteringCondenserEnergy; }
            set { _steamEnteringCondenserEnergy = value; totalSteamEnteringCondenserEnergy += value; }
        }

        private double _waterEnteringCondenserEnergy;
        private double totalWaterEnteringCondenserEnergy = 0;

        public double WaterEnteringCondenserEnergy
        {
            get { return _waterEnteringCondenserEnergy; }
            set { _waterEnteringCondenserEnergy = value; totalWaterEnteringCondenserEnergy += value; }
        }

        private double _condenserCoolingEnergy;
        private double totalCondenserCoolingEnergy = 0;

        public double CondenserCoolingEnergy
        {
            get { return _condenserCoolingEnergy; }
            set { _condenserCoolingEnergy = value; totalCondenserCoolingEnergy += value; }
        }        

        private double _condenserOutflowEnergy;
        private double totalCondenserOutflowEnergy = 0;

        public double CondenserOutflowEnergy
        {
            get { return _condenserOutflowEnergy; }
            set { _condenserOutflowEnergy = value; totalCondenserOutflowEnergy += value; }
        }       

        private double _condenserOutflowHeatingEnergy;
        private double totalCondenserOutflowHeatingEnergy = 0;

        public double CondenserOutflowHeatingEnergy
        {
            get { return _condenserOutflowHeatingEnergy; }
            set { _condenserOutflowHeatingEnergy = value; totalCondenserOutflowHeatingEnergy += value; }
        }

        private double _otherFlowIntoFeedWaterEnergy;
        private double totalOtherFlowIntoFeedWaterEnergy = 0;

        public double OtherFlowIntoFeedWaterEnergy
        {
            get { return _otherFlowIntoFeedWaterEnergy; }
            set { _otherFlowIntoFeedWaterEnergy = value; totalOtherFlowIntoFeedWaterEnergy += value; }
        }

        private double _feedWaterEnergy;
        private double totalFeedWaterEnergy = 0;

        public double FeedWaterEnergy
        {
            get { return _feedWaterEnergy; }
            set { _feedWaterEnergy = value; totalFeedWaterEnergy += value; }
        }

        private double _feedwaterHeatingEnergy;
        private double totalFeedwaterHeatingEnergy = 0;

        public double FeedwaterHeatingEnergy
        {
            get { return _feedwaterHeatingEnergy; }
            set { _feedwaterHeatingEnergy = value; totalFeedwaterHeatingEnergy += value; }
        }

        internal double TurbineOutput;
        internal double TurbineWaste;

        internal double totalTurbine;

        internal double pumpInput;

        internal string GetReport()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Fuel: \t\t\t{Form1.ToSI(_fuelGeneratedEnergy * 10, capToZero: true) + "W",-11}\t {Form1.ToSI(totalFuelGeneratedEnergy, capToZero: true)}J");
            sb.AppendLine($"Fuel to Coolant:  \t\t{Form1.ToSI(_fuelToWaterEnergy * 10, capToZero: true) + "W",-11}\t {Form1.ToSI(totalFuelToWaterEnergy, capToZero: true)}J");
            sb.AppendLine($"Coolant to Steam:  \t\t{Form1.ToSI(_coolantToSteamEnergy * 10, capToZero: true) + "W",-11}\t {Form1.ToSI(totalCoolantToSteamEnergy, capToZero: true)}J");
            sb.AppendLine($"Coolant to Vessel:  \t\t{Form1.ToSI(_coolantToVesselEnergy * 10, capToZero: true) + "W",-11}\t {Form1.ToSI(totalCoolantToVesselEnergy, capToZero: true)}J");
            sb.AppendLine($"Steam to Pipe:  \t\t{Form1.ToSI(_steamToPipeEnergy * 10, capToZero: true) + "W",-11}\t {Form1.ToSI(totalSteamToPipeEnergy, capToZero: true)}J");
            sb.AppendLine($"Pipe to Turbine:  \t\t{Form1.ToSI(_pipeToTurbineEnergy * 10, capToZero: true) + "W",-11}\t {Form1.ToSI(totalPipeToTurbineEnergy, capToZero: true)}J");
            sb.AppendLine($"Pipe to MSR:  \t\t{Form1.ToSI(PipeToMainSteamReheatEnergy * 10, capToZero: true) + "W",-11}\t {Form1.ToSI(totalPipeToMainSteamReheatEnergy, capToZero: true)}J");
            sb.AppendLine($"MSR Applied:  \t\t{Form1.ToSI(MainSteamReheatApplied * 10, capToZero: true) + "W",-11}\t {Form1.ToSI(totalMainSteamReheatApplied, capToZero: true)}J");
            sb.AppendLine($"Bypass:  \t\t\t{Form1.ToSI(_bypassEnergy * 10, capToZero: true) + "W",-11}\t {Form1.ToSI(totalBypassEnergy, capToZero: true)}J");
            sb.AppendLine($"Condenser Steam Entry:  \t{Form1.ToSI(_steamEnteringCondenserEnergy * 10, capToZero: true) + "W",-11}\t {Form1.ToSI(totalSteamEnteringCondenserEnergy, capToZero: true)}J");            
            sb.AppendLine($"Condenser Cooling:  \t{Form1.ToSI(_condenserCoolingEnergy * 10, capToZero: true) + "W",-11}\t {Form1.ToSI(totalCondenserCoolingEnergy, capToZero: true)}J");
            sb.AppendLine($"Condenser Water Entry:  \t{Form1.ToSI(_waterEnteringCondenserEnergy * 10, capToZero: true) + "W",-11}\t {Form1.ToSI(totalWaterEnteringCondenserEnergy, capToZero: true)}J");
            sb.AppendLine($"Condenser Outflow:  \t{Form1.ToSI(_condenserOutflowEnergy * 10, capToZero: true) + "W",-11}\t {Form1.ToSI(totalCondenserOutflowEnergy, capToZero: true)}J");
            sb.AppendLine($"Condenser Outflow Heating:  \t{Form1.ToSI(_condenserOutflowHeatingEnergy * 10, capToZero: true) + "W",-11}\t {Form1.ToSI(totalCondenserOutflowHeatingEnergy, capToZero: true)}J");
            sb.AppendLine($"Waste Flow To Feedwater:  \t{Form1.ToSI(_otherFlowIntoFeedWaterEnergy * 10, capToZero: true) + "W",-11}\t {Form1.ToSI(totalOtherFlowIntoFeedWaterEnergy, capToZero: true)}J");
            sb.AppendLine($"Feedwater Flow:  \t\t{Form1.ToSI(_feedWaterEnergy * 10, capToZero: true) + "W",-11}\t {Form1.ToSI(totalFeedWaterEnergy, capToZero: true)}J");
            sb.AppendLine($"Feedwater Heating:  \t{Form1.ToSI(_feedwaterHeatingEnergy * 10, capToZero: true) + "W",-11}\t {Form1.ToSI(totalFeedwaterHeatingEnergy, capToZero: true)}J");
            sb.AppendLine($"Feedwater Total:  \t\t{Form1.ToSI((_feedWaterEnergy + _feedwaterHeatingEnergy) * 10, capToZero: true) + "W",-11}\t {Form1.ToSI(totalFeedWaterEnergy + totalFeedwaterHeatingEnergy, capToZero: true)}J");
            sb.AppendLine();
            //sb.AppendLine($"Mystery Shortfall:  \t\t{Form1.ToSI((_steamToPipeEnergy - _fuelToWaterEnergy - _feedWaterEnergy - _feedwaterHeatingEnergy) * 10, capToZero: true) + "W",-11}\t {Form1.ToSI(totalSteamToPipeEnergy - totalFuelToWaterEnergy - totalFeedWaterEnergy - totalFeedwaterHeatingEnergy, capToZero: true)}J");
            //sb.AppendLine();
            //sb.AppendLine($"Energy Error:  \t\t{Form1.ToSI(10 * (_steamToPipeEnergy - TurbineOutput / 10 - _condenserCoolingEnergy - _feedWaterEnergy - _feedwaterHeatingEnergy), capToZero: true) + "J",-11}\t{Form1.ToSI(totalSteamToPipeEnergy - totalTurbine - totalCondenserCoolingEnergy - totalFeedWaterEnergy - totalFeedwaterHeatingEnergy, capToZero: true) + "J"}");
            sb.AppendLine($"Reactor Input: \t {Form1.ToSI(_fuelGeneratedEnergy * 10, capToZero: true)}W");
            sb.AppendLine($"Pump Input: \t {Form1.ToSI(pumpInput * 10, capToZero: true)}W");
            sb.AppendLine($"Useful Output :\t {Form1.ToSI(TurbineOutput - TurbineWaste, capToZero: true)}W");
            double totalWaste = TurbineWaste + _condenserCoolingEnergy * 10;
            sb.AppendLine($"Waste Output: \t {Form1.ToSI(totalWaste, capToZero: true)}W");
            sb.AppendLine($"Total Output: \t {Form1.ToSI(TurbineOutput + totalWaste, capToZero: true)}W");
            //sb.AppendLine($"Instant Efficiency: \t {Math.Round(100 * (TurbineOutput - TurbineWaste) / (TurbineOutput - TurbineWaste + totalWaste),3)}%");
            _bypassEnergy = 0; //Manual reset as it doesn't get set every frame
            return sb.ToString();
        }

    }
}
