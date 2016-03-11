#region License

// Pomona is open source software released under the terms of the LICENSE specified in the
// project's repository, or alternatively at http://pomona.io/

#endregion

using System.Linq;

namespace Pomona.Common.Linq
{
    internal interface IRestQueryRoot : IQueryable
    {
        string Uri { get; }
    }
}