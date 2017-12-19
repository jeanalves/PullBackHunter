#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	public class PullBackHunter : Indicator
	{		
		private PBHunter pbStrategy0;
		private	PBHunter pbStrategy1;
							
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Indicator here.";
				Name										= "PullBackHunter";
				Calculate									= Calculate.OnPriceChange;
				IsOverlay									= true;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= false;
				DrawVerticalGridLines						= false;
				PaintPriceMarkers							= false;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event. 
				//See Help Guide for additional information.
				IsSuspendedWhileInactive					= true;
				AddPlot(Brushes.Transparent, "SellBuySignal");
				PipDiff										= 0.3;
				MaxFiboPercent								= 76.40;
				MinFiboPercent								= 50;
				Arrow_DotDownColor							= Brushes.Red;
				Arrow_DotUpColor							= Brushes.Green;
				ShowTopBottomPoints							= false;
				UseHighLow									= false;
			}
			else if (State == State.Configure)
			{
			}
			else if(State == State.DataLoaded)
			{
				pbStrategy0 = new PBHunter(this);
				pbStrategy1 = new PBHunter(this);
			}
		}

		protected override void OnBarUpdate()
		{
			
			if(CurrentBars[0] < 10)
				return;
			
			pbStrategy0.Start(0, true);
			pbStrategy1.Start(1, false);
		}
		
		#region Properties
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="PipDiff", Order=1, GroupName="Parameters")]
		public double PipDiff
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(Name="MaxFiboPercent", Order=2, GroupName="Parameters")]
		public double MaxFiboPercent
		{ get; set; }			
		
		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(Name="MinFiboPercent", Order=3, GroupName="Parameters")]
		public double MinFiboPercent
		{ get; set; }
		
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="Arrow_DotDownColor", Order=4, GroupName="Parameters")]
		public Brush Arrow_DotDownColor
		{ get; set; }

		[Browsable(false)]
		public string Arrow_DotDownColorSerializable
		{
			get { return Serialize.BrushToString(Arrow_DotDownColor); }
			set { Arrow_DotDownColor = Serialize.StringToBrush(value); }
		}
		
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="Arrow_DotUpColor", Order=5, GroupName="Parameters")]
		public Brush Arrow_DotUpColor
		{ get; set; }

		[Browsable(false)]
		public string Arrow_DotUpColorSerializable
		{
			get { return Serialize.BrushToString(Arrow_DotUpColor); }
			set { Arrow_DotUpColor = Serialize.StringToBrush(value); }
		}
		
		[NinjaScriptProperty]
		[Display(Name="ShowTopBottomPoints", Order=6, GroupName="Parameters")]
		public bool ShowTopBottomPoints
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="UseHighLow", Order=7, GroupName="Parameters")]
		public bool UseHighLow
		{ get; set; }
		
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> SellBuySignal
		{
			get { return Values[0]; }
		}
		#endregion

	}
	public class PBHunter
	{
		private double 			low;
		private double 			high;
							
		private double 			lastPrice;
		
		private double 			lastBottom;
		private double 			lastTop;
		
		private bool 			isFirstLowValue;
		private bool 			isFirstHighValue;
				
		private bool			isFalling;
		private bool			isRising;
		
		private bool			isOverLowPipDiff;
		private bool			isOverHighPipDiff;
		
		private string 			currentLowTagNameZz;
		private string 			currentHighTagNameZz;
		
		private string			currentLowTagNamePb;
		private string			currentHighTagNamePb;
		
		private bool			starting;
		
		private const double	pipSpace	= 0.2;
			
		private PullBackHunter 	pb;
		
		public PBHunter(PullBackHunter _pb)
		{			
			
			starting = true;
			pb 		= _pb;
			
		}
		
		public void Start(int barsAgo, bool justCurrentCandleSignal)
		{
				
			if(starting)
			{
				low						= pb.Input[0];
				high					= pb.Input[0];
				
				isFirstLowValue			= true;
				isFirstHighValue		= true;
				
				currentLowTagNamePb 	= "0";
				currentHighTagNamePb	= "1";
				
				starting				= false;
			}
			
			if(pb.IsFirstTickOfBar)
			{
				pb.RemoveDrawObject(@"Arrow down");
				pb.RemoveDrawObject(@"Arrow up");
				pb.RemoveDrawObject(@"Fall Dot");
				pb.RemoveDrawObject(@"Rise Dot");
			}
			
			//Calculation
			isFalling			= pb.Close[barsAgo] < pb.Close[barsAgo + 1];
			isRising			= pb.Close[barsAgo] > pb.Close[barsAgo + 1] ;
			
			isOverLowPipDiff	= pb.Close[barsAgo] <= ( high - (pb.PipDiff * (pb.TickSize * 10)));
			isOverHighPipDiff	= pb.Close[barsAgo] >= (low + (pb.PipDiff * (pb.TickSize * 10)));
						
			// Add low
			if(isFirstLowValue && isFalling && isOverLowPipDiff)
			{
				low 				= pb.Close[barsAgo];
				lastPrice			= low;
				lastTop				= high;
				
				// Show fibo signals
				if(fiboCalc(high, lastBottom, lastPrice, -1) < pb.MaxFiboPercent
				   	&& fiboCalc(high, lastBottom, lastPrice, -1) > pb.MinFiboPercent)
				{
					pb.Value[barsAgo] 		= 1;
					if(!justCurrentCandleSignal)
					{
						currentLowTagNamePb		= @"Arrow up " + pb.Time[barsAgo].ToString();
						Draw.ArrowUp(pb, currentLowTagNamePb, false, barsAgo, pb.Close[barsAgo] - (pipSpace * (pb.TickSize * 10)), pb.Arrow_DotUpColor);
					}
					else
						Draw.ArrowUp(pb, @"Arrow up", false, barsAgo, pb.Close[barsAgo] - (pipSpace * (pb.TickSize * 10)), pb.Arrow_DotUpColor);
				}	
				
				// Show zigzag points
				if(pb.ShowTopBottomPoints)
				{
					if(!justCurrentCandleSignal)
					{
						currentLowTagNameZz		= @"Fall Dot " + pb.Time[barsAgo].ToString();
						Draw.Dot(pb, currentLowTagNameZz, false, barsAgo, pb.Close[barsAgo], pb.Arrow_DotDownColor);
					}
					else
						Draw.Dot(pb, @"Fall Dot", false, barsAgo, pb.Close[barsAgo], pb.Arrow_DotDownColor);
				}
					
				isFirstLowValue		= false;
				isFirstHighValue	= true;
				return;
			}
			// Add high
			else if(isFirstHighValue && isRising && isOverHighPipDiff)
			{
				high 				= pb.Close[barsAgo];
				lastPrice			= high;
				lastBottom 			= low;
				
				// Show fibo signals
				if(fiboCalc(lastTop, low, lastPrice, 1) < pb.MaxFiboPercent
				   	&& fiboCalc(lastTop, low, lastPrice, 1) > pb.MinFiboPercent)
				{
					pb.Value[barsAgo]			= -1;
					if(!justCurrentCandleSignal)
					{
						currentHighTagNamePb 	= @"Arrow down " + pb.Time[barsAgo].ToString();
						Draw.ArrowDown(pb, currentHighTagNamePb, false, barsAgo, pb.Close[barsAgo] + (pipSpace * (pb.TickSize * 10)), pb.Arrow_DotDownColor);
					}
					else
						Draw.ArrowDown(pb, @"Arrow down", false, barsAgo, pb.Close[barsAgo] + (pipSpace * (pb.TickSize * 10)), pb.Arrow_DotDownColor);
				}
				
				// Show zigzag points
				if(pb.ShowTopBottomPoints)
				{
					if(!justCurrentCandleSignal)
					{
						currentHighTagNameZz	= @"Rise Dot " + pb.Time[barsAgo].ToString();
						Draw.Dot(pb, currentHighTagNameZz, false, barsAgo, pb.Close[barsAgo], pb.Arrow_DotUpColor);
					}
					else
						Draw.Dot(pb, @"Rise Dot", false, barsAgo, pb.Close[barsAgo], pb.Arrow_DotUpColor);
				}
				
				isFirstHighValue 	= false;
				isFirstLowValue		= true;
				return;
			}
			// Update low
			if(!isFirstLowValue && isFalling && isOverLowPipDiff && pb.Close[barsAgo] < lastPrice)
			{
				low 				= pb.Close[barsAgo];
				lastPrice			= low;
				
				// Show fibo signals
				if(fiboCalc(high, lastBottom, lastPrice, -1) < pb.MaxFiboPercent
				   	&& fiboCalc(high, lastBottom, lastPrice, -1) > pb.MinFiboPercent)
				{
					pb.Value[barsAgo] = 1;
					if(!justCurrentCandleSignal)
						Draw.ArrowUp(pb, currentLowTagNamePb, false, barsAgo, pb.Close[barsAgo] - (pipSpace * (pb.TickSize * 10)) , pb.Arrow_DotUpColor);
					else
						Draw.ArrowUp(pb, @"Arrow up", false, barsAgo, pb.Close[barsAgo] - (pipSpace * (pb.TickSize * 10)), pb.Arrow_DotUpColor);
				}
				
				// Show zigzag points
				if(pb.ShowTopBottomPoints)
				{
					if(!justCurrentCandleSignal)
						Draw.Dot(pb, currentLowTagNameZz, false, barsAgo, pb.Close[barsAgo], pb.Arrow_DotDownColor);							
					else
						Draw.Dot(pb, @"Fall Dot", false, barsAgo, pb.Close[barsAgo], pb.Arrow_DotDownColor);
				}
			}
			// Update high
			else if(!isFirstHighValue && isRising && isOverHighPipDiff && pb.Close[barsAgo] > lastPrice)
			{
				high 				= pb.Close[barsAgo];
				lastPrice			= high;
				
				// Show fibo signals
				if(fiboCalc(lastTop, low, lastPrice, 1) < pb.MaxFiboPercent
				   	&& fiboCalc(lastTop, low, lastPrice, 1) > pb.MinFiboPercent)
				{
					pb.Value[barsAgo]	= -1;
					if(!justCurrentCandleSignal)
						Draw.ArrowDown(pb, currentHighTagNamePb, false, barsAgo, pb.Close[barsAgo] + (pipSpace * (pb.TickSize * 10)), pb.Arrow_DotDownColor);
					else
						Draw.ArrowDown(pb, @"Arrow down", false, barsAgo, pb.Close[barsAgo] + (pipSpace * (pb.TickSize * 10)), pb.Arrow_DotDownColor);
				}
				
				// Show zigzag points
				if(pb.ShowTopBottomPoints)
				{
					if(!justCurrentCandleSignal)
						Draw.Dot(pb, currentHighTagNameZz, false, barsAgo, pb.Close[barsAgo], pb.Arrow_DotUpColor);
					else
						Draw.Dot(pb, @"Rise Dot", false, barsAgo, pb.Close[barsAgo], pb.Arrow_DotUpColor);
				}
			}
		}
		
		private double fiboCalc(double priceValue1, double priceValue2, double pbPriceValue, int trendDir)
		{
			double diffComp 		= priceValue1 - priceValue2;
			double diffCompPbValue 	= pbPriceValue - priceValue2;
			
			double var0				= (diffCompPbValue * 100) / diffComp;
			double var1				= (((diffCompPbValue * 100) / diffComp) - 100) * -1;
			
			if(trendDir == 1)
				return var0 >=0 && var0 <= 100 ? var0 : 0;
			else if(trendDir == -1)
				return var1 >=0 && var1 <=100 ? var1 : 0;
			
			return 0;
		}
		
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private PullBackHunter[] cachePullBackHunter;
		public PullBackHunter PullBackHunter(double pipDiff, double maxFiboPercent, double minFiboPercent, Brush arrow_DotDownColor, Brush arrow_DotUpColor, bool showTopBottomPoints, bool useHighLow)
		{
			return PullBackHunter(Input, pipDiff, maxFiboPercent, minFiboPercent, arrow_DotDownColor, arrow_DotUpColor, showTopBottomPoints, useHighLow);
		}

		public PullBackHunter PullBackHunter(ISeries<double> input, double pipDiff, double maxFiboPercent, double minFiboPercent, Brush arrow_DotDownColor, Brush arrow_DotUpColor, bool showTopBottomPoints, bool useHighLow)
		{
			if (cachePullBackHunter != null)
				for (int idx = 0; idx < cachePullBackHunter.Length; idx++)
					if (cachePullBackHunter[idx] != null && cachePullBackHunter[idx].PipDiff == pipDiff && cachePullBackHunter[idx].MaxFiboPercent == maxFiboPercent && cachePullBackHunter[idx].MinFiboPercent == minFiboPercent && cachePullBackHunter[idx].Arrow_DotDownColor == arrow_DotDownColor && cachePullBackHunter[idx].Arrow_DotUpColor == arrow_DotUpColor && cachePullBackHunter[idx].ShowTopBottomPoints == showTopBottomPoints && cachePullBackHunter[idx].UseHighLow == useHighLow && cachePullBackHunter[idx].EqualsInput(input))
						return cachePullBackHunter[idx];
			return CacheIndicator<PullBackHunter>(new PullBackHunter(){ PipDiff = pipDiff, MaxFiboPercent = maxFiboPercent, MinFiboPercent = minFiboPercent, Arrow_DotDownColor = arrow_DotDownColor, Arrow_DotUpColor = arrow_DotUpColor, ShowTopBottomPoints = showTopBottomPoints, UseHighLow = useHighLow }, input, ref cachePullBackHunter);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.PullBackHunter PullBackHunter(double pipDiff, double maxFiboPercent, double minFiboPercent, Brush arrow_DotDownColor, Brush arrow_DotUpColor, bool showTopBottomPoints, bool useHighLow)
		{
			return indicator.PullBackHunter(Input, pipDiff, maxFiboPercent, minFiboPercent, arrow_DotDownColor, arrow_DotUpColor, showTopBottomPoints, useHighLow);
		}

		public Indicators.PullBackHunter PullBackHunter(ISeries<double> input , double pipDiff, double maxFiboPercent, double minFiboPercent, Brush arrow_DotDownColor, Brush arrow_DotUpColor, bool showTopBottomPoints, bool useHighLow)
		{
			return indicator.PullBackHunter(input, pipDiff, maxFiboPercent, minFiboPercent, arrow_DotDownColor, arrow_DotUpColor, showTopBottomPoints, useHighLow);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.PullBackHunter PullBackHunter(double pipDiff, double maxFiboPercent, double minFiboPercent, Brush arrow_DotDownColor, Brush arrow_DotUpColor, bool showTopBottomPoints, bool useHighLow)
		{
			return indicator.PullBackHunter(Input, pipDiff, maxFiboPercent, minFiboPercent, arrow_DotDownColor, arrow_DotUpColor, showTopBottomPoints, useHighLow);
		}

		public Indicators.PullBackHunter PullBackHunter(ISeries<double> input , double pipDiff, double maxFiboPercent, double minFiboPercent, Brush arrow_DotDownColor, Brush arrow_DotUpColor, bool showTopBottomPoints, bool useHighLow)
		{
			return indicator.PullBackHunter(input, pipDiff, maxFiboPercent, minFiboPercent, arrow_DotDownColor, arrow_DotUpColor, showTopBottomPoints, useHighLow);
		}
	}
}

#endregion
