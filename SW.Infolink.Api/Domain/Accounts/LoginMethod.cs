using System;

namespace SW.Infolink.Domain.Accounts
{
    [Flags]
    public enum LoginMethod
    {
        None = 0,
        ApiKey = 1,
        EmailAndPassword = 2,
        PhoneAndOtp = 4,
    }
}