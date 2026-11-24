# 使用 PowerShell 7 的獨立 .NET 程序測試購買／配送核心。
# Unity、主角與進度旗標使用最小替身；不編譯 Unity 專案、不啟動 Editor、不修改資產。
# 執行：pwsh -NoProfile -File Tests/ShopPurchaseRegression.ps1
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$sourceRoot = Join-Path $projectRoot 'Assets/Scripts/02_ShopItemInventory'
$sourceFiles = @(
    'Config/ItemConfigData.cs', 'Config/ItemCatalog.cs',
    'Model/ProtagonistInventoryModel.cs', 'Model/ShopStatusModel.cs',
    'Model/PendingDeliveryModel.cs', 'Model/DeliveryOrder.cs', 'Model/EffectContext.cs',
    'DTO/InventorySaveData.cs', 'DTO/ShopSaveData.cs', 'DTO/DeliverySaveData.cs',
    'Effects/ItemEffect.cs', 'Effects/PassiveEffects.cs', 'Effects/ProtagonistEffects.cs',
    'Effects/HeroineEffects.cs', 'Service/ShopService.cs'
) | ForEach-Object { Join-Path $sourceRoot $_ }

$testCode = @'
using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine
{
    public class ScriptableObject { public string name; }
    public class Sprite { }
    public class HeaderAttribute : Attribute { public HeaderAttribute(string value) { } }
    public class TooltipAttribute : Attribute { public TooltipAttribute(string value) { } }
    public class InspectorNameAttribute : Attribute { public InspectorNameAttribute(string value) { } }
    public class MinAttribute : Attribute { public MinAttribute(float value) { } }
    public class SerializeReference : Attribute { }
    public class CreateAssetMenuAttribute : Attribute { public string menuName; }
    public static class Debug
    {
        public static void Log(object value) { }
        public static void LogWarning(object value) { }
        public static void LogError(object value) { }
    }
    public static class Resources
    {
        public static object[] Items = Array.Empty<object>();
        public static T[] LoadAll<T>(string path) => Items.OfType<T>().ToArray();
    }
}
public class ProtagonistStatusModel
{
    public int Money = 1000;
    public int SkillPoints;
    public event Action<int> OnMoneyChanged;
    public void AddMoney(int value) { Money += value; OnMoneyChanged?.Invoke(Money); }
    public void AddSkillPoints(int value) { SkillPoints += value; }
    public bool TryReduceMoney(int cost)
    {
        if (cost < 0 || Money < cost) return false;
        AddMoney(-cost);
        return true;
    }
}
public class ProgressFlagDefinition { public string FlagID; }
public class ProgressFlagModel
{
    public HashSet<string> Flags = new HashSet<string>();
    public void AddPersistentFlag(string id) => Flags.Add(id);
    public void AddDailyFlag(string id) => Flags.Add(id);
    public void AddPhaseFlag(string id) => Flags.Add(id);
}
public class ItemDatabase
{
    public IEnumerable<ItemConfigData> AllItemsInAllTiers = Array.Empty<ItemConfigData>();
}
public struct ValueChangeRecord
{
    public bool isHeroineResource;
    public string resourceTypeKey;
    public int finalAmount;
    public ValueChangeResult.EffectResult effectResult;
    public string heroineNameKey;
}
public class ValueChangeResult { public enum EffectResult { Normal } }

public class ShopPurchaseFixture
{
    public ProtagonistStatusModel Player = new ProtagonistStatusModel();
    public ProtagonistInventoryModel Inventory = new ProtagonistInventoryModel();
    public ShopStatusModel Status = new ShopStatusModel();
    public PendingDeliveryModel Delivery = new PendingDeliveryModel();
    public ProgressFlagModel Flags = new ProgressFlagModel();
    public ItemConfigData Product = new ItemConfigData
    {
        name = "condom", Price = 100, PurchaseQuantity = 3, MaxPurchaseLimit = 2,
        DescriptionKey = "Item.Description", ShopDescriptionKey = "Store.Description"
    };
    public ItemConfigData Bonus = new ItemConfigData { name = "bonus" };
    public ShopService Shop;
    public ShopPurchaseFixture()
    {
        Bonus.AcquireEffects.Add(new AddMoneyEffect { Amount = 500 });
        Product.AcquireEffects.Add(new AddSkillPointsEffect { Amount = 1 });
        Product.AcquireEffects.Add(new GrantItemEffect { Item = Bonus, Amount = 2 });
        Product.AcquireEffects.Add(new ActivateFlagEffect
        {
            FlagToActivate = new ProgressFlagDefinition { FlagID = "bought" }
        });
        UnityEngine.Resources.Items = new object[] { Product, Bonus };
        Shop = new ShopService(Player, Inventory, Status, Delivery, new ItemCatalog(), Flags, () => 1);
    }
}

public static class ShopPurchaseRegression
{
    private static void Check(bool condition, string message)
    { if (!condition) throw new Exception(message); }

    public static string[] Run()
    {
        var passed = new List<string>();
        var f = new ShopPurchaseFixture();
        Check(f.Shop.TryPurchaseItem("condom") == PurchaseResult.Success, "現購失敗");
        Check(f.Player.Money == 900 && f.Inventory.GetItemCount("condom") == 3,
            "每份需只扣一次價格並入庫三個");
        Check(f.Player.SkillPoints == 1 && f.Inventory.GetItemCount("bonus") == 2 && f.Flags.Flags.Contains("bought"),
            "購得效果應每份一次");
        Check(f.Status.GetPurchasedCount("condom") == 1, "限購需以份數計算");
        Check(f.Shop.TryPurchaseItem("condom") == PurchaseResult.Success && f.Inventory.GetItemCount("condom") == 6,
            "第二份需累加為六個");
        Check(f.Shop.TryPurchaseItem("condom") == PurchaseResult.PurchaseLimitReached && f.Player.Money == 800,
            "達上限後不可扣款");
        passed.Add("現購數量、額外道具、旗標、每份一次效果與限購");

        f = new ShopPurchaseFixture();
        f.Product.Category = ItemCategory.Claim;
        Check(f.Shop.TryPurchaseItem("condom") == PurchaseResult.Success && f.Inventory.GetItemCount("condom") == 3,
            "分類不應改變入庫數量");
        f.Inventory.AddItem("condom", 5);
        f.Inventory.AddItem("bonus", 1);
        Check(f.Player.SkillPoints == 1 && f.Player.Money == 900 && f.Inventory.GetItemCount("bonus") == 3,
            "直接加庫存或額外道具不可觸發購得效果");
        passed.Add("分類無行為差異；一般入庫與贈品不連鎖觸發效果");

        f = new ShopPurchaseFixture();
        f.Product.IsOnlineOrder = true;
        Check(f.Shop.TryPurchaseItem("condom") == PurchaseResult.Success, "網購下單失敗");
        Check(f.Player.Money == 900 && f.Inventory.GetItemCount("condom") == 0 && f.Player.SkillPoints == 0,
            "網購下單不可提前入庫或套用效果");
        f.Delivery.ProcessDeliveries(1, f.Shop);
        Check(f.Inventory.GetItemCount("condom") == 0, "未到貨日卻已入庫");
        f.Product.PurchaseQuantity = 20;
        int arrived = 0;
        f.Delivery.OnDeliveryArrived += () => arrived++;
        f.Delivery.ProcessDeliveries(2, f.Shop);
        f.Delivery.ProcessDeliveries(2, f.Shop);
        Check(f.Inventory.GetItemCount("condom") == 3 && f.Player.SkillPoints == 1 &&
              f.Inventory.GetItemCount("bonus") == 2 && f.Player.Money == 900 &&
              f.Status.GetPurchasedCount("condom") == 1 && arrived == 1 && !f.Delivery.HasPendingOrders,
            "到貨應保留下單數量、只觸發一次且不再扣款／計次");
        passed.Add("網購延後觸發、下單數量快照、到貨不重複執行");

        f = new ShopPurchaseFixture();
        f.Delivery.PlaceOrder("condom", 6, 1, 2);
        f.Delivery.ProcessDeliveries(2, f.Shop);
        Check(f.Inventory.GetItemCount("condom") == 6 && f.Player.SkillPoints == 2 &&
              f.Inventory.GetItemCount("bonus") == 4, "多份訂單的效果次數不正確");
        passed.Add("多份配送依份數執行效果");

        f = new ShopPurchaseFixture();
        f.Product.Price = 1001;
        Check(f.Shop.TryPurchaseItem("condom") == PurchaseResult.NotEnoughCoins && f.Player.Money == 1000,
            "金錢不足不可扣款");
        Check(f.Shop.TryPurchaseItem("missing") == PurchaseResult.ItemNotFound, "未知道具需拒絕購買");
        f.Product.Price = -1;
        Check(f.Shop.TryPurchaseItem("condom") == PurchaseResult.InvalidConfiguration, "負價格需拒絕");
        f.Product.Price = 100;
        f.Product.PurchaseQuantity = 0;
        Check(f.Shop.TryPurchaseItem("condom") == PurchaseResult.InvalidConfiguration, "零數量需拒絕");
        f.Product.PurchaseQuantity = 3;
        f.Product.AcquireEffects.Add(new GrantItemEffect { Amount = 1 });
        Check(f.Shop.TryPurchaseItem("condom") == PurchaseResult.InvalidConfiguration &&
              f.Player.Money == 1000 && f.Status.GetPurchasedCount("condom") == 0,
            "無效贈品應在扣款前阻止交易");
        passed.Add("金錢不足、未知商品、無效價格／數量／贈品不扣款");

        f = new ShopPurchaseFixture();
        f.Product.AcquireEffects.Add(new GrantItemEffect { Item = f.Product, Amount = 2 });
        f.Inventory.SetItemCount("condom", int.MaxValue - 4);
        Check(f.Shop.TryPurchaseItem("condom") == PurchaseResult.InvalidConfiguration && f.Player.Money == 1000,
            "商品與同 ID 贈品合計需檢查整數溢位");
        passed.Add("同 ID 商品與贈品合計的庫存溢位防護");

        f = new ShopPurchaseFixture();
        PurchaseResult reentry = PurchaseResult.Success;
        f.Player.OnMoneyChanged += _ => reentry = f.Shop.TryPurchaseItem("condom");
        Check(f.Shop.TryPurchaseItem("condom") == PurchaseResult.Success &&
              reentry == PurchaseResult.PurchaseInProgress && f.Inventory.GetItemCount("condom") == 3,
            "金錢事件不可重入購買");
        f = new ShopPurchaseFixture();
        f.Delivery.PlaceOrder("condom", 3, 1, 1);
        f.Inventory.OnItemCountChanged += (_, __) => f.Delivery.ProcessDeliveries(2, f.Shop);
        f.Delivery.ProcessDeliveries(2, f.Shop);
        Check(f.Inventory.GetItemCount("condom") == 3 && f.Player.SkillPoints == 1,
            "入庫事件不可重入同筆配送");
        passed.Add("同步事件重入不重複購買／配送");

        f = new ShopPurchaseFixture();
        f.Delivery.PlaceOrder("condom", 3, 1, 1);
        f.Product.AcquireEffects.Add(new GrantItemEffect { Amount = 0 });
        f.Delivery.ProcessDeliveries(2, f.Shop);
        Check(f.Delivery.HasPendingOrders && f.Inventory.GetItemCount("condom") == 0,
            "無效配送需保留訂單且不可部分入庫");
        f.Product.AcquireEffects.RemoveAt(f.Product.AcquireEffects.Count - 1);
        f.Delivery.ProcessDeliveries(2, f.Shop);
        Check(!f.Delivery.HasPendingOrders && f.Inventory.GetItemCount("condom") == 3,
            "修正設定後應能正常配送");
        passed.Add("無效配送保留訂單，修復後可重新配送");

        f = new ShopPurchaseFixture();
        Check(f.Product.EffectiveShopDescriptionKey == "Store.Description" &&
              f.Product.DescriptionKey == "Item.Description", "商店與一般說明需分開");
        f.Product.ShopDescriptionKey = "  ";
        Check(f.Product.EffectiveShopDescriptionKey == "Item.Description", "空白商店說明需回退");
        passed.Add("商店專用說明與空白回退");
        return passed.ToArray();
    }
}
'@

$testStubPath = Join-Path ([System.IO.Path]::GetTempPath()) ('NHK-ShopRegression-' + [guid]::NewGuid() + '.cs')
try {
    [System.IO.File]::WriteAllText($testStubPath, $testCode, [System.Text.UTF8Encoding]::new($false))
    Add-Type -Path (@($testStubPath) + $sourceFiles)
    [ShopPurchaseRegression]::Run() | ForEach-Object { Write-Output "通過：$_" }

    # 使用實際 Newtonsoft JSON 驗證新增配送欄位的往返與舊存檔相容。
    $fixture = [ShopPurchaseFixture]::new()
    $fixture.Product.IsOnlineOrder = $true
    $null = $fixture.Shop.TryPurchaseItem('condom')
    $json = [Newtonsoft.Json.JsonConvert]::SerializeObject($fixture.Delivery.ToSaveData())
    $loaded = [Newtonsoft.Json.JsonConvert]::DeserializeObject[DeliverySaveData]($json)
    $fixture.Delivery.LoadFromSaveData($loaded)
    if ($fixture.Inventory.GetItemCount('condom') -ne 0 -or $fixture.Player.SkillPoints -ne 0) {
        throw '讀取待配送存檔時不應入庫或觸發效果'
    }
    $fixture.Delivery.ProcessDeliveries(2, $fixture.Shop)
    if ($fixture.Inventory.GetItemCount('condom') -ne 3 -or $fixture.Player.SkillPoints -ne 1) {
        throw '新配送資料 JSON 往返後的入庫數量或效果次數錯誤'
    }
    $finishedJson = [Newtonsoft.Json.JsonConvert]::SerializeObject($fixture.Delivery.ToSaveData())
    $fixture.Delivery.LoadFromSaveData([Newtonsoft.Json.JsonConvert]::DeserializeObject[DeliverySaveData]($finishedJson))
    $fixture.Delivery.ProcessDeliveries(2, $fixture.Shop)
    if ($fixture.Inventory.GetItemCount('condom') -ne 3 -or $fixture.Player.SkillPoints -ne 1) {
        throw '已完成配送的存檔不應再次給物'
    }
    Write-Output '通過：新訂單 JSON 往返、讀檔不觸發、完成配送不重複'

    $fixture = [ShopPurchaseFixture]::new()
    $legacyJson = '{"PendingOrders":[{"ItemID":"condom","Amount":1,"DeliveryDay":2}]}'
    $fixture.Delivery.LoadFromSaveData([Newtonsoft.Json.JsonConvert]::DeserializeObject[DeliverySaveData]($legacyJson))
    $fixture.Delivery.ProcessDeliveries(2, $fixture.Shop)
    if ($fixture.Inventory.GetItemCount('condom') -ne 1 -or $fixture.Player.SkillPoints -ne 1) {
        throw '舊訂單必須保留原數量，不得乘上新版每份數量'
    }
    Write-Output '通過：舊訂單缺少 PurchaseCount 時保留原數量'
}
finally {
    if (Test-Path -LiteralPath $testStubPath) { Remove-Item -LiteralPath $testStubPath }
}
