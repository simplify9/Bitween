﻿namespace SW.Infolink
{
    public class InfolinkOptions
    {
        public const string ConfigurationSection = "Infolink";

        public InfolinkOptions()
        {
            //   AESEncryptionKey = "infolinkS9SecretKey";
            AdapterPath = "./adapters";
            AdminCredentials = "admin:1234512345";
            DocumentPrefix = "temp30/infolinkdocs";
            ClientIpHeaderName = "X-Real-IP";
            DatabaseType = "MySql";
            AdminDatabaseName = "defaultdb";
            ServerlessCommandTimeout = 300;
            ApiCallSubscriptionResponseAcceptedStatusCode = 202;
            ReceiversDelayInSeconds = 63;
            StorageProvider = "S3";
            JwtExpiryMinutes = 60;
            BusDefaultQueuePrefetch = 12;
        }

        public ushort? BusDefaultQueuePrefetch { get; set; }

        public string DatabaseType { get; set; }
        public string AdminDatabaseName { get; set; }
        public string AdapterPath { get; set; }
        public string AdminCredentials { get; set; }
        public string DocumentPrefix { get; set; }
        public string ClientIpHeaderName { get; set; }
        public int ServerlessCommandTimeout { get; set; }
        public bool AreXChangeFilesPrivate { get; set; } = false;
        public int? ApiCallSubscriptionResponseAcceptedStatusCode { get; set; }
        public int? ReceiversDelayInSeconds { get; set; }

        public string StorageProvider { get; set; }

        // public string AESEncryptionKey { get; set; }
        public string MsalClientId { get; set; }
        public string MsalRedirectUri { get; set; }

        public string MsalTenantId { get; set; }
        public int JwtExpiryMinutes { get; set; }
    }
}