using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Edge10.CouchDb.Client.Tasks
{
	/// <summary>
	/// A replication task record from the CouchDb _active_tasks feed.
	/// </summary>
	public class ReplicationTask
	{
		public const string TaskType = "replication";

		/// <summary>
		/// Gets or sets the process ID.
		/// </summary>
		[JsonProperty("pid")]
		public string ProcessId { get; set; }

		/// <summary>
		/// Gets or sets the checkpointed source sequence number.
		/// </summary>
		[JsonProperty("checkpointed_source_seq")]
		public int CheckpointedSourceSequence { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="ReplicationTask"/> is continuous.
		/// </summary>
		[JsonProperty("continuous")]
		public bool Continuous { get; set; }

		/// <summary>
		/// Gets or sets the ID of the replicator document.
		/// </summary>
		[JsonProperty("doc_id")]
		public string DocumentId { get; set; }

		/// <summary>
		/// Gets or sets the number of document write failures.
		/// </summary>
		[JsonProperty("doc_write_failures")]
		public int DocumentWriteFailures { get; set; }

		/// <summary>
		/// Gets or sets the number of documents read.
		/// </summary>
		[JsonProperty("docs_read")]
		public int DocumentsRead { get; set; }

		/// <summary>
		/// Gets or sets the number of documents written.
		/// </summary>
		[JsonProperty("docs_written")]
		public int DocumentsWritten { get; set; }

		/// <summary>
		/// Gets or sets the numbr of missing revisions found.
		/// </summary>
		[JsonProperty("missing_revisions_found")]
		public int MissingRevisionsFound { get; set; }

		/// <summary>
		/// Gets or sets the progress as a percentage.
		/// </summary>
		[JsonProperty("progress")]
		public int Progress { get; set; }

		/// <summary>
		/// Gets or sets the ID of the replication.  Note this is different to the ID of the replicator document.
		/// </summary>
		[JsonProperty("replication_id")]
		public string ReplicationId { get; set; }

		/// <summary>
		/// Gets or sets the number of revisions checked.
		/// </summary>
		[JsonProperty("revisions_checked")]
		public int RevisionsChecked { get; set; }

		/// <summary>
		/// Gets or sets the URL of the source database.
		/// </summary>
		[JsonProperty("source")]
		public string Source { get; set; }

		/// <summary>
		/// Gets or sets the source sequence number.
		/// </summary>
		[JsonProperty("source_seq")]
		public int SourceSequence { get; set; }

		/// <summary>
		/// Gets or sets the time the replication was started as a UTC unix time.
		/// </summary>
		[JsonProperty("started_on")]
		public int StartedOn { get; set; }

		/// <summary>
		/// Gets or sets the URL of the target database.
		/// </summary>
		[JsonProperty("target")]
		public string Target { get; set; }

		/// <summary>
		/// Gets or sets the time the replication task was last updated as a UTC unix time.  Updates are made by the job as progress occurs.
		/// </summary>
		[JsonProperty("updated_on")]
		public int UpdatedOn { get; set; }
	}
}
