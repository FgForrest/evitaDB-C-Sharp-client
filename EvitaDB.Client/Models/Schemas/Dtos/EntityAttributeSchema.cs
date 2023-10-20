using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Schemas.Dtos;

public class EntityAttributeSchema : AttributeSchema, IEntityAttributeSchema
{
    public bool Representative { get; }
    
	public EntityAttributeSchema(
		string name,
		IDictionary<NamingConvention, string?> nameVariants,
		string? description,
		string? deprecationNotice,
		bool unique,
		bool filterable,
		bool sortable,
		bool localized,
		bool nullable,
		bool representative,
		Type type,
		object? defaultValue,
		int indexedDecimalPlaces
	) : base(name, nameVariants, description, deprecationNotice,
		unique, filterable, sortable, localized, nullable,
		type, defaultValue, indexedDecimalPlaces) {
		Representative = representative;
	}

	/// <summary>
	/// This method is for internal purposes only. It could be used for reconstruction of GlobalAttributeSchema from
	/// different package than current, but still internal code of the Evita ecosystems.
	/// </summary>
	public new static EntityAttributeSchema InternalBuild(
		string name,
		Type type,
		bool localized
	) {
		return new EntityAttributeSchema(
			name, NamingConventionHelper.Generate(name),
			null, null,
			false, false, false, localized, false, false,
			type, null,
			0
		);
	}

	/// <summary>
	/// This method is for internal purposes only. It could be used for reconstruction of GlobalAttributeSchema from
	/// different package than current, but still internal code of the Evita ecosystems.
	/// </summary>
	public static EntityAttributeSchema InternalBuild(
		string name,
		bool unique,
		bool filterable,
		bool sortable,
		bool localized,
		bool nullable,
		bool representative,
		Type type,
		object? defaultValue
	) {
		return new EntityAttributeSchema(
			name, NamingConventionHelper.Generate(name),
			null, null,
			unique, filterable, sortable, localized, nullable, representative,
			type, defaultValue,
			0
		);
	}

	/// <summary>
	/// This method is for internal purposes only. It could be used for reconstruction of GlobalAttributeSchema from
	/// different package than current, but still internal code of the Evita ecosystems.
	/// </summary>
	public new static EntityAttributeSchema InternalBuild(
		string name,
		string? description,
		string? deprecationNotice,
		bool unique,
		bool filterable,
		bool sortable,
		bool localized,
		bool nullable,
		bool representative,
		Type type,
		object? defaultValue,
		int indexedDecimalPlaces
	) {
		return new EntityAttributeSchema(
			name, NamingConventionHelper.Generate(name),
			description, deprecationNotice,
			unique, filterable, sortable, localized, nullable, representative,
			type, defaultValue,
			indexedDecimalPlaces
		);
	}

	/// <summary>
	/// This method is for internal purposes only. It could be used for reconstruction of GlobalAttributeSchema from
	/// different package than current, but still internal code of the Evita ecosystems.
	/// </summary>
	public static EntityAttributeSchema InternalBuild(
		string name,
		IDictionary<NamingConvention, string?> nameVariants,
		string? description,
		string? deprecationNotice,
		bool unique,
		bool filterable,
		bool sortable,
		bool localized,
		bool nullable,
		bool representative,
		Type type,
		object? defaultValue,
		int indexedDecimalPlaces
	) {
		return new EntityAttributeSchema(
			name, nameVariants,
			description, deprecationNotice,
			unique, filterable, sortable, localized, nullable, representative,
			type, defaultValue,
			indexedDecimalPlaces
		);
	}

	public override string ToString() {
		return "GlobalAttributeSchema{" +
			"name='" + Name + '\'' +
			", unique=" + Unique +
			", filterable=" + Filterable +
			", sortable=" + Sortable +
			", localized=" + Localized +
			", nullable=" + Nullable +
			", representative=" + Representative +
			", type=" + Type +
			", indexedDecimalPlaces=" + IndexedDecimalPlaces +
			'}';
	}

    
}