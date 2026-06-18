# Fix Toolbar Icon Sorting & Size

## Goal

修复两个工具栏 bug：
1. 图标排序在切换位置时颠倒（再点一次恢复）
2. 在 Top/Bottom 位置时图标变形（横向拉长，纵向压扁）

## Bug 1: Icon Sorting Reversal

### Root Cause

`ToolbarPositionManagerButton.GetToolbarOrder()` (line 428) 用反射查找 `ToolbarOrder` 属性，但 `BindingFlags` 只指定了 `NonPublic`，而 `ToolbarOrder` 是 **public** 属性。反射找不到 → 始终返回 0 → 排序变成 no-op。

同时 `CollectItemsFromCustomToolbar` 和 `CollectItemsFromGrasshopperToolbar` 都从后往前遍历（`for i = Count-1; i >= 0; i--`），收集结果天然反序。因为排序不生效，移动一次就反转，再移动一次就恢复。

### Fix

直接访问 `button.ToolbarOrder`，删除不必要的反射代码。

## Bug 2: Icon Size Distortion on Top/Bottom

### Root Cause

`MoveItemsToCustomToolbar` 先调用 `CustomMotionToolbar.SetPosition(position)` 再调用 `ConfigureCustomToolbarAppearance(position)`，后者覆盖了前者的尺寸设置。

| 位置 | SetPosition 设的高/宽 | ConfigureCustomToolbarAppearance 覆盖后 |
|---|---|---|
| Left/Right | width = `iconSize + 16` = **40** | width = `size + 10` = **40** ✓ |
| Top/Bottom | height = `iconSize + 16` = **40** | height = `size` = **30** ✗ |

Top/Bottom 最终高度被覆盖为 **30px**，而图标是 24x24。30px 对 24px 图标加上 ToolStrip 内部 padding 来说太紧了，WinForms 会压缩图标的纵向尺寸来适应，导致图标变形。

Left/Right 的宽度一致保持 40px，所以正常。

### Fix

`ConfigureCustomToolbarAppearance` 中 Top/Bottom 的 height 应该和 Left/Right 的 width 一致，使用 `size + 10`（40px），或者直接删除 `ConfigureCustomToolbarAppearance` 的重复设置，只保留 `SetPosition` 的调用。

## Requirements

1. 删除 `GetToolbarOrder` 反射方法，改为直接访问 `button.ToolbarOrder`
2. 统一 Top/Bottom 和 Left/Right 的图标空间（40px），消除尺寸覆盖问题

## Acceptance Criteria

- [ ] 工具栏图标在任何位置（Top/Left/Right/Bottom/OnToolbar）都保持正确排序
- [ ] 多次点击同一位置菜单项不会导致排序翻转
- [ ] Top/Bottom 位置的图标和 Left/Right 保持相同的正方形比例（24x24），不变形
- [ ] 启动时加载保存的位置设置后排序和图标大小均正确
