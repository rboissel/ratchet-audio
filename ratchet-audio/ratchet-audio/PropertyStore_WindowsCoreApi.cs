using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ratchet.Audio
{
    class PropertyStore_WindowsCoreApi
    {
        Factory_WindowsCoreApi.IPropertyStore _IPropertyStore;
        Dictionary<uint, Factory_WindowsCoreApi.PROPERTYKEY> _PropertyKeys = new Dictionary<uint, Factory_WindowsCoreApi.PROPERTYKEY>();
        public PropertyStore_WindowsCoreApi(Factory_WindowsCoreApi.IMMDevice IMMDevice)
        {
            IMMDevice.OpenPropertyStore(0, out _IPropertyStore);
            int count = 0;
            _IPropertyStore.GetCount(out count);
            for (int n = 0; n < count; n++)
            {
                Factory_WindowsCoreApi.PROPERTYKEY key;
                _IPropertyStore.GetAt(n, out key);
                if (!_PropertyKeys.ContainsKey((uint)key.pid))
                {
                    _PropertyKeys.Add((uint)key.pid, key);
                }
            }
        }

        public ulong GetProperty(uint id)
        {
            lock (this)
            {
                try
                {
                    if (_PropertyKeys.ContainsKey(id))
                    {
                        Factory_WindowsCoreApi.PROPVARIANT variant;
                        Factory_WindowsCoreApi.PROPERTYKEY key = _PropertyKeys[id];
                        _IPropertyStore.GetValue(ref key, out variant);
                        return variant.data;
                    }
                }
                catch { return 0; }
            }
            return 0;
        }
    }
}
