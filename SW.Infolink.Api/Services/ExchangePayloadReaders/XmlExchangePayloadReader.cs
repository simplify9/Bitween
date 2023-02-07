using System.Xml.Linq;
using System.Xml.XPath;

namespace SW.Infolink;

public class XmlExchangePayloadReader : IExchangePayloadReader
{
    private readonly XDocument _doc;
    
    public XmlExchangePayloadReader(string data)
    {
        _doc = XDocument.Parse(data);
    }
    
    public bool TryGetValue(string path, out string value)
    {
        value = default(string);
        var node = _doc.XPathSelectElement(path);
        if (node == null) return false;
        var trimmed = node.Value.Trim();
        value = trimmed == string.Empty ? default(string) : trimmed;
        return true;
    }
}