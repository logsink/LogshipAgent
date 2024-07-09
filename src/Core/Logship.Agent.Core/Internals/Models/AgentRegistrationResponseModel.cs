using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logship.Agent.Core.Internals.Models
{
    public sealed record AgentRegistrationResponseModel(Guid AgentId, string HandshakeToken);
}
