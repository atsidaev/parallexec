using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Atsidaev.Parallexec
{
	public class GitClient : ClientBase
	{
		public GitClient(RemoteExecutor executor) : base(executor) { }

		public async Task<string> Instantiate(string url, string directoryName = null)
		{
			// Determina path of git directory
			string targetPath;
			if (!String.IsNullOrEmpty(directoryName))
				targetPath = Path.Combine(BasicDir, directoryName);
			else
			{
				var gitDir = new Uri(url).Segments.Last();
				if (gitDir.EndsWith(".git"))
					gitDir = gitDir.Substring(0, gitDir.Length - 4);

				targetPath = Path.Combine(BasicDir, gitDir);
			}

			// Cloning repository in new directory
			var alreadyCheckedOut = await executor.Exists($"{targetPath}/.git/config");

			var res = await executor.Run($"if [ ! -d {BasicDir} ]; then mkdir {BasicDir}; fi");
			if (res.ExitStatus != 0)
				return null;

			if (alreadyCheckedOut)
				res = await executor.Run($"(cd {targetPath}; git reset --hard origin/master; git pull origin --rebase;)");
			else
				res = await executor.Run($"(cd {BasicDir}; git clone {url} {directoryName})");

			return res.ExitStatus == 0 ? targetPath : null;
		}
	}
}
