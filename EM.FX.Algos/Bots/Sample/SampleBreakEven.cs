﻿// -------------------------------------------------------------------------------------------------
//
//    This code is a cAlgo API sample.
//
//    This cBot is intended to be used as a sample and does not guarantee any particular outcome or
//    profit of any kind. Use it at your own risk
//
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Internals;

namespace EM.FX.Algos.Bots.Sample
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class SampleBreakEven : Robot
    {
        private Symbol _symbol;
        private const string DefaultPositionIdParameterValue = "PID";

        [Parameter("Position Id", DefaultValue = DefaultPositionIdParameterValue)]
        public string PositionId { get; set; }

        [Parameter("Add Pips", DefaultValue = 0.0, MinValue = 0.0)]
        public double AddPips { get; set; }

        [Parameter("Trigger Pips", DefaultValue = 10, MinValue = 1)]
        public double TriggerPips { get; set; }

        protected override void OnStart()
        {
            if (PositionId == DefaultPositionIdParameterValue)
                PrintErrorAndStop("You have to specify \"Position Id\" in cBot Parameters");

            if (TriggerPips < AddPips + 2)
                PrintErrorAndStop("\"Trigger Pips\" must be greater or equal to \"Add Pips\" + 2");

            var position = FindPositionOrStop();
            _symbol = MarketData.GetSymbol(position.SymbolCode);

            BreakEvenIfNeeded();
        }

        private void PrintErrorAndStop(string errorMessage)
        {
            Print(errorMessage);
            Stop();

            throw new Exception(errorMessage);
        }

        private Position FindPositionOrStop()
        {
            var position = Positions.FirstOrDefault(p => "PID" + p.Id == PositionId || p.Id.ToString() == PositionId);
            if (position == null)
                PrintErrorAndStop("Position with Id = " + PositionId + " doesn't exist");

            return position;
        }

        protected override void OnTick()
        {
            BreakEvenIfNeeded();
        }

        private void BreakEvenIfNeeded()
        {
            var position = FindPositionOrStop();

            if (position.Pips < TriggerPips)
                return;

            var desiredNetProfitInDepositAsset = AddPips * _symbol.PipValue * position.Volume;
            var desiredGrossProfitInDepositAsset = desiredNetProfitInDepositAsset - position.Commissions * 2 - position.Swap;
            var quoteToDepositRate = _symbol.PipValue / _symbol.PipSize;
            var priceDifference = desiredGrossProfitInDepositAsset / (position.Volume * quoteToDepositRate);

            var priceAdjustment = GetPriceAdjustmentByTradeType(position.TradeType, priceDifference);
            var breakEvenLevel = position.EntryPrice + priceAdjustment;
            var roundedBreakEvenLevel = RoundPrice(breakEvenLevel, position.TradeType);

            ModifyPosition(position, roundedBreakEvenLevel, position.TakeProfit);

            Print("Stop loss for position PID" + position.Id + " has been moved to break even.");
            Print("Stopping cBot..");
            Stop();
        }

        private double RoundPrice(double price, TradeType tradeType)
        {
            var multiplier = Math.Pow(10, _symbol.Digits);

            if (tradeType == TradeType.Buy)
                return Math.Ceiling(price * multiplier) / multiplier;

            return Math.Floor(price * multiplier) / multiplier;
        }

        private static double GetPriceAdjustmentByTradeType(TradeType tradeType, double priceDifference)
        {
            if (tradeType == TradeType.Buy)
                return priceDifference;

            return -priceDifference;
        }
    }
}
