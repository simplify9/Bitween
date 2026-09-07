namespace SW.Bitween.Model;

public class OrSpec(IPropertyMatchSpecification left, IPropertyMatchSpecification right) : IPropertyMatchSpecification
{
    public IPropertyMatchSpecification Left { get; private set; } = left;

    public IPropertyMatchSpecification Right { get; private set; } = right;

    public bool IsMatch(IExchangePayloadReader reader) => Left.IsMatch(reader) || Right.IsMatch(reader);

    public override string ToString()
    {
        return $"({Left}) OR ({Right})";
    }
    
    public string Name => "or";
}