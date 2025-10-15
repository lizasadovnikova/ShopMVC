using ShopDomain.Model;

namespace ShopInfrastructure.ViewModels
{
    public class ItemLandingVm
    {
        public Item Item { get; set; } = default!;
        public List<Item> Related { get; set; } = new();
    }
}

