﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Surging.Cloud.CPlatform.Ioc;
using Surging.Cloud.CPlatform.Runtime.Server.Implementation.ServiceDiscovery.Attributes;
using Surging.Cloud.ProxyGenerator.Interceptors.Implementation.Metadatas;
using Surging.Cloud.System.Intercept;
using Surging.Hero.Common;
using Surging.Hero.Organization.IApplication.Position.Dtos;

namespace Surging.Hero.Organization.IApplication.Position
{
    [ServiceBundle(HeroConstants.RouteTemplet)]
    public interface IPositionAppService : IServiceKey
    {
        /// <summary>
        ///     获取职位信息
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [ServiceRoute("{id}")]
        [HttpGet]
        [InterceptMethod(CachingMethod.Get, Key = CacheKeyConstant.GetPositionById, Mode = CacheTargetType.Redis)]
        [Service(Director = Developers.Liuhll, Date = "2020-07-04", Name = "获取职位信息", AllowPermission = true)]
        Task<GetPositionOutput> Get([CacheKey(1)] long id);

        /// <summary>
        ///     根据部门id获取部门职位
        /// </summary>
        /// <param name="deptId"></param>
        /// <returns></returns>
        [ServiceRoute("dept/{deptId}")]
        [HttpGet]
        [InterceptMethod(CachingMethod.Get, Key = CacheKeyConstant.GetDeptPositionById, Mode = CacheTargetType.Redis)]
        [Service(Director = Developers.Liuhll, Date = "2020-07-04", Name = "根据部门id获取部门职位", AllowPermission = true)]
        Task<IEnumerable<GetPositionOutput>> GetDeptPosition([CacheKey(1)] long deptId);

        /// <summary>
        ///     根据组织机构id获取职位列表
        /// </summary>
        /// <param name="orgId"></param>
        /// <returns></returns>
        [ServiceRoute("org/{orgId}")]
        [HttpGet]
        [InterceptMethod(CachingMethod.Get, Key = CacheKeyConstant.GetDeptPositionByOrgId,
            Mode = CacheTargetType.Redis)]
        [Service(Director = Developers.Liuhll, Date = "2020-07-04", Name = "根据组织机构id获取职位列表", AllowPermission = true)]
        Task<IEnumerable<GetPositionOutput>> GetDeptPositionByOrgId([CacheKey(1)] long orgId);

        /// <summary>
        ///     检查某个职位是否允许删除
        /// </summary>
        /// <param name="positionId"></param>
        /// <returns></returns>
        [HttpPost]
        [ServiceRoute("check/{positionId}")]
        [Service(Director = Developers.Liuhll, Date = "2020-07-04", Name = "检查某个职位是否允许删除", AllowPermission = true)]
        Task<bool> CheckCanDeletePosition(long positionId);

        /// <summary>
        ///     检查某个职位是否存在
        /// </summary>
        /// <param name="positionId"></param>
        /// <returns></returns>
        [HttpPost]
        [Service(Director = Developers.Liuhll, Date = "2020-07-04", Name = "检查某个职位是否存在", DisableNetwork = true)]
        Task<bool> CheckExsit(long positionId);
    }
}