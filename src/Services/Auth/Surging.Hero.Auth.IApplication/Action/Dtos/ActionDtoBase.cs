﻿using System;

namespace Surging.Hero.Auth.IApplication.Action.Dtos
{
    public abstract class ActionDtoBase
    {
        public string ServiceId { get; set; }
        public string ServiceHost { get; set; }
        public string Application { get; set; }
        public string Name { get; set; }

        public string WebApi { get; set; }

        public string Method { get; set; }

        public bool DisableNetwork { get; set; }

        public bool EnableAuthorization { get; set; }

        public bool AllowPermission { get; set; }
        public string Developer { get; set; }

        public DateTime? Date { get; set; }
    }
}