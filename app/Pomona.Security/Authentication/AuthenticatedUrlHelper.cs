#region License

// ----------------------------------------------------------------------------
// Pomona source code
// 
// Copyright � 2014 Karsten Nikolai Strand
// 
// Permission is hereby granted, free of charge, to any person obtaining a 
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
// ----------------------------------------------------------------------------

#endregion

using System;

using Pomona.Common;

namespace Pomona.Security.Authentication
{
    public class AuthenticatedUrlHelper
    {
        private readonly CryptoSerializer cryptoSerializer;


        public AuthenticatedUrlHelper(CryptoSerializer cryptoSerializer)
        {
            if (cryptoSerializer == null)
                throw new ArgumentNullException("cryptoSerializer");
            this.cryptoSerializer = cryptoSerializer;
        }


        public static string AddQueryParameterString(string url, string key, string value)
        {
            var uri = new Uri(url);

            // this gets all the query string key value pairs as a collection
            var newQueryString = HttpUtility.ParseQueryString(uri.Query);

            // this removes the key if exists
            newQueryString.Add(key, value);

            // this gets the page path from root without QueryString
            var pagePathWithoutQueryString = uri.GetLeftPart(UriPartial.Path);

            return newQueryString.Count > 0
                ? String.Format("{0}?{1}", pagePathWithoutQueryString, newQueryString)
                : pagePathWithoutQueryString;
        }


        public static string RemoveQueryStringByKey(string url, string key)
        {
            var uri = new Uri(url);

            // this gets all the query string key value pairs as a collection
            var newQueryString = HttpUtility.ParseQueryString(uri.Query);

            // this removes the key if exists
            newQueryString.Remove(key);

            // this gets the page path from root without QueryString
            var pagePathWithoutQueryString = uri.GetLeftPart(UriPartial.Path);

            return newQueryString.Count > 0
                ? String.Format("{0}?{1}", pagePathWithoutQueryString, newQueryString)
                : pagePathWithoutQueryString;
        }


        public virtual DateTime OnGetUtcNow()
        {
            return DateTime.UtcNow;
        }


        public string CreatePreAuthorizedUrl(string urlString, DateTime? expiration = null)
        {
            // Path and query is part of verified url
            var url = new Uri(urlString);
            var verifiedUrlPart = url.PathAndQuery;
            var urlToken = new UrlToken() { PathQueryHash = verifiedUrlPart, Expiration = expiration };
            var tokenParameter = this.cryptoSerializer.SerializeEncryptedHexString(urlToken);
            return AddQueryParameterString(urlString, "$token", tokenParameter);
        }


        public bool VerifyPreAuthorizedUrl(string urlString)
        {
            var query = HttpUtility.ParseQueryString(new Uri(urlString).Query);
            var tokenParameter = query.Get("$token");
            var urlToken = this.cryptoSerializer.DeserializeEncryptedHexString<UrlToken>(tokenParameter);
            var urlWithoutToken = new Uri(RemoveQueryStringByKey(urlString, "$token"));
            if (urlToken.Expiration.HasValue && urlToken.Expiration < OnGetUtcNow())
                return false;
            return urlToken.PathQueryHash == urlWithoutToken.PathAndQuery;
        }
    }
}