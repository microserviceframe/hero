﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Surging.Cloud.AutoMapper;
using Surging.Cloud.CPlatform.Exceptions;
using Surging.Cloud.CPlatform.Runtime.Session;
using Surging.Cloud.Dapper.Repositories;
using Surging.Cloud.Domain.PagedAndSorted;
using Surging.Cloud.ProxyGenerator;
using Surging.Cloud.Validation.DataAnnotationValidation;
using Surging.Hero.Auth.Domain.Permissions;
using Surging.Hero.Auth.Domain.Roles;
using Surging.Hero.Auth.Domain.Users;
using Surging.Hero.Auth.IApplication.Role;
using Surging.Hero.Auth.IApplication.Role.Dtos;
using Surging.Hero.Common.Runtime.Session;

namespace Surging.Hero.Auth.Application.Role
{
    public class RoleAppService : ProxyServiceBase, IRoleAppService
    {
        private readonly IRoleDomainService _roleDomainService;
        private readonly IDapperRepository<Domain.Roles.Role, long> _roleRepository;
        private readonly ISurgingSession _session;
        
        public RoleAppService(IRoleDomainService roleDomainService,
            IDapperRepository<Domain.Roles.Role, long> roleRepository)
        {
            _roleDomainService = roleDomainService;
            _roleRepository = roleRepository;
            _session = NullSurgingSession.Instance;
        }

        public async Task<string> Create(CreateRoleInput input)
        {
            input.CheckDataAnnotations().CheckValidResult();
            CheckIsAllOrg(input);
            //_session.CheckLoginUserDataPermision(input.DataPermissionType,"您设置的角色的数据权限大于您拥有数据权限,系统不允许该操作");
            await _roleDomainService.Create(input);
            return "新增角色信息成功";
        }

        public async Task<string> Delete(long id)
        {  
            //_session.CheckLoginUserDataPermision("您设置的角色的数据权限大于您拥有数据权限,系统不允许该操作");
            await _roleDomainService.Delete(id);
            return "删除角色信息成功";
        }

        public async Task<GetRoleOutput> Get(long id)
        {
            return await _roleDomainService.Get(id);
        }

        public async Task<IEnumerable<GetRoleOutput>> List(string searchKey)
        {
            var queryRoles = await _roleRepository.GetAllAsync(p => p.Name.Contains(searchKey));
            return queryRoles.MapTo<IEnumerable<GetRoleOutput>>();
        }

        public async Task<IPagedResult<GetRoleOutput>> Search(QueryRoleInput query)
        {
            return await _roleDomainService.Search(query);
        }


        public async Task<string> Status(UpdateRoleStatusInput input)
        {
            await _roleDomainService.UpdateStatus(input);
            if (input.Status == Common.Status.Valid) return "启用角色成功";
            return "禁用角色成功";
        }

        public async Task<string> Update(UpdateRoleInput input)
        {
            input.CheckDataAnnotations().CheckValidResult();
            
            CheckIsAllOrg(input);
            //_session.CheckLoginUserDataPermision(input.DataPermissionType,"您设置的角色的数据权限大于您拥有数据权限,系统不允许该操作");
            await _roleDomainService.Update(input);
            return "更新角色信息成功";
        }

        private static void CheckIsAllOrg(RoleDtoBase input)
        {
            if (!input.IsAllOrg && (input.OrgIds == null || input.OrgIds.Length <= 0))
            {
                throw new BusinessException("角色所属部门不允许为空");
            }

            if (input.IsAllOrg && input.OrgIds?.Length > 0)
            {
                throw new BusinessException("角色设置为可被分配所有部门,则不需要传递orgIds参数");
            }
        }
    }
}