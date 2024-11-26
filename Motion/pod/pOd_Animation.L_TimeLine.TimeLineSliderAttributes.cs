// 警告：某些程序集引用无法自动解析。这可能会导致某些部分反编译错误，
// 例如属性 getter/setter 访问。要获得最佳反编译结果，请手动将缺少的引用添加到加载的程序集列表中。
// pOd_Animation.L_TimeLine.TimeLineSliderAttributes
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using pOd_Animation.L_TimeLine;
using pOd_GH_Animation.Classes;
using pOd_GH_Animation.Containers;
using pOd_GH_Animation.Tasks;

public class TimeLineSliderAttributes : GH_ComponentAttributes
{
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	[CompilerGenerated]
	private bool page;

	[CompilerGenerated]
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private RectangleF m_Adapter;

	private static TimeLineSliderAttributes ListRequest;

	public bool Activate
	{
		[CompilerGenerated]
		get
		{
			//Discarded unreachable code: IL_0002
			return page;
		}
		[CompilerGenerated]
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
					page = value;
					num2 = 0;
				}
				while (FlushRequest() == null);
			}
		}
	}

	public TimeLineSliderAttributes(TimeLineSlider owner)
	{
		//Discarded unreachable code: IL_0002, IL_002e
		InterruptRequest();
		base._002Ector(owner);
		int num = 0;
		if (true)
		{
			num = 1;
		}
		while (true)
		{
			switch (num)
			{
			default:
				return;
			case 0:
				return;
			case 1:
				Activate = false;
				num = 0;
				if (false)
				{
					int num2;
					num = num2;
				}
				break;
			}
		}
	}

	protected override void Layout()
	{
		//Discarded unreachable code: IL_0002
		int num = 5;
		Rectangle utils = default(Rectangle);
		Rectangle rectangle = default(Rectangle);
		while (true)
		{
			int num2 = num;
			while (true)
			{
				switch (num2)
				{
				case 6:
					SearchRequest(this, ProcessProccesor.GetUtils(utils, ProcessProccesor.PostRules));
					num2 = 3;
					continue;
				case 8:
					rectangle = utils;
					num2 = 0;
					if (WriteRequest())
					{
						num2 = 2;
					}
					continue;
				case 2:
					ParamsOrder.GetUtils(ref rectangle, ProducerProccesor.GetUtils(ref rectangle, ProducerProccesor.PushRules) - 22, ParamsOrder.IncludeVisitor);
					num2 = 0;
					if (FlushRequest() == null)
					{
						num2 = 0;
					}
					continue;
				case 7:
					SchemaProccesor.GetUtils(ref rectangle, -2, -2, SchemaProccesor.TestRules);
					num2 = 6;
					if (WriteRequest())
					{
						continue;
					}
					break;
				case 3:
					PostWrapper(ProcessProccesor.GetUtils(rectangle, ProcessProccesor.PostRules));
					num = 9;
					break;
				case 9:
					return;
				case 5:
					TestsAnnotation.GetUtils(this, TestsAnnotation.VisitVisitor);
					num2 = 4;
					continue;
				default:
					ParamsOrder.GetUtils(ref rectangle, 22, ParamsOrder.CustomizeVisitor);
					num2 = 7;
					continue;
				case 4:
					utils = CollectionProccesor.GetUtils(UpdateRequest(this), CollectionProccesor.RevertTokenizer);
					num2 = 1;
					if (WriteRequest())
					{
						continue;
					}
					break;
				case 1:
					ParamsOrder.GetUtils(ref utils, ProducerProccesor.GetUtils(ref utils, ProducerProccesor.PopVisitor) + 22, ParamsOrder.CustomizeVisitor);
					num2 = 8;
					continue;
				}
				break;
			}
		}
	}

	[SpecialName]
	[CompilerGenerated]
	private RectangleF ResetWrapper()
	{
		//Discarded unreachable code: IL_0002
		return m_Adapter;
	}

	[SpecialName]
	[CompilerGenerated]
	private void PostWrapper(RectangleF value)
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
				m_Adapter = value;
				num2 = 0;
			}
			while (FlushRequest() == null);
		}
	}

	protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
	{
		//Discarded unreachable code: IL_0002, IL_0378, IL_03a8, IL_03b7, IL_0543, IL_0975, IL_09a6, IL_09f2, IL_0a9b
		int num = 61;
		_003C_003Ec__DisplayClass10_0 _003C_003Ec__DisplayClass10_ = default(_003C_003Ec__DisplayClass10_0);
		List<IGH_DocumentObject> list = default(List<IGH_DocumentObject>);
		TimeLineSlider timeLineSlider = default(TimeLineSlider);
		bool flag9 = default(bool);
		RectangleF utils3 = default(RectangleF);
		PointF[] array2 = default(PointF[]);
		RectangleF utils = default(RectangleF);
		bool flag10 = default(bool);
		IGH_DocumentObject utils4 = default(IGH_DocumentObject);
		Pen pen4 = default(Pen);
		IGH_DocumentObject iGH_DocumentObject = default(IGH_DocumentObject);
		int num4 = default(int);
		Pen pen3 = default(Pen);
		PointF[] array3 = default(PointF[]);
		bool flag7 = default(bool);
		RectangleF rectangleF2 = default(RectangleF);
		Pen pen2 = default(Pen);
		RectangleF rectangleF = default(RectangleF);
		bool flag = default(bool);
		SolidBrush solidBrush = default(SolidBrush);
		GH_Capsule utils2 = default(GH_Capsule);
		float num3 = default(float);
		StringFormat stringFormat = default(StringFormat);
		bool flag8 = default(bool);
		PointF[] array = default(PointF[]);
		bool flag2 = default(bool);
		bool flag4 = default(bool);
		bool flag5 = default(bool);
		bool flag3 = default(bool);
		bool flag6 = default(bool);
		Pen pen = default(Pen);
		while (true)
		{
			int num2 = num;
			while (true)
			{
				int num5;
				switch (num2)
				{
				case 61:
					_003C_003Ec__DisplayClass10_ = new _003C_003Ec__DisplayClass10_0();
					num2 = 60;
					continue;
				case 28:
					list = timeLineSlider.sliders.Where(_003C_003Ec__DisplayClass10_.CallClass).Select(_003C_003Ec__DisplayClass10_.MoveClass).ToList();
					num2 = 27;
					continue;
				case 5:
					if (!flag9)
					{
						num2 = 36;
						if (WriteRequest())
						{
							num2 = 59;
						}
						continue;
					}
					goto case 36;
				case 43:
					WorkerProccesor.GetUtils(ref utils3, 4f, 4f, WorkerProccesor.NewRules);
					num2 = 53;
					continue;
				case 49:
					array2[0] = new PointF(ManagerProccesor.GetUtils(ref utils, ManagerProccesor.PushVisitor), ManagerProccesor.GetUtils(ref utils, ManagerProccesor.InvokeVisitor) + ManagerProccesor.GetUtils(ref utils, ManagerProccesor.ResetTokenizer) / 2f);
					num2 = 39;
					if (!WriteRequest())
					{
						num2 = 36;
					}
					continue;
				case 10:
					flag10 = utils4 != null;
					num2 = 51;
					if (WriteRequest())
					{
						continue;
					}
					break;
				case 38:
					pen4 = new Pen(TaskProccesor.GetUtils(TaskProccesor.CalcVisitor), 3f);
					num2 = 7;
					if (WriteRequest())
					{
						num2 = 13;
					}
					continue;
				case 17:
					timeLineSlider = base.Owner as TimeLineSlider;
					num2 = 27;
					if (WriteRequest())
					{
						num2 = 32;
					}
					continue;
				case 46:
					iGH_DocumentObject = list[num4];
					num = 19;
					break;
				case 30:
					TaskOrder.GetUtils(graphics, pen3, array3[0], array3[1], TaskOrder.RestartVisitor);
					num2 = 32;
					if (FlushRequest() == null)
					{
						num2 = 44;
					}
					continue;
				case 18:
				case 66:
					flag7 = num4 < CountRequest(list);
					num2 = 35;
					continue;
				case 77:
					array3[1] = new PointF(ManagerProccesor.GetUtils(ref rectangleF2, ManagerProccesor.PushVisitor) + ManagerProccesor.GetUtils(ref rectangleF2, ManagerProccesor.CheckTokenizer) / 2f, ManagerProccesor.GetUtils(ref rectangleF2, ManagerProccesor.InvokeVisitor) + ManagerProccesor.GetUtils(ref rectangleF2, ManagerProccesor.ResetTokenizer) / 2f);
					num2 = 30;
					continue;
				case 36:
					pen2 = new Pen(TaskProccesor.GetUtils(TaskProccesor.CompareVisitor), 1f);
					num2 = 56;
					continue;
				case 39:
					array2[1] = new PointF(ManagerProccesor.GetUtils(ref rectangleF, ManagerProccesor.PushVisitor) + ManagerProccesor.GetUtils(ref rectangleF, ManagerProccesor.CheckTokenizer) / 2f, ManagerProccesor.GetUtils(ref rectangleF, ManagerProccesor.InvokeVisitor) + ManagerProccesor.GetUtils(ref rectangleF, ManagerProccesor.ResetTokenizer) / 2f);
					num = 47;
					break;
				case 67:
					if (flag)
					{
						num = 58;
						break;
					}
					goto case 44;
				case 16:
					flag = ResolveRequest(this);
					num2 = 67;
					if (WriteRequest())
					{
						continue;
					}
					break;
				case 13:
					solidBrush = new SolidBrush(TaskProccesor.GetUtils(TaskProccesor.CalcVisitor));
					num2 = 6;
					if (FlushRequest() != null)
					{
						num2 = 0;
					}
					continue;
				case 56:
					array2 = new PointF[2];
					num2 = 49;
					continue;
				case 57:
					CodeOrder.GetUtils(graphics, new SolidBrush(TaskProccesor.GetUtils(TaskProccesor.CollectVisitor)), utils, CodeOrder.ExcludeVisitor);
					num2 = 2;
					continue;
				case 15:
					array3[0] = new PointF(ManagerProccesor.GetUtils(ref utils3, ManagerProccesor.PushVisitor), ManagerProccesor.GetUtils(ref utils3, ManagerProccesor.InvokeVisitor) + ManagerProccesor.GetUtils(ref utils3, ManagerProccesor.ResetTokenizer) / 2f);
					num2 = 77;
					if (FlushRequest() == null)
					{
						continue;
					}
					break;
				case 78:
					ProductOrder.GetUtils(this, _003C_003Ec__DisplayClass10_.broadcasterEvent, graphics, channel, ProductOrder.ComputeVisitor);
					num2 = 73;
					if (FlushRequest() == null)
					{
						continue;
					}
					break;
				case 65:
				{
					bool flag11 = true;
					num2 = 17;
					if (WriteRequest())
					{
						continue;
					}
					break;
				}
				case 55:
					utils2 = RegistryOrder.GetUtils(ResetWrapper(), ResetWrapper(), GH_Palette.Black, ProcessTaskContainer.VisitFactory(8898), 2, 0, RegistryOrder.SortVisitor);
					num2 = 2;
					if (WriteRequest())
					{
						num2 = 40;
					}
					continue;
				case 26:
				{
					int num6 = num4 + 1;
					ObserverOrder.GetUtils(graphics, ComposerOrder.GetUtils(ref num6, ComposerOrder.LoginRules), new Font((string)ManageRequest(8884), 10f), solidBrush, ManagerProccesor.GetUtils(ref utils3, ManagerProccesor.PushVisitor) + num3 * 3f, ManagerProccesor.GetUtils(ref utils3, ManagerProccesor.InvokeVisitor) - 6f * num3 / 4f, stringFormat, ObserverOrder.StartVisitor);
					num2 = 43;
					continue;
				}
				default:
				{
					SolidBrush solidBrush2 = new SolidBrush(TaskProccesor.GetUtils(TaskProccesor.CompareVisitor));
					num2 = 21;
					continue;
				}
				case 53:
					BroadcasterProccesor.GetUtils(graphics, pen4, CollectionProccesor.GetUtils(utils3, CollectionProccesor.RevertTokenizer), BroadcasterProccesor.SetRules);
					num2 = 23;
					continue;
				case 40:
				case 75:
					ExceptionProccesor.GetUtils(utils2, graphics, Selected, DecoratorAnnotation.GetUtils(base.Owner, DecoratorAnnotation.SetVisitor), DecoratorAnnotation.GetUtils(base.Owner, DecoratorAnnotation.DisableVisitor), ExceptionProccesor.CallRules);
					num = 25;
					break;
				case 70:
					if (flag8)
					{
						num2 = 34;
						continue;
					}
					goto case 7;
				case 50:
					ModelOrder.GetUtils(stringFormat, StringFormatFlags.NoWrap, ModelOrder.TestVisitor);
					num2 = 31;
					continue;
				case 29:
					array3 = new PointF[2];
					num2 = 15;
					if (FlushRequest() == null)
					{
						continue;
					}
					break;
				case 52:
					utils3 = ListenerOrder.GetUtils(CustomerOrder.GetUtils(iGH_DocumentObject, CustomerOrder.OrderVisitor), ListenerOrder.ConnectVisitor);
					num = 14;
					break;
				case 23:
					CodeOrder.GetUtils(graphics, new SolidBrush(TaskProccesor.GetUtils(TaskProccesor.InitVisitor)), utils3, CodeOrder.ExcludeVisitor);
					num2 = 16;
					if (FlushRequest() == null)
					{
						continue;
					}
					break;
				case 58:
					pen3 = new Pen(TaskProccesor.GetUtils(TaskProccesor.CalcVisitor), 1f);
					num2 = 29;
					continue;
				case 51:
					if (flag10)
					{
						num2 = 64;
						continue;
					}
					goto case 7;
				case 3:
				case 14:
					rectangleF2 = UpdateRequest(this);
					num2 = 25;
					if (FlushRequest() == null)
					{
						num2 = 76;
					}
					continue;
				case 2:
					flag9 = ResolveRequest(this);
					num2 = 0;
					if (WriteRequest())
					{
						num2 = 5;
					}
					continue;
				case 76:
					array = new PointF[3];
					num2 = 11;
					if (FlushRequest() == null)
					{
						continue;
					}
					break;
				case 21:
					utils = ListenerOrder.GetUtils(CustomerOrder.GetUtils(utils4, CustomerOrder.OrderVisitor), ListenerOrder.ConnectVisitor);
					num = 8;
					break;
				case 4:
					array[2] = new PointF(ManagerProccesor.GetUtils(ref utils3, ManagerProccesor.PushVisitor) + num3 * 2f, ManagerProccesor.GetUtils(ref utils3, ManagerProccesor.InvokeVisitor) - num3 * 2f);
					num = 72;
					break;
				case 27:
					num3 = 8f;
					num2 = 62;
					if (FlushRequest() == null)
					{
						continue;
					}
					break;
				case 35:
					if (!flag7)
					{
						num2 = 10;
						continue;
					}
					goto case 46;
				case 6:
					num4 = 0;
					num2 = 66;
					continue;
				case 22:
					if (flag2)
					{
						num2 = 24;
						continue;
					}
					goto case 52;
				case 1:
					ContainerOrder.GetUtils(stringFormat, StringTrimming.EllipsisCharacter, ContainerOrder.RemoveVisitor);
					num2 = 50;
					continue;
				case 24:
					utils3 = ProcessProccesor.GetUtils(SelectRequest((GH_NumberSlider2Attributes)CustomerOrder.GetUtils(iGH_DocumentObject, CustomerOrder.OrderVisitor)), ProcessProccesor.PostRules);
					num2 = 3;
					continue;
				case 32:
					if (QueryRequest(timeLineSlider) != null)
					{
						num2 = 33;
						continue;
					}
					num5 = 0;
					goto IL_0c58;
				case 71:
					if (flag4)
					{
						num2 = 38;
						if (WriteRequest())
						{
							continue;
						}
						break;
					}
					goto case 10;
				case 68:
					if (!flag5)
					{
						num2 = 48;
						continue;
					}
					goto case 55;
				case 9:
					if (!flag3)
					{
						num2 = 63;
						continue;
					}
					goto case 65;
				case 47:
					TaskOrder.GetUtils(graphics, pen2, array2[0], array2[1], TaskOrder.RestartVisitor);
					num2 = 7;
					continue;
				case 34:
					utils4 = AccountOrder.GetUtils(SpecificationOrder.GetUtils(_003C_003Ec__DisplayClass10_.broadcasterEvent, SpecificationOrder.FillVisitor), SingletonAnnotation.GetUtils(QueryRequest(timeLineSlider), SingletonAnnotation.CalculateContext), true, AccountOrder.DeleteContext);
					num2 = 28;
					continue;
				case 33:
					num5 = ((timeLineSlider.sliders != null) ? 1 : 0);
					goto IL_0c58;
				case 12:
					WrapperOrder.GetUtils(stringFormat, StringAlignment.Near, WrapperOrder.FindVisitor);
					num2 = 50;
					if (WriteRequest())
					{
						num2 = 74;
					}
					continue;
				case 45:
					if (flag6)
					{
						num2 = 69;
						if (WriteRequest())
						{
							continue;
						}
						break;
					}
					return;
				case 69:
					flag5 = !Activate;
					num2 = 68;
					if (FlushRequest() == null)
					{
						continue;
					}
					break;
				case 11:
					array[0] = new PointF(ManagerProccesor.GetUtils(ref utils3, ManagerProccesor.PushVisitor) + num3, ManagerProccesor.GetUtils(ref utils3, ManagerProccesor.InvokeVisitor) - num3 / 2f);
					num2 = 41;
					if (FlushRequest() == null)
					{
						continue;
					}
					break;
				case 31:
					flag4 = CountRequest(list) != 0;
					num2 = 71;
					continue;
				case 44:
					num4++;
					num2 = 13;
					if (FlushRequest() == null)
					{
						num2 = 18;
					}
					continue;
				case 72:
					ThreadOrder.GetUtils(graphics, solidBrush, array, ThreadOrder.LogoutVisitor);
					num2 = 26;
					continue;
				case 60:
					_003C_003Ec__DisplayClass10_.broadcasterEvent = canvas;
					num2 = 78;
					continue;
				case 20:
					BroadcasterProccesor.GetUtils(graphics, pen, CollectionProccesor.GetUtils(utils, CollectionProccesor.RevertTokenizer), BroadcasterProccesor.SetRules);
					num2 = 57;
					continue;
				case 73:
					flag3 = channel == GH_CanvasChannel.First;
					num2 = 9;
					continue;
				case 8:
					rectangleF = UpdateRequest(this);
					num2 = 37;
					if (WriteRequest())
					{
						continue;
					}
					break;
				case 19:
					flag2 = iGH_DocumentObject is pOd_TimeLineSlider;
					num2 = 22;
					continue;
				case 74:
					WrapperOrder.GetUtils(stringFormat, StringAlignment.Center, WrapperOrder.CallVisitor);
					num2 = 1;
					if (!WriteRequest())
					{
						num2 = 1;
					}
					continue;
				case 25:
					TestsAnnotation.GetUtils(utils2, TestsAnnotation.RemoveRules);
					num2 = 42;
					continue;
				case 7:
				case 59:
				case 63:
					flag6 = channel == GH_CanvasChannel.Objects;
					num2 = 45;
					if (FlushRequest() == null)
					{
						continue;
					}
					break;
				case 62:
					stringFormat = new StringFormat();
					num2 = 12;
					continue;
				case 42:
					return;
				case 41:
					array[1] = new PointF(ManagerProccesor.GetUtils(ref utils3, ManagerProccesor.PushVisitor), ManagerProccesor.GetUtils(ref utils3, ManagerProccesor.InvokeVisitor) - num3 * 2f);
					num2 = 4;
					if (WriteRequest())
					{
						continue;
					}
					break;
				case 64:
					pen = new Pen(TaskProccesor.GetUtils(TaskProccesor.CompareVisitor), 3f);
					num2 = 0;
					if (WriteRequest())
					{
						continue;
					}
					break;
				case 48:
				case 54:
					utils2 = RegistryOrder.GetUtils(ResetWrapper(), ResetWrapper(), GH_Palette.White, (string)ManageRequest(8898), 2, 0, RegistryOrder.SortVisitor);
					num = 75;
					break;
				case 37:
					{
						WorkerProccesor.GetUtils(ref utils, 4f, 4f, WorkerProccesor.NewRules);
						num2 = 20;
						continue;
					}
					IL_0c58:
					flag8 = (byte)num5 != 0;
					num2 = 70;
					continue;
				}
				break;
			}
		}
	}

	public override GH_ObjectResponse RespondToMouseUp(GH_Canvas sender, GH_CanvasMouseEvent e)
	{
		//Discarded unreachable code: IL_0002, IL_0064, IL_0073, IL_00ad, IL_00bc, IL_00c7, IL_00d6, IL_016f
		int num = 14;
		GH_ObjectResponse result = default(GH_ObjectResponse);
		bool utils = default(bool);
		RectangleF rectangleF = default(RectangleF);
		bool flag = default(bool);
		while (true)
		{
			int num2 = num;
			while (true)
			{
				switch (num2)
				{
				case 6:
					result = GH_ObjectResponse.Release;
					num2 = 1;
					if (WriteRequest())
					{
						continue;
					}
					break;
				case 9:
					if (!utils)
					{
						num2 = 10;
						continue;
					}
					goto case 3;
				case 2:
					utils = ClassProccesor.GetUtils(ref rectangleF, MessageProccesor.GetUtils(e, MessageProccesor.InstantiateRules), ClassProccesor.DestroyRules);
					num2 = 9;
					continue;
				case 12:
					Activate = false;
					num2 = 5;
					continue;
				case 5:
					TestsAnnotation.GetUtils(sender, TestsAnnotation.CreateRules);
					num2 = 6;
					if (FlushRequest() == null)
					{
						continue;
					}
					break;
				case 11:
					rectangleF = ResetWrapper();
					num2 = 2;
					if (WriteRequest())
					{
						num2 = 2;
					}
					continue;
				default:
					result = CalculateRequest(this, sender, e);
					num2 = 7;
					continue;
				case 1:
				case 7:
				case 8:
					return result;
				case 14:
					flag = PrototypeProccesor.GetUtils(e, PrototypeProccesor.DisableRules) == MouseButtons.Left;
					num2 = 13;
					if (WriteRequest())
					{
						continue;
					}
					break;
				case 13:
					if (!flag)
					{
						num2 = 4;
						continue;
					}
					goto case 11;
				case 3:
					SetterProccesor.GetUtils(base.Owner, true, SetterProccesor.InstantiateVisitor);
					num2 = 12;
					if (FlushRequest() == null)
					{
						continue;
					}
					break;
				}
				break;
			}
		}
	}

	public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
	{
		//Discarded unreachable code: IL_0002, IL_0082, IL_0149
		int num = 7;
		bool utils = default(bool);
		RectangleF rectangleF = default(RectangleF);
		GH_ObjectResponse result = default(GH_ObjectResponse);
		bool flag = default(bool);
		while (true)
		{
			int num2 = num;
			while (true)
			{
				switch (num2)
				{
				case 4:
					utils = ClassProccesor.GetUtils(ref rectangleF, MessageProccesor.GetUtils(e, MessageProccesor.InstantiateRules), ClassProccesor.DestroyRules);
					num = 9;
					break;
				case 1:
				case 8:
				case 10:
					return result;
				case 2:
					result = GH_ObjectResponse.Capture;
					num2 = 1;
					if (!WriteRequest())
					{
						num2 = 0;
					}
					continue;
				default:
					Activate = true;
					num2 = 5;
					if (WriteRequest())
					{
						continue;
					}
					break;
				case 3:
					rectangleF = ResetWrapper();
					num2 = 4;
					continue;
				case 6:
					if (flag)
					{
						num2 = 3;
						continue;
					}
					goto case 11;
				case 9:
					if (utils)
					{
						num2 = 0;
						if (FlushRequest() == null)
						{
							continue;
						}
						break;
					}
					goto case 11;
				case 11:
					result = AdapterOrder.GetUtils(this, sender, e, AdapterOrder.DestroyVisitor);
					num2 = 8;
					continue;
				case 7:
					flag = PrototypeProccesor.GetUtils(e, PrototypeProccesor.DisableRules) == MouseButtons.Left;
					num = 6;
					break;
				case 5:
					TestsAnnotation.GetUtils(sender, TestsAnnotation.CreateRules);
					num2 = 2;
					if (FlushRequest() != null)
					{
						num2 = 1;
					}
					continue;
				}
				break;
			}
		}
	}

	static TimeLineSliderAttributes()
	{
		//Discarded unreachable code: IL_0002
		DeleteRequest();
		CloneRequest();
	}

	internal static bool WriteRequest()
	{
		//Discarded unreachable code: IL_0002
		return ListRequest == null;
	}

	internal static TimeLineSliderAttributes FlushRequest()
	{
		//Discarded unreachable code: IL_0002
		return ListRequest;
	}

	internal static void InterruptRequest()
	{
		//Discarded unreachable code: IL_0002
		TokenizerClass.DisableDic();
	}

	internal static RectangleF UpdateRequest(object P_0)
	{
		//Discarded unreachable code: IL_0002
		return ((GH_Attributes<IGH_Component>)P_0).Bounds;
	}

	internal static void SearchRequest(object P_0, RectangleF P_1)
	{
		//Discarded unreachable code: IL_0002
		((GH_Attributes<IGH_Component>)P_0).Bounds = P_1;
	}

	internal static object QueryRequest(object P_0)
	{
		//Discarded unreachable code: IL_0002
		return ((TimeLineSlider)P_0).control;
	}

	internal static int CountRequest(object P_0)
	{
		//Discarded unreachable code: IL_0002
		return ((List<IGH_DocumentObject>)P_0).Count;
	}

	internal static Rectangle SelectRequest(object P_0)
	{
		//Discarded unreachable code: IL_0002
		return ((GH_NumberSlider2Attributes)P_0).CombineBounds;
	}

	internal static object ManageRequest(int previousparam)
	{
		//Discarded unreachable code: IL_0002
		return ProcessTaskContainer.VisitFactory(previousparam);
	}

	internal static bool ResolveRequest(object P_0)
	{
		//Discarded unreachable code: IL_0002
		return ((GH_Attributes<IGH_Component>)P_0).Selected;
	}

	internal static GH_ObjectResponse CalculateRequest(object P_0, object P_1, object P_2)
	{
		//Discarded unreachable code: IL_0002
		return ((GH_Attributes<IGH_Component>)P_0).RespondToMouseUp((GH_Canvas)P_1, (GH_CanvasMouseEvent)P_2);
	}

	internal static void DeleteRequest()
	{
		//Discarded unreachable code: IL_0002
		ProcessTaskContainer.StartFactory();
	}

	internal static void CloneRequest()
	{
		//Discarded unreachable code: IL_0002
		ObserverAuthenticationClass.kLjw4iIsCLsZtxc4lksN0j();
	}
}
