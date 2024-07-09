using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logship.Agent.Core.Events
{
    public interface IHandshakeAuth : IOutputAuth
    {
       Task SetInitialToken(string refreshToken, CancellationToken token);
    }
}
