using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

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
        void SendCommand(ICommand command);
        TResult SendCommand<TResult>(ICommand<TResult> command);
        TResult SendQuery<TResult>(IQuery<TResult> query);
        void SendEvent<T>(T e);
        IUnRegister RegisterEvent<T>(Action<T> onEvent);
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

        public void SendCommand(ICommand command)
        {
            command.Architecture = this;
            command.Execute();
        }

        public TResult SendCommand<TResult>(ICommand<TResult> command)
        {
            command.Architecture = this;
            return command.Execute();
        }

        public TResult SendQuery<TResult>(IQuery<TResult> query)
        {
            query.Architecture = this;
            return query.Do();
        }

        public void SendEvent<TEvent>(TEvent e) => _eventSystem.Send(e);
        public IUnRegister RegisterEvent<TEvent>(Action<TEvent> onEvent) => _eventSystem.Register(onEvent);
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
            self.Architecture.SendCommand(command);

        public static TResult SendCommand<TResult>(this ICommandSender self, ICommand<TResult> command) =>
            self.Architecture.SendCommand<TResult>(command);
    }

    public static class IQuerySenderExtensions
    {
        public static TResult SendQuery<TResult>(this IQuerySender self, IQuery<TResult> query) =>
            self.Architecture.SendQuery(query);
    }

    public static class IEventSenderExtensions
    {
        public static void SendEvent<T>(this IEventSender self, T e) => self.Architecture.SendEvent(e);
    }

    public static class IEventReceiverExtensions
    {
        public static void RegisterEvent<T>(this IEventReceiver self, Action<T> onEvent) =>
            self.Architecture.RegisterEvent(onEvent);
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

    public interface ICommand : ISystemAware, IModelAware, ICommandSender, IQuerySender, IEventSender
    {
        void Execute();
    }

    public interface ICommand<TResult> : ISystemAware, IModelAware, ICommandSender, IQuerySender, IEventSender
    {
        TResult Execute();
    }

    public interface IQuery<TResult> : ISystemAware, IModelAware, IQuerySender
    {
        TResult Do();
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
        public abstract void Execute();
    }

    public abstract class AbstractCommand<TResult> : ICommand<TResult>
    {
        public IArchitecture Architecture { get; set; }
        public abstract TResult Execute();
    }

    public abstract class AbstractQuery<TResult> : IQuery<TResult>
    {
        public IArchitecture Architecture { get; set; }
        public abstract TResult Do();
    }

    #endregion

    #region EventSystem

    public interface IUnRegister
    {
        void UnRegister();
    }

    public class CommonUnRegister : IUnRegister
    {
        private Action _onUnRegister;

        public CommonUnRegister(Action onUnRegister)
        {
            _onUnRegister = onUnRegister;
        }

        public void UnRegister()
        {
            _onUnRegister?.Invoke();
            _onUnRegister = null;
        }
    }

    public interface IEvent
    {
    }

    public class Event : IEvent
    {
        private Action _onEvent = () => { };

        public void Trigger()
        {
            _onEvent?.Invoke();
        }

        public IUnRegister Register(Action onEvent)
        {
            _onEvent += onEvent;
            return new CommonUnRegister(() => _onEvent -= onEvent);
        }

        public void UnRegister(Action onEvent)
        {
            _onEvent -= onEvent;
        }
    }

    public class Event<T> : IEvent
    {
        private Action<T> _onEvent = _ => { };

        public void Trigger(T e)
        {
            _onEvent?.Invoke(e);
        }

        public IUnRegister Register(Action<T> onEvent)
        {
            _onEvent += onEvent;
            return new CommonUnRegister(() => _onEvent -= onEvent);
        }

        public void UnRegister(Action<T> onEvent)
        {
            _onEvent -= onEvent;
        }
    }

    public class Event<T1, T2> : IEvent
    {
        private Action<T1, T2> _onEvent = (_, _) => { };

        public void Trigger(T1 arg1, T2 arg2)
        {
            _onEvent?.Invoke(arg1, arg2);
        }

        public IUnRegister Register(Action<T1, T2> onEvent)
        {
            _onEvent += onEvent;
            return new CommonUnRegister(() => _onEvent -= onEvent);
        }

        public void UnRegister(Action<T1, T2> onEvent)
        {
            _onEvent -= onEvent;
        }
    }

    public class Event<T1, T2, T3> : IEvent
    {
        private Action<T1, T2, T3> _onEvent = (_, _, _) => { };

        public void Trigger(T1 arg1, T2 arg2, T3 arg3)
        {
            _onEvent?.Invoke(arg1, arg2, arg3);
        }

        public IUnRegister Register(Action<T1, T2, T3> onEvent)
        {
            _onEvent += onEvent;
            return new CommonUnRegister(() => _onEvent -= onEvent);
        }

        public void UnRegister(Action<T1, T2, T3> onEvent)
        {
            _onEvent -= onEvent;
        }
    }

    public class EventSystem
    {
        private readonly Dictionary<Type, IEvent> _typeEvents = new Dictionary<Type, IEvent>();

        public static readonly EventSystem Global = new EventSystem();

        public void Clear() => _typeEvents.Clear();

        public void Send<T>() where T : new() => GetEvent<Event<T>>()?.Trigger(new T());

        public void Send<T>(T e) => GetEvent<Event<T>>()?.Trigger(e);

        public IUnRegister Register<T>(Action<T> onEvent) => GetOrAddEvent<Event<T>>().Register(onEvent);

        public void UnRegister<T>(Action<T> onEvent) => GetEvent<Event<T>>()?.UnRegister(onEvent);

        private T GetEvent<T>() where T : IEvent
        {
            return _typeEvents.TryGetValue(typeof(T), out var e) ? (T)e : default;
        }

        private T GetOrAddEvent<T>() where T : IEvent, new()
        {
            var eType = typeof(T);
            if (_typeEvents.TryGetValue(eType, out var e))
                return (T)e;

            var t = new T();
            _typeEvents.Add(eType, t);
            return t;
        }
    }

    #endregion

    #region BindableProperty

    public interface IReadonlyBindableProperty<T>
    {
        T Value { get; }
        IUnRegister Register(Action<T> onValueChanged);
        IUnRegister RegisterWithInitValue(Action<T> onValueChanged);
        void UnRegister(Action<T> onValueChanged);
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

        public IUnRegister Register(Action<T> onValueChanged) => _onValueChanged.Register(onValueChanged);

        public IUnRegister RegisterWithInitValue(Action<T> onValueChanged)
        {
            onValueChanged(_value);
            return Register(onValueChanged);
        }

        public void UnRegister(Action<T> onValueChanged) => _onValueChanged.UnRegister(onValueChanged);

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
            mOnClear?.Trigger();
            if (beforeCount > 0) mOnCountChanged?.Trigger(Count);
        }

        protected override void InsertItem(int index, T item)
        {
            base.InsertItem(index, item);
            mCollectionAdd?.Trigger(index, item);
            mOnCountChanged?.Trigger(Count);
        }

        public void Move(int oldIndex, int newIndex) => MoveItem(oldIndex, newIndex);

        protected virtual void MoveItem(int oldIndex, int newIndex)
        {
            var item = this[oldIndex];
            base.RemoveItem(oldIndex);
            base.InsertItem(newIndex, item);
            mOnMove?.Trigger(oldIndex, newIndex, item);
        }

        protected override void RemoveItem(int index)
        {
            var item = this[index];
            base.RemoveItem(index);
            mOnRemove?.Trigger(index, item);
            mOnCountChanged?.Trigger(Count);
        }

        protected override void SetItem(int index, T item)
        {
            var oldItem = this[index];
            base.SetItem(index, item);
            mOnReplace?.Trigger(index, oldItem, item);
        }

        [NonSerialized] private Event<int> mOnCountChanged;
        public Event<int> OnCountChanged => mOnCountChanged ?? (mOnCountChanged = new Event<int>());

        [NonSerialized] private Event mOnClear;
        public Event OnClear => mOnClear ?? (mOnClear = new Event());

        [NonSerialized] private Event<int, T> mCollectionAdd;
        public Event<int, T> OnAdd => mCollectionAdd ?? (mCollectionAdd = new Event<int, T>());

        [NonSerialized] private Event<int, int, T> mOnMove;
        public Event<int, int, T> OnMove => mOnMove ?? (mOnMove = new Event<int, int, T>());

        [NonSerialized] private Event<int, T> mOnRemove;
        public Event<int, T> OnRemove => mOnRemove ?? (mOnRemove = new Event<int, T>());

        [NonSerialized] private Event<int, T, T> mOnReplace;
        public Event<int, T, T> OnReplace => mOnReplace ?? (mOnReplace = new Event<int, T, T>());
    }

    public static class BindableListExtensions
    {
        public static BindableList<T> ToBindableList<T>(this IEnumerable<T> self) => new BindableList<T>(self);
    }

    #endregion
}