using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Atsidaev.Parallexec
{
	public class MakeClient : ClientBase
	{
		public MakeClient(RemoteExecutor executor) : base(executor) { }

		public async Task<bool> Make(string directory, string makeParameters = null)
		{
			var result = await executor.Run($"(cd {directory}; make {makeParameters})");
			return result.ExitStatus == 0;
		}
	}
}
