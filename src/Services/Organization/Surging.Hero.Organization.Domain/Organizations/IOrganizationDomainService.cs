﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Surging.Cloud.CPlatform.Ioc;

namespace Surging.Hero.Organization.Domain
{
    public interface IOrganizationDomainService : ITransientDependency
    {
        Task<IEnumerable<Organization>> GetOrganizations();
        Task<IEnumerable<Organization>> GetSubOrgs(long orgId);

        Task<IEnumerable<Organization>> GetParentsOrganizations(long orgId);
    }
}