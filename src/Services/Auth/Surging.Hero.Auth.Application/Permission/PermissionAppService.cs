﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Surging.Cloud.AutoMapper;
using Surging.Cloud.Caching;
using Surging.Cloud.CPlatform.Cache;
using Surging.Cloud.CPlatform.Exceptions;
using Surging.Cloud.CPlatform.Ioc;
using Surging.Cloud.CPlatform.Runtime.Session;
using Surging.Cloud.Dapper.Repositories;
using Surging.Cloud.ProxyGenerator;
using Surging.Cloud.System.Intercept;
using Surging.Cloud.Validation.DataAnnotationValidation;
using Surging.Hero.Auth.Domain.Permissions;
using Surging.Hero.Auth.Domain.Permissions.Actions;
using Surging.Hero.Auth.Domain.Permissions.Menus;
using Surging.Hero.Auth.Domain.Permissions.Operations;
using Surging.Hero.Auth.Domain.Shared;
using Surging.Hero.Auth.Domain.Shared.Permissions;
using Surging.Hero.Auth.IApplication.Permission;
using Surging.Hero.Auth.IApplication.Permission.Dtos;
using Surging.Hero.Common;
using Surging.Hero.Common.Extensions;

namespace Surging.Hero.Auth.Application.Permission
{
    public class PermissionAppService : ProxyServiceBase, IPermissionAppService
    {
        private readonly IActionDomainService _actionDomainService;
        private readonly IMenuDomainService _menuDomainService;
        private readonly IDapperRepository<Menu, long> _menuRepository;
        private readonly IOperationDomainService _operationDomainService;
        private readonly IDapperRepository<Operation, long> _operationRepository;
        private readonly IPermissionDomainService _permissionDomainService;
        private readonly ISurgingSession _session;
        private readonly ICacheProvider _cacheProvider;

        public PermissionAppService(IMenuDomainService menuDomainService,
            IOperationDomainService operationDomainService,
            IDapperRepository<Menu, long> menuRepository,
            IDapperRepository<Operation, long> operationRepository,
            IActionDomainService actionDomainService,
            IPermissionDomainService permissionDomainService)
        {
            _menuDomainService = menuDomainService;
            _operationDomainService = operationDomainService;
            _menuRepository = menuRepository;
            _operationRepository = operationRepository;
            _actionDomainService = actionDomainService;
            _permissionDomainService = permissionDomainService;
            _session = NullSurgingSession.Instance;
            _cacheProvider = CacheContainer.GetService<ICacheProvider>(HeroConstants.CacheProviderKey);
        }

        public async Task<CreateMenuOutput> CreateMenu(CreateMenuInput input)
        {
            input.CheckDataAnnotations().CheckValidResult();
            return await _menuDomainService.Create(input);
        }

        public async Task<CreateOperationOutput> CreateOperation(CreateOperationInput input)
        {
            input.CheckDataAnnotations().CheckValidResult();
            return await _operationDomainService.Create(input);
        }

        public async Task<GetMenuOutput> GetMenu(long permissionId)
        {
            var menu = await _menuRepository.FirstOrDefaultAsync(p => p.PermissionId == permissionId);
            if (menu == null) throw new BusinessException($"不存在Id为{permissionId}的菜单信息");
            return menu.MapTo<GetMenuOutput>();
        }

        public async Task<GetOperationOutput> GetOperation(long permissionId)
        {
            var operation = await _operationRepository.SingleOrDefaultAsync(p => p.PermissionId == permissionId);
            if (operation == null) throw new BusinessException($"不存在permissionId为{permissionId}的操作信息");
            var operationOutput = operation.MapTo<GetOperationOutput>();
            operationOutput.ActionIds =
                (await _actionDomainService.GetOperationOutputActions(operation.Id)).Select(p => p.Id);
            return operationOutput;
        }

        public async Task<IEnumerable<GetPermissionTreeOutput>> GetTree()
        {
            return await _menuDomainService.GetTree();
        }

        public async Task<UpdateMenuOutput> UpdateMenu(UpdateMenuInput input)
        {
            input.CheckDataAnnotations().CheckValidResult();
            return await _menuDomainService.Update(input);
        }

        public async Task<UpdateOperationOutput> UpdateOperation(UpdateOperationInput input)
        {
            input.CheckDataAnnotations().CheckValidResult();
            return await _operationDomainService.Update(input);
        }

        public async Task<string> Delete(DeletePermissionInput input)
        {
            switch (input.Mold)
            {
                case PermissionMold.Menu:
                    await _menuDomainService.Delete(input.PermissionId);
                    break;
                case PermissionMold.Operation:
                    await _operationDomainService.Delete(input.PermissionId);
                    break;
                default:
                    throw new BusinessException("PermissionType不正确");
            }

            return $"删除{input.Mold.GetDescription()}成功";
        }

        public async Task<IDictionary<string, object>> Check(string serviceId)
        {
            if (_session == null || !_session.UserId.HasValue) throw new AuthException("您还没有登录系统");

            return await _cacheProvider.GetFromCacheFirst(string.Format(HeroConstants.CacheKey.PermissionCheck,serviceId,_session.UserId.Value),async () => await _permissionDomainService.Check(_session.UserId.Value, serviceId),typeof(IDictionary<string,object>));
            // return await _permissionDomainService.Check(_session.UserId.Value, serviceId);
        }

        public async Task<IEnumerable<EnumDto>> GetDataPermissionTypes()
        {
            return await Task.Run(() => typeof(DataPermissionType).GetAllEnumValues());
        }
    }
}