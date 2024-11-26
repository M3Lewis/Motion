// 警告：某些程序集引用无法自动解析。这可能会导致某些部分反编译错误，
// 例如属性 getter/setter 访问。要获得最佳反编译结果，请手动将缺少的引用添加到加载的程序集列表中。
// pOd_Animation.L_TimeLine.pOd_TimeLineSlider
using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using Grasshopper.GUI.Base;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using pOd_Animation;
using pOd_Animation.L_TimeLine;
using pOd_GH_Animation.Classes;
using pOd_GH_Animation.Containers;
using pOd_GH_Animation.Tasks;
using Rhino.Geometry;

public class pOd_TimeLineSlider : GH_NumberSlider
{
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	[CompilerGenerated]
	private EventHandler m_Decorator;

	private pOd_TimeLineSlider _Mapper;

	private int _Parser;

	private static pOd_TimeLineSlider CustomizeDecorator;

	public override Guid ComponentGuid
	{
		get
		{
			//Discarded unreachable code: IL_0002, IL_0034, IL_0043
			int num = 1;
			int num2 = num;
			Guid result = default(Guid);
			while (true)
			{
				switch (num2)
				{
				default:
					return result;
				case 1:
					result = new Guid((string)FillDecorator(9702));
					num2 = 0;
					if (IncludeDecorator())
					{
						num2 = 0;
					}
					break;
				}
			}
		}
	}

	public override GH_Exposure Exposure
	{
		get
		{
			//Discarded unreachable code: IL_0002, IL_0034, IL_0043
			int num = 1;
			int num2 = num;
			GH_Exposure result = default(GH_Exposure);
			while (true)
			{
				switch (num2)
				{
				case 1:
					result = GH_Exposure.tertiary;
					num2 = 0;
					if (ComputeDecorator() != null)
					{
						num2 = 0;
					}
					break;
				default:
					return result;
				}
			}
		}
	}

	public pOd_TimeLineSlider BaseSlider
	{
		get
		{
			//Discarded unreachable code: IL_0002, IL_005a, IL_0069
			int num = 1;
			int num2 = num;
			pOd_TimeLineSlider mapper = default(pOd_TimeLineSlider);
			while (true)
			{
				switch (num2)
				{
				default:
					return mapper;
				case 1:
					mapper = _Mapper;
					num2 = 0;
					if (ComputeDecorator() == null)
					{
						num2 = 0;
					}
					break;
				}
			}
		}
		set
		{
			//Discarded unreachable code: IL_0002
			int num = 1;
			while (true)
			{
				int num2 = num;
				do
				{
					switch (num2)
					{
					default:
						return;
					case 1:
						break;
					case 0:
						return;
					}
					_Mapper = value;
					num2 = 0;
				}
				while (ComputeDecorator() == null);
			}
		}
	}

	public int SortIndex
	{
		get
		{
			//Discarded unreachable code: IL_0002, IL_0037, IL_0046
			int num = 1;
			int num2 = num;
			int parser = default(int);
			while (true)
			{
				switch (num2)
				{
				default:
					return parser;
				case 1:
					parser = _Parser;
					num2 = 0;
					if (ComputeDecorator() != null)
					{
						num2 = 0;
					}
					break;
				}
			}
		}
		set
		{
			//Discarded unreachable code: IL_0002
			int num = 1;
			int num2 = num;
			while (true)
			{
				switch (num2)
				{
				default:
					return;
				case 1:
					_Parser = value;
					num2 = 0;
					if (ComputeDecorator() != null)
					{
						num2 = 0;
					}
					break;
				case 0:
					return;
				}
			}
		}
	}

	public event EventHandler ComponentMoved
	{
		[CompilerGenerated]
		add
		{
			//Discarded unreachable code: IL_0002
			int num = 4;
			EventHandler eventHandler = default(EventHandler);
			EventHandler value2 = default(EventHandler);
			EventHandler eventHandler2 = default(EventHandler);
			while (true)
			{
				int num2 = num;
				do
				{
					IL_0019:
					switch (num2)
					{
					case 5:
						return;
					case 1:
						eventHandler = Interlocked.CompareExchange(ref m_Decorator, value2, eventHandler2);
						num2 = 2;
						goto IL_0019;
					case 3:
						eventHandler2 = eventHandler;
						num2 = 0;
						if (ComputeDecorator() == null)
						{
							num2 = 0;
						}
						goto IL_0019;
					case 2:
						if ((object)eventHandler == eventHandler2)
						{
							num2 = 4;
							if (IncludeDecorator())
							{
								num2 = 5;
							}
							goto IL_0019;
						}
						goto case 3;
					case 4:
						eventHandler = m_Decorator;
						num2 = 3;
						goto IL_0019;
					}
					value2 = (EventHandler)PolicyOrder.GetUtils(eventHandler2, value, PolicyOrder.ResolveVisitor);
					num2 = 1;
				}
				while (IncludeDecorator());
			}
		}
		[CompilerGenerated]
		remove
		{
			//Discarded unreachable code: IL_0002
			int num = 2;
			int num2 = num;
			EventHandler eventHandler = default(EventHandler);
			EventHandler value2 = default(EventHandler);
			EventHandler eventHandler2 = default(EventHandler);
			while (true)
			{
				switch (num2)
				{
				case 5:
					eventHandler = Interlocked.CompareExchange(ref m_Decorator, value2, eventHandler2);
					num2 = 0;
					if (!IncludeDecorator())
					{
						num2 = 0;
					}
					continue;
				case 4:
					value2 = (EventHandler)PolicyOrder.GetUtils(eventHandler2, value, PolicyOrder.CalculateVisitor);
					num2 = 5;
					continue;
				case 2:
					eventHandler = m_Decorator;
					num2 = 1;
					if (ComputeDecorator() == null)
					{
						num2 = 1;
					}
					continue;
				default:
					if ((object)eventHandler == eventHandler2)
					{
						num2 = 3;
						continue;
					}
					break;
				case 3:
					return;
				case 1:
					break;
				}
				eventHandler2 = eventHandler;
				num2 = 4;
				if (!IncludeDecorator())
				{
					num2 = 0;
				}
			}
		}
	}

	public pOd_TimeLineSlider()
	{
		//Discarded unreachable code: IL_0002, IL_002a
		TokenizerClass.DisableDic();
		this._002Ector(null, -1);
		int num = 0;
		if (1 == 0)
		{
			int num2;
			num = num2;
		}
		switch (num)
		{
		case 0:
			break;
		}
	}

	public override string ToString()
	{
		//Discarded unreachable code: IL_0002, IL_0072, IL_00bf
		int num = 1;
		string utils = default(string);
		string text = default(string);
		string utils2 = default(string);
		Interval interval = default(Interval);
		while (true)
		{
			int num2 = num;
			while (true)
			{
				object obj;
				switch (num2)
				{
				case 6:
					utils = ProxyOrder.GetUtils((string)FillDecorator(9530), text, (string)FillDecorator(9572), utils2, ProxyOrder.WriteVisitor);
					num2 = 5;
					continue;
				case 3:
					if (!ValAnnotation.GetUtils(PoolAnnotation.GetUtils(this, PoolAnnotation.RunVisitor), "", ValAnnotation.ListVisitor))
					{
						num2 = 2;
						if (ComputeDecorator() == null)
						{
							continue;
						}
						break;
					}
					obj = PoolAnnotation.GetUtils(this, PoolAnnotation.RunVisitor);
					goto IL_0194;
				case 2:
					obj = FillDecorator(9524);
					goto IL_0194;
				case 1:
					interval = new Interval(SerializerOrder.GetUtils(SingletonOrder.GetUtils(MethodOrder.GetUtils(this, MethodOrder.InsertVisitor), SingletonOrder.RestartContext), SerializerOrder.GetVisitor), SerializerOrder.GetUtils(SingletonOrder.GetUtils(MethodOrder.GetUtils(this, MethodOrder.InsertVisitor), SingletonOrder.CollectContext), SerializerOrder.GetVisitor));
					num2 = 0;
					if (ComputeDecorator() == null)
					{
						num2 = 0;
					}
					continue;
				default:
					utils2 = InvocationAnnotation.GetUtils((string)FillDecorator(8120), ReaderOrder.GetUtils(interval.ToString(), (string)FillDecorator(8126), (string)FillDecorator(8132), ReaderOrder.InterruptContext), (string)FillDecorator(8144), InvocationAnnotation.CheckUtils);
					num2 = 3;
					if (ComputeDecorator() == null)
					{
						continue;
					}
					break;
				case 4:
				case 5:
					{
						return utils;
					}
					IL_0194:
					text = (string)obj;
					num2 = 6;
					if (IncludeDecorator())
					{
						continue;
					}
					break;
				}
				break;
			}
		}
	}

	public pOd_TimeLineSlider(pOd_TimeLineSlider slider, int index)
	{
		//Discarded unreachable code: IL_0002, IL_0028
		FindDecorator();
		base._002Ector();
		int num = 1;
		if (false)
		{
			goto IL_002d;
		}
		goto IL_0031;
		IL_0031:
		FieldInfo utils = default(FieldInfo);
		int num2;
		while (true)
		{
			switch (num)
			{
			case 4:
				StructAnnotation.GetUtils(this, (string)FillDecorator(2482), StructAnnotation.UpdateVisitor);
				num = 0;
				if (true)
				{
					continue;
				}
				break;
			case 6:
				SortIndex = index;
				num = 2;
				continue;
			case 8:
				StatusOrder.GetUtils(utils, this, pOd_Res.pOd_NumberSlider, StatusOrder.SelectVisitor);
				num = 3;
				continue;
			case 2:
				return;
			case 1:
				StructAnnotation.GetUtils(this, (string)FillDecorator(9578), StructAnnotation.FlushVisitor);
				num = 5;
				continue;
			case 5:
				StructAnnotation.GetUtils(this, (string)FillDecorator(9620), StructAnnotation.InterruptVisitor);
				num = 4;
				continue;
			case 3:
				BaseSlider = slider;
				num2 = 6;
				break;
			default:
				StructAnnotation.GetUtils(this, (string)FillDecorator(8424), StructAnnotation.SearchVisitor);
				num = 7;
				continue;
			case 7:
				utils = TemplateOrder.GetUtils(DescriptorAnnotation.GetUtils(this, DescriptorAnnotation.QueryVisitor), (string)FillDecorator(9668), BindingFlags.Instance | BindingFlags.NonPublic, TemplateOrder.CountVisitor);
				num2 = 8;
				break;
			}
			break;
		}
		goto IL_002d;
		IL_002d:
		num = num2;
		goto IL_0031;
	}

	private void CountWrapper(object ident, GH_SliderEventArgs visitor)
	{
		//Discarded unreachable code: IL_0002
		int num = 1;
		while (true)
		{
			int num2 = num;
			do
			{
				switch (num2)
				{
				default:
					return;
				case 0:
					return;
				case 1:
					break;
				}
				CallDecorator(MethodOrder.GetUtils(this, MethodOrder.InsertVisitor), new GH_SliderBase.ValueChangedEventHandler(CountWrapper));
				num2 = 0;
			}
			while (IncludeDecorator());
		}
	}

	public override void CreateAttributes()
	{
		//Discarded unreachable code: IL_0002
		int num = 2;
		while (true)
		{
			int num2 = num;
			while (true)
			{
				switch (num2)
				{
				default:
					return;
				case 0:
					return;
				case 1:
					((GH_NumberSlider2Attributes)CustomerOrder.GetUtils(this, CustomerOrder.ManageVisitor)).PivotMoved += RegisterWrapper;
					num2 = 0;
					if (ComputeDecorator() == null)
					{
						continue;
					}
					break;
				case 2:
					m_attributes = new GH_NumberSlider2Attributes(this);
					num2 = 1;
					if (ComputeDecorator() == null)
					{
						continue;
					}
					break;
				}
				break;
			}
		}
	}

	private void RegisterWrapper(object param, EventArgs cont)
	{
		//Discarded unreachable code: IL_0002
		int num = 1;
		ComponentMovedEventArgs e = default(ComponentMovedEventArgs);
		while (true)
		{
			int num2 = num;
			do
			{
				IL_0019:
				switch (num2)
				{
				case 1:
					break;
				default:
					OnComponentMoved(e);
					num2 = 2;
					if (!IncludeDecorator())
					{
						num2 = 2;
					}
					goto IL_0019;
				case 2:
					return;
				}
				e = cont as ComponentMovedEventArgs;
				num2 = 0;
			}
			while (IncludeDecorator());
		}
	}

	public void OnComponentMoved(ComponentMovedEventArgs e)
	{
		//Discarded unreachable code: IL_0002, IL_0060, IL_006f, IL_00b3, IL_00c2
		int num = 1;
		EventHandler decorator = default(EventHandler);
		while (true)
		{
			int num2 = num;
			do
			{
				IL_0019:
				switch (num2)
				{
				case 3:
				case 5:
					break;
				case 2:
					return;
				case 4:
					return;
				case 1:
					decorator = m_Decorator;
					num2 = 0;
					if (ComputeDecorator() != null)
					{
						num2 = 0;
					}
					goto IL_0019;
				default:
					if (decorator == null)
					{
						return;
					}
					num2 = 5;
					goto IL_0019;
				}
				decorator(this, e);
				num2 = 2;
			}
			while (IncludeDecorator());
		}
	}

	static pOd_TimeLineSlider()
	{
		//Discarded unreachable code: IL_0002
		ProcessTaskContainer.StartFactory();
		RemoveDecorator();
	}

	internal static bool IncludeDecorator()
	{
		//Discarded unreachable code: IL_0002
		return CustomizeDecorator == null;
	}

	internal static pOd_TimeLineSlider ComputeDecorator()
	{
		//Discarded unreachable code: IL_0002
		return CustomizeDecorator;
	}

	internal static object FillDecorator(int previousparam)
	{
		//Discarded unreachable code: IL_0002
		return ProcessTaskContainer.VisitFactory(previousparam);
	}

	internal static void FindDecorator()
	{
		//Discarded unreachable code: IL_0002
		TokenizerClass.DisableDic();
	}

	internal static void CallDecorator(object P_0, object P_1)
	{
		//Discarded unreachable code: IL_0002
		((GH_SliderBase)P_0).ValueChanged -= (GH_SliderBase.ValueChangedEventHandler)P_1;
	}

	internal static void RemoveDecorator()
	{
		//Discarded unreachable code: IL_0002
		ObserverAuthenticationClass.kLjw4iIsCLsZtxc4lksN0j();
	}
}
