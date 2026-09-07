using System;

namespace SW.Bitween.Model
{
    public class UserLogin
    {
        /// <summary>Null on a refresh-token or Microsoft-token sign-in.</summary>
        public string? Username { get; set; }

        public string? Password { get; set; }

        /// <summary>Sent instead of a password to renew an expired session.</summary>
        public string? RefreshToken { get; set; }

        /// <summary>Sent instead of a password when Microsoft sign-in is on.</summary>
        public string? MsToken { get; set; }
    }


    public class AccountLoginResult
    {
        public string Jwt { get; set; } = null!;
        public string RefreshToken { get; set; } = null!;
    }

 
}