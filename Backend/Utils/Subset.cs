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

		public T[] Universe
		{
			get { return this.universe; }
		}

		public bool IsMember(int index)
		{
			return this.data[index];
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

		public void Union(Subset<T> set)
		{
			this.data.Or(set.data);
		}

		public void Intersect(Subset<T> set)
		{
			this.data.And(set.data);
		}

		public void Except(Subset<T> set)
		{
			set.data.Not();
			this.data.And(set.data);
			set.data.Not();
		}

		public void SymmetricExcept(Subset<T> set)
		{
			this.data.Xor(set.data);
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
