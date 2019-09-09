// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Test
{
	class ExamplesGenerics<A>
	{
		public A field1;

		public class NestedClass<B>
		{
			public A field2;
			public B field3;

			public KeyValuePair<K, V> ExampleGenericMethod<K, V>(A p, B q, K key, V value, KeyValuePair<K, V> pair)
			{
				return pair;
			}
		}
	}

	class ExamplesGenericReferences
	{
		public void Example()
		{
			var obj = new ExamplesGenerics<int>.NestedClass<bool>();

			obj.field2 = 5;
			obj.field3 = true;
			obj.ExampleGenericMethod<float, string>(1, true, 0, "hola", new KeyValuePair<float, string>(4, "chau"));
		}
	}
}
