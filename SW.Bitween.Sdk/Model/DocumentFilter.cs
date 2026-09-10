using System;
using System.Collections.Generic;
using System.Text;

namespace SW.Bitween.Model
{
    public class DocumentFilter
    {
        public DocumentFilter()
        {
            Properties = new Dictionary<string, PropertyFilter>();
            DocumentsWithNoPromotedProperties = new FilterResult();
        }

        public IDictionary<string, PropertyFilter> Properties  { get;  }

        public FilterResult DocumentsWithNoPromotedProperties { get; set; }

    }


    public class PropertyFilter(string path)
    {
        public string Path { get; set; } = path;
        public ICollection<int> Ignored { get; } = new List<int>();
        public IDictionary<string, ICollection<int>> SubscribersByValues { get; } = new Dictionary<string, ICollection<int>>();
    }




}
