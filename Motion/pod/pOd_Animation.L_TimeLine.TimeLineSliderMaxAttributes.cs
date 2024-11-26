// 警告：某些程序集引用无法自动解析。这可能会导致某些部分反编译错误，
// 例如属性 getter/setter 访问。要获得最佳反编译结果，请手动将缺少的引用添加到加载的程序集列表中。
// pOd_Animation.L_TimeLine.TimeLineSliderMaxAttributes
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

public class TimeLineSliderMaxAttributes : GH_ComponentAttributes
{
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	[CompilerGenerated]
	private RectangleF queue;

	private static TimeLineSliderMaxAttributes CheckRequest;

	public bool Activate
	{
		[CompilerGenerated]
		get
		{
			//Discarded unreachable code: IL_0002
			return _Template;
		}
		[CompilerGenerated]
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
					_Template = value;
					num2 = 0;
					if (ConcatRequest() == null)
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

	public bool LockAct
	{
		[CompilerGenerated]
		get
		{
			//Discarded unreachable code: IL_0002
			return _Worker;
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
					case 0:
						return;
					case 1:
						break;
					}
					_Worker = value;
					num2 = 0;
				}
				while (ConcatRequest() == null);
			}
		}
	}

	public TimeLineSliderMaxAttributes(TimeLineSliderMax owner)
	{
		//Discarded unreachable code: IL_0002, IL_002e
		ResetRequest();
		base._002Ector(owner);
		int num = 1;
		if (false)
		{
			num = 1;
		}
		while (true)
		{
			int num2;
			switch (num)
			{
			case 1:
				Activate = false;
				num = 0;
				if (true)
				{
					continue;
				}
				break;
			case 2:
				return;
			default:
				LockAct = false;
				num2 = 2;
				break;
			}
			num = num2;
		}
	}

	protected override void Layout()
	{
		//Discarded unreachable code: IL_0002
		int num = 4;
		Rectangle rectangle2 = default(Rectangle);
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
					SchemaProccesor.GetUtils(ref rectangle2, -2, -2, SchemaProccesor.TestRules);
					num2 = 8;
					continue;
				case 12:
					ParamsOrder.GetUtils(ref utils, ProducerProccesor.GetUtils(ref utils, ProducerProccesor.PopVisitor) + 42, ParamsOrder.CustomizeVisitor);
					num = 11;
					break;
				case 9:
					ParamsOrder.GetUtils(ref rectangle, ProducerProccesor.GetUtils(ref rectangle, ProducerProccesor.PushRules) - 22, ParamsOrder.IncludeVisitor);
					num2 = 7;
					continue;
				case 3:
					utils = CollectionProccesor.GetUtils(VerifyRequest(this), CollectionProccesor.RevertTokenizer);
					num2 = 12;
					continue;
				case 8:
					rectangle = utils;
					num2 = 9;
					if (MoveRequest())
					{
						continue;
					}
					break;
				case 14:
					SchemaProccesor.GetUtils(ref rectangle, -2, -2, SchemaProccesor.TestRules);
					num2 = 10;
					continue;
				default:
					ComputeWrapper(ProcessProccesor.GetUtils(rectangle2, ProcessProccesor.PostRules));
					num2 = 1;
					if (ConcatRequest() == null)
					{
						continue;
					}
					break;
				case 5:
					ParamsOrder.GetUtils(ref rectangle2, 22, ParamsOrder.CustomizeVisitor);
					num2 = 3;
					if (ConcatRequest() == null)
					{
						num2 = 6;
					}
					continue;
				case 7:
					ParamsOrder.GetUtils(ref rectangle, 22, ParamsOrder.CustomizeVisitor);
					num2 = 14;
					continue;
				case 10:
					RegisterRequest(this, ProcessProccesor.GetUtils(utils, ProcessProccesor.PostRules));
					num2 = 0;
					if (!MoveRequest())
					{
						num2 = 0;
					}
					continue;
				case 4:
					TestsAnnotation.GetUtils(this, TestsAnnotation.VisitVisitor);
					num2 = 3;
					continue;
				case 11:
					rectangle2 = utils;
					num2 = 2;
					continue;
				case 2:
					ParamsOrder.GetUtils(ref rectangle2, ProducerProccesor.GetUtils(ref rectangle2, ProducerProccesor.PushRules) - 42, ParamsOrder.IncludeVisitor);
					num2 = 5;
					if (!MoveRequest())
					{
						num2 = 2;
					}
					continue;
				case 1:
					ViewWrapper(ProcessProccesor.GetUtils(rectangle, ProcessProccesor.PostRules));
					num2 = 13;
					continue;
				case 13:
					return;
				}
				break;
			}
		}
	}

	[SpecialName]
	[CompilerGenerated]
	private RectangleF AddWrapper()
	{
		//Discarded unreachable code: IL_0002
		return _Exception;
	}

	[SpecialName]
	[CompilerGenerated]
	private void ComputeWrapper(RectangleF value)
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
				_Exception = value;
				num2 = 0;
			}
			while (ConcatRequest() == null);
		}
	}

	[SpecialName]
	[CompilerGenerated]
	private RectangleF CancelWrapper()
	{
		//Discarded unreachable code: IL_0002
		return queue;
	}

	[SpecialName]
	[CompilerGenerated]
	private void ViewWrapper(RectangleF value)
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
				queue = value;
				num2 = 0;
			}
			while (ConcatRequest() == null);
		}
	}

	protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
	{
		//Discarded unreachable code: IL_0002, IL_02c3, IL_045c, IL_048e, IL_049d, IL_060d, IL_06e7, IL_06f6, IL_0813, IL_0822, IL_09d9, IL_09e8, IL_0aa6, IL_0e0f, IL_0e1e, IL_0e2d
		int num = 17;
		pOd_TimeLineSlider pOd_TimeLineSlider3 = default(pOd_TimeLineSlider);
		IGH_DocumentObject utils4 = default(IGH_DocumentObject);
		IGH_DocumentObject iGH_DocumentObject = default(IGH_DocumentObject);
		List<IGH_DocumentObject> list = default(List<IGH_DocumentObject>);
		int num5 = default(int);
		RectangleF utils3 = default(RectangleF);
		StringFormat stringFormat = default(StringFormat);
		bool flag9 = default(bool);
		PointF[] array4 = default(PointF[]);
		RectangleF bounds2 = default(RectangleF);
		bool flag7 = default(bool);
		Pen pen4 = default(Pen);
		Pen pen3 = default(Pen);
		GH_Capsule utils5 = default(GH_Capsule);
		PointF[] array2 = default(PointF[]);
		Pen pen2 = default(Pen);
		Pen pen = default(Pen);
		GH_Capsule utils2 = default(GH_Capsule);
		float num4 = default(float);
		TimeLineSliderMax timeLineSliderMax = default(TimeLineSliderMax);
		RectangleF bounds = default(RectangleF);
		RectangleF utils = default(RectangleF);
		bool flag = default(bool);
		bool flag6 = default(bool);
		SolidBrush solidBrush = default(SolidBrush);
		GH_NumberSlider2Attributes gH_NumberSlider2Attributes = default(GH_NumberSlider2Attributes);
		bool flag3 = default(bool);
		_003C_003Ec__DisplayClass18_0 _003C_003Ec__DisplayClass18_ = default(_003C_003Ec__DisplayClass18_0);
		bool lockAct = default(bool);
		PointF[] array3 = default(PointF[]);
		GH_NumberSlider2Attributes gH_NumberSlider2Attributes2 = default(GH_NumberSlider2Attributes);
		PointF[] array = default(PointF[]);
		bool flag8 = default(bool);
		pOd_TimeLineSlider pOd_TimeLineSlider2 = default(pOd_TimeLineSlider);
		bool flag5 = default(bool);
		SolidBrush solidBrush2 = default(SolidBrush);
		bool flag2 = default(bool);
		bool flag4 = default(bool);
		while (true)
		{
			int num2 = num;
			while (true)
			{
				int num3;
				switch (num2)
				{
				case 64:
					pOd_TimeLineSlider3 = (pOd_TimeLineSlider)utils4;
					num2 = 22;
					continue;
				case 62:
					iGH_DocumentObject = list[num5];
					num2 = 76;
					continue;
				case 13:
					CodeOrder.GetUtils(graphics, new SolidBrush(TaskProccesor.GetUtils(TaskProccesor.CollectVisitor)), utils3, CodeOrder.ExcludeVisitor);
					num2 = 47;
					if (ConcatRequest() == null)
					{
						continue;
					}
					break;
				case 38:
					WrapperOrder.GetUtils(stringFormat, StringAlignment.Center, WrapperOrder.CallVisitor);
					num2 = 73;
					continue;
				case 90:
					if (flag9)
					{
						num2 = 86;
						if (MoveRequest())
						{
							continue;
						}
						break;
					}
					goto case 85;
				case 8:
					array4[1] = new PointF(ManagerProccesor.GetUtils(ref bounds2, ManagerProccesor.PushVisitor) + ManagerProccesor.GetUtils(ref bounds2, ManagerProccesor.CheckTokenizer) / 2f, ManagerProccesor.GetUtils(ref bounds2, ManagerProccesor.InvokeVisitor) + ManagerProccesor.GetUtils(ref bounds2, ManagerProccesor.ResetTokenizer) / 2f);
					num2 = 70;
					continue;
				case 61:
					flag7 = !Activate;
					num2 = 28;
					continue;
				case 3:
					pen4 = new Pen(TaskProccesor.GetUtils(TaskProccesor.CompareVisitor), 3f);
					num2 = 78;
					if (!MoveRequest())
					{
						num2 = 60;
					}
					continue;
				case 44:
					return;
				case 53:
					pen3 = new Pen(TaskProccesor.GetUtils(TaskProccesor.CalcVisitor), 3f);
					num2 = 55;
					if (MoveRequest())
					{
						continue;
					}
					break;
				case 42:
				case 69:
					ExceptionProccesor.GetUtils(utils5, graphics, Selected, DecoratorAnnotation.GetUtils(base.Owner, DecoratorAnnotation.SetVisitor), DecoratorAnnotation.GetUtils(base.Owner, DecoratorAnnotation.DisableVisitor), ExceptionProccesor.CallRules);
					num2 = 68;
					continue;
				case 15:
					bounds2 = Bounds;
					num2 = 24;
					continue;
				case 24:
					array2 = new PointF[3];
					num2 = 80;
					if (ConcatRequest() == null)
					{
						continue;
					}
					break;
				case 26:
					num5 = 0;
					num2 = 0;
					if (ConcatRequest() == null)
					{
						continue;
					}
					break;
				case 73:
					ContainerOrder.GetUtils(stringFormat, StringTrimming.EllipsisCharacter, ContainerOrder.RemoveVisitor);
					num2 = 5;
					continue;
				case 86:
					pen2 = new Pen(TaskProccesor.GetUtils(TaskProccesor.CalcVisitor), 1f);
					num2 = 49;
					if (MoveRequest())
					{
						continue;
					}
					break;
				case 10:
					pen = new Pen(TaskProccesor.GetUtils(TaskProccesor.CompareVisitor), 1f);
					num2 = 48;
					continue;
				case 39:
					TestsAnnotation.GetUtils(utils2, TestsAnnotation.RemoveRules);
					num2 = 81;
					continue;
				case 65:
					array2[2] = new PointF(ManagerProccesor.GetUtils(ref utils3, ManagerProccesor.PushVisitor) - num4 * 2f, ManagerProccesor.GetUtils(ref utils3, ManagerProccesor.InvokeVisitor) + ManagerProccesor.GetUtils(ref utils3, ManagerProccesor.ResetTokenizer) / 2f + num4);
					num2 = 50;
					if (MoveRequest())
					{
						continue;
					}
					break;
				case 4:
					timeLineSliderMax = base.Owner as TimeLineSliderMax;
					num2 = 57;
					if (ConcatRequest() == null)
					{
						continue;
					}
					break;
				case 66:
					return;
				case 30:
					bounds = Bounds;
					num2 = 36;
					if (ConcatRequest() == null)
					{
						continue;
					}
					break;
				case 88:
					CodeOrder.GetUtils(graphics, new SolidBrush(TaskProccesor.GetUtils(TaskProccesor.InitVisitor)), utils, CodeOrder.ExcludeVisitor);
					num2 = 87;
					continue;
				case 34:
					if (flag)
					{
						num2 = 53;
						continue;
					}
					goto case 46;
				case 58:
					if (!flag6)
					{
						num2 = 46;
						continue;
					}
					goto case 62;
				case 50:
					ThreadOrder.GetUtils(graphics, solidBrush, array2, ThreadOrder.LogoutVisitor);
					num2 = 9;
					continue;
				case 41:
					utils = ProcessProccesor.GetUtils(PostDecorator(gH_NumberSlider2Attributes), ProcessProccesor.PostRules);
					num2 = 30;
					continue;
				case 51:
					BroadcasterProccesor.GetUtils(graphics, pen3, CollectionProccesor.GetUtils(utils, CollectionProccesor.RevertTokenizer), BroadcasterProccesor.SetRules);
					num2 = 88;
					if (MoveRequest())
					{
						continue;
					}
					break;
				case 52:
					if (flag3)
					{
						num = 4;
						break;
					}
					goto case 23;
				case 43:
					array4[0] = new PointF(ManagerProccesor.GetUtils(ref utils3, ManagerProccesor.PushVisitor), ManagerProccesor.GetUtils(ref utils3, ManagerProccesor.InvokeVisitor) + ManagerProccesor.GetUtils(ref utils3, ManagerProccesor.ResetTokenizer) / 2f);
					num2 = 8;
					continue;
				case 59:
					WrapperOrder.GetUtils(stringFormat, StringAlignment.Far, WrapperOrder.FindVisitor);
					num2 = 38;
					continue;
				case 87:
					flag9 = NewDecorator(this);
					num2 = 90;
					continue;
				case 17:
					_003C_003Ec__DisplayClass18_ = new _003C_003Ec__DisplayClass18_0();
					num2 = 16;
					continue;
				case 40:
					num4 = 8f;
					num2 = 32;
					if (ConcatRequest() == null)
					{
						continue;
					}
					break;
				case 81:
					lockAct = LockAct;
					num2 = 7;
					continue;
				case 68:
					TestsAnnotation.GetUtils(utils5, TestsAnnotation.RemoveRules);
					num2 = 66;
					continue;
				case 60:
					array3[1] = new PointF(ManagerProccesor.GetUtils(ref utils, ManagerProccesor.PushVisitor) - num4 * 2f, ManagerProccesor.GetUtils(ref utils, ManagerProccesor.InvokeVisitor) + ManagerProccesor.GetUtils(ref utils, ManagerProccesor.ResetTokenizer) / 2f - num4);
					num2 = 18;
					if (!MoveRequest())
					{
						num2 = 0;
					}
					continue;
				case 70:
					TaskOrder.GetUtils(graphics, pen, array4[0], array4[1], TaskOrder.RestartVisitor);
					num2 = 79;
					continue;
				case 57:
					if (timeLineSliderMax.control == null)
					{
						num2 = 74;
						continue;
					}
					goto case 71;
				case 32:
					stringFormat = new StringFormat();
					num2 = 59;
					if (ConcatRequest() == null)
					{
						continue;
					}
					break;
				case 7:
					if (!lockAct)
					{
						num2 = 20;
						continue;
					}
					goto case 11;
				case 12:
					utils3 = ProcessProccesor.GetUtils(PostDecorator(gH_NumberSlider2Attributes2), ProcessProccesor.PostRules);
					num2 = 15;
					continue;
				case 27:
					BroadcasterProccesor.GetUtils(graphics, pen4, CollectionProccesor.GetUtils(utils3, CollectionProccesor.RevertTokenizer), BroadcasterProccesor.SetRules);
					num = 13;
					break;
				case 56:
					TaskOrder.GetUtils(graphics, pen2, array[0], array[1], TaskOrder.RestartVisitor);
					num2 = 85;
					continue;
				case 82:
					if (!flag8)
					{
						num2 = 23;
						continue;
					}
					goto case 3;
				case 76:
					pOd_TimeLineSlider2 = (pOd_TimeLineSlider)iGH_DocumentObject;
					num2 = 24;
					if (ConcatRequest() == null)
					{
						num2 = 35;
					}
					continue;
				case 48:
					array4 = new PointF[2];
					num2 = 43;
					if (ConcatRequest() == null)
					{
						continue;
					}
					break;
				case 28:
					if (!flag7)
					{
						num2 = 2;
						continue;
					}
					goto case 14;
				case 5:
					ModelOrder.GetUtils(stringFormat, StringFormatFlags.NoWrap, ModelOrder.TestVisitor);
					num2 = 83;
					continue;
				case 18:
					array3[2] = new PointF(ManagerProccesor.GetUtils(ref utils, ManagerProccesor.PushVisitor) - num4 * 2f, ManagerProccesor.GetUtils(ref utils, ManagerProccesor.InvokeVisitor) + ManagerProccesor.GetUtils(ref utils, ManagerProccesor.ResetTokenizer) / 2f + num4);
					num2 = 29;
					if (MoveRequest())
					{
						continue;
					}
					break;
				case 11:
					utils5 = RegistryOrder.GetUtils(CancelWrapper(), CancelWrapper(), GH_Palette.Black, (string)ViewDecorator(9492), 2, 0, RegistryOrder.SortVisitor);
					num2 = 24;
					if (MoveRequest())
					{
						num2 = 42;
					}
					continue;
				case 45:
					array3[0] = new PointF(ManagerProccesor.GetUtils(ref utils, ManagerProccesor.PushVisitor) - num4 / 2f, ManagerProccesor.GetUtils(ref utils, ManagerProccesor.InvokeVisitor) + ManagerProccesor.GetUtils(ref utils, ManagerProccesor.ResetTokenizer) / 2f);
					num = 60;
					break;
				case 31:
				case 89:
					ExceptionProccesor.GetUtils(utils2, graphics, NewDecorator(this), DecoratorAnnotation.GetUtils(base.Owner, DecoratorAnnotation.SetVisitor), DecoratorAnnotation.GetUtils(base.Owner, DecoratorAnnotation.DisableVisitor), ExceptionProccesor.CallRules);
					num2 = 39;
					if (ConcatRequest() == null)
					{
						continue;
					}
					break;
				default:
					flag6 = num5 < RevertRequest(list);
					num = 58;
					break;
				case 22:
					gH_NumberSlider2Attributes2 = (GH_NumberSlider2Attributes)CustomerOrder.GetUtils(pOd_TimeLineSlider3, CustomerOrder.PrepareContext);
					num2 = 12;
					continue;
				case 25:
					if (!flag5)
					{
						num2 = 44;
						if (ConcatRequest() != null)
						{
							num2 = 18;
						}
						continue;
					}
					goto case 61;
				case 29:
					ThreadOrder.GetUtils(graphics, solidBrush2, array3, ThreadOrder.LogoutVisitor);
					num2 = 63;
					continue;
				case 20:
				case 72:
					utils5 = RegistryOrder.GetUtils(CancelWrapper(), CancelWrapper(), GH_Palette.White, (string)ViewDecorator(9508), 2, 0, RegistryOrder.SortVisitor);
					num2 = 69;
					continue;
				case 80:
					array2[0] = new PointF(ManagerProccesor.GetUtils(ref utils3, ManagerProccesor.PushVisitor) - num4 / 2f, ManagerProccesor.GetUtils(ref utils3, ManagerProccesor.InvokeVisitor) + ManagerProccesor.GetUtils(ref utils3, ManagerProccesor.ResetTokenizer) / 2f);
					num2 = 19;
					if (!MoveRequest())
					{
						num2 = 17;
					}
					continue;
				case 71:
					num3 = ((timeLineSliderMax.sliders != null) ? 1 : 0);
					goto IL_0e90;
				case 47:
					flag2 = NewDecorator(this);
					num2 = 91;
					continue;
				case 37:
					ProductOrder.GetUtils(this, _003C_003Ec__DisplayClass18_.proxyEvent, graphics, channel, ProductOrder.ComputeVisitor);
					num = 75;
					break;
				case 19:
					array2[1] = new PointF(ManagerProccesor.GetUtils(ref utils3, ManagerProccesor.PushVisitor) - num4 * 2f, ManagerProccesor.GetUtils(ref utils3, ManagerProccesor.InvokeVisitor) + ManagerProccesor.GetUtils(ref utils3, ManagerProccesor.ResetTokenizer) / 2f - num4);
					num2 = 65;
					continue;
				case 23:
				case 67:
				case 79:
					flag5 = channel == GH_CanvasChannel.Objects;
					num2 = 25;
					continue;
				case 84:
					if (flag4)
					{
						num2 = 33;
						continue;
					}
					goto case 23;
				case 75:
					flag3 = channel == GH_CanvasChannel.First;
					num2 = 52;
					continue;
				case 33:
					utils4 = AccountOrder.GetUtils(SpecificationOrder.GetUtils(_003C_003Ec__DisplayClass18_.proxyEvent, SpecificationOrder.FillVisitor), SingletonAnnotation.GetUtils(timeLineSliderMax.control, SingletonAnnotation.CalculateContext), true, AccountOrder.DeleteContext);
					num2 = 8;
					if (MoveRequest())
					{
						num2 = 77;
					}
					continue;
				case 21:
					array[1] = new PointF(ManagerProccesor.GetUtils(ref bounds, ManagerProccesor.PushVisitor) + ManagerProccesor.GetUtils(ref bounds, ManagerProccesor.CheckTokenizer) / 2f, ManagerProccesor.GetUtils(ref bounds, ManagerProccesor.InvokeVisitor) + ManagerProccesor.GetUtils(ref bounds, ManagerProccesor.ResetTokenizer) / 2f);
					num2 = 56;
					continue;
				case 36:
					array3 = new PointF[3];
					num2 = 45;
					continue;
				case 91:
					if (!flag2)
					{
						num2 = 24;
						if (MoveRequest())
						{
							num2 = 67;
						}
						continue;
					}
					goto case 10;
				case 9:
					WorkerProccesor.GetUtils(ref utils3, 4f, 4f, WorkerProccesor.NewRules);
					num2 = 27;
					continue;
				case 35:
					gH_NumberSlider2Attributes = (GH_NumberSlider2Attributes)CustomerOrder.GetUtils(pOd_TimeLineSlider2, CustomerOrder.PrepareContext);
					num2 = 41;
					continue;
				case 77:
					list = timeLineSliderMax.sliders.Where(_003C_003Ec__DisplayClass18_.OrderClass).Select(_003C_003Ec__DisplayClass18_.SetClass).ToList();
					num2 = 40;
					continue;
				case 55:
					solidBrush2 = new SolidBrush(TaskProccesor.GetUtils(TaskProccesor.CalcVisitor));
					num2 = 26;
					continue;
				case 78:
					solidBrush = new SolidBrush(TaskProccesor.GetUtils(TaskProccesor.CompareVisitor));
					num2 = 64;
					continue;
				case 1:
				case 2:
					utils2 = RegistryOrder.GetUtils(AddWrapper(), AddWrapper(), GH_Palette.White, ProcessTaskContainer.VisitFactory(8898), 2, 0, RegistryOrder.SortVisitor);
					num2 = 89;
					continue;
				case 46:
					flag8 = utils4 != null;
					num2 = 60;
					if (MoveRequest())
					{
						num2 = 82;
					}
					continue;
				case 6:
					array[0] = new PointF(ManagerProccesor.GetUtils(ref utils, ManagerProccesor.PushVisitor), ManagerProccesor.GetUtils(ref utils, ManagerProccesor.InvokeVisitor) + ManagerProccesor.GetUtils(ref utils, ManagerProccesor.ResetTokenizer) / 2f);
					num2 = 21;
					if (MoveRequest())
					{
						continue;
					}
					break;
				case 83:
					flag = RevertRequest(list) != 0;
					num2 = 34;
					continue;
				case 16:
					_003C_003Ec__DisplayClass18_.proxyEvent = canvas;
					num2 = 37;
					if (MoveRequest())
					{
						continue;
					}
					break;
				case 49:
					array = new PointF[2];
					num2 = 6;
					if (MoveRequest())
					{
						continue;
					}
					break;
				case 63:
					WorkerProccesor.GetUtils(ref utils, 4f, 4f, WorkerProccesor.NewRules);
					num2 = 51;
					if (ConcatRequest() != null)
					{
						num2 = 36;
					}
					continue;
				case 14:
					utils2 = RegistryOrder.GetUtils(AddWrapper(), AddWrapper(), GH_Palette.Black, (string)ViewDecorator(8898), 2, 0, RegistryOrder.SortVisitor);
					num2 = 31;
					if (ConcatRequest() != null)
					{
						num2 = 4;
					}
					continue;
				case 85:
					num5++;
					num = 54;
					break;
				case 74:
					{
						num3 = 0;
						goto IL_0e90;
					}
					IL_0e90:
					flag4 = (byte)num3 != 0;
					num2 = 84;
					continue;
				}
				break;
			}
		}
	}

	public override GH_ObjectResponse RespondToMouseUp(GH_Canvas sender, GH_CanvasMouseEvent e)
	{
		//Discarded unreachable code: IL_0002, IL_00f4, IL_0103, IL_0112
		int num = 10;
		GH_ObjectResponse result = default(GH_ObjectResponse);
		bool flag = default(bool);
		bool utils = default(bool);
		RectangleF rectangleF = default(RectangleF);
		while (true)
		{
			int num2 = num;
			while (true)
			{
				switch (num2)
				{
				case 4:
					result = GH_ObjectResponse.Release;
					num2 = 2;
					continue;
				case 5:
					TestsAnnotation.GetUtils(sender, TestsAnnotation.CreateRules);
					num2 = 4;
					if (MoveRequest())
					{
						continue;
					}
					break;
				case 1:
				case 2:
				case 11:
					return result;
				default:
					result = AssetDecorator(this, sender, e);
					num2 = 1;
					if (MoveRequest())
					{
						continue;
					}
					break;
				case 6:
					Activate = false;
					num2 = 5;
					continue;
				case 9:
					if (flag)
					{
						num2 = 12;
						continue;
					}
					goto default;
				case 7:
					utils = ClassProccesor.GetUtils(ref rectangleF, MessageProccesor.GetUtils(e, MessageProccesor.InstantiateRules), ClassProccesor.DestroyRules);
					num2 = 3;
					continue;
				case 3:
					if (utils)
					{
						num2 = 8;
						if (ConcatRequest() == null)
						{
							continue;
						}
						break;
					}
					goto default;
				case 10:
					flag = PrototypeProccesor.GetUtils(e, PrototypeProccesor.DisableRules) == MouseButtons.Left;
					num2 = 9;
					continue;
				case 12:
					rectangleF = AddWrapper();
					num2 = 7;
					if (ConcatRequest() == null)
					{
						continue;
					}
					break;
				case 8:
					SetterProccesor.GetUtils(base.Owner, true, SetterProccesor.InstantiateVisitor);
					num2 = 6;
					if (ConcatRequest() != null)
					{
						num2 = 5;
					}
					continue;
				}
				break;
			}
		}
	}

	public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
	{
		//Discarded unreachable code: IL_0002, IL_00d4, IL_00e3, IL_0141, IL_0150, IL_01fa, IL_0209, IL_0233, IL_0263, IL_0272
		int num = 10;
		RectangleF rectangleF = default(RectangleF);
		GH_ObjectResponse result = default(GH_ObjectResponse);
		bool utils2 = default(bool);
		bool flag = default(bool);
		bool utils = default(bool);
		while (true)
		{
			int num2 = num;
			while (true)
			{
				switch (num2)
				{
				case 11:
				case 17:
					rectangleF = CancelWrapper();
					num2 = 14;
					continue;
				case 1:
					TestsAnnotation.GetUtils(sender, TestsAnnotation.CreateRules);
					num2 = 15;
					if (!MoveRequest())
					{
						num2 = 8;
					}
					continue;
				case 4:
					SetterProccesor.GetUtils(base.Owner, true, SetterProccesor.InstantiateVisitor);
					num2 = 12;
					continue;
				case 3:
				case 6:
				case 13:
				case 21:
					return result;
				case 14:
					utils2 = ClassProccesor.GetUtils(ref rectangleF, MessageProccesor.GetUtils(e, MessageProccesor.InstantiateRules), ClassProccesor.DestroyRules);
					num2 = 19;
					continue;
				case 10:
					flag = PrototypeProccesor.GetUtils(e, PrototypeProccesor.DisableRules) == MouseButtons.Left;
					num2 = 9;
					continue;
				case 15:
					result = GH_ObjectResponse.Capture;
					num2 = 3;
					continue;
				case 16:
				case 20:
					result = AdapterOrder.GetUtils(this, sender, e, AdapterOrder.DestroyVisitor);
					num2 = 21;
					continue;
				default:
					rectangleF = AddWrapper();
					num2 = 18;
					if (ConcatRequest() != null)
					{
						num2 = 11;
					}
					continue;
				case 5:
					TestsAnnotation.GetUtils(sender, TestsAnnotation.CreateRules);
					num = 4;
					break;
				case 18:
					utils = ClassProccesor.GetUtils(ref rectangleF, MessageProccesor.GetUtils(e, MessageProccesor.InstantiateRules), ClassProccesor.DestroyRules);
					num = 7;
					break;
				case 19:
					if (utils2)
					{
						num2 = 3;
						if (MoveRequest())
						{
							num2 = 8;
						}
						continue;
					}
					goto case 16;
				case 8:
					LockAct = !LockAct;
					num2 = 5;
					continue;
				case 7:
					if (!utils)
					{
						num2 = 8;
						if (ConcatRequest() == null)
						{
							num2 = 17;
						}
						continue;
					}
					goto case 2;
				case 9:
					if (!flag)
					{
						num2 = 16;
						if (ConcatRequest() == null)
						{
							continue;
						}
						break;
					}
					goto default;
				case 2:
					Activate = true;
					num2 = 0;
					if (MoveRequest())
					{
						num2 = 1;
					}
					continue;
				case 12:
					result = GH_ObjectResponse.Handled;
					num = 6;
					break;
				}
				break;
			}
		}
	}

	static TimeLineSliderMaxAttributes()
	{
		//Discarded unreachable code: IL_0002
		VisitDecorator();
		PopDecorator();
	}

	internal static bool MoveRequest()
	{
		//Discarded unreachable code: IL_0002
		return CheckRequest == null;
	}

	internal static TimeLineSliderMaxAttributes ConcatRequest()
	{
		//Discarded unreachable code: IL_0002
		return CheckRequest;
	}

	internal static void ResetRequest()
	{
		//Discarded unreachable code: IL_0002
		TokenizerClass.DisableDic();
	}

	internal static RectangleF VerifyRequest(object P_0)
	{
		//Discarded unreachable code: IL_0002
		return ((GH_Attributes<IGH_Component>)P_0).Bounds;
	}

	internal static void RegisterRequest(object P_0, RectangleF P_1)
	{
		//Discarded unreachable code: IL_0002
		((GH_Attributes<IGH_Component>)P_0).Bounds = P_1;
	}

	internal static int RevertRequest(object P_0)
	{
		//Discarded unreachable code: IL_0002
		return ((List<IGH_DocumentObject>)P_0).Count;
	}

	internal static Rectangle PostDecorator(object P_0)
	{
		//Discarded unreachable code: IL_0002
		return ((GH_NumberSlider2Attributes)P_0).CombineBounds;
	}

	internal static bool NewDecorator(object P_0)
	{
		//Discarded unreachable code: IL_0002
		return ((GH_Attributes<IGH_Component>)P_0).Selected;
	}

	internal static object ViewDecorator(int previousparam)
	{
		//Discarded unreachable code: IL_0002
		return ProcessTaskContainer.VisitFactory(previousparam);
	}

	internal static GH_ObjectResponse AssetDecorator(object P_0, object P_1, object P_2)
	{
		//Discarded unreachable code: IL_0002
		return ((GH_Attributes<IGH_Component>)P_0).RespondToMouseUp((GH_Canvas)P_1, (GH_CanvasMouseEvent)P_2);
	}

	internal static void VisitDecorator()
	{
		//Discarded unreachable code: IL_0002
		ProcessTaskContainer.StartFactory();
	}

	internal static void PopDecorator()
	{
		//Discarded unreachable code: IL_0002
		ObserverAuthenticationClass.kLjw4iIsCLsZtxc4lksN0j();
	}
}
