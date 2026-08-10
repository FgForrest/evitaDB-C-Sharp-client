using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Mutations;

public abstract class MutationPredicate
{
    protected readonly MutationPredicateContext _context;
    
    public MutationPredicate(MutationPredicateContext context)
    {
        _context = context;
    }
    
    public abstract bool Test(IMutation mutation);
    
    public MutationPredicateContext Context => _context;

    public static MutationPredicate Or(params MutationPredicate[] predicates)
    {
        return new OrMutationPredicate(predicates);
    }
    
    public MutationPredicate And(MutationPredicate predicate)
    {
        return new AndMutationPredicate(this, predicate);
    }
    
    private class AndMutationPredicate : MutationPredicate
    {
        private readonly MutationPredicate _former;
        private readonly MutationPredicate _other;
            
        public AndMutationPredicate(MutationPredicate former, MutationPredicate other) : base(former.Context)
        {
            _former = former;
            _other = other;
            Assert.IsPremiseValid(former.Context == other.Context, "Contexts of the predicates must be the same");
        }
        
        public override bool Test(IMutation mutation) => _former.Test(mutation) && _other.Test(mutation);
    }

    private class OrMutationPredicate : MutationPredicate
    {
        private readonly MutationPredicate[] _predicates;
        
        public OrMutationPredicate(MutationPredicate[] predicates) : base(predicates[0].Context)
        {
            _predicates = predicates;
            for (int i = 1; i < predicates.Length; i++)
            {
                Assert.IsPremiseValid(predicates[i].Context == predicates[0].Context, "Contexts of the predicates must be the same");
            }
        }

        public override bool Test(IMutation mutation)
        {
            return _predicates.Any(predicate => predicate.Test(mutation));
        }
    }
}
