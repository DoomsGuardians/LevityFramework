using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Levity.UnifiedSave
{
    public interface IUnifiedSaveStore
    {
        Task ReplaceAsync(
            string slotId,
            UnifiedSaveRecord record,
            CancellationToken cancellationToken = default);
        Task<UnifiedSaveRecord> ReadAsync(
            string slotId,
            CancellationToken cancellationToken = default);
    }

    public sealed class UnifiedSaveRecord
    {
        public UnifiedSaveRecord(IReadOnlyList<UnifiedSaveContribution> contributions) =>
            Contributions = contributions ?? throw new ArgumentNullException(nameof(contributions));

        public IReadOnlyList<UnifiedSaveContribution> Contributions { get; }
    }

    public sealed class UnifiedSaveContribution
    {
        public UnifiedSaveContribution(string id, int version, string state)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Version = version;
            State = state;
        }

        public string Id { get; }
        public int Version { get; }
        public string State { get; }
    }

    /// <summary>Writes a candidate beside the slot and atomically replaces the previous file.</summary>
    public sealed class FileUnifiedSaveStore : IUnifiedSaveStore
    {
        private const int FormatVersion = 1;
        private static readonly byte[] Magic = Encoding.ASCII.GetBytes("LEVITYSAVE");
        private readonly string directory;

        public FileUnifiedSaveStore(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory))
                throw new ArgumentException("A Unified Save directory is required.", nameof(directory));
            this.directory = Path.GetFullPath(directory);
        }

        public Task ReplaceAsync(
            string slotId,
            UnifiedSaveRecord record,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (record == null) throw new ArgumentNullException(nameof(record));
            Directory.CreateDirectory(directory);

            var destination = GetSlotPath(slotId);
            var candidate = Path.Combine(directory, $".{slotId}.{Guid.NewGuid():N}.candidate");
            var backup = Path.Combine(directory, $".{slotId}.{Guid.NewGuid():N}.backup");
            try
            {
                WriteRecord(candidate, record);
                cancellationToken.ThrowIfCancellationRequested();
                if (File.Exists(destination))
                {
                    try
                    {
                        File.Replace(candidate, destination, backup);
                    }
                    catch
                    {
                        if (!File.Exists(destination) && File.Exists(backup))
                            File.Move(backup, destination);
                        throw;
                    }
                }
                else
                {
                    File.Move(candidate, destination);
                }
            }
            finally
            {
                if (File.Exists(candidate)) File.Delete(candidate);
                if (File.Exists(backup) && File.Exists(destination)) File.Delete(backup);
            }
            return Task.CompletedTask;
        }

        public Task<UnifiedSaveRecord> ReadAsync(
            string slotId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(ReadRecord(GetSlotPath(slotId)));
        }

        private string GetSlotPath(string slotId)
        {
            if (string.IsNullOrWhiteSpace(slotId))
                throw new ArgumentException("A Unified Save slot ID cannot be empty.", nameof(slotId));
            foreach (var character in slotId)
            {
                if (!char.IsLetterOrDigit(character) && character != '-' && character != '_')
                    throw new ArgumentException("A Unified Save slot ID contains invalid characters.", nameof(slotId));
            }
            return Path.Combine(directory, $"{slotId}.levity-save");
        }

        private static void WriteRecord(string path, UnifiedSaveRecord record)
        {
            using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                writer.Write(Magic);
                writer.Write(FormatVersion);
                writer.Write(record.Contributions.Count);
                foreach (var contribution in record.Contributions)
                {
                    writer.Write(contribution.Id);
                    writer.Write(contribution.Version);
                    writer.Write(contribution.State != null);
                    if (contribution.State != null) writer.Write(contribution.State);
                }
                writer.Flush();
                stream.Flush(true);
            }
        }

        private static UnifiedSaveRecord ReadRecord(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var reader = new BinaryReader(stream, Encoding.UTF8))
            {
                var magic = reader.ReadBytes(Magic.Length);
                if (magic.Length != Magic.Length || !BytesEqual(magic, Magic))
                    throw new InvalidDataException("The file is not a Levity Unified Save.");
                if (reader.ReadInt32() != FormatVersion)
                    throw new InvalidDataException("The Unified Save format version is unsupported.");

                var count = reader.ReadInt32();
                if (count < 0) throw new InvalidDataException("The contributor count is invalid.");
                var contributions = new List<UnifiedSaveContribution>(count);
                for (var index = 0; index < count; index++)
                {
                    var id = reader.ReadString();
                    var version = reader.ReadInt32();
                    var state = reader.ReadBoolean() ? reader.ReadString() : null;
                    contributions.Add(new UnifiedSaveContribution(id, version, state));
                }
                return new UnifiedSaveRecord(contributions);
            }
        }

        private static bool BytesEqual(byte[] left, byte[] right)
        {
            for (var index = 0; index < left.Length; index++)
                if (left[index] != right[index]) return false;
            return true;
        }
    }
}
