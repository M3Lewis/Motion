import clr
clr.AddReference('RevitAPI')
clr.AddReference('RevitAPIUI')
clr.AddReference('RevitServices')
from RevitServices.Persistence import DocumentManager
from RevitServices.Transactions import TransactionManager
from Autodesk.Revit.DB import *

def create_table(doc, title_name, col1_width, col2_width, col3_width, row_height, 
                levels, level_names, level_symbol, electrical_room_symbol, point_symbol, view_name="设备布置表"):
    # 创建事务
    transaction = Transaction(doc, "Create Table")
    transaction.Start()
    
    try:
        # 先查找是否存在指定名称的视图
        collector = FilteredElementCollector(doc).OfClass(ViewSection)
        tableView = None
        for view in collector:
            if view.Name == view_name:
                tableView = view
                break
        
        # 如果找不到视图，则创建新视图
        if tableView is None:
            # 获取详图视图族类型
            collector = FilteredElementCollector(doc).OfClass(ViewFamilyType)
            viewFamilyType = None
            for vft in collector:
                if vft.ViewFamily == ViewFamily.Detail:
                    viewFamilyType = vft
                    break
            
            if viewFamilyType is None:
                raise Exception("未找到详图视图族类型")
            
            # 创建视图的边界框
            bbox = BoundingBoxXYZ()
            bbox.Transform = Transform.Identity
            bbox.Min = XYZ(-1, -1, -1)
            bbox.Max = XYZ(col1_width + col2_width + col3_width + 1,
                          row_height * (len(levels) + 2) + 1, 1)
            bbox.Enabled = True
            
            # 创建新视图
            tableView = ViewSection.CreateDetail(doc, viewFamilyType.Id, bbox)
            tableView.Name = view_name
        
        # 清除视图中现有的所有元素
        collector = FilteredElementCollector(doc, tableView.Id).WhereElementIsNotElementType()
        elementIds = [elem.Id for elem in collector 
                    if elem.Category is not None  # 跳过没有类别的元素
                    and elem.Category.AllowsBoundParameters]  # 只删除可编辑的元素
        if elementIds:
            doc.Delete(List[ElementId](elementIds))
        
        # 计算总宽度和高度
        total_width = col1_width + col2_width + col3_width
        total_height = row_height * (len(levels) + 2)  # 标题行 + 表头行 + 数据行
        
        # 绘制表格外框
        draw_rectangle(doc, tableView, XYZ(0, 0, 0), total_width, total_height)
        
        # 绘制水平分隔线
        for i in range(len(levels) + 2):
            y = i * row_height
            draw_line(doc, tableView, XYZ(0, y, 0), XYZ(total_width, y, 0))
        
        # 绘制垂直分隔线
        # 第一列（点位）后的分隔线 - 从底部到顶部
        x = col3_width
        draw_line(doc, tableView, XYZ(x, 0, 0), XYZ(x, total_height, 0))
        
        # 第二列（强电间）两侧的分隔线 - 只到第一行底部
        first_row_bottom = total_height - row_height
        x = col3_width  # 强电间左侧线
        draw_line(doc, tableView, XYZ(x, 0, 0), XYZ(x, first_row_bottom, 0))
        x += col2_width  # 强电间右侧线
        draw_line(doc, tableView, XYZ(x, 0, 0), XYZ(x, first_row_bottom, 0))
        
        # 最右侧分隔线 - 从底部到顶部
        x = col3_width + col2_width
        draw_line(doc, tableView, XYZ(x, 0, 0), XYZ(x, total_height, 0))
        
        # 创建文本注释选项
        textNoteOptions = TextNoteOptions()
        textNoteOptions.HorizontalAlignment = HorizontalTextAlignment.Center
        textNoteOptions.TypeId = doc.GetDefaultElementTypeId(ElementTypeGroup.TextNoteType)
        
        # 添加文字
        # 标题
        add_text(doc, tableView, title_name, 
                XYZ(total_width/2, total_height - row_height/2, 0), 
                textNoteOptions)
        
        # 表头（从左到右）
        y = total_height - row_height * 2
        add_text(doc, tableView, "点位", 
                XYZ(col3_width/2, y + row_height/2, 0), 
                textNoteOptions)
        add_text(doc, tableView, "强电间", 
                XYZ(col3_width + col2_width/2, y + row_height/2, 0), 
                textNoteOptions)
        add_text(doc, tableView, "标高", 
                XYZ(col3_width + col2_width + col1_width/2, y + row_height/2, 0), 
                textNoteOptions)
        
        # 对标高进行排序（从高到低）
        sorted_pairs = sorted(zip(levels, level_names), reverse=True)
        sorted_levels, sorted_names = zip(*sorted_pairs)
        
        # 填充数据行并放置图例
        for i, (level_value, level_name) in enumerate(zip(sorted_levels, sorted_names)):
            row_y = total_height - row_height * (i + 3)
            
            # 放置点位图例（第一列）
            if point_symbol:
                x_position = col3_width/2
                place_family_symbol(doc, tableView, point_symbol,
                                 XYZ(x_position, row_y + row_height/2, 0))
            
            # 放置强电间图例（第二列）
            if electrical_room_symbol:
                x_position = col3_width + col2_width/2
                place_family_symbol(doc, tableView, electrical_room_symbol, 
                                 XYZ(x_position, row_y + row_height/2, 0))
            
            # 放置标高族（第三列）并设置其标高值和名称
            if level_symbol:
                x_position = col3_width + col2_width + col1_width/2
                
                # 创建实例
                instance = place_family_symbol(doc, tableView, level_symbol,
                                          XYZ(x_position, row_y + row_height/2, 0))
                                          
                # 使用指定模式设置参数
                if instance:
                    # 尝试不同的参数名称设置标高值（作为文本）
                    formatted_level = "%.3f" % float(level_value)  # 使用旧式字符串格式化
                    for param_name in ['标高', 'ELEVATION_PARAM', 'Elevation', 'Level']:
                        param = instance.LookupParameter(param_name)
                        if param and not param.IsReadOnly:
                            param.Set(formatted_level)
                            break
                    
                    # 尝试不同的参数名称设置名称
                    for param_name in ['名称', 'Name', 'TEXT_PARAM']:
                        param = instance.LookupParameter(param_name)
                        if param and not param.IsReadOnly:
                            param.Set(str(level_name))
                            break
        
        transaction.Commit()
        return tableView
        
    except Exception as e:
        transaction.RollBack()
        raise e

def draw_rectangle(doc, view, origin, width, height):
    """绘制矩形"""
    points = [
        origin,
        XYZ(origin.X + width, origin.Y, origin.Z),
        XYZ(origin.X + width, origin.Y + height, origin.Z),
        XYZ(origin.X, origin.Y + height, origin.Z),
        origin
    ]
    for i in range(4):
        draw_line(doc, view, points[i], points[i+1])

def draw_line(doc, view, start, end):
    """绘制直线"""
    line = Line.CreateBound(start, end)
    doc.Create.NewDetailCurve(view, line)

def add_text(doc, view, text, position, textNoteOptions):
    """添加文字"""
    TextNote.Create(doc, view.Id, position, text, textNoteOptions)

def place_family_symbol(doc, view, symbol, position):
    """放置实例
    doc: 当前文档
    view: 目标视图
    symbol: 要放置的族类型 (FamilyType)
    position: 放置位置
    """
    if symbol:
        family_symbol = UnwrapElement(symbol)
        instance = doc.Create.NewFamilyInstance(position, family_symbol, view)
        return instance

# 使用示例：
doc = DocumentManager.Instance.CurrentDBDocument
title = IN[0]
col_widths = IN[1]  # [标高列宽, 强电间列宽, 点位列宽]
row_height = IN[2]
levels = IN[3]
level_names = IN[4]
level_symbol = IN[5]          # 直接使用输入的 Family Type
electrical_room_symbol = IN[6] # 直接使用输入的 Family Type
point_symbol = IN[7]          # 直接使用输入的 Family Type

# 调用函数
table = create_table(doc, title, col_widths[0], col_widths[1], col_widths[2], 
                    row_height, levels, level_names, 
                    level_symbol, electrical_room_symbol, point_symbol,
                    "设备布置表")

OUT = table
