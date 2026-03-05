using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;

namespace Framework
{
    #region IOCContainer

    public class IOCContainer
    {
        private Dictionary<Type, object> _instances = new Dictionary<Type, object>();

        public void Register<T>(T instance)
        {
            var key = typeof(T);

            if (_instances.ContainsKey(key))
                _instances[key] = instance;
            else
                _instances.Add(key, instance);
        }

        public T Get<T>() where T : class
        {
            var key = typeof(T);

            if (_instances.TryGetValue(key, out var retInstance))
                return retInstance as T;

            return null;
        }

        public IEnumerable<T> GetInstancesByType<T>()
        {
            var type = typeof(T);
            return _instances.Values.Where(instance => type.IsInstanceOfType(instance)).Cast<T>();
        }

        public void Clear() => _instances.Clear();
    }

    #endregion

    #region Core

    public interface IArchitecture
    {
        void RegisterSystem<T>(T system) where T : ISystem;
        void RegisterModel<T>(T model) where T : IModel;
        T GetSystem<T>() where T : class, ISystem;
        T GetModel<T>() where T : class, IModel;
        void SendCommand(object sender, ICommand command);
        TResult SendCommand<TResult>(object sender, ICommand<TResult> command);
        TResult SendQuery<TResult>(object sender, IQuery<TResult> query);
        void SendEvent<T>(object sender, T e);
        void SendEvent<T>(object sender) where T : new();
        IUnregister RegisterEvent<T>(Action<T> onEvent) where T : new();
        void UnregisterEvent<T>(Action<T> onEvent) where T : new();
        void Update();
        void FixedUpdate();
        void Shutdown();
    }

    public abstract class Architecture<T> : IArchitecture where T : Architecture<T>, new()
    {
        private static IArchitecture _instance;

        public static IArchitecture Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new T();

                return _instance;
            }
        }

        public static Action<T> OnRegisterPatch = architecture => { };

        private readonly IOCContainer _container = new IOCContainer();
        private readonly EventSystem _eventSystem = new EventSystem();
        private readonly List<IUpdateable> _updateables = new List<IUpdateable>();
        private readonly List<IFixedUpdateable> _fixedUpdateables = new List<IFixedUpdateable>();
        private bool _inited;

        public static void Launch()
        {
            var arch = Instance as T;
            if (arch._inited) return;

            arch.RegisterModule();
            OnRegisterPatch?.Invoke(arch);

            var models = arch._container.GetInstancesByType<IModel>();
            foreach (var model in models)
                model.Init();

            var systems = arch._container.GetInstancesByType<ISystem>();
            foreach (var system in systems)
                system.Init();

            arch._inited = true;
        }

        protected virtual void RegisterModule()
        {
        }

        public void Shutdown()
        {
            OnPreShutdown();

            foreach (var system in _container.GetInstancesByType<ISystem>())
                system.Deinit();
            foreach (var model in _container.GetInstancesByType<IModel>())
                model.Deinit();

            _container.Clear();
            _eventSystem.Clear();
            _instance = null;

            OnPostShutdown();
        }

        protected virtual void OnPreShutdown()
        {
        }

        protected virtual void OnPostShutdown()
        {
        }


        public void RegisterSystem<TSystem>(TSystem system) where TSystem : ISystem
        {
            system.Architecture = this;
            _container.Register(system);

            if (system is IUpdateable updateable)
                _updateables.Add(updateable);
            if (system is IFixedUpdateable fixedUpdateable)
                _fixedUpdateables.Add(fixedUpdateable);

            if (_inited)
                system.Init();
        }

        public void RegisterModel<TModel>(TModel model) where TModel : IModel
        {
            model.Architecture = this;
            _container.Register(model);
            if (_inited)
                model.Init();
        }

        public TSystem GetSystem<TSystem>() where TSystem : class, ISystem
        {
            return _container.Get<TSystem>();
        }

        public TModel GetModel<TModel>() where TModel : class, IModel
        {
            return _container.Get<TModel>();
        }

        public void SendCommand(object sender, ICommand command)
        {
            command.Architecture = this;
            command.Execute(sender);
        }

        public TResult SendCommand<TResult>(object sender, ICommand<TResult> command)
        {
            command.Architecture = this;
            return command.Execute(sender);
        }

        public TResult SendQuery<TResult>(object sender, IQuery<TResult> query)
        {
            query.Architecture = this;
            return query.Do(sender);
        }

        public void SendEvent<TEvent>(object sender, TEvent e) => _eventSystem.Send(sender, e);

        public void SendEvent<TEvent>(object sender) where TEvent : new() => _eventSystem.Send<TEvent>(sender);

        public IUnregister RegisterEvent<TEvent>(Action<TEvent> onEvent) where TEvent : new() => _eventSystem.Register(onEvent);

        public void UnregisterEvent<TEvent>(Action<TEvent> onEvent) where TEvent : new() => _eventSystem.Unregister(onEvent);

        public void Update()
        {
            for (int i = 0; i < _updateables.Count; i++)
            {
                _updateables[i].OnUpdate();
            }
        }

        public void FixedUpdate()
        {
            for (int i = 0; i < _fixedUpdateables.Count; i++)
            {
                _fixedUpdateables[i].OnFixedUpdate();
            }
        }
    }

    #endregion

    #region Rule

    public interface IBelongToArchitecture
    {
        public IArchitecture Architecture { get; set; }
    }

    public interface ICommandSender : IBelongToArchitecture
    {
    }

    public interface IQuerySender : IBelongToArchitecture
    {
    }

    public interface IEventSender : IBelongToArchitecture
    {
    }

    public interface IEventReceiver : IBelongToArchitecture
    {
    }

    public interface ISystemAware : IBelongToArchitecture
    {
    }

    public interface IModelAware : IBelongToArchitecture
    {
    }

    public static class ICommandSenderExtensions
    {
        public static void SendCommand(this ICommandSender self, ICommand command) =>
            self.Architecture.SendCommand(self, command);

        public static TResult SendCommand<TResult>(this ICommandSender self, ICommand<TResult> command) =>
            self.Architecture.SendCommand<TResult>(self, command);
    }

    public static class IQuerySenderExtensions
    {
        public static TResult SendQuery<TResult>(this IQuerySender self, IQuery<TResult> query) =>
            self.Architecture.SendQuery(self, query);
    }

    public static class IEventSenderExtensions
    {
        public static void SendEvent<T>(this IEventSender self, T e)
        {
            self.Architecture.SendEvent(self, e);
        }
    }

    public static class IEventReceiverExtensions
    {
        public static void RegisterEvent<T>(this IEventReceiver self, Action<T> onEvent) where T : new() =>
            self.Architecture.RegisterEvent(onEvent);

        public static void UnregisterEvent<T>(this IEventReceiver self, Action<T> onEvent) where T : new() =>
            self.Architecture.UnregisterEvent(onEvent);
    }

    public static class ISystemAwareExtensions
    {
        public static T GetSystem<T>(this ISystemAware self) where T : class, ISystem =>
            self.Architecture.GetSystem<T>();
    }

    public static class IModelAwareExtensions
    {
        public static T GetModel<T>(this IModelAware self) where T : class, IModel => self.Architecture.GetModel<T>();
    }

    #endregion

    #region Module

    public interface IModel : IEventSender
    {
        void Init();
        void Deinit();
    }

    public interface IController : ISystemAware, IModelAware, ICommandSender, IQuerySender, IEventReceiver
    {
    }

    public interface ISystem : ISystemAware, IModelAware, IEventSender, IEventReceiver
    {
        void Init();
        void Deinit();
    }

    public interface IUpdateable
    {
        void OnUpdate();
    }

    public interface IFixedUpdateable
    {
        void OnFixedUpdate();
    }

    public interface ICommand : ISystemAware, IModelAware, ICommandSender, IQuerySender, IEventSender
    {
        void Execute(object sender);
    }

    public interface ICommand<TResult> : ISystemAware, IModelAware, ICommandSender, IQuerySender, IEventSender
    {
        TResult Execute(object sender);
    }

    public interface IQuery<TResult> : ISystemAware, IModelAware, IQuerySender
    {
        TResult Do(object sender);
    }

    public abstract class AbstractModel : IModel
    {
        public IArchitecture Architecture { get; set; }
        public virtual void Init() { }
        public virtual void Deinit() { }
    }

    public abstract class AbstractSystem : ISystem
    {
        public IArchitecture Architecture { get; set; }
        public virtual void Init() { }
        public virtual void Deinit() { }
    }

    public abstract class AbstractCommand : ICommand
    {
        public IArchitecture Architecture { get; set; }
        public abstract void Execute(object sender);
    }

    public abstract class AbstractCommand<TResult> : ICommand<TResult>
    {
        public IArchitecture Architecture { get; set; }
        public abstract TResult Execute(object sender);
    }

    public abstract class AbstractQuery<TResult> : IQuery<TResult>
    {
        public IArchitecture Architecture { get; set; }
        public abstract TResult Do(object sender);
    }

    #endregion

    #region EventSystem

    public interface IUnregister
    {
        void Unregister();
    }

    public class CommonUnregister : IUnregister
    {
        private Action _onUnregister;

        public CommonUnregister(Action onUnregister)
        {
            _onUnregister = onUnregister;
        }

        public void Unregister()
        {
            _onUnregister?.Invoke();
            _onUnregister = null;
        }
    }

    public interface IEvent
    {
    }

    public class Event : IEvent
    {
        private Action _onEvent;

        public void Trigger()
        {
            _onEvent?.Invoke();
        }

        public IUnregister Register(Action onEvent)
        {
            _onEvent += onEvent;
            return new CommonUnregister(() => _onEvent -= onEvent);
        }

        public void Unregister(Action onEvent)
        {
            _onEvent -= onEvent;
        }
    }

    public class Event<T> : IEvent
    {
        private Action<T> _onEvent;

        public void Trigger(T e)
        {
            _onEvent?.Invoke(e);
        }

        public IUnregister Register(Action<T> onEvent)
        {
            _onEvent += onEvent;
            return new CommonUnregister(() => _onEvent -= onEvent);
        }

        public void Unregister(Action<T> onEvent)
        {
            _onEvent -= onEvent;
        }
    }

    public class Event<T1, T2> : IEvent
    {
        private Action<T1, T2> _onEvent;

        public void Trigger(T1 arg1, T2 arg2)
        {
            _onEvent?.Invoke(arg1, arg2);
        }

        public IUnregister Register(Action<T1, T2> onEvent)
        {
            _onEvent += onEvent;
            return new CommonUnregister(() => _onEvent -= onEvent);
        }

        public void Unregister(Action<T1, T2> onEvent)
        {
            _onEvent -= onEvent;
        }
    }

    public class Event<T1, T2, T3> : IEvent
    {
        private Action<T1, T2, T3> _onEvent;

        public void Trigger(T1 arg1, T2 arg2, T3 arg3)
        {
            _onEvent?.Invoke(arg1, arg2, arg3);
        }

        public IUnregister Register(Action<T1, T2, T3> onEvent)
        {
            _onEvent += onEvent;
            return new CommonUnregister(() => _onEvent -= onEvent);
        }

        public void Unregister(Action<T1, T2, T3> onEvent)
        {
            _onEvent -= onEvent;
        }
    }

    public class EventSystem
    {
        private readonly Dictionary<Type, IEvent> _typeEvents = new Dictionary<Type, IEvent>();

        public static readonly EventSystem Global = new EventSystem();

        // 全局事件发送回调: (senderType, eventData)
        public static event Action<Type, object> OnEventSent;

        public void Clear() => _typeEvents.Clear();

        public void Send<T>(object sender) where T : new()
        {
            var e = new T();
            GetEvent<T>()?.Trigger(e);
            OnEventSent?.Invoke(sender.GetType(), e);
        }

        public void Send<T>(object sender, T e)
        {
            var evt = GetEvent<T>();
            evt?.Trigger(e);
            OnEventSent?.Invoke(sender.GetType(), e);
        }

        public IUnregister Register<T>(Action<T> onEvent) where T : new() => (GetOrAddEvent<T>() as Event<T>)?.Register(onEvent);

        public void Unregister<T>(Action<T> onEvent) => (GetEvent<T>() as Event<T>)?.Unregister(onEvent);

        private Event<T> GetEvent<T>()
        {
            return _typeEvents.TryGetValue(typeof(T), out var e) ? e as Event<T> : default;
        }

        private IEvent GetOrAddEvent<T>() where T : new()
        {
            var eType = typeof(T);
            if (_typeEvents.TryGetValue(eType, out var e))
                return e;

            var t = new Event<T>();
            _typeEvents.Add(eType, t);
            return t;
        }
    }

    #endregion

    #region BindableProperty

    public interface IReadonlyBindableProperty<T>
    {
        T Value { get; }
        IUnregister Register(Action<T> onValueChanged);
        IUnregister RegisterWithInitValue(Action<T> onValueChanged);
        void Unregister(Action<T> onValueChanged);
    }

    public interface IBindableProperty<T> : IReadonlyBindableProperty<T>
    {
        new T Value { get; set; }
        void SetValueWithoutEvent(T newValue);
    }

    public class BindableProperty<T> : IBindableProperty<T>
    {
        private T _value;
        private Event<T> _onValueChanged = new Event<T>();

        public BindableProperty(T defaultValue = default) => _value = defaultValue;

        public T Value
        {
            get => _value;
            set
            {
                if (EqualityComparer<T>.Default.Equals(_value, value)) return;
                _value = value;
                _onValueChanged.Trigger(value);
            }
        }

        public IUnregister Register(Action<T> onValueChanged) => _onValueChanged.Register(onValueChanged);

        public IUnregister RegisterWithInitValue(Action<T> onValueChanged)
        {
            onValueChanged(_value);
            return Register(onValueChanged);
        }

        public void Unregister(Action<T> onValueChanged) => _onValueChanged.Unregister(onValueChanged);

        public void SetValueWithoutEvent(T newValue) => _value = newValue;

        public override string ToString() => _value?.ToString() ?? "null";
    }

    #endregion

    #region BindableList

    [Serializable]
    public class BindableList<T> : Collection<T>
    {
        public BindableList() { }

        public BindableList(IEnumerable<T> collection)
        {
            if (collection == null) throw new ArgumentNullException("collection");
            foreach (var item in collection) Add(item);
        }

        public BindableList(List<T> list) : base(list != null ? new List<T>(list) : null) { }

        protected override void ClearItems()
        {
            var beforeCount = Count;
            base.ClearItems();
            _OnClear?.Trigger();
            if (beforeCount > 0) _OnCountChanged?.Trigger(Count);
        }

        protected override void InsertItem(int index, T item)
        {
            base.InsertItem(index, item);
            _OnAdd?.Trigger(index, item);
            _OnCountChanged?.Trigger(Count);
        }

        public void Move(int oldIndex, int newIndex) => MoveItem(oldIndex, newIndex);

        protected virtual void MoveItem(int oldIndex, int newIndex)
        {
            var item = this[oldIndex];
            base.RemoveItem(oldIndex);
            base.InsertItem(newIndex, item);
            _OnMove?.Trigger(oldIndex, newIndex, item);
        }

        protected override void RemoveItem(int index)
        {
            var item = this[index];
            base.RemoveItem(index);
            _OnRemove?.Trigger(index, item);
            _OnCountChanged?.Trigger(Count);
        }

        protected override void SetItem(int index, T item)
        {
            var oldItem = this[index];
            base.SetItem(index, item);
            _OnReplace?.Trigger(index, oldItem, item);
        }

        [NonSerialized] private Event<int> _OnCountChanged;
        public Event<int> OnCountChanged => _OnCountChanged ??= new Event<int>();

        [NonSerialized] private Event _OnClear;
        public Event OnClear => _OnClear ??= new Event();

        [NonSerialized] private Event<int, T> _OnAdd;
        public Event<int, T> OnAdd => _OnAdd ??= new Event<int, T>();

        [NonSerialized] private Event<int, int, T> _OnMove;
        public Event<int, int, T> OnMove => _OnMove ??= new Event<int, int, T>();

        [NonSerialized] private Event<int, T> _OnRemove;
        public Event<int, T> OnRemove => _OnRemove ??= new Event<int, T>();

        [NonSerialized] private Event<int, T, T> _OnReplace;
        public Event<int, T, T> OnReplace => _OnReplace ??= new Event<int, T, T>();
    }

    public static class BindableListExtensions
    {
        public static BindableList<T> ToBindableList<T>(this IEnumerable<T> self) => new BindableList<T>(self);
    }

    #endregion

    #region BindableDictionary
    
    [Serializable]
    public class BindableDictionary<TKey, TValue> : IDictionary<TKey, TValue>,
        IDictionary, ISerializable, IDeserializationCallback
    {
        private readonly Dictionary<TKey, TValue> mInner;

        public BindableDictionary() => mInner = new Dictionary<TKey, TValue>();
        public BindableDictionary(IEqualityComparer<TKey> comparer) => mInner = new Dictionary<TKey, TValue>(comparer);
        public BindableDictionary(Dictionary<TKey, TValue> innerDictionary) => mInner = innerDictionary;

        public TValue this[TKey key]
        {
            get => mInner[key];
            set
            {
                if (mInner.TryGetValue(key, out var oldValue))
                {
                    mInner[key] = value;
                    mOnReplace?.Trigger(key, oldValue, value);
                }
                else
                {
                    mInner[key] = value;
                    mOnAdd?.Trigger(key, value);
                    mOnCountChanged?.Trigger(Count);
                }
            }
        }

        public int Count => mInner.Count;
        public Dictionary<TKey, TValue>.KeyCollection Keys => mInner.Keys;
        public Dictionary<TKey, TValue>.ValueCollection Values => mInner.Values;

        public void Add(TKey key, TValue value)
        {
            mInner.Add(key, value);
            mOnAdd?.Trigger(key, value);
            mOnCountChanged?.Trigger(Count);
        }

        public void Clear()
        {
            var beforeCount = Count;
            mInner.Clear();
            mOnClear?.Trigger();
            if (beforeCount > 0)
            {
                mOnCountChanged?.Trigger(Count);
            }
        }

        public bool Remove(TKey key)
        {
            if (mInner.TryGetValue(key, out var oldValue))
            {
                if (mInner.Remove(key))
                {
                    mOnRemove?.Trigger(key, oldValue);
                    mOnCountChanged?.Trigger(Count);
                    return true;
                }
            }
            return false;
        }

        public bool ContainsKey(TKey key) => mInner.ContainsKey(key);
        public bool TryGetValue(TKey key, out TValue value) => mInner.TryGetValue(key, out value);
        public Dictionary<TKey, TValue>.Enumerator GetEnumerator() => mInner.GetEnumerator();

        // --- 核心事件系统：已完全对齐你的 Core.cs Event 接口 ---

        [NonSerialized] private Event<int> mOnCountChanged;
        public Event<int> OnCountChanged => mOnCountChanged ??= new Event<int>();

        [NonSerialized] private Event mOnClear;
        public Event OnClear => mOnClear ??= new Event();

        [NonSerialized] private Event<TKey, TValue> mOnAdd;
        public Event<TKey, TValue> OnAdd => mOnAdd ??= new Event<TKey, TValue>();

        [NonSerialized] private Event<TKey, TValue> mOnRemove;
        public Event<TKey, TValue> OnRemove => mOnRemove ??= new Event<TKey, TValue>();

        [NonSerialized] private Event<TKey, TValue, TValue> mOnReplace;
        /// <summary> Trigger参数: (Key, OldValue, NewValue) </summary>
        public Event<TKey, TValue, TValue> OnReplace => mOnReplace ??= new Event<TKey, TValue, TValue>();


        #region IDictionary Explicit Implementation
        object IDictionary.this[object key] { get => this[(TKey)key]; set => this[(TKey)key] = (TValue)value; }
        bool IDictionary.IsFixedSize => ((IDictionary)mInner).IsFixedSize;
        bool IDictionary.IsReadOnly => ((IDictionary)mInner).IsReadOnly;
        bool ICollection.IsSynchronized => ((IDictionary)mInner).IsSynchronized;
        ICollection IDictionary.Keys => ((IDictionary)mInner).Keys;
        object ICollection.SyncRoot => ((IDictionary)mInner).SyncRoot;
        ICollection IDictionary.Values => ((IDictionary)mInner).Values;
        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => ((ICollection<KeyValuePair<TKey, TValue>>)mInner).IsReadOnly;
        ICollection<TKey> IDictionary<TKey, TValue>.Keys => mInner.Keys;
        ICollection<TValue> IDictionary<TKey, TValue>.Values => mInner.Values;
        void IDictionary.Add(object key, object value) => Add((TKey)key, (TValue)value);
        bool IDictionary.Contains(object key) => ((IDictionary)mInner).Contains(key);
        void ICollection.CopyTo(Array array, int index) => ((IDictionary)mInner).CopyTo(array, index);
        public void GetObjectData(SerializationInfo info, StreamingContext context) => ((ISerializable)mInner).GetObjectData(info, context);
        public void OnDeserialization(object sender) => ((IDeserializationCallback)mInner).OnDeserialization(sender);
        void IDictionary.Remove(object key) => Remove((TKey)key);
        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);
        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item) => ((ICollection<KeyValuePair<TKey, TValue>>)mInner).Contains(item);
        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => ((ICollection<KeyValuePair<TKey, TValue>>)mInner).CopyTo(array, arrayIndex);
        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => ((ICollection<KeyValuePair<TKey, TValue>>)mInner).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => mInner.GetEnumerator();
        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        {
            if (TryGetValue(item.Key, out var v) && EqualityComparer<TValue>.Default.Equals(v, item.Value))
            {
                Remove(item.Key);
                return true;
            }
            return false;
        }
        IDictionaryEnumerator IDictionary.GetEnumerator() => ((IDictionary)mInner).GetEnumerator();
        #endregion
    }

    public static class BindableDictionaryExtensions
    {
        public static BindableDictionary<TKey, TValue> ToBindableDictionary<TKey, TValue>(this Dictionary<TKey, TValue> self)
            => new BindableDictionary<TKey, TValue>(self);
    }

    #endregion
}