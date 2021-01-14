﻿using System.Threading.Tasks;
using Surging.Cloud.Dapper.Repositories;
using Surging.Cloud.ProxyGenerator;
using Surging.Cloud.Validation.DataAnnotationValidation;
using Surging.Hero.Organization.Domain.Organizations.Departments;
using Surging.Hero.Organization.IApplication.Department;
using Surging.Hero.Organization.IApplication.Department.Dtos;

namespace Surging.Hero.Organization.Application.Department
{
    public class DepartmentAppService : ProxyServiceBase, IDepartmentAppService
    {
        private readonly IDepartmentDomainService _departmentDomainService;
        private readonly IDapperRepository<Domain.Department, long> _departmentRepository;

        public DepartmentAppService(IDepartmentDomainService departmentDomainService,
            IDapperRepository<Domain.Department, long> departmentRepository)
        {
            _departmentDomainService = departmentDomainService;
            _departmentRepository = departmentRepository;
        }

        public async Task<bool> Check(long orgId)
        {
            var department = await _departmentRepository.SingleOrDefaultAsync(p => p.OrgId == orgId);
            if (department == null) return false;
            return true;
        }

        public async Task<CreateDepartmentOutput> Create(CreateDepartmentInput input)
        {
            input.CheckDataAnnotations().CheckValidResult();
            return await _departmentDomainService.CreateDepartment(input);
        }

        public async Task<string> DeleteByOrgId(long orgId)
        {
            await _departmentDomainService.DeleteDepartmentByOrgId(orgId);
            return "删除部门信息成功";
        }

        public async Task<GetDepartmentOutput> Get(long id)
        {
            return await _departmentDomainService.GetDepartment(id);
        }

        public async Task<GetDepartmentOutput> GetByOrgId(long orgId)
        {
            return await _departmentDomainService.GetDepartmentByOrgId(orgId);
        }

        public async Task<UpdateDepartmentOutput> Update(UpdateDepartmentInput input)
        {
            input.CheckDataAnnotations().CheckValidResult();
            return await _departmentDomainService.UpdateDepartment(input);
        }
    }
}