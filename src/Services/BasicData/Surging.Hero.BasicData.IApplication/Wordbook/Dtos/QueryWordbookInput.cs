﻿using Surging.Cloud.Domain.PagedAndSorted;

namespace Surging.Hero.BasicData.IApplication.Wordbook.Dtos
{
    public class QueryWordbookInput : PagedResultRequestDto
    {
        public string SearchKey { get; set; }
    }
}