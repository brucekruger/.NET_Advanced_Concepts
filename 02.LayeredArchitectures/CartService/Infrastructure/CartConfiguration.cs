using CartService.Domain;
using LiteDB;

namespace CartService.Infrastructure;

public static class CartConfiguration
{
    public static void ConfigureMapping()
    {
        var mapper = BsonMapper.Global;

        mapper.Entity<Cart>()
            .Id(x => x.Id); // set your document ID
    }
}
