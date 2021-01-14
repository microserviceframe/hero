﻿using System.Threading.Tasks;
using Autofac;
using Org.BouncyCastle.Utilities;
using Surging.Cloud.Caching.DependencyResolution;
using Surging.Cloud.CPlatform.Utilities;
using Surging.Cloud.Domain.Entities.Auditing;
using Surging.Cloud.ProxyGenerator;
using Surging.Hero.Auth.IApplication.User;
using Surging.Hero.Common.FullAuditDtos;

namespace Surging.Hero.Auth.IApplication.FullAuditDtos
{
    public static class FullAuditDtoExtensions
    {
        public static async Task SetAuditInfo(this IAuditedDto auditDto)
        {
            var userAppServiceProxy = GetUserAppService();
            if (auditDto.CreatorUserId.HasValue)
            {
                var creator = await userAppServiceProxy.GetUserBasicInfo(auditDto.CreatorUserId.Value);
                if (creator != null) auditDto.CreatorUserName = creator.ChineseName;
            }

            if (auditDto.LastModifierUserId.HasValue)
            {
                var modificationUser = await userAppServiceProxy.GetUserBasicInfo(auditDto.LastModifierUserId.Value);
                if (modificationUser != null) auditDto.LastModificationUserName = modificationUser.ChineseName;
            }
        }


        private static IUserAppService GetUserAppService()
        {
            if (ServiceLocator.Current.IsRegistered<IUserAppService>())
            {
                return ServiceLocator.GetService<IUserAppService>();
            }

            var result = ServiceResolver.Current.GetService<IUserAppService>();
            if (result == null)
            {
                result = ServiceLocator.GetService<IServiceProxyFactory>().CreateProxy<IUserAppService>();
                ServiceResolver.Current.Register(null, result);
            }

            return result;
        }
    }
}