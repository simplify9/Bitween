using SW.Bitween;
using System;
using System.Collections.Generic;
using System.Text;

namespace SW.Bitween.Model
{
    public class AdapterSearchRequest
    {
        /// <summary>Optional filter; null lists every adapter.</summary>
        public string? Prefix { get; set; }
    }

    public class AdapterRow
    {
        public string Id { get; set; } = null!;
    }

}
