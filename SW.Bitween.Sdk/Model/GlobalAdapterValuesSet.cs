using SW.PrimitiveTypes;
using System.Collections.Generic;

namespace SW.Bitween.Model
{
    public class GlobalAdapterValuesSetCreate : IName
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public Dictionary<string, string> Values { get; set; }
    }

    // Id is inherited from GlobalAdapterValuesSetCreate. Redeclaring it here shadowed the base
    // property (CS0108): a write through a base-typed reference set a different slot from the one
    // a serializer read back.
    public class GlobalAdapterValuesSetRow : GlobalAdapterValuesSetUpdate
    {
    }

    public class GlobalAdapterValuesSetUpdate : GlobalAdapterValuesSetCreate
    {
    }

    public class DeleteGlobalAdapterValuesSetModel
    {
    }
}
