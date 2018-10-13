using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Threading;

namespace GO.Collections
{
    // All none UI threads calls are delegated to the UI thread
    // None UI threads are not allowed to subscribe to INotifyCollectionChanged
    // UI thread INotifyCollectionChanged event handlers are not allowed to modify the collection (reentrancy is blocked)
    // GetEnumerator methods return a snapshot
    public class ConcurrentObservableCollection<T> : IList<T>, INotifyCollectionChanged, INotifyPropertyChanged where T : class
    {
        #region ctor

        protected List<T> m_list;

        public ConcurrentObservableCollection()
        {
            m_list = new List<T>();
            VerifyDispatherExists();
        }

        public ConcurrentObservableCollection(List<T> list)
        {
            m_list = new List<T>(list);
            VerifyDispatherExists();
        }

        public ConcurrentObservableCollection(IEnumerable<T> list)
        {
            m_list = new List<T>(list);
            VerifyDispatherExists();
        }

        protected virtual void VerifyDispatherExists()
        {
            if (System.Windows.Application.Current == null || System.Windows.Application.Current.Dispatcher == null)
                throw new Exception("Dispatcher is missing");
        }

        protected virtual bool CheckAccess()
        {
            return System.Windows.Application.Current.Dispatcher.CheckAccess();
        }

        protected virtual Dispatcher Dispatcher
        {
            get { return System.Windows.Application.Current.Dispatcher; }
        }

        #endregion

        #region public

        private object m_syncRoot = new object();
        public object SyncRoot
        {
            get { return m_syncRoot; }
        }

        private DispatcherPriority m_priority = DispatcherPriority.Background;
        public DispatcherPriority Priority
        {
            get { return m_priority; }
            set { m_priority = value; }
        }

        #endregion

        #region IList<T>

        public T this[int index]
        {
            get
            {
                if (!CheckAccess())
                {
                    using (Lock.This(SyncRoot))
                        Dispatcher.Invoke(() => { return this[index]; }, Priority);
                }
                return m_list[index];
            }
            set
            {
                if (!CheckAccess())
                {
                    using (Lock.This(SyncRoot))
                        Dispatcher.Invoke(() => { this[index] = value; }, Priority);
                }
                CheckReentrancy();
                SetItem(index, value);
            }
        }

        // Not delegating to UI thread...
        public int Count
        {
            get { return m_list.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public void Add(T item)
        {
            if (!CheckAccess())
            {
                using (Lock.This(SyncRoot))
                    Dispatcher.Invoke(() => { Add(item); }, Priority);
            }
            CheckReentrancy();
            AddItem(item);
        }

        public void Clear()
        {
            if (!CheckAccess())
            {
                using (Lock.This(SyncRoot))
                    Dispatcher.Invoke(() => { Clear(); }, Priority);
            }
            CheckReentrancy();
            ClearItems();
        }

        public bool Contains(T item)
        {
            if (!CheckAccess())
            {
                using (Lock.This(SyncRoot))
                    Dispatcher.Invoke(() => { Contains(item); }, Priority);
            }
            return ContainsItem(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            if (!CheckAccess())
            {
                using (Lock.This(SyncRoot))
                    Dispatcher.Invoke(() => { CopyTo(array, arrayIndex); }, Priority);
            }
            m_list.CopyTo(array, arrayIndex);
        }

        // Note: Snapshot!
        public IEnumerator<T> GetEnumerator()
        {
            if (!CheckAccess())
            {
                using (Lock.This(SyncRoot))
                    Dispatcher.Invoke(() => { GetEnumerator(); }, Priority);
            }
            return m_list.ToList().GetEnumerator();
        }

        public int IndexOf(T item)
        {
            if (!CheckAccess())
            {
                using (Lock.This(SyncRoot))
                    Dispatcher.Invoke(() => { return IndexOf(item); }, Priority);
            }
            return m_list.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            if (!CheckAccess())
            {
                using (Lock.This(SyncRoot))
                    Dispatcher.Invoke(() => { Insert(index, item); }, Priority);
            }
            CheckReentrancy();
            InsertItem(index, item);
        }

        public bool Remove(T item)
        {
            if (!CheckAccess())
            {
                using (Lock.This(SyncRoot))
                    Dispatcher.Invoke(() => { Remove(item); }, Priority);
            }
            CheckReentrancy();
            return RemoveItem(item);
        }

        public void RemoveAt(int index)
        {
            if (!CheckAccess())
            {
                using (Lock.This(SyncRoot))
                    Dispatcher.Invoke(() => { RemoveAt(index); }, Priority);
            }
            CheckReentrancy();
            RemoveItemAt(index);
        }

        // Note: Snapshot!
        IEnumerator IEnumerable.GetEnumerator()
        {
            if (!CheckAccess())
            {
                using (Lock.This(SyncRoot))
                    Dispatcher.Invoke(() => { return ((IEnumerable)this).GetEnumerator(); }, Priority);
            }
            return m_list.ToList().GetEnumerator();
        }

        #endregion

        #region Protected Virtual

        protected virtual void ClearItems()
        {
            m_list.Clear();
            OnPropertyChanged("Count");
            OnPropertyChanged("Item[]");
            OnCollectionReset();
        }

        protected virtual bool ContainsItem(T item)
        {
            return m_list.Contains(item);
        }

        protected virtual void AddItem(T item)
        {
            var index = m_list.Count;
            m_list.Insert(m_list.Count, item);
            OnPropertyChanged("Count");
            OnPropertyChanged("Item[]");
            OnCollectionChanged(NotifyCollectionChangedAction.Add, item, index);
        }

        protected virtual void InsertItem(int index, T item)
        {
            m_list.Insert(index, item);
            OnPropertyChanged("Count");
            OnPropertyChanged("Item[]");
            OnCollectionChanged(NotifyCollectionChangedAction.Add, item, index);
        }

        protected virtual void SetItem(int index, T newItem)
        {
            T oldItem = m_list[index];
            m_list[index] = newItem;
            OnPropertyChanged("Item[]");
            OnCollectionChanged(NotifyCollectionChangedAction.Replace, newItem, oldItem, index);
        }

        protected virtual bool RemoveItem(T item)
        {
            var index = m_list.IndexOf(item);
            if (index < 0)
                return false;
            RemoveItemAt(index);
            return true;
        }

        protected virtual void RemoveItemAt(int index)
        {
            T item = m_list[index];
            m_list.RemoveAt(index);
            OnPropertyChanged("Count");
            OnPropertyChanged("Item[]");
            OnCollectionChanged(NotifyCollectionChangedAction.Remove, item, index);
        }

        #endregion

        #region IPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region INotifyCollectionChanged

        private List<NotifyCollectionChangedEventHandler> m_collectionChanged = new List<NotifyCollectionChangedEventHandler>();

        public event NotifyCollectionChangedEventHandler CollectionChanged
        {
            add
            {
                if (!CheckAccess())
                    throw new InvalidOperationException("Only the UI thread can subscribe to this event");
                m_collectionChanged.Add(value);
            }
            remove
            {
                if (!CheckAccess())
                    throw new InvalidOperationException("Only the UI thread can subscribe to this event");
                m_collectionChanged.Remove(value);
            }
        }

        // https://blog.stephencleary.com/2009/07/interpreting-notifycollectionchangedeve.html
        // https://blogs.msdn.microsoft.com/xtof/2008/02/10/making-sense-of-notifycollectionchangedeventargs/
        private void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (m_collectionChanged.Count > 0)
            {
                using (BlockReentrancy())
                {
                    foreach (var handler in m_collectionChanged)
                        handler(this, e);
                }
            }
        }

        protected void OnCollectionChanged(NotifyCollectionChangedAction action, object changedItem, int index)
        {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, changedItem, index));
        }

        protected void OnCollectionChanged(NotifyCollectionChangedAction action, object newItem, object oldItem, int index)
        {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, newItem, oldItem, index));
        }

        protected void OnCollectionReset()
        {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        #endregion

        #region Reentrancy

        private class ReentrancyMonitor : IDisposable
        {
            private int m_lockCount;
            public bool IsLocked => m_lockCount > 0;
            public void Enter() { m_lockCount++; }
            public void Dispose() { m_lockCount--; }
        }

        private ReentrancyMonitor m_monitor = new ReentrancyMonitor();

        private IDisposable BlockReentrancy()
        {
            m_monitor.Enter();
            return m_monitor;
        }

        // https://stackoverflow.com/questions/6247427/blockreentrancy-in-observablecollectiont
        // But really. Allowing reentrancy, when just 1 subscriber exists, is just higher order idiocy
        private void CheckReentrancy()
        {
            if (m_monitor.IsLocked && m_collectionChanged.Count > 1)
                throw new InvalidOperationException("ConcurrentObservableCollection: It's not allowed to modify the collection within the event handler");
        }

        #endregion
    }
}
