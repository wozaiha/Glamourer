using Dalamud.Interface;
using Dalamud.Interface.Internal.Notifications;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Glamourer.Designs;
using Glamourer.Gui.Customization;
using Glamourer.Gui.Equipment;
using Glamourer.Gui.Tabs.DesignTab;
using Glamourer.Interop;
using Glamourer.State;
using ImGuiNET;
using OtterGui;
using OtterGui.Classes;
using OtterGui.Raii;
using Penumbra.GameData.Enums;

namespace Glamourer.Gui.Tabs.NpcTab;

public class NpcPanel(
    NpcSelector _selector,
    LocalNpcAppearanceData _favorites,
    CustomizationDrawer _customizeDrawer,
    EquipmentDrawer _equipDrawer,
    DesignConverter _converter,
    DesignManager _designManager,
    StateManager _state,
    ObjectManager _objects,
    DesignColors _colors)
{
    private readonly DesignColorCombo _colorCombo = new(_colors, true);
    private          string           _newName    = string.Empty;
    private          DesignBase?      _newDesign;

    public void Draw()
    {
        using var group = ImRaii.Group();

        DrawHeader();
        DrawPanel();
    }

    private void DrawHeader()
    {
        HeaderDrawer.Draw(_selector.HasSelection ? _selector.Selection.Name : "未选择", ColorId.NormalDesign.Value(),
            ImGui.GetColorU32(ImGuiCol.FrameBg), 2, ExportToClipboardButton(), SaveAsDesignButton(), FavoriteButton());
        SaveDesignDrawPopup();
    }

    private HeaderDrawer.Button FavoriteButton()
    {
        var (desc, color) = _favorites.IsFavorite(_selector.Selection)
            ? ("从你的收藏中删除此NPC外观。", ColorId.FavoriteStarOn.Value())
            : ("将此NPC外观添加到你的收藏中。", 0x80000000);
        return new HeaderDrawer.Button
        {
            Icon        = FontAwesomeIcon.Star,
            OnClick     = () => _favorites.ToggleFavorite(_selector.Selection),
            Visible     = _selector.HasSelection,
            Description = desc,
            TextColor   = color,
        };
    }

    private HeaderDrawer.Button ExportToClipboardButton()
        => new()
        {
            Description =
                "将当前NPC外观复制到剪贴板。\n按住Ctrl禁止复制外貌。\n按住Shift可禁止复制装备。",
            Icon    = FontAwesomeIcon.Copy,
            OnClick = ExportToClipboard,
            Visible = _selector.HasSelection,
        };

    private HeaderDrawer.Button SaveAsDesignButton()
        => new()
        {
            Description =
                "将此NPC外观保存为设计。\n按住Ctrl禁止保存外貌。\n按住Shift可禁止保存装备。",
            Icon    = FontAwesomeIcon.Save,
            OnClick = SaveDesignOpen,
            Visible = _selector.HasSelection,
        };

    private void ExportToClipboard()
    {
        try
        {
            var data = ToDesignData();
            var text = _converter.ShareBase64(data, new StateMaterialManager(), ApplicationRules.NpcFromModifiers());
            ImGui.SetClipboardText(text);
        }
        catch (Exception ex)
        {
            Glamourer.Messager.NotificationMessage(ex, $"无法复制{_selector.Selection.Name}的数据到剪贴板。",
                $"无法从NPC外观复制数据{_selector.Selection.Kind} {_selector.Selection.Id.Id}到剪贴板",
                NotificationType.Error);
        }
    }

    private void SaveDesignOpen()
    {
        ImGui.OpenPopup("另存为设计");
        _newName = _selector.Selection.Name;
        var data = ToDesignData();
        _newDesign = _converter.Convert(data, new StateMaterialManager(), ApplicationRules.NpcFromModifiers());
    }

    private void SaveDesignDrawPopup()
    {
        if (!ImGuiUtil.OpenNameField("另存为设计", ref _newName))
            return;

        if (_newDesign != null && _newName.Length > 0)
            _designManager.CreateClone(_newDesign, _newName, true);
        _newDesign = null;
        _newName   = string.Empty;
    }

    private void DrawPanel()
    {
        using var child = ImRaii.Child("##Panel", -Vector2.One, true);
        if (!child || !_selector.HasSelection)
            return;

        DrawButtonRow();
        DrawCustomization();
        DrawEquipment();
        DrawAppearanceInfo();
    }

    private void DrawButtonRow()
    {
        DrawApplyToSelf();
        ImGui.SameLine();
        DrawApplyToTarget();
    }

    private void DrawCustomization()
    {
        var header = _selector.Selection.ModelId == 0
            ? "外貌"
            : $"外貌（模型Id #{_selector.Selection.ModelId}）###Customization";
        using var h = ImRaii.CollapsingHeader(header);
        if (!h)
            return;

        _customizeDrawer.Draw(_selector.Selection.Customize, true, true);
        ImGui.Dummy(new Vector2(ImGui.GetTextLineHeight() / 2));
    }

    private void DrawEquipment()
    {
        using var h = ImRaii.CollapsingHeader("装备");
        if (!h)
            return;

        _equipDrawer.Prepare();
        var designData = ToDesignData();

        foreach (var slot in EquipSlotExtensions.EqdpSlots)
        {
            var data = new EquipDrawData(slot, designData) { Locked = true };
            _equipDrawer.DrawEquip(data);
        }

        var mainhandData = new EquipDrawData(EquipSlot.MainHand, designData) { Locked = true };
        var offhandData  = new EquipDrawData(EquipSlot.OffHand,  designData) { Locked = true };
        _equipDrawer.DrawWeapons(mainhandData, offhandData, false);

        ImGui.Dummy(new Vector2(ImGui.GetTextLineHeight() / 2));
        EquipmentDrawer.DrawMetaToggle(ToggleDrawData.FromValue(MetaIndex.VisorState, _selector.Selection.VisorToggled));
        ImGui.Dummy(new Vector2(ImGui.GetTextLineHeight() / 2));
    }

    private DesignData ToDesignData()
    {
        var selection  = _selector.Selection;
        var items      = _converter.FromDrawData(selection.Equip.ToArray(), selection.Mainhand, selection.Offhand, true).ToArray();
        var designData = new DesignData { Customize = selection.Customize };
        foreach (var (slot, item, stain) in items)
        {
            designData.SetItem(slot, item);
            designData.SetStain(slot, stain);
        }

        return designData;
    }

    private void DrawApplyToSelf()
    {
        var (id, data) = _objects.PlayerData;
        if (!ImGuiUtil.DrawDisabledButton("应用到自己", Vector2.Zero,
                "将当前NPC外观应用于你的角色。\n按住Ctrl仅应用装备。\n按住Shift仅应用外貌。",
                !data.Valid))
            return;

        if (_state.GetOrCreate(id, data.Objects[0], out var state))
        {
            var design = _converter.Convert(ToDesignData(), new StateMaterialManager(), ApplicationRules.NpcFromModifiers());
            _state.ApplyDesign(state, design, ApplySettings.Manual);
        }
    }

    private void DrawApplyToTarget()
    {
        var (id, data) = _objects.TargetData;
        var tt = id.IsValid
            ? data.Valid
                ? "将当前NPC外观应用于你的目标。\n按住Ctrl仅应用装备。\n按住Shift仅应用外貌。"
                : "当前目标无法操作。"
            : "未选择有效的目标。";
        if (!ImGuiUtil.DrawDisabledButton("应用到目标", Vector2.Zero, tt, !data.Valid))
            return;

        if (_state.GetOrCreate(id, data.Objects[0], out var state))
        {
            var design = _converter.Convert(ToDesignData(), new StateMaterialManager(), ApplicationRules.NpcFromModifiers());
            _state.ApplyDesign(state, design, ApplySettings.Manual);
        }
    }


    private void DrawAppearanceInfo()
    {
        using var h = ImRaii.CollapsingHeader("外观详情");
        if (!h)
            return;

        using var table = ImRaii.Table("Details", 2);
        if (!table)
            return;

        using var style = ImRaii.PushStyle(ImGuiStyleVar.ButtonTextAlign, new Vector2(0, 0.5f));
        ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize("Last Update Datem").X);
        ImGui.TableSetupColumn("Data", ImGuiTableColumnFlags.WidthStretch);

        var selection = _selector.Selection;
        CopyButton("NPC名称", selection.Name);
        CopyButton("NPC ID",   selection.Id.Id.ToString());
        ImGuiUtil.DrawFrameColumn("NPC类型");
        ImGui.TableNextColumn();
        var width = ImGui.GetContentRegionAvail().X;
        ImGuiUtil.DrawTextButton(selection.Kind is ObjectKind.BattleNpc ? "战斗NPC" : "事件NPC", new Vector2(width, 0),
            ImGui.GetColorU32(ImGuiCol.FrameBg));

        ImGuiUtil.DrawFrameColumn("配色");
        var color     = _favorites.GetColor(selection);
        var colorName = color.Length == 0 ? DesignColors.AutomaticName : color;
        ImGui.TableNextColumn();
        if (_colorCombo.Draw("##colorCombo", colorName,
                "将颜色与此NPC外观相关联。\n"
              + "右键单击可恢复为自动配色。\n"
              + "按住Ctrl并滚动鼠标滚轮进行滚动选择。",
                width - ImGui.GetStyle().ItemSpacing.X - ImGui.GetFrameHeight(), ImGui.GetTextLineHeight())
         && _colorCombo.CurrentSelection != null)
        {
            color = _colorCombo.CurrentSelection is DesignColors.AutomaticName ? string.Empty : _colorCombo.CurrentSelection;
            _favorites.SetColor(selection, color);
        }

        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
        {
            _favorites.SetColor(selection, string.Empty);
            color = string.Empty;
        }

        if (_colors.TryGetValue(color, out var currentColor))
        {
            ImGui.SameLine();
            if (DesignColorUi.DrawColorButton($"Color associated with {color}", currentColor, out var newColor))
                _colors.SetColor(color, newColor);
        }
        else if (color.Length != 0)
        {
            ImGui.SameLine();
            var       size = new Vector2(ImGui.GetFrameHeight());
            using var font = ImRaii.PushFont(UiBuilder.IconFont);
            ImGuiUtil.DrawTextButton(FontAwesomeIcon.ExclamationCircle.ToIconString(), size, 0, _colors.MissingColor);
            ImGuiUtil.HoverTooltip("与此设计相关联的颜色不存在。");
        }

        return;

        static void CopyButton(string label, string text)
        {
            ImGuiUtil.DrawFrameColumn(label);
            ImGui.TableNextColumn();
            if (ImGui.Button(text, new Vector2(ImGui.GetContentRegionAvail().X, 0)))
                ImGui.SetClipboardText(text);
            ImGuiUtil.HoverTooltip("Click to copy to clipboard.");
        }
    }
}
