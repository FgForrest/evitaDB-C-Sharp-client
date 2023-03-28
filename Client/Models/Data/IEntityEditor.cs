using Client.Models.Schemas;

namespace Client.Models.Data;

public interface IEntityEditor<out TW> : IEntity, IAttributeEditor<TW> where TW : IEntityEditor<TW> //TODO: OTHER EDITORS
{
    TW SetHierarchicalPlacement(int orderAmongSiblings);
    
	TW SetHierarchicalPlacement(int parentPrimaryKey, int orderAmongSiblings);
	
	TW RemoveHierarchicalPlacement();

	TW SetReference(string referenceName, int referencedPrimaryKey);
	
	/*TW SetReference(
		string referenceName,
		int referencedPrimaryKey,
		Action<ReferenceBuilder>? whichIs
	);*/

	TW SetReference(
		string referenceName,
		string referencedEntityType,
		Cardinality cardinality,
		int referencedPrimaryKey
	);
		
	/*TW SetReference(
		string referenceName,
		string referencedEntityType,
		Cardinality cardinality,
		int referencedPrimaryKey,
		Action<ReferenceBuilder>? whichIs
	);*/
		
	TW RemoveReference(string referenceName, int referencedPrimaryKey);
}