// Scripts/UI/InventoryScreen.cs
// Inventário (I ou Tab): o boneco do entregador com o que está equipado, os 4 slots de
// equipamento e a mochila. Clique num item para equipar/usar ou descartar.
using Godot;

namespace DeadFrequency;

public partial class InventoryScreen : ModalScreen
{
    private CharacterModel _preview;
    private readonly System.Collections.Generic.Dictionary<ItemSlot, Button> _slotButtons = new();
    private GridContainer _grid;
    private Label _bagTitle;
    private Label _details;
    private Label _feedback;
    private Label _money;
    private Button _useButton;
    private Button _dropButton;
    private int _selected = -1;

    private static PlayerInventory Inv => PlayerInventory.Instance;

    private Label _homeLabel;
    private Button _homeButton;

    private void ToggleHome()
    {
        HomeGuide.Active = !HomeGuide.Active && HomeGuide.Available;
        RefreshHome();
        if (HomeGuide.Active)
            GameHud.Toast(this, "Casa marcada: siga a seta no topo da tela.", new Color(1f, 0.85f, 0.5f), 3.0);
    }

    private void RefreshHome()
    {
        if (_homeLabel == null)
            return;
        _homeLabel.Text = HomeGuide.Available ? $"SEU ENDEREÇO: {HomeGuide.Address}" : "Sem casa nesta fase (os apartamentos ficam na cidade).";
        _homeButton.Disabled = !HomeGuide.Available;
        _homeButton.Text = HomeGuide.Active ? "Desmarcar casa" : "Marcar casa";
    }

    protected override void BuildUi()
    {
        PanelContainer panel = CenteredPanel(new Vector2(940, 540));
        var columns = new HBoxContainer();
        columns.AddThemeConstantOverride("separation", 24);
        panel.AddChild(columns);

        // ---- Esquerda: boneco + equipamentos
        var left = new VBoxContainer();
        left.AddThemeConstantOverride("separation", 8);
        columns.AddChild(left);
        left.AddChild(UiStyle.MakeLabel("ENTREGADOR", 26));

        var viewportContainer = new SubViewportContainer { CustomMinimumSize = new Vector2(280, 300), Stretch = true };
        left.AddChild(viewportContainer);
        var viewport = new SubViewport { OwnWorld3D = true, TransparentBg = true, Size = new Vector2I(280, 300), RenderTargetUpdateMode = SubViewport.UpdateMode.WhenVisible };
        viewportContainer.AddChild(viewport);
        _preview = CharacterModel.Create(new Color(0.2f, 0.25f, 0.22f), new Color(0.12f, 0.13f, 0.18f), new Color(0.76f, 0.58f, 0.44f), UiStyle.Phosphor);
        viewport.AddChild(_preview);
        viewport.AddChild(new Camera3D { Position = new Vector3(0f, 1.15f, -2.6f), RotationDegrees = new Vector3(-4f, 180f, 0f), Fov = 45f, Current = true });
        viewport.AddChild(new DirectionalLight3D { RotationDegrees = new Vector3(-35f, 150f, 0f), LightEnergy = 1.4f });
        viewport.AddChild(new WorldEnvironment
        {
            Environment = new Environment { BackgroundMode = Environment.BGMode.ClearColor, AmbientLightSource = Environment.AmbientSource.Color, AmbientLightColor = new Color(0.5f, 0.6f, 0.55f), AmbientLightEnergy = 0.8f },
        });

        foreach (ItemSlot slot in new[] { ItemSlot.Head, ItemSlot.Hands, ItemSlot.Back, ItemSlot.Wrist, ItemSlot.Feet })
        {
            ItemSlot captured = slot;
            Button button = UiStyle.MakeButton("", () => OnSlotPressed(captured), 280);
            button.CustomMinimumSize = new Vector2(280, 38);
            button.AddThemeFontSizeOverride("font_size", 16);
            button.Alignment = HorizontalAlignment.Left;
            left.AddChild(button);
            _slotButtons[slot] = button;
        }

        // ---- Direita: mochila
        var right = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        right.AddThemeConstantOverride("separation", 10);
        columns.AddChild(right);
        _bagTitle = UiStyle.MakeLabel("MOCHILA", 26, HorizontalAlignment.Left);
        right.AddChild(_bagTitle);
        _grid = new GridContainer { Columns = 5 };
        _grid.AddThemeConstantOverride("h_separation", 8);
        _grid.AddThemeConstantOverride("v_separation", 8);
        right.AddChild(_grid);

        _details = UiStyle.MakeLabel("Selecione um item.", 16, HorizontalAlignment.Left, UiStyle.PhosphorDim);
        _details.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _details.CustomMinimumSize = new Vector2(560, 70);
        right.AddChild(_details);

        var actions = new HBoxContainer();
        actions.AddThemeConstantOverride("separation", 10);
        _useButton = UiStyle.MakeButton("Equipar", OnUsePressed, 180);
        _dropButton = UiStyle.MakeButton("Descartar", OnDropPressed, 180, UiSound.Back);
        actions.AddChild(_useButton);
        actions.AddChild(_dropButton);
        right.AddChild(actions);

        _feedback = UiStyle.MakeLabel("", 16, HorizontalAlignment.Left, UiStyle.Warning);
        right.AddChild(_feedback);
        right.AddChild(new Control { SizeFlagsVertical = Control.SizeFlags.ExpandFill });
        _money = UiStyle.MakeLabel("", 22, HorizontalAlignment.Left);
        right.AddChild(_money);
        // Endereço de casa (apartamento no Edifício Sintonia, ganho ao entrar) e o botão fixo "Marcar casa".
        var home = new HBoxContainer();
        home.AddThemeConstantOverride("separation", 10);
        _homeLabel = UiStyle.MakeLabel("", 15, HorizontalAlignment.Left, new Color(1f, 0.85f, 0.5f));
        _homeLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _homeLabel.CustomMinimumSize = new Vector2(330, 0);
        _homeLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        home.AddChild(_homeLabel);
        _homeButton = UiStyle.MakeButton("Marcar casa", ToggleHome, 190);
        home.AddChild(_homeButton);
        right.AddChild(home);
        right.AddChild(UiStyle.MakeLabel("I / Tab / Esc fecha · clique no slot da esquerda para tirar o item", 14, HorizontalAlignment.Left, UiStyle.PhosphorDim));
        Button close = UiStyle.MakeButton("Fechar", Close, 180, UiSound.Back);
        right.AddChild(close);

        if (Inv != null)
            Inv.Changed += Refresh;
    }

    public override void _ExitTree()
    {
        if (Inv != null)
            Inv.Changed -= Refresh;
        base._ExitTree();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!@event.IsActionPressed("inventory"))
            return;
        // Não abre por cima do menu de pausa / loja.
        if (IsOpen)
            Close();
        else if (!AnyOpen && !GetTree().Paused)
            Open();
        GetViewport().SetInputAsHandled();
    }

    public override void _Process(double delta)
    {
        if (IsOpen && _preview != null)
            _preview.RotateY((float)delta * 0.6f);
    }

    protected override void OnOpened()
    {
        RefreshHome();
        _selected = -1;
        _feedback.Text = "";
        Refresh();
    }

    private void Refresh()
    {
        if (Inv == null || _grid == null)
            return;

        _preview.SetEquipment(Inv.EquipmentCsv());
        foreach ((ItemSlot slot, Button button) in _slotButtons)
        {
            string label = ItemCatalog.SlotName(slot);
            button.Text = Inv.Equipped.TryGetValue(slot, out string id) ? $"{label}: {ItemCatalog.Get(id)?.Name}" : $"{label}: —";
        }

        _bagTitle.Text = $"MOCHILA  {Inv.Items.Count}/{Inv.Capacity}";
        foreach (Node child in _grid.GetChildren())
            child.QueueFree();
        for (int i = 0; i < Inv.Capacity; i++)
        {
            int index = i;
            bool hasItem = i < Inv.Items.Count;
            ItemDef def = hasItem ? ItemCatalog.Get(Inv.Items[i]) : null;
            var button = new Button
            {
                Text = def != null ? ShortName(def.Name) : "·",
                CustomMinimumSize = new Vector2(104, 64),
                Disabled = !hasItem,
                ToggleMode = true,
                ButtonPressed = index == _selected,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            };
            button.AddThemeFontSizeOverride("font_size", 13);
            if (def != null)
                button.AddThemeColorOverride("font_color", def.Color);
            button.Pressed += () => Select(index);
            _grid.AddChild(button);
        }

        ItemDef selected = _selected >= 0 && _selected < Inv.Items.Count ? ItemCatalog.Get(Inv.Items[_selected]) : null;
        _useButton.Visible = selected != null;
        _dropButton.Visible = selected != null;
        if (selected != null)
        {
            _useButton.Text = selected.IsGear ? "Equipar" : "Usar";
            _details.Text = $"{selected.Name} ({ItemCatalog.SlotName(selected.Slot)})\n{selected.Description}";
        }
        else
        {
            _details.Text = Inv.Items.Count == 0 ? "Mochila vazia. Compre itens na barraca do comerciante." : "Selecione um item.";
        }

        int money = GetTree().GetFirstNodeInGroup("delivery_manager") is DeliveryManager dm ? dm.Money : Inv.Wallet;
        _money.Text = Online ? $"Dinheiro do grupo: $ {money}" : $"Carteira: $ {money}";
    }

    private static string ShortName(string name) => name.Length > 22 ? name[..21] + "…" : name;

    private void Select(int index)
    {
        _selected = index;
        _feedback.Text = "";
        Refresh();
    }

    private void OnUsePressed()
    {
        if (Inv == null || _selected < 0 || _selected >= Inv.Items.Count)
            return;
        ItemDef def = ItemCatalog.Get(Inv.Items[_selected]);
        if (def == null)
            return;
        if (def.IsGear)
        {
            _feedback.Text = Inv.Equip(_selected) ? $"{def.Name} equipado." : "Não deu para equipar.";
        }
        else
        {
            string result = Inv.Use(_selected, Player);
            _feedback.Text = string.IsNullOrEmpty(result)
                ? (def.Id == ItemCatalog.EnergyDrink ? "" : "Segure uma caixa para usar isso.")
                : result;
        }
        _selected = -1;
        Refresh();
    }

    private void OnDropPressed()
    {
        if (Inv == null || _selected < 0)
            return;
        Inv.RemoveAt(_selected);
        _selected = -1;
        _feedback.Text = "Item descartado.";
        Refresh();
    }

    private void OnSlotPressed(ItemSlot slot)
    {
        if (Inv == null || !Inv.Equipped.ContainsKey(slot))
            return;
        _feedback.Text = Inv.Unequip(slot) ? "Item guardado na mochila." : "Sem espaço na mochila para guardar.";
        Refresh();
    }
}
