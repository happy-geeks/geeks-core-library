using System;
using System.Collections.Specialized;
using System.Text;
using System.Web;

namespace GeeksCoreLibrary.Core.Extensions
{
    public static class NameValueCollectionExtensions
    {
        /// <summary>
        /// Converts the <see cref="NameValueCollection"/> object into a string to be used as a query string for URLs.
        /// This method is copied from HttpUtility.cs.
        /// </summary>
        /// <param name="nameValueCollection"></param>
        /// <returns></returns>
        public static string ToQueryString(this NameValueCollection nameValueCollection)
        {
            var count = nameValueCollection.Count;
            if (count == 0)
            {
                return "";
            }

            var stringBuilder = new StringBuilder();
            var allKeys = nameValueCollection.AllKeys;
            for (var index = 0; index < count; ++index)
            {
                var values = nameValueCollection.GetValues(allKeys[index]);
                if (values == null)
                {
                    continue;
                }

                foreach (var str in values)
                {
                    if (String.IsNullOrEmpty(allKeys[index]))
                    {
                        stringBuilder.AppendFormat("{0}&", HttpUtility.UrlEncode(str));
                    }
                    else
                    {
                        stringBuilder.AppendFormat("{0}={1}&", allKeys[index], HttpUtility.UrlEncode(str));
                    }
                }
            }
            return stringBuilder.ToString(0, stringBuilder.Length - 1);
        }
    }
}
