namespace SW.Bitween.NativeAdapters;

public static class ReflectionExtensions
{
    public static T ConvertTo<T>(this IDictionary<string,string> settings)
    {
        var inputInstance = Activator.CreateInstance(typeof(T));

        // Map dictionary properties to the input model
        foreach (var prop in typeof(T).GetProperties())
        {
            // Case-insensitive property lookup
            var propEntry = settings.FirstOrDefault(p =>
                string.Equals(p.Key, prop.Name, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(propEntry.Key))
            {
                var value = propEntry.Value;
                try
                {
                    var convertedValue = Convert.ChangeType(value,
                        Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
                    prop.SetValue(inputInstance, convertedValue);
                }
                catch
                {
                    // If conversion fails, set string value directly
                    if (prop.PropertyType == typeof(string))
                        prop.SetValue(inputInstance, value);
                }
            }
        }
        
        // Activator.CreateInstance returns null for a Nullable<T>, which is the one shape this
        // cast cannot survive.
        return inputInstance is T typed
            ? typed
            : throw new InvalidOperationException($"Could not build an instance of {typeof(T)}.");
    }
}