using System.Collections.Frozen;
using System.Diagnostics;

namespace PandoraIO
{

	public class ConfigManager(string path)
	{
#if ANDROID
		private static string _config_path = FileSystem.AppDataDirectory + "/data/configs/app.cfg";
#else
		private static string _config_path = Path.GetFullPath("data/configs/app.cfg");
#endif
		public static string ConfigPath
		{
			get
			{
				return _config_path;
			}
			set
			{
				if (string.IsNullOrWhiteSpace(value))
				{
					throw new ArgumentException("Config path cannot be null or whitespace.", nameof(value));
				}
				if (!Path.IsPathRooted(value))
				{
					value = Path.GetFullPath(value);
				}
				if (!File.Exists(value))
				{
					var dir = Path.GetDirectoryName(value);
					if (dir is not null && !Directory.Exists(dir))
					{
						Directory.CreateDirectory(dir);
					}
				}
				StaticInstance = new ConfigManager(value);
				_config_path = value;
			}
		}

		private static ConfigManager _instance = new(ConfigPath);
		public static ConfigManager StaticInstance
		{
			get
			{
				if (!_instance.IsOpen) _instance.CreateOrOpen();
				return _instance;
			}
			private set
			{
				_instance = value;
				_instance ??= new ConfigManager(ConfigPath);
				_instance.CreateOrOpen(false);
			}
		}

		public static Dictionary<string, ConfigManager> Instances = new Dictionary<string, ConfigManager>();

		public static void CreateNamed(string name, string cfg_path, bool openOnCreate = false)
		{
			ConfigManager cfgm = new ConfigManager(cfg_path);
			if (openOnCreate) cfgm.CreateOrOpen();
			if(!Instances.ContainsKey(name)) Instances.Add(name, cfgm);
		}

		public static List<string> GetStoredConfigManagers()
		{
			return Instances.Keys.ToList();
		}


		StreamWriter? infoWriter;
		StreamReader? infoReader;
		bool _isOpen = false;
		public bool IsOpen => _isOpen;

		Dictionary<string, string> infoCache = new();

		public void LoadCache()
		{
			if (infoReader is null) return;
			infoCache.Clear();
			infoReader.BaseStream.Seek(0, SeekOrigin.Begin);
			while (!infoReader.EndOfStream)
			{
				var line = infoReader.ReadLine();
				if (line is null) continue;
				var split = line.Split('=', 2);
				if (split.Length != 2) continue;
				infoCache[split[0]] = split[1].Trim('"');
			}
		}

		private void SaveCache()
		{
			if (infoWriter is null) return;
			infoWriter.BaseStream.SetLength(0);
			infoWriter.Flush();
			foreach (var kv in infoCache)
			{
				infoWriter.WriteLine($"{kv.Key}=\"{kv.Value.Trim('"')}\"");
			}
			infoWriter.Flush();
		}

		public void CreateOrOpen(bool forceRecreate = false)
		{
			if (!Directory.Exists(Path.GetDirectoryName(path))) Directory.CreateDirectory(Path.GetDirectoryName(path));
			if (forceRecreate)
			{
				infoWriter?.Flush();
				infoWriter?.Close();
				infoWriter?.Dispose();
				infoWriter = null;
				if(File.Exists(path)) File.Delete(path);
				infoWriter = new StreamWriter(new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite));
				//infoWriter = new(File.Create(path), new FileStreamOptions() { Share = FileShare.ReadWrite, Access = FileAccess.Write });
				infoReader = infoWriter.BaseStream.CanRead ? new StreamReader(infoWriter.BaseStream) : null;
				_isOpen = true;
				return;
			}
			infoWriter ??= new(File.Exists(path) ? new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite) : new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite));

			infoReader ??= infoWriter.BaseStream.CanRead ? new StreamReader(infoWriter.BaseStream) : null;
			_isOpen = true;
			LoadCache();
		}

		public string? GetValue(string key)
		{
			if (infoReader is null)
			{
				throw new InvalidOperationException("Instance info file is not opened. Call CreateOrOpen() first.");
			}
			infoCache.Select(info => KeyValuePair.Create(info.Key.ToLower(),info.Value)).ToDictionary().TryGetValue(key.ToLower(), out var value);
			return value;
		}

		public T? GetValue<T>(string key) where T : struct
		{
			if (infoReader is null)
			{
				throw new InvalidOperationException("Instance info file is not opened. Call CreateOrOpen() first.");
			}
			if (!infoCache.Select(info => KeyValuePair.Create(info.Key.ToLower(), info.Value)).ToDictionary().TryGetValue(key, out var value)) return null;
			try
			{
				if (!typeof(T).IsEnum)
				{
					T _result = (T)Convert.ChangeType(value, typeof(T));
					return _result;
				}
				T? result = null;
				string[] enums = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

				foreach (string _enum in enums)
				{
					if (Enum.TryParse<T>(_enum, out var parsed_result))
					{
						if (result is null)
							result = parsed_result;
						else
						{
							ulong resVal = Convert.ToUInt64(result.Value);
							ulong parsedVal = Convert.ToUInt64(parsed_result);
							result = (T)Enum.ToObject(typeof(T), resVal | parsedVal);
						}
					}
				}

				return result;
			}
			catch
			{
				return null;
			}
		}

		public bool TryGetValue(string key, out string? value)
		{
			if (infoReader is null)
			{
				throw new InvalidOperationException("Instance info file is not opened. Call CreateOrOpen() first.");
			}
			bool res = infoCache.Select(info => KeyValuePair.Create(info.Key.ToLower(), info.Value)).ToDictionary().TryGetValue(key, out value);
			return res && value is not null;
		}

		public bool TryGetValue<T>(string key, out T? value) where T : struct
		{
			if (infoReader is null)
			{
				throw new InvalidOperationException("Instance info file is not opened. Call CreateOrOpen() first.");
			}
			value = GetValue<T>(key);
			return value.HasValue && value is not null;
		}

		public void SetValue(string key, string value)
		{
			if (infoWriter is null)
			{
				throw new InvalidOperationException("Instance info file is not opened. Call CreateOrOpen() first.");
			}
			infoCache[key] = value.Trim('"');
			SaveCache();
		}

		public void SetValue<TEnum>(string key, TEnum value) where TEnum : struct, Enum
		{
			var enumType = typeof(TEnum);
			string result;

			if (Attribute.IsDefined(enumType, typeof(FlagsAttribute)))
			{
				var names = new List<string>();
				ulong input = Convert.ToUInt64(value);

				var defined = Enum.GetValues(enumType)
					.Cast<object>()
					.Select(v => new { Val = Convert.ToUInt64(v), Name = Enum.GetName(enumType, v) })
					.Where(x => x.Name is not null && x.Val != 0)
					.OrderByDescending(x => x.Val);

				foreach (var x in defined)
				{
					if ((input & x.Val) == x.Val)
					{
						names.Add(x.Name!);
						input &= ~x.Val;
					}
				}

				if (names.Count == 0)
				{
					if (Convert.ToUInt64(value) == 0)
					{
						var zeroName = Enum.GetName(enumType, Enum.ToObject(enumType, 0));
						if (zeroName is not null) names.Add(zeroName);
					}
					else
					{
						// Fallback: unknown combination, keep default string representation
						result = value.ToString();
						SetValue(key, result);
						return;
					}
				}

				result = string.Join(",", names);
			}
			else
			{
				result = Enum.GetName(enumType, value) ?? value.ToString();
			}

			SetValue(key, result);
		}



		public void RemoveKey(string key)
		{
			if (infoWriter is null)
			{
				throw new InvalidOperationException("Instance info file is not opened. Call CreateOrOpen() first.");
			}
			if (infoCache.Remove(key))
			{
				SaveCache();
			}
		}

		public bool ContainsKey(string key)
		{
			if (infoReader is null)
			{
				throw new InvalidOperationException("Instance info file is not opened. Call CreateOrOpen() first.");
			}
			return infoCache.ContainsKey(key);
		}

		public string GetKey(string key) => infoCache.Keys.FirstOrDefault(k => k.ToLower() == key.ToLower(), "null");

		public bool IsEmpty()
		{
			if (infoReader is null)
			{
				throw new InvalidOperationException("Instance info file is not opened. Call CreateOrOpen() first.");
			}
			return infoCache.Count == 0;
		}

		public bool HasKey(string key)
		{
			if (infoReader is null)
			{
				throw new InvalidOperationException("Instance info file is not opened. Call CreateOrOpen() first.");
			}
			return infoCache.ContainsKey(key);
		}

		public FrozenDictionary<string, string> GetAllKeyValue()
		{
			if (infoReader is null)
			{
				throw new InvalidOperationException("Instance info file is not opened. Call CreateOrOpen() first.");
			}
			return infoCache.ToFrozenDictionary();
		}

		public FrozenDictionary<string, T> GetAllKeyValue<T>() where T : struct
		{
			if (infoReader is null)
			{
				throw new InvalidOperationException("Instance info file is not opened. Call CreateOrOpen() first.");
			}
			var dict = new Dictionary<string, T>();
			foreach (var kv in infoCache)
			{
				var val = GetValue<T>(kv.Key);
				if (val.HasValue)
				{
					dict[kv.Key] = val.Value;
				}
			}
			return dict.ToFrozenDictionary();
		}

		public string[] GetKeys()
		{
			if (infoReader is null)
			{
				throw new InvalidOperationException("Instance info file is not opened. Call CreateOrOpen() first.");
			}
			return infoCache.Keys.ToArray();
		}

		public string this[string key]
		{
			get => GetValue(key) ?? string.Empty;
			set => SetValue(key, value);
		}


		~ConfigManager()
		{
			infoWriter?.Flush();
			infoWriter?.Close();
			infoWriter?.Dispose();

			infoReader?.Close();
			infoReader?.Dispose();
		}
	}
}
