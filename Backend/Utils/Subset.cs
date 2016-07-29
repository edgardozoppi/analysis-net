// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace Backend.Utils
{
	public class Subset<T>
	{
		private T[] universe;
		private BitArray data;

		public Subset(T[] universe, bool empty = true)
		{
			this.universe = universe;
			this.data = new BitArray(universe.Length, !empty);
		}

		public Subset(Subset<T> subset)
		{
			this.universe = subset.universe;
			this.data = new BitArray(subset.data);
		}

		public T[] Universe
		{
			get { return this.universe; }
		}

		public bool Contains(int index)
		{
			return this.data[index];
		}

		public Subset<T> Clone()
		{
			return new Subset<T>(this);
		}

		public void Clear()
		{
			this.data.SetAll(false);
		}

		public void AddAll()
		{
			this.data.SetAll(true);
		}

		public void Add(int index)
		{
			this.data[index] = true;
		}

		public void Remove(int index)
		{
			this.data[index] = false;
		}

		public void Complement()
		{
			this.data.Not();
		}

		public void Union(Subset<T> subset)
		{
			this.data.Or(subset.data);
		}

		public void Intersect(Subset<T> subset)
		{
			this.data.And(subset.data);
		}

		public void Except(Subset<T> subset)
		{
			subset.data.Not();
			this.data.And(subset.data);
			subset.data.Not();
		}

		public void SymmetricExcept(Subset<T> subset)
		{
			this.data.Xor(subset.data);
		}

		public ISet<T> ToSet()
		{
			var set = new HashSet<T>();
			this.ToSet(set);
			return set;
		}

		public void ToSet(ISet<T> set)
		{
			set.Clear();

			for (var i = 0; i < this.universe.Length; ++i)
			{
				var isMember = this.data[i];

				if (isMember)
				{
					var element = this.universe[i];
					set.Add(element);
				}
			}
		}

		public override int GetHashCode()
		{
			return this.data.GetHashCode();
		}

		public override bool Equals(object other)
		{
			var subset = other as Subset<T>;

			if (subset == null ||
				this.universe.Length != subset.universe.Length)
				return false;

			for (var i = 0; i < this.universe.Length; ++i)
			{
				if (this.data[i] != subset.data[i])
					return false;
			}

			return true;
		}

		public override string ToString()
		{
			var sb = new StringBuilder(" ");

			for (var i = 0; i < this.universe.Length; ++i)
			{
				var isMember = this.data[i];

				if (isMember)
				{
					var element = this.universe[i];
					sb.AppendFormat("{0} ", element);
				}
			}

			return sb.ToString();
		}
	}
}
