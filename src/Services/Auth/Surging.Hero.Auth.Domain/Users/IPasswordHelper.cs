﻿using Surging.Core.CPlatform.Ioc;

namespace Surging.Hero.Auth.Domain.Users
{
    public interface IPasswordHelper : ITransientDependency
    {
        string EncryptPassword(string userName, string plainPassword);
    }
}