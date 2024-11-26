// 警告：某些程序集引用无法自动解析。这可能会导致某些部分反编译错误，
// 例如属性 getter/setter 访问。要获得最佳反编译结果，请手动将缺少的引用添加到加载的程序集列表中。
// pOd_Animation.L_TimeLine.TimeLineSliderMax
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using GH_IO.Serialization;
using Grasshopper.GUI.Base;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using pOd_Animation;
using pOd_Animation.L_Loading;
using pOd_Animation.L_TimeLine;
using pOd_GH_Animation.Classes;
using pOd_GH_Animation.Containers;
using pOd_GH_Animation.Tasks;

public class TimeLineSliderMax : GH_Component
{
	private pOd_TimeLineSlider connection;

	private List<pOd_TimeLineSlider> _Ref;

	private Guid _Importer;

	private List<Guid> m_Registry;

	private static TimeLineSliderMax PrintRequest;

	public pOd_TimeLineSlider control
	{
		get
		{
			//Discarded unreachable code: IL_0002, IL_00bd, IL_00fd, IL_017f, IL_018e, IL_019e, IL_01ad, IL_0234, IL_0244
			int num = 14;
			bool flag4 = default(bool);
			bool flag3 = default(bool);
			bool flag2 = default(bool);
			GH_Document utils = default(GH_Document);
			bool flag = default(bool);
			IGH_DocumentObject iGH_DocumentObject = default(IGH_DocumentObject);
			pOd_TimeLineSlider pOd_TimeLineSlider2 = default(pOd_TimeLineSlider);
			pOd_TimeLineSlider result = default(pOd_TimeLineSlider);
			while (true)
			{
				int num2 = num;
				while (true)
				{
					object obj;
					switch (num2)
					{
					case 13:
						flag4 = connection != null;
						num2 = 6;
						if (!StopRequest())
						{
							num2 = 2;
						}
						continue;
					case 19:
						if (flag3)
						{
							num2 = 7;
							if (StopRequest())
							{
								continue;
							}
							break;
						}
						goto case 5;
					case 4:
					case 15:
						flag2 = utils != null;
						num2 = 0;
						if (StopRequest())
						{
							continue;
						}
						break;
					case 17:
						flag = iGH_DocumentObject != null;
						num2 = 3;
						continue;
					case 1:
						pOd_TimeLineSlider2 = iGH_DocumentObject as pOd_TimeLineSlider;
						num2 = 8;
						continue;
					case 14:
						utils = SpecificationOrder.GetUtils(this, SpecificationOrder.UpdateContext);
						num2 = 8;
						if (ReadRequest() == null)
						{
							num2 = 13;
						}
						continue;
					case 5:
						result = connection;
						num2 = 18;
						if (StopRequest())
						{
							num2 = 18;
						}
						continue;
					case 9:
						result = connection;
						num2 = 20;
						continue;
					case 8:
						flag3 = pOd_TimeLineSlider2 != null;
						num2 = 19;
						continue;
					case 11:
					case 16:
					case 18:
					case 20:
						return result;
					case 6:
						if (!flag4)
						{
							num2 = 15;
							continue;
						}
						goto case 9;
					default:
						if (flag2)
						{
							num2 = 2;
							if (ReadRequest() != null)
							{
								num2 = 1;
							}
							continue;
						}
						goto case 5;
					case 7:
						result = pOd_TimeLineSlider2;
						num2 = 11;
						if (StopRequest())
						{
							continue;
						}
						break;
					case 3:
						if (flag)
						{
							num2 = 1;
							if (StopRequest())
							{
								continue;
							}
							break;
						}
						goto case 5;
					case 10:
						obj = null;
						goto IL_026e;
					case 2:
						if (utils != null)
						{
							num2 = 12;
							continue;
						}
						goto case 10;
					case 12:
						{
							obj = AccountOrder.GetUtils(utils, _Importer, true, AccountOrder.ChangeContext);
							goto IL_026e;
						}
						IL_026e:
						iGH_DocumentObject = (IGH_DocumentObject)obj;
						num2 = 17;
						continue;
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
				case 1:
					connection = value;
					num2 = 0;
					if (!StopRequest())
					{
						num2 = 0;
					}
					break;
				case 2:
					return;
				default:
					_Importer = ((connection != null) ? SingletonAnnotation.GetUtils(connection, SingletonAnnotation.CalculateContext) : RuleAnnotation.GetUtils(RuleAnnotation.StartAnnotation));
					num2 = 2;
					if (ReadRequest() == null)
					{
						num2 = 2;
					}
					break;
				}
			}
		}
	}

	public List<pOd_TimeLineSlider> sliders
	{
		get
		{
			//Discarded unreachable code: IL_0002
			GH_Document utils = SpecificationOrder.GetUtils(this, SpecificationOrder.UpdateContext);
			List<pOd_TimeLineSlider> list = new List<pOd_TimeLineSlider>();
			for (int i = 0; i < m_Registry.Count; i++)
			{
				Guid guid = m_Registry[i];
				IGH_DocumentObject iGH_DocumentObject = ((utils != null) ? AccountOrder.GetUtils(utils, guid, true, AccountOrder.ChangeContext) : null);
				if (iGH_DocumentObject != null)
				{
					list.Add(iGH_DocumentObject as pOd_TimeLineSlider);
				}
			}
			return list;
		}
		set
		{
			//Discarded unreachable code: IL_0002
			_Ref = value;
			m_Registry.Clear();
			if (_Ref != null && _Ref.Count > 0)
			{
				for (int i = 0; i < _Ref.Count; i++)
				{
					m_Registry.Add((_Ref[i] != null) ? SingletonAnnotation.GetUtils(_Ref[i], SingletonAnnotation.CalculateContext) : RuleAnnotation.GetUtils(RuleAnnotation.StartAnnotation));
				}
			}
		}
	}

	public override Guid ComponentGuid
	{
		get
		{
			//Discarded unreachable code: IL_0002, IL_0037, IL_0046
			int num = 1;
			Guid result = default(Guid);
			while (true)
			{
				int num2 = num;
				do
				{
					switch (num2)
					{
					default:
						return result;
					case 1:
						break;
					}
					result = new Guid((string)PrepareRequest(9412));
					num2 = 0;
				}
				while (ReadRequest() == null);
			}
		}
	}

	public override GH_Exposure Exposure
	{
		get
		{
			//Discarded unreachable code: IL_0002, IL_0050, IL_005f
			int num = 2;
			GH_Exposure result = default(GH_Exposure);
			while (true)
			{
				int num2 = num;
				do
				{
					switch (num2)
					{
					case 2:
						break;
					default:
						return result;
					}
					result = GH_Exposure.secondary;
					num2 = 1;
				}
				while (ReadRequest() == null);
			}
		}
	}

	protected override Bitmap Internal_Icon_24x24
	{
		get
		{
			//Discarded unreachable code: IL_0002, IL_0037, IL_0046
			int num = 2;
			int num2 = num;
			Bitmap result = default(Bitmap);
			while (true)
			{
				switch (num2)
				{
				default:
					return result;
				case 2:
					result = (Bitmap)PublishRequest();
					num2 = 1;
					if (ReadRequest() == null)
					{
						num2 = 1;
					}
					break;
				}
			}
		}
	}

	public TimeLineSliderMax()
	{
		//Discarded unreachable code: IL_0002, IL_006a
		TokenizerClass.DisableDic();
		m_Registry = new List<Guid>();
		base._002Ector((string)PrepareRequest(8914), (string)PrepareRequest(8288), (string)PrepareRequest(8970), ProcessTaskContainer.VisitFactory(2482), (string)PrepareRequest(8424));
		int num = 0;
		if (false)
		{
			int num2;
			num = num2;
		}
		while (true)
		{
			switch (num)
			{
			case 1:
				return;
			}
			StructAnnotation.GetUtils(this, (string)PrepareRequest(9106), StructAnnotation.ForgotRules);
			num = 0;
			if (0 == 0)
			{
				num = 1;
			}
		}
	}

	public override void CreateAttributes()
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
				m_attributes = new TimeLineSliderMaxAttributes(this);
				num2 = 0;
				if (StopRequest())
				{
					num2 = 0;
				}
				break;
			case 0:
				return;
			}
		}
	}

	protected bool ButtonActivate()
	{
		//Discarded unreachable code: IL_0002, IL_0044, IL_0053, IL_00a7, IL_00bd
		int num = 1;
		bool flag = default(bool);
		TimeLineSliderMaxAttributes timeLineSliderMaxAttributes = default(TimeLineSliderMaxAttributes);
		bool result = default(bool);
		while (true)
		{
			int num2 = num;
			do
			{
				IL_0019:
				int num3;
				switch (num2)
				{
				case 4:
					if (!flag)
					{
						num2 = 6;
						if (ReadRequest() != null)
						{
							num2 = 0;
						}
						goto IL_0019;
					}
					goto case 3;
				case 1:
					goto IL_007f;
				case 3:
					num3 = (LoginRequest(timeLineSliderMaxAttributes) ? 1 : 0);
					break;
				case 2:
				case 5:
					return result;
				default:
					flag = timeLineSliderMaxAttributes != null;
					num2 = 4;
					if (ReadRequest() != null)
					{
						num2 = 2;
					}
					goto IL_0019;
				case 6:
					num3 = 0;
					break;
				}
				result = (byte)num3 != 0;
				num2 = 5;
				goto IL_0019;
				IL_007f:
				timeLineSliderMaxAttributes = m_attributes as TimeLineSliderMaxAttributes;
				num2 = 0;
			}
			while (StopRequest());
		}
	}

	protected override void RegisterInputParams(GH_InputParamManager pManager)
	{
	}//Discarded unreachable code: IL_0002


	protected override void RegisterOutputParams(GH_OutputParamManager pManager)
	{
		//Discarded unreachable code: IL_0002
		int num = 1;
		while (true)
		{
			int num2 = num;
			while (true)
			{
				switch (num2)
				{
				default:
					InterceptorProccesor.GetUtils(pManager, (string)PrepareRequest(8552), (string)PrepareRequest(8582), (string)PrepareRequest(8590), GH_ParamAccess.list, InterceptorProccesor.DeleteRules);
					num2 = 2;
					if (StopRequest())
					{
						continue;
					}
					break;
				case 1:
					InterceptorProccesor.GetUtils(pManager, (string)PrepareRequest(8490), (string)PrepareRequest(8522), (string)PrepareRequest(8530), GH_ParamAccess.item, InterceptorProccesor.DeleteRules);
					num2 = 0;
					if (StopRequest())
					{
						continue;
					}
					break;
				case 2:
					return;
				}
				break;
			}
		}
	}

	protected override void SolveInstance(IGH_DataAccess DA)
	{
		//Discarded unreachable code: IL_0002, IL_01d6, IL_0232, IL_0241, IL_0276, IL_0285, IL_0342, IL_0351, IL_03c6, IL_03d5, IL_0430, IL_043f, IL_04d9, IL_0568, IL_0577, IL_05bb, IL_05ca, IL_06ad, IL_06bc, IL_07e3, IL_08a8, IL_08b7, IL_08c4, IL_0a40, IL_0baf, IL_0cef
		int num = 9;
		bool flag15 = default(bool);
		bool flag3 = default(bool);
		GH_NumberSlider2Attributes gH_NumberSlider2Attributes4 = default(GH_NumberSlider2Attributes);
		int num6 = default(int);
		List<pOd_TimeLineSlider>.Enumerator enumerator = default(List<pOd_TimeLineSlider>.Enumerator);
		decimal num5 = default(decimal);
		pOd_TimeLineSlider current = default(pOd_TimeLineSlider);
		bool utils2 = default(bool);
		int num10 = default(int);
		GH_NumberSlider2Attributes gH_NumberSlider2Attributes2 = default(GH_NumberSlider2Attributes);
		GH_NumberSlider2Attributes gH_NumberSlider2Attributes3 = default(GH_NumberSlider2Attributes);
		int num3 = default(int);
		bool flag2 = default(bool);
		GH_NumberSlider2Attributes gH_NumberSlider2Attributes = default(GH_NumberSlider2Attributes);
		pOd_TimeLineSlider pOd_TimeLineSlider2 = default(pOd_TimeLineSlider);
		bool flag = default(bool);
		bool flag12 = default(bool);
		bool flag11 = default(bool);
		bool flag14 = default(bool);
		bool flag6 = default(bool);
		bool flag13 = default(bool);
		GH_Document utils = default(GH_Document);
		bool flag8 = default(bool);
		bool flag5 = default(bool);
		bool flag10 = default(bool);
		bool flag9 = default(bool);
		bool flag7 = default(bool);
		bool flag4 = default(bool);
		while (true)
		{
			int num2 = num;
			while (true)
			{
				int num4;
				switch (num2)
				{
				case 11:
					WatcherOrder.GetUtils(MethodOrder.GetUtils(control, MethodOrder.ExcludeContext), 0m, WatcherOrder.PrintContext);
					num2 = 7;
					continue;
				case 12:
					flag15 = ButtonActivate();
					num = 36;
					break;
				case 57:
					num4 = ((ReflectRequest(sliders) > 0) ? 1 : 0);
					goto IL_0e36;
				case 28:
					sliders.Remove(control);
					num2 = 19;
					continue;
				case 14:
				case 34:
					DatabaseOrder.GetUtils((string)PrepareRequest(9178), DatabaseOrder.ValidateVisitor);
					num = 69;
					break;
				case 83:
					if (!flag3)
					{
						num2 = 86;
						continue;
					}
					goto case 80;
				default:
					gH_NumberSlider2Attributes4 = (GH_NumberSlider2Attributes)CustomerOrder.GetUtils(sliders[num6], CustomerOrder.PrepareContext);
					num2 = 38;
					continue;
				case 70:
					try
					{
						while (true)
						{
							IL_0327:
							int num7;
							if (!enumerator.MoveNext())
							{
								num7 = 3;
								goto IL_0294;
							}
							goto IL_0302;
							IL_0294:
							while (true)
							{
								switch (num7)
								{
								default:
									num5 = SingletonOrder.GetUtils(MethodOrder.GetUtils(current, MethodOrder.ExcludeContext), SingletonOrder.CollectContext);
									num7 = 6;
									if (ReadRequest() != null)
									{
										num7 = 1;
									}
									continue;
								case 5:
									if (!utils2)
									{
										int num8 = 2;
										num7 = num8;
										continue;
									}
									goto default;
								case 4:
									break;
								case 2:
								case 6:
									goto IL_0327;
								case 1:
									utils2 = AttributeOrder.GetUtils(num5, SingletonOrder.GetUtils(MethodOrder.GetUtils(current, MethodOrder.ExcludeContext), SingletonOrder.CollectContext), AttributeOrder.CreateVisitor);
									num7 = 5;
									continue;
								case 3:
									goto end_IL_0327;
								}
								break;
							}
							goto IL_0302;
							IL_0302:
							current = enumerator.Current;
							num7 = 0;
							if (ReadRequest() == null)
							{
								num7 = 1;
							}
							goto IL_0294;
							continue;
							end_IL_0327:
							break;
						}
					}
					finally
					{
						((IDisposable)enumerator).Dispose();
						int num9 = 0;
						if (!StopRequest())
						{
							num9 = num10;
						}
						switch (num9)
						{
						case 0:
							break;
						}
					}
					goto case 62;
				case 32:
					FacadeOrder.GetUtils(CustomerOrder.GetUtils(control, CustomerOrder.PrepareContext), new RectangleF(0f, 0f, ReponseOrder.GetUtils(num5, ReponseOrder.MapVisitor), 20f), FacadeOrder.ForgotContext);
					num2 = 2;
					if (StopRequest())
					{
						continue;
					}
					break;
				case 61:
					gH_NumberSlider2Attributes2.toggleMove = false;
					num2 = 33;
					continue;
				case 58:
					gH_NumberSlider2Attributes3 = (GH_NumberSlider2Attributes)CustomerOrder.GetUtils(sliders[num3], CustomerOrder.PrepareContext);
					num = 46;
					break;
				case 1:
				case 69:
				case 84:
					if (control == null)
					{
						num2 = 16;
						if (ReadRequest() == null)
						{
							continue;
						}
						break;
					}
					goto case 56;
				case 67:
					ConfigOrder.GetUtils(MethodOrder.GetUtils(control, MethodOrder.ExcludeContext), GH_SliderAccuracy.Float, ConfigOrder.ReadContext);
					num2 = 77;
					continue;
				case 77:
					DefinitionOrder.GetUtils(CustomerOrder.GetUtils(control, CustomerOrder.PrepareContext), new PointF(0f, 200f), DefinitionOrder.LoginContext);
					num2 = 32;
					if (!StopRequest())
					{
						num2 = 17;
					}
					continue;
				case 76:
					MethodOrder.GetUtils(control, MethodOrder.ExcludeContext).ValueChanged -= ValueChange;
					num2 = 11;
					if (ReadRequest() == null)
					{
						num2 = 25;
					}
					continue;
				case 60:
					InterpreterOrder.GetUtils(MethodOrder.GetUtils(control, MethodOrder.ExcludeContext), GH_SliderGripDisplay.ShapeAndText, InterpreterOrder.ResolveContext);
					num2 = 30;
					if (ReadRequest() == null)
					{
						continue;
					}
					break;
				case 52:
					flag3 = ReflectRequest(sliders) != 0;
					num2 = 83;
					if (ReadRequest() == null)
					{
						continue;
					}
					break;
				case 82:
					InvocationOrder.GetUtils(DA, 1, sliders, InvocationOrder.AssetContext);
					num2 = 31;
					continue;
				case 9:
					flag2 = ForgotRequest();
					num2 = 8;
					if (ReadRequest() == null)
					{
						continue;
					}
					break;
				case 27:
					IteratorProccesor.GetUtils(this, GH_RuntimeMessageLevel.Remark, (string)AddRequest(), IteratorProccesor.PublishTokenizer);
					num2 = 37;
					continue;
				case 10:
					gH_NumberSlider2Attributes = (GH_NumberSlider2Attributes)CustomerOrder.GetUtils(control, CustomerOrder.PrepareContext);
					num2 = 23;
					continue;
				case 47:
					StructAnnotation.GetUtils(pOd_TimeLineSlider2, (string)PrepareRequest(9144), StructAnnotation.SelectContext);
					num2 = 53;
					continue;
				case 4:
					if (!flag)
					{
						num2 = 29;
						continue;
					}
					goto case 26;
				case 5:
					DefineRequest(sliders);
					num2 = 72;
					continue;
				case 20:
					if (!flag12)
					{
						num2 = 45;
						continue;
					}
					goto case 76;
				case 33:
				case 41:
					EventOrder.GetUtils(DA, 0, control, EventOrder.AddRules);
					num2 = 82;
					if (ReadRequest() == null)
					{
						continue;
					}
					break;
				case 13:
				case 35:
					flag11 = ReflectRequest(sliders) != 0;
					num2 = 42;
					continue;
				case 38:
					gH_NumberSlider2Attributes4.toggleMove = false;
					num2 = 79;
					if (StopRequest())
					{
						continue;
					}
					break;
				case 45:
				case 54:
					flag14 = RateRequest(m_attributes as TimeLineSliderMaxAttributes);
					num2 = 6;
					continue;
				case 78:
				case 86:
					flag6 = control != null;
					num = 39;
					break;
				case 36:
					if (!flag15)
					{
						num2 = 1;
						if (ReadRequest() == null)
						{
							continue;
						}
						break;
					}
					goto case 5;
				case 29:
				case 73:
					ServiceAnnotation.GetUtils(MethodOrder.GetUtils(control, MethodOrder.ExcludeContext), 4, ServiceAnnotation.CloneContext);
					num2 = 11;
					continue;
				case 56:
					if (sliders != null)
					{
						num = 57;
						break;
					}
					goto case 16;
				case 74:
				{
					IGH_Attributes utils3 = CustomerOrder.GetUtils(control, CustomerOrder.PrepareContext);
					PointF utils4 = MessageProccesor.GetUtils(CustomerOrder.GetUtils(control, CustomerOrder.PrepareContext), MessageProccesor.ReflectContext);
					DefinitionOrder.GetUtils(utils3, new PointF(0f, StrategyProccesor.GetUtils(ref utils4, StrategyProccesor.AssetRules)), DefinitionOrder.LoginContext);
					num2 = 59;
					if (ReadRequest() == null)
					{
						continue;
					}
					break;
				}
				case 6:
					if (!flag14)
					{
						num2 = 35;
						if (ReadRequest() != null)
						{
							num2 = 10;
						}
						continue;
					}
					goto case 52;
				case 51:
					pOd_TimeLineSlider2 = new pOd_TimeLineSlider();
					num2 = 9;
					if (ReadRequest() == null)
					{
						num2 = 47;
					}
					continue;
				case 8:
					if (flag2)
					{
						num2 = 27;
						continue;
					}
					goto case 37;
				case 21:
					TestsAnnotation.GetUtils(CustomerOrder.GetUtils(control, CustomerOrder.PrepareContext), TestsAnnotation.DefineContext);
					num2 = 74;
					continue;
				case 79:
					num6++;
					num2 = 17;
					continue;
				case 75:
					flag13 = sliders.Contains(control);
					num2 = 15;
					if (ReadRequest() != null)
					{
						num2 = 2;
					}
					continue;
				case 53:
					ExpressionOrder.GetUtils(utils, pOd_TimeLineSlider2, false, int.MaxValue, ExpressionOrder.ManageContext);
					num2 = 43;
					continue;
				case 18:
					flag = AccountOrder.GetUtils(utils, SingletonAnnotation.GetUtils(control, SingletonAnnotation.CalculateContext), true, AccountOrder.DeleteContext) == null;
					num2 = 4;
					if (ReadRequest() == null)
					{
						continue;
					}
					break;
				case 66:
				case 81:
					flag8 = control != null;
					num2 = 63;
					if (StopRequest())
					{
						continue;
					}
					break;
				case 24:
					num3 = 0;
					num2 = 85;
					continue;
				case 15:
					if (flag13)
					{
						num = 28;
						break;
					}
					goto case 19;
				case 3:
					if (!flag5)
					{
						goto case 1;
					}
					num2 = 18;
					continue;
				case 7:
					WatcherOrder.GetUtils(MethodOrder.GetUtils(control, MethodOrder.ExcludeContext), num5, WatcherOrder.StopContext);
					num2 = 67;
					if (!StopRequest())
					{
						num2 = 55;
					}
					continue;
				case 17:
				case 71:
					flag10 = num6 < ReflectRequest(sliders);
					num2 = 68;
					if (StopRequest())
					{
						continue;
					}
					break;
				case 50:
					if (!flag9)
					{
						num2 = 2;
						if (StopRequest())
						{
							num2 = 14;
						}
						continue;
					}
					goto case 65;
				case 59:
					TestsAnnotation.GetUtils(CustomerOrder.GetUtils(control, CustomerOrder.PrepareContext), TestsAnnotation.AddContext);
					num2 = 44;
					continue;
				case 42:
					if (!flag11)
					{
						num2 = 81;
						continue;
					}
					goto case 24;
				case 80:
					num6 = 0;
					num2 = 71;
					if (ReadRequest() != null)
					{
						num2 = 56;
					}
					continue;
				case 26:
					ExpressionOrder.GetUtils(utils, control, false, int.MaxValue, ExpressionOrder.ManageContext);
					num2 = 73;
					if (StopRequest())
					{
						continue;
					}
					break;
				case 85:
				case 87:
					flag7 = num3 < sliders.Count;
					num2 = 48;
					if (!StopRequest())
					{
						num2 = 18;
					}
					continue;
				case 44:
					TestsAnnotation.GetUtils(CustomerOrder.GetUtils(control, CustomerOrder.PrepareContext), TestsAnnotation.DefineContext);
					num = 84;
					break;
				case 64:
					num3++;
					num2 = 87;
					continue;
				case 62:
					flag4 = control == null;
					num2 = 22;
					continue;
				case 68:
					if (flag10)
					{
						num2 = 49;
						continue;
					}
					goto case 78;
				case 31:
					return;
				case 25:
					MethodOrder.GetUtils(control, MethodOrder.ExcludeContext).ValueChanged += ValueChange;
					num2 = 54;
					continue;
				case 19:
					enumerator = sliders.GetEnumerator();
					num2 = 70;
					if (ReadRequest() == null)
					{
						continue;
					}
					break;
				case 37:
					utils = SpecificationOrder.GetUtils(this, SpecificationOrder.UpdateContext);
					num2 = 12;
					if (ReadRequest() != null)
					{
						num2 = 5;
					}
					continue;
				case 40:
					flag9 = ReflectRequest(sliders) != 0;
					num2 = 50;
					continue;
				case 63:
					if (flag8)
					{
						num2 = 10;
						if (StopRequest())
						{
							continue;
						}
						break;
					}
					goto case 33;
				case 23:
					gH_NumberSlider2Attributes.toggleMove = true;
					num2 = 41;
					continue;
				case 48:
					if (!flag7)
					{
						num2 = 66;
						continue;
					}
					goto case 58;
				case 39:
					if (!flag6)
					{
						goto case 33;
					}
					num2 = 55;
					if (ReadRequest() == null)
					{
						continue;
					}
					break;
				case 65:
					num5 = default(decimal);
					num2 = 75;
					continue;
				case 43:
					control = pOd_TimeLineSlider2;
					num2 = 60;
					if (StopRequest())
					{
						continue;
					}
					break;
				case 22:
					if (flag4)
					{
						num2 = 51;
						if (ReadRequest() == null)
						{
							continue;
						}
						break;
					}
					goto case 30;
				case 46:
					gH_NumberSlider2Attributes3.toggleMove = true;
					num2 = 7;
					if (ReadRequest() == null)
					{
						num2 = 64;
					}
					continue;
				case 30:
					flag5 = control != null;
					num2 = 3;
					if (StopRequest())
					{
						continue;
					}
					break;
				case 2:
					TestsAnnotation.GetUtils(CustomerOrder.GetUtils(control, CustomerOrder.PrepareContext), TestsAnnotation.AddContext);
					num2 = 21;
					continue;
				case 55:
					gH_NumberSlider2Attributes2 = (GH_NumberSlider2Attributes)CustomerOrder.GetUtils(control, CustomerOrder.PrepareContext);
					num2 = 61;
					if (StopRequest())
					{
						continue;
					}
					break;
				case 72:
					sliders = (from x in (from x in MapperOrder.GetUtils(utils, MapperOrder.SearchContext)
							where x is pOd_TimeLineSlider
							select x).OrderBy(delegate(IGH_DocumentObject x)
						{
							//Discarded unreachable code: IL_0002
							int num13 = 1;
							PointF utils6 = default(PointF);
							while (true)
							{
								int num14 = num13;
								do
								{
									switch (num14)
									{
									default:
										return (int)StrategyProccesor.GetUtils(ref utils6, StrategyProccesor.AssetRules);
									case 1:
										break;
									}
									utils6 = MessageProccesor.GetUtils(CustomerOrder.GetUtils(x, CustomerOrder.OrderVisitor), MessageProccesor.ReflectContext);
									num14 = 0;
								}
								while (_003C_003Ec.SetMethod());
							}
						}).ThenBy(delegate(IGH_DocumentObject x)
						{
							//Discarded unreachable code: IL_0002
							int num11 = 1;
							PointF utils5 = default(PointF);
							while (true)
							{
								int num12 = num11;
								do
								{
									switch (num12)
									{
									case 1:
										break;
									default:
										return (int)StrategyProccesor.GetUtils(ref utils5, StrategyProccesor.AssetRules);
									}
									utils5 = MessageProccesor.GetUtils(CustomerOrder.GetUtils(x, CustomerOrder.OrderVisitor), MessageProccesor.ReflectContext);
									num12 = 0;
								}
								while (_003C_003Ec.SetMethod());
							}
						})
						select x as pOd_TimeLineSlider).ToList();
					num2 = 40;
					continue;
				case 16:
					{
						num4 = 0;
						goto IL_0e36;
					}
					IL_0e36:
					flag12 = (byte)num4 != 0;
					num2 = 20;
					continue;
				}
				break;
			}
		}
	}

	public void ValueChange(object sender, GH_SliderEventArgs args)
	{
		//Discarded unreachable code: IL_0002, IL_0039, IL_0180, IL_0220, IL_0244, IL_0256, IL_0265, IL_02e1, IL_035e, IL_03a3, IL_03b2
		int num = 1;
		List<pOd_TimeLineSlider>.Enumerator enumerator = default(List<pOd_TimeLineSlider>.Enumerator);
		bool utils5 = default(bool);
		pOd_TimeLineSlider current = default(pOd_TimeLineSlider);
		int num4 = default(int);
		bool utils4 = default(bool);
		decimal utils2 = default(decimal);
		bool utils3 = default(bool);
		bool utils = default(bool);
		int num6 = default(int);
		while (true)
		{
			int num2 = num;
			do
			{
				IL_0019:
				switch (num2)
				{
				case 3:
					try
					{
						while (true)
						{
							IL_02ee:
							int num3;
							if (!enumerator.MoveNext())
							{
								num3 = 11;
								goto IL_004b;
							}
							goto IL_026b;
							IL_004b:
							while (true)
							{
								switch (num3)
								{
								case 10:
									utils5 = AttributeOrder.GetUtils(SingletonOrder.GetUtils(MethodOrder.GetUtils(current, MethodOrder.ExcludeContext), SingletonOrder.RateContext), SingletonOrder.GetUtils(MethodOrder.GetUtils(current, MethodOrder.ExcludeContext), SingletonOrder.CollectContext), AttributeOrder.PatchVisitor);
									num4 = 4;
									goto IL_0047;
								case 9:
									WatcherOrder.GetUtils(MethodOrder.GetUtils(current, MethodOrder.ExcludeContext), SingletonOrder.GetUtils(MethodOrder.GetUtils(current, MethodOrder.ExcludeContext), SingletonOrder.RestartContext), WatcherOrder.CancelContext);
									num4 = 7;
									goto IL_0047;
								case 12:
								case 19:
									utils4 = AttributeOrder.GetUtils(utils2, 0m, AttributeOrder.PublishContext);
									num3 = 17;
									continue;
								case 2:
								case 6:
									WatcherOrder.GetUtils(MethodOrder.GetUtils(current, MethodOrder.ExcludeContext), utils2, WatcherOrder.CancelContext);
									num3 = 20;
									if (ReadRequest() != null)
									{
										num3 = 20;
									}
									continue;
								case 16:
									WatcherOrder.GetUtils(MethodOrder.GetUtils(current, MethodOrder.ExcludeContext), SingletonOrder.GetUtils(MethodOrder.GetUtils(current, MethodOrder.ExcludeContext), SingletonOrder.CollectContext), WatcherOrder.CancelContext);
									num3 = 1;
									if (StopRequest())
									{
										continue;
									}
									goto IL_0047;
								case 4:
									if (!utils5)
									{
										num3 = 8;
										continue;
									}
									goto case 16;
								case 13:
									utils3 = AttributeOrder.GetUtils(utils2, SingletonOrder.GetUtils(MethodOrder.GetUtils(control, MethodOrder.ExcludeContext), SingletonOrder.CollectContext), AttributeOrder.AwakeContext);
									num3 = 5;
									if (ReadRequest() == null)
									{
										continue;
									}
									goto IL_0047;
								case 17:
									if (!utils4)
									{
										num4 = 6;
										goto IL_0047;
									}
									goto case 18;
								case 14:
									break;
								case 18:
									utils = AttributeOrder.GetUtils(SingletonOrder.GetUtils(MethodOrder.GetUtils(current, MethodOrder.ExcludeContext), SingletonOrder.RateContext), SingletonOrder.GetUtils(MethodOrder.GetUtils(current, MethodOrder.ExcludeContext), SingletonOrder.RestartContext), AttributeOrder.PatchVisitor);
									num3 = 3;
									if (StopRequest())
									{
										continue;
									}
									goto IL_0047;
								default:
									goto IL_02ee;
								case 5:
									if (!utils3)
									{
										num3 = 12;
										if (StopRequest())
										{
											continue;
										}
										goto IL_0047;
									}
									goto case 10;
								case 15:
									TestIfLockedSlider(current, utils2);
									num3 = 13;
									continue;
								case 3:
									if (!utils)
									{
										num3 = 0;
										if (ReadRequest() != null)
										{
											num3 = 0;
										}
										continue;
									}
									goto case 9;
								case 11:
									return;
									IL_0047:
									num3 = num4;
									continue;
								}
								break;
							}
							goto IL_026b;
							IL_026b:
							current = enumerator.Current;
							num3 = 3;
							if (StopRequest())
							{
								num3 = 15;
							}
							goto IL_004b;
						}
					}
					finally
					{
						((IDisposable)enumerator).Dispose();
						int num5 = 0;
						if (ReadRequest() != null)
						{
							num5 = num6;
						}
						switch (num5)
						{
						case 0:
							break;
						}
					}
				case 1:
					break;
				case 2:
					return;
				default:
					enumerator = sliders.GetEnumerator();
					num2 = 3;
					goto IL_0019;
				}
				utils2 = SingletonOrder.GetUtils(MethodOrder.GetUtils(control, MethodOrder.ExcludeContext), SingletonOrder.RateContext);
				num2 = 0;
			}
			while (ReadRequest() == null);
		}
	}

	public void TestIfLockedSlider(GH_NumberSlider item, decimal currentValue)
	{
		//Discarded unreachable code: IL_0002, IL_004e, IL_005d, IL_00e4, IL_0145, IL_0154
		int num = 1;
		bool flag = default(bool);
		while (true)
		{
			int num2 = num;
			while (true)
			{
				int num3;
				switch (num2)
				{
				case 1:
					if (!AttributeOrder.GetUtils(currentValue, SingletonOrder.GetUtils(MethodOrder.GetUtils(item, MethodOrder.ExcludeContext), SingletonOrder.RestartContext), AttributeOrder.CreateVisitor))
					{
						num2 = 0;
						if (ReadRequest() != null)
						{
							num2 = 0;
						}
						continue;
					}
					num3 = 1;
					goto IL_0160;
				case 6:
					if (!flag)
					{
						num2 = 2;
						if (StopRequest())
						{
							continue;
						}
						break;
					}
					goto case 8;
				default:
					num3 = (AttributeOrder.GetUtils(currentValue, SingletonOrder.GetUtils(MethodOrder.GetUtils(item, MethodOrder.ExcludeContext), SingletonOrder.CollectContext), AttributeOrder.EnableVisitor) ? 1 : 0);
					goto IL_0160;
				case 8:
					SetterProccesor.GetUtils(item, true, SetterProccesor.SetupVisitor);
					num = 7;
					break;
				case 5:
				case 7:
					SetterProccesor.GetUtils(item, false, SetterProccesor.ListContext);
					num2 = 3;
					if (ReadRequest() == null)
					{
						continue;
					}
					break;
				case 2:
				case 4:
					SetterProccesor.GetUtils(item, false, SetterProccesor.SetupVisitor);
					num2 = 5;
					continue;
				case 3:
					return;
					IL_0160:
					flag = (byte)num3 != 0;
					num2 = 6;
					continue;
				}
				break;
			}
		}
	}

	public override bool Write(GH_IWriter writer)
	{
		//Discarded unreachable code: IL_0002, IL_0074, IL_0083, IL_00a4, IL_00df, IL_00ee
		int num = 11;
		bool flag = default(bool);
		bool flag2 = default(bool);
		bool flag3 = default(bool);
		int num3 = default(int);
		bool result = default(bool);
		bool utils2 = default(bool);
		GH_IWriter utils = default(GH_IWriter);
		while (true)
		{
			int num2 = num;
			while (true)
			{
				switch (num2)
				{
				case 5:
					if (!flag)
					{
						num2 = 7;
						continue;
					}
					goto case 16;
				case 18:
					if (!flag2)
					{
						num = 6;
						break;
					}
					goto case 13;
				case 7:
				case 15:
					flag3 = m_Registry != null;
					num2 = 14;
					continue;
				case 14:
					if (flag3)
					{
						num2 = 8;
						continue;
					}
					goto case 6;
				case 8:
					num3 = 0;
					num2 = 17;
					continue;
				case 6:
					result = utils2;
					num2 = 3;
					continue;
				case 12:
					ClientOrder.GetUtils(utils, ProcessTaskContainer.VisitFactory(9272), AwakeRequest(m_Registry), ClientOrder.ConcatContext);
					num2 = 4;
					if (StopRequest())
					{
						continue;
					}
					break;
				case 11:
					utils2 = GlobalOrder.GetUtils(this, writer, GlobalOrder.CheckContext);
					num2 = 10;
					continue;
				case 1:
					flag = true;
					num2 = 5;
					continue;
				case 10:
					utils = ParserOrder.GetUtils(writer, (string)PrepareRequest(9234), ParserOrder.MoveContext);
					num2 = 12;
					continue;
				case 4:
					_ = _Importer;
					num2 = 1;
					if (!StopRequest())
					{
						num2 = 0;
					}
					continue;
				case 2:
				case 17:
					flag2 = num3 < AwakeRequest(m_Registry);
					num2 = 18;
					continue;
				default:
					return result;
				case 13:
					PoolOrder.GetUtils(utils, (string)PrepareRequest(9364), num3, m_Registry[num3], PoolOrder.VerifyContext);
					num2 = 2;
					if (ReadRequest() == null)
					{
						num2 = 9;
					}
					continue;
				case 9:
					num3 = checked(num3 + 1);
					num2 = 2;
					if (!StopRequest())
					{
						num2 = 2;
					}
					continue;
				case 16:
					DescriptorOrder.GetUtils(utils, ProcessTaskContainer.VisitFactory(9322), _Importer, DescriptorOrder.ResetContext);
					num2 = 15;
					continue;
				}
				break;
			}
		}
	}

	public override bool Read(GH_IReader reader)
	{
		//Discarded unreachable code: IL_0002, IL_00af, IL_00be, IL_0157, IL_0166, IL_0227
		int num = 2;
		bool utils4 = default(bool);
		bool flag2 = default(bool);
		GH_IReader utils = default(GH_IReader);
		bool result = default(bool);
		bool flag = default(bool);
		int utils2 = default(int);
		bool utils3 = default(bool);
		int num3 = default(int);
		while (true)
		{
			int num2 = num;
			while (true)
			{
				switch (num2)
				{
				case 1:
					CancelRequest(m_Registry);
					num2 = 10;
					continue;
				case 3:
					if (utils4)
					{
						num2 = 7;
						continue;
					}
					goto case 16;
				case 16:
					flag2 = utils != null;
					num2 = 14;
					continue;
				case 9:
				case 18:
					return result;
				case 4:
					if (!flag)
					{
						num2 = 13;
						if (StopRequest())
						{
							continue;
						}
						break;
					}
					goto default;
				case 12:
					utils2 = IdentifierOrder.GetUtils(utils, (string)PrepareRequest(9272), IdentifierOrder.ViewVisitor);
					num2 = 15;
					if (ReadRequest() == null)
					{
						continue;
					}
					break;
				case 11:
					utils4 = AuthenticationAnnotation.GetUtils(utils, (string)PrepareRequest(9322), AuthenticationAnnotation.PostVisitor);
					num2 = 3;
					continue;
				case 14:
					if (!flag2)
					{
						num2 = 5;
						if (ReadRequest() == null)
						{
							num2 = 6;
						}
						continue;
					}
					goto case 12;
				case 10:
					utils = ParamOrder.GetUtils(reader, (string)PrepareRequest(9234), ParamOrder.RevertContext);
					num2 = 11;
					continue;
				case 6:
				case 13:
					result = utils3;
					num2 = 9;
					if (ReadRequest() == null)
					{
						continue;
					}
					break;
				default:
					m_Registry.Add(SystemOrder.GetUtils(utils, (string)PrepareRequest(9364), num3, SystemOrder.AssetVisitor));
					num2 = 17;
					if (ReadRequest() != null)
					{
						num2 = 13;
					}
					continue;
				case 2:
					utils3 = ValOrder.GetUtils(this, reader, ValOrder.RegisterContext);
					num2 = 1;
					if (ReadRequest() != null)
					{
						num2 = 1;
					}
					continue;
				case 5:
				case 8:
					flag = num3 < utils2;
					num2 = 4;
					continue;
				case 7:
					_Importer = RuleOrder.GetUtils(utils, (string)PrepareRequest(9322), RuleOrder.NewVisitor);
					num = 16;
					break;
				case 17:
					num3 = checked(num3 + 1);
					num2 = 5;
					continue;
				case 15:
					num3 = 0;
					num2 = 8;
					continue;
				}
				break;
			}
		}
	}

	static TimeLineSliderMax()
	{
		//Discarded unreachable code: IL_0002
		ProcessTaskContainer.StartFactory();
		ChangeRequest();
	}

	internal static object PrepareRequest(int previousparam)
	{
		//Discarded unreachable code: IL_0002
		return ProcessTaskContainer.VisitFactory(previousparam);
	}

	internal static bool StopRequest()
	{
		//Discarded unreachable code: IL_0002
		return PrintRequest == null;
	}

	internal static TimeLineSliderMax ReadRequest()
	{
		//Discarded unreachable code: IL_0002
		return PrintRequest;
	}

	internal static bool LoginRequest(object P_0)
	{
		//Discarded unreachable code: IL_0002
		return ((TimeLineSliderMaxAttributes)P_0).Activate;
	}

	internal static bool ForgotRequest()
	{
		//Discarded unreachable code: IL_0002
		return Astrict.ShowWarningMessage();
	}

	internal static object AddRequest()
	{
		//Discarded unreachable code: IL_0002
		return Astrict.Warning;
	}

	internal static void DefineRequest(object P_0)
	{
		//Discarded unreachable code: IL_0002
		((List<pOd_TimeLineSlider>)P_0).Clear();
	}

	internal static int ReflectRequest(object P_0)
	{
		//Discarded unreachable code: IL_0002
		return ((List<pOd_TimeLineSlider>)P_0).Count;
	}

	internal static bool RateRequest(object P_0)
	{
		//Discarded unreachable code: IL_0002
		return ((TimeLineSliderMaxAttributes)P_0).LockAct;
	}

	internal static int AwakeRequest(object P_0)
	{
		//Discarded unreachable code: IL_0002
		return ((List<Guid>)P_0).Count;
	}

	internal static void CancelRequest(object P_0)
	{
		//Discarded unreachable code: IL_0002
		((List<Guid>)P_0).Clear();
	}

	internal static object PublishRequest()
	{
		//Discarded unreachable code: IL_0002
		return pOd_Res.pOd_TimelineSlider_Union_;
	}

	internal static void ChangeRequest()
	{
		//Discarded unreachable code: IL_0002
		ObserverAuthenticationClass.kLjw4iIsCLsZtxc4lksN0j();
	}
}
