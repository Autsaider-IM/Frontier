using Content.Shared._NF.Bank.Components;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._NF.Bank;

[NetSerializable, Serializable]
public enum BankATMMenuUiKey : byte
{
    ATM,
    BlackMarket
}

public abstract partial class SharedBankSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BankATMComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<BankATMComponent, ComponentRemove>(OnComponentRemove);
        SubscribeLocalEvent<StationBankATMComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<StationBankATMComponent, ComponentRemove>(OnComponentRemove);
    }
    // Corvax Frontier
    public void SetBalance(EntityUid uid, int amount)
    {
        if (!TryComp<BankAccountComponent>(uid, out var account))
            return;

        account.Balance = amount;
    }
    // Corvax Frontier
    private void OnComponentInit(EntityUid uid, BankATMComponent component, ComponentInit args)
    {
        _itemSlotsSystem.AddItemSlot(uid, BankATMComponent.CashSlotId, component.CashSlot);
    }

    private void OnComponentRemove(EntityUid uid, BankATMComponent component, ComponentRemove args)
    {
        _itemSlotsSystem.RemoveItemSlot(uid, component.CashSlot);
    }

    private void OnComponentInit(EntityUid uid, StationBankATMComponent component, ComponentInit args)
    {
        _itemSlotsSystem.AddItemSlot(uid, StationBankATMComponent.CashSlotId, component.CashSlot);
    }

    private void OnComponentRemove(EntityUid uid, StationBankATMComponent component, ComponentRemove args)
    {
        _itemSlotsSystem.RemoveItemSlot(uid, component.CashSlot);
    }
}

