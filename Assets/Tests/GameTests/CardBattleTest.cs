using System.Collections;
using System.Collections.Generic;
using Framework.Modules.UI;
using Framework.Utils;
using Game;
using Game.Gameplay.CardBattle;
using UnityEngine;

public class CardBattleTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    [Button]
    public void OpenCardBattlePanel()
    {
        // 1. 启动架构 (这会调用 RegisterModule)
        CardBattleArchitecture.Launch();

        var arch = CardBattleArchitecture.Instance;
        var model = arch.GetModel<BattleModel>();

        // 2. 初始化一些模拟数据，防止 UI 报错
        if (model.Player == null)
        {
            model.Player = new EntityData("P1", EntityType.Player, "勇者", 100);
            model.Enemies.Add(new EntityData("E1", EntityType.Enemy, "小史莱姆", 20));
        }

        // 3. 给抽牌堆塞点牌，否则没牌可抽
        if (model.DrawPile.Count == 0)
        {
            for (int i = 0; i < 10; i++)
            {
                var isDamage = i % 2 == 0;
                var card = new CardData 
                { 
                    Name = isDamage ? "斩击" : "防御",
                    TargetType = isDamage ? CardTargetType.SingleEnemy : CardTargetType.Self
                };

                // 给测试牌添加真正的逻辑效果
                if (isDamage)
                {
                    card.OnPlayEffects.Add(new DealDamageEffect { BaseDamage = 6 });
                }
                else
                {
                    card.OnPlayEffects.Add(new GainBlockEffect { BaseBlock = 5 });
                }

                model.DrawPile.Add(card);
            }
        }

        // 4. 仅仅打开 UI (不再自动开始)
        GameArchitecture.Instance.GetSystem<IUISystem>().Open<UI_CardBattlePanel>(arch);
    }

    [Button]
    public void StartBattle()
    {
        // 5. 点击第二个按钮才开启对局
        var arch = CardBattleArchitecture.Instance;
        arch.SendCommand(this, new StartBattleCommand()); 
    }
}
