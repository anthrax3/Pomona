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

namespace Pomona.Common.TypeSystem
{
    public class ExportedPropertyDetails
    {
        private readonly HttpMethod accessMode;
        private readonly ExpandMode expandMode;
        private readonly bool exposedAsRepository;
        private readonly bool isAttributesProperty, isEtagProperty, isPrimaryKey;
        private readonly bool isSerialized;

        private readonly HttpMethod itemAccessMode;
        private readonly string uriName;


        public ExportedPropertyDetails(bool isAttributesProperty,
                                       bool isEtagProperty,
                                       bool isPrimaryKey,
                                       bool isSerialized,
                                       HttpMethod accessMode,
                                       HttpMethod itemAccessMode,
                                       bool exposedAsRepository,
                                       string uriName,
                                       ExpandMode expandMode)
        {
            this.isAttributesProperty = isAttributesProperty;
            this.isEtagProperty = isEtagProperty;
            this.isPrimaryKey = isPrimaryKey;
            this.isSerialized = isSerialized;
            this.accessMode = accessMode;
            this.itemAccessMode = itemAccessMode;
            this.exposedAsRepository = exposedAsRepository;
            this.uriName = uriName;
            this.expandMode = expandMode;
        }


        public HttpMethod AccessMode
        {
            get { return this.accessMode; }
        }

        public ExpandMode ExpandMode
        {
            get { return this.expandMode; }
        }

        public bool ExposedAsRepository
        {
            get { return this.exposedAsRepository; }
        }

        public bool IsAttributesProperty
        {
            get { return this.isAttributesProperty; }
        }

        public bool IsEtagProperty
        {
            get { return this.isEtagProperty; }
        }

        public bool IsPrimaryKey
        {
            get { return this.isPrimaryKey; }
        }

        public bool IsSerialized
        {
            get { return this.isSerialized; }
        }

        public HttpMethod ItemAccessMode
        {
            get { return this.itemAccessMode; }
        }

        public string UriName
        {
            get { return this.uriName; }
        }
    }
}