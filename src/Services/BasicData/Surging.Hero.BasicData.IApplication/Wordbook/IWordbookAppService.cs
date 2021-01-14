﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Surging.Cloud.CPlatform.Ioc;
using Surging.Cloud.CPlatform.Runtime.Server.Implementation.ServiceDiscovery.Attributes;
using Surging.Cloud.Domain.PagedAndSorted;
using Surging.Cloud.ProxyGenerator.Interceptors.Implementation.Metadatas;
using Surging.Cloud.System.Intercept;
using Surging.Hero.BasicData.IApplication.Wordbook.Dtos;
using Surging.Hero.Common;

namespace Surging.Hero.BasicData.IApplication.Wordbook
{
    [ServiceBundle(HeroConstants.RouteTemplet)]
    public interface IWordbookAppService : IServiceKey
    {
        /// <summary>
        ///     创建字典
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost]
        [ServiceRoute("")]
        [Service(Director = Developers.Liuhll, Date = "2020-07-04", Name = "创建字典")]
        Task<string> Create(CreateWordbookInput input);

        /// <summary>
        ///     更新字典
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPut]
        [ServiceRoute("")]
        [InterceptMethod(CachingMethod.Remove, CorrespondingKeys = new[] {CacheKeyConstant.GetWordBookById},
            Mode = CacheTargetType.Redis)]
        [Service(Director = Developers.Liuhll, Date = "2020-07-04", Name = "更新字典")]
        Task<string> Update(UpdateWordbookInput input);

        /// <summary>
        ///     删除字典
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [ServiceRoute("{id}")]
        [HttpDelete]
        [InterceptMethod(CachingMethod.Remove, CorrespondingKeys = new[] {CacheKeyConstant.GetWordBookById},
            Mode = CacheTargetType.Redis)]
        [Service(Director = Developers.Liuhll, Date = "2020-07-04", Name = "删除字典")]
        Task<string> Delete([CacheKey(1)] long id);

        /// <summary>
        ///     查询字典
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        [ServiceRoute("search")]
        [Service(Director = Developers.Liuhll, Date = "2020-07-04", Name = "查询字典", AllowPermission = true)]
        Task<IPagedResult<GetWordbookOutput>> Search(QueryWordbookInput query);

        /// <summary>
        ///     获取字典信息
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [ServiceRoute("{id}")]
        [HttpGet]
        [InterceptMethod(CachingMethod.Get, Key = CacheKeyConstant.GetWordBookById, Mode = CacheTargetType.Redis)]
        [Service(Director = Developers.Liuhll, Date = "2020-07-04", Name = "获取字典信息", AllowPermission = true)]
        Task<GetWordbookOutput> Get([CacheKey(1)] long id);

        /// <summary>
        ///     获取字典项
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [ServiceRoute("items/page")]
        [HttpPost]
        [Service(Director = Developers.Liuhll, Date = "2020-07-04", Name = "获取字典项")]
        Task<IPagedResult<GetWordbookItemOutput>> GetWordbookItems(GetWordbookItemsInput input);

        /// <summary>
        ///     新增字典项
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost]
        [ServiceRoute("item")]
        [Service(Director = Developers.Liuhll, Date = "2020-07-04", Name = "新增字典项")]
        Task<string> CreateWordbookItem(CreateWordbookItemInput input);

        /// <summary>
        ///     更新字典项
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPut]
        [ServiceRoute("item")]
        [InterceptMethod(CachingMethod.Remove,
            CorrespondingKeys = new[]
            {
                CacheKeyConstant.GetWordBookItemsById, CacheKeyConstant.GetWordBookItemById,
                CacheKeyConstant.RemoveGetWordBookItem, CacheKeyConstant.RemoveGetWordBookItems
            }, Mode = CacheTargetType.Redis)]
        [Service(Director = Developers.Liuhll, Date = "2020-07-04", Name = "更新字典项")]
        Task<string> UpdateWordbookItem(UpdateWordbookItemInput input);

        /// <summary>
        ///     删除字典项
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [ServiceRoute("item/{id}")]
        [HttpDelete]
        [InterceptMethod(CachingMethod.Remove,
            CorrespondingKeys = new[]
            {
                CacheKeyConstant.GetWordBookItemsById, CacheKeyConstant.GetWordBookItemById,
                CacheKeyConstant.RemoveGetWordBookItem, CacheKeyConstant.RemoveGetWordBookItems
            }, Mode = CacheTargetType.Redis)]
        [Service(Director = Developers.Liuhll, Date = "2020-07-04", Name = "删除字典项")]
        Task<string> DeleteWordbookItem([CacheKey(1)] long id);

        /// <summary>
        ///     获取字典项目
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [ServiceRoute("item/{id}")]
        [HttpGet]
        [InterceptMethod(CachingMethod.Get, Key = CacheKeyConstant.GetWordBookItemById, Mode = CacheTargetType.Redis)]
        [Service(Director = Developers.Liuhll, Date = "2020-07-04", Name = "获取字典项目", AllowPermission = true)]
        Task<GetWordbookItemOutput> GetWordbookItem([CacheKey(1)] long id);

        /// <summary>
        ///     检查是否存在某个字典项
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost]
        [ServiceRoute("check")]
        [Service(Director = Developers.Liuhll, Date = "2020-07-04", Name = "检查是否存在某个字典项", DisableNetwork = true)]
        Task<bool> Check(CheckWordbookInput input);

        [ServiceRoute("items/code/{code}")]
        [HttpGet]
        [InterceptMethod(CachingMethod.Get, Key = CacheKeyConstant.GetWordBookItemsByCode,
            Mode = CacheTargetType.Redis)]
        [Service(Director = Developers.Liuhll, Date = "2020-07-04", Name = "根据字典标识获取字典项", AllowPermission = true)]
        Task<IEnumerable<GetWordbookItemOutput>> GetWordbookItemsByCode([CacheKey(1)] string code);

        [ServiceRoute("item/key/{key}")]
        [HttpGet]
        [InterceptMethod(CachingMethod.Get, Key = CacheKeyConstant.GetWordBookItemByKey, Mode = CacheTargetType.Redis)]
        [Service(Director = Developers.Liuhll, Date = "2020-12-13", Name = "根据字典项的key获取字典信息", AllowPermission = true)]
        Task<GetWordbookItemOutput> GetWordbookItemByKey([CacheKey(1)] string wordbookCode, [CacheKey(2)] string key);
    }
}