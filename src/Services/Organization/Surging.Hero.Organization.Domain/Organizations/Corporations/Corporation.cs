﻿using System;
using Surging.Cloud.Domain.Entities.Auditing;
using Surging.Hero.Organization.Domain.Shared;

namespace Surging.Hero.Organization.Domain
{
    public class Corporation : FullAuditedEntity<long>, IMultiTenant
    {
        public CorporationMold Mold { get; set; }

        public string Address { get; set; }

        public string Logo { get; set; }

        public string LogoPosition { get; set; }

        public string CorporateRepresentative { get; set; }

        public DateTime RegisterDate { get; set; }

        public DateTime OpenDate { get; set; }

        public string Trade { get; set; }

        public string Memo { get; set; }

        public long OrgId { get; set; }
        
        public long? TenantId { get; set; }
    }
}