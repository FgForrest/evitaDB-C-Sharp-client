using System.Globalization;
using Client;
using Client.Config;
using Client.DataTypes;
using Client.Models;
using Client.Models.Data.Mutations;
using Client.Models.Data.Mutations.Attributes;
using Client.Models.Data.Structure;
using Client.Models.Schemas.Dtos;
using Client.Models.Schemas.Mutations.Attributes;
using Client.Models.Schemas.Mutations.Catalog;
using Client.Queries.Order;
using static Client.Queries.IQueryConstraints;

const string catalogName = "moda";

var clientConfiguration = new EvitaClientConfiguration.Builder()
    .SetHost("localhost")
    .SetPort(5556)
    .SetSystemApiPort(5557)
    .Build();

var client = new EvitaClient(clientConfiguration);

EvitaEntityReferenceResponse referenceResponse = await client.QueryCatalogAsync(catalogName, session =>
    session.Query<EvitaEntityReferenceResponse, EntityReference>(
        Query(
            Collection("Product"),
            FilterBy(
                And(
                    Not(
                        AttributeContains("url", "bla")
                    ),
                    Or(
                        EntityPrimaryKeyInSet(677, 678, 679, 680),
                        And(
                            PriceBetween(1.2m, 1000),
                            PriceValidIn(DateTimeOffset.Now),
                            PriceInPriceLists("basic", "vip"),
                            PriceInCurrency(new Currency("CZK"))
                        )
                    )
                )
            ),
            OrderBy(
                PriceNatural(OrderDirection.Desc)
            ),
            Require(
                Page(1, 20),
                DataInLocales(new CultureInfo("en-US"), new CultureInfo("cs-CZ")),
                QueryTelemetry()
            )
        )
    ));

EvitaEntityResponse entityResponse = await client.QueryCatalogAsync(catalogName, session =>
    session.Query<EvitaEntityResponse, SealedEntity>(
        Query(
            Collection("Product"),
            FilterBy(
                And(
                    Not(
                        AttributeContains("url", "bla")
                    ),
                    Or(
                        EntityPrimaryKeyInSet(677, 678, 679, 680),
                        And(
                            PriceBetween(102.2m, 10000),
                            PriceValidIn(DateTimeOffset.Now),
                            PriceInPriceLists("basic", "vip"),
                            PriceInCurrency(new Currency("CZK"))
                        )
                    )
                )
            ),
            OrderBy(
                PriceNatural(OrderDirection.Desc)
            ),
            Require(
                Page(1, 20),
                EntityFetch(AttributeContent(), ReferenceContent(), PriceContent()),
                DataInLocales(new CultureInfo("en-US"), new CultureInfo("cs-CZ")),
                QueryTelemetry()
            )
        )
    ));

EvitaEntityReferenceResponse test = await client.QueryCatalogAsync(catalogName, session => 
    session.Query<EvitaEntityReferenceResponse, EntityReference>(
        Query(
            Collection("Product"),
            Require(Page(1, 20))
        )
        )
);

EvitaClientSession session = client.CreateReadOnlySession(catalogName);
SealedEntity? entity = await session.GetEntityAsync("Product", 678, AttributeContent(), ReferenceContent(), PriceContent());
session.Close();


const string testCatalog = "testingCatalog";
client.DeleteCatalogIfExists(testCatalog);
const string testCollection = "testingCollection";
const string attributeDateTime = "attrDateTime";
const string attributeDecimalRange = "attrDecimalRange";
CatalogSchema catalogSchema = await client.DefineCatalogAsync(testCatalog);
using (var rwSession = client.CreateReadWriteSession(testCatalog))
{
    rwSession.UpdateAndFetchCatalogSchema(new CreateEntitySchemaMutation(testCollection));

    CreateAttributeSchemaMutation createAttributeDateTime = new CreateAttributeSchemaMutation(
        attributeDateTime, nameof(attributeDateTime), null, false, true, true, false, true, typeof(DateTimeOffset), null, 0
    );
    CreateAttributeSchemaMutation createAttributeDecimalRange = new CreateAttributeSchemaMutation(
        attributeDecimalRange, nameof(attributeDecimalRange), null, false, true, true, false, true, typeof(DecimalNumberRange), null, 2
    );
    rwSession.UpdateCatalogSchema(new ModifyEntitySchemaMutation(testCollection, createAttributeDateTime, createAttributeDecimalRange));

    var entitySchema = rwSession.GetEntitySchema(testCollection);
    rwSession.GoLiveAndClose();
}

using var newSession = client.CreateReadWriteSession(testCatalog);

var shouldWork = newSession.UpsertAndFetchEntity(
    new EntityUpsertMutation(
        testCollection, 
        null, 
        EntityExistence.MayExist, 
        new UpsertAttributeMutation(attributeDateTime, DateTimeOffset.Now)
    ),
    AttributeContent()
);

var shouldWorkAlso = newSession.UpsertAndFetchEntity(
    new EntityUpsertMutation(
        testCollection, 
        null, 
        EntityExistence.MayExist, 
        new UpsertAttributeMutation("blabla", true)
    ),
    AttributeContent()
);

//entity schema it mutated

Console.WriteLine();
