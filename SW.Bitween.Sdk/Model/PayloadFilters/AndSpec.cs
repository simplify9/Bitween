using System;
using System.Linq.Expressions;
using SW.PrimitiveTypes;

namespace SW.Bitween.Model;

public class AndSpec(IPropertyMatchSpecification left, IPropertyMatchSpecification right) : IPropertyMatchSpecification
{
    public IPropertyMatchSpecification Left { get; private set; } = left;

    public IPropertyMatchSpecification Right { get; private set; } = right;

    public bool IsMatch(IExchangePayloadReader reader) => Left.IsMatch(reader) && Right.IsMatch(reader);
    public string Name => "and";

    public override string ToString()
    {
        return $"({Left}) AND ({Right})";
    }
}