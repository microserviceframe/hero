﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Surging.Cloud.AutoMapper;
using Surging.Cloud.CPlatform.Exceptions;
using Surging.Cloud.CPlatform.Runtime.Session;
using Surging.Cloud.CPlatform.Utilities;
using Surging.Cloud.Dapper.Manager;
using Surging.Cloud.Dapper.Repositories;
using Surging.Cloud.Validation.DataAnnotationValidation;
using Surging.Hero.Auth.IApplication.User;
using Surging.Hero.BasicData.Domain.Shared.Wordbooks;
using Surging.Hero.BasicData.IApplication.Wordbook;
using Surging.Hero.BasicData.IApplication.Wordbook.Dtos;
using Surging.Hero.Common;
using Surging.Hero.Organization.Domain.Positions;
using Surging.Hero.Organization.IApplication.Department.Dtos;
using Surging.Hero.Organization.IApplication.Position.Dtos;

namespace Surging.Hero.Organization.Domain.Organizations.Departments
{
    public class DepartmentDomainService : ManagerBase, IDepartmentDomainService
    {
        private readonly IDapperRepository<Corporation, long> _corporationRepository;
        private readonly IDapperRepository<Department, long> _departmentRepository;
        private readonly IDapperRepository<Organization, long> _organizationRepository;
        private readonly IPositionDomainService _positionDomainService;
        private readonly IDapperRepository<Position, long> _positionRepository;
        private readonly ISurgingSession _session;

        public DepartmentDomainService(IDapperRepository<Department, long> departmentRepository,
            IDapperRepository<Corporation, long> corporationRepository,
            IDapperRepository<Position, long> positionRepository,
            IDapperRepository<Organization, long> organizationRepository,
            IPositionDomainService positionDomainService)
        {
            _departmentRepository = departmentRepository;
            _corporationRepository = corporationRepository;
            _positionRepository = positionRepository;
            _organizationRepository = organizationRepository;
            _positionDomainService = positionDomainService;
            _session = NullSurgingSession.Instance;
        }

        public async Task<CreateDepartmentOutput> CreateDepartment(CreateDepartmentInput input)
        {
            var parentOrg = await _organizationRepository.SingleOrDefaultAsync(p => p.Id == input.ParentId);
            if (parentOrg == null) throw new BusinessException($"不存在Id为{input.ParentId}的上级信息");

            if (input.DeptTypeKey.IsNullOrEmpty()) throw new BusinessException("请选择部门类型");

            var exsitOrg =
                await _organizationRepository.SingleOrDefaultAsync(p => p.Identification == input.Identification);
            if (exsitOrg != null) throw new BusinessException($"系统中已经存在标识为{input.Identification}的组织机构");

            var workbookAppServiceProxy = GetService<IWordbookAppService>();
            var checkDeptTypeResult = await workbookAppServiceProxy.Check(new CheckWordbookInput
                {WordbookCode = SystemPresetWordbookCode.Organization.DeptType, WordbookItemKey = input.DeptTypeKey});
            if (!checkDeptTypeResult) throw new BusinessException("指定的部门类型Key不正确,请选择正确的部门类型");
            var department = input.MapTo<Department>();
            var orgInfo = input.MapTo<Organization>();

            var orgCode = string.Empty;
            var maxLevelOrg = (await _organizationRepository.GetAllAsync(p => p.ParentId == parentOrg.Id))
                .OrderByDescending(p => p.Id).FirstOrDefault();
            if (maxLevelOrg == null)
                orgCode = "1".PadLeft(HeroConstants.CodeRuleRestrain.CodeCoverBit,
                    HeroConstants.CodeRuleRestrain.CodeCoverSymbol);
            else
                orgCode =
                    (Convert.ToInt32(maxLevelOrg.Code.Split(HeroConstants.CodeRuleRestrain.CodeCoverSymbol).Last()
                        .TrimStart('0')) + 1).ToString().PadLeft(HeroConstants.CodeRuleRestrain.CodeCoverBit,
                        HeroConstants.CodeRuleRestrain.CodeCoverSymbol);

            orgInfo.Level = parentOrg.Level + 1;
            orgInfo.Code = parentOrg.Code + HeroConstants.CodeRuleRestrain.CodeSeparator + orgCode;

            await UnitOfWorkAsync(async (conn, trans) =>
            {
                var orgId = await _organizationRepository.InsertAndGetIdAsync(orgInfo, conn, trans);
                department.OrgId = orgId;
                var deptId = await _departmentRepository.InsertAndGetIdAsync(department, conn, trans);
                if (input.Positions != null && input.Positions.Any())
                {
                    if (input.Positions.Count(p => p.IsLeadingOfficial) > 1)
                        throw new BusinessException("部门只允许设置一个负责人岗位");
                    var sort = 1;
                    foreach (var positionInput in input.Positions)
                    {
                        var position = positionInput.MapTo<Position>();
                        position.DeptId = deptId;
                        position.CheckDataAnnotations().CheckValidResult();
                        var positionCode = orgInfo.Code + HeroConstants.CodeRuleRestrain.CodeSeparator + sort.ToString()
                            .PadLeft(HeroConstants.CodeRuleRestrain.CodeCoverBit,
                                HeroConstants.CodeRuleRestrain.CodeCoverSymbol);
                        position.Code = positionCode;
                        await _positionRepository.InsertAsync(position, conn, trans);
                        sort++;
                    }
                }
            }, Connection);

            return new CreateDepartmentOutput
            {
                OrgId = orgInfo.Id,
                DeptId = department.Id,
                Tips = "新增部门信息成功"
            };
        }

        public async Task DeleteDepartmentByOrgId(long orgId)
        {
            var department = await _departmentRepository.SingleOrDefaultAsync(p => p.OrgId == orgId);
            var orgInfo = await _organizationRepository.SingleOrDefaultAsync(p => p.Id == orgId);
            if (department == null || orgInfo == null) throw new BusinessException($"系统中不存在Id为{orgId}的部门信息");
            var children = await _organizationRepository.GetAllAsync(p => p.ParentId == orgId);
            if (children.Any()) throw new BusinessException("请先删除子部门信息");
            var departmentUsers = await GetService<IUserAppService>().GetOrgUser(orgInfo.Id, true);
            if (departmentUsers.Any()) throw new BusinessException("该部门存在用户,请先删除该部门下的用户");
            await UnitOfWorkAsync(async (conn, trans) =>
            {
                await _organizationRepository.DeleteAsync(orgInfo, conn, trans);
                await _departmentRepository.DeleteAsync(department, conn, trans);
                await _positionRepository.DeleteAsync(p => p.DeptId == department.Id, conn, trans);
                foreach (var departmentUser in departmentUsers)
                    if (!await GetService<IUserAppService>().ResetUserOrgInfo(departmentUser.Id))
                        throw new BusinessException("重置该公司部门员工部门信息失败,请稍后重试");
            }, Connection);
        }

        public async Task<GetDepartmentOutput> GetDepartment(long id)
        {
            var department = await _departmentRepository.SingleOrDefaultAsync(p => p.Id == id);

            if (department == null) throw new BusinessException($"系统中不存在Id为{id}的部门信息");
            var orgInfo = await _organizationRepository.SingleOrDefaultAsync(p => p.Id == department.OrgId);
            if (orgInfo == null) throw new BusinessException($"系统中不存在Id为{id}的部门信息");
            var departmentOutput = department.MapTo<GetDepartmentOutput>();
            departmentOutput = orgInfo.MapTo(departmentOutput);
            departmentOutput.Positions = await _positionDomainService.GetPositionsByDeptId(department.Id);
            return departmentOutput;
        }

        public async Task<GetDepartmentOutput> GetDepartmentByOrgId(long orgId)
        {
            var department = await _departmentRepository.SingleOrDefaultAsync(p => p.OrgId == orgId);

            if (department == null) throw new BusinessException($"系统中不存在orgId为{orgId}的部门信息");
            var orgInfo = await _organizationRepository.SingleOrDefaultAsync(p => p.Id == department.OrgId);
            if (orgInfo == null) throw new BusinessException($"系统中不存在orgId为{orgId}的部门信息");
            var departmentOutput = department.MapTo<GetDepartmentOutput>();
            departmentOutput = orgInfo.MapTo(departmentOutput);
            var positions = await _positionDomainService.GetPositionsByDeptId(department.Id);
            departmentOutput.Positions = positions.MapTo<IEnumerable<GetPositionOutput>>();
            return departmentOutput;
        }

        public async Task<UpdateDepartmentOutput> UpdateDepartment(UpdateDepartmentInput input)
        {
            var department = await _departmentRepository.SingleOrDefaultAsync(p => p.Id == input.Id);

            if (department == null) throw new BusinessException($"系统中不存在Id为{input.Id}的部门信息");
            var orgInfo = await _organizationRepository.SingleOrDefaultAsync(p => p.Id == department.OrgId);
            if (orgInfo == null) throw new BusinessException($"系统中不存在Id为{department.Id}的部门信息");

            if (!input.Identification.Equals(orgInfo.Identification))
            {
                var exsitOrg =
                    await _organizationRepository.FirstOrDefaultAsync(p => p.Identification == input.Identification);
                if (exsitOrg != null) throw new BusinessException($"系统中已经存在标识为{input.Identification}的组织机构");
            }

            if (input.DeptTypeKey.IsNullOrEmpty()) throw new BusinessException("请选择部门类型");

            var workbookAppServiceProxy = GetService<IWordbookAppService>();
            var checkDeptTypeResult = await workbookAppServiceProxy.Check(new CheckWordbookInput
                {WordbookCode = SystemPresetWordbookCode.Organization.DeptType, WordbookItemKey = input.DeptTypeKey});
            if (!checkDeptTypeResult) throw new BusinessException("部门类型Id不正确,请选择正确的部门类型");
            department = input.MapTo(department);
            orgInfo = input.MapTo(orgInfo);
            Debug.Assert(_session.UserId.HasValue, "登录用户的UserId不会为空");
            await UnitOfWorkAsync(async (conn, trans) =>
            {
                await _organizationRepository.UpdateAsync(orgInfo, conn, trans);
                await _departmentRepository.UpdateAsync(department, conn, trans);

                if (input.Positions != null && input.Positions.Any())
                {
                    if (input.Positions.Count(p => p.IsLeadingOfficial) > 1)
                        throw new BusinessException("部门只允许设置一个负责人岗位");
                    var positionIds = input.Positions.Where(p => p.Id != 0 && p.Id.HasValue).Select(p => p.Id);
                    var deletePositionSql =
                        "UPDATE `Position` SET IsDeleted=@IsDeleted,DeleteBy=@DeleteBy,DeleteTime=@DeleteTime WHERE DeptId=@DeptId AND Id NOT in @Id AND TenantId=@TenantId";
                    await conn.ExecuteAsync(deletePositionSql,
                        new
                        {
                            IsDeleted = HeroConstants.DeletedFlag, DeleteBy = _session.UserId.Value,
                            DeleteTime = DateTime.Now, DeptId = department.Id, Id = positionIds,
                            TenantId = _session.TenantId
                        }, trans);
                    var sort = 1;
                    foreach (var positionInput in input.Positions)
                    {
                        var positionCode = orgInfo.Code + HeroConstants.CodeRuleRestrain.CodeSeparator + sort.ToString()
                            .PadLeft(HeroConstants.CodeRuleRestrain.CodeCoverBit,
                                HeroConstants.CodeRuleRestrain.CodeCoverSymbol);
                        if (positionInput.Id.HasValue && positionInput.Id != 0)
                        {
                            var position = await _positionRepository.GetAsync(positionInput.Id.Value);
                            position.CheckDataAnnotations().CheckValidResult();
                            position = positionInput.MapTo(position);
                            position.Code = positionCode;
                            await _positionRepository.UpdateAsync(position, conn, trans);
                        }
                        else
                        {
                            var position = positionInput.MapTo<Position>();
                            position.DeptId = department.Id;
                            position.CheckDataAnnotations().CheckValidResult();
                            position.Code = positionCode;
                            await _positionRepository.InsertAsync(position, conn, trans);
                        }

                        sort++;
                    }
                }
            }, Connection);

            return new UpdateDepartmentOutput
            {
                OrgId = orgInfo.Id,
                DeptId = department.Id,
                Tips = "更新部门信息成功"
            };
        }
    }
}