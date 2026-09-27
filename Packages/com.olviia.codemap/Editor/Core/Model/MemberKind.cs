namespace Olviia.CodeMap.Core.Model
{
    /// <summary>What kind of member an entry describes. A delegate is stored as a type with one <see cref="Method"/> member holding its signature.</summary>
    public enum MemberKind
    {
        Constructor,
        Method,
        Property,
        Indexer,
        Field,
        Event,
        Operator,
        EnumValue
    }
}
