﻿using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Surging.Cloud.AutoMapper;
using Surging.Cloud.CPlatform.Configurations;
using Surging.Cloud.CPlatform.Exceptions;
using Surging.Cloud.Dapper.Manager;
using Surging.Cloud.Dapper.Repositories;
using Surging.Cloud.Lock;
using Surging.Cloud.Lock.Provider;
using Surging.Hero.Auth.Domain.Permissions.Actions;
using Surging.Hero.Auth.Domain.Permissions.Menus;
using Surging.Hero.Auth.Domain.Shared.Operations;
using Surging.Hero.Auth.IApplication.Permission.Dtos;
using Surging.Hero.Common;
using AppConfig = Surging.Cloud.CPlatform.AppConfig;

namespace Surging.Hero.Auth.Domain.Permissions.Operations
{
    public class OperationDomainService : ManagerBase, IOperationDomainService
    {
        private readonly IDapperRepository<Action, long> _actionRepository;
        private readonly ILockerProvider _lockerProvider;
        private readonly IDapperRepository<Menu, long> _menuRepository;
        private readonly IDapperRepository<OperationActionRelation, long> _operationActionRepository;
        private readonly IDapperRepository<Operation, long> _operationRepository;
        private readonly IDapperRepository<Permission, long> _permissionRepository;

        public OperationDomainService(IDapperRepository<Permission, long> permissionRepository,
            IDapperRepository<Operation, long> operationRepository,
            IDapperRepository<Menu, long> menuRepository,
            IDapperRepository<OperationActionRelation, long> operationActionRepository,
            IDapperRepository<Action, long> actionRepository,
            ILockerProvider lockerProvider)
        {
            _permissionRepository = permissionRepository;
            _operationRepository = operationRepository;
            _menuRepository = menuRepository;
            _operationActionRepository = operationActionRepository;
            _actionRepository = actionRepository;
            _lockerProvider = lockerProvider;
        }

        public async Task<CreateOperationOutput> Create(CreateOperationInput input)
        {
            var exsitOperation = await _operationRepository.FirstOrDefaultAsync(p => p.Name == input.Name);
            if (exsitOperation != null) throw new BusinessException($"系统中已经存在{input.Name}的操作,请勿重复添加");
            var exsitPermission = await _permissionRepository.FirstOrDefaultAsync(p => p.Name == input.Name);
            if (exsitPermission != null) throw new BusinessException($"系统中已经存在{input.Name}的权限,请勿重复添加");
            var menu = await _menuRepository.SingleOrDefaultAsync(p => p.PermissionId == input.PermissionId);
            if (menu == null) throw new BusinessException($"不存在PermissionId为{input.PermissionId}的菜单信息");
            var operation = input.MapTo<Operation>();
            var permission = input.MapTo<Permission>();

            var operationCount = await _operationRepository.GetCountAsync(p => p.MenuId == input.PermissionId);
            operation.Code = menu.Code + HeroConstants.CodeRuleRestrain.CodeSeparator + (operationCount + 1).ToString()
                .PadLeft(HeroConstants.CodeRuleRestrain.CodeCoverBit, HeroConstants.CodeRuleRestrain.CodeCoverSymbol);
            operation.Level = menu.Level + 1;
            using (var locker = await _lockerProvider.CreateLockAsync("CreateOperation"))
            {
                return await locker.Lock(async () =>
                {
                    await UnitOfWorkAsync(async (conn, trans) =>
                    {
                        var permissionId = await _permissionRepository.InsertAndGetIdAsync(permission, conn, trans);
                        operation.PermissionId = permissionId;
                        operation.MenuId = menu.Id;
                        var operationId = await _operationRepository.InsertAndGetIdAsync(operation, conn, trans);
                        if (input.ActionIds != null && input.ActionIds.Any())
                            foreach (var actionId in input.ActionIds)
                            {
                                var action = await _actionRepository.SingleOrDefaultAsync(p => p.Id == actionId);
                                if (action == null) throw new BusinessException($"系统中不存在Id为{actionId}的方法");
                                if (operation.Mold == OperationMold.Look || operation.Mold == OperationMold.Query)
                                {
                                    if (await _operationActionRepository.GetCountAsync(p =>
                                        p.ServiceId == action.ServiceId,conn,trans) > 1)
                                    {
                                        throw new BusinessException($"一个查询接口仅被允许分配给一个操作");
                                    }
                                }
                                var operationAction = new OperationActionRelation
                                    {ActionId = actionId, OperationId = operationId, ServiceId = action.ServiceId};
                                await _operationActionRepository.InsertAsync(operationAction, conn, trans);
                            }
                    }, Connection);
                    return new CreateOperationOutput
                        {Id = operation.Id, PermissionId = operation.PermissionId, Tips = "新增操作成功"};
                });
            }
        }

        public async Task Delete(long permissionId)
        {
            var operation = await _operationRepository.SingleOrDefaultAsync(p => p.PermissionId == permissionId);
            if (operation == null) throw new BusinessException($"不存在permissionId为{permissionId}的操作信息");
            using (var locker = await _lockerProvider.CreateLockAsync("DeleteOperation"))
            {
                await locker.Lock(async () =>
                {
                    await UnitOfWorkAsync(async (conn, trans) =>
                    {
                        await _operationRepository.DeleteAsync(p => p.PermissionId == permissionId, conn, trans);
                        await _operationActionRepository.DeleteAsync(p => p.OperationId == operation.Id, conn, trans);
                        await _permissionRepository.DeleteAsync(p => p.Id == operation.PermissionId, conn, trans);
                    }, Connection);
                });
            }
        }

        public async Task<IEnumerable<Operation>> GetAll()
        {
            return await _operationRepository.GetAllAsync();
        }

        public async Task<bool> CheckPermission(long operationId, string serviceId)
        {
            
            var operation = await _operationRepository.SingleOrDefaultAsync(p => p.Id == operationId);
            if (operation == null)
            {
                throw new BusinessException($"系统中不存在{operationId}的操作");
            }

            if (operation.Mold == OperationMold.Query || operation.Mold == OperationMold.Query)
            {
                return true;
            }
            if (AppConfig.ServerOptions.Environment == RuntimeEnvironment.Test)
            {
                if (AuthDomainConstants.PermissionServiceIdWhiteList.Contains(serviceId))
                    return true;
                throw new AuthException("项目演示环境，不允许操作！", StatusCode.UnAuthorized);
            }
            
            var operationActionRelations =
                await _operationActionRepository.GetAllAsync(p => p.OperationId == operationId);
            
            return operationActionRelations.Any(p => p.ServiceId == serviceId);
        }

        public async Task<IEnumerable<Operation>> GetOperationsByServiceId(string serviceId)
        {
            var sql = @"SELECT o.* FROM Operation as o INNER JOIN 
OperationActionRelation as oac on o.Id=oac.OperationId
WHERE o.IsDeleted=@IsDeleted AND oac.ServiceId=@ServiceId";
            await using (var  conn = Connection)
            {
                if (conn.State != ConnectionState.Open)
                {
                    await conn.OpenAsync();
                }

                return  await  conn.QueryAsync<Operation>(sql, new { IsDeleted = HeroConstants.UnDeletedFlag, ServiceId = serviceId });
            }
        }

        public async Task<UpdateOperationOutput> Update(UpdateOperationInput input)
        {
            var operation = await _operationRepository.SingleOrDefaultAsync(p => p.Id == input.Id);
            if (operation == null) throw new BusinessException($"不存在Id为{input.Id}的操作信息");
            var permission = await _permissionRepository.SingleOrDefaultAsync(p => p.Id == operation.PermissionId);
            if (permission == null) throw new BusinessException($"不存在Id为{operation.PermissionId}的权限信息");
            var menu = await _menuRepository.SingleOrDefaultAsync(p => p.Id == operation.MenuId);
            if (menu == null) throw new BusinessException($"不存在Id为{operation.MenuId}的菜单信息");
            operation = input.MapTo(operation);
            //permission = input.MapTo(permission);
            permission.Name = input.Name;
            permission.Memo = input.Memo;

            using (var locker = await _lockerProvider.CreateLockAsync("UpdateOperation"))
            {
                return await locker.Lock(async () =>
                {
                    await UnitOfWorkAsync(async (conn, trans) =>
                    {
                        await _permissionRepository.UpdateAsync(permission, conn, trans);
                        await _operationRepository.UpdateAsync(operation, conn, trans);
                        await _operationActionRepository.DeleteAsync(p => p.OperationId == operation.Id, conn, trans);
                        if (input.ActionIds != null && input.ActionIds.Any())
                            foreach (var actionId in input.ActionIds)
                            {
                                var action =
                                    await _actionRepository.SingleOrDefaultAsync(p => p.Id == actionId, conn, trans);
                                if (action == null) throw new BusinessException($"系统中不存在Id为{actionId}的方法");
                                if (operation.Mold == OperationMold.Look || operation.Mold == OperationMold.Query)
                                {
                                    if (await _operationActionRepository.GetCountAsync(p =>
                                        p.ServiceId == action.ServiceId,conn,trans) > 1)
                                    {
                                        throw new BusinessException($"一个查询接口仅被允许分配给一个操作");
                                    }
                                }
                                var operationAction = new OperationActionRelation
                                    {ActionId = actionId, OperationId = operation.Id, ServiceId = action.ServiceId};
                                await _operationActionRepository.InsertAsync(operationAction, conn, trans);
                            }
                    }, Connection);
                    return new UpdateOperationOutput
                        {Id = operation.Id, PermissionId = operation.PermissionId, Tips = "更新操作成功"};
                });
            }
        }
    }
}