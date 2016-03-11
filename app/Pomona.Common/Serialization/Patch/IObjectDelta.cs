﻿#region License

// Pomona is open source software released under the terms of the LICENSE specified in the
// project's repository, or alternatively at http://pomona.io/

#endregion

using System.Collections.Generic;

namespace Pomona.Common.Serialization.Patch
{
    public interface IObjectDelta : IDelta
    {
        IEnumerable<KeyValuePair<string, object>> ModifiedProperties { get; }
    }
}