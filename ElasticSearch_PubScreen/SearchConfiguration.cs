using ElasticSearch_PubScreen.Model;
using Nest;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace ElasticSearch_PubScreen
{
    public static class ElasticSearchPubScreen
    {
        private static ConnectionSettings settings = new ConnectionSettings(new Uri("https://localhost:9200"))
                           .CertificateFingerprint("")
                           .BasicAuthentication("elastic", "");

        private static string _cnnString = "Server=.\\SQLEXPRESS;Database=MouseBytes;Trusted_Connection=True;MultipleActiveResultSets=true";
        private static string _cnnString_PubScreen = "Server=.\\SQLEXPRESS;Database=PubScreen;Trusted_Connection=True;MultipleActiveResultSets=true";
        private static string _cnnString_Cogbytes = "Server=.\\SQLEXPRESS;Database=CogBytes;Trusted_Connection=True;MultipleActiveResultSets=true";

        public static ConnectionSettings GetElasticsearchSettings()
        {
            return settings;
        }
        public static string GetMouseByteConnectionString()
        {
            return _cnnString;
        }
        public static string GetPubScreenConnectionString()
        {
            return _cnnString_PubScreen;
        }

        public static string GetCogbytesConnectionString()
        {
            return _cnnString_Cogbytes;
        }
        public static void Main()
        {

        }
    }
}

