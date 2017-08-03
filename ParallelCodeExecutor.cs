using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Atsidaev.Parallexec
{
	public class ParallelCodeExecutor : ClientBase
	{
		public ParallelCodeExecutor(RemoteExecutor executor) : base(executor) { }

		public int JobId { get; private set; }

		private string _outputFileName;
		private string _errorFileName;

		public async Task<CommandOutput> Execute(int nodesNumber, int gpuNumber, string partitionName, string program, params ProgramArgument[] arguments)
		{
			string gpu = "", nodes = "";
			if (gpuNumber > 0)
				gpu = $"--gres=gpu:{gpuNumber}";

			if (nodesNumber > 0)
				nodes = $"--nodes={nodesNumber}";

			if (!String.IsNullOrEmpty(partitionName))
				partitionName = "-p " + partitionName;

			// #SBATCH -t 30 -n 1 --mem-per-cpu 1950 --input /dev/null --job-name ls --output ls.1/output --error ls.1/errors

			_outputFileName = ProgramArgument.ForRandomFileName("txt").Value;
			_errorFileName = ProgramArgument.ForRandomFileName("txt").Value;

			var cmd = String.Join(" ", new[] {
				"echo '#!/bin/bash\n#SBATCH",
				nodes,
				gpu,
				partitionName,
				$"--output {_outputFileName}",
				$"--error {_errorFileName}\n\"srun\"",
				program,
				String.Join(" ", arguments.Select(a => $"\"{a.Value}\"")),
				"' | sbatch",
			});

			var g = await executor.Run(cmd);
			var submittedBatchJob = "Submitted batch job";
			if (!g.StdOut.StartsWith(submittedBatchJob))
				throw new InvalidOperationException("Unexpected result of sbatch run: " + g.StdOut);

			int jobId;
			if (!int.TryParse(g.StdOut.Replace(submittedBatchJob, "").Trim(), out jobId))
				throw new InvalidOperationException("Cannot extract job id: " + g.StdOut);

			JobId = jobId;

			var e = new ManualResetEvent(false);
			var worker = new BackgroundWorker();
			worker.DoWork += delegate {
				while (!IsCompleted().GetAwaiter().GetResult())
					Thread.Sleep(600);
			};

			worker.RunWorkerCompleted += delegate {
				e.Set();
			};

			worker.RunWorkerAsync();

			e.WaitOne();

			return await GetResult();
		}

		private async Task<bool> IsCompleted()
		{
			var result = await executor.Run($"mqinfo | grep '\\b{JobId}\\b'");
			return result.ExitStatus != 0;
		}

		private Task<CommandOutput> GetResult()
		{
			return Task.Run(() =>
			{
				var ms1 = new MemoryStream();
				executor.ReadFile(_outputFileName, ms1);
				var output = Encoding.ASCII.GetString(ms1.ToArray());

				var ms2 = new MemoryStream();
				executor.ReadFile(_errorFileName, ms2);
				var error = Encoding.ASCII.GetString(ms2.ToArray());

				return new CommandOutput() { StdOut = output, StdErr = error };
			});
		}
	}
}
