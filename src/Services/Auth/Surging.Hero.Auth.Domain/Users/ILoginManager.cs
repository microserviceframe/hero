﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Surging.Core.CPlatform.Ioc;

namespace Surging.Hero.Auth.Domain.Users
{
    public interface ILoginManager : ITransientDependency
    {
        Task<IDictionary<string, object>> Login(string userName, string password);
    }
}