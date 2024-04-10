// Copyright (c) 2011-2023 Roland Pheasant. All rights reserved.
// Roland Pheasant licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Kernel;

namespace GuitarConfigurator.NetCore;

internal class Filter<T>(IObservable<IChangeSet<T>> source, Func<T, bool> predicate)
    where T : notnull
{
    private readonly Func<T, bool> _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));

    private readonly IObservable<IChangeSet<T>> _source = source ?? throw new ArgumentNullException(nameof(source));

    public IObservable<IChangeSet<T>> Run() => Observable.Create<IChangeSet<T>>(
        observer =>
        {
            var locker = new object();

            var all = new List<ItemWithMatch>();
            var filtered = new ChangeAwareList<ItemWithMatch>();
            
            /*
             * Apply the transform operator so 'IsMatch' state can be evaluated and captured one time only
             * This is to eliminate the need to re-apply the predicate when determining whether an item was previously matched,
             * which is essential when we have mutable state
             */

            // Need to get item by index and store it in the transform
            var filteredResult = _source.Synchronize(locker).Transform<T, ItemWithMatch>(
                    (t, previous) =>
                    {
                        var wasMatch = previous.ConvertOr(p => p!.IsMatch, () => false);
                        return new ItemWithMatch(t, _predicate(t), wasMatch);
                    },
                    true)
                .Select(changes =>
                {
                    
                    all.Clone(changes);

                    return Process(filtered, all, changes);
                });

            return filteredResult
                .Select(changes => changes.Transform(iwm => iwm.Item)) // use convert, not transform
                .SubscribeSafe(observer);
        });

    private static IChangeSet<ItemWithMatch> Process(ChangeAwareList<ItemWithMatch> filtered, List<ItemWithMatch> all, IChangeSet<ItemWithMatch> changes)
    {
        // Maintain all items as well as filtered list. This enables us to a) re-query when the predicate changes b) check the previous state when Refresh is called
        foreach (var item in changes)
        {
            switch (item.Reason)
            {
                case ListChangeReason.Add:
                {
                    var change = item.Item;
                    if (change.Current.IsMatch)
                    {
                        filtered.Add(change.Current);
                    }

                    break;
                }

                case ListChangeReason.AddRange:
                {
                    var matches = item.Range.Where(t => t.IsMatch).ToList();
                    filtered.AddRange(matches);
                    break;
                }

                case ListChangeReason.Replace:
                {
                    var change = item.Item;
                    var match = change.Current.IsMatch;
                    var wasMatch = item.Item.Current.WasMatch;
                    if (match)
                    {
                        if (wasMatch)
                        {
                            // an update, so get the latest index and pass the index up the chain
                            var previous = filtered.Select(x => x.Item).IndexOfOptional(change.Previous.Value.Item).ValueOrThrow(() => new InvalidOperationException($"Cannot find index of {typeof(T).Name} -> {change.Previous.Value}. Expected to be in the list"));

                            // replace inline
                            filtered[previous.Index] = change.Current;
                        }
                        else
                        {
                            filtered.Add(change.Current);
                        }
                    }
                    else if (wasMatch)
                    {
                        filtered.Remove(change.Previous.Value);
                    }

                    break;
                }
                
                case ListChangeReason.Moved:
                {
                    if (item.Item.Current.IsMatch)
                    {
                        // Only support move if all the items are currently visible
                        // Stupid, but for our purposes this works just fine
                        if (all.Count == filtered.Count)
                        {
                            filtered.Move(item.Item.PreviousIndex, item.Item.CurrentIndex);
                        }
                    }
                    break;
                }

                case ListChangeReason.Refresh:
                {
                    var change = item.Item;
                    var match = change.Current.IsMatch;
                    var wasMatch = item.Item.Current.WasMatch;
                    if (match)
                    {
                        if (wasMatch)
                        {
                            // an update, so get the latest index and pass the index up the chain
                            var previous = filtered.Select(x => x.Item).IndexOfOptional(change.Current.Item).ValueOrThrow(() => new InvalidOperationException($"Cannot find index of {typeof(T).Name} -> {change.Previous.Value}. Expected to be in the list"));

                            filtered.RefreshAt(previous.Index);
                        }
                        else
                        {
                            filtered.Add(change.Current);
                        }
                    }
                    else if (wasMatch)
                    {
                        filtered.Remove(change.Current);
                    }

                    break;
                }

                case ListChangeReason.Remove:
                {
                    filtered.Remove(item.Item.Current);
                    break;
                }

                case ListChangeReason.RemoveRange:
                {
                    filtered.RemoveMany(item.Range);
                    break;
                }

                case ListChangeReason.Clear:
                {
                    if (filtered.Count == item.Range.Count)
                    {
                        filtered.Clear();
                    }
                    else
                    {
                        filtered.RemoveMany(item.Range);
                    }
                    break;
                }
            }
        }

        return filtered.CaptureChanges();
    }
    
    private sealed class ItemWithMatch(T item, bool isMatch, bool wasMatch = false) : IEquatable<ItemWithMatch>
    {
        public T Item { get; } = item;

        public bool IsMatch { get; set; } = isMatch;

        public bool WasMatch { get; set; } = wasMatch;

        public static bool operator ==(ItemWithMatch? left, ItemWithMatch? right) =>
            Equals(left, right);

        public static bool operator !=(ItemWithMatch? left, ItemWithMatch? right) =>
            !Equals(left, right);

        public bool Equals(ItemWithMatch? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return EqualityComparer<T>.Default.Equals(Item, other.Item);
        }

        public override bool Equals(object? obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((ItemWithMatch)obj);
        }

        public override int GetHashCode() => EqualityComparer<T>.Default.GetHashCode(Item!);

        public override string ToString() => $"{Item}, (was {IsMatch} is {WasMatch}";
    }
}