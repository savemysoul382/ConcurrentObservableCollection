# ConcurrentObservableCollection

All none UI threads calls are delegated to the UI thread
None UI threads are not allowed to subscribe to INotifyCollectionChanged
UI thread INotifyCollectionChanged event handlers are not allowed to modify the collection (reentrancy is blocked)
GetEnumerator methods return a snapshot
