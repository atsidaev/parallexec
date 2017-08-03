using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Atsidaev.Parallexec
{
	public class CommandOutput
	{
		public int ExitStatus { get; set; }
		public string StdOut { get; set; }
		public string StdErr { get; set; }
	}

	public class RemoteExecutor
	{
		private readonly object _lockObject = new object();

		SshClient client;
		ScpClient scp;

		public string ProgramLocation { get; }

		public RemoteExecutor(string server, string username, string password = null)
		{
			client = new SshClient(server, username, password);
			scp = new ScpClient(client.ConnectionInfo);
		}

		public void Connect()
		{
			if (!client.IsConnected)
			{
				client.Connect();
				scp.Connect();
			}
		}

		public async Task<CommandOutput> Run(string command)
		{
			SshCommand cmd;
			lock(_lockObject)
			{
				Connect();

				cmd = client.CreateCommand(command);
			}

			var stdout = await Task.Factory.FromAsync(cmd.BeginExecute(), cmd.EndExecute);
			return new CommandOutput() { ExitStatus = cmd.ExitStatus, StdOut = stdout, StdErr = cmd.Error };
		}

		private static string SendCommand(ShellStream stream, string customCMD)
		{
			var result = new StringBuilder();

			var reader = new StreamReader(stream);
			var writer = new StreamWriter(stream);
			writer.AutoFlush = true;
			WriteStream(customCMD, writer, stream);

			result.AppendLine(ReadStream(reader));

			string answer = result.ToString();
			return answer.Trim();
		}

		private static void WriteStream(string cmd, StreamWriter writer, ShellStream stream)
		{
			writer.WriteLine(cmd);
			while (stream.Length == 0)
				Thread.Sleep(500);
		}

		private static string ReadStream(StreamReader reader)
		{
			var result = new StringBuilder();

			string line;
			while ((line = reader.ReadLine()) != null)
				result.AppendLine(line);

			return result.ToString();
		}

		public async Task<bool> Exists(string fileName) => (await Run($"test -e {fileName}"))?.ExitStatus == 0;
		public async Task<bool> IsFile(string fileName) => (await Run($"test -f {fileName}"))?.ExitStatus == 0;
		public async Task<bool> IsDirectory(string fileName) => (await Run($"test -d {fileName}"))?.ExitStatus == 0;

		public Task ReadFile(string fileName, Stream inputStream)
		{
			return Task.Run(() => {
				if (inputStream == null)
					inputStream = new MemoryStream();
				scp.Download(fileName, inputStream);
			});
		}

		public Task WriteFile(string sourceFileName, string targetFileName)
		{
			return Task.Run(() => {
				using (var f = File.OpenRead(sourceFileName))
					scp.Upload(f, targetFileName);
			});
		}
	}
}
