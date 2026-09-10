using System.Threading.Tasks;
using SW.Bitween.Services;
using SW.PrimitiveTypes;

namespace SW.Bitween.Resources.Settings;

[Unprotect]
[HandlerName("Config")]
public class Config(BitweenOptions BitweenOptions, ThemeOptions themeOptions) : IQueryHandler<object>
{
    public async Task<object> Handle()
    {
        return new
        {
            BitweenOptions.MsalClientId,
            BitweenOptions.MsalRedirectUri,
            BitweenOptions.MsalTenantId,
            BitweenOptions.DisableEmailPasswordLogin,
            IsRabbitMqManagementConfigured = !string.IsNullOrWhiteSpace(BitweenOptions.RabbitMqManagementUrl)
                                             && !string.IsNullOrWhiteSpace(BitweenOptions.RabbitMqManagementUsername)
                                             && !string.IsNullOrWhiteSpace(BitweenOptions.RabbitMqManagementPassword),
            Theme = themeOptions,
            // The product defaults, so the sign-in page — which has no session and can't read the
            // settings list — can tell a brand value someone chose from one nobody has touched.
            ThemeDefaults = SettingsService.DefaultsUnder("Theme.")
        };
    }
}
