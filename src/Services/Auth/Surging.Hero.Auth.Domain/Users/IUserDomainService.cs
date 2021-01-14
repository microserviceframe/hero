﻿using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using Surging.Cloud.CPlatform.Ioc;
using Surging.Cloud.Domain.PagedAndSorted;
using Surging.Hero.Auth.Domain.Permissions;
using Surging.Hero.Auth.Domain.Permissions.Menus;
using Surging.Hero.Auth.Domain.Permissions.Operations;
using Surging.Hero.Auth.Domain.Roles;
using Surging.Hero.Auth.IApplication.User.Dtos;
using Surging.Hero.Common;

namespace Surging.Hero.Auth.Domain.Users
{
    public interface IUserDomainService : ITransientDependency
    {  
        Task<long> Create (CreateUserInput input,long? tenanId = null);

        Task<long> Create(CreateUserInput input, DbConnection conn, DbTransaction trans, long? tenanId = null);
        
        Task ResetPassword(UserInfo userInfo, string newPassword);
        Task Update(UpdateUserInput input);
        Task Delete(long id);
        Task<IEnumerable<Role>> GetUserRoles(long userId, Status? status = null);
        Task<GetUserNormOutput> GetUserNormInfoById(long id);
        Task<bool> CheckPermission(long userId, string serviceId);
        Task<IEnumerable<Menu>> GetUserMenu(long userId);
        Task<IEnumerable<Operation>> GetUserOperation(long userId, long menuId);
        Task<IPagedResult<GetUserNormOutput>> Search(QueryUserInput query);
        Task<CheckPermissionResult> GetDataPermissions(long userId, long permissionId);
        
    }
}