﻿using System.ComponentModel.DataAnnotations;
using Surging.Hero.Common;

namespace Surging.Hero.Auth.IApplication.User.Dtos
{
    public class CreateUserInput : UserDtoBase
    {
        public CreateUserInput()
        {
            RoleIds = new long[0];
            UserGroupIds = new long[0];
        }

        [Required(ErrorMessage = "部门信息不允许为空")]
        public long? OrgId { get; set; }

        public long? PositionId { get; set; }

        [Required(ErrorMessage = "用户名不允许为空")]
        [RegularExpression(RegExpConstants.UserName, ErrorMessage = "用户名格式不正确")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "密码不允许为空")]
        [RegularExpression(RegExpConstants.Password, ErrorMessage = "密码格式不正确")]
        public string Password { get; set; }

        /// <summary>
        ///     分配的角色
        /// </summary>
        public long[] RoleIds { get; set; }

        /// <summary>
        ///     分配的用户组
        /// </summary>
        public long[] UserGroupIds { get; set; }
    }
}