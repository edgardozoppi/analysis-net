// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Test
{
	class ExamplesGenerics<A>
	{
		class NestedClass<B>
		{
			public KeyValuePair<K, V> ExampleGenericMethod<K, V>(A p, B q, K key, V value, KeyValuePair<K, V> pair)
			{
				ExampleGenericMethod(p, q, 3, "hola", new KeyValuePair<int, string>(4, "chau"));

				return pair;
			}
		}
	}
}
