using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SL.UI
{
	public class ObservableRangeCollection<T> : ObservableCollection<T>
	{
		private bool _suppressNotification = false;

		protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
		{
			if (!_suppressNotification)
				base.OnCollectionChanged(e);
		}

		public void AddRange(IEnumerable<T> items)
		{
			if (items == null) return;

			_suppressNotification = true;
			foreach (var item in items)
				Add(item);
			_suppressNotification = false;

			// itt a Reset eseménnyel értesítjük egyszer
			OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}

		public void ResetWith(IEnumerable<T> items)
		{
			_suppressNotification = true;
			Clear();
			foreach (var item in items)
				Add(item);
			_suppressNotification = false;
			OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}
	}
	public static class ObservableCollectionEx
	{
		public static void AddRange<T>(this ObservableCollection<T> col, IEnumerable<T> items)
		{
			if (items is null) return;
			foreach (var i in items) col.Add(i); // egyszerű út: ha akadozik, lásd Reset-es verzió lent
		}

		// Gyorsabb: 1 Reset értesítés (UI teljes újrarajz), nagy listáknál jobb
		public static void ResetWith<T>(this ObservableCollection<T> col, IEnumerable<T> items)
		{
			col.Clear();
			foreach (var i in items) col.Add(i);

			// Disambiguate the protected OnCollectionChanged method by specifying the exact signature
			var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
			var onCollectionChanged = typeof(ObservableCollection<T>)
				.GetMethod(
					"OnCollectionChanged",
					BindingFlags.Instance | BindingFlags.NonPublic,
					binder: null,
					types: new[] { typeof(NotifyCollectionChangedEventArgs) },
					modifiers: null);

			onCollectionChanged?.Invoke(col, new object[] { args });
		}
	}
}
