using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Web;

namespace Permaverse.AO
{
    public static class UrlUtilities
    {
        [DllImport("__Internal")]
        private static extern string GetURLFromQueryStr();

        // Start is called before the first frame update
        public static string GetUrlParameterValue(string key)
        {
            string urlString = GetURLFromQueryStr();
            string value = null;

            Uri uri = new Uri(urlString);

            string query = uri.Query;
            var queryParameters = HttpUtility.ParseQueryString(query);

            if (queryParameters.Count > 0 && queryParameters.AllKeys.Contains(key))
            {
                value = queryParameters[key];
            }

            return value;
        }
    }
}