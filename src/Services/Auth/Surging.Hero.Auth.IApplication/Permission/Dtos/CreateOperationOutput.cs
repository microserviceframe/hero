﻿namespace Surging.Hero.Auth.IApplication.Permission.Dtos
{
    public class CreateOperationOutput
    {
        /// <summary>
        ///     操作Id
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        ///     权限Id
        /// </summary>
        public long PermissionId { get; set; }

        /// <summary>
        ///     提示信息
        /// </summary>
        public string Tips { get; set; }
    }
}