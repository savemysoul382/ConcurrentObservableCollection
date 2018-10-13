# ConcurrentObservableCollection

1. Multi Thread ObservableCollection with constraints.  
1. None UI threads are delegated to the UI thread
2. None UI threads are not allowed to subscribe to INotifyCollectionChanged
3. UI thread INotifyCollectionChanged event handlers are not allowed to modify the collection (reentrancy is blocked)
4. GetEnumerator methods return a snapshot
