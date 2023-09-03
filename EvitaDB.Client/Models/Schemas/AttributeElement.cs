using EvitaDB.Client.Queries.Order;

namespace EvitaDB.Client.Models.Schemas;

public record AttributeElement(
    string AttributeName,
    OrderDirection Direction,
    OrderBehaviour Behaviour
)
{
    /**
		 * Helper method to create an attribute element. The direction is set to {@link OrderDirection#ASC} and the
		 * behaviour is set to {@link OrderBehaviour#NULLS_LAST}.
		 */
    public static AttributeElement CreateAttributeElement(string attributeName)
    {
        return new AttributeElement(attributeName, Queries.Order.OrderDirection.Asc, Schemas.OrderBehaviour.NullsLast);
    }

    /**
		 * Helper method to create an attribute element with the given direction. The behaviour is set to
		 * {@link OrderBehaviour#NULLS_LAST}.
		 */
    public static AttributeElement CreateAttributeElement(string attributeName, OrderDirection direction)
    {
        return new AttributeElement(attributeName, direction, Schemas.OrderBehaviour.NullsLast);
    }

    /**
		 * Helper method to create an attribute element with the given behaviour. The direction is set to
		 * {@link OrderDirection#ASC}.
		 */
    public static AttributeElement CreateAttributeElement(string attributeName, OrderBehaviour behaviour)
    {
        return new AttributeElement(attributeName, Queries.Order.OrderDirection.Asc, behaviour);
    }

    /**
		 * Helper method to create an attribute element with the given direction and behaviour.
		 */
    public static AttributeElement CreateAttributeElement(string attributeName, OrderDirection direction,
        OrderBehaviour behaviour)
    {
        return new AttributeElement(attributeName, direction, behaviour);
    }

    public override string ToString()
    {
        return '\'' + AttributeName + '\'' + " " + Direction + " " + Behaviour;
    }
}