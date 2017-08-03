using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Atsidaev.Parallexec
{
	public class ClientBase
	{
		protected const string BasicDir = "./parallexec/";

		protected RemoteExecutor executor;

		public ClientBase(RemoteExecutor executor)
		{
			this.executor = executor;
		}
	}
}
