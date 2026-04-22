using System;
using DG.Tweening;
using Framework;
using Framework.Modules.UI;
using Game.Gameplay.Demo1.Command;
using Game.Gameplay.Demo1.System;
using UnityEngine;
using Game.Gameplay.Demo1;

[GameState(GameState.Shop)]
public partial class UI_ShopPanel : UIPanel
{
    partial void InitComponents()
    {
        BtnQuit.onClick.AddListener(() =>
        {
            this.SendCommand(new QuitEncounterCommand());
        });

        BtnRefresh.onClick.AddListener(OnRefreshClicked);
    }

    override public void OnOpen(object data = null)
    {
        LoadShopPurchase();
        UpdateRefreshButton();
        CanvasGroup.interactable = true;
        CanvasGroup.blocksRaycasts = true;
        CanvasGroup.alpha = 0;
        DOTween.To(() => CanvasGroup.alpha, x => CanvasGroup.alpha = x, 1, .5f);
    }

    private void LoadShopPurchase()
    {
        var shopPurchaseSystem = this.GetSystem<IShopPurchaseSystem>();
        shopPurchaseSystem.OpenShop();
        var cards = shopPurchaseSystem.GetCurrentShopCards();
        ShopZone.LoadCards(cards);
    }

    private void OnRefreshClicked()
    {
        var shopPurchaseSystem = this.GetSystem<IShopPurchaseSystem>();
        if (shopPurchaseSystem.RefreshShop())
        {
            var cards = shopPurchaseSystem.GetCurrentShopCards();
            ShopZone.LoadCards(cards);
        }
        UpdateRefreshButton();
    }

    private void UpdateRefreshButton()
    {
        var shopPurchaseSystem = this.GetSystem<IShopPurchaseSystem>();
        BtnRefresh.gameObject.SetActive(shopPurchaseSystem.RemainingRefreshCount > 0);
    }

    override public void OnClose()
    {
        CanvasGroup.interactable = false;
        CanvasGroup.blocksRaycasts = false;
        DOTween.To(() => CanvasGroup.alpha, x => CanvasGroup.alpha = x, 0, .5f);
    }
}
