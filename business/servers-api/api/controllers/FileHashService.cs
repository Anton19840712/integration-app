using System.Collections.Concurrent;

namespace servers_api.api.controllers
{
	public class FileHashService
	{
		private readonly ConcurrentDictionary<string, bool> _processedFileHashes = new();

		public bool TryAddHash(string fileHash)
		{
			return _processedFileHashes.TryAdd(fileHash, true);
		}

		public bool RemoveHash(string fileHash)
		{
			return _processedFileHashes.TryRemove(fileHash, out _);
		}

		public bool ContainsHash(string fileHash)
		{
			return _processedFileHashes.ContainsKey(fileHash);
		}
	}
}
