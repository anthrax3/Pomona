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

namespace Pomona.Common.Proxies
{
    public class LazyProxyBase : IHasSettableResourceUri, ILazyProxy
    {
        private IPomonaClient client;
        private IResourceFetchContext fetchContext;
        private object proxyTarget;
        private Type proxyTargetType;
        private string uri;

        public IPomonaClient Client
        {
            get { return this.client; }
            internal set { this.client = value; }
        }

        public bool IsLoaded
        {
            get { return this.proxyTarget != null; }
        }

        public object ProxyTarget
        {
            get { return this.proxyTarget; }
            internal set { this.proxyTarget = value; }
        }

        public Type ProxyTargetType
        {
            get { return this.proxyTargetType; }
            internal set { this.proxyTargetType = value; }
        }

        public string Uri
        {
            get { return this.uri; }
            set { this.uri = value; }
        }


        public virtual void Initialize(IResourceFetchContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            this.fetchContext = context;
        }


        protected TPropType OnGet<TOwner, TPropType>(PropertyWrapper<TOwner, TPropType> property)
        {
            if (this.fetchContext == null)
                throw new InvalidOperationException(String.Format("{0}.Initialize(IResourceFetchContext) must be invoked before OnGet.", this));

            if (!IsLoaded && !this.fetchContext.LazyEnabled)
                throw new InvalidOperationException(String.Format("Lazy fetching has been disabled for {0}.", property));

            Fetch();

            return property.Getter((TOwner)this.proxyTarget);
        }


        protected void OnSet<TOwner, TPropType>(PropertyWrapper<TOwner, TPropType> property, TPropType value)
        {
            throw new InvalidOperationException(String.Format("{0} is just a proxy. Use Patch to modify a resource.",
                                                              property));
        }


        private void Fetch()
        {
            if (IsLoaded)
                return;

            this.proxyTarget = this.client.Get(this.uri, this.proxyTargetType);
            var hasResourceUri = this.proxyTarget as IHasResourceUri;
            if (hasResourceUri != null)
                Uri = hasResourceUri.Uri;
        }
    }
}