using System;
using System.Collections.Concurrent;
using System.IO;

namespace Sharp.Core.Editor
{
	/// <summary>
	/// 
	/// </summary>
	public class BufferedFileSystemWatcher
	{
		static readonly ConcurrentQueue<Change> ChangesQueue = new ();
		readonly FileSystemWatcher fileSystemWatcher = new();
			
		string Source = @"c:\Source";

		/// <summary>
		/// Tests this instance.
		/// </summary>
		public BufferedFileSystemWatcher(string folderToWatch)
		{
			Source = folderToWatch;
			fileSystemWatcher.Path = Source;
			fileSystemWatcher.IncludeSubdirectories = true;
			fileSystemWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.LastAccess | NotifyFilters.CreationTime | NotifyFilters.FileName | NotifyFilters.DirectoryName;
			fileSystemWatcher.InternalBufferSize = 65536;
			fileSystemWatcher.Created += FileSystemWatcherOnCreated;
			fileSystemWatcher.Deleted += FileSystemWatcherOnDeleted;
			fileSystemWatcher.Renamed += FileSystemWatcherOnRenamed;
			fileSystemWatcher.Error += FileSystemWatcherOnError;
			fileSystemWatcher.Changed += FileSystemWatcherOnChange;
			fileSystemWatcher.EnableRaisingEvents = true;
			/*ThreadPool.QueueUserWorkItem
			(
				(state) =>
				{
					while (true)
					{
						if (ChangesQueue.TryDequeue(out var change))
						{
							switch (change.ChangeType)
							{
								case WatcherChangeTypes.Created:
									
									break;
								case WatcherChangeTypes.Deleted:
									
									break;
								case WatcherChangeTypes.Changed:
									
									break;
								case WatcherChangeTypes.Renamed:
									
									break;
								case WatcherChangeTypes.All:
									break;
								default:
									throw new ArgumentOutOfRangeException();
							}
						}
					}
				}
			);*/
		}

		private void FileSystemWatcherOnChange(object sender, FileSystemEventArgs fileSystemEventArgs)
		{
			ChangesQueue.Enqueue(new Change
			{
				ChangeType = WatcherChangeTypes.Changed,
				FullPath = fileSystemEventArgs.FullPath,
				Name = fileSystemEventArgs.Name
			});
		}

		/// <summary>
		/// Files the system watcher configuration created.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="fileSystemEventArgs">The <see cref="FileSystemEventArgs"/> instance containing the event data.</param>
		private static void FileSystemWatcherOnCreated(object sender, FileSystemEventArgs fileSystemEventArgs)
		{
			ChangesQueue.Enqueue(new Change
			{
				ChangeType = WatcherChangeTypes.Created,
				FullPath = fileSystemEventArgs.FullPath,
				Name = fileSystemEventArgs.Name
			});
		}

		/// <summary>
		/// Files the system watcher configuration deleted.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="fileSystemEventArgs">The <see cref="FileSystemEventArgs"/> instance containing the event data.</param>
		private static void FileSystemWatcherOnDeleted(object sender, FileSystemEventArgs fileSystemEventArgs)
		{
			ChangesQueue.Enqueue(new Change
			{
				ChangeType = WatcherChangeTypes.Deleted,
				FullPath = fileSystemEventArgs.FullPath,
				Name = fileSystemEventArgs.Name
			});
		}

		/// <summary>
		/// Files the system watcher configuration error.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="errorEventArgs">The <see cref="ErrorEventArgs"/> instance containing the event data.</param>
		private static void FileSystemWatcherOnError(object sender, ErrorEventArgs errorEventArgs)
		{
			var exception = errorEventArgs.GetException();
			Console.WriteLine(exception.Message);
		}

		/// <summary>
		/// Files the system watcher configuration renamed.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="fileSystemEventArgs">The <see cref="RenamedEventArgs"/> instance containing the event data.</param>
		private static void FileSystemWatcherOnRenamed(object sender, RenamedEventArgs fileSystemEventArgs)
		{
			ChangesQueue.Enqueue(new Change
			{
				ChangeType = WatcherChangeTypes.Renamed,
				FullPath = fileSystemEventArgs.FullPath,
				Name = fileSystemEventArgs.Name,
				OldFullPath = fileSystemEventArgs.OldFullPath,
				OldName = fileSystemEventArgs.OldName
			});
		}

		/// <summary>
		/// Gets the type of the file system.
		/// </summary>
		/// <param name="fullPath">The full path.</param>
		/// <returns></returns>
		private static FileSystemType GetFileSystemType(string fullPath)
		{
			if (Directory.Exists(fullPath))
				return FileSystemType.Directory;
			if (File.Exists(fullPath))
				return FileSystemType.File;
			return FileSystemType.NotExists;
		}
	}

	/// <summary>
	/// Type of file system object
	/// </summary>
	internal enum FileSystemType
	{
		/// <summary>
		/// The file
		/// </summary>
		File,
		/// <summary>
		/// The directory
		/// </summary>
		Directory,
		/// <summary>
		/// The not existant
		/// </summary>
		NotExists
	}

	/// <summary>
	/// Change information
	/// </summary>
	public readonly record struct Change(WatcherChangeTypes ChangeType, string FullPath, string Name, string OldFullPath, string OldName)
	{
		/// <summary>
		/// Gets or sets the type of the change.
		/// </summary>
		/// <value>
		/// The type of the change.
		/// </value>

		/// <summary>
		/// Gets or sets the full path.
		/// </summary>
		/// <value>
		/// The full path.
		/// </value>

		/// <summary>
		/// Gets or sets the name.
		/// </summary>
		/// <value>
		/// The name.
		/// </value>

		/// <summary>
		/// Gets or sets the old full path.
		/// </summary>
		/// <value>
		/// The old full path.
		/// </value>

		/// <summary>
		/// Gets or sets the old name.
		/// </summary>
		/// <value>
		/// The old name.
		/// </value>
	}
}
