﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Surging.Cloud.AutoMapper;
using Surging.Cloud.CPlatform.Exceptions;
using Surging.Cloud.Dapper.Manager;
using Surging.Cloud.Dapper.Repositories;
using Surging.Hero.Auth.IApplication.User;
using Surging.Hero.Common;
using Surging.Hero.Organization.Domain.Shared;
using Surging.Hero.Organization.Domain.Shared.Organizations;
using Surging.Hero.Organization.IApplication.Corporation.Dtos;

namespace Surging.Hero.Organization.Domain.Organizations
{
    public class CorporationDomainService : ManagerBase, ICorporationDomainService
    {
        private readonly IDapperRepository<Corporation, long> _corporationRepository;
        private readonly IDapperRepository<Department, long> _departmentRepository;
        private readonly IDapperRepository<Organization, long> _organizationRepository;

        public CorporationDomainService(IDapperRepository<Organization, long> organizationRepository,
            IDapperRepository<Corporation, long> corporationRepository,
            IDapperRepository<Department, long> departmentRepository)
        {
            _organizationRepository = organizationRepository;
            _corporationRepository = corporationRepository;
            _departmentRepository = departmentRepository;
        }


        public async Task<CreateCorporationOutput> CreateCorporation(CreateCorporationInput input)
        {
            var exsitOrg =
                await _organizationRepository.SingleOrDefaultAsync(p => p.Identification == input.Identification);
            if (exsitOrg != null) throw new BusinessException($"系统中已经存在标识为{input.Identification}的组织机构");

            if (!input.ParentId.HasValue || input.ParentId == 0)
                return await CreateTopCorporation(input);
            return await CreateSubCorporation(input);
        }
        
        public async Task<CreateCorporationOutput> CreateByTenant(CreateCorporationByTenantInput input)
        {
            var exsitOrg =
                await _organizationRepository.SingleOrDefaultAsync(p => p.Identification == input.Identification);
            if (exsitOrg != null) throw new BusinessException($"系统中已经存在标识为{input.Identification}的组织机构");

            if (!input.ParentId.HasValue || input.ParentId == 0)
                return await CreateTopCorporation(input, input.TenantId);
            return await CreateSubCorporation(input, input.TenantId);
        }

        public async Task DeleteCorporation(long orgId)
        {
            var corporation = await _corporationRepository.SingleOrDefaultAsync(p => p.OrgId == orgId);
            var orgInfo = await _organizationRepository.SingleOrDefaultAsync(p => p.Id == orgId);
            if (corporation == null || orgInfo == null) throw new BusinessException($"系统中不存在OrgId为{orgId}的企业信息");

            var children = await _organizationRepository.GetAllAsync(p => p.ParentId == orgInfo.Id);
            if (children.Any()) throw new BusinessException("请先删除子公司信息");
            var departments = await _departmentRepository.GetAllAsync(p => p.OrgId == orgInfo.Id);
            if (departments.Any()) throw new BusinessException("请先删除该公司的部门信息");
            var corporationUsers = await GetService<IUserAppService>().GetOrgUser(orgInfo.Id, true);
            await UnitOfWorkAsync(async (conn, trans) =>
            {
                await _organizationRepository.DeleteAsync(p => p.Id == orgInfo.Id, conn, trans);
                await _corporationRepository.DeleteAsync(p => p.OrgId == orgInfo.Id, conn, trans);
                foreach (var corporationUser in corporationUsers)
                    if (!await GetService<IUserAppService>().ResetUserOrgInfo(corporationUser.Id))
                        throw new BusinessException("重置该公司部门员工部门信息失败,请稍后重试");
            }, Connection);
        }

        public async Task<GetCorporationOutput> GetCorporation(long orgId)
        {
            var orgInfo = await _organizationRepository.GetAsync(orgId);
            var corporation = await _corporationRepository.SingleOrDefaultAsync(p => p.OrgId == orgId);
            if (corporation == null) throw new BusinessException($"不存在orgid为{orgId}的公司信息");
            var output = corporation.MapTo<GetCorporationOutput>();
            output = orgInfo.MapTo(output);
            return output;
        }



        public async Task<UpdateCorporationOutput> UpdateCorporation(UpdateCorporationInput input)
        {
            var corporation = await _corporationRepository.SingleOrDefaultAsync(p => p.Id == input.Id);

            if (corporation == null) throw new BusinessException($"系统中不存在Id为{input.Id}的公司信息");

            var orgInfo = await _organizationRepository.SingleOrDefaultAsync(p => p.Id == corporation.OrgId);
            if (orgInfo == null) throw new BusinessException($"系统中不存在Id为{input.Id}的公司信息");
            if (!input.Identification.Equals(orgInfo.Identification))
            {
                var exsitOrg =
                    await _organizationRepository.FirstOrDefaultAsync(p => p.Identification == input.Identification);
                if (exsitOrg != null) throw new BusinessException($"系统中已经存在标识为{input.Identification}的组织机构");
            }

            corporation = input.MapTo(corporation);
            orgInfo = input.MapTo(orgInfo);
            await UnitOfWorkAsync(async (conn, trans) =>
            {
                await _organizationRepository.UpdateAsync(orgInfo, conn, trans);
                await _corporationRepository.UpdateAsync(corporation, conn, trans);
            }, Connection);
            return new UpdateCorporationOutput
            {
                OrgId = orgInfo.Id,
                CorporationId = corporation.Id,
                Tips = "更新公司信息成功"
            };
        }

        private async Task<CreateCorporationOutput> CreateSubCorporation(CreateCorporationInput input,long? tenantId = null)
        {
            var parentCorporation =
                await _corporationRepository.SingleOrDefaultAsync(p => p.OrgId == input.ParentId.Value);
            if (parentCorporation.Mold == CorporationMold.Monomer) throw new BusinessException("单体公司不允许增加子公司");
            var parentOrg = await _organizationRepository.GetAsync(input.ParentId.Value);
            var corporation = input.MapTo<Corporation>();
            var orgInfo = input.MapTo<Organization>();
            if (tenantId.HasValue)
            {
                orgInfo.TenantId = tenantId;
                corporation.TenantId = tenantId;
            }
            var orgCode = string.Empty;
            var maxLevelOrg = (await _organizationRepository.GetAllAsync(p => p.ParentId == parentOrg.Id))
                .OrderByDescending(p => p.Id).FirstOrDefault();
            if (maxLevelOrg == null)
                orgCode = "1".PadLeft(HeroConstants.CodeRuleRestrain.CodeCoverBit,
                    HeroConstants.CodeRuleRestrain.CodeCoverSymbol);
            else
                orgCode =
                    (Convert.ToInt32(maxLevelOrg.Code.Split(HeroConstants.CodeRuleRestrain.CodeSeparator).Last()
                        .TrimStart('0')) + 1).ToString().PadLeft(HeroConstants.CodeRuleRestrain.CodeCoverBit,
                        HeroConstants.CodeRuleRestrain.CodeCoverSymbol);
            orgInfo.Code = parentOrg.Code + HeroConstants.CodeRuleRestrain.CodeSeparator + orgCode;
            orgInfo.Level = parentOrg.Level + 1;
            await UnitOfWorkAsync(async (conn, trans) =>
            {
                var orgId = await _organizationRepository.InsertAndGetIdAsync(orgInfo, conn, trans);
                corporation.OrgId = orgId;
                await _corporationRepository.InsertAsync(corporation, conn, trans);
            }, Connection);
            return new CreateCorporationOutput
            {
                OrgId = orgInfo.Id,
                CorporationId = corporation.Id,
                Tips = "新增公司信息成功"
            };
        }

        private async Task<CreateCorporationOutput> CreateTopCorporation(CreateCorporationInput input,long? tenantId = null)
        {
            if (input.Mold != CorporationMold.Group && input.Mold != CorporationMold.Monomer)
                throw new BusinessException("公司类型不正确,顶层公司只能指定为:集团公司或单体公司");
            var orgCode = string.Empty;
            var maxTopOrg =
                (await _organizationRepository.GetAllAsync(p =>
                    p.Level == 1 && p.OrgType == OrganizationType.Corporation)).OrderByDescending(p => p.Id)
                .FirstOrDefault();
            if (maxTopOrg == null)
                orgCode = "1".PadLeft(HeroConstants.CodeRuleRestrain.CodeCoverBit,
                    HeroConstants.CodeRuleRestrain.CodeCoverSymbol);
            else
                orgCode = (Convert.ToInt32(maxTopOrg.Code.TrimStart('0')) + 1).ToString().PadLeft(
                    HeroConstants.CodeRuleRestrain.CodeCoverBit, HeroConstants.CodeRuleRestrain.CodeCoverSymbol);
            var topCorporation = input.MapTo<Corporation>();
            var topOrgInfo = input.MapTo<Organization>();
            topOrgInfo.Code = orgCode;
            topOrgInfo.Level = 1;
            if (tenantId.HasValue)
            {
                topCorporation.TenantId = tenantId;
                topOrgInfo.TenantId = tenantId;
            }

            await UnitOfWorkAsync(async (conn, trans) =>
            {
                var orgId = await _organizationRepository.InsertAndGetIdAsync(topOrgInfo, conn, trans);
                topCorporation.OrgId = orgId;
                await _corporationRepository.InsertAsync(topCorporation, conn, trans);
            }, Connection);
            return new CreateCorporationOutput
            {
                OrgId = topOrgInfo.Id,
                CorporationId = topCorporation.Id,
                Tips = "新增公司信息成功"
            };
        }
    }
}