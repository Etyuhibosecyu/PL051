namespace PL051.NStar;

[DebuggerDisplay("{ToString()}")]
public class BranchCollection : List<TreeBranch>
{
	public BranchCollection() : base() { }

	public BranchCollection(G.IDictionary<String?, TreeBranch> collection) : this(collection.Values)
	{
		foreach (var key in collection.Keys)
			if (key is not null)
				Keys.Add(key, Keys.Length);
	}

	public BranchCollection(G.IEnumerable<G.KeyValuePair<String?, TreeBranch>> collection)
	{
		foreach (var item in collection)
		{
			if (item.Key is not null)
				Keys.Add(item.Key, _size);
			Add(item.Value);
		}
	}

	public BranchCollection(G.IEnumerable<TreeBranch> collection) : base(collection) { }

	public virtual void Add(String? key, TreeBranch item)
	{
		if (key is not null)
			Keys.Add(key, _size);
		Add(item);
	}

	public virtual void AddRange(BranchCollection collection)
	{
		foreach (var item in collection.Keys)
			Keys.Add(item.Key, item.Value + _size);
		base.AddRange(collection);
	}

	public Mirror<String, int> Keys { get; } = [];

	public TreeBranch this[String key]
	{
		get => Keys.TryGetValue(key, out var index) ? this[index] : throw new G.KeyNotFoundException();
		set
		{
			if (!Keys.TryGetValue(key, out var index))
				throw new G.KeyNotFoundException();
			this[index] = value;
		}
	}

	public override bool Equals(object? obj)
	{
		if (obj is not BranchCollection m)
			return false;
		if (Length != m.Length)
			return false;
		return this.Combine(m).All(x => x.Item1 == x.Item2);
	}

	public override int GetHashCode()
	{
		var hash = 486187739;
		var en = GetEnumerator();
		if (en.MoveNext())
		{
			hash = (hash * 16777619) ^ en.Current.GetHashCode();
			if (en.MoveNext())
			{
				hash = (hash * 16777619) ^ en.Current.GetHashCode();
				hash = (hash * 16777619) ^ this[^1].GetHashCode();
			}
		}
		return hash;
	}

	public virtual bool Remove(String key)
	{
		if (Keys.TryGetValue(key, out var index))
		{
			Keys.RemoveKey(key);
			RemoveAt(index);
			return true;
		}
		return false;
	}

	public virtual void Replace(BranchCollection collection)
	{
		Clear();
		AddRange(collection);
	}

	public virtual bool TryAdd(String key, TreeBranch value)
	{
		if (Keys.TryAdd(key, _size))
		{
			Add(value);
			return true;
		}
		return false;
	}

	public override string ToString() => string.Join(", ", this.ToArray(x => x.ToShortString()));

	public static bool operator ==(BranchCollection? x, BranchCollection? y) => x?.Equals(y) ?? y is null;

	public static bool operator !=(BranchCollection? x, BranchCollection? y) => !(x == y);
}

public readonly struct BranchCollectionEComparer : G.IEqualityComparer<BranchCollection>
{
	public readonly bool Equals(BranchCollection? x, BranchCollection? y)
	{
		if (x is null && y is null)
			return true;
		if (x is null || y is null)
			return false;
		if (x.Length != y.Length)
			return false;
		else if (x.Length == 0 && y.Length == 0)
			return true;
		for (var i = 0; i < x.Length && i < y.Length; i++)
		{
			if (x[i] != y[i])
				return false;
		}
		return true;
	}

	public readonly int GetHashCode(BranchCollection x)
	{
		var hash = 0;
		for (var i = 0; i < x.Length; i++)
			hash ^= x[i].GetHashCode();
		return hash;
	}
}
