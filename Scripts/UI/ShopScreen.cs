// Scripts/UI/ShopScreen.cs
// Loja do comerciante de rua. Solo: paga com a carteira. Online: o servidor desconta do dinheiro do grupo.
using System.Linq;
using Godot;

namespace DeadFrequency;

public partial class ShopScreen : ModalScreen
{
    [Export] public DeliveryManager Delivery { get; set; }

    public static ShopScreen Instance { get; private set; }

    private Label _title;
    private Label _money;
    private Label _feedback;
    private VBoxContainer _list;
    private AudioStreamPlayer _buySound;

    public override void _EnterTree() => Instance = this;

    protected override void BuildUi()
    {
        PanelContainer panel = CenteredPanel(new Vector2(820, 0));
        var stack = new VBoxContainer();
        stack.AddThemeConstantOverride("separation", 10);
        panel.AddChild(stack);

        _title = UiStyle.MakeLabel("BARRACA", 28);
        stack.AddChild(_title);
        stack.AddChild(UiStyle.MakeLabel("\"Tudo testado, tudo funcionando. Quase tudo.\"", 15, color: UiStyle.PhosphorDim));
        _list = new VBoxContainer();
        _list.AddThemeConstantOverride("separation", 6);
        stack.AddChild(_list);

        _feedback = UiStyle.MakeLabel("", 16, color: UiStyle.Warning);
        stack.AddChild(_feedback);
        _money = UiStyle.MakeLabel("", 22);
        stack.AddChild(_money);
        Button close = UiStyle.MakeButton("Fechar", Close, 220, UiSound.Back);
        close.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
        stack.AddChild(close);

        _buySound = new AudioStreamPlayer { Bus = AudioManager.UiBus };
        if (ResourceLoader.Exists("res://Audio/SFX/buy.wav"))
            _buySound.Stream = GD.Load<AudioStream>("res://Audio/SFX/buy.wav");
        AddChild(_buySound);

        if (PlayerInventory.Instance != null)
            PlayerInventory.Instance.Changed += Refresh;
    }

    public override void _ExitTree()
    {
        if (PlayerInventory.Instance != null)
            PlayerInventory.Instance.Changed -= Refresh;
        if (Instance == this)
            Instance = null;
        base._ExitTree();
    }

    /// <summary>Só comida e bebida (Churrasquinho do Duvidoso).</summary>
    private bool _foodOnly;

    public void OpenFor(string vendorName, bool foodOnly = false)
    {
        _foodOnly = foodOnly;
        _title.Text = vendorName.ToUpperInvariant();
        _feedback.Text = "";
        Open();
    }

    protected override void OnOpened() => Refresh();

    public override void _Process(double delta)
    {
        // Online o dinheiro do grupo pode mudar enquanto a loja está aberta.
        if (IsOpen)
            _money.Text = (Online ? "Dinheiro do grupo: $ " : "Carteira: $ ") + (Delivery?.Money ?? 0);
    }

    private void Refresh()
    {
        if (_list == null)
            return;
        foreach (Node child in _list.GetChildren())
            child.QueueFree();

        foreach (ItemDef def in ItemCatalog.All)
        {
            if (def.Hidden || (_foodOnly && !def.Food))
                continue;
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 12);

            var info = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
            Label name = UiStyle.MakeLabel(def.Name + (def.IsGear ? $"  [{ItemCatalog.SlotName(def.Slot)}]" : ""), 18, HorizontalAlignment.Left, def.Color);
            Label desc = UiStyle.MakeLabel(def.Description, 13, HorizontalAlignment.Left, UiStyle.PhosphorDim);
            desc.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            desc.CustomMinimumSize = new Vector2(520, 0);
            info.AddChild(name);
            info.AddChild(desc);
            row.AddChild(info);

            bool owned = def.IsGear && (PlayerInventory.Instance?.IsEquipped(def.Id) == true || (PlayerInventory.Instance?.Items.Contains(def.Id) ?? false));
            string id = def.Id;
            Button buy = UiStyle.MakeButton(owned ? "Já tem" : $"$ {def.Price}", () => Buy(id), 120);
            buy.Disabled = owned;
            row.AddChild(buy);
            _list.AddChild(row);
        }
    }

    private bool HasSpaceFor(ItemDef def)
    {
        PlayerInventory inv = PlayerInventory.Instance;
        if (inv == null)
            return false;
        if (def.IsGear && !inv.Equipped.ContainsKey(def.Slot))
            return true;
        return !inv.IsFull;
    }

    private void Buy(string itemId)
    {
        ItemDef def = ItemCatalog.Get(itemId);
        if (def == null || Delivery == null)
            return;
        if (!HasSpaceFor(def))
        {
            _feedback.Text = "Mochila cheia. Descarte algo ou compre a mochila maior.";
            return;
        }
        if (Delivery.Money < def.Price)
        {
            _feedback.Text = "Dinheiro insuficiente.";
            return;
        }

        if (Online)
        {
            if (NetSync.Instance == null)
                return;
            _feedback.Text = "Comprando...";
            NetSync.Instance.RequestPurchase(itemId, ok => Delivered(def, ok));
            return;
        }

        if (!Delivery.TrySpendLocal(def.Price))
        {
            _feedback.Text = "Dinheiro insuficiente.";
            return;
        }
        Delivered(def, true);
    }

    private void Delivered(ItemDef def, bool ok)
    {
        if (!ok)
        {
            _feedback.Text = "O servidor recusou a compra (dinheiro do grupo insuficiente?).";
            return;
        }
        PlayerInventory.Instance?.TryAdd(def.Id);
        _buySound.Play();
        _feedback.Text = def.IsGear ? $"{def.Name} comprado e equipado (veja no inventário: I)." : $"{def.Name} foi para a mochila.";
        Refresh();
    }
}
