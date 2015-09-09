// /*
//  * Copyright (C) 2015 Mikko Taivainen <mikko.taivainen@gmail.com>
//  */

using System;
using System.Collections;
using System.Collections.Generic;

namespace Util
{
#if EXAMPLES
	class LogExample {
		private static readonly Log = new Log(typeof(LogExample));
		public void FooMethod() { 

			// Output a debug message: [DEBUG] 10:43:27.439 LogExample - Foo happened: ...
			log.Debug("Foo happened: " + foo);

			// Output a debug message: [DEBUG] 10:43:27.439 LogExample - Foo happened: ...
			// This version is more efficient if the log level Debug is disabled (foo.ToString() is not called)
			log.DebugFormat("Foo happened: {0}", foo);


			// We can obtain the Log object by extension method GetLog():
			this.GetLog().DebugFormat("Foo happended: {0}", foo);

			// We can also use extension methods LogTrace, LogDebug, etc.:
			this.LogDebugFormat("Foo happended: {0}", foo);
		}
	}
#endif


#if INCLUDE_UTIL_LOG
	public class LogConfiguration
	{
		private string namePrefix;
		private Log.Level levelThreshold;
		public LogConfiguration (string name, Log.Level levelThreshold)
		{
			this.namePrefix = name;
			this.levelThreshold = levelThreshold;
		}
		public string NamePrefix {
			get {
				return namePrefix;
			}
		}
		public Log.Level LevelThreshold {
			get {
				return levelThreshold;
			}
			set {
				this.levelThreshold = value;
			}
		}
	}

	public class DefaultLogConfiguration : LogConfiguration
	{
		public DefaultLogConfiguration () : base("", Log.Level.Info)
		{
		}
	}


	public class Log
	{
		public enum Level : int
		{
			Trace = 0,
			Debug,
			Info,
			Warning,
			Error,
		}
		private static readonly string[] LEVEL_NAMES = {"TRACE", "DEBUG", "INFO", "WARN", "ERROR"};

		private class ConfigurationResolver
		{
			private static readonly LogConfiguration DEFAULT = new DefaultLogConfiguration ();

			private static readonly LogConfiguration HACK_CONFIG_FOR_TRACKS = new LogConfiguration ("Tracks", Log.Level.Debug);
			private static readonly LogConfiguration HACK_CONFIG_FOR_PATHS = new LogConfiguration ("Paths", Log.Level.Debug);

			public static LogConfiguration FindLogConfiguration (string name)
			{
				// TODO IMPLEMENT THE CONFIGURATION SYSTEM FOR REAL!
				if (name.StartsWith ("Paths")) {
					return HACK_CONFIG_FOR_PATHS;
				} else if (name.StartsWith ("Tracks")) {
					return HACK_CONFIG_FOR_TRACKS;
				} else {
					return DEFAULT;
				}
			}
		}

		// 0 - Level
		// 1 - Timestamp
		// 2 - Timestamp milliseconds
		// 3 - Type / name (full name)
		// 4 - Short name
		// 5 - Message

		// [TRACE] 2015-09-06T10:43:27 namespace.Name - Message
		// private static readonly string DEFAULT_FORMAT = "[{0,5}] {1:s} {3} - {5}";
		// [TRACE] 10:43 Name - Message
		// private static readonly string DEFAULT_FORMAT = "[{0,5}] {1:t} {4} - {5}";


		// [TRACE] 10:43:27.439 Name - Message
		private static readonly string DEFAULT_FORMAT = "[{0,5}] {1:T}.{2:d3} {4} - {5}";


		private string name;
		private string shortName;
		private LogConfiguration configuration;
		public Log (string name)
		{
			this.name = name;
			this.shortName = MakeShortName (name);
			this.configuration = ConfigurationResolver.FindLogConfiguration (name);


		}
		public Log (Type type) : this (type.FullName)
		{
		}

		public LogConfiguration Configuration {
			get {
				return configuration;
			}
		}
		private static string MakeShortName (string name)
		{
			int lastPeriodPos = name.LastIndexOf ('.');
			if (lastPeriodPos >= 0) {
				return name.Substring (lastPeriodPos + 1);
			} else {
				return name;
			}
		}

		private string FormatMessage (Level level, object message)
		{

			DateTime now = DateTime.Now;
			string levelString;
			if ((int)level >= 0 && (int)level < LEVEL_NAMES.Length) {
				levelString = LEVEL_NAMES [(int)level];
			} else {
				levelString = Enum.GetName (typeof(Level), level);
			}
			int ms = now.Millisecond;
			return string.Format (DEFAULT_FORMAT, levelString, now, ms, name, shortName, message);
		}

		public bool IsLogLevelEnabled (Log.Level level)
		{
			return level >= configuration.LevelThreshold;
		}

		public void Print (Level level, object message)
		{
			if (IsLogLevelEnabled (level)) {
				if (level <= Level.Info) {
					UnityEngine.Debug.Log (FormatMessage (level, message));
				} else if (level == Level.Warning) {
					UnityEngine.Debug.LogWarning (FormatMessage (level, message));
				} else if (level >= Level.Error) {
					UnityEngine.Debug.LogError (FormatMessage (level, message));
				}
			}
		}
		public void Print (Level level, object message, UnityEngine.Object context)
		{
			if (IsLogLevelEnabled (level)) {
				if (level <= Level.Info) {
					UnityEngine.Debug.Log (FormatMessage (level, message), context);
				} else if (level == Level.Warning) {
					UnityEngine.Debug.LogWarning (FormatMessage (level, message), context);
				} else if (level >= Level.Error) {
					UnityEngine.Debug.LogError (FormatMessage (level, message), context);
				}
			}
		}
		public void Format (Level level, string format, params object[] args)
		{
			if (IsLogLevelEnabled (level)) {
				string message;
				try {
					message = string.Format (format, args);
				} catch (Exception e) {
					message = "INVALID FORMAT SPECIFIER FOR LOGGER: " + format + " (" + e.Message + ")";
				}
				if (level <= Level.Info) {
					UnityEngine.Debug.Log (FormatMessage (level, message));
				} else if (level == Level.Warning) {
					UnityEngine.Debug.LogWarning (FormatMessage (level, message));
				} else if (level >= Level.Error) {
					UnityEngine.Debug.LogError (FormatMessage (level, message));
				}
			}
		}
		public void Format (Level level, UnityEngine.Object context, string format, params object[] args)
		{
			if (IsLogLevelEnabled (level)) {
				string message;
				try {
					message = string.Format (format, args);
				} catch (Exception e) {
					message = "INVALID FORMAT SPECIFIER FOR LOGGER: " + format + " (" + e.Message + ")";
				}
				if (level <= Level.Info) {
					UnityEngine.Debug.Log (FormatMessage (level, message), context);
				} else if (level == Level.Warning) {
					UnityEngine.Debug.LogWarning (FormatMessage (level, message), context);
				} else if (level >= Level.Error) {
					UnityEngine.Debug.LogError (FormatMessage (level, message), context);
				}
			}
		}


		public void Trace (object message)
		{
			Print (Level.Trace, message);
		}
		public void Trace (object message, UnityEngine.Object context)
		{
			Print (Level.Trace, message, context);
		}
		public void TraceFormat (string format, params object[] formatParameters)
		{
			Format (Level.Trace, format, formatParameters);
		}
		public void TraceFormat (UnityEngine.Object context, string format, params object[] formatParameters)
		{
			Format (Level.Trace, context, format, formatParameters);
		}

		public void Debug (object message)
		{
			Print (Level.Debug, message);
		}
		public void Debug (object message, UnityEngine.Object context)
		{
			Print (Level.Debug, message, context);
		}
		public void DebugFormat (string format, params object[] formatParameters)
		{
			Format (Level.Debug, format, formatParameters);
		}
		public void DebugFormat (UnityEngine.Object context, string format, params object[] formatParameters)
		{
			Format (Level.Debug, context, format, formatParameters);
		}

		public void Info (object message)
		{
			Print (Level.Info, message);
		}
		public void Info (object message, UnityEngine.Object context)
		{
			Print (Level.Info, message, context);
		}
		public void InfoFormat (string format, params object[] formatParameters)
		{
			Format (Level.Info, format, formatParameters);
		}
		public void InfoFormat (UnityEngine.Object context, string format, params object[] formatParameters)
		{
			Format (Level.Info, context, format, formatParameters);
		}

		public void Warning (object message)
		{
			Print (Level.Warning, message);
		}
		public void Warning (object message, UnityEngine.Object context)
		{
			Print (Level.Warning, message, context);
		}
		public void WarningFormat (string format, params object[] formatParameters)
		{
			Format (Level.Warning, format, formatParameters);
		}
		public void WarningFormat (UnityEngine.Object context, string format, params object[] formatParameters)
		{
			Format (Level.Warning, context, format, formatParameters);
		}

		public void Error (object message)
		{
			Print (Level.Error, message);
		}
		public void Error (object message, UnityEngine.Object context)
		{
			Print (Level.Error, message, context);
		}
		public void ErrorFormat (string format, params object[] formatParameters)
		{
			Format (Level.Error, format, formatParameters);
		}
		public void ErrorFormat (UnityEngine.Object context, string format, params object[] formatParameters)
		{
			Format (Level.Error, context, format, formatParameters);
		}


	}

	public static class LogExtensionMethods
	{
		public static void XLog (this UnityEngine.Debug o, object message)
		{
			o.LogInfo (message);
		}

		private static readonly Dictionary<string, Log> cachedLoggers = new Dictionary<string, Log> ();

		private static Log GetLog (string name)
		{
			Log log;
			if (cachedLoggers.ContainsKey (name)) {
				log = cachedLoggers [name];
			} else {
				log = new Log (name);
				cachedLoggers [name] = log;
			}
			return log;
		}
		private static Log GetLog (Type type)
		{
			return GetLog (type.FullName);
		}

		public static Log GetLog (this object o)
		{
			return GetLog (o.GetType ());
		}

		public static void LogTrace (this object o, object message)
		{
			GetLog (o.GetType ()).Print (Log.Level.Trace, message);
		}
		public static void LogTrace (this object o, object message, UnityEngine.Object context)
		{
			GetLog (o.GetType ()).Print (Log.Level.Trace, message, context);
		}
		public static void LogTraceFormat (this object o, string format, params object[] args)
		{
			GetLog (o.GetType ()).Format (Log.Level.Trace, format, args);
		}
		public static void LogTraceFormat (this object o, UnityEngine.Object context, string format, params object[] args)
		{
			GetLog (o.GetType ()).Format (Log.Level.Trace, context, format, args);
		}


		public static void LogDebug (this object o, object message)
		{
			GetLog (o.GetType ()).Print (Log.Level.Debug, message);
		}
		public static void LogDebug (this object o, object message, UnityEngine.Object context)
		{
			GetLog (o.GetType ()).Print (Log.Level.Debug, message, context);
		}
		public static void LogDebugFormat (this object o, string format, params object[] args)
		{
			GetLog (o.GetType ()).Format (Log.Level.Debug, format, args);
		}
		public static void LogDebugFormat (this object o, UnityEngine.Object context, string format, params object[] args)
		{
			GetLog (o.GetType ()).Format (Log.Level.Debug, context, format, args);
		}

		public static void LogInfo (this object o, object message)
		{
			GetLog (o.GetType ()).Print (Log.Level.Info, message);
		}
		public static void LogInfo (this object o, object message, UnityEngine.Object context)
		{
			GetLog (o.GetType ()).Print (Log.Level.Info, message, context);
		}
		public static void LogInfoFormat (this object o, string format, params object[] args)
		{
			GetLog (o.GetType ()).Format (Log.Level.Info, format, args);
		}
		public static void LogInfoFormat (this object o, UnityEngine.Object context, string format, params object[] args)
		{
			GetLog (o.GetType ()).Format (Log.Level.Info, context, format, args);
		}

		public static void LogWarning (this object o, object message)
		{
			GetLog (o.GetType ()).Print (Log.Level.Warning, message);
		}
		public static void LogWarning (this object o, object message, UnityEngine.Object context)
		{
			GetLog (o.GetType ()).Print (Log.Level.Warning, message, context);
		}
		public static void LogWarningFormat (this object o, string format, params object[] args)
		{
			GetLog (o.GetType ()).Format (Log.Level.Warning, format, args);
		}
		public static void LogWarningFormat (this object o, UnityEngine.Object context, string format, params object[] args)
		{
			GetLog (o.GetType ()).Format (Log.Level.Warning, context, format, args);
		}

		public static void LogError (this object o, object message)
		{
			GetLog (o.GetType ()).Print (Log.Level.Error, message);
		}
		public static void LogError (this object o, object message, UnityEngine.Object context)
		{
			GetLog (o.GetType ()).Print (Log.Level.Error, message, context);
		}
		public static void LogErrorFormat (this object o, string format, params object[] args)
		{
			GetLog (o.GetType ()).Format (Log.Level.Error, format, args);
		}
		public static void LogErrorFormat (this object o, UnityEngine.Object context, string format, params object[] args)
		{
			GetLog (o.GetType ()).Format (Log.Level.Error, context, format, args);
		}


	}
#endif // INCLUDE_UTIL_LOG
}
