namespace GVisionWpf.Utils
{
    public class MultiMap<K, V>
    {
        private readonly Dictionary<K, List<V>> dictionary = new Dictionary<K, List<V>>();

        public void Add(K key, V value)
        {
            // Add a key.
            List<V> list;
            if (this.dictionary.TryGetValue(key, out list!))
            {
                list.Add(value);
            }
            else
            {
                list = new List<V>(8) { value };
                this.dictionary[key] = list;
            }
        }

        // Get all keys.
        public IEnumerable<K> Keys => this.dictionary.Keys;

        public List<V> this[K key]
        {
            get
            {
                // Get list at a key.
                List<V> list;
                if (this.dictionary.TryGetValue(key, out list!))
                {
                    return list;
                }

                list = new List<V>();
                this.dictionary[key] = list;
                return list;
            }
        }
    }
}