﻿using System.ComponentModel.DataAnnotations;

namespace Surging.Hero.BasicData.IApplication.Wordbook.Dtos
{
    public class CreateWordbookItemInput : WordbookItemDtoBase
    {
        public long WordbookId { get; set; }

        [Required(ErrorMessage = "字典项编码不允许为空")]
        [MaxLength(50, ErrorMessage = "字典项值长度不允许超过50")]
        public string Key { get; set; }
    }
}