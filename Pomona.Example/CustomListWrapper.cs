﻿// ----------------------------------------------------------------------------
// Pomona source code
// 
// Copyright © 2012 Karsten Nikolai Strand
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

using System.Collections;
using System.Collections.Generic;

namespace Pomona.Example
{
    public class CustomListWrapper<T> : IList<T>
    {
        private List<T> wrappedList = new List<T>();

        #region IList<T> Members

        public T this[int index]
        {
            get { return wrappedList[index]; }
            set { wrappedList[index] = value; }
        }


        public int Count
        {
            get { return wrappedList.Count; }
        }

        public bool IsReadOnly
        {
            get { return ((IList<T>) wrappedList).IsReadOnly; }
        }


        public void Add(T item)
        {
            wrappedList.Add(item);
        }


        public void Clear()
        {
            wrappedList.Clear();
        }


        public bool Contains(T item)
        {
            return wrappedList.Contains(item);
        }


        public void CopyTo(T[] array, int arrayIndex)
        {
            wrappedList.CopyTo(array, arrayIndex);
        }


        public IEnumerator<T> GetEnumerator()
        {
            return wrappedList.GetEnumerator();
        }


        public int IndexOf(T item)
        {
            return wrappedList.IndexOf(item);
        }


        public void Insert(int index, T item)
        {
            wrappedList.Insert(index, item);
        }


        public bool Remove(T item)
        {
            return wrappedList.Remove(item);
        }


        public void RemoveAt(int index)
        {
            wrappedList.RemoveAt(index);
        }


        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}