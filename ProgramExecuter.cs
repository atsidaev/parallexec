using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Atsidaev.Parallexec
{
	public class ProgramArgument
	{
		public enum ArgumentType { String, RandomFileName, LocalFile };
		public ArgumentType Type { get; private set; }
		public string Value { get; private set; }
		public string LocalFileName { get; private set; }
		public static ProgramArgument ForString(string str) => new ProgramArgument() { Type = ArgumentType.String, Value = str };
		public static ProgramArgument ForRandomFileName(string ext) => new ProgramArgument() { Type = ArgumentType.RandomFileName, Value = Path.GetRandomFileName() + "." + ext };
		public static ProgramArgument ForLocalFile(string fileName) => new ProgramArgument() { Type = ArgumentType.LocalFile, Value = Path.GetRandomFileName() + Path.GetExtension(fileName), LocalFileName = fileName };
	}
	public class ProgramExecuter : ClientBase
	{
		public ProgramExecuter(RemoteExecutor executor) : base(executor)
		{
			
		}

		public async Task Execute(string program, params ProgramArgument[] arguments)
		{
			// Upload local files if needed
			var transmissions = new List<Task>();
			foreach (var f in arguments.Where(a => a.Type == ProgramArgument.ArgumentType.LocalFile))
				transmissions.Add(executor.WriteFile(f.LocalFileName, f.Value));

			await Task.WhenAll(transmissions);
			var g = executor.Run(program + " " + String.Join(" ", arguments.Select(a => $"\"{a.Value}\"")));
		}
	}
}
