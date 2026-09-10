using System;
using System.Collections.Generic;
using System.Linq;

namespace SW.Bitween.Model;

public class OneOfSpec(string path, IEnumerable<string> values) : IPropertyMatchSpecification
{
    public string Path { get; private set; } = path;

    public string[] Values { get; private set; } = values.ToArray();

    public override string ToString()
    {
        return $"{Path} is one of [{string.Join(",", Values)}]";
    }

    public bool IsMatch(IExchangePayloadReader reader)
    {
        reader.TryGetValue(Path, out var value);
        
        return Values.Any(i => i.Equals(value, StringComparison.InvariantCultureIgnoreCase));
    }
    
    public string Name => "one_of";
}