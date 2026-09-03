using EvitaDB.Client;
using EvitaDB.Client.Models.Data;
using EvitaDB.Client.Models.Data.Structure;
using EvitaDB.Client.Queries;
using EvitaDB.Test.Utils;
using static EvitaDB.Client.Queries.IQueryConstraints;

namespace EvitaDB.Test.Tests;

/// <summary>
/// The selling-price range a product with variants exposes — <c>priceForSaleMin</c> / <c>priceForSaleMax</c>.
/// </summary>
/// <remarks>
/// <para>
/// Against a real server, deliberately. The range is <em>computed by evitaDB</em> from prices grouped by inner
/// record id and ranked by price-list priority; a fake would only prove that this driver copies two fields it was
/// handed. What is worth knowing is that the numbers are right, and only the server can say that.
/// </para>
/// <para>
/// The fixture is evitaDB's own documented example (`documentation/user/en/deep-dive/price-for-sale-calculation.md`,
/// "Product variants extension"): one master carrying three variants' prices under <c>LOWEST_PRICE</c>.
/// </para>
/// </remarks>
public class PriceRangeForSaleTest : BaseTest<SetupFixture>
{
    public PriceRangeForSaleTest(ITestOutputHelper outputHelper, SetupFixture setupFixture)
        : base(outputHelper, setupFixture)
    {
    }

    private const string PriceList = "basic";

    [Fact]
    public void ShouldReadTheRangeOfAProductWithVariants()
    {
        using EvitaClientSession session = Client!.CreateReadWriteSession(Data.TestCatalog);

        int master = CreateMaster(session, [(1, 10m), (2, 12m), (3, 21m)]);

        ISealedEntity? fetched = QueryOne(session, master);

        Assert.NotNull(fetched);
        Assert.Equal(PriceInnerRecordHandling.LowestPrice, fetched.InnerRecordHandling);

        PriceRangeForSale? range = fetched.PriceRangeForSale;

        // Before this driver read fields 20 and 21 of GrpcSealedEntity, this was null and the range was
        // unobtainable without reimplementing the server's price-for-sale algorithm.
        Assert.NotNull(range);

        // "from 10 to 21" - and the selling price is the cheapest of the three, which is what LOWEST_PRICE means.
        Assert.Equal(10m, range.LowestPrice.PriceWithoutTax);
        Assert.Equal(21m, range.HighestPrice.PriceWithoutTax);
        Assert.Equal(10m, range.PriceForSale.PriceWithoutTax);
        Assert.Equal(fetched.PriceForSale?.PriceWithoutTax, range.PriceForSale.PriceWithoutTax);

        Assert.True(range.Spans, "three differently-priced variants are a range");
    }

    /// <remarks>
    /// evitaDB's own worked example uses this case (`Jumper X-Mas Deer`): every variant priced the same still has a
    /// range, and its ends are equal. A storefront that printed the range unconditionally would show
    /// "from 26 to 26", so <see cref="PriceRangeForSale.Spans"/> exists and is asserted here rather than left to
    /// whoever writes the template.
    /// </remarks>
    [Fact]
    public void ShouldReportNoSpanWhenEveryVariantCostsTheSame()
    {
        using EvitaClientSession session = Client!.CreateReadWriteSession(Data.TestCatalog);

        int master = CreateMaster(session, [(1, 26m), (2, 26m), (3, 26m)]);

        PriceRangeForSale? range = QueryOne(session, master)?.PriceRangeForSale;

        Assert.NotNull(range);
        Assert.Equal(26m, range.LowestPrice.PriceWithoutTax);
        Assert.Equal(26m, range.HighestPrice.PriceWithoutTax);
        Assert.False(range.Spans, "one price repeated is not a range");
    }

    /// <remarks>
    /// An ordinary product has no inner records, so all three bounds are the same price. Asserted because a
    /// storefront reads the range for every product, not only for masters, and must get a sane answer.
    /// </remarks>
    [Fact]
    public void ShouldReportADegenerateRangeForAnOrdinaryProduct()
    {
        using EvitaClientSession session = Client!.CreateReadWriteSession(Data.TestCatalog);

        string code = $"plain-{Guid.CreateVersion7():N}"[..24];

        int product = session.CreateNewEntity(Entities.Product)
            .SetAttribute(Data.AttributeCode, code)
            .SetAttribute(Data.AttributeName, Data.EnglishLocale, code)
            .SetPrice(1, PriceList, Data.CurrencyUsd, 99m, 20m, 118.8m, true)
            .UpsertVia(session)
            .PrimaryKey!.Value;


        PriceRangeForSale? range = QueryOne(session, product)?.PriceRangeForSale;

        Assert.NotNull(range);
        Assert.Equal(99m, range.LowestPrice.PriceWithoutTax);
        Assert.Equal(99m, range.HighestPrice.PriceWithoutTax);
        Assert.Equal(99m, range.PriceForSale.PriceWithoutTax);
        Assert.False(range.Spans);
    }

    /// <remarks>
    /// No price constraints in the query means no price context, so there is nothing for the server to compute and
    /// the range is absent rather than wrong. This is the case the nullable return exists for.
    /// </remarks>
    [Fact]
    public void ShouldHaveNoRangeWhenTheQueryAsksForNoPrices()
    {
        using EvitaClientSession session = Client!.CreateReadWriteSession(Data.TestCatalog);

        int master = CreateMaster(session, [(1, 10m), (2, 21m)]);

        ISealedEntity? fetched = session.QueryOneSealedEntity(
            Query(
                Collection(Entities.Product),
                FilterBy(EntityPrimaryKeyInSet(master)),
                Require(EntityFetch(AttributeContentAll()))));

        Assert.NotNull(fetched);
        Assert.Null(fetched.PriceRangeForSale);
    }

    /// <summary>Creates a master product carrying one price per variant, under <c>LOWEST_PRICE</c>.</summary>
    /// <remarks>
    /// The inner record id is the variant's identity — that is the whole mechanism: the master holds every variant's
    /// price, and evitaDB blocks them by inner record id when it computes the selling price.
    /// </remarks>
    private static int CreateMaster(EvitaClientSession session, IReadOnlyList<(int Variant, decimal Price)> variants)
    {
        // The seeded Product schema has a non-nullable localized `name` and a unique `code`, so both are set even
        // though this test is only about prices.
        string code = $"master-{Guid.CreateVersion7():N}"[..24];

        IEntityBuilder builder = session.CreateNewEntity(Entities.Product)
            .SetAttribute(Data.AttributeCode, code)
            .SetAttribute(Data.AttributeName, Data.EnglishLocale, code)
            .SetPriceInnerRecordHandling(PriceInnerRecordHandling.LowestPrice);

        int priceId = 1;

        foreach ((int variant, decimal price) in variants)
        {
            builder = builder.SetPrice(
                priceId++,
                PriceList,
                Data.CurrencyUsd,
                variant,
                price,
                20m,
                decimal.Round(price * 1.2m, 2),
                true);
        }

        int primaryKey = builder.UpsertVia(session).PrimaryKey!.Value;


        return primaryKey;
    }

    /// <summary>Fetches one entity with a price context — currency and price list, which the range needs.</summary>
    private static ISealedEntity? QueryOne(EvitaClientSession session, int primaryKey) =>
        session.QueryOneSealedEntity(
            Query(
                Collection(Entities.Product),
                FilterBy(
                    EntityPrimaryKeyInSet(primaryKey),
                    PriceInPriceLists(PriceList),
                    PriceInCurrency(Data.CurrencyUsd)),
                Require(EntityFetch(PriceContentRespectingFilter()))));
}
